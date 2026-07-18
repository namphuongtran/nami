---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the existing structural ADRs (ADR-0020, ADR-0024, ADR-0021, ADR-0047) and the DDD / SOLID literature
informed: all contributors, via this repository
---

# Adopt Separation of Concerns and pragmatic SOLID as binding architectural principles

## Context and Problem Statement

Nami records its architecture decision-first: dozens of ADRs, authored over time, by different contributors. Individual ADRs already embody a consistent design philosophy (hexagonal shell with vertical slices in ADR-0024, bounded contexts and a single rich aggregate in ADR-0020, seam isolation in ADR-0021, a swappable authorization port in ADR-0047), but that philosophy was never itself recorded. The "why" behind those structural choices lived tacitly in the reviewer's head.

Without the principles written down as a shared, citable reference, three things happen as the project grows: new ADRs re-derive (or quietly drift from) the same reasoning; contributors cannot tell whether a proposed design "fits Nami" until a maintainer intuits it; and the principles cannot be superseded deliberately because they were never stated. This ADR records the principles that future decisions are evaluated against. It sits above the structural ADRs as their shared rationale; it does not replace or restate their concrete rules.

## Decision Drivers

* Give contributors a stable yardstick so design decisions stay consistent across authors and across time.
* Make the principles citable from future ADRs and from code review, rather than tacit.
* Keep the principles pragmatic: they must match what Nami actually builds, not import textbook doctrine that a real decision has already rejected.
* Preserve the "start simple, add a boundary only when it earns its place" posture that ADR-0024 already committed to.

## Considered Options

* Leave the principles tacit, implied by the individual structural ADRs.
* Record them as soft guidance in `CONTRIBUTING.md` or `CLAUDE.md`.
* Record them as a binding foundational ADR that the structural ADRs cross-reference.

## Decision Outcome

Chosen option: "a binding foundational ADR", because the principles are meant to govern future decisions, and in this repository a decision that governs other decisions is an ADR (binding until superseded), not agent guidance or a contributor tip. The two principles below are stated abstractly and then anchored to the decisions that already apply them, so they are testable rather than aspirational.

### Principle 1: Separation of Concerns

Each part of the system has one clear reason to change. Nami separates concerns at three levels, each already decided:

* **Bounded contexts separate business concerns.** Core-IdP, Admin, and the control plane are distinct contexts with their own ubiquitous language (ADR-0020). A concern that belongs to one context is not reached into from another.
* **Aggregates separate consistency concerns.** Tactical DDD is applied only where a real invariant exists: the `Proposal` aggregate owns dual-control consistency (proposer not equal to approver, single-use, expiry, TOCTOU safety, a state machine, ADR-0020). CRUD without an invariant carries no aggregate ceremony. Consistency boundaries are drawn where the rules are, not everywhere.
* **Vertical slices separate feature concerns.** The Application layer is organized by feature slice (`Features/<Area>/<UseCase>/`), each slice grouping its request, handler, validator, and response, with coupling minimized between slices and cohesion maximized within one (ADR-0024).

Where these concerns are mixed, the system becomes fragile; keeping them separate is why a change to shipping-style delivery rules in one context cannot destabilize token issuance in another.

### Principle 2: SOLID, applied pragmatically at the architectural level

SOLID is treated as an architectural discipline (over services, contexts, slices, and ports), not as a per-class checklist. It is applied where it buys isolation, and deliberately not applied where it would only add ceremony.

* **Single Responsibility.** A bounded context, a slice, or a port focuses on one business responsibility and changes for one reason. This aligns with the bounded-context boundaries of ADR-0020 and the slice cohesion of ADR-0024.
* **Open/Closed.** The system is extended by adding new components, not by rewriting existing ones. Nami's real extension seams are: adding an infrastructure adapter behind an existing port (key/secret/data-protection, audit sink, tenant store, per ADR-0006/0009/0008/0001) leaves the domain untouched; inserting an OpenIddict handler at a named order-anchor extends the protocol pipeline without forking the engine (ADR-0021, ADR-0024); the authorization engine is swappable to ReBAC behind the `ICheckAccess` port (ADR-0047); and per-client policy providers, a dynamic external-IdP scheme provider, and demand-driven federation extensions (ADR-0050, ADR-0034, ADR-0055/0056/0057) each add behavior without editing the core. Event reaction is an Open/Closed mechanism only at the edges (the audit outbox and back-channel-logout fan-out, ADR-0008/0019); there is deliberately no message-bus backbone and event-driven architecture is forbidden on the synchronous dual-control path, because eventual consistency there would be a security bug (ADR-0020).
* **Dependency Inversion.** High-level business logic does not depend on infrastructure detail. The dependency rule is absolute: Domain references no OpenIddict, EF Core, or cloud SDK, and infrastructure is plugged in from the outside through edge ports (ADR-0024). Business rules stay stable when the technology behind a port changes.
* **Interface Segregation and Liskov, pragmatically.** Ports are narrow and a port must have at least two real reasons to exist (a genuine swap, a test seam, or a real boundary); a single-implementation interface created only to satisfy layering is rejected as noise (ADR-0024). Any adapter must be fully substitutable behind its port, which is what makes the cloud-agnostic swaps and in-process test fakes safe.

**The pragmatism guardrail (binding).** These principles are applied to earn isolation, never as ceremony for its own sake. Do not create a boundary, a port, or an aggregate that has no invariant or swap behind it. CRUD stays thin. "Start simple; add a boundary when it earns its place" (ADR-0024) governs when a principle is applied, and this ADR does not override it.

**Enforcement.** The principles are checked, not merely stated: the `Nami.Identity.ArchitectureTests` suite (TngTech.ArchUnitNET, per ADR-0024) enforces the dependency rule and slice decoupling in CI; ADR review evaluates new decisions against these principles and cites this ADR; and the slice template plus the code-review checklist compensate for the guardrails that vertical slice removes.

### Consequences

* Good, because contributors and future ADRs now have one citable yardstick, so design stays consistent across authors and over time without a maintainer re-deriving the rationale each time.
* Good, because every principle is anchored to a decision that already applies it and to an enforcement mechanism, so this ADR is testable rather than aspirational.
* Good, because the pragmatism guardrail makes explicit that the principles are subordinate to "start simple", preventing this record from being used to justify over-engineering.
* Bad, because principles stated once can ossify; this is mitigated by the fact that an ADR is supersede-able, so a principle that stops fitting is revised by a follow-up ADR, not quietly ignored.
* Bad, because a principle can be cited to block a pragmatic exception; this is mitigated by the guardrail and by the precedent exceptions already on record (the BFF composition boundary with no port, ADR-0029; edge-only eventing, ADR-0020).

### Confirmation

* The dependency-rule and slice-decoupling clauses are confirmed by the ArchUnitNET architecture tests in CI (ADR-0024).
* Separation-of-concerns at the context and aggregate level is confirmed against ADR-0020 (bounded contexts; the `Proposal` aggregate as the sole tactical-DDD case).
* The Open/Closed framing was reconciled against ADR-0020 during authoring: the "event-driven consumers" pattern is scoped to the edges and the no-message-bus / EDA-forbidden-on-dual-control decision is preserved, rather than asserting an event-driven backbone Nami does not have.
* Compliance of a new decision is confirmed at ADR review by checking it against Principle 1 and Principle 2 and citing this ADR.

## Pros and Cons of the Options

### Leave the principles tacit

* Good, because it needs no work and the structural ADRs already imply the principles.
* Bad, because the rationale stays in the maintainer's head, new ADRs drift, and there is nothing to cite or to supersede.

### Soft guidance in CONTRIBUTING.md or CLAUDE.md

* Good, because it is lightweight and close to the contributor workflow.
* Bad, because principles that govern binding decisions are themselves a decision; a contributor tip or an agent-config file is neither the public decision record nor supersede-able, and human contributors would not treat it as binding.

### Binding foundational ADR (chosen)

* Good, because it is binding until superseded, citable by number, reviewable, and lives in the public decision record where the structural ADRs already point.
* Bad, because it adds one more foundational document to keep aligned with the structural ADRs, mitigated by anchoring each principle to those ADRs rather than restating their rules.

## More Information

* This ADR is foundational in intent but late in number: ADRs are numbered chronologically, not topically, and new decisions continue from 0036, so a shared-rationale record authored now is ADR-0058. Its authority comes from its `accepted` status and its cross-references, not from a low number.
* Related decisions: ADR-0024 (the hexagonal shell plus vertical slices that apply these principles structurally), ADR-0020 (bounded contexts, the `Proposal` aggregate, and the edge-only eventing / no-EDA-backbone rule), ADR-0021 (seam isolation), ADR-0047 (the swappable `ICheckAccess` port), ADR-0029 (the BFF composition-boundary exception), the infrastructure-port ADRs (ADR-0001/0006/0008/0009), and ADR-0066 (design patterns applied pragmatically, which extends this ADR's guardrail to the pattern catalog).
* Authored fresh for this repository (not imported from the design corpus); the Order/Shipping illustration common to SOLID material is replaced with Nami's own contexts and seams so the record states what Nami actually builds.
