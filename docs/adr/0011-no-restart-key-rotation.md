---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: verification of the OpenIddict 7.5.0 source and a running spike (see Confirmation); integration tests are required before this seam is relied on
informed: all contributors, via this repository
---

# Rotate signing and encryption keys without restarting, via a custom OpenIddict options monitor

## Context and Problem Statement

OpenIddict has no native signing-key store or automatic rotation (see openiddict-core issue #386); keys are registered in code into the `OpenIddictServerOptions.SigningCredentials` list. A critical identity service must not restart on every key-rotation cycle — a rolling restart every 90 days was explicitly rejected — and the `IOptionsMonitorCache.Clear()` workaround was also rejected as fragile and as fighting the framework. Nami needs a dynamic, no-restart key-reload mechanism, comparable to the key-manager-plus-cache pattern that mature commercial identity servers ship. How should keys reload at runtime without a restart?

## Decision Drivers

* A critical identity service must rotate keys with zero downtime and no restart.
* The mechanism must not depend on a fragile hack that fights the framework.
* Key material must load from the abstraction port (DB-backed by default, cloud optional) established in ADR-0006 and ADR-0009.
* Nami is OSS-only (ADR-0026), so a paid turnkey component is a last resort, not the plan.

## Considered Options

* Rolling restart every 90 days
* An `IOptionsMonitorCache.Clear()` workaround
* A custom `IOptionsMonitor<OpenIddictServerOptions>` plus `ISigningKeyStore`, a TTL cache, and a tripped change-token (maintainer-endorsed, issue #1434)
* Buying a commercial key-rotation component

## Decision Outcome

Chosen option: "A custom `IOptionsMonitor<OpenIddictServerOptions>` plus `ISigningKeyStore`, a TTL cache, and a tripped change-token", because it rotates keys with no restart, uses a maintainer-endorsed seam, and stays OSS and cloud-agnostic.

Fixed parameters of the decision:

* The mechanism is a custom `IOptionsMonitor<OpenIddictServerOptions>` plus an `IConfigureOptions` that reads from `ISigningKeyStore`, an in-memory TTL cache, and a tripped `IOptionsChangeTokenSource`.
* The key store is an abstraction port with a **DB-backed default** (a `SigningKeys` table encrypted at rest via Data Protection); a cloud KMS/vault is optional (ADR-0006, ADR-0009).
* A 90/14/14 state machine (announce → active → retire → delete), a common industry pattern; signing uses the certificate with the furthest `NotAfter` (a future-`NotBefore` certificate does not sign); the JWKS publishes all asymmetric keys; validation accepts any key by `kid`.
* `ISigningKeyCache`: TTL of 24 hours in steady state, 1 minute when a new key exists; `SigningCredentials` are materialized once per version via `Lazy<>`; old certificates are tracked and disposed to avoid an `IDisposable` leak.
* **Local-validation gotcha (updated by a spike run on 2026-07-07; the source-read had been optimistic)**: `UseLocalServer` (the app validating its own tokens) snapshots signing keys into an immutable `StaticConfigurationManager` at startup, and tripping the change-token does not refresh it (`RequestRefresh()` is a no-op on the static manager), so a token signed with a new key fails self-validation with `ID2090` until restart. Scope is narrow: signing/issuance rotation is still no-restart (proven), and a remote resource server (`AddValidation` + issuer + JWKS) refreshes normally through a non-static configuration manager; only in-process `UseLocalServer` self-validation was frozen. **Proven fix**: replace the static manager with a custom non-static `IConfigurationManager<OpenIddictConfiguration>` that reads the live key store (installed via `IPostConfigureOptions<OpenIddictValidationOptions>`, setting `Configuration = null` and `ConfigurationManager = <dynamic>`), so a token signed with a new key self-validates immediately with no restart. The manager returns a key-**set** (the active signing key plus all validation keys, including retired ones), so both old and new tokens validate during the overlap window.
* Perf: `CurrentValue` is read several times per request, so materialized credentials are cached rather than recreated (no `RSA.Create()` per call).

### Consequences

* Good, because keys rotate with no downtime and no restart, cloud-agnostically, comparable to the automatic key management of mature commercial servers.
* Bad, because it relies on a seam that the OpenIddict maintainer endorses (issue #1434) but that is not in the official OpenIddict documentation, so it is fragile across OpenIddict minor upgrades; this mandates an "options-monitor contract regression test" on every bump (7.5 → 7.6 → 8.0) that fails the build if the contract breaks.
* This decision depends on ADR-0005 (separate encryption-credential lifecycle), ADR-0006 (DB-backed key store and DR), ADR-0007 (break-glass reuses this reload mechanism), ADR-0009 (store access), ADR-0026 (OSS-only, which the buy option would violate), and ADR-0033 (key-scope isolation, which amends `ISigningKeyStore.LoadAsync(ct)` to `LoadAsync(scope, ct)`).

### Confirmation

* OpenIddict source verified: `AttachSecurityCredentials` reads `context.Options.SigningCredentials.First()`; `AttachSigningKeys` iterates the whole list without filtering on `NotBefore`; `UseLocalServer` snapshots into a static configuration manager plus change-token; issue #1434 records the maintainer recommending a custom `IOptionsMonitor`.
* Mandatory integration tests before relying on the seam: tripping the change-token makes `UseLocalServer` self-validate a token signed with the new key without a restart; reading `CurrentValue` multiple times does not create a new key; a contract regression test runs on every OpenIddict version bump.
* Residual production items (not open decisions): the configuration manager reads the deployment's single key-set (keys are per-deployment under ADR-0033, so no per-request per-tenant key scoping is needed for v1); break-glass must remove a revoked key from the set, not only add (ADR-0007); and the TTL/`Lazy` cache must replace rebuilding the configuration on every call (perf at 10k concurrent users).
* Conditional contingency (not an open decision): if the seam proves too fragile in the regression test, open a mini-ADR to evaluate a fallback; buying a commercial key-rotation component conflicts with the OSS-only policy (ADR-0026) and would require its exception clause.

## Pros and Cons of the Options

### Rolling restart every 90 days

Restart the service each rotation cycle so it picks up the new key.

* Good, because it is trivial and uses no unofficial seam.
* Bad, because scheduled downtime on a critical identity service is unacceptable.

### An `IOptionsMonitorCache.Clear()` workaround

Clear the options cache to force a reload.

* Good, because it needs no custom infrastructure.
* Bad, because it is fragile, fights the framework, and is not an official extension seam.

### A custom `IOptionsMonitor` plus `ISigningKeyStore`, TTL cache, and change-token (chosen)

The maintainer-endorsed dynamic-reload seam (issue #1434), with the local-validation fix above.

* Good, because it rotates keys with no restart, stays OSS and cloud-agnostic, and keeps JWT self-validation.
* Bad, because the seam is undocumented and fragile across upgrades, requiring a contract regression test on every bump.

### Buying a commercial key-rotation component

Adopt a paid turnkey rotation add-on.

* Good, because it is turnkey and maintained by a vendor.
* Bad, because it is paid and conflicts with the OSS-only policy (ADR-0026); it is only a fallback under that policy's exception clause.

## More Information

* Original decision: 2026-06-28. This is a verify-before-build decision: the seam is maintainer-endorsed but absent from the official OpenIddict documentation, so the integration and contract tests above are required before it is relied on in production.
* Evidence: openiddict-core issue #386 (no native signing-key store or automatic rotation) and issue #1434 (the maintainer recommending a custom `IOptionsMonitor`). The 90/14/14 rotation-interval / propagation / retention shape follows a common industry pattern also seen in mature commercial identity servers. The 90-day rotation interval is deliberately conservative against NIST SP 800-57 Part 1 Rev.5 (Table 1), which allows a private signature key a cryptoperiod of 1 to 3 years; 90 days is well under that ceiling for an internet-facing IdP (verified V16).
* Related decisions: ADR-0005 (encryption-credential lifecycle), ADR-0006 (DB-backed key store and DR), ADR-0007 (break-glass reload), ADR-0009 (store access), ADR-0026 (OSS-only dependency/license policy), ADR-0033 (key-scope isolation and the scope-aware `LoadAsync`).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. References to a specific commercial identity server, a commercial key-rotation component, and a named maintainer were generalized; OpenIddict and its public issue numbers are retained as the project's own dependency.
