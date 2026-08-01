---
status: reviewed
created: 2026-07-23
tags: [design, revocation, caching, redis, fusioncache, distrusted-kid, cross-node]
---

# Revocation propagation and caching (detailed design)

## 1. Decisions realized

| Decision | What this design applies |
|---|---|
| ADR-0039 | Per-path cross-node revocation freshness with **no backplane** for the per-request entity cache; the distrusted-kid module; the config-cache backplane |
| ADR-0040 | Redis is an accelerator, never the sole source of truth; ordinary caches fail open, security checks fail closed; a Redis outage degrades latency, not authentication |
| ADR-0003 / ADR-0004 | Force-logout via `ITicketStore` with the `ValidationInterval` backstop; the 15-minute JWT residual and the 30-second reuse leeway as the freshness bounds |
| ADR-0048 / ADR-0049 | Reference-token clients validate via introspection; a shared Pool signature is not a boundary, so the RS validates signature + issuer + audience + tenant |
| ADR-0007 / ADR-0021 | The distrusted-kid module realizes the break-glass eject SLO; the version-sensitive OpenIddict and IdentityModel facts are catalogued seams with a per-bump contract test |
| ADR-0068 (proposed) | A v2 Shared-Signals/CAEP channel may externalize revocation to relying parties as a parallel addition that must not alter the internal enforcement here |

## 2. Purpose and scope

How a revocation performed on one node takes effect on every other node, and how the
caches that could hold stale authorization data are kept coherent. The starting premise
of the original audit finding was wrong, and correcting it is the design: OpenIddict's
entity caches (Application/Scope/Authorization/Token managers) are `TryAddScoped` private
per-request `MemoryCache`s with a local change-token and no `IDistributedCache` and no
cross-node hook, so a stale read cannot outlive a single HTTP request. There is nothing to
invalidate across nodes, and no Redis pub/sub backplane for the manager cache is needed or
built. Revocation freshness is instead solved **per path**, each with the standard
mechanism already present in OpenIddict, ASP.NET Core, or an established library.

This design is largely an **integration layer**. It references the per-path targets owned
elsewhere and owns only the cross-node propagation and cache-coherence over them, plus the
two pieces that live nowhere else: the fail-closed distrusted-kid module (path e) and the
config-cache backplane (path f), and the Redis/FusionCache wiring that foundations left
open. It adds no database tables and emits only audit events already in the catalog.

In scope: the reframe and the six-path model; the distrusted-kid set (owned here, with the
key-management design providing only the break-glass trigger); the config-cache
FusionCache + Redis backplane; the JWKS/discovery output-cache eviction; the fail-open
versus fail-closed discipline; and the Redis/FusionCache wiring.

Out of scope, referenced not redefined: the revocation endpoint, entry-validation flags,
per-client `AccessTokenType`, the introspection result cache, and the 15-minute access TTL
(04); server-side sessions, `ITicketStore` force-logout, `ValidationInterval`, and the
session side of `RevokeBySubjectAsync` (08); the key-break-glass trigger and the RS
validation-key set (12); and the schema (02).

One boundary is worth naming explicitly because the design corpus draws it differently.
That corpus bundles **single logout** into the same document as revocation propagation, so
a reader arriving from it will look for the back-channel fan-out here. In this layer the
fan-out and its `logout_token` mint are owned by the login, consent, and logout design
(11), the outbox chassis the fan-out reuses is 10, the delivery table is 02, and the
session revoke that triggers it is 08. This design owns none of that.

## 3. Interfaces and contract

### 3.1 OpenIddict and IdentityModel facts this design rests on (pinned seams)

| Fact | Consequence |
|---|---|
| The manager entity caches are `TryAddScoped` private `MemoryCache`s, `EntityCacheLimit = 250`, no time-TTL (size-bounded eviction), local change-token, no `IDistributedCache` | A stale entity-cache read dies at the end of the request; no cross-node invalidation is needed for it |
| Entity caching can only be switched off with a **single global `DisableEntityCaching` flag**, not per tenant or per client | There is no way to make one tenant's reads uncached; the per-request lifetime is the whole of the guarantee, and it is enough |
| `EnableTokenEntryValidation` / `EnableAuthorizationEntryValidation` live on the **`.AddValidation()`** builder (not `.AddServer`); enabled, each API request makes a DB call to validate the entry | Paths (a/b/d) are DB-direct and therefore already cross-node-consistent; the cost is DB load, not propagation lag. Reaching for these on the server builder is the mistake to avoid |
| There are **two distinct validator caches**: the client-side `IdentityModel DiscoveryCache` (24h) and the resource-server `ConfigurationManager` JWKS cache (~12h, `AutomaticRefreshInterval`) | Only the RS JWKS cache holds stale *revocation* data (a distrusted `kid`); it is the one addressed by path (e). Do not conflate the two |
| `AutomaticRefreshInterval` defaults to 12h with a 5-minute floor (`MinimumAutomaticRefreshInterval`) enforced by a throw, not a silent clamp; it is a different constant from `RefreshInterval` (5-minute default, 1-second minimum) | Shortening `AutomaticRefreshInterval` to ~5 minutes for break-glass is the lowest legal value, not a workaround |
| Those constants live on `BaseConfigurationManager` in `Microsoft.IdentityModel.Tokens`, pulled transitively via `Microsoft.IdentityModel.Protocols` | Pin `Microsoft.IdentityModel.Protocols >= 8.16.0` and re-verify the constants on each bump (ADR-0021) |
| `RefreshTokenReuseLeeway` defaults to 30 seconds, and Nami sets 30 seconds, so the configured value **matches** the default | The value is explicit for readability, not to harden a weaker default. An earlier 15-second draft was corrected upward because it sat below typical network timeouts (ADR-0004) |

### 3.2 The six revocation paths

| Path | Freshness needed | Cache holding stale | Mechanism | Owned by |
|---|---|---|---|---|
| (a) access-token revoke | JWT to expiry / reference immediate | the JWT itself | short-TTL 15-min JWT + reference token for sensitive clients | 04 (referenced) |
| (b) refresh-family revoke | immediate on next use | none (DB-direct) | native family-revoke on rotation, DB-fresh | 04 (referenced) |
| (c) force-logout / session | near-immediate | cookie not yet re-validated | `ITicketStore` row-delete + `ValidationInterval` 1-2 min | 08 (referenced) |
| (d) delegated-admin grant revoke | immediate (live check) | none (DB-direct) | `EnableAuthorizationEntryValidation`, DB-fresh | 04/05 (referenced) |
| (e) signing-key break-glass | 60s or less (SLO) | RS JWKS cache ~12h | ~5-min RS refresh + fail-closed distrusted-kid set | **this design** |
| (f) client/scope config change | 30s or less (SLO) | the config cache you add | FusionCache + Redis backplane | **this design** |

Paths (a) to (d) are DB-direct or expiry-bounded and depend on no cross-node channel; the
hot 10k-CCU token path therefore has **no mandatory synchronous Redis hit**. Redis is a
bounded fast path for the low-traffic paths (e) and (f) only.

## 4. Data and structure

This design defines no tables. It reads `SigningKeys.RevokedAt` (the source of truth the
distrusted-kid set is rebuilt from), the `ServerSideSessions` rows (whose deletion is
force-logout), and the OpenIddict `TenantToken` / `TenantAuthorization` tables (which
entry-validation reads); all are defined in 02. The distrusted-kid set and the config
cache are intentionally non-persistent (Redis plus in-memory); if any persistence were
ever needed it would be raised as an ADR, not settled here.

## 5. Behaviour

### 5.1 The reframe, and the shape it leaves behind

The premise that had to be corrected was that a Redis pub/sub backplane was needed to
invalidate the engine's manager cache. It is not, because that cache never crosses a
request boundary. What remains after the correction is smaller and has fewer failure modes
than the original framing implied, which is the unusual case of a finding making a design
simpler rather than larger.

Two comparisons are worth stating so that "no backplane" is not misread as a missing
feature. A per-request entity cache is **stricter** than the expiry-only in-process
configuration caches the commercial engines ship, where staleness is bounded by a refresh
interval of minutes rather than by one request, so the absence of a backplane here is a
tighter guarantee and not a gap. Conversely, paths (e) and (f) **go beyond** those engines:
they ship no emergency signing-key revocation at all, only rotation and retention, and
their configuration caches expire without cross-node invalidation. Those two paths are
therefore deliberate additions rather than parity work, and they are the only two this
design owns.

```mermaid
sequenceDiagram
  autonumber
  participant A as Node A (revoker)
  participant DB as PostgreSQL
  participant R as Redis
  participant B as Node B (validator)

  alt reference token / grant (a, b, d)
    A->>DB: TryRevokeAsync / family-revoke / grant revoke
    B->>DB: entry-validation on next request, sees revoked, 401 (DB-fresh)
  else force-logout (c)
    A->>DB: delete ITicketStore session row
    B->>DB: cookie re-validation within ValidationInterval, row gone, reject
  else signing-key break-glass (e)
    A->>R: add kid to distrusted-kid set, un-register key, evict JWKS
    B->>R: check distrusted-kid (L1 first), reject within 60s
  else config change (f)
    A->>R: FusionCache backplane publishes invalidation
    B->>B: L1 entry evicted within 30s, next read refreshes
  end
```

### 5.2 Paths a, b, d: token, refresh, and grant (referenced from 04)

The tiered model is a short-TTL JWT validated locally by default, with reference tokens
plus introspection reserved for the clients that need instant revocation.

What revocation actually does is easy to get backwards. **A revoke marks the token
*entry*, and it does so for a JWT exactly as for a reference token**: the operation is not
tied to the token format and does not require reference tokens, as long as token storage is
not disabled. What differs is whether anyone looks. A self-contained JWT validated locally
by a resource server stays acceptable until it expires, because nothing on that path
consults the entry; the same revoke is honoured immediately once
`EnableTokenEntryValidation` or introspection is in play. So the 15-minute residual on the
JWT path is a property of the **validation mode**, not evidence that the revoke failed.

Per-client `AccessTokenType` (jwt or reference, default jwt) is enforced by the custom
`GenerateTokenContext` handler (04); opting a client into reference forces that client's
resource server onto introspection, because an opaque reference token cannot be validated
locally. That cost decides the selection: **JWT** for high-volume, first-party,
BFF-fronted, and machine-to-machine clients, where local validation is the point;
**reference** for administrative, privileged, and high-assurance clients, where instant
revocation is worth an introspection round trip per call.

The revocation endpoint is single-token native (no cascade); "log out everywhere" is the
separate built `RevokeBySubjectAsync`, and family-revoke by `AuthorizationId` is native and
must not be double-called. Family-revoke also **leaves the `Authorization` row in place**
and only revokes the sibling tokens, deliberately, so a legitimate client can start a fresh
flow; a test that asserts the authorization itself was revoked will fail (ADR-0004).
`EnableTokenEntryValidation` and `EnableAuthorizationEntryValidation` (on
`.AddValidation()`) make token and grant checks DB-direct, so a revoke on node A is seen by
node B on its next validation with no lag; the cost is one DB read per validation, measured
on the hot path in the capacity design (19). The introspection result cache (~5 min, owned
by 04) is the one caching TTL that trades directly against revocation freshness, so its
value is reconciled against the revocation SLO rather than set independently.

### 5.3 Path c: force-logout (referenced from 08)

Force-logout deletes the session row from the shared `ITicketStore` (PostgreSQL); the
row's absence *is* the revoked state, so the next request on any node fails cookie
re-validation within the 1-2 minute `ValidationInterval`. No backplane is involved.

### 5.4 Path e: the distrusted-kid module (owned here)

When a signing key is revoked in a break-glass event, tokens it signed must stop
validating fast, but a resource server's `ConfigurationManager` caches the JWKS for about
12 hours. Two mechanisms close that window, and both are owned here (the key-management
design owns only the *trigger*, setting `RevokedAt`, refreshing its cache, tripping the
change-token, un-registering the cert, and evicting JWKS, and references this module):

- The resource-server `AutomaticRefreshInterval` is shortened to about 5 minutes (the
  lowest legal automatic value); on a distrusted-kid signal a resource server may
  additionally call `ConfigurationManager.RequestRefresh()` (whose `RefreshInterval` floor
  is about 1 second) to re-fetch the JWKS on demand. This complements, and does not
  replace, the set below, which enforces the SLO even before any JWKS refresh.
- A Redis-backed **distrusted-kid set** is checked on every validation, **fail-closed**:
  if Redis is unreachable or the answer cannot be confirmed, the `kid` is treated as
  distrusted, not trusted (the classic denylist-fails-open trap, avoided the way CRL and
  OCSP do for certificates). It is served from an in-process `HybridCache` L1 so the happy
  path takes no per-request Redis hit, and cross-node propagation is under 60 seconds.

```mermaid
flowchart TD
  classDef ok fill:#d5e8d4,stroke:#82b366,color:#000
  classDef bad fill:#f8cecc,stroke:#b85450,color:#000
  V["Validate token, read kid"] --> L1{In L1 distrusted set?}
  L1 -->|yes| REJ["Reject, invalid_token"]:::bad
  L1 -->|no / stale| RD{Redis reachable?}
  RD -->|yes, not distrusted| OK["Accept, signature + iss + aud + tenant"]:::ok
  RD -->|yes, distrusted| REJ
  RD -->|unreachable| REJ
```

The set is **not persisted**: it is Redis-only and rebuildable from `SigningKeys.RevokedAt`
(02), so this design adds no table. The JWKS/discovery output cache (`AddOutputCache` with
a Redis backplane, tag-evicted on rotation) is owned here as well; local self-validation
drops a revoked key through the key-management design's live `IConfigurationManager` (12),
not through this set.

The published `kid` has to be the same string the key store holds, or this path silently
addresses nothing: the engine infers a `kid` from a certificate thumbprint when a key
carries none, so the key identifier is set explicitly at rotation (12). A distrusted-kid
entry naming an identifier that no token ever carried is indistinguishable from a working
denylist until the moment it is needed.

### 5.5 Path f: the config-cache backplane (owned here)

Client, scope, and tenant configuration is cached for latency (the per-client CORS provider
of 04 reads this same cache), and a config change must reach every node within about 30
seconds. Bare .NET 10 `HybridCache` has no cross-node L1 invalidation (open proposal
dotnet/runtime #125602), so the config cache is **FusionCache plus a Redis backplane**,
which adds the cross-node invalidation and brings built-in stampede protection (a single
concurrent factory) and jittered TTLs. Probabilistic early expiration of that kind is the
XFetch result (Vattani and others, PVLDB 2015), which is the citable origin for the
technique. This is the single system of record for that cache; it is invalidated when a
client changes, and a cold refresh lists the applications per tenant under the Finbuckle
ambient context.

### 5.6 Failure modes and the fail-closed discipline

- **Redis pub/sub is at-most-once** (fire-and-forget): it is a fast path only, never the
  sole source of truth, and every path it accelerates is bounded by a TTL or backed by a
  durable store.
- **Fail-open versus fail-closed.** Ordinary performance caches fail open (a miss reads
  through to the durable store at higher latency, never a 5xx); security checks fail closed.
  The distrusted-kid set is fail-closed under this general rule, not as a special
  carve-out (the one deliberate carve-out in the resiliency posture is the email throttle,
  ADR-0038, which is not part of this design). Note the DPoP `jti` replay cache (06) is a
  different pattern again: Redis is its **authoritative L2 store** (a replay set has no DB
  backstop to read through to), so it is fail-closed by necessity, not an accelerator in
  front of a durable source.
- **Redis down.** Paths (a) to (d) do not depend on Redis and stay DB-fresh; path (e) fails
  closed; path (f) degrades to the durable store with a short TTL. The session store is
  durable PostgreSQL and the Data Protection keyring is independent of Redis, so an outage
  degrades latency, not authentication. This is load-tested (no 5xx, higher latency, auth
  continues).
- **A retried revoke is safe.** Revoking a token that does not exist is rejected inside the
  engine and then normalized to an RFC 7009 success response, so a client or operator
  retrying after a timeout does not see an error and does not have to distinguish "already
  revoked" from "never existed" (04).

### 5.7 v2 awareness (does not touch v1 enforcement)

A proposed v2 channel (Shared Signals Framework / CAEP as a transmitter, ADR-0068; the
identity-change-events design) would externalize the revocations here to relying parties as
standard Shared-Signals events, and would emit backend integration events through an outbox
from a single handler at the existing back-channel-logout and session-revoke seam. It is a
**parallel addition** for external consumers; it does not replace or alter the internal
per-path enforcement in this design, and v1 stays frozen.

## 6. Dependencies and wiring

### Patterns applied

Patterns applied (ADR-0066, whose list is explicitly not exhaustive): **Cache-aside** for
the config cache and the distrusted-kid L1 read; **Publish-subscribe** for the FusionCache
Redis backplane that carries invalidation between nodes; and **Adapter** for the cache and
Redis edges, which stay behind ports so Redis is swappable and never a call-site
dependency. There is deliberately **no Outbox** here: this design propagates state, it does
not deliver messages, and the outbox paths belong to 03 and 10.

### Libraries

All permissive (ADR-0026), with the exact package identifiers a license scan can act on,
each read from its own package metadata rather than from a summary:

| Package | License |
|---|---|
| `ZiggyCreatures.FusionCache` | MIT |
| `ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis` | MIT |
| `StackExchange.Redis` | MIT |
| `Microsoft.Extensions.Caching.Hybrid` | MIT |

### Wiring (extends the foundations composition)

The foundations design names Redis and FusionCache as phase-later libraries but does not
wire them; this design adds that wiring: the Redis connection (externalized per-deploy
config), FusionCache with its Redis backplane for the config cache, and the `HybridCache`
L1 for the distrusted-kid set. Any new port this introduces (for example a distrusted-key
checker) is declared in its own package, not retro-added to the Phase-01 ports catalog, and
Redis is never the sole source of truth (consistent with the `ITicketStore` principle in
01). The degraded-mode startup guard (ADR-0043) underpins the fail-closed stance.

## 7. Error handling, edge cases, invariants

- **The distrusted-kid check fails closed**, including when Redis is unreachable; there is
  no configuration that turns it into an accept-all.
- **Redis is never a source of truth.** Every Redis-accelerated path is bounded by a TTL
  and rebuildable from a durable store (`SigningKeys.RevokedAt` for path e, the
  configuration tables for path f).
- **The entry-validation flags belong to `.AddValidation()`.** Registering them on the
  server builder does not fail loudly; it simply leaves validation uncoupled from the
  entry, which is the silent version of this design not working.
- **A revoke marks the entry regardless of token format**, so a design that wants instant
  revocation changes the validation mode rather than the revoke.
- **Family-revoke leaves the authorization row**, so nothing downstream may assume its
  absence.
- **No new audit event type is invented here**; a genuinely new one is raised into the 03
  catalog.

## 8. Security and multi-tenancy notes

- A shared Pool signature is not a tenant boundary; the resource server validates
  signature and issuer and audience and the `tenant` claim (ADR-0049), so a revoked key or
  a cross-tenant token is rejected regardless of cache state.
- **The revocation and introspection paths are themselves tenant-scoped.** Beyond the
  engine's native caller confinement, the token-entry lookup rides the Pool tenant filter,
  so a caller in tenant A cannot revoke or introspect an entry belonging to tenant B (04).
  That is a store-level guarantee, distinct from the token-validation checks above, and a
  cross-tenant negative test covers it.
- The JWT path has an inherent 15-minute revocation residual (a self-contained token
  cannot be pulled back mid-life); the tiered model gives reference tokens to the clients
  that cannot tolerate it.
- This design emits only audit events already in the catalog (03): `token_revoked`
  (critical, synchronous), `mass_revoke`, `force_logout`, `key_rotation`, `break_glass`,
  `degraded_mode_enabled`, and `refresh_reuse_detected`.

## 9. Testing

- **Cross-node propagation:** a revoke on node A is enforced on node B within the path's
  SLO, the distrusted-kid set within 60 seconds, config within 30 seconds,
  a reference-token revoke DB-fresh with no lag, and force-logout on the next request.
- **Redis-down fail-closed:** with Redis unreachable, the distrusted-kid check rejects,
  and paths (a) to (d) continue DB-fresh.
- **No hot-path Redis:** the 10k-CCU token path issues and validates with Redis down (no
  5xx, latency only).
- **Cross-tenant revoke and introspect:** a caller in one tenant cannot revoke or
  introspect another tenant's token entry (a negative test riding the Pool-isolation
  suite).
- **Revoke retry:** revoking an unknown or already-revoked token returns success, so a
  retry after a timeout is indistinguishable from the first call.
- **Contract regression (per OpenIddict and IdentityModel bump):** the entity cache is
  still per-request, the entry-validation flags are still on `.AddValidation()`, the
  `AutomaticRefreshInterval` 5-minute floor still holds, `RefreshTokenReuseLeeway` still
  defaults to 30 seconds, and `Microsoft.IdentityModel.Protocols` is pinned at or above
  8.16.0.

## 10. Open and build-time items

- **SLO numbers deferred:** the concrete propagation-time and cache-TTL targets (the 60s,
  ~5-min, 15-min, and 30s figures are interim design values) are ratified with the SLO
  numeric table and error-budget policy (Product/Ops, ADR-0041, Pre-GA checklist).
- **Break-glass automation sign-off:** the multi-node cache-evict automation designed here
  has its operational sign-off (and the authorized-personnel list) deferred to
  ADR-0007 (Security, Pre-GA checklist), also flagged in 12.
- **Deferred to v2:** the Shared-Signals/CAEP transmitter and the backend event-bus
  (ADR-0068 proposed; the v2 change-event design is not in this layer yet).

## 11. Sources

- ADRs: ADR-0039 (revocation propagation), ADR-0040 (resiliency / Redis accelerator),
  ADR-0003 (sessions), ADR-0004 (token posture, the 30-second leeway and the family-revoke
  semantics), ADR-0007 (break-glass), ADR-0048 (introspection), ADR-0049 (resource-server
  validation), ADR-0021 (seam catalogue), ADR-0026 (licenses), ADR-0043 (degraded mode),
  ADR-0066 (patterns), ADR-0068 (v2 Shared Signals, proposed).
- Design docs: [04 core protocol](04-core-protocol.md) (revocation endpoint,
  entry-validation, `AccessTokenType`, introspection cache, 15-min TTL, caller and tenant
  confinement), [08 user management](08-user-management.md) (sessions, force-logout,
  `ValidationInterval`), [11 login, consent, and logout](11-login-consent-ui.md) (the
  back-channel logout fan-out this design does not own), [12 key
  management](12-key-management.md) (break-glass trigger, the explicit `kid`, the RS
  validation-key set), [02 data](02-data.md) (`SigningKeys.RevokedAt`,
  `ServerSideSessions`), [01 foundations](01-foundations.md) (Redis / cache wiring),
  [03 audit](03-audit.md) (event catalog),
  [19 observability](19-observability-capacity-slo.md) (the numeric SLO table and the
  hot-path measurement).
- [Architecture](../architecture/README.md): runtime view 4 (cross-node revocation),
  cross-cutting (resiliency and overload, the fail-closed carve-outs).
- Verification: the cross-node revocation research (R15); the introspection and revocation
  edge-case deep-dive that established single-token revocation and native caller
  confinement (V14); the `AutomaticRefreshInterval` floor check. Package licenses were read
  from package metadata. Unbenchmarked latency and availability figures circulating in blog
  posts about this pattern are deliberately not cited.
- [Pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

---

[Prev: Key management and rotation](12-key-management.md) · [Index](README.md) · Next: [Advanced flows](14-advanced-flows.md)
