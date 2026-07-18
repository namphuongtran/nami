---
status: "accepted"
stack-record: true
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: primary-source research (2026-07-04) on ASP.NET Core Identity in .NET 10, including its built-in passkeys/WebAuthn
informed: all contributors, via this repository
---

# Build user management on ASP.NET Core Identity with native passkeys and a lifecycle layer, packaged as Nami.Identity.Users

## Context and Problem Statement

A commercial identity server sells a "User Management SDK" (passwords, MFA, passkeys, profile, lifecycle). Nami needs a parity answer, but OSS. The insight is that ASP.NET Core Identity **is** Microsoft's first-party user store — the commercial product also sits on top of it — so Nami builds on Identity rather than reinventing it or buying the commercial SDK. Most of the pieces are already scattered across the design (external login, email flows, MFA in ADR-0013, and admin user CRUD), so this ADR consolidates them into one unified user-management decision and fills the gaps (passkeys, profile/lifecycle, packaging).

Research on 2026-07-04 found that .NET 10 ASP.NET Core Identity has passkeys/WebAuthn **built in** (GA 2025-11-11, an LTS release). Native coverage includes password and hashing, MFA/TOTP with recovery codes, lockout, email/phone confirmation, external/social login, roles/claims, `MapIdentityApi`, and a scaffoldable Razor UI. The real gap is not the primitives, which are sufficient, but the admin console, the lifecycle, and the passkey wiring: the passkey endpoints are not auto-mapped, there is no default attestation validation, and a passkey is a primary factor rather than a second factor.

## Decision Drivers

* Parity with a commercial user-management SDK, but OSS and free.
* Do not reinvent Microsoft's first-party store or buy a commercial SDK.
* Consolidate the scattered user-management pieces into one decision.
* Fill the real gaps: passkey wiring, profile/lifecycle, and packaging.

## Considered Options

* Buy or clone a commercial user-management SDK
* Build on ASP.NET Core Identity and fill the gaps (passkey wiring, profile/lifecycle, packaging)
* Use a different third-party OSS user store

## Decision Outcome

Chosen option: "Build on ASP.NET Core Identity and fill the gaps", packaged as `Nami.Identity.Users`, because ASP.NET Core Identity is the first-party store the commercial product itself builds on, so buying or reinventing is unnecessary.

* **A. Store and primitives (native Identity, .NET 10, in scope for v1):** password (a configurable hashing policy with re-hash-on-verify; the hardening baseline is E), MFA/TOTP with recovery codes (ADR-0013), lockout, email/phone confirmation, external/social login, and roles/claims. Identity is **global** (one human is one `ApplicationUser`, ADR-0001), and tenant belonging is expressed through membership.
* **B. Passkeys/WebAuthn (native .NET 10, in scope for v1, wiring to build):** use the native passkey APIs (`SignInManager.MakePasskeyCreationOptionsAsync`/`PerformPasskeyAttestationAsync`/`MakePasskeyRequestOptionsAsync`/`PasskeySignInAsync`/`PerformPasskeyAssertionAsync`, `UserManager.AddOrUpdatePasskeyAsync`, `IdentityPasskeyOptions`, and `UserPasskeyInfo`). Nami must build: (a) mapping the endpoints, which are not auto-mapped outside the Blazor template; (b) an attestation-validation policy, since there is no native default, added where high assurance is needed; and (c) treating a passkey as a primary factor, combined with step-up/AAL (ADR-0013) for sensitive operations.
* **C. Profile and lifecycle (build thin; partly v1, the console partly post-v1):** self-service (changing email, phone, MFA, passkey, and password) through custom endpoints rather than `MapIdentityApi` (decided 2026-07-13, because `MapIdentityApi` exposes `/register`, `/login`, and similar as an attack surface that bypasses the UI flow, anti-enumeration, and the challenge layer), with UI pages Nami owns. A lifecycle state machine runs invite → active → disabled → offboarded, with disable-not-delete by default (ADR-0015) and offboarding tied to erasure (ADR-0016); every action is audited (ADR-0008). The admin surface (CRUD, lock, reset, force-logout) already exists in the Admin API/App; this ADR adds the lifecycle and audit provenance.
* **D. Packaging (ADR-0027):** package `Nami.Identity.Users` with the store, passkey wiring, MFA, a lifecycle service, and the self-service endpoints; a consumer enables it via the builder `.AddUsers(...)`. The ports (the email `IEmailDispatcher` and the audit `IAuditSink`) are swappable (ADR-0024).
* **E. Credential-hardening baseline (source-verified against NIST SP 800-63B and OWASP).** The security lever order, largest effect first, is: phishing-resistant MFA/passkeys (ADR-0013), a breached-password check, length over complexity, strong hashing, then lockout with a short security-stamp interval; complexity rules and periodic rotation are explicitly **not** the primary levers, because they push users toward weaker, guessable passwords. Concrete settings: `Password.RequiredLength` = 12; PBKDF2 `IterationCount` >= 210,000 (Argon2id via a custom `IPasswordHasher` if wanted); `User.RequireUniqueEmail` = true (one email is one identity, ADR-0001); `SecurityStampValidatorOptions.ValidationInterval` 1-2 minutes (fast logout-everywhere, matching ADR-0003); lockout-on-failure enabled (the template defaults it off) at a 5-attempt threshold; the complexity flags kept on only as a defense-in-depth backstop; and no forced periodic rotation. A breached-password check runs through a pluggable `IPasswordBreachChecker` port using the HIBP Pwned-Passwords range API (k-anonymity, so only a hash prefix leaves the process), called at set-password and change-password, fail-open on timeout or error, on in the hardened/production profile and off in development. The length, the iteration count, and the HIBP check are interim-accepted and require Security ratification at build; sending a hash prefix to an external service is a DPO/DP.01 item to ratify.

### Consequences

* Good, because user-management parity, including passkeys, is nearly free thanks to the .NET 10 native support and ASP.NET Core Identity, with no commercial SDK to buy and a first-party Microsoft store that is durable and familiar.
* Good, because it consolidates the external-login, email-flow, admin-CRUD, and MFA pieces into one coherent story.
* Bad, because the passkey wiring and the attestation policy must be built (the endpoints are not auto-mapped), and the lifecycle console is partly post-v1.
* Bad, because it depends on the ASP.NET Core Identity version (the passkey API is new in .NET 10), which calls for a contract-regression check when .NET is bumped (the ADR-0021 thinking).

### Confirmation

* .NET 10 ASP.NET Core Identity passkeys are built in (GA 2025-11-11), with the documented caveats: no default attestation validation, endpoints not auto-mapped, and a passkey being a primary factor. Identity's native coverage is documented, and because a commercial identity server has no user store of its own (it sits on Identity), building on Identity is the standard approach.
* Security/DPO follow-ups: the passkey attestation policy, PII profile retention, the credential-hardening thresholds (length and PBKDF2 iterations), and the HIBP breach-check (a DP.01 item, since a hash prefix is sent externally).

## Pros and Cons of the Options

### Buy or clone a commercial user-management SDK

* Good, because it would be turnkey.
* Bad, because the commercial option is rejected by the OSS-only policy (ADR-0026), and cloning from scratch is a large, pointless cost given Microsoft already provides the store.

### Build on ASP.NET Core Identity and fill the gaps (chosen)

* Good, because the primitives (including passkeys) come from the first-party store nearly for free, and only the wiring, lifecycle, and packaging need building.
* Bad, because the passkey wiring and attestation policy are Nami's to build and to keep aligned across .NET versions.

### A different third-party OSS user store

* Good, because it might offer a different feature set.
* Bad, because there is no reason to leave the first-party Microsoft store that the ecosystem (and the commercial product) already builds on.

## More Information

* Original decision 2026-07-04 (the shape accepted, with build-time picks still open); the self-service decision to avoid `MapIdentityApi` was made 2026-07-13; the credential-hardening baseline (E) and the HIBP breach-check were decided 2026-07-05. The lockout-DoS mitigation and the risk-triggered challenge layer that complement this baseline are recorded in ADR-0042.
* Build-time follow-ups: passkey endpoint mapping, the attestation-validation policy, and the UI; the profile schema and lifecycle state machine (invite/approval/offboard) with audit provenance; the admin-console extension; and, from Security/DPO, the passkey attestation policy and PII profile retention.
* Related decisions: ADR-0001 (global identity with per-tenant membership), ADR-0005 (claims), ADR-0008 (audit), ADR-0013 (MFA/assurance/step-up, with the passkey as an `amr` producer), ADR-0015 (disable-not-delete), ADR-0016 (offboarding tied to erasure), ADR-0024 (ports as swappable adapters), ADR-0026 (OSS-only, so no commercial SDK), and ADR-0027 (packaging and the `.AddUsers` builder).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial identity server and its commercial user-management SDK were generalized; the product-name placeholder was set to the repository's `Nami.Identity.*` convention (the `Nami.Identity.Users` package); ASP.NET Core Identity and the .NET APIs are retained as neutral framework references.
