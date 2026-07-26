---
status: reviewed
created: 2026-07-26
tags: [architecture, risks, technical-debt]
---

# Risks and technical debt

> **Part of:** the [Software Architecture Document](README.md), supporting views.

The **known** architectural risks and the **deliberate** technical debt, stated plainly so they
are tracked rather than rediscovered.

The distinction is worth keeping sharp. A **risk** is something that may go wrong and needs a
response. **Technical debt** is a shortcut taken on purpose, with a known repayment path. A
thing that is neither, because it was decided that way and is not owed back, is **not debt** and
listing it as debt schedules work nobody agreed to. Section 3 exists because that mistake is
easy to make.

Neither a risk nor a debt entry is a compliance verdict.

## 1. Architectural risks

Likelihood and impact are qualitative. The response column is the mitigation **already in the
design**, not a plan.

| # | Risk | L | I | Response already designed | Owner |
|---|---|---|---|---|---|
| R1 | **Pool shared-keyset blast radius.** Compromising a pool-group signing keyset affects every tenant in that group, not one | L | H | Isolation is enforced by **issuer and tenant binding plus row-level security, never by the signature**, so a shared key does not imply shared data. A tenant needing crypto isolation chooses Silo. This is an **accepted risk**, and accepting it is a named pre-GA item rather than an implicit consequence (ADR-0033, ADR-0049) | Security |
| R2 | **Throughput is unbenchmarked.** The concurrency goal is an architecture target; no published figure exists for this stack | M | M | The number is established by our own load test, and what is enforced is the **SLO**, not a headline rate (ADR-0041) | Product, Ops |
| R3 | **Erasure legal sufficiency is contested.** Whether crypto-shred satisfies the erasure right is a legal question with evolving guidance | M | H | The design builds the **mechanism** only, and routes the verdict to its owner. No compliance claim is made anywhere (ADR-0016) | Data protection, Legal |
| R4 | **Multi-tenant pruning is genuinely hard.** The engine has no multi-tenant cleanup story | M | M | Pruning iterates tenants explicitly and runs **off the request path** as its own invocation; the default schema was measured adequate rather than assumed (ADR-0031, ADR-0004) | Maintainers |
| R5 | **A protocol-engine major version can break the wiring.** Options types, validation hooks, and obsolete members all move across a major | M | M | Pinned version plus a **contract-regression suite on every bump**, and build-interim features isolated behind our own ports so a swap changes an adapter rather than every caller (ADR-0021) | Maintainers |
| R6 | **The engine emits no telemetry**, so a naive observability setup is blind on the protocol pipeline | H | L | Gap-filled with our own meter over the handler seams, carrying a decommission marker (ADR-0021, ADR-0022) | Maintainers |
| R7 | **Back-channel logout is an interim build**, and front-channel logout is dead under third-party-cookie blocking | M | M | Built now on the session store that already exists, with a decommission marker for the native equivalent. A relying party without a back-channel endpoint degrades to bounded logout at the access-token lifetime, which is a **stated parity boundary** (ADR-0019) | Maintainers |
| R8 | **Human ratification is not done.** Several parameters and verdicts are a named owner's call | H | M | Ratification runs **in parallel with the build** and gates production, not the build. Consolidated as one release gate (the [checklist](../PRE-GA-RATIFICATION-CHECKLIST.md)) | All named owners |
| R9 | **Acceptance evidence needs running code.** The load, conformance, recovery-drill, and cross-tenant negative gates cannot close before there is something to run | H | M | Wired as CI gates that land with the code they test; the SLO gate and the cross-tenant negative gate are must-pass (ADR-0041, ADR-0060, ADR-0001) | Maintainers |
| R10 | **Six load-bearing claims still have no owning decision**, of eight surfaced by writing and auditing this layer | M | M | Each is recorded **in place** in the view that carries it, marked as a candidate rather than presented as settled, and all are enumerated in [18-decisions-index](18-decisions-index.md) section 4 so the set is checkable rather than counted. They are being resolved one at a time: ADR-0075 closed the first and covered three other security-sensitive ports with it, and ADR-0076 closed the second and picked up a dangerous-toggle gap in passing | Maintainers |

R10 is this repository's own finding rather than an inherited one, and it is a risk in the
precise sense: nothing is wrong today, and the exposure is that a control with no decision
behind it can be changed by someone who does not know it is load-bearing.

## 2. Deliberate technical debt

| # | Debt | Why it was taken | Repayment |
|---|---|---|---|
| T1 | **The Pool-mode operational context is registered non-pooled** in v1 | A naively pooled context **leaked the previous tenant** in a spike test. Correctness beat the optimisation, and the leak was found by testing rather than reasoned about | A pooled-plus-mutable variant is a spike-gated post-v1 option. Deferring costs nothing at the schema, wire, or login level (ADR-0018) |
| T2 | **Our own telemetry instrumentation** over the engine | The engine emits none (R6) | Remove or thin it if native instrumentation ships. The decommission marker is what makes this repayable rather than permanent (ADR-0021) |
| T3 | **Interim back-channel logout** rather than native | The capability was needed now and the session store it requires already existed (R7) | Swap to the native mechanism at the next major. The relying-party registry and the delivery outbox stay, so the swap is the token-minting path only (ADR-0019) |

Three entries, not five. Two items the corpus lists as debt are **not** debt here, for reasons
worth stating rather than leaving as a silent difference: see section 3.

## 3. Things that look like debt and are not

**Self-service client registration is a chosen mechanism, not a placeholder.** It is tempting to
file "no standard registration endpoint" as debt repaid by the engine's next major. ADR-0035 is
explicit that it is **not gated on that major**: registration through the authenticated Admin
API builds on the current version and reuses the existing admin machinery, and the standard
endpoint is an **optional future addition** serving a capability the Admin API genuinely lacks,
namely programmatic self-registration by a tool or relying party. So a native endpoint arriving
**repays nothing**; it adds an option. Filing this as debt would schedule a migration nobody
decided on and would imply the current choice is a compromise.

**The de-scoped protocol surface is not debt either.** Several capabilities are deliberately out
of v1 scope: the message-signing tier, decoupled authentication, per-call active-user validation,
and front-channel logout, each with a recorded decision. Six more are `proposed` with an explicit
revisit trigger rather than simply absent (see [19-evolution-and-extensions](19-evolution-and-extensions.md)). A decision
not to build is not a debt, and the difference matters: debt accrues, whereas a recorded
non-decision waits without cost. If demand appears, these become planned work rather than
emergency work, which is the entire reason the analysis was written down.

## Sources

* ADR-0033 and ADR-0049 (the shared-keyset risk and the binding that contains it), ADR-0041 and
  ADR-0060 (the load-test target, the SLO gate, and the acceptance gates that need code),
  ADR-0016 (mechanism built, verdict reserved), ADR-0031 and ADR-0004 (pruning off the request
  path and its retention floor), ADR-0021 and ADR-0022 (version adaptation, the seam isolation
  that makes an interim swappable, and the telemetry gap), ADR-0019 (interim logout and its
  stated parity boundary), ADR-0018 (the non-pooled context and the spike that forced it),
  ADR-0001 (the cross-tenant negative gate), ADR-0035 (why self-service registration is not
  debt).
* The [Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md) is the authoritative
  list for R8; this view summarises rather than duplicates it.
* Reconciled against the design corpus's risks view on 2026-07-26. Taken from it: the
  risk-versus-debt convention, the risk register shape with a designed response rather than a
  plan, and most of the entries. Three differences. **One entry was reclassified**: the corpus
  files deferred client registration as debt repaid by adopting the standard endpoint at the
  next major, but ADR-0035 states it is not gated on that major and that the standard endpoint
  is an optional future for a different capability, so it is not debt and section 3 says why.
  **One entry was dropped**: the corpus's last debt item concerns illustrative naming in its own
  older documents, which is an artifact of that corpus rather than a property of this
  repository. **One entry was added**: R10, the load-bearing claims with no owning decision,
  which this repository found by writing and auditing its own architecture layer and which has
  no corpus counterpart. R10 read "six" until 2026-07-26; two further claims were found while
  writing the threat model and the count was never revisited, so the set is now enumerated in
  the decisions index and referenced from here rather than counted twice. Eight were found and
  six remain: the deny-by-default claim-destination rule became ADR-0075 and the application's
  transport security became ADR-0076, both on 2026-07-26.

---

[Prev: Reliability, backup, and DR](22-reliability-backup-dr.md) · [Index](README.md) · Next: [Glossary](24-glossary.md)
