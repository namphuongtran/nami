---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: primary-source review (2026-07-04) of how mainstream identity servers and the OpenIddict samples structure their code, Microsoft web-architecture guidance, and the 2025-2026 vertical-slice-versus-Clean literature
informed: all contributors, via this repository
---

# Adopt a hexagonal shell (dependency rule plus ports/adapters) with vertical slices inside, for both IdP-core and Admin

## Context and Problem Statement

An earlier design recommended Clean Architecture but warned that it is not OpenIddict doctrine and should not be over-engineered, so no official style was settled. Nami has two deployables of genuinely different natures: the **IdP-core**, which is protocol-heavy and where OpenIddict *owns* the flow through an event-handler pipeline (chain-of-responsibility); and the **Admin API**, which is CRUD-ish (clients, scopes, users, roles, tenants, delegated-admin) with its Application logic as a folder inside it (ADR-0020). Nami needs one unified style for both so the codebase is consistent, without forcing a rigid mold that mis-fits either.

Primary-source research (2026-07-04) found that neither a mainstream commercial identity server nor OpenIddict uses Clean Architecture internally: both keep the auth-server host flat/single-project and express layering only through a store-interface seam. Microsoft (.NET 10) recommends starting simple, reserves Clean for non-trivial apps, and defaults new projects to Minimal APIs. The 2025-2026 consensus is that vertical slice and ports/adapters are not mutually exclusive: ports/adapters govern technical coupling, and vertical slices govern cohesion.

## Decision Drivers

* One consistent style across both deployables, differing in weight rather than in rule.
* Fit the reality that OpenIddict owns the protocol pipeline; do not build a layering tower around a framework-owned flow.
* Preserve every committed infrastructure swap (cloud-agnostic key/secret ports, audit sink, tenant store, ReBAC) behind real boundaries.
* Keep ceremony low and cohesion high, and avoid single-implementation interfaces that exist only to satisfy layering.

## Considered Options

* Rigid multi-assembly Clean/Onion (Domain/Application/Infrastructure/Presentation as separate assemblies, layered by technical folder)
* Pure vertical slice (everything by slice, minimal abstraction, no layers)
* A synthesis: a light hexagonal shell (the dependency rule plus ports/adapters only at the infrastructure edge) with the Application layer organized by vertical slice inside

## Decision Outcome

Chosen option: "The synthesis", applied as one rulebook to both the IdP-core and the Admin API, differing only in weight.

**Macro (the shell, mandatory):**

* Keep the dependency rule: Domain ← Application ← (Infrastructure / Presentation). Domain references no OpenIddict, EF, or cloud SDK.
* Ports/adapters live only at the real infrastructure edge (things that genuinely swap): persistence (a port plus an EF Core adapter, the one seam every reference ships), key/secret/data-protection (ADR-0006/0009), the audit/security-event sink (ADR-0008), the tenant store/resolver (ADR-0001), and the ReBAC `ICheckAccess` (ADR-0010).
* Do not create a single-implementation interface just to satisfy layering ("just noise"); a port must have at least two real reasons to exist (swap, test, or a genuine boundary).
* **Deliberate exception, the BFF proxy**: `Nami.Identity.Bff` (composing a reverse proxy plus an access-token-management library, see ADR-0029) is a real infrastructure edge but has no port. It is a deliberate composition boundary, not a port: the seam is in configuration (route/cluster/token-management options) and the adapter is the proxy plus the library itself. It does not meet the "at least two real reasons for a port" bar (no engine swap, and no need for an in-process fake since it is tested over HTTP), so the "a port must have a real reason" doctrine is honored by not wrapping the library in a port. This is an explicitly acknowledged exception to the ports-at-the-edge rule.

**Micro (internal organization, mandatory):**

* Organize the Application layer by **feature slice** (feature folders such as `Features/Clients/CreateClient/`), each slice grouping request, handler, validator, and response. Do not organize by technical folder (`Services/`, `DTOs/`, `Validators/`).
* Maxim: minimize coupling between slices, maximize cohesion within a slice.

**Applied to the IdP-core (protocol):** a flat host like the OpenIddict samples, with the authorization controller, endpoints, and `AddOpenIddict()` wiring in the server host (the Presentation/Infrastructure boundary). The IdP-core "slice" is the handler pipeline plus a few domain services (claims, consent, keys); there is no separate Domain/Application/Infrastructure tower for the protocol flow. Custom protocol logic is an inserted handler (order-anchored at a named position), never a fork of the engine.

**Applied to the Admin API (CRUD):** the `Application` folder inside `Admin.Api` (ADR-0020) holds the feature slices; slices are thin for plain CRUD, and only when an operation has a real invariant (a delegated-admin scope, a tenant-hierarchy rule, ADR-0010) is a domain core built inside that slice. CQRS-lite (a handler per request, with or without a mediator) is optional within a slice, not mandatory.

**Endpoint mechanism (orthogonal to slice organization):** the Admin API keeps REST controllers (per ADR-0020; controllers fit where advanced model-binding/validation is needed), and the IdP-core keeps controllers for the OpenIddict pass-through flows (authorize, token, logout, userinfo, device). Minimal APIs are used for auxiliary endpoints where convenient, with no mass migration.

**Shared logic (avoiding a `Common` God-project):** share at the DTO level via `Nami.Identity.Contracts` and `Nami.Identity.Admin.Contracts` (ADR-0020), and share domain via the Domain core; do not dump everything into one `Common` that reintroduces coupling.

**Enforcement:** an architecture-test suite (`Nami.Identity.ArchitectureTests`) using TngTech.ArchUnitNET (Apache-2.0, actively maintained in 2026), chosen over the original `NetArchTest.Rules` (unmaintained since 2021); an MIT fork exists as a drop-in alternative. Rules: (a) Domain references no OpenIddict/EF/cloud SDK; (b) Application references no cloud SDK or engine; (c) a slice does not reference another slice (only via Contracts/Domain); (d) cloud adapters live in Infrastructure; (e) `Nami.Identity.Bff` must not reference `Nami.Identity.Admin.*` (the BFF package is shared by admin and SPA consumers, so admin routes/policies stay in `Nami.Identity.Admin.App` and no coupling leaks into the BFF layer). A slice template and a code-review checklist compensate for the guardrail that vertical slice removes.

### Consequences

* Good, because it matches how both reference identity servers actually build (flat plus a store seam) and the reality that OpenIddict owns the pipeline; ceremony is low, feature cohesion is high, and a slice can grow its own domain core when it needs one.
* Good, because every committed infrastructure swap (cloud-agnostic, audit, tenant, ReBAC) is preserved behind narrow edge ports.
* Good, because it is one consistent style for both the IdP-core and Admin, with no conflict against ADR-0020.
* Bad, because vertical slice removes guardrails and leans on team discipline; this is mitigated by ArchUnitNET, the slice template, and review.
* Bad, because logic can be duplicated between slices; this is mitigated by the layered shared-logic rule (Contracts and Domain) rather than a God-`Common`.
* Bad, because it needs explicit "a port must have a real reason" guidance to avoid both over-abstraction and no boundary at all.

### Confirmation

* A mainstream commercial identity server uses no internal Clean Architecture: middleware plus a store-interface seam (storage abstractions with an optional EF adapter), a flat single-host quickstart, and prescriptiveness only about process separation, not layering.
* The OpenIddict samples are flat, single-project-per-host, with the controller and `DbContext` in the host and `Program.cs` wired inline, and no Clean or vertical-slice structure; the project describes itself as a framework rather than a turnkey solution.
* Microsoft equates Clean with hexagonal/ports-adapters/onion, reserves it for non-trivial apps, advises starting simple, and recommends Minimal APIs for new projects.
* The 2025-2026 literature treats vertical slice and Clean as complementary: ports/adapters for technical coupling, vertical slice for cohesion, a module boundary plus the dependency rule at the macro level and slices at the micro level, with vertical slice fitting handler/CQRS and CRUD reducing to thin slices.
* Not independently verified: there is no explicit maintainer quote opposing Clean (only the implicit signal of the flat sample), and "framework-owned flow" is Nami's own phrasing, though the vertical-slice-to-handler/CQRS mapping is verified.

## Pros and Cons of the Options

### Rigid multi-assembly Clean/Onion

* Good, because the boundaries are maximally explicit.
* Bad, because the ceremony is high, features are shredded across layers (low cohesion), single-implementation interfaces exist only to satisfy layering, no reference identity server does this, and it over-engineers a flow that OpenIddict already owns.

### Pure vertical slice

* Good, because it fits the pipeline/handler model and minimizes abstraction.
* Bad, because it drops the guardrails, putting the discipline burden on the team, risking duplicated shared logic, and leaving cross-cutting concerns and infrastructure swaps without a clear home — and Nami genuinely needs those swaps (cloud-agnostic ports, audit sink, tenant store).

### Hexagonal shell plus vertical slice inside (chosen)

* Good, because it matches the 2025-2026 consensus and degrades gracefully for both deployables, keeping ceremony low while preserving the infrastructure swaps behind narrow ports.
* Bad, because it requires discipline and guidance to keep ports meaningful and slices decoupled, mitigated by the arch-tests and templates.

## More Information

* Original decision: 2026-07-04.
* Build-time follow-ups: the slice template and folder convention (`Features/<Area>/<UseCase>/`) for the Admin API; the architecture-test rules in CONTRIBUTING; and whether CQRS-lite uses a mediator or plain handlers (non-blocking, settled when the admin code is written).
* Related decisions: ADR-0020 (two admin projects, the Application folder, and ArchUnitNET), ADR-0001/0005/0006/0008/0009/0010 (the infrastructure ports), ADR-0021 (the seam catalogue), ADR-0022 (the two-lane logging), ADR-0029 (the BFF, the acknowledged port exception), ADR-0058 (the Separation-of-Concerns and pragmatic-SOLID principles this style applies), and ADR-0060 (the testing strategy, including the ArchUnitNET architecture tests defined here).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial identity server and its documentation URLs, a named maintainer, and the vertical-slice-literature authors were generalized; the product-name placeholder was set to the repository's `Nami.Identity.*` convention; the BFF token-management library is described generically (a non-vendor-branded choice, per ADR-0020 and ADR-0026). Test libraries (ArchUnitNET) and the Microsoft reference material are retained as neutral technical references.
