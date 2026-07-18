---
status: "accepted"
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
* **Chain of Responsibility** for the OpenIddict event-handler pipeline that owns the protocol flow; custom logic is an inserted handler at a named order-anchor, never a fork (ADR-0024/0021).
* **Factory** for the pooled `DbContext` in Pool mode (ADR-0018).
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

* Related decisions: ADR-0058 (SOLID and the pragmatism guardrail this ADR applies to patterns), ADR-0024 (the architecture and the "start simple, no single-implementation interface" rule), ADR-0059 (the DDD tactical building blocks), ADR-0065 (naming, including calling a pattern by its catalog name), ADR-0061 (the index-versus-authority split), and the pattern-owning ADRs cited in the map (0006/0009, 0047, 0050, 0034, 0018, 0008, 0038, 0020, 0011, 0052).
* Reference (named factually, adopted by reference): the Gang of Four design-pattern catalog, with refactoring.guru as the recommended readable reference.
* Authored fresh for this repository; the generic textbook examples common to pattern material are replaced with Nami's own usages.
