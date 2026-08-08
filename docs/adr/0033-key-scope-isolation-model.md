---
status: "accepted"
stack-record: true
date: 2026-07-05
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security (ratifying the accepted-risk Pool-shared keyset before GA); source-verified key-isolation research (the OpenIddict options pipeline and per-tenant tenanted-options prior art)
informed: all contributors, via this repository
---

# Align key-scope isolation to the tenant tier with one keyset per deployment and a scope-aware key store

## Context and Problem Statement

A final-review finding, **F1** (critical), was that the design intended signing and encryption keys scoped per tenant or pool-group (a `SigningKeys.KeyScope = pool-group | tenant` column), but the wiring was scope-blind: `ISigningKeyStore.LoadAsync(ct)` had no scope argument, and the options cache behind the #1434 seam (ADR-0011) was keyed only by version. As designed, therefore, every tenant shared one key list, the JWKS leaked across tenants, and per-tenant rotation and revocation were impossible. The issuer was already per-tenant (spike A-5), but the key was not.

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

* **Pool deployment = one pool-group keyset** (`KeyScope = pool-group`): every tenant in the pool-group shares that keyset. This is an explicit **accepted risk** (a leaked Pool key affects every tenant in the pool-group), so a tenant needing strong crypto-isolation must choose Silo (or Option C), and Security ratifies this risk before GA.
* **Silo = a per-tenant keyset** (`KeyScope = tenant`), through its own connection/deployment (ADR-0018), which naturally yields one keyset per instance.
* **Each running instance serves exactly one keyset**, as an invariant. The options cache behind the #1434 seam (ADR-0011) stays version-only (correct for B, with no per-request scope resolution needed), and the `UseLocalServer` single snapshot is a non-issue because one scope is one snapshot.
* **The F1 scope fix is mandatory for every tier:** `ISigningKeyStore.LoadAsync(ct)` becomes `LoadAsync(KeyScope scope, ct)`, with the scope fixed per deployment (from config/connection, not resolved per request in B), and the cache is no longer keyed only by version (scope is added, even though in B it is a constant of the instance).
* **The F2 data-layer backstop:** the `SigningKeys` table (and the encryption keys) must carry a mandatory scope predicate centralized in a single adapter, plus a unit test that no query omits the scope; for a Pool multi-scope store, row-level security on `(KeyScope, TenantId/pool-group)` is considered. The key store must not lack the defense-in-depth the token store already has.
* **Encryption and JWE (finding F49) follow the same pipeline and the same Option B model;** only the volume differs (the roughly eight-hour retention means more overlapping keys per scope).
* **Upgrade path (Option A):** if co-hosting multiple pool-groups or processes is ever required, run a spike (folding in spike A-5's hypothesis H3, in a single-owner tenanted-options form: drop the custom monitor, make Finbuckle the sole owner of the options cache, hold the version in the configure delegate, and rotate via `Clear(tenantId)`), plus a cross-scope JWKS negative test and moving `UseLocalServer` to JWKS-based validation. Option A is not shipped on trust.

### Consequences

* Good, because it provably works on OpenIddict 7.5 with the least machinery and no unknowns, maps directly to the `KeyScope` column, keeps the ADR-0011 monitor unchanged, fixes the scope-blind wiring for both signing and encryption (findings F1 and F49), and gives the store a data-layer backstop (finding F2).
* Good, because isolation matches the tier (Pool is a shared crypto boundary, Silo is isolated), which is honest and easy to explain to consumers.
* Bad, because Pool tenants share a keyset and so are not crypto-isolated within the pool-group; this is an accepted risk that is documented for adopters and ratified by Security, with strong isolation available by choosing Silo. At the resource server the shared key is mitigated by mandatory issuer and tenant binding (ADR-0049); without it, the shared signature would allow cross-tenant token acceptance.
* Bad, because it does not co-host multiple pool-groups or processes (which would require the Option A spike).
* Bad, because it needs a `LoadAsync` signature refactor, the data-layer backstop, and the invariant formalized.

### Confirmation

* The source-verified key-isolation research (the OpenIddict options pipeline, per-tenant tenanted options, the #1434 prior art, and the composition crux) was adversarially verified as sound with caveats; it is recorded as the F1 key-isolation research note, and findings F1, F2, and F49 are recorded in the 2026-07 final-review record. Primary-verified: the `.First()` signing selection, the per-request `CurrentValue` snapshot, and the PostConfigure validation order. Doc-sourced, so to be spiked if Option A is taken: the `UseLocalServer` single snapshot and the JWKS publishing the whole list.
* OpenIddict honors a single-scope per-request keyset with zero handler changes, so B is exactly the topology the version-only monitor was built for.

## Pros and Cons of the Options

### Option A: co-host with a per-request per-scope key

* Good, because one instance could serve many pool-groups, and the tenanted-options approach is maintainer-endorsed.
* Bad, because it carries composition unknowns that must be spiked (a two-owner conflict between Finbuckle's per-tenant options cache and the custom monitor, the tenant accessor at JWKS and self-validation under pooling, and the single-snapshot `UseLocalServer`) and may force `UseLocalServer` onto JWKS-based validation.

### Option B: tier-aligned, one keyset per deployment (chosen)

* Good, because it provably works on 7.5 with the least machinery, maps to the tier, and keeps the existing monitor.
* Bad, because Pool tenants are not crypto-isolated within the pool-group (the accepted risk) and multiple pool-groups cannot be co-hosted.

### Option C: a separate instance/issuer per scope

* Good, because it is the strongest isolation, effectively Silo taken to its limit.
* Bad, because it is the most infrastructure per scope, so it is reserved for high-isolation tenants rather than the default.

## More Information

* **Corrected 2026-08-08: the Related-decisions bullet no longer calls ADR-0018 "pooled DbContext".**
  [ADR-0018](0018-dbcontext-pooling-for-pool-mode.md) is titled "Register the Pool-mode OpenIddict
  DbContext **non-pooled** in v1, with pooled-plus-mutable deferred", and `0018:35` chooses the
  non-pooled scoped `AddDbContext` for v1. So this ADR labelled its sibling by the option that sibling
  declined. The rest of the bullet is unchanged and was correct: a Silo's own connection does give one
  keyset per instance, which is what this ADR relies on.
  * **How it was found matters more than the wording.** It was not found by reading. Seed S-026 was
    scoped to two other ADRs and its verification asked for a `docs/adr/` sweep to come back clean.
    The sweep returned this line, so the seed grew from two ADRs to three. A verification that only
    confirms what the author already believed would have missed it.
  * **This is the sixth known instance of one defect and the fifth to be repaired.** `0061:145` and
    `architecture/07-container-view.md:288-290` record the first two, both 2026-07-25. ADR-0066's
    `Factory` entry was the third and [ADR-0036](0036-database-key-strategy-uuidv7.md)'s
    Related-decisions bullet the fourth, both 2026-08-08. **One remains**, and seed S-024 owns it:
    `architecture/03-drivers-and-constraints.md:116`, in the architecture layer rather than in an ADR.
    `0061:118` predicted this, saying the remaining rows deserved the same pass.
  * **"Non-pooled" is not the correction, and the enumeration of what is already right lives in
    ADR-0036's amendment** rather than being copied here, because a second copy of that list is a
    second place to be wrong. In short: pooling is used per context, three global contexts are pooled
    (`docs/design/02-data.md:55-59`), and about thirty other lines naming ADR-0018 near the word pool
    are accurate.
* Original decision 2026-07-05 (Option B); Option A is a spike-gated upgrade path taken only if co-hosting becomes mandatory.
* Open follow-ups: Security ratifies the accepted-risk Pool-shared keyset (the pool-group blast radius) before GA and it is documented for adopters; and build-time work refactors `ISigningKeyStore.LoadAsync(scope, ct)`, adds the data-layer backstop (a centralized scope predicate with tests, or RLS), and formalizes the "one keyset per deployment" invariant with a startup assertion.
* Related decisions: ADR-0001 (the Pool/Silo tier this scopes keys by), ADR-0005 (the encryption-credential lifecycle, the same model for JWE keys), ADR-0011 (the no-restart rotation monitor kept version-only; this is the ADR that amends `LoadAsync(ct)` to `LoadAsync(scope, ct)`), ADR-0018 (per-context `DbContext` pooling, where a Silo's own connection gives one keyset per instance), ADR-0021 (re-verifying the #1434 seam on each OpenIddict bump), and ADR-0049 (the resource-server issuer/tenant binding that mitigates the shared-key risk at token validation).
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25 to restore the F1, F2, and F49 finding labels in the body, the spike A-5 hypothesis H3 fold-in, and the real name of the multi-tenant dependency. A commercial identity server's community discussion cited as prior art stays generalized; the OpenIddict issue #1434 is kept, being the public issue tracker of a dependency Nami uses, and OSS packages Nami actually depends on are named, per ADR-0026.
