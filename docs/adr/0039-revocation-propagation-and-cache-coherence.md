---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OpenIddict 7.5.0 source (entity-cache model, verified 2026-06-29); Microsoft.IdentityModel ConfigurationManager (interval floor, verified 2026-07-04); FusionCache / Redis; evidence R15
informed: all contributors, via this repository
---

# Achieve cross-node revocation freshness per-path, with no backplane for the per-request entity cache

## Context and Problem Statement

Nami runs multiple stateless nodes behind a load balancer, so a revocation performed on one node must take effect on the others quickly. An early audit framed this as "add a Redis pub/sub backplane to invalidate the OpenIddict manager cache". Reading the OpenIddict 7.5.0 source showed that premise is false: the entity caches (Application/Scope/Authorization/Token managers) are registered `TryAddScoped` with a private `MemoryCache` per DI scope, size-bounded (`EntityCacheLimit=250`, no time-TTL), invalidated by a local change token, with no `IDistributedCache` and no cross-node hook. A stale entity-cache read therefore cannot outlive a single HTTP request, so there is nothing to invalidate across nodes.

The real question is different and per-path: each thing that can be revoked (an access token, a refresh family, a session, a delegated-admin grant, a signing key, a piece of client/scope config) has its own freshness requirement and its own cache that might hold stale data. What propagation mechanism does each path need, and where (if anywhere) is a cross-node backplane actually justified, without adding a mandatory synchronous Redis hit to the 10k-CCU hot path?

## Decision Drivers

* Correctness: a revocation must become visible on other nodes within a stated freshness bound per path.
* The hot token path must not gain a mandatory synchronous cross-node cache hit at 10k CCU.
* No mechanism may be a hack: no clearing of options caches, no monkey-patching, no poll-and-pray.
* Security-critical checks must fail closed; ordinary performance caches may fail open.
* Every version-sensitive assumption about OpenIddict and Microsoft.IdentityModel internals must be pinned and re-verified on each bump (ADR-0021).

## Considered Options

* A Redis pub/sub backplane to invalidate the OpenIddict entity/manager cache
* Bare .NET `HybridCache` for the config cache
* A per-path freshness model with no backplane for the entity cache, adding a cross-node backplane only for the two paths that need it

## Decision Outcome

Chosen option: "A per-path freshness model with no backplane for the entity cache". The manager-cache backplane is rejected as solving a non-problem (the cache is per-request), and each revocation path uses the standard mechanism already available in OpenIddict, ASP.NET Core, or an established library. The fixed per-path design is:

* **(a) Access-token revoke.** A self-contained JWT is valid until expiry (15 min, ADR-0003/ADR-0004); for clients that need instant revocation, a reference token is used instead. Whether a client gets a JWT or a reference token is a **built per-client property** (`AccessTokenType`, default JWT), enforced by a custom `IOpenIddictServerHandler<GenerateTokenContext>` that flips `IsReferenceToken`/`PersistTokenPayload` per client, ordered before the token-generation and store-persist handlers and pinned by a pipeline-snapshot test. This is deliberately **not** the native `UseReferenceAccessTokens`, which is a single global flag. Opting a client into reference tokens forces that client's resource server onto the introspection endpoint (a reference token is opaque and cannot be validated locally). Selection guide: JWT for high-volume/first-party/BFF/M2M; reference for admin/privileged/high-assurance clients. (The broader token posture is ADR-0004; this ADR fixes the cross-node revocation mechanism.)
* **(b) Refresh-family revoke.** Native and DB-direct: rotation hits the database, so a revoked family is rejected on its next use on any node with no propagation lag.
* **(c) Force-logout / session revoke.** Removal of the row from the shared `ITicketStore` (ADR-0003) takes effect on the next cookie re-validation on any node, backstopped by the `SecurityStampValidator.ValidationInterval` of 1-2 min.
* **(d) Delegated-admin grant revoke.** `EnableAuthorizationEntryValidation` (on the validation builder) makes the check DB-direct and live, so a revoked grant is enforced immediately.
* **(e) Signing-key break-glass (SLO ≤ 60s).** Shorten the resource server's `AutomaticRefreshInterval` to ~5 min (this is the lowest legal value; the framework enforces a 5-min floor by throwing, so it is a supported setting, not a workaround) **and** maintain a Redis-backed "distrusted-kid" set checked **fail-closed** (if Redis is unreachable, treat the kid as distrusted), served from a `HybridCache` L1 so the happy path takes no per-request Redis hit. Ties to ADR-0007 (eject within 5 min) and ADR-0011/ADR-0012.
* **(f) Client/scope config change (SLO ≤ 30s).** A config cache built as FusionCache plus a Redis backplane, chosen over bare `HybridCache` because `HybridCache` has no built-in cross-node L1 invalidation (open proposal dotnet/runtime #125602).

The consequence is that the 10k-CCU hot path has **no mandatory synchronous Redis hit**: Redis is only a bounded fast path for the low-traffic (e) and (f), and paths (a)-(d) are DB-fresh or expiry-bounded and do not depend on Redis at all.

### Consequences

* Good, because the design is smaller and cleaner than the backplane it replaces: no cross-node invalidation for the entity cache means fewer moving parts and fewer failure modes.
* Good, because the hot path adds at most one DB round-trip on the DB-direct paths and no synchronous cross-node hit, so it scales.
* Good, because each path uses a standard, source-verified mechanism (DB-direct entry validation, shared session store, a fail-closed denylist analogous to CRL/OCSP, an established cache backplane), not a hack.
* Bad, because DB-direct validation and reference tokens trade cross-node freshness for extra DB reads on those clients, so reference tokens are reserved for sensitive clients and their DB-read cost is measured against the capacity model.
* Bad, because it depends on undocumented OpenIddict internals (the per-request entity-cache model, handler ordering) and on Microsoft.IdentityModel constants (the 5-min interval floor), all of which must be pinned and re-verified on each bump (ADR-0021).
* Neutral, because a self-contained JWT still cannot be revoked before its 15-min expiry; that is the inherent JWT trade-off, and the reference-token path exists precisely for clients that cannot accept it.

### Confirmation

* Verified at OpenIddict 7.5.0 source (2026-06-29): the entity cache is `TryAddScoped` with a private `MemoryCache`, `EntityCacheLimit=250`, no time-TTL, a local change-token, no `IDistributedCache`, and no cross-node hook; the entry-validation flags live on the validation builder and make a DB call per request.
* Verified at Microsoft.IdentityModel source (2026-07-04): `AutomaticRefreshInterval` carries a 5-min minimum enforced by throwing, so ~5 min is the lowest legal value; these constants are pinned via the transitive `Microsoft.IdentityModel.Protocols` graph and re-verified per bump (ADR-0021).
* Cross-node acceptance tests: revoke on node A and confirm node B rejects within the path's SLO; Redis-down proves the distrusted-kid check fails closed; force-logout takes effect on the next request on another node.

## Pros and Cons of the Options

### Redis pub/sub backplane to invalidate the entity/manager cache

* Good, because it is the intuitive answer to "cache stale across nodes".
* Bad, because it solves a non-problem: the OpenIddict entity cache is per-request, so a stale read dies at end of request and there is nothing to invalidate cross-node; it would add a synchronous dependency and failure mode for no freshness benefit.

### Bare .NET `HybridCache` for config

* Good, because it is the framework-native two-level cache.
* Bad, because it has no built-in cross-node L1 invalidation (open proposal dotnet/runtime #125602), so a config change on one node would not be reflected on others within the 30s SLO.

### Per-path freshness model, backplane only where needed (chosen)

* Good, because each path is DB-fresh, expiry-bounded, or backed by a purpose-built cache, with no hot-path Redis dependency and fewer moving parts.
* Bad, because it leans on version-sensitive internals that must be pinned and re-verified, and it accepts the inherent non-revocability of self-contained JWTs.

## More Information

* This ADR corrects the earlier "invalidate the manager cache" framing and records the per-path model that lives in the design corpus (doc 20, deep-diving doc 11 §2; evidence R15). Task anchors: (e) and (f) are build tasks 8.35/8.36 with cross-node propagation tests 9.T16-9.T18.
* Compared with mainstream commercial identity servers, this model is at least as fresh and in places stricter: their config cache is typically expiry-only (around 15 minutes) with no active invalidation, whereas the per-request entity cache here is stale for at most one request and the config path adds active cross-node invalidation; emergency signing-key revocation (path e) is a capability those servers generally do not offer at all. These are independent design choices, stated for comparison only and not a claim of parity or derivation.
* Related decisions: ADR-0003 (server-side session store and the `ITicketStore` removal path), ADR-0004 (refresh-token posture and the broader token-format posture that path (a) draws on), ADR-0007 (eject a compromised key within 5 min, which path (e) implements at the resource server), ADR-0011/ADR-0012 (key rotation and bootstrap the break-glass path ties into), ADR-0021 (the seam catalogue and contract-regression that pin these internals), ADR-0033 (key-scope isolation, relevant to which kid the distrusted set covers), and ADR-0037 (PostgreSQL, the DB behind the DB-direct paths). The general fail-open-versus-fail-closed cache policy is set by the forthcoming resiliency and overload-protection posture (Tier-1 candidate D).
* Authored in this repository in 2026-07 to record the settled revocation-propagation design as an ADR; neutral libraries and proposals (FusionCache, Redis, Microsoft.IdentityModel, dotnet/runtime #125602) are named factually for identification only, and the direct commercial competitor is not named.
