---
status: "accepted"
stack-record: true
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: verification of the OpenIddict 7.5.0 source and a running spike (see Confirmation); integration tests are required before this seam is relied on
informed: all contributors, via this repository
---

# Rotate signing and encryption keys without restarting, via the OpenIddict options change-token seam

## Context and Problem Statement

OpenIddict has no native signing-key store or automatic rotation (see openiddict-core issue #386); keys are registered in code into the `OpenIddictServerOptions.SigningCredentials` list. A critical identity service must not restart on every key-rotation cycle (a rolling restart every 90 days was explicitly rejected), and the `IOptionsMonitorCache.Clear()` workaround was also rejected as fragile and as fighting the framework. Nami needs a dynamic, no-restart key-reload mechanism, comparable to the key-manager-plus-cache pattern that mature commercial identity servers ship. How should keys reload at runtime without a restart?

## Decision Drivers

* A critical identity service must rotate keys with zero downtime and no restart.
* The mechanism must not depend on a fragile hack that fights the framework.
* Key material must load from the abstraction port (DB-backed by default, cloud optional) established in ADR-0006 and ADR-0009.
* Nami is OSS-only (ADR-0026), so a paid turnkey component is a last resort, not the plan.

## Considered Options

* Rolling restart every 90 days
* An `IOptionsMonitorCache.Clear()` workaround
* The framework options monitor driven by a custom `IConfigureOptions<OpenIddictServerOptions>` plus `ISigningKeyStore`, a TTL cache, and a tripped change-token (maintainer-endorsed, issue #1434)
* Buying a commercial key-rotation component

## Decision Outcome

Chosen option: "The framework options monitor driven by a custom `IConfigureOptions<OpenIddictServerOptions>` plus `ISigningKeyStore`, a TTL cache, and a tripped change-token" (the #1434 seam), because it rotates keys with no restart, uses a maintainer-endorsed seam, and stays OSS and cloud-agnostic.

Fixed parameters of the decision:

* **The mechanism is the framework `IOptionsMonitor<OpenIddictServerOptions>`, driven by a custom `IConfigureOptions<OpenIddictServerOptions>` that reads from `ISigningKeyStore`, an in-memory TTL cache, and a custom `IOptionsChangeTokenSource` tripped on rotation.** Nami writes the configure-options and the change-token source; it does **not** write the monitor. This is called "the #1434 seam" where it needs a short name.

  **Corrected 2026-08-01, and the old name was load-bearing rather than cosmetic.** This ADR previously said the mechanism *is* a custom `IOptionsMonitor`, and that phrasing had become the repository's canonical name for the whole approach. Reading issue #1434 that way and hand-building a monitor would break four things at once, because `OpenIddictServerConfiguration` is an `IPostConfigureOptions<OpenIddictServerOptions>` (`OpenIddictServerConfiguration.cs:22`) that runs on **every** materialisation and, verified at the pinned 7.5.0 source, does all of the following:

  1. sorts `options.Handlers` by `Order` (`:541`), so every `SetOrder` in this system (the DPoP handlers, the token-type handler) would silently run in registration order instead;
  2. sorts the signing and encryption credentials (`:544-545`), so seam S2's rule that the signer is whichever credential is **first** would pick an arbitrary key, possibly one that is announced but not yet valid, or retired;
  3. generates a `KeyId` for any key lacking one (`:550-552`), so JWKS entries would lose their `kid` and a resource server could not select a key;
  4. populates `TokenValidationParameters.IssuerSigningKeys` and `TokenDecryptionKeys` (`:556`, `:561`), so `UseLocalServer` self-validation would get an empty key set.

  A monitor that constructs its own options instance skips all four, and none of them fails loudly. **So the rule, if a custom monitor is ever written anyway: it must obtain options through `IOptionsFactory.Create(name)` and never `new` an instance.**
* The key store is an abstraction port with a **DB-backed default** (a `SigningKeys` table encrypted at rest via Data Protection); a cloud KMS/vault is optional (ADR-0006, ADR-0009).
* **Key material never leaves the store into an unsanctioned destination.** Key configuration (vault or KMS coordinates) and certificate bytes stay inside the store and its adapters; they are never pasted into an issue, a chat, a log, or any other destination outside the sanctioned path. ADR-0009 governs store access, and ADR-0012 applies the same rule to the root certificate that protects the keyring.
* A 90/14/14 state machine (announce → active → retire → delete), a common industry pattern; signing uses the certificate with the furthest `NotAfter` (a future-`NotBefore` certificate does not sign); the JWKS publishes all asymmetric keys; validation accepts any key by `kid`.
* `ISigningKeyCache`: TTL of 24 hours in steady state, 1 minute when a new key exists; `SigningCredentials` are materialized once per version via `Lazy<>`; old certificates are tracked and disposed to avoid an `IDisposable` leak.
* **Local-validation gotcha (updated by spike A-2, run 2026-07-07, verification record V19; the source-read had been optimistic)**: `UseLocalServer` (the app validating its own tokens) snapshots signing keys into an immutable `StaticConfigurationManager` at startup, wired by `OpenIddictValidationServerIntegrationConfiguration`. Tripping the change-token does not refresh it: the spike observed **both the server and the validation change-tokens firing** while `RequestRefresh()` stayed a no-op on the static manager, so a token signed with a new key fails self-validation with `ID2090` until restart. Scope is narrow: signing/issuance rotation is still no-restart (proven), and a remote resource server (`AddValidation` + issuer + JWKS) refreshes normally through a non-static configuration manager; only in-process `UseLocalServer` self-validation was frozen. **Proven fix (test T3c)**: replace the static manager with a custom non-static `IConfigurationManager<OpenIddictConfiguration>` that reads the live key store (installed via `IPostConfigureOptions<OpenIddictValidationOptions>`, setting `Configuration = null` and `ConfigurationManager = <dynamic>`), so a token signed with a new key self-validates immediately with no restart. The manager returns a key-**set** (the active signing key plus all validation keys, including retired ones), so **test T3d** confirmed that both the old and the new token validate during the overlap window.
* Perf: `CurrentValue` is read several times per request, so materialized credentials are cached rather than recreated (no `RSA.Create()` per call).

### Consequences

* Good, because keys rotate with no downtime and no restart, cloud-agnostically, comparable to the automatic key management of mature commercial servers.
* Bad, because it relies on a seam that the OpenIddict maintainer endorses (issue #1434) but that is not in the official OpenIddict documentation, so it is fragile across OpenIddict minor upgrades; this mandates an "options contract regression test" (test 9.K6) on every bump (7.5 → 7.6 → 8.0) that fails the build if the contract breaks.
* This decision depends on ADR-0005 (separate encryption-credential lifecycle), ADR-0006 (DB-backed key store and DR), ADR-0007 (break-glass reuses this reload mechanism), ADR-0009 (store access), ADR-0026 (OSS-only, which the buy option would violate), and ADR-0033 (key-scope isolation, which amends `ISigningKeyStore.LoadAsync(ct)` to `LoadAsync(scope, ct)`).

### Confirmation

* OpenIddict source verified: `AttachSecurityCredentials` reads `context.Options.SigningCredentials.First()`; `AttachSigningKeys` iterates the whole list without filtering on `NotBefore`; `UseLocalServer` snapshots into a static configuration manager plus change-token; issue #1434 records the maintainer recommending a custom `IOptionsMonitor`.
* Mandatory integration tests before relying on the seam (tests 9.K3 and 9.K6): tripping the change-token makes `UseLocalServer` self-validate a token signed with the new key without a restart; reading `CurrentValue` multiple times does not create a new key; the contract regression test runs on every OpenIddict version bump.
* Spike A-2 has run (2026-07-07, verification record V19): test T3c passed (self-validation immediately, no restart) and test T3d passed (both the old and the new token validate during the overlap window). Spike A-5 (verification record V20) separately established that the issuer is per-request while keys are per-deployment.
* Residual production items (not open decisions): the configuration manager reads the deployment's single key-set, so `LoadAsync(scope, ct)` differs only **between deployments** (a separate Silo, or the deferred Option A co-host of ADR-0033) and never per request, which is why per-request per-tenant key scoping is not applicable to v1; break-glass must remove a revoked key from the set rather than only add (ADR-0007); and the TTL/`Lazy` cache must replace rebuilding the configuration on every call (perf at 10k concurrent users).
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

### The #1434 seam: framework monitor, custom `IConfigureOptions`, `ISigningKeyStore`, TTL cache, and change-token (chosen)

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
* **Ratify-pending (added 2026-07-26).** The 90/14/14 figures are this ADR's engineering decision and are what the build implements. Recording them as the **formal cryptoperiod**, the information-security-management artifact that states the sign, encrypt, and retention periods as policy with a named owner, is a separate pre-GA sign-off held by Security with Ops. It was found asserted by this repository's key-management design and by the architecture layer's sign-off list while neither this ADR nor the Pre-GA Ratification Checklist carried it, so it is recorded here and mirrored there. Ratifying a longer period than 90 days is possible and would still sit well inside the NIST ceiling above; ratifying a shorter one costs nothing operationally, because rotation needs no restart, which is the point of this decision.
* Related decisions: ADR-0005 (encryption-credential lifecycle, whose periods the same sign-off covers), ADR-0006 (DB-backed key store and DR), ADR-0007 (break-glass reload), ADR-0009 (store access), ADR-0026 (OSS-only dependency/license policy), ADR-0033 (key-scope isolation and the scope-aware `LoadAsync`).
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25 to restore the spike and verification record identifiers (A-2/V19 with tests T3c and T3d, A-5/V20), the test labels 9.K3 and 9.K6, the `OpenIddictValidationServerIntegrationConfiguration` type that performs the snapshot, the precise negative finding that both change-tokens fire while the static manager ignores them, and the rule that key material never leaves the store. References to a specific commercial identity server, a commercial key-rotation component, and a named maintainer stay generalized; OpenIddict and its public issue numbers are named as the project's own dependency, per ADR-0026 section E.
