---
status: "accepted"
stack-record: true
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
| hsts-enabled-outside-dev | the HSTS middleware is registered and `max-age` is at least the product default, outside Development | enforces ADR-0076 |
| tls-floor | where the application terminates TLS itself, no explicitly configured protocol below TLS 1.2 is permitted | enforces ADR-0076 |
| transport-security-required | OpenIddict's `DisableTransportSecurityRequirement` is off outside Development | enforces ADR-0076 |
| no-app-only-admin | no client-credentials client is registered with the `admin-api` scope | enforces ADR-0020, **added 2026-08-01** (see the note below on why it needs its own async assertion) |
| client-permissions-enforced | none of OpenIddict's six `Ignore*Permissions` switches is set, so endpoint, grant-type, response-type, scope, resource, and audience permissions all stay enforced | enforces ADR-0001 and ADR-0035, **added 2026-08-01** |

The invariants split into two kinds. Some are the executable enforcement of a decision owned elsewhere (PKCE mandatory, no implicit, rolling refresh under ADR-0004, asymmetric-only signing under ADR-0005, the three transport rows under ADR-0076, the admin-scope row under ADR-0020, and the permission-switch row under ADR-0001 and ADR-0035). The rest are hardening parameters that had no prior ADR home and are fixed by this ADR: S256-only PKCE, the JWE `A256CBC-HS512`/no-RSA1_5 pinning, the cookie-attribute set, and the no-degraded-mode-in-production guard. A test asserts that the self-check runs at startup and fails fast when any invariant is violated.

**`no-app-only-admin` added 2026-08-01, and it is the clearest case yet of why this ADR exists.** ADR-0020 names "no client-credentials client is granted the `admin-api` scope" as the **primary** control protecting the Admin API, with the `RequireActor` policy as the runtime backstop. Three documents stated that rule and **nothing asserted it**, which is exactly the invariant class that drifts the first time somebody adds a scope to a machine client. It is also the reason `RequireActor` alone cannot carry the load: as ADR-0020 originally worded it, the policy rejected a token lacking `sub`, and no such token exists (see that ADR's amendment). Unlike every row above, this one reads the **application store** (`IOpenIddictApplicationManager`) rather than `OpenIddictServerOptions`, so it does not fit the synchronous `AssertSecureInvariants(o)` signature and needs its own async assertion at startup. That signature mismatch is the likeliest reason it was never written, and it is recorded here so the next person does not rediscover it as a blocker.

**`client-permissions-enforced` added 2026-08-01, and the switch it guards has two writers rather than one.** OpenIddict exposes six per-client permission opt-outs on the server options, `IgnoreAudiencePermissions`, `IgnoreEndpointPermissions`, `IgnoreGrantTypePermissions`, `IgnoreResourcePermissions`, `IgnoreResponseTypePermissions`, and `IgnoreScopePermissions`, each a `bool` with no initializer and each carrying the upstream remark "Setting this property to `true` is NOT recommended" (read at the pinned version in the design corpus's checked-in upstream source, `OpenIddictServerOptions.cs:603-643`). Two of the six carry decided Nami rules: `IgnoreScopePermissions` turns off `ValidateScopePermissions`, which is the engine handler that enforces ADR-0001's "per-tenant differences are expressed as scope allowlists on the client grant", and `IgnoreGrantTypePermissions` turns off the per-client grant check that ADR-0035 relies on for "limited `grant_types` ... `client_credentials` only with operator approval". A single fluent call inverts either one (`OpenIddictServerBuilder.cs:1618-1619` for the scope case) and until this row nothing forbade it in any environment.

**The second writer is why this row is not redundant with `no-degraded-mode-in-prod`.** The engine's own `PostConfigure` sets four of the six to `true` whenever `EnableDegradedMode` is on (`OpenIddictServerConfiguration.cs:41-46`), so the opt-out is reachable as a side effect of a different switch and not only by naming it. The degraded-mode row above blocks that path in token-issuing environments, which leaves **Development**, where degraded mode is permitted and would silently disable the per-client permission checks: that is exactly where a per-tenant scope allowlist is tested, and it would test as passing while enforcing nothing. Asserting the switches directly covers both writers; asserting only that nobody called the builder method would cover neither in Development.

**All six are asserted, and only two of them have a named owning decision.** The other four are gathered in because they are one option family with one upstream recommendation against setting any of them, and splitting the assertion would leave four holes for no benefit. That breadth is deliberate and stated here rather than implied, so a later reader does not go looking for a decided Nami rule about audience or response-type permissions and conclude one was lost: there is none, and the row does not claim otherwise.

**Transport rows added 2026-07-26.** ADR-0073 recorded that this check covered PKCE, signing, JWE, cookies, and degraded mode but **not transport**, and left the gap open rather than filling it silently. ADR-0076 closed it, and its three invariants are enforced here so the application still has one place where it refuses to serve rather than two.

### Consequences

* Good, because a configuration that weakens security cannot silently reach production: the app refuses to serve rather than serving in a degraded posture.
* Good, because the invariants become executable rather than prose, so they cannot quietly rot, and the previously-unrecorded hardening parameters now have a durable home.
* Bad, because the check reads OpenIddict option members whose names/shape are version-sensitive, so it must be pinned and re-verified on each bump (ADR-0021); a broken assertion would itself block startup.
* Bad, because a legitimate future configuration change must deliberately update the invariant set, which is friction (by design).
* Neutral, because several invariants restate decisions owned by other ADRs; this ADR is the enforcement mechanism plus the four parameters it is the first to record.

### Confirmation

* A test (xUnit + `WebApplicationFactory`) asserts the self-check runs at startup and fails fast on each drift (symmetric key added, `plain` re-enabled, implicit on, rolling refresh off).
* A test (OWASP ASVS Level 2 V3) asserts the core cookie carries `Secure` + `HttpOnly` + pinned `SameSite` + the `__Host-`/`__Secure-` prefix, does not break the `form_post` POST-back, and is reissued after primary authentication.
* A test asserts that a client presenting a scope outside its own grant is rejected with the permission handler's `invalid_request`, and that setting `IgnoreScopePermissions` fails startup rather than widening the allowlist. The negative case belongs with the authorization suite ([07](../design/07-authorization.md) section 9), because what it protects is a tenant-isolation property, not an options detail.
* Verify-before-build: confirm the `OpenIddictServerOptions` member names the check reads on the pinned version, tracked under ADR-0021. The six permission switches and the degraded-mode `PostConfigure` that writes four of them are part of that surface, so both are re-read per bump under the seam catalogue ([22](../design/22-openiddict-seam-catalogue.md)).

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

* The invariant set and the `AssertSecureInvariants` mechanism are recorded in the [testing design](../design/20-testing.md) section 5.2. They came from the **design corpus's** testing/observability/deployment document, whose startup secure-invariant self-check section carries both the mechanism and the cookie invariant added by its 2026-07-05 review; that document's own section and task numbers are the corpus's and resolve to nothing here. The `pkce-no-plain` and `jwe-enc-cbc` invariants were fixed in the same review (R2 #2 and #3).
* Related decisions: ADR-0001 (the global scope catalogue and the per-tenant scope allowlist on the client grant, which the permission-switch invariant keeps enforceable), ADR-0035 (the self-service registration guardrails, whose limited `grant_types` depend on the same switch family), ADR-0020 (the primary control the `no-app-only-admin` row asserts), ADR-0003 (server-side sessions, whose cookies the cookie invariant hardens), ADR-0004 (refresh posture, enforced by the rolling-refresh invariant), ADR-0005 (encryption credential lifecycle and asymmetric signing, enforced by the no-symmetric-key and JWE invariants), ADR-0014 (the sender-constrained and advanced-protocol scope this posture sits within), ADR-0021 (the seam catalogue and contract-regression that pin the version-sensitive option members this check reads), and ADR-0062 (the OWASP ASVS baseline this invariant set and its ASVS-tagged tests roll up to).
* Authored in this repository in 2026-07 to record the settled hardening-invariant decisions as an ADR; standards and libraries (RFC 9700, OWASP ASVS, OpenIddict) are named factually for identification only, and no commercial competitor is named.
