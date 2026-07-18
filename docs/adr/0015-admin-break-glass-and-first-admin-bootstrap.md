---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security and DPO (custody, rotation cadence, approvers, and alert recipients are ISMS control DP.01 and await their ratification); precedent from Microsoft Entra emergency access, AWS SEC03-BP03, Keycloak bootstrap-admin, and NIST SP 800-53 AC-2
informed: all contributors, via this repository
---

# Provide an OIDC-independent admin break-glass path and a one-time first-admin bootstrap

## Context and Problem Statement

The admin portal is an OIDC client of the very identity provider it manages (dogfooding). If the IdP is down, misconfigured, missing a signing key, or on a first deploy with an empty database, OIDC login is impossible, so an administrator cannot get in to fix it — a chicken-and-egg problem. This is distinct from ADR-0007, which is break-glass for a *compromised key*; this is break-glass for *admin access* when the IdP itself is unavailable. It is a SEV-2 hidden risk and an ISMS control, not merely code. How should an administrator recover access when the normal login path is dead, and how is the very first admin created on an empty database?

## Decision Drivers

* Admin access must be recoverable even when the IdP and its keys are down.
* No standing default credential may exist (that would be a backdoor).
* Blast radius must be bounded and every use fully auditable.
* This is an ISMS control (DP.01), so custody, rotation, and approvers must be governed, not just implemented.

## Considered Options

* Emergency path: bypass OIDC via a separate cookie scheme, or try to repair OIDC in place.
* Sealed credential: one sealed account, or two sealed accounts with dual-control unseal.
* First-admin seed: a one-time setup token with a forced change, or a seed password from the secret store.

## Decision Outcome

Chosen: a separate emergency cookie scheme, two sealed accounts with dual-control unseal, and a one-time-token first-admin seeder.

Fixed parameters of the decision:

* **Emergency local login is a separate `"BreakGlass"` cookie scheme** (`AddCookie`, `__Host-bg`, path `/breakglass`, a 15-minute hard cap), with the session issued via `SignInAsync`, **independent of the OIDC token pipeline**. The cookie is protected by Data Protection, not the OIDC signing key, so it works even with no key or JWKS — exactly the situation that needs rescuing. Its only dependency is the Data Protection keyring, kept minimal.
* **Gating (so it is not a backdoor)**: a feature flag `EmergencyAccess:Enabled` defaulting OFF, an IP allow-list (returning 404 to hide the endpoint), binding to an internal admin-network listener, a policy that adds the `BreakGlass` scheme plus a role, and least-privilege repair-only, time-boxed access.
* **Sealed credential**: two break-glass accounts (not ordinary user rows), whose hashes are verified with `PasswordHasher<T>` from the secret store; **dual-control unseal** (a password and a second factor held by two different custodians); rotated after every use; never expired by disuse. Microsoft Entra recommends FIDO2 / certificate-based, phishing-resistant authentication for emergency accounts, which is stronger than a password hash, so the password hash is the baseline and an upgrade to FIDO2/CBA is to be considered at ratification.
* **Audit-before-action**: `await audit.RecordSuccessAndAlert()` (Severity-0) runs **before** `SignInAsync`; a sink failure is fail-closed; every attempt, including failures, is recorded; a post-mortem and a quarterly drill follow.
* **First-admin seeder (empty database)**: idempotent (`FindByNameAsync`/`FindByClientIdAsync` plus an advisory lock, running once even across many nodes); it issues a one-time setup token (`GeneratePasswordResetTokenAsync`) with a temporary random password that is never logged, forces a change, enrolls MFA, and flags the account "temporary" until a real admin exists.
* **Turnkey first-admin for the reference image**: for the zero-code reference host (ADR-0027), a `Bootstrap__Admin*` environment configuration (email/password, or a client id/secret for automation) is applied **once** at first start when no admin exists, forcing a password change, auditing `admin.bootstrap`, and failing fast in Production on a weak or absent value. Apply-once plus forced-change means this is not a standing default credential (the risk the setup-token path above also avoids); it is the container-friendly variant of first-admin seeding.

### Consequences

* Good, because access is recoverable even when the IdP or its keys are broken, no default credential exists, the blast radius is bounded, and every use is auditable.
* Bad, because a break-glass path is a controlled backdoor, so custody, rotation, and approvers must follow ISMS, and the extra surface needs layered gating.
* This decision depends on ADR-0008 (the audit-before-action sink), ADR-0003 (session), and ADR-0012 (a minimal Data Protection keyring for the cookie). It deliberately does **not** depend on ADR-0011 (the token pipeline).
* A behavior to verify: any path that issues a token when a key is "announced but not yet promoted"; break-glass must not depend on it.

### Confirmation

* Verify-before-build: the break-glass cookie login works with an empty store and no signing key; the audit-before-`SignInAsync` ordering holds; and the gating returns 404 when disabled or off-allow-list.
* A mandatory validation drill runs every 90 days and after each staff change, and confirms that break-glass can be flipped on when the IdP is down — the tension being that a default-OFF feature flag must not render it unavailable exactly when it is needed, so the enable mechanism is independent of the IdP.
* Precedent verified: Microsoft Entra emergency access (at least two cloud-only, non-federated accounts, excluded from Conditional Access with a different strong factor, split sealed storage, a Severity-0 alert per use, no expiry, a 90-day review); AWS SEC03-BP03 (alternate direct access, split-custody dual-control, rotate-after-use, and the explicit framing that break-glass is a backdoor); Keycloak's temporary bootstrap admin (banner, removed after a permanent admin); and NIST SP 800-53 AC-2/AC-2(2) (an emergency account that bypasses authorization, short-term, auto-disabled). ASP.NET Core 10 mechanics verified: a named cookie scheme, a cookie protected by Data Protection, `PasswordHasher` PBKDF2 at 100k iterations, `AuthenticationSchemes.Add` on the policy, and API endpoints that return 401 rather than redirecting.

## Pros and Cons of the Options

### Emergency path

* **Separate cookie scheme (chosen)** — good, because it does not depend on the OIDC pipeline or signing keys, so it survives the exact failure it exists for; bad, because it is a second authentication surface that must be gated carefully.
* **Repair OIDC in place** — good, because there is no second path to secure; bad, because it cannot help when there is no signing key or the database is empty, which is precisely the failure mode.

### Sealed credential

* **Two sealed accounts with dual-control unseal (chosen)** — good, because no single custodian can use it and there is redundancy; bad, because it needs two custodians and a rotation ceremony.
* **One sealed account** — good, because it is simpler; bad, because it is a single point of both failure and compromise.

### First-admin seed

* **One-time setup token with forced change (chosen)** — good, because no lasting seeded secret exists and the temporary account self-retires; bad, because the operator must complete setup promptly.
* **Seed password from the secret store** — good, because it is straightforward; bad, because a standing seeded password is a default-credential risk.

## More Information

* Original decision: 2026-06-28. Custody, rotation cadence, the Severity-0 alert recipients, whether a second approver is required to unseal, the network allow-list, and the drill cadence are ISMS control DP.01 and await Security/DPO ratification.
* This is a different concern from ADR-0007: that ADR is break-glass for a compromised key, whereas this one is break-glass for admin access when the IdP is unavailable.
* Related decisions: ADR-0003 (session), ADR-0007 (key-compromise break-glass, distinct), ADR-0008 (audit-before-action sink), ADR-0011 (token pipeline, deliberately not a dependency), ADR-0012 (Data Protection keyring bootstrap).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A non-substantive competitor cross-check note was dropped; Microsoft Entra, AWS, Keycloak, and NIST are retained as neutral emergency-access precedent.
