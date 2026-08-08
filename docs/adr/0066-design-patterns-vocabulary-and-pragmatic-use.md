---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the Gang of Four design-pattern catalog (refactoring.guru as the readable reference); the principles and patterns already recorded in ADR-0058, ADR-0024, and the pattern-applying ADRs (0006/0009, 0047, 0050, 0034, 0018, 0008, 0038, 0020, 0011, 0052)
informed: all contributors, via this repository
---

# Adopt design patterns as a shared vocabulary applied pragmatically, not preemptively

## Context and Problem Statement

Nami already applies design patterns throughout, each recorded in the decision that uses it: the Adapter pattern is the whole cloud-agnostic ports story, Strategy is the swappable `ICheckAccess` engine, Chain of Responsibility is the OpenIddict handler pipeline, the Outbox carries audit and email, the `Proposal` aggregate is a State machine, and the Application layer uses optional CQRS-lite handlers. SOLID and Separation of Concerns are settled in ADR-0058, and the architecture in ADR-0024. What is not recorded is a shared vocabulary for these patterns, a reference for that vocabulary, and, most importantly, a rule for when a pattern is warranted.

Two failure modes follow from the gap. A contributor reinvents a known pattern under a private name, so reviewers cannot recognize it; or, worse, a contributor applies patterns preemptively as ceremony, which is precisely the over-engineering ADR-0058's pragmatism guardrail and ADR-0024's "start simple" rule exist to prevent. This ADR adopts the pattern catalog as shared vocabulary by reference, sets the pragmatic-use rule, and maps the patterns Nami already uses to their owning ADRs. It does not transcribe pattern tutorials, and it deliberately does not mandate patterns.

## Decision Drivers

* A shared vocabulary so a pattern is named the same way by everyone (ADR-0065 naming).
* A readable reference so contributors can learn a pattern without the project owning the teaching material.
* A guardrail against cargo-culting: the real risk is preemptive pattern use, not missing patterns.
* Reuse a known catalog rather than invent private names.
* Ground the guidance in this domain, not generic textbook examples.

## Considered Options

* Leave patterns implicit, recorded only per decision where they are used.
* Adopt the GoF catalog as shared vocabulary by reference, with a pragmatic-use rule and a map of patterns-in-use.
* Mandate design patterns ("always use patterns"), treating the catalog as a checklist.

## Decision Outcome

Chosen: "adopt the catalog as shared vocabulary, applied pragmatically." Mandating patterns is rejected because it contradicts the project's own guardrails; leaving them implicit is rejected because it loses the shared vocabulary and the anti-cargo-cult rule.

### Shared vocabulary by reference (binding)

The Gang of Four design-pattern catalog (creational, structural, behavioral) is Nami's shared vocabulary. When a pattern is used, it is called by its catalog name in code and docs so reviewers share the language (ADR-0065). The catalog is adopted by reference, not transcribed; refactoring.guru is the recommended readable reference, but the decision is the vocabulary, not any one site.

### The pragmatic-use rule (binding, the core of this ADR)

A pattern is introduced to solve a demonstrated problem, never preemptively. It must earn its place exactly as a port must (ADR-0058): prefer the simplest thing that works, and refactor toward a pattern only when duplication, real complexity, or genuine change-pressure demonstrates the need. This is ADR-0024's "start simple; do not create a single-implementation interface just to satisfy layering" applied to patterns. "Always use patterns" is explicitly not the rule; a pattern applied without a problem to solve is a defect, not good design.

### Patterns Nami already uses, mapped to their owners

Among the patterns already in deliberate use (not an exhaustive list):

* **Adapter** for the cloud-agnostic ports: key, secret, and data-protection stores (ADR-0006/0009), email delivery (ADR-0038), the tenant store (ADR-0001), EF persistence, and the `ICheckAccess` adapter (ADR-0047), all under the ports doctrine of ADR-0024.
* **Strategy** for swappable behavior: the `ICheckAccess` engine (DB-first now, ReBAC later, ADR-0047), the per-client CORS policy provider (ADR-0050), and the dynamic external-IdP scheme provider (ADR-0034).
* **Chain of Responsibility** for the OpenIddict event-handler pipeline that owns the protocol flow; custom logic is an inserted handler at a named order-anchor, never a fork (ADR-0021 parameter F, which owns the anchoring rule; the citation here read ADR-0024/0021 until 2026-08-02, and ADR-0024 rules on nothing in this pipeline: the only handler it defines is the feature-slice handler of a vertical slice, and its one mention of the "pipeline/handler model" is a consequence bullet arguing for the architecture style, not a rule about insertion).
* **Outbox** for the audit and email delivery paths, the sanctioned edge-eventing path (ADR-0008/0038/0020).
* **State** for the `Proposal` aggregate's state machine (ADR-0020) and the key-rotation lifecycle (ADR-0011).
* **Mediator / CQRS-lite** as an optional per-slice handler shape in the Application layer (ADR-0020/0024).
* **Options / Builder** for the ergonomic, fail-closed configuration layer (ADR-0052).

### Anti-patterns this rule forbids (binding)

* Wrapping a single implementation in an interface only to "use" Adapter or Strategy (ADR-0024 rejects the single-implementation interface).
* Adding a Mediator or CQRS layer where a plain method call suffices (CQRS-lite is optional, ADR-0024).
* Introducing event-driven choreography as a design pattern; edge-only eventing is allowed, an event-driven backbone is forbidden (ADR-0020).

Patterns serve the pragmatism guardrail; the guardrail does not bend to accommodate a pattern.

### Where the guidance lives

Each pattern-in-use is owned by the ADR that applies it; this ADR indexes them and does not override them (the index-versus-authority split of ADR-0061). The vocabulary reference is external. ADR review uses the shared catalog names and applies the pragmatic-use rule.

### Consequences

* Good, because contributors share one vocabulary, so a pattern in a PR is recognized rather than re-explained, and the patterns already in use are discoverable in one map.
* Good, because the pragmatic-use rule gives reviewers an explicit basis to reject preemptive pattern ceremony, which is the actual risk.
* Good, because it reuses a known catalog by reference and grounds every example in Nami's own decisions, so nothing is duplicated or invented.
* Bad, because "has this pattern earned its place" is a judgment call; mitigated by the same guardrail and review ADR-0058 already relies on.
* Bad, because a shared-vocabulary ADR must be kept from drifting into a pattern tutorial; mitigated by adopting the catalog by reference and keeping this ADR to the rule and the map.

## Pros and Cons of the Options

### Leave patterns implicit

* Good, because each pattern already lives in its owning decision.
* Bad, because there is no shared vocabulary and no recorded rule against preemptive use, so patterns get reinvented or cargo-culted.

### Shared vocabulary plus pragmatic-use rule plus map (chosen)

* Good, because it gives the vocabulary, the anti-cargo-cult rule, and the domain-grounded map, without duplicating tutorials or mandating patterns.
* Bad, because it needs judgment and must not drift into a tutorial; both mitigated as above.

### Mandate design patterns

* Good, because it would be simple to state.
* Bad, because it directly contradicts ADR-0058 and ADR-0024, invites over-engineering, and treats a toolbox as a checklist; rejected.

## More Information

* **Corrected 2026-08-08: the `Factory` entry was removed from the patterns-in-use list, because the
  pattern is in use nowhere and the entry named a deferred option as current.** It read "**Factory**
  for the pooled `DbContext` in Pool mode (ADR-0018)". [ADR-0018](0018-dbcontext-pooling-for-pool-mode.md)
  is titled "Register the Pool-mode OpenIddict DbContext **non-pooled** in v1, with pooled-plus-mutable
  deferred". Its Option A is the pooled one and uses `AddPooledDbContextFactory<T>` plus a scoped
  `IDbContextFactory<T>`; `0018:35` chooses Option B, and `0018:41` records the factory shape as "the
  A-4b pattern held for later". So the only source of a Factory here is an option that was not taken.
  * **This ADR's own core rule decides the repair.** The pragmatic-use rule above states that "a
    pattern applied without a problem to solve is a defect, not good design", and the heading claims
    the listed patterns are "already in deliberate use". An entry with no use contradicts both, so the
    fix is removal rather than a re-label. The fact itself is not lost: ADR-0018 owns A-4b and states
    the shape, and the "Where the guidance lives" section already assigns each pattern to the ADR that
    applies it, this one being an index that "does not override them".
  * **The searches are written down, because the whole claim is an absence.** Using `git grep -P`, the
    pattern `\bFactor(y|ies)\b` returned **exactly one** line across `docs/` excluding the work queue
    and the seed tracker, which was this entry itself, and **zero** across `src/` and `tests/`.
    `AddPooledDbContextFactory` occurs only at `0018:17`, which defines the term, and `0018:29`, which
    is Option A. The three genuinely pooled contexts are registered with `AddDbContextPool`
    (`docs/design/02-data.md:1164-1166`), which takes no factory. **The `-P` matters:** the same
    search written `git grep -cE "\bFactory\b"` returns 0 against a file that contains the word, which
    `docs/CLAUDE.md` records as this clone's word-boundary trap, and an absence written that way
    reports zero for every term.
  * **"Non-pooled" would have been the wrong correction too.** Pooling is used, per context. Read
    2026-08-08, `docs/design/02-data.md:55-59` pools `IdentityDbContext`,
    `DataProtectionDbContext`, and `ControlPlaneDbContext`, and leaves the two tenant-scoped contexts
    unpooled. [ADR-0061](0061-technology-stack-of-record.md) carries the accurate framing.
  * **The frontmatter is deliberately unchanged.** Its `consulted:` line groups ADR-0018 among "the
    pattern-applying ADRs". ADR-0018 genuinely was consulted on 2026-07-18, so the record of that
    reading stays; only the grouping label over-includes it, and editing a dated record to match a
    later finding is what this repository forbids.
  * **This is the fourth instance of one defect, and the previous three are on record.**
    `0061:145` corrected the same inversion in the stack table and
    `architecture/07-container-view.md:288-290` in that view, both 2026-07-25; `0061:118` predicted the
    rest. Seed S-024 owns the fifth, `architecture/03-drivers-and-constraints.md:116`, and the same
    seed as this change owns `0036:76`.
* Related decisions: ADR-0058 (SOLID and the pragmatism guardrail this ADR applies to patterns), ADR-0024 (the architecture and the "start simple, no single-implementation interface" rule), ADR-0059 (the DDD tactical building blocks), ADR-0065 (naming, including calling a pattern by its catalog name), ADR-0061 (the index-versus-authority split), and the pattern-owning ADRs cited in the map (0006/0009, 0047, 0050, 0034, 0018, 0008, 0038, 0020, 0011, 0052).
* Reference (named factually, adopted by reference): the Gang of Four design-pattern catalog, with refactoring.guru as the recommended readable reference.
* Authored fresh for this repository; the generic textbook examples common to pattern material are replaced with Nami's own usages.
