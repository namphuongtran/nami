---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OpenIddict application-descriptor and permission constants (deny-by-default, source-verified V07); a search confirming no community facade library exists for OpenIddict
informed: all contributors, via this repository
---

# Build an ergonomic, fail-closed configuration layer for declaring clients and scopes

## Context and Problem Statement

OpenIddict is powerful but verbose and deny-by-default: declaring a single web client means enumerating endpoints, grant types, response types, each scope (with the right prefix), and the PKCE requirement explicitly, roughly three to four times the surface of the concise client POCO that commercial identity servers offer. Ease of adoption ("as easy as a commercial identity server") is a stated product goal, and a search found no community facade library that provides this over OpenIddict. Worse, a verbose deny-by-default API is easy to misconfigure into a client that works but is insecure (a public client without PKCE, a wildcard redirect URI, a confidential client with no credential). How should consumers declare clients and scopes so it is both concise and safe by default?

## Decision Drivers

* Concise declaration comparable to commercial identity servers, since ease of adoption is a product goal.
* No community facade library exists, so the choice is build or stay verbose.
* A verbose API invites insecure-but-working misconfiguration; the layer should make an insecure client impossible to construct, not merely possible to get right.
* Configuration is multi-tenant: it must be tenant-aware and seeded per tenant (ADR-0001).

## Considered Options

* Use OpenIddict descriptors directly
* Adopt a community facade library
* Build a bespoke ergonomic layer (POCO definitions plus a fail-closed descriptor mapper and an idempotent seeder)

## Decision Outcome

Chosen option: "Build a bespoke ergonomic layer". The fixed parameters are:

* **A. A thin POCO model plus a mapper and an idempotent seeder.** `ClientDefinition` and `ScopeDefinition` POCOs with safe defaults are mapped to the `OpenIddictApplicationDescriptor`/scope descriptor by a `ToDescriptor()` mapper, and an idempotent seeder applies them (re-running seeds no duplicates). A helper translation table turns a concise flow enum into the explicit OpenIddict permission sets (endpoints plus grant types plus response types plus scopes) that deny-by-default requires.
* **B. The mapper is fail-closed by construction.** It bakes in security invariants so an insecure client cannot be built: a public or code client is always forced to PKCE (throwing if absent), a confidential client without a credential (secret or JWKS) throws, wildcard redirect URIs are forbidden (exact match only), a native app sets `ApplicationType = Native` for the built-in loopback relaxation, and the incorrect "openid maps to a profile" case is dropped. These construction-time guarantees complement the startup self-check of the same invariants (ADR-0043): the mapper stops a bad configuration being built, and the startup check stops a drifted one from serving.
* **C. Per-client policy is baked into the definition.** `IssueRefreshToken` (machine-to-machine defaults to false), `AccessTokenType` jwt|reference (the built per-client property of ADR-0039), an absolute refresh lifetime, `AllowedCorsOrigins` (per-client CORS, ADR-0050), and the M2M `AuthMethod` defaulting to `private_key_jwt` (ADR-0009) are all expressed on the definition rather than scattered across configuration.
* **D. Tenant-aware seeding.** A definition carries a `TenantId`, and the seeder runs per tenant at provisioning under that tenant's ambient context, so client declarations respect the isolation model (ADR-0001).
* **E. The mapper is a version-sensitive seam.** The permission and endpoint constants and the descriptor API change across OpenIddict versions (for example the `Endpoints.Logout` constant was renamed `Endpoints.EndSession`), so the mapper and the translation table are contract-regression items re-verified on each bump (ADR-0021).

### Consequences

* Good, because declaring a client is concise and adoption-friendly, matching the product's ease-of-use goal.
* Good, because an insecure client configuration is impossible to construct rather than merely discouraged, and the same invariants are re-checked at startup for defense in depth.
* Good, because per-client policy (refresh, token type, CORS, auth method) is consolidated in one declaration, and seeding is tenant-aware and idempotent.
* Bad, because it is a bespoke layer to build and maintain, since no community library provides it.
* Bad, because the mapper and translation table depend on version-sensitive OpenIddict constants and must be re-verified on each bump.
* Neutral, because the POCO model is part of the public API surface and is therefore governed by the SemVer and deprecation policy (ADR-0044); this ADR records the decision to build the layer and its fail-closed principle, not an exhaustive field list.

### Confirmation

* Declaring a public client without PKCE throws at mapping time; a confidential client without a credential throws; a wildcard redirect URI is rejected.
* The seeder is idempotent (re-running produces no duplicates) and runs under each tenant's ambient context.
* A contract-regression test pins the permission and endpoint constant names so a rename in an OpenIddict bump fails the build rather than silently mis-mapping.

## Pros and Cons of the Options

### Use OpenIddict descriptors directly

* Good, because there is nothing extra to build.
* Bad, because every client declaration is verbose and deny-by-default, which is both poor DX and an easy path to an insecure-but-working client.

### Adopt a community facade library

* Good, because it would save building one.
* Bad, because a search found none for OpenIddict; there is nothing to adopt.

### Build a bespoke ergonomic layer (chosen)

* Good, because it is concise, safe by construction, tenant-aware, and consolidates per-client policy.
* Bad, because it is a maintained layer whose mapper tracks version-sensitive OpenIddict constants.

## More Information

* Recorded from the configuration-DX design (doc 13 §1-§3), whose mapper was fixed as fail-closed and tenant-aware. The concise declaration goal is drawn from what commercial identity servers offer; the translation table and mapper are Nami's own, not derived from any such product.
* Related decisions: ADR-0027 (the fluent builder and host DX this declaration layer sits within), ADR-0035 (the runtime self-service client registration that is the Admin-API counterpart to this design-time declaration), ADR-0043 (the startup self-check that re-verifies the same fail-closed invariants at boot), ADR-0039 (the per-client `AccessTokenType`), ADR-0050 (per-client CORS), ADR-0009 (the `private_key_jwt` M2M default), ADR-0001 (tenant-aware per-tenant seeding), ADR-0021 (the version-sensitive permission-constant seam), and ADR-0044 (the POCO model as part of the versioned public API).
* Authored in this repository in 2026-07 to record the settled configuration-layer decision as an ADR; a comparison to the concise client model of commercial identity servers was generalized (no vendor named), and OpenIddict is named factually for identification only.
