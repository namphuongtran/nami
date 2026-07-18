---
status: "accepted"
date: 2026-07-02
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: industry survey of admin architectures (API + UI separation) and a documented multi-admin-approval API-bypass gap as a cautionary example; verification V04
informed: all contributors, via this repository
---

# Split admin into a REST API and an MVC Razor BFF app, enforce dual-control server-side, and reject app-only tokens

## Context and Problem Statement

Admin is build-your-own (decided 2026-07-01), referencing only a community permissions/requirements pattern. A new requirement is to split admin into at least two projects: a .NET Admin API and a Razor MVC frontend that consumes it. The industry has converged on separating the API from the UI (commercial admin UIs pair a React UI with a .NET REST API; established open-source admin suites use separate UI, API, and STS hosts). Nami's server-side MVC Razor variant is stronger on security than a SPA, because the token never reaches the browser, matching the BFF stance of ADR-0019. There is also a gap Nami must exceed: even a well-known enterprise multi-admin-approval feature has a documented API-bypass, where approval is enforced only on the UI/delegated path while an app registration with application permissions bypasses the approval workflow entirely; and existing open-source admin projects lack dual-control, delegated-admin, multi-tenancy, and tamper-evident audit. How should admin be structured?

## Decision Drivers

* Separate the deploy, scale, and attack surface of the admin API from the UI.
* Keep the token out of the browser (the BFF stance, ADR-0019).
* Dual-control must not be bypassable through a direct API path (the cautionary lesson above).
* Every action must be attributed to a real actor in the audit hash-chain.
* Avoid over-engineering, since admin mostly wraps existing managers.

## Considered Options

* Project shape: a single admin project; two admin projects (API plus a Razor BFF app); or full Clean Architecture with separate Domain/Infrastructure/Application projects.
* Frontend-to-API authentication: a user-delegated (BFF) token, or an app-only (client-credentials) token.

## Decision Outcome

Chosen: **two admin projects (a REST API plus an MVC Razor BFF app) with user-delegated BFF authentication and hard anti-bypass invariants.** Full Clean Architecture and app-only tokens are rejected.

Fixed parameters of the decision (revised 2026-07-04, down from four projects to two admin projects plus two DTO assemblies):

* **Projects**:
  * `Nami.Identity.Contracts` (top-level, common): only genuinely cross-cutting types used by at least two sides including the core IdP; kept minimal and zero-dependency, and not created at all if there is no real shared content.
  * `Nami.Identity.Admin.Contracts`: the admin request/response DTOs (application, proposal, tenant, audit-entry) plus problem codes, referenced only by `Admin.Api` and `Admin.App`, zero-dependency.
  * `Nami.Identity.Admin.Api`: the REST host (controllers, ProblemDetails per RFC 9457, OpenAPI), authz policies, and a resource server of the IdP itself. Its business logic lives in an `Application/` folder **inside** the project (the dual-control saga, delegated-admin check, audit emission, validators), following managers-not-stores: it calls `IOpenIddict*Manager`, `UserManager`, `RoleManager`, and ports, never a `DbContext` directly.
  * `Nami.Identity.Admin.App`: the MVC Razor BFF, a confidential OIDC client of the IdP itself, consuming the API through a typed `HttpClient`.
* **Boundary enforcement** (compile-enforce where cheap, test-enforce where expensive): the DTO boundary is compile-enforced by project reference — the core IdP references only `Nami.Identity.Contracts`, never `Nami.Identity.Admin.Contracts`, so the compiler blocks the core from depending on admin contracts; the `Application/`-folder boundary is an architecture test (ArchUnitNET) that forbids feature services in `Admin.Api/Application/` from referencing ASP.NET/HTTP/EF types, keeping the dependency rule without a separate Application project.
* **Frontend-to-API authentication is a user-delegated token, BFF-style**: the App is a confidential client (auth code plus PKCE) with a server-side session cookie (ADR-0003); the admin user's access token is stored server-side, auto-refreshed, and forwarded as a bearer via an open-source access-token-management library for the OIDC BFF pattern (one that works with any OIDC provider). The API authorizes the real person, so the audit records the correct actor, the proposer ≠ approver dual-control is enforceable, and it matches anti-confused-deputy (ADR-0010). The token never reaches the browser.
* **API style is REST controllers** with attribute routing, ProblemDetails, the built-in .NET 10 OpenAPI, and ETag/If-Match on mutations.
* **Anti-bypass invariants** (the cautionary lesson): dual-control is enforced in the Application layer (the `Application/` folder in `Admin.Api`, not the controller or UI), so no path executes a destructive action without going through the proposal saga; and the Admin API accepts no app-only token — no client-credentials client is registered with an admin-api scope, and the API policy `RequireActor` rejects any token lacking a `sub` (a real user). The break-glass path (ADR-0015) is separate and does not go through the Admin API.

Architecture style is hybrid — each style used where it fits, none as the backbone:

* **Clean-lite**: keep the dependency rule at two layers (the DTO boundary by compiler, the logic boundary by ArchUnitNET), but not full Clean, since separate Domain/Infrastructure/Application projects for admin would be over-engineering when admin wraps existing managers.
* **Vertical slice inside `Admin.Api`** (the `Application/` folder): feature folders (DualControl, Applications, Tenants), each self-contained (service, validator, executor), a controller per resource, no technical-layer split inside, and no mediator dependency (a popular mediator library moved to a commercial license around 2025 — verify before using; plain DI services suffice).
* **DDD**: strategic DDD exists (bounded contexts for Core-IdP, Admin, and control-plane, with a ubiquitous language of Proposal/Grant/Capability/Membership), but tactical DDD is used only for the `Proposal` aggregate (a rich class enforcing proposer ≠ approver, single-use, expiry, TOCTOU safety, and a state machine); the remaining CRUD has no ceremony.
* **Event-driven only at the edges**: the audit outbox (at-least-once, ADR-0008) and back-channel logout fan-out (ADR-0019). EDA is forbidden for dual-control execution, because approve-and-execute must be synchronous, transactional, and TOCTOU-safe, so eventual consistency there would be a security bug. There is no message-bus backbone.
* The core IdP (outside this ADR's scope) follows its own decided style: the OpenIddict pipeline plus ports/adapters (ADR-0006 and ADR-0009).

### Consequences

* Good, because the admin API and UI have separate deploy/scale/attack surfaces, the API is testable independently of the UI, and the Contracts assemblies prevent DTO drift.
* Good, because dual-control cannot be bypassed through the API directly (unlike the cautionary enterprise example), and every real actor lands in the audit hash-chain.
* Good, because MVC Razor keeps the token out of the browser with less complex JavaScript, matching both the team's capability and the BFF stance.
* Bad, because of one extra internal hop (App to API) of latency, and `Nami.Identity.Admin.Contracts` needs versioning discipline (a DTO change touches both admin projects), while the common `Nami.Identity.Contracts` change touches the core IdP too, so it must stay minimal and stable.
* Bad, because admin depends on a live IdP (dogfooding), which makes the bootstrap/break-glass path (ADR-0015) mandatory; it exists.

### Confirmation

* Boundary tests: a compile failure if the core references `Nami.Identity.Admin.Contracts`, and an ArchUnitNET failure if `Admin.Api/Application/` references ASP.NET/HTTP/EF.
* Anti-bypass: no client-credentials client carries an admin-api scope; `RequireActor` rejects a token with no `sub`; and a destructive action cannot execute outside the proposal saga.
* Folder layout is flat `src/` (no `admin/` subfolder; grouping by name prefix, `Nami.Identity.*` versus `Nami.Identity.Admin.*`, with IDE solution folders for visual grouping only).
* Build-time follow-ups: an API versioning scheme (path `/v1` proposed); OpenAPI exposure in production (recommended dev-only); and a capability-to-`acr` map per destructive action for step-up on approval.

## Pros and Cons of the Options

### Project shape

* **Two admin projects plus two DTO assemblies (chosen)** — good, because it separates API and UI concerns and compile-enforces the DTO boundary without over-engineering; bad, because it adds an internal hop and versioning discipline.
* **A single admin project** — good, because it is the least structure; bad, because the API cannot be deployed, scaled, or attack-surfaced separately, and the DTO boundary is not compiler-enforced.
* **Full Clean Architecture** — good, because it is the most decoupled; bad, because separate Domain/Infrastructure/Application projects are over-engineering when admin merely wraps existing managers.

### Frontend-to-API authentication

* **User-delegated BFF token (chosen)** — good, because the real actor is authorized and audited, dual-control is enforceable, and the token never reaches the browser; bad, because it needs server-side token management.
* **App-only (client-credentials) token** — good, because it is simple; bad, because it destroys actor attribution and is exactly the API-bypass that the cautionary example demonstrates, so it is rejected.

## More Information

* Original decision 2026-07-02, revised 2026-07-04 (from four projects to two admin projects plus two DTO assemblies, with the former `.Web` renamed `.App`).
* Detailed mini-specs (the Admin API surface and the Admin App screens/token-management) live in separate design documents.
* Related decisions: ADR-0003 (server-side session for the BFF cookie), ADR-0006 and ADR-0009 (ports/adapters, the core IdP style), ADR-0008 (audit hash-chain and outbox), ADR-0010 (delegated-admin and anti-confused-deputy), ADR-0015 (break-glass, separate from the Admin API), ADR-0019 (BFF stance and back-channel logout fan-out), ADR-0024 (architecture style; admin uses the same shell and vertical-slice approach), and ADR-0058 (the bounded-context, aggregate, and pragmatic-SOLID principles this admin split applies).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial admin UI, an open-source admin suite, an individual's community pattern, and a named enterprise product used as a cautionary example were all generalized; the product-name placeholder was set to Nami. The BFF token-management library is described generically here; the concrete package choice is a dependency decision to be confirmed separately (see the repository's dependency-license policy, ADR-0026).
