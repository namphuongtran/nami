---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: comparison of ASP.NET Core Identity external login and the OpenIddict client stack (see More Information)
informed: all contributors, via this repository
---

# Integrate external identity providers through ASP.NET Core Identity external login

## Context and Problem Statement

Nami must let users sign in through external identity providers (Microsoft Entra ID, Google, enterprise IdPs). On .NET with OpenIddict there are two viable technical paths, and early design drafts described both in different places, which is a contradiction waiting to become inconsistent code. One path must be the standard, because it governs user provisioning, account linking, claims mapping, callback conventions, and logout behavior.

## Decision Drivers

* The user store, provisioning, and account linking are already built on ASP.NET Core Identity, and identity is global per ADR-0001; the federation path should plug into that with as little bridging code as possible.
* One consistent integration path: every dependent design (claims handling, linking, logout) must have a single answer.
* Account takeover via external login is a classic IdP vulnerability class; the chosen path must support strict linking and claims hygiene.
* Avoid building or maintaining bridges that duplicate what the framework already does.

## Considered Options

* ASP.NET Core Identity external login (handler-based)
* OpenIddict client stack (`OpenIddict.Client.WebIntegration`)

## Decision Outcome

Chosen option: "ASP.NET Core Identity external login (handler-based)", because the user store, provisioning, and account linking already live on ASP.NET Core Identity, so the handler-based path (`AddAuthentication().AddOpenIdConnect()` and provider packages, callback via `SignInManager.GetExternalLoginInfoAsync()`) integrates naturally with no provisioning bridge. The OpenIddict client stack remains an allowed exception where provider-specific token management is genuinely needed (for example calling a provider's downstream APIs), accepting that such a use must bring its own provisioning bridge.

Alignment with ADR-0001 (important): an external login provisions or links into the **global** identity store; one external person is one Nami identity. Tenant access is then granted through **membership**, never by creating a per-tenant identity.

Scope note: v1 uses a **static, host-level set of external IdPs** configured at deployment time. Dynamic per-tenant self-service federation was deliberately deferred, and later opened as a separate additive v2 feature (ADR-0034); that does not change this decision.

### Consequences

* Good, because there is one consistent path, and account linking, claims allow-listing, and logout are all designed around ASP.NET Core Identity.
* Bad, because some conveniences of the OpenIddict client stack's prebuilt provider catalog are given up.
* This decision unlocks its dependent security decisions, which are binding implementation requirements (they ship with this ADR rather than as separate ADRs):
  * Claims from external IdPs pass an **allow-list**; sensitive claims (roles, groups, `email_verified`) are always taken from the local record, never trusted from the external token.
  * The account-linking key is **(provider, subject)**; an unverified email is never a linking key (anti-takeover). Auto-linking happens only when the email is verified on both the external and local sides; otherwise the user signs in locally and links deliberately.
  * Provider client secrets live in the secret store, never in plaintext configuration.
  * Authority/discovery URLs are validated against SSRF (https only, allow-listed hosts) at configuration time, and every runtime backchannel call (discovery, JWKS, token, userinfo) passes through a fail-closed egress handler that resolves the host to an IP before connecting and rejects loopback, private (RFC 1918/ULA), link-local, and cloud-metadata addresses, non-HTTPS, and cross-host redirects.
  * Each provider gets a unique callback path, and the authorization response issuer is verified (RFC 9207), with the correlation state bound to the initiating provider scheme, to defend against IdP mix-up.

### Confirmation

Code review against this ADR: any external-provider integration must use the handler-based path (or document the token-management exception explicitly), provision into the global identity store, and implement all five security requirements above. Integration tests cover provisioning, linking by (provider, subject), and rejection of unverified-email linking.

## Pros and Cons of the Options

### ASP.NET Core Identity external login (handler-based)

Standard ASP.NET Core authentication handlers per provider, with `SignInManager` completing the round trip into the local user store.

* Good, because it plugs directly into the already-chosen user store: provisioning and linking use `UserManager`/`SignInManager` as designed, no bridge code.
* Good, because it is the framework-native, widely documented path; every mainstream provider ships a handler.
* Good, because logout and session semantics stay within one authentication system.
* Bad, because provider-specific token management (refresh, downstream API calls against the provider) is not built in and needs extra work where required.

### OpenIddict client stack (`OpenIddict.Client.WebIntegration`)

OpenIddict's own client with `UseWebProviders()`, offering a large catalog of prebuilt provider integrations and strong token management.

* Good, because of the prebuilt provider catalog and first-class token management for calling providers' APIs.
* Neutral, because it is a natural fit in an OpenIddict-based server codebase.
* Bad, because it does not know the local user store: provisioning and account linking into ASP.NET Core Identity must be written and maintained as a custom bridge.
* Bad, because standardizing on it would put the primary login path on the less-integrated option purely for conveniences that v1 does not need.

## More Information

* Original decision: 2026-06-28, synchronized with ADR-0001 v2 (global identity, tenant-scoped membership). Imported into this repository and translated in 2026-07; content preserved, internal references generalized.
* Related decisions: ADR-0001 (multi-tenant isolation: global identity and membership), ADR-0034 (dynamic per-tenant external IdP, v2, additive).
* Open follow-up (does not block implementation): the initial list of external providers (for example Entra ID, Google) is finalized during implementation of the federation feature.
