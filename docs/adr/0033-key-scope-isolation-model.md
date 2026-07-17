---
status: "accepted"
date: 2026-07-05
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security (ratifying the accepted-risk Pool-shared keyset before GA); source-verified key-isolation research (the OpenIddict options pipeline and per-tenant tenanted-options prior art)
informed: all contributors, via this repository
---

# Align key-scope isolation to the tenant tier with one keyset per deployment and a scope-aware key store

## Context and Problem Statement

A final-review finding (critical) was that the design intended signing and encryption keys scoped per tenant or pool-group (a `SigningKeys.KeyScope = pool-group | tenant` column), but the wiring was scope-blind: `ISigningKeyStore.LoadAsync(ct)` had no scope argument, and the custom `IOptionsMonitor<OpenIddictServerOptions>` cache was keyed only by version. As designed, therefore, every tenant shared one key list, the JWKS leaked across tenants, and per-tenant rotation and revocation were impossible. The issuer was already per-tenant (spike A-5), but the key was not.

Source-verified research established that per-tenant keys via tenanted options is a long-term-clean, maintainer-endorsed approach (OpenIddict issue #1434 plus prior art in the identity-server community), not a workaround. But two topologies differ fundamentally: (A) one instance serving many key-scopes at once (co-host) needs per-request scope resolution and has composition unknowns that must be spiked; and (B) one keyset per deployment lets the monitor stay version-only, provably works on the pinned OpenIddict 7.5, and is the least machinery. The crux, settled by Nam on 2026-07-05, is that one instance does not serve more than one key-scope at once, so Option B is chosen.

## Decision Drivers

* Fix the scope-blind wiring so per-tenant/pool-group key isolation actually works (no JWKS cross-leak, and per-scope rotate/revoke).
* Isolation must match the tenant tier (ADR-0001).
* Prefer the topology that provably works on the pinned OpenIddict 7.5 with the least machinery.
* Give the key store the defense-in-depth the token store already has.

## Considered Options

* Option A: co-host, with a per-request per-scope key via tenanted options
* Option B: tier-aligned, one keyset per deployment
* Option C: a separate instance/issuer per scope

## Decision Outcome

Chosen option: "Option B, tier-aligned with one keyset per deployment", because one instance never serves more than one key-scope, so B provably works on OpenIddict 7.5 with the least machinery, while A's composition unknowns stay behind a spike gate.

* **Pool deployment = one pool-group keyset** (`KeyScope = pool-group`): every tenant in the pool-group shares that keyset. This is an explicit **accepted risk** — a leaked Pool key affects every tenant in the pool-group — so a tenant needing strong crypto-isolation must choose Silo (or Option C), and Security ratifies this risk before GA.
* **Silo = a per-tenant keyset** (`KeyScope = tenant`), through its own connection/deployment (ADR-0018), which naturally yields one keyset per instance.
* **Each running instance serves exactly one keyset**, as an invariant. The custom `IOptionsMonitor` (ADR-0011) stays version-only (correct for B, with no per-request scope resolution needed), and the `UseLocalServer` single snapshot is a non-issue because one scope is one snapshot.
* **The scope fix is mandatory for every tier:** `ISigningKeyStore.LoadAsync(ct)` becomes `LoadAsync(KeyScope scope, ct)`, with the scope fixed per deployment (from config/connection, not resolved per request in B), and the cache is no longer keyed only by version (scope is added, even though in B it is a constant of the instance).
* **A data-layer backstop:** the `SigningKeys` table (and the encryption keys) must carry a mandatory scope predicate centralized in a single adapter, plus a unit test that no query omits the scope; for a Pool multi-scope store, row-level security on `(KeyScope, TenantId/pool-group)` is considered. The key store must not lack the defense-in-depth the token store already has.
* **Encryption/JWE follows the same pipeline and the same Option B model;** only the volume differs (the roughly eight-hour retention means more overlapping keys per scope).
* **Upgrade path (Option A):** if co-hosting multiple pool-groups or processes is ever required, run a spike (a single-owner tenanted-options form: drop the custom monitor, make the multi-tenant library the sole owner, hold the version in the configure delegate, and rotate via `Clear(tenantId)`), plus a cross-scope JWKS negative test and moving `UseLocalServer` to JWKS-based validation. Option A is not shipped on trust.

### Consequences

* Good, because it provably works on OpenIddict 7.5 with the least machinery and no unknowns, maps directly to the `KeyScope` column, keeps the ADR-0011 monitor unchanged, fixes the scope-blind wiring for both signing and encryption, and gives the store a data-layer backstop.
* Good, because isolation matches the tier (Pool is a shared crypto boundary, Silo is isolated), which is honest and easy to explain to consumers.
* Bad, because Pool tenants share a keyset and so are not crypto-isolated within the pool-group; this is an accepted risk that is documented for adopters and ratified by Security, with strong isolation available by choosing Silo.
* Bad, because it does not co-host multiple pool-groups or processes (which would require the Option A spike).
* Bad, because it needs a `LoadAsync` signature refactor, the data-layer backstop, and the invariant formalized.

### Confirmation

* The source-verified key-isolation research (the OpenIddict options pipeline, per-tenant tenanted options, the #1434 prior art, and the composition crux) was adversarially verified as sound with caveats. Primary-verified: the `.First()` signing selection, the per-request `CurrentValue` snapshot, and the PostConfigure validation order. Doc-sourced, so to be spiked if Option A is taken: the `UseLocalServer` single snapshot and the JWKS publishing the whole list.
* OpenIddict honors a single-scope per-request keyset with zero handler changes, so B is exactly the topology the version-only monitor was built for.

## Pros and Cons of the Options

### Option A: co-host with a per-request per-scope key

* Good, because one instance could serve many pool-groups, and the tenanted-options approach is maintainer-endorsed.
* Bad, because it carries composition unknowns that must be spiked (multi-owner cache interplay, the tenant accessor at JWKS/self-validation under pooling, the single-snapshot `UseLocalServer`) and may force `UseLocalServer` onto JWKS-based validation.

### Option B: tier-aligned, one keyset per deployment (chosen)

* Good, because it provably works on 7.5 with the least machinery, maps to the tier, and keeps the existing monitor.
* Bad, because Pool tenants are not crypto-isolated within the pool-group (the accepted risk) and multiple pool-groups cannot be co-hosted.

### Option C: a separate instance/issuer per scope

* Good, because it is the strongest isolation, effectively Silo taken to its limit.
* Bad, because it is the most infrastructure per scope, so it is reserved for high-isolation tenants rather than the default.

## More Information

* Original decision 2026-07-05 (Option B); Option A is a spike-gated upgrade path taken only if co-hosting becomes mandatory.
* Open follow-ups: Security ratifies the accepted-risk Pool-shared keyset (the pool-group blast radius) before GA and it is documented for adopters; and build-time work refactors `ISigningKeyStore.LoadAsync(scope, ct)`, adds the data-layer backstop (a centralized scope predicate with tests, or RLS), and formalizes the "one keyset per deployment" invariant with a startup assertion.
* Related decisions: ADR-0001 (the Pool/Silo tier this scopes keys by), ADR-0005 (the encryption-credential lifecycle, the same model for JWE keys), ADR-0011 (the no-restart rotation monitor kept version-only; this is the ADR that amends `LoadAsync(ct)` to `LoadAsync(scope, ct)`), ADR-0018 (pooled DbContext, where a Silo's own connection gives one keyset per instance), and ADR-0021 (re-verifying the #1434 seam on each OpenIddict bump).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial identity server's community discussion cited as prior art was generalized (the OpenIddict issue #1434 is retained as the dependency's public issue); the multi-tenant library is described generically; and the internal audit-finding labels are kept as the decision's own traceability.
