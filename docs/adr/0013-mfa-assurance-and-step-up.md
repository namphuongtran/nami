---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security and DPO (the AAL threshold per dangerous capability and the per-scope required-acr list for sensitive data await their ratification); verification against RFC 8176, OIDC Core, RFC 9470, NIST SP 800-63B, ASP.NET Core Identity (.NET 10), and OpenIddict constants
informed: all contributors, via this repository
---

# Make MFA the producer of acr/amr/auth_time and enforce step-up assurance

## Context and Problem Statement

MFA is where the `acr`, `amr`, and `auth_time` claims are **produced**, the same claims that the session, single-logout, and authorization designs **consume**. Previously the consumer side had a spec but the producer side did not, so the binding "TOTP verified, therefore emit `acr`/`amr`/`auth_time`" was never defined. An earlier design even referenced a step-up ADR that was never written. OpenIddict does not implement MFA (it leaves it to the host UI), so Nami builds MFA on ASP.NET Core Identity. How should MFA produce assurance claims, and how should step-up be enforced?

## Decision Drivers

* Close the producer/consumer gap: the assurance claims must have a defined producer.
* Standards alignment: RFC 8176 (`amr`), OIDC Core (`acr`, `auth_time`, `max_age`), RFC 9470 (step-up), and NIST SP 800-63B (AAL freshness).
* Sensitive scopes (for example patient-data or billing) must be able to force a higher assurance level than the client's default.
* Assurance must reflect current freshness, so `acr` is recomputed rather than statically stored.

## Considered Options

* MFA methods: TOTP plus recovery codes as the baseline, optionally adding SMS/email OTP, optionally adding WebAuthn/passkey.
* `acr`: statically stored versus recomputed per token-request from `amr` plus session age.
* Enforcement: always-MFA, per-client, or a three-tier `max(client, scope, runtime)`.

## Decision Outcome

Chosen: TOTP plus recovery codes as the baseline with WebAuthn/passkey in v1; `acr` **recomputed per token-request**; and **three-tier `max(client, scope, runtime)`** enforcement.

Fixed parameters of the decision:

* **Methods**: a TOTP authenticator plus 10 recovery codes is the production baseline; WebAuthn/passkey (`amr` `hwk`/`swk`) ships in v1 (native to .NET 10, per ADR-0028, as a primary factor with enroll/list/remove UI); SMS/email OTP (`amr` `sms`) is roadmap.
* **`amr` (RFC 8176)**: password plus TOTP produces `["pwd","otp","mfa"]` (an array; a historical fact of the sign-in). It is stamped at sign-in via `SignInWithClaimsAsync`, and **`auth_time` is stamped as an immutable claim in the same call** (see the amendment in More Information: it is deliberately not `AuthenticationProperties.IssuedUtc`). Because `amr` can be absent on a silent refresh, resource servers gate on `acr` plus `auth_time` and treat `amr` as informational.
* **`acr`**: URN-style `urn:nami.identity:aal1|aal2|aal3` (the lowercase product URN form, ADR-0065), **recomputed per token-request** from `amr` plus session age (NIST AAL2 freshness is 12h/30min, so an aged session drops out of aal2 even when `amr` still shows MFA). The effective freshness window is capped by the 8-hour absolute session ceiling (ADR-0003), so the 12-hour aal2 branch is never actually reached (effective aal2 window is at most 8h). Levels: aal1 = password, aal2 = password plus TOTP, **or a passkey whose assertion carried user verification** (see the amendment below: a passkey without user verification is one factor and stays aal1), aal3 = hardware plus a second factor.
* This `acr` recompute is **bespoke on top of NIST 800-63B**: mainstream commercial identity servers do not recompute the AAL tier: they use `max_age`/a max-age requirement (the relying party or API decides freshness per request) and compare `acr_values` against the session claim. Nami's per-request evaluation matches that industry approach; the AAL-tier mapping and automatic downgrade are Nami's own design, more rigorous but not a feature copied from any product.
* **Producer (OpenIddict, verified constants)**: `SetClaims(Claims.AuthenticationMethodReference, [...])` (array), `SetClaim(Claims.AuthenticationContextReference, ComputeAcr(...))`, and `SetClaim(Claims.AuthenticationTime, ...)`. Destinations: `amr` goes to the id_token; `acr` and `auth_time` go to both the id_token and the access_token, so resource servers can implement RFC 9470.
* **Step-up (RFC 9470)**: an API returns `401 insufficient_user_authentication` with `acr_values`/`max_age`; the authorize endpoint checks `GetAcrValues()`/`MaxAge`/`prompt` against the session and re-challenges; `prompt=none` yields a `login_required` forbid; the `sid` rotates on step-up (ADR-0003).
* **Three-tier enforcement**: `required_acr = max(per-client DefaultAcr, per-scope RequiredAcr, runtime step-up)`. A sensitive scope forces aal2 even when the client defaults to aal1.

### Consequences

* Good, because it closes the producer/consumer gap, provides standards-based step-up and assurance, and allows per-scope elevation for sensitive data.
* Bad, because recomputing `acr` needs correct freshness-window logic. (A related serialization subtlety is resolved: `auth_time` is emitted as a JSON number via the `long` overload, and because OpenIddict does not auto-emit `auth_time`, only `sub`, it must be set explicitly.)
* This decision depends on ADR-0003 (`sid` rotation, the sliding inactivity window that makes an immutable `auth_time` claim necessary, and the 8-hour ceiling that caps freshness), ADR-0010 (step-up for dangerous capabilities), and ADR-0028 (passkey/WebAuthn as an `amr` producer). `max_age` is evaluated against the `auth_time` **claim**, never against the ticket's `IssuedUtc`.

### Confirmation

* Standards verified: RFC 8176 (`amr` values and combining), OIDC Core (`acr` as a single string, `auth_time`, `max_age=0` equivalent to `prompt=login`), RFC 9470 (`insufficient_user_authentication` with `acr_values`/`max_age`), and NIST SP 800-63B (AAL2 freshness).
* ASP.NET Core Identity (.NET 10) mechanics verified: authenticator key get/reset, `TwoFactorAuthenticatorSignInAsync`, `GenerateNewTwoFactorRecoveryCodesAsync` (default 10), and `SignInWithClaimsAsync`.
* OpenIddict constants verified (`AuthenticationMethodReference`, `AuthenticationContextReference`, `AuthenticationTime`); `auth_time` number coercion and `amr` array serialization are resolved (the `long` overload yields a JSON number, and multiple string claims serialize to a JSON array).
* Verify-before-build: the step-up re-challenge is still to be tested end-to-end at build time.

## Pros and Cons of the Options

### MFA methods

* **TOTP plus recovery, with WebAuthn/passkey in v1 (chosen)**: good, because it is a strong, phishing-resistant baseline native to .NET 10; the SMS/email OTP path is deferred to roadmap because it is the weakest factor.

### `acr` storage

* **Recompute per token-request (chosen)**: good, because assurance reflects current session freshness and can auto-downgrade; bad, because it needs correct freshness-window logic.
* **Static stored `acr`**: good, because it is trivial; bad, because it cannot express freshness decay and would report aal2 for a stale session.

### Enforcement

* **Three-tier `max(client, scope, runtime)` (chosen)**: good, because a sensitive scope can force elevation regardless of the client default, and runtime step-up still applies; bad, because it is more logic than a single per-client flag.
* **Always-MFA**: good, because it is simple; bad, because it is a poor user experience for low-risk clients and is not how mainstream servers behave.
* **Per-client only**: good, because it is simple; bad, because it cannot elevate for a sensitive scope or a runtime step-up.

## More Information

* Original decision: 2026-06-28. This ADR replaces a dangling reference to a step-up ADR that was never written, and it is the producer for the assurance claims that the session (ADR-0003), single-logout, and authorization designs consume.
* Enforcement precedent: mainstream identity servers, including Keycloak and Auth0, all drive assurance enforcement by policy; none hardcodes "always".
* The `acr` freshness numbers should be confirmed against the mandated revision of NIST SP 800-63B.
* **Amendment, 2026-08-01: a passkey maps to `aal2` only when the assertion carried user verification.** The assurance design had scored every passkey `aal2` on the ground that attestation is off in v1, and the AAL resolver read only the AAGUID, the attestation trust and the backup-eligibility flag. None of those is user verification. A cryptographic authenticator used without UV proves possession and nothing more, so the login is single-factor, and scoring it `aal2` let the per-scope `RequiredAcr` gate pass on one factor while the RFC 9470 step-up in this ADR saw `aal2` already satisfied and never challenged. .NET 10 exposes what the fix needs: `IdentityPasskeyOptions.UserVerificationRequirement` (applies to both ceremonies, default `"required"`) and `UserPasskeyInfo.IsUserVerified`, reachable from `PasskeyAssertionResult<T>.Passkey`, both read at the .NET 10.0.9 reference assemblies. The rule is to assert the flag rather than trust the ceremony parameter, since the parameter states intent and the flag states what the authenticator did. The mechanics are in the user-management design, section 5.2, including what is **not** verified: whether the framework enforces the requirement server-side.
* **Amendment, 2026-08-01: `auth_time` moved from `AuthenticationProperties.IssuedUtc` to an immutable claim, and the whole step-up mechanism depended on it.** As originally written, this ADR sourced `auth_time` from the ticket. Microsoft's shipped documentation defines `IssuedUtc` as "the time at which the **authentication ticket** was issued", and defines `SlidingExpiration` as instructing the handler "to **re-issue a new cookie with a new expiration time** any time it processes a request which is more than halfway through the expiration window" (both read at the .NET 10.0.9 reference assemblies). ADR-0003 sets a sliding 1-hour inactivity window, so on an active session the ticket is re-issued by ordinary traffic. An `auth_time` derived from it advances with no re-authentication, which makes `max_age` and `prompt=login` never fire and leaves the RFC 9470 step-up in this ADR decorative: a resource server asking for a fresher authentication is answered by a timestamp that refreshed itself. `amr` was always stamped as an immutable claim and was never affected; `auth_time` now travels the same way, in the same call.
* **Not re-verified in this repository:** that the handler assigns the refreshed instant back onto `properties.IssuedUtc` during renewal. That read was done against `dotnet/aspnetcore` in the design corpus. The fix does not rest on it, because an immutable claim is the right source either way, and the sliding-renewal test carried in the testing design settles it as a regression test: force a renewal past half of `ExpireTimeSpan`, then assert `auth_time` is unchanged while `max_age` and `prompt=login` still fire.
* Related decisions: ADR-0003 (session `sid` rotation and absolute ceiling), ADR-0010 (step-up for dangerous capabilities), ADR-0028 (user management, including passkey/WebAuthn).
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25, which corrected the `acr` URN. The 2026-07 import had substituted the organization name, producing `urn:nami:aalN`; the convention is the lowercase **product** URN form, so the correct value is `urn:nami.identity:aalN`. The same error had propagated into three design documents and is fixed with this change, and ADR-0065 now records the URN form so it cannot drift again. References to a specific commercial identity server stay generalized; Keycloak and Auth0 are named as neutral enforcement-pattern precedent.
