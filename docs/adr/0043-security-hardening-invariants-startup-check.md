---
status: "accepted"
date: 2026-07-05
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: OpenIddict 7.5.0 server-options surface (source-verified); RFC 9700 (OAuth 2.0 Security BCP); OWASP ASVS V3 (session management)
informed: all contributors, via this repository
---

# Enforce security hardening invariants with a fail-fast startup self-check

## Context and Problem Statement

Nami's security rests on a set of configuration choices that must all hold together: PKCE mandatory for public clients, implicit flow off, rolling refresh with reuse detection on, asymmetric-only signing, and so on. Each of these is decided somewhere, but a configuration that quietly drifts (a symmetric signing key added, `plain` PKCE re-enabled, implicit turned back on, a weakened cookie) would still start and serve traffic while being materially less secure. Prose in an ADR does not stop drift; nothing was asserting these invariants at runtime.

Two of the needed hardening parameters also had no ADR home at all: removing the PKCE `plain` method (OpenIddict defaults to enabling and advertising it), and pinning the JWE content-encryption algorithm while banning the weak RSA1_5 key-management algorithm. How should Nami make its security posture executable, so a drift cannot ship silently, and where are these previously-unrecorded parameters fixed?

## Decision Drivers

* A security-weakening misconfiguration must not be able to reach production silently.
* Invariants should be machine-checked, not only documented, so they cannot rot.
* The two unrecorded hardening parameters (S256-only PKCE, JWE `A256CBC-HS512` with RSA1_5 banned) need a durable home.
* The check reads OpenIddict option internals, which are version-sensitive and must be pinned and re-verified per bump (ADR-0021).

## Considered Options

* Rely on correct configuration and document the invariants
* Check invariants in tests only
* A fail-fast startup self-check that refuses to serve traffic on any drift, plus recording the previously-unrecorded hardening parameters

## Decision Outcome

Chosen option: "A fail-fast startup self-check". At startup the application runs `AssertSecureInvariants`, which throws a `SecurityInvariantException` and prevents the app from serving traffic if any invariant is violated. This is the last line of defense against configuration drift. The enforced invariants are:

| Invariant | Assertion | Home |
|---|---|---|
| pkce-mandatory-public | `RequireProofKeyForCodeExchange` is on | protocol baseline |
| no-implicit | the implicit/hybrid-implicit grant is off | protocol baseline (implicit is deprecated) |
| rolling-refresh-on | rolling refresh and reuse detection are on | enforces ADR-0004 |
| no-symmetric-signing-key | no `HS*` key; signing is asymmetric only (RS/ES) | enforces ADR-0005 |
| pkce-no-plain | `CodeChallengeMethods` excludes `plain` (S256 only) | **recorded here** (RFC 9700; OpenIddict defaults to enabling+advertising `plain`) |
| jwe-enc-cbc | encryption credential content-encoding is `A256CBC-HS512`; RSA1_5 key-management is banned | **recorded here** (only `A256CBC-HS512` is reachable via OpenIddict's standard API, source-verified; RSA1_5 is Bleichenbacher-weak) |
| core-cookie-attributes | the core SSO/session and correlation/nonce cookies carry `Secure`, `HttpOnly`, a pinned `SameSite`, and a `__Host-`/`__Secure-` prefix, reconciled with `response_mode=form_post` so `SameSite` does not block the POST-back | **recorded here** (backstop against cookie-weakening drift) |
| no-degraded-mode-in-prod | OpenIddict degraded mode is forbidden in token-issuing (Staging/Production) environments; the guard fails fast and emits a security event | **recorded here** |

The invariants split into two kinds. Some are the executable enforcement of a decision owned elsewhere (PKCE mandatory, no implicit, rolling refresh under ADR-0004, asymmetric-only signing under ADR-0005). The rest are hardening parameters that had no prior ADR home and are fixed by this ADR: S256-only PKCE, the JWE `A256CBC-HS512`/no-RSA1_5 pinning, the cookie-attribute set, and the no-degraded-mode-in-production guard. A test asserts that the self-check runs at startup and fails fast when any invariant is violated.

### Consequences

* Good, because a configuration that weakens security cannot silently reach production: the app refuses to serve rather than serving in a degraded posture.
* Good, because the invariants become executable rather than prose, so they cannot quietly rot, and the previously-unrecorded hardening parameters now have a durable home.
* Bad, because the check reads OpenIddict option members whose names/shape are version-sensitive, so it must be pinned and re-verified on each bump (ADR-0021); a broken assertion would itself block startup.
* Bad, because a legitimate future configuration change must deliberately update the invariant set, which is friction (by design).
* Neutral, because several invariants restate decisions owned by other ADRs; this ADR is the enforcement mechanism plus the four parameters it is the first to record.

### Confirmation

* Test 9.6e (xUnit + `WebApplicationFactory`) asserts the self-check runs at startup and fails fast on each drift (symmetric key added, `plain` re-enabled, implicit on, rolling refresh off).
* Test 9.6h (OWASP ASVS Level 2 V3) asserts the core cookie carries `Secure` + `HttpOnly` + pinned `SameSite` + the `__Host-`/`__Secure-` prefix, does not break the `form_post` POST-back, and is reissued after primary authentication.
* Verify-before-build: confirm the `OpenIddictServerOptions` member names the check reads on the pinned version, tracked under ADR-0021.

## Pros and Cons of the Options

### Rely on correct configuration and document the invariants

* Good, because it needs no code.
* Bad, because documentation does not stop drift; a weakened config would start and serve traffic with no signal.

### Check invariants in tests only

* Good, because it catches drift in CI for configurations the tests exercise.
* Bad, because it does not protect a production instance whose runtime configuration differs from what the tests ran, which is exactly where drift bites.

### Fail-fast startup self-check (chosen)

* Good, because the running instance itself refuses to serve when an invariant is violated, protecting every environment, not just CI.
* Bad, because it depends on version-sensitive option internals and adds deliberate friction to legitimate config changes.

## More Information

* The invariant set and the `AssertSecureInvariants` mechanism are recorded in the testing/deployment design (doc 09 §1.1, task 9.6e; the cookie invariant and its ASVS test are task 9.6h, from the 2026-07-05 review). The `pkce-no-plain` and `jwe-enc-cbc` invariants were fixed in the same review (R2 #2 and #3).
* Related decisions: ADR-0003 (server-side sessions, whose cookies the cookie invariant hardens), ADR-0004 (refresh posture, enforced by the rolling-refresh invariant), ADR-0005 (encryption credential lifecycle and asymmetric signing, enforced by the no-symmetric-key and JWE invariants), ADR-0014 (the sender-constrained and advanced-protocol scope this posture sits within), ADR-0021 (the seam catalogue and contract-regression that pin the version-sensitive option members this check reads), and ADR-0062 (the OWASP ASVS baseline this invariant set and its ASVS-tagged tests roll up to).
* Authored in this repository in 2026-07 to record the settled hardening-invariant decisions as an ADR; standards and libraries (RFC 9700, OWASP ASVS, OpenIddict) are named factually for identification only, and no commercial competitor is named.
