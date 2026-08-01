---
status: reviewed
created: 2026-07-26
tags: [architecture, solution-strategy, arc42]
---

# Solution strategy

> **Part of:** the [Software Architecture Document](README.md), the bridge from the problem to
> the structure.

The previous views state the problem: who cares
([02-stakeholders-and-concerns](02-stakeholders-and-concerns.md)), what forces apply
([03-drivers-and-constraints](03-drivers-and-constraints.md)), and where the system ends
([04-system-context](04-system-context.md)). The views after this one show the structure that
resulted. **This view is the short answer in between: the handful of decisions that determined
everything downstream**, so a reader can understand why the system looks the way it does
without reverse-engineering it from the diagrams.

It introduces nothing. Every strategy here is an accepted decision, named, and the point of
gathering them is that **their interaction is not visible from any one of them**.

## 1. Seven strategies, and the goal each one buys

| # | Strategy | Bought | Decision of record |
|---|---|---|---|
| S1 | **Build on an existing protocol engine; do not write OAuth** | C10 conformance | ADR-0021, ADR-0014 |
| S2 | **Tiered tenancy, with isolation in layers rather than one mechanism** | C1 isolation | ADR-0001, ADR-0033, ADR-0049 |
| S3 | **One database engine, and the cache is never a source of truth** | C4, C5 | ADR-0037, ADR-0039, ADR-0074 |
| S4 | **Stateless processes with state pushed outward** | C3 scale | ADR-0031, ADR-0041 |
| S5 | **A modular monolith: hexagonal shell, vertical slices, ports only at real seams** | C8 evolvability | ADR-0024, ADR-0058, ADR-0059 |
| S6 | **Keys rotate without a restart** | C2 | ADR-0011, ADR-0012, ADR-0005 |
| S7 | **Additive evolution, killed by composition rather than by configuration** | C8 | ADR-0034, ADR-0035, ADR-0071 |

Concern identifiers are from
[02-stakeholders-and-concerns](02-stakeholders-and-concerns.md) section 2.

## 2. What each one actually commits to

### S1. Build on an existing protocol engine

Nami implements the parts of an identity provider that are **product**, and delegates the
parts that are **protocol** to a certified engine. Writing an authorization server from the
specifications is where identity products acquire their worst defects, and conformance is not
a thing a small project can self-certify into existence.

The commitment this creates is a **version dependency on someone else's roadmap**, and the
strategy is only safe because it is paired with two things: seam isolation, so a breaking
change touches an adapter rather than every caller, and a contract-regression suite that runs
on every version bump (ADR-0021). Where a capability is genuinely absent from the engine, the
answer is a handler inserted at a named order anchor, not a fork.

The same reasoning sets the protocol scope: sender-constrained tokens are built because they
answer a real threat, while the message-signing and high-assurance profiles are de-scoped for
want of a use case, with the trigger that reopens them recorded (ADR-0014).

### S2. Isolation in layers, not in one mechanism

Tenancy is **tiered rather than uniform**: shared-schema pooling by default because it is what
makes many tenants affordable, and a dedicated database per tenant on demand for those whose
requirements need it (ADR-0001). Choosing per tenant instead of once for the product is what
lets one deployment serve both.

The load-bearing part is that isolation is enforced **more than once, by different kinds of
mechanism**, so no single failure is a breach: a tenant discriminator in the application, forced
row-level security in the database as a backstop that holds even against application error, and
issuer plus tenant binding at the resource server.

That last one carries the strategy's sharpest consequence. Because a pool group shares a signing
keyset, **the signature is not an isolation boundary** (ADR-0049). A resource server that
validates only the signature re-opens cross-tenant acceptance, so part of this architecture's
correctness lives in code this project does not own and has to be stated as an obligation rather
than assumed. The residual blast radius of the shared keyset is a named accepted risk rather
than an unstated consequence (ADR-0033).

### S3. One engine, and the cache is an accelerator

A single database engine (ADR-0037) rather than a portable subset, because the isolation
strategy above depends on capabilities that are not portable: row-level security, `SET LOCAL`
for the per-request tenant variable, and `FOR UPDATE SKIP LOCKED` for queue drains. A portable
data layer would have cost the strongest isolation mechanism.

The paired rule is that **the cache is never a source of truth**. Revocation freshness is
achieved per path, with a backplane only where a path needs one, rather than by making
correctness depend on a cache being warm and current (ADR-0039). A cache that is empty must
mean slower, never wrong. This is what allows the topology to treat Redis durability as an
operational lever rather than a correctness requirement (ADR-0074), and it is the reason the
recovery story is bounded.

### S4. Stateless processes

Every process is disposable and holds no request-spanning state, so capacity is added by adding
processes (ADR-0031). Self-contained signed access tokens keep the hot validation path off the
database entirely, and short lifetimes bound what a stale token can do.

The strategy's discipline is on **background work**, which is where statelessness usually
leaks. There are exactly three sanctioned patterns and no fourth: a leader-guarded singleton for
schedule-driven jobs, competing consumers claiming rows with `SKIP LOCKED` for queue drains, and
a separate invocation for the one job that should not run in the serving process at all. An
unguarded timer inside a serving process is forbidden, because it is correct at one replica and
silently wrong at two (ADR-0031).

What this strategy explicitly does **not** buy is a throughput number. The concurrency goal is
an architecture target established by the project's own load test, and what is enforced is the
service-level objective rather than a headline rate (ADR-0041).

### S5. A modular monolith

One deployable core rather than services, with the boundaries drawn **inside** it: a hexagonal
dependency rule at the outside, vertical feature slices within (ADR-0024). An identity provider
is one consistency domain, and splitting it into services would buy independent deployment at
the cost of distributed transactions on exactly the paths that must not be eventually
consistent.

Two guardrails keep this from becoming a big ball of mud. Ports exist only where there is a
**real** seam, meaning a genuine swap, a test seam, or an actual boundary; a single-implementation
interface created to satisfy layering is rejected as noise. And aggregates are used only where a
transactional invariant exists, so entities without invariants carry no ceremony (ADR-0058,
ADR-0059).

The dual-control admin path is where this is most visible: event-driven indirection is
**forbidden** there, because eventual consistency on an approval check is a security defect
rather than a design preference (ADR-0020).

### S6. Keys rotate without a restart

Signing and encryption keys live in the database and are surfaced to the protocol engine through
the #1434 options seam, so rotation is a data change rather than a deployment (ADR-0011). The
overlap window is what makes it invisible: a new key is published before it signs, and a retired
key is deleted only after any token it signed has expired.

This strategy is the one that makes several others affordable. A short cryptoperiod costs nothing
operationally when rotation needs no restart, and break-glass key removal becomes a runbook step
rather than an outage. The encryption credential is tracked on its own lifecycle rather than
being assumed to follow the signing key, because their retention floors are set by different
things (ADR-0005). Cold start seeds the keyring automatically and roots it in a certificate, so a
new deployment is not a manual ceremony (ADR-0012).

### S7. Additive evolution, killed by composition

Everything beyond v1 attaches by **calling a registration extension**, and is absent if that call
is absent. Nothing is behind a feature flag.

The distinction is the point: **a flag leaves the code path present and one mistake away from
live, while an unregistered module leaves nothing to be mistaken about.** It also means v1 is
exactly what the other views describe, with nothing to disable. The catalogue of what attaches
this way is [19-evolution-and-extensions](19-evolution-and-extensions.md); this view states only
why the mechanism is composition (ADR-0034, ADR-0035, ADR-0071).

## 3. Where the strategies pull against each other

Three tensions are structural rather than incidental, and each is resolved by a named
decision rather than by a compromise.

| Tension | Resolution |
|---|---|
| **Pooling is affordable; pooling shares a keyset** (S2 against S2) | Isolation is moved off the signature and onto issuer and tenant binding plus row-level security, and the residual is an accepted risk with a named ratifier rather than a silence (ADR-0049, ADR-0033) |
| **Erasure wants data gone; a tamper-evident chain wants nothing altered** (C6) | Chain over commitments rather than over content, so the chain verifies after the content is crypto-shredded. Naming this as a tension is what forced a mechanism satisfying both instead of a compromise satisfying neither (ADR-0016, ADR-0008) |
| **Statelessness wants no leader; some jobs must run once** (S4) | Not resolved by exception but by classification: three sanctioned patterns, each with a different answer to "what happens at two replicas", and a job that fits none of them fails an architecture test (ADR-0031) |

The third row is the one worth generalising. The failure this design guards against is not
"used the wrong base class"; it is a job being **unclassified**, which is how an unguarded timer
gets written in the first place.

## 4. What was deliberately not chosen

Recording the rejected shape matters as much as the chosen one, because these are the questions
a reader will otherwise ask again.

* **Not microservices.** One consistency domain, and the dual-control path must not be eventually
  consistent (ADR-0024, ADR-0020).
* **Not a portable data layer.** The strongest isolation mechanism is not portable, and pretending
  otherwise would have meant giving it up (ADR-0037).
* **Not a message-bus backbone.** Event reaction is used at the edges, specifically the audit
  outbox and the logout fan-out, and nowhere on a synchronous authorization path (ADR-0058).
* **Not a hand-written protocol implementation.** See S1.
* **Not a general-purpose third-party library for mediation, mapping, or messaging.** Where a
  pattern is needed, a small first-party implementation scoped to that need is preferred over
  inheriting a dependency's licensing risk (ADR-0026).
* **Not feature flags for optional capability.** See S7.

## Sources

* ADR-0021 and ADR-0014 (S1: seam isolation, per-bump contract regression, and the protocol
  scope with its de-scope triggers), ADR-0001, ADR-0033, and ADR-0049 (S2: the tiered model, the
  keyset scope, and the signature-is-not-a-boundary consequence), ADR-0037, ADR-0039, and
  ADR-0074 (S3: the single engine, per-path freshness, and durability as a lever rather than a
  requirement), ADR-0031 and ADR-0041 (S4: the process model, the three background patterns, and
  the target-versus-objective distinction), ADR-0024, ADR-0058, and ADR-0059 (S5: the shell, the
  slices, the port test, and the aggregate test), ADR-0011, ADR-0012, and ADR-0005 (S6: the
  options-monitor seam, bootstrap, and the separate encryption lifecycle), ADR-0034, ADR-0035,
  and ADR-0071 (S7: the three features that attach by registration), ADR-0016, ADR-0008, and
  ADR-0020 (section 3's tensions), ADR-0026 (the dependency posture in section 4).
* This view is **synthesis only**. Every claim traces to an accepted decision above, per the
  authority order in the [index](README.md); where a strategy and a decision disagree, the
  decision governs and this view is the bug.
* **No corpus counterpart.** The design corpus's architecture document has no solution-strategy
  chapter, so nothing was imported here. The gap was found on 2026-07-26 by mapping both
  document sets onto the arc42 template, which names this as its section 4: neither set had it,
  and the material existed only scattered across the decisions it gathers.

---

[Prev: System context](04-system-context.md) · [Index](README.md) · Next: [Domain model](06-domain-model.md)
