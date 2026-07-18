---
status: "accepted"
stack-record: true
date: 2026-06-29
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OpenIddict CORS support (issue #28, no per-client origins, verified 2026-06-29); the ASP.NET Core `ICorsPolicyProvider` contract
informed: all contributors, via this repository
---

# Provide per-client CORS through a custom policy provider, not static global CORS

## Context and Problem Statement

Browser-based single-page-application clients need CORS on the token-issuing endpoints to call them from their own origin. In Nami tenants self-register SPA clients (ADR-0001, ADR-0035), so adding a new SPA must take effect immediately without a redeployment, and each client must be limited to its own declared origins. OpenIddict has no per-client CORS: its application descriptor has no allowed-origins field and the maintainers have declined to add one (issue #28, wontfix), whereas commercial identity servers read per-client origins from the client store. A static, global CORS policy cannot express per-client origins and cannot admit a newly registered client's origin without a configuration change and redeploy. How should Nami provide CORS so it is per-client and dynamic?

## Decision Drivers

* Each SPA client must be limited to its own declared origins.
* A newly registered SPA client must work immediately, with no redeploy (multi-tenant self-service, ADR-0001/ADR-0035).
* No schema change and no override of the OpenIddict store model.
* No database query on every CORS preflight (it is on the hot path).

## Considered Options

* A static, global CORS policy
* Per-client dynamic CORS through a custom `ICorsPolicyProvider`

## Decision Outcome

Chosen option: "Per-client dynamic CORS through a custom `ICorsPolicyProvider`". The fixed parameters are:

* **A. Origins are stored per client as the system of record.** Allowed origins live in `Application.Properties['cors_origins']` (the JSON dictionary OpenIddict already provides), so there is no schema change and no override of the store model. They are surfaced through the client-definition config mapper (the `AllowedCorsOrigins` field on the client definition).
* **B. A custom `ICorsPolicyProvider` serves the policy per request.** It replaces the default provider (registered singleton), reads the request `Origin` header, looks it up in a per-tenant cached origin-set (a derived read-model), and builds the `CorsPolicy`. It never queries the database on a preflight.
* **C. The origin-set cache reuses the config-change cache.** Per-client CORS is configuration data, so the origin-set is served from the same FusionCache-plus-Redis config-change cache as other client/scope config (ADR-0039 path f): one cache, one invalidation path, one SLO, invalidated when a client changes. The off-hot-path refresh lists all applications per tenant and extracts `cors_origins` in memory (OpenIddict has no distinct-origins query primitive), and it must run under each tenant's ambient multi-tenant context so isolation (row-level security and the query filter) holds (ADR-0001). An indexed side-table backing the same refresh is a clean future upgrade.
* **D. CORS is applied only where it belongs.** `RequireCors` is placed on discovery, JWKS, token, userinfo, and revocation, and not on authorize (a top-level browser navigation, not a CORS request) or introspection (server-to-server); there is no blanket `app.UseCors()`.

### Consequences

* Good, because each SPA client is limited to exactly its own origins, and a newly registered client works immediately with no redeploy.
* Good, because there is no schema change, the preflight hot path hits a cache rather than the database, and the origin-set shares one cache, invalidation path, and SLO with other config changes.
* Bad, because it is a build-over-native gap-fill (OpenIddict has no per-client CORS), so it is a custom provider to build and keep aligned across bumps.
* Bad, because the refresh must list applications per tenant under ambient context and extract origins in memory (there is no distinct-origins primitive), which is heavier than an indexed query until the optional side-table is added.
* Neutral, because it relies on the OpenIddict `Application.Properties` dictionary and the ASP.NET Core `ICorsPolicyProvider` contract, both pinned and re-verified per bump (ADR-0021).

### Confirmation

* A registered SPA origin passes CORS; an unknown origin receives no access-control-allow-origin header.
* Adding a client at runtime invalidates the cache and takes effect with no redeploy.
* The per-tenant refresh runs under the correct ambient context, so one tenant's origins never leak into another tenant's origin-set.
* CORS headers appear only on discovery/JWKS/token/userinfo/revocation, not on authorize or introspection.

## Pros and Cons of the Options

### A static, global CORS policy

* Good, because it is the simplest to configure and needs no custom provider.
* Bad, because it cannot express per-client origins and cannot admit a new client's origin without a configuration change and redeploy, which breaks tenant self-service.

### Per-client dynamic CORS through a custom `ICorsPolicyProvider` (chosen)

* Good, because origins are per client and a new client works immediately, with the preflight served from cache.
* Bad, because it is a custom provider to maintain and its refresh is heavier than an indexed query until a side-table is added.

## More Information

* Decided 2026-06-29 (per-client, dynamic; "Posture B"). Recorded from the core-protocol design (doc 03 §9bis, tasks 3.16a-d) and the configuration design (doc 13 §3a/§3b, task 13.9).
* Related decisions: ADR-0001 (per-tenant isolation and the ambient context the refresh runs under), ADR-0035 (self-service client registration that adds the SPA clients whose origins this serves), ADR-0039 (the config-change cache reused for the origin-set), and ADR-0021 (the version-sensitive `Application.Properties`/`ICorsPolicyProvider` seam re-verified per bump). The `AllowedCorsOrigins` field is part of the client-definition config mapper recorded with the configuration-ergonomics decision.
* Authored in this repository in 2026-07 to record the settled CORS decision as an ADR; a comparison to commercial identity servers' per-client CORS was generalized (no vendor named), and OpenIddict (with its public issue #28), FusionCache, Redis, and the ASP.NET Core CORS contract are named factually for identification only.
