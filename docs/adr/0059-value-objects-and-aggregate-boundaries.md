---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: verified EF Core 8-10 documentation on complex types versus owned entity types (Microsoft Learn and the EF Core what's-new pages, checked 2026-07-18); the existing tactical-DDD decisions (ADR-0020, ADR-0036, ADR-0058)
informed: all contributors, via this repository
---

# Model value objects as complex types and gate aggregates on a transactional invariant

## Context and Problem Statement

Nami has recorded that every entity has a stable identity keyed by UUIDv7 (ADR-0036), that its one rich aggregate is `Proposal` while the rest is CRUD (ADR-0020), and that aggregates separate consistency concerns under a pragmatism guardrail (ADR-0058). What was never recorded is the tactical layer beneath those decisions: how a value-defined concept (a redirect URI, a scope name, a token lifetime, an email address, a key `kid`) is modeled and persisted, and what reusable rule tells a contributor whether a new aggregate is warranted or whether the change is plain CRUD.

Without this recorded, two contributors will model the same value three different ways (a bare `string`, an owned entity, a hand-rolled class), and value concepts will drift into identity bugs when persisted through the wrong Entity Framework Core construct. This ADR fixes the tactical-DDD conventions for Nami's own domain model. It uses the entity / value-object / aggregate vocabulary only as framing and cites the ADRs that already own each concept; the binding new content is the value-object modeling convention and the aggregate-boundary rule.

## Decision Drivers

* Consistency: one recorded answer to "how do I model this value" and "does this need an aggregate", so slices do not each reinvent it.
* Correct value semantics: a value object must compare and persist by value, never accidentally acquire identity, because identity leaking into a value is a subtle correctness bug.
* Do not fight framework-owned entities: ASP.NET Core Identity and OpenIddict own their own entity models, and Nami must not wrap them in its own building blocks.
* Keep CRUD thin: the convention must not become a mandate to build aggregates everywhere, which ADR-0058's pragmatism guardrail forbids.

## Considered Options

* **Value objects:** (a) primitives everywhere (stringly-typed); (b) owned entity types; (c) immutable records mapped as EF Core complex types.
* **Aggregates:** (a) an aggregate per entity (DDD-heavy); (b) no aggregates at all (pure CRUD); (c) an aggregate only around a real transactional invariant.

## Decision Outcome

Chosen: value objects as **immutable records mapped to EF Core complex types**, and aggregates introduced **only around a transactional invariant**, with `Proposal` remaining the exemplar and current sole aggregate.

### Shared vocabulary (framing; each concept's owner in parentheses)

* **Entity**: has a stable identity that persists over time; compared by identity, not by attributes; keyed by UUIDv7 (ADR-0036). In Nami's own domain: `Tenant`, `Membership`, `Role`, `Grant`, `Proposal` (the ubiquitous language of ADR-0020).
* **Value object**: has no identity and is defined only by its value; two value objects with equal attributes are equal. In Nami: a redirect URI, a scope name, a token lifetime, an email address, a key `kid`, a cryptoperiod (ADR-0011).
* **Aggregate and root**: a consistency boundary whose root is the only entry point for changes and the enforcer of the boundary's invariant; `Proposal` is the exemplar (ADR-0020).

### Value-object modeling (binding)

* Model a value object as an **immutable record** (a `record`, or a `record struct` where a small value benefits from value-type semantics), with all validation in the constructor so an instance is **valid by construction** and can never exist in an invalid state.
* Persist value objects as **EF Core complex types**, not owned entity types. Complex types carry value semantics (content equality, straightforward copying, correct LINQ comparisons) and are stored inline; owned entity types carry hidden identity and reference semantics that produce duplicate-reference and identity-comparison bugs when used for values. EF Core 10 explicitly steers value-object, JSON, and table-splitting usage from owned types to complex types, and adds optional (nullable) complex types, struct and record-struct support, and `.ToJson()` mapping.
* **Collection caveat (accurate as of EF Core 10):** complex types support single values only; a collection of value objects is not expressible as a complex type. Model a value-object collection as a JSON-mapped collection, an owned-type collection, or a child entity if the items in fact have identity. This is the one place owned types still earn their place, and it is a deliberate, documented exception rather than a default.
* A value earns a value-object type only when it has validation or behavior (a redirect URI enforces absolute-URI and scheme rules; a token lifetime enforces bounds); a value with neither stays a primitive. This is the ADR-0058 pragmatism guardrail applied to values.

### Framework-owned-entity boundary (binding)

Value objects and aggregates apply to **Nami's own domain model** (the control plane and the Admin domain). Framework-owned entities keep their native shapes: ASP.NET Core Identity users and roles (ADR-0028) and OpenIddict's application, scope, authorization, and token entities (ADR-0021) are not re-modeled or wrapped in Nami value objects. Where Nami holds its own copy of a value (for example an email in its user-lifecycle layer), it may use a value object there without imposing it on the framework's entity.

### Aggregate boundaries (binding)

* Introduce an aggregate **only when a real invariant spans more than one object and must hold within a single transaction** (ADR-0058 guardrail). Absent such an invariant, the change is CRUD and carries no aggregate ceremony (ADR-0020). `Proposal` is currently the only aggregate that clears this bar.
* The **aggregate root is the sole entry point** for changes to anything inside its boundary and is where the invariant is enforced. Other aggregates are referenced **by identity** (a UUIDv7, ADR-0036), never by navigating into their internals.
* **The transactional-consistency rule:** a single transaction modifies exactly one domain aggregate instance; the transactional-outbox write of an audit or outbox record within that same transaction is the deliberate atomic-capture exception (ADR-0008), an infrastructure concern rather than a second domain aggregate. If two things do not need to change in the same transaction, they belong to different aggregates. Cross-aggregate consistency is reached out of band (the audit and back-channel-logout outbox, ADR-0008/0019, or a domain service), never by enlarging an aggregate to span them. This reduces coupling, keeps transactions small under the tenant-scoped row-level-security model (ADR-0001), and preserves ADR-0020's rule that the dual-control path stays a single-aggregate, synchronous, TOCTOU-safe transaction rather than an eventually-consistent one.

### Enforcement and confirmation

Domain-convention checks in the `Nami.Identity.ArchitectureTests` suite where expressible (for example, value-object types are immutable and have no key), plus the slice template and code review (ADR-0024). Build-time confirmation: when persistence code lands (M1), confirm the complex-type mappings against real EF Core 10 behavior, in particular the single-value limitation and the chosen fallback for each value-object collection.

### Consequences

* Good, because there is now one recorded answer for modeling a value and for deciding whether an aggregate is warranted, so slices stay consistent and value concepts do not acquire accidental identity.
* Good, because it uses the EF Core construct the framework itself recommends for value objects, and records the collection limitation accurately so no contributor is surprised by it.
* Good, because it respects the framework-owned-entity boundary, so Nami does not fight ASP.NET Core Identity or OpenIddict.
* Good, because the transactional-consistency rule keeps transactions single-aggregate, which fits the RLS tenant model and the no-EDA-on-dual-control decision.
* Bad, because the complex-type collection limitation forces a per-case choice (JSON, owned collection, or child entity) rather than one uniform rule; mitigated by recording the three options and requiring the choice to be justified.
* Bad, because "a value earns a value-object type only when it has behavior" is a judgment call; mitigated by the pragmatism guardrail and code review, the same mitigation ADR-0058 already relies on.

## Pros and Cons of the Options

### Value objects as primitives (stringly-typed)

* Good, because it is zero ceremony.
* Bad, because validation scatters across call sites, invalid values are representable, and there is no place for value behavior; correctness bugs follow.

### Value objects as owned entity types

* Good, because it maps and persists structured values and predates complex types.
* Bad, because owned types are still entities with hidden identity and reference semantics, which causes duplicate-reference and content-comparison bugs for values; EF Core 10 explicitly recommends migrating value-object usage away from them.

### Value objects as records mapped to complex types (chosen)

* Good, because it gives true value semantics (equality, immutability, correct comparisons) with inline persistence, and is the framework's recommended construct.
* Bad, because collections of value objects are not supported and need a documented fallback; accepted as the single, bounded exception.

### Aggregate per entity

* Good, because boundaries are maximally explicit.
* Bad, because it is ceremony without invariants, which ADR-0058 forbids and which fattens transactions.

### No aggregates at all

* Good, because it is simplest.
* Bad, because the dual-control invariant genuinely needs a consistency boundary; `Proposal` would have nowhere to enforce proposer-not-approver and TOCTOU safety.

### Aggregate only around a transactional invariant (chosen)

* Good, because it matches ADR-0020's single aggregate and ADR-0058's guardrail, keeps CRUD thin, and keeps transactions single-aggregate.
* Bad, because "is there a real invariant" is a judgment call, mitigated by review.

## More Information

* Related decisions: ADR-0036 (UUIDv7 entity identity), ADR-0020 (the `Proposal` aggregate, the ubiquitous language, and CRUD-without-ceremony), ADR-0058 (Separation of Concerns and the pragmatism guardrail this ADR applies tactically), ADR-0024 (the vertical-slice domain core that houses these types, and the architecture tests), ADR-0037 (PostgreSQL with EF Core 10, the committed persistence stack), ADR-0021 and ADR-0028 (the framework-owned entities this ADR does not re-model), ADR-0001 (the tenant-scoped RLS model that single-aggregate transactions fit), and ADR-0008/0019 (the outbox path for cross-aggregate consistency).
* Build-time follow-up: confirm complex-type mappings and each value-object-collection fallback against real EF Core 10 code at M1.
* Authored fresh for this repository (not imported from the design corpus). The Customer/Order and Address/Money illustrations common to DDD material are replaced with Nami's own entities and values. EF Core version behavior was verified against Microsoft's EF Core 8-10 documentation on 2026-07-18, including the still-current limitation that complex types do not support collections.
