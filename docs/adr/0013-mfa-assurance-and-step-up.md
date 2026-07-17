---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security and DPO (the AAL threshold per dangerous capability and the per-scope required-acr list for sensitive data await their ratification); verification against RFC 8176, OIDC Core, RFC 9470, NIST SP 800-63B, ASP.NET Core Identity (.NET 10), and OpenIddict constants
informed: all contributors, via this repository
---

# Make MFA the producer of acr/amr/auth_time and enforce step-up assurance

## Context and Problem Statement

MFA is where the `acr`, `amr`, and `auth_time` claims are **produced** — the same claims that the session, single-logout, and authorization designs **consume**. Previously the consumer side had a spec but the producer side did not, so the binding "TOTP verified, therefore emit `acr`/`amr`/`auth_time`" was never defined. An earlier design even referenced a step-up ADR that was never written. OpenIddict does not implement MFA (it leaves it to the host UI), so Nami builds MFA on ASP.NET Core Identity. How should MFA produce assurance claims, and how should step-up be enforced?

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
* **`amr` (RFC 8176)**: password plus TOTP produces `["pwd","otp","mfa"]` (an array; a historical fact of the sign-in). It is stamped at sign-in via `SignInWithClaimsAsync`, with `AuthenticationProperties.IssuedUtc` as `auth_time`. Because `amr` can be absent on a silent refresh, resource servers gate on `acr` plus `auth_time` and treat `amr` as informational.
* **`acr`**: URN-style `urn:nami:aal1|aal2|aal3`, **recomputed per token-request** from `amr` plus session age (NIST AAL2 freshness is 12h/30min, so an aged session drops out of aal2 even when `amr` still shows MFA). The effective freshness window is capped by the 8-hour absolute session ceiling (ADR-0003), so the 12-hour aal2 branch is never actually reached (effective aal2 window is at most 8h). Levels: aal1 = password, aal2 = password plus TOTP/passkey, aal3 = hardware plus a second factor.
* This `acr` recompute is **bespoke on top of NIST 800-63B**: mainstream commercial identity servers do not recompute the AAL tier — they use `max_age`/a max-age requirement (the relying party or API decides freshness per request) and compare `acr_values` against the session claim. Nami's per-request evaluation matches that industry approach; the AAL-tier mapping and automatic downgrade are Nami's own design, more rigorous but not a feature copied from any product.
* **Producer (OpenIddict, verified constants)**: `SetClaims(Claims.AuthenticationMethodReference, [...])` (array), `SetClaim(Claims.AuthenticationContextReference, ComputeAcr(...))`, and `SetClaim(Claims.AuthenticationTime, ...)`. Destinations: `amr` goes to the id_token; `acr` and `auth_time` go to both the id_token and the access_token, so resource servers can implement RFC 9470.
* **Step-up (RFC 9470)**: an API returns `401 insufficient_user_authentication` with `acr_values`/`max_age`; the authorize endpoint checks `GetAcrValues()`/`MaxAge`/`prompt` against the session and re-challenges; `prompt=none` yields a `login_required` forbid; the `sid` rotates on step-up (ADR-0003).
* **Three-tier enforcement**: `required_acr = max(per-client DefaultAcr, per-scope RequiredAcr, runtime step-up)`. A sensitive scope forces aal2 even when the client defaults to aal1.

### Consequences

* Good, because it closes the producer/consumer gap, provides standards-based step-up and assurance, and allows per-scope elevation for sensitive data.
* Bad, because recomputing `acr` needs correct freshness-window logic. (A related serialization subtlety is resolved: `auth_time` is emitted as a JSON number via the `long` overload, and because OpenIddict does not auto-emit `auth_time`, only `sub`, it must be set explicitly.)
* This decision depends on ADR-0003 (`sid` rotation, session `IssuedUtc` for `max_age`, and the 8-hour ceiling that caps freshness), ADR-0010 (step-up for dangerous capabilities), and ADR-0028 (passkey/WebAuthn as an `amr` producer).

### Confirmation

* Standards verified: RFC 8176 (`amr` values and combining), OIDC Core (`acr` as a single string, `auth_time`, `max_age=0` equivalent to `prompt=login`), RFC 9470 (`insufficient_user_authentication` with `acr_values`/`max_age`), and NIST SP 800-63B (AAL2 freshness).
* ASP.NET Core Identity (.NET 10) mechanics verified: authenticator key get/reset, `TwoFactorAuthenticatorSignInAsync`, `GenerateNewTwoFactorRecoveryCodesAsync` (default 10), and `SignInWithClaimsAsync`.
* OpenIddict constants verified (`AuthenticationMethodReference`, `AuthenticationContextReference`, `AuthenticationTime`); `auth_time` number coercion and `amr` array serialization are resolved (the `long` overload yields a JSON number, and multiple string claims serialize to a JSON array).
* Verify-before-build: the step-up re-challenge is still to be tested end-to-end at build time.

## Pros and Cons of the Options

### MFA methods

* **TOTP plus recovery, with WebAuthn/passkey in v1 (chosen)** — good, because it is a strong, phishing-resistant baseline native to .NET 10; the SMS/email OTP path is deferred to roadmap because it is the weakest factor.

### `acr` storage

* **Recompute per token-request (chosen)** — good, because assurance reflects current session freshness and can auto-downgrade; bad, because it needs correct freshness-window logic.
* **Static stored `acr`** — good, because it is trivial; bad, because it cannot express freshness decay and would report aal2 for a stale session.

### Enforcement

* **Three-tier `max(client, scope, runtime)` (chosen)** — good, because a sensitive scope can force elevation regardless of the client default, and runtime step-up still applies; bad, because it is more logic than a single per-client flag.
* **Always-MFA** — good, because it is simple; bad, because it is a poor user experience for low-risk clients and is not how mainstream servers behave.
* **Per-client only** — good, because it is simple; bad, because it cannot elevate for a sensitive scope or a runtime step-up.

## More Information

* Original decision: 2026-06-28. This ADR replaces a dangling reference to a step-up ADR that was never written, and it is the producer for the assurance claims that the session (ADR-0003), single-logout, and authorization designs consume.
* Enforcement precedent: mainstream identity servers, including Keycloak and Auth0, all drive assurance enforcement by policy; none hardcodes "always".
* The `acr` freshness numbers should be confirmed against the mandated revision of NIST SP 800-63B.
* Related decisions: ADR-0003 (session `sid` rotation and absolute ceiling), ADR-0010 (step-up for dangerous capabilities), ADR-0028 (user management, including passkey/WebAuthn).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. References to a specific commercial identity server were generalized (Keycloak and Auth0 are retained as neutral enforcement-pattern precedent); the product-name placeholder in the `acr` URN was set to Nami.
