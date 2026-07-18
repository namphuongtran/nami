---
status: "accepted"
date: 2026-07-10
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security (ratifying the guardrail thresholds before the v2.1 build)
informed: all contributors, via this repository
---

# Offer self-service client registration through the authenticated Admin API (DCR-inspired), not the standard DCR endpoint

## Context and Problem Statement

A commercial identity server offers Dynamic Client Registration, and the coverage audit placed CIBA, DCR, and dynamic-external-IdP after v1. Having opened dynamic external IdP (ADR-0034), the goal now is to complete the self-service tenant-onboarding line: a delegated tenant-admin registers and manages their own tenant's clients/apps, in parallel with self-registering an IdP in v2. This is a v2.1 scope — it comes after the v2 dynamic-IdP work, is not part of v1, and does not touch the in-flight dynamic-IdP effort. How should self-service client registration work?

## Decision Drivers

* Complete self-service tenant onboarding (IdP plus client) on the same admin surface.
* Reuse the ADR-0034 machinery (RLS, secret handling, delegated-admin, audit).
* Do not gate the feature on OpenIddict 8.0.
* Self-service client registration is a security-sensitive surface, so it needs guardrails.

## Considered Options

* The standard RFC 7591/7592 DCR endpoint (`/connect/register`)
* A DCR-inspired Admin-API client CRUD for delegated tenant-admins

## Decision Outcome

Chosen option: "A DCR-inspired Admin-API client CRUD", through the authenticated Admin API rather than the standard RFC 7591 `/connect/register` endpoint, because the Admin-API model builds now on the pinned OpenIddict 7.5 and reuses the existing admin machinery, whereas the standard endpoint is only native in 8.0.

* **Model = an Admin-API client CRUD** for a delegated tenant-admin (ADR-0010), tenant-scoped with RLS. The client (an OpenIddict `Application`) is already per-tenant (ADR-0001), and it uses `IOpenIddictApplicationManager`, which is available in 7.5.0.
* **Not gated on OpenIddict 8.0.** The standard `/connect/register` (RFC 7591/7592, native in 8.0) is an optional future for programmatic/tool self-registration; only that would ride 8.0 (ADR-0021/0030). The v2.1 Admin-API CRUD builds now on 7.5.
* **Guardrails (the core security part, because it is self-service):** a strict `redirect_uri` (https, no wildcard or loopback abuse, a per-client allow-list); limited `grant_types` (no implicit, and `client_credentials` only with operator approval); requestable scopes limited to the subset of the global catalog the tenant is entitled to (a client-grant allow-list requiring a `TenantScopeEntitlement` authority); a per-tenant client cap; and audit on every mutation (ADR-0008).
* **Client secret (source-verified in the OpenIddict application descriptor):** OpenIddict auto-hashes the secret on `IOpenIddictApplicationManager.CreateAsync`, so Nami generates a random secret, passes the plaintext into `CreateAsync` (OpenIddict hashes and stores it on the `Application`), shows the plaintext exactly once to the tenant-admin, stores no plaintext, and does **not** use the secret store. This is deliberately distinct from ADR-0034: an external-IdP secret uses the secret store because there the IdP-core *is* the client, whereas here the client secret is verified *by* the IdP-core, so OpenIddict hashes it. The two must not be conflated.

### Consequences

* Good, because it completes self-service onboarding (IdP plus client) on the same admin surface and reuses the ADR-0034 RLS, secret, delegated-admin, and audit machinery, making it a light, additive feature.
* Good, because it has no OpenIddict 8.0 dependency and ships flexibly.
* Bad, because it is not standard DCR (there is no `/connect/register`), so an RP or tool cannot self-register programmatically; this is accepted and can be added later via the standard endpoint.
* Bad, because a self-service client is a threat surface, so the `redirect_uri`, scope, and grant rules must be tight (the guardrails); if they were loose the result would be open-redirect or scope-escalation.

Impact on v1 is additive and non-breaking: the `Application` is already per-tenant, so only Admin-API endpoints, guardrail validation, and UI are added, reusing the admin app, with no change to the core protocol or token.

### Confirmation

* Source-verified: OpenIddict auto-hashes the client secret on `CreateAsync`, so the plaintext is shown once and never stored and the secret store is not used.
* `IOpenIddictApplicationManager` is available in 7.5.0, so the Admin-API CRUD needs no 8.0 feature.
* v2.1-build follow-ups: Security ratifies the guardrail thresholds (the `redirect_uri` policy, the self-service-allowed `grant_types`, the per-tenant scope entitlement, and the client cap); and the standard `/connect/register` (RFC 7591/7592) on OpenIddict 8.0 would be a follow-up ADR if it is ever needed (ADR-0064, proposed, is one demand driver, since some MCP clients expect the standard DCR endpoint).

## Pros and Cons of the Options

### The standard RFC 7591/7592 DCR endpoint

* Good, because it is the interoperable standard and allows programmatic/tool self-registration.
* Bad, because it is native only in OpenIddict 8.0, so it would gate the feature on a version bump, and it needs its own guardrails regardless.

### A DCR-inspired Admin-API client CRUD (chosen)

* Good, because it builds now on 7.5, reuses the admin/RLS/secret/audit machinery, and keeps self-service behind an authenticated, delegated-admin surface.
* Bad, because it is not the interoperable standard, so a programmatic RP/tool cannot self-register until the standard endpoint is added.

## More Information

* Original decision 2026-07-10 (v2.1 scope-opened). This closes the DCR item in the coverage audit and completes the self-service onboarding line begun with ADR-0034, without touching v1 or the in-flight dynamic-IdP work.
* Related decisions: ADR-0001 (the per-tenant `Application`), ADR-0008 (audit on every mutation), ADR-0009 (the external-IdP secret store, contrasted with the OpenIddict-hashed client secret here), ADR-0010 (the delegated tenant-admin actor), ADR-0021/0030 (version-adaptation, relevant only if the standard endpoint is added later), ADR-0026 (OSS-only), and ADR-0034 (the sibling self-service IdP registration reusing the same admin/RLS/secret machinery). The implementation mini-spec is a separate design document.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial identity server's Dynamic Client Registration and the coverage-audit reference were generalized; OpenIddict and its application manager and descriptor are retained as the dependency.
