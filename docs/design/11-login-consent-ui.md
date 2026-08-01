---
status: reviewed
created: 2026-07-23
tags: [design, ui, login, consent, logout, razor, theming, localization]
---

# Login, consent, and logout UI (detailed design)

## 1. Decisions realized

| Decision | What this design applies |
|---|---|
| ADR-0019 | Single logout: front-channel iframe dropped (dead); end-session and tenant-switch are top-level redirects; interim back-channel `logout_token` fan-out; first-party SPAs delegate logout to the BFF |
| ADR-0004 | Persistent consent as a permanent OpenIddict `Authorization` (no expiry); silent `prompt=none` renew; the grants page is the sole revoke path |
| ADR-0003 | Server-side sessions keyed by `sid`; `sid` rotates on step-up; a new `sid` is minted at primary auth (fixation defense); revoke denies authorize/refresh |
| ADR-0001 | Tenant-aware UI resolved by host/path; the tenant switcher reads `memberships` and switches via silent `prompt=none`; access tokens are single-tenant |
| ADR-0002 | Handler-based external login into the global identity; `(provider, sub)` anti-takeover linking; deny-by-default external-claim allow-list; unique callback + `iss` + SSRF guard |
| ADR-0013 | Step-up challenge page triggered by `acr_values`/`max_age`/`prompt`; `required_acr = max(client, scope, runtime)`; UI renders the challenge, 07 enforces |
| ADR-0028 | Passkeys as v1 core (enroll/list/remove); self-service via custom endpoints, never `MapIdentityApi`; credential-hardening surfaced in copy |
| ADR-0029 / ADR-0043 | The UI's server-rendered antiforgery profile is distinct from the BFF's CSRF profile; the core-cookie-attributes invariant with a startup self-check |
| ADR-0053 / ADR-0008 | A hash-chained consent receipt on grant and `consent.revoked` on revoke; branding changes are an `admin_config_change` security event |
| ADR-0042 / ADR-0038 | Risk-triggered `IChallengeProvider` on login/reset/device/signup (off in Development); per-IP plus per-account lockout; constant-time anti-enumeration |
| ADR-0062 | OWASP ASVS 5.0 L2 baseline for this whole surface, with each security test tagged to its requirement id |
| ADR-0072 / ADR-0091 | Razor Pages with no client runtime, and the browser-facing response headers as three profiles: the `SecurityHeadersAttribute` applies the UI profile, `form_post` takes its own, framing is denied outright, and no nonce is available to theming (section 7.4) |

## 2. Purpose and scope

The human-facing surface of the authorization server: a complete set of Razor Pages
(login, consent, logout, passkeys, account management, error) decoupled from the
OpenIddict engine by a thin interaction service, plus the cross-cutting UI concerns that
protect every page (security headers, antiforgery, cookie pinning, open-redirect guard,
theming, localization). OpenIddict ships no UI and, unlike the commercial engines, no
interaction-service abstraction, so this layer is what makes the product turnkey.

This design owns the **presentation** layer and one piece of runtime behavior that is
naturally UI-initiated: the single-logout fan-out. It deliberately defers the machinery
it renders:

- **Protocol** (authorize / consent / end-session pass-through, `prompt=none` errors,
  single-token revocation) to the core protocol server (04).
- **Auth backend** (the `SignInManager` / `UserManager` calls, server-side sessions,
  the change-email flow and policy) to user management (08), and **external federation
  with the canonical claims contract** to (09).
- **Email landing pages** (reset / confirm anti-enumeration timing, per-purpose token
  lifespans, the `en`-floor i18n chain) to the email subsystem (10).
- **Audit** to (03), **step-up enforcement and dual-control** to (07), and the **schema**
  (`TenantBranding`, `ServerSideSessions`, the OpenIddict `Application`) to (02, the SSOT).

In scope: the interaction service, the page catalog, consent/grants UI, the single-logout
fan-out, the tenant switcher, the step-up challenge page, the external-login / passkey /
MFA / account-management pages, the error-state inventory, security-headers/CSP wiring,
antiforgery, the cookie matrix, the open-redirect guard, theming and the OSS override
seam, and localization. The UI lives inside the `Nami.Identity` reference host (a
dedicated UI assembly would be a future ADR); the admin console is a separate app (16 /
ADR-0020) and is out of scope here.

## 3. Interfaces and contract

### 3.1 The interaction service

OpenIddict has no engine-to-UI bridge (there is no `IIdentityServerInteractionService`
equivalent). The UI builds a thin `IInteractionService` over
`HttpContext.GetOpenIddictServerRequest()`, `IOpenIddictApplicationManager`, and
`IOpenIddictScopeManager`: it reads the current OIDC request, resolves the client and
scope display metadata, and turns a page decision back into an engine result. Consent is
a **round-trip to the pass-through authorize endpoint** (the original request parameters
are carried to `/Consent` and back), not a callback. The authorize controller's
`Challenge()` redirects an unauthenticated request to `/Account/Login?returnUrl=<the full
original request>`, and the login page round-trips that `returnUrl`.

```mermaid
flowchart LR
  classDef eng fill:#d5e8d4,stroke:#82b366,color:#000
  classDef ui fill:#fff2cc,stroke:#d6b656,color:#000

  ENG["OpenIddict authorize<br/>(pass-through)"]:::eng
  LOGIN["/Account/Login"]:::ui
  CONSENT["/Consent"]:::ui
  LOGOUT["/Account/Logout"]:::ui

  ENG -->|Challenge, returnUrl| LOGIN
  LOGIN -->|SignIn, cookie set| ENG
  ENG -->|consent required| CONSENT
  CONSENT -->|SignIn approve / Forbid deny| ENG
  ENG -->|end-session| LOGOUT
  LOGOUT -->|top-level redirect| ENG
```

### 3.2 Page catalog

The five v1 core pages are Login, Passkeys, Logout, Consent, and Home/Error; the rest
round out parity. There is no API-Resource / Identity-Resource concept in the UI
(OpenIddict has none); the consent screen shows scopes only.

| Page | Core v1 | Owns / renders | Backend |
|---|---|---|---|
| Account/Login | yes | credentials, passkey button, external buttons, tenant branding | 08 |
| Account/Passkeys | yes | enroll / list / remove (WebAuthn) | 08 |
| Account/Logout | yes | end-session + single-logout fan-out | 04 / this doc |
| Consent | yes | client branding, scope list, remember, approve/deny | 04 |
| Home/Error | yes | workflow error surface | 04 |
| Account/Create + ConfirmEmail | | register + confirm landing | 08 / 10 |
| Account/Forgot + ResetPassword | | reset request + landing | 08 / 10 |
| Account/Manage (family) | | TOTP, recovery codes, password, email, profile | 08 |
| Account/ExternalLogin | | callback + anti-takeover linking | 08 / 09 |
| Account/StepUp | | re-auth / MFA challenge | 07 / 08 |
| Account/AccessDenied | | authenticated-but-unauthorized | 04 |
| Tenant/Switcher | | membership list, silent switch | 04 / 08 |
| Grants | | view / revoke persistent consent | 04 |
| Device | | device-code user entry | 14 |

CIBA is de-scoped (ADR-0014); the device page is kept.

## 4. Data and structure

The UI reads three tables defined in the data-tier SSOT (02); it does not define or extend
their schema.

- **`TenantBranding`** (`TenantId` PK/FK, `LogoUri`, `ThemeJson` jsonb, `DisplayName`,
  `UpdatedByMembershipId`, `UpdatedAtUtc`): the tenant-level branding source, resolved
  per-tenant with a global fallback. `ThemeJson` holds **design tokens, not raw CSS**;
  tenant-supplied raw CSS is not part of the v1 schema and is treated as a future
  extension. `LogoUri` is https-only and SSRF-safe.
- **`ServerSideSessions`** (and the child `SessionParticipatingClients`): the session the
  logout UI revokes and the RP list the fan-out iterates. Session establishment is owned by
  08; the fan-out behavior over `SessionParticipatingClients` is owned here.
- **OpenIddict `Application`**: `DisplayName` (native) plus `Properties['logo_uri' /
  'client_uri']` for consent branding, following the `cors_origins` property pattern.

## 5. Behaviour

### 5.1 Login

The login page is tenant-aware: tenant is resolved by host/path (never a claim, ADR-0001)
and the page renders that tenant's branding and context. It carries the five login-form
parity behaviors (R17): a remember-me checkbox mapped to `AuthenticationProperties.IsPersistent`
(gated by `AllowRememberLogin`); a Cancel button that returns `access_denied` to the
client via the authorize controller's `Forbid()` (not a redirect home); a single generic
invalid-credentials message that discloses no field (anti-enumeration); an
`EnableLocalLogin` toggle with external-only auto-redirect (local off plus exactly one
external provider redirects straight through); and a client display ("sign in to
«AppName»") from `IOpenIddictApplicationManager.FindByClientIdAsync` then
`GetDisplayNameAsync`, with the external-provider list from `IAuthenticationSchemeProvider`.

Two backend contracts the page must honor (owned by 08): sign-in stamps `amr` **and
`auth_time`** as claims in the one `SignInWithClaimsAsync(user, isPersistent, [...])` call,
never taking `auth_time` from the ticket's `IssuedUtc`, which a sliding cookie re-issues on
ordinary traffic (08); and at the anonymous-to-authenticated primary auth the page mints a **new
`sid`** and a new ticket-store row, discarding the pre-login session key (session-fixation
defense). The external-button enumeration is the single v1-to-v2 touch point for dynamic
per-tenant IdPs: v1 shows the static host-level set, and when v2 lands exactly one
call-site routes through `IExternalProviderQuery.GetForTenantAsync` (the seam is
deliberately not planted in v1, ADR-0034).

Interactive login through consent back to the client:

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant AZ as authorize (pass-through)
  participant LG as /Account/Login
  participant SM as SignInManager (08)
  participant CO as /Consent

  U->>AZ: GET /connect/authorize
  AZ-->>U: Challenge, redirect to Login?returnUrl
  U->>LG: credentials / passkey / external
  LG->>SM: PasswordSignInAsync / PasskeySignInAsync
  alt requires 2FA
    SM-->>LG: RequiresTwoFactor
    U->>LG: TOTP / recovery code
  end
  LG->>LG: mint new sid, stamp amr + auth_time
  LG-->>AZ: SignIn, cookie set, replay returnUrl
  alt consent required (not Implicit, no stored grant)
    AZ-->>U: redirect to Consent
    U->>CO: approve (remember?) / deny
    CO->>CO: CreateAsync Permanent + SetAuthorizationId, emit consent receipt
    CO-->>AZ: SignIn approve (or Forbid access_denied)
  end
  AZ-->>U: authorization code to client redirect_uri
```

### 5.2 Consent and grants

The consent controller switches on `GetConsentTypeAsync`: `Implicit` (first-party)
auto-consents silently; `External` with no stored authorization returns
`Forbid(ConsentRequired)`; otherwise the screen renders. Approve builds a `ClaimsIdentity`,
sets scopes/resources/destinations, and `SignIn`s under the OpenIddict server scheme;
deny or cancel returns `Forbid` with `access_denied` to the client. When the user ticks
remember, the page persists a permanent authorization via
`IOpenIddictAuthorizationManager.CreateAsync(identity, subject, clientId,
AuthorizationTypes.Permanent, scopes)` and then calls `identity.SetAuthorizationId(...)`
(load-bearing: family-revoke and entry validation key on it). The silent-renew and
grants-list lookups use `FindAsync(subject, client, Statuses.Valid,
AuthorizationTypes.Permanent, request.GetScopes())` (not `FindBySubjectAsync`); the scope
filter is what forces re-consent on scope expansion. The don't-remember branch creates no
permanent authorization and issues an ad-hoc grant for the current request only.

Consent has **no expiry** (OpenIddict permanent authorizations never expire; there is no
per-client `ConsentLifetime`), so the grants page (`TryRevokeAsync`) is the only removal
path; this is an accepted decision, revisited only if a security/DPO policy later requires
periodic re-consent. On grant the page emits a hash-chained **consent receipt** through
the audit sink (subject, client, tenant, scope set, purpose, legal basis, policy-version
hash, timestamp, locale, method) and `consent.revoked` on revoke (ADR-0053 §F); the
receipt schema and consent-policy-version governance are owned by the data-subject-rights
design (17) and its ADR, referenced here, not defined here.

Client branding on the consent (and login) screen reads the client display name from the
native `DisplayName` descriptor field and the `logo_uri` / `client_uri` from
`Application.Properties` (the same JSON-dict mechanism used for `cors_origins`, 04 /
23); no new client column is introduced, and the exact read path is confirmed against 02.
The logo URL is validated https-only with a mixed-content / SSRF guard on render. Scope
display metadata (DisplayName, Description, Emphasize, Required, Checked) comes from
`IOpenIddictScopeManager`, grouped identity-versus-api.

### 5.3 Logout and single logout

Front-channel iframe logout is dropped as a dead dependency (third-party-cookie blocking,
V11); end-session is a top-level redirect. The logout page invokes the backend (cookie
sign-out, OIDC end-session, server-side session revoke) and initiates the **back-channel
fan-out**, which this design owns. The fan-out reuses the shared outbox chassis from the
email design (10): it stores delivery *intent* (the recipient's own `SidIssued`, `sub`,
`ApplicationId`, `backchannel_logout_uri`)
over the `SessionParticipatingClients` rows, mints a fresh `logout_token` on each send
(`typ=logout+jwt`, the `backchannel-logout` events member, `sub` and/or `sid`, `iat`,
`jti` replay guard, no `nonce`, `exp` under about two minutes), claims with `SKIP LOCKED`,
retries with backoff (attempt cap about five, total about ten minutes), and dead-letters.
Interactive logout never blocks on the fan-out.

**Two ordering rules carry the guarantee, and both are easy to lose in implementation.**
First, the delivery rows are enqueued **in the same transaction as the session revoke**, the
same boundary the email design takes for its critical flows (ADR-0038): if the revoke
commits and the rows do not, the session is gone and no relying party is ever told. That is
not a local exception to the one-aggregate-per-transaction rule either; ADR-0059 already
names "the transactional-outbox write of an audit or outbox record within that same
transaction" as the deliberate atomic-capture exception, an infrastructure concern rather
than a second aggregate. Second,
**the immediate dispatch runs after the response is written, never before** (ADR-0019).

**The immediate dispatch is best-effort and deliberately has no retry of its own.** Once the
rows are committed, the logout handler fires one parallel POST attempt per participating
relying party and does not wait for the results before completing the response, so the
common case is near-immediate without adding the N-call latency the outbox exists to remove.
A success marks its row `delivered`; anything else is simply left `pending` for the relay.
The dispatch attempts **at most once per relying party**: every retry, backoff and
dead-letter decision belongs to the relay alone, because two delivery paths with two retry
policies would let one relying party receive logout tokens governed by different rules. A
dispatch that never runs at all, because the pod died first, costs nothing but latency, and
that is the property that makes the guarantee independent of it.

"Log out everywhere" maps to the built `RevokeBySubjectAsync` (owned by 08 / revocation
propagation 13) plus session revocation, never the single-token `/connect/revoke` endpoint.
Force-logout is a ticket-store row removal, effective on the next request on any node with
the 1-2 minute validation-interval backstop. First-party SPAs delegate logout to the BFF,
which receives the back-channel token (BFF details out of scope). Post-v1 logout
extensibility (upstream notification, custom redirect writer, login/logout context) is
deferred.

Single logout (top-level redirect plus non-blocking back-channel fan-out):

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant LO as /Account/Logout
  participant SS as Session store (02)
  participant OB as Logout outbox (chassis from 10)
  participant RB as Fan-out relay
  participant RP as RP backchannel_logout_uri

  U->>LO: POST logout (antiforgery-validated)
  LO->>SS: revoke session row (sid)
  LO->>OB: enqueue intent per SessionParticipatingClients (SidIssued, sub, ApplicationId, uri)
  LO-->>U: top-level redirect to signed-out (does not block on fan-out)
  RB->>OB: claim intent (SKIP LOCKED)
  RB->>RB: mint fresh logout_token (typ logout+jwt, jti, exp under 2 min)
  RB->>RP: POST logout_token
  RP-->>RB: 200 (or retry with backoff, then dead-letter)
```

### 5.4 Tenant switcher, step-up, external login, passkeys, MFA, and account management

The **tenant switcher** reads `memberships` (and `memberships_truncated`) from the
`id_token`, calling the self-service full-list endpoint (08) when truncated; a switch
is a silent `prompt=none` authorize round trip as a top-level redirect (never an iframe),
with no password prompt, and access tokens stay single-tenant.

The **step-up** page is triggered when an API returns `401
insufficient_user_authentication` with `acr_values` / `max_age` (RFC 9470); the authorize
endpoint checks these against the session and re-challenges, a `prompt=none` that cannot
satisfy the requirement returns `login_required`, and the `sid` rotates on step-up. The
required level is `max(client DefaultAcr, scope RequiredAcr, runtime request)`; the UI
renders the challenge, 07 enforces the threshold.

**External login** uses handler-based ASP.NET Core Identity external login
(`GetExternalLoginInfoAsync` then `ExternalLoginSignInAsync`, or create plus
`AddLoginAsync`), provisioning into the global identity. Linking keys on `(provider, sub)`
and never on an unverified email: auto-link only when the email is verified on both sides,
otherwise the user signs in locally and links deliberately. External claims pass a
deny-by-default allow-list; role, groups, and `email_verified` always come from the local
record. Each provider has a unique callback path, `iss` is verified (RFC 9207), correlation
is bound to the scheme, a fail-closed SSRF guard rejects cross-host redirects, and the
`idp` claim is set explicitly. The friendly failure page for a remote error / correlation
failure / user cancellation is error-state E3.

**Passkeys** are v1 core: enroll/list/remove, with a passkey as a primary factor.
Registration is `MakePasskeyCreationOptionsAsync` then `navigator.credentials.create` then
`PerformPasskeyAttestationAsync` then `AddOrUpdatePasskeyAsync`; sign-in is
`MakePasskeyRequestOptionsAsync` then `navigator.credentials.get` then
`PasskeySignInAsync`. The endpoints are not auto-mapped, so the UI hand-maps
`/account/passkey/*` with antiforgery and HTTPS, list/remove read `UserPasskeyInfo`, and
there is no default attestation validation (the attestation policy is a GA gate). The `amr`
is `hwk` or `swk`, never the string "passkey".

The **MFA** page runs the `PasswordSignInAsync` then `RequiresTwoFactor` branch and calls
`TwoFactorAuthenticatorSignInAsync(code, isPersistent, rememberClient)`, surfacing **two
distinct checkboxes** (remember-me = `isPersistent`, remember-this-machine =
`rememberClient`) plus a "use a recovery code" fallback
(`TwoFactorRecoveryCodeSignInAsync`). The **Account/Manage** family covers TOTP enroll (QR
from the `otpauth://` provisioning URI then `VerifyTwoFactorTokenAsync` then
`SetTwoFactorEnabledAsync`), recovery codes (`GenerateNewTwoFactorRecoveryCodesAsync`,
display-once, regenerate), change-password, change-email, and profile edit. Change-email
enforces the four-branch hardening whose flow is owned by 08 (step-up before initiate,
notify the old address with a no-token tripwire, verify the new address before the switch,
rotate the security stamp on completion); the UI enforces the branches and defers the
flow. Self-service uses custom endpoints; `MapIdentityApi` is deliberately not mapped.

The **reset / confirm** pages preserve the constant-time anti-enumeration contract owned
by 10, Base64Url-decode the token, land on the per-purpose lifespans (reset about 1h), and
render explicit success versus expired/invalid states with a request-new link.

Step-up challenge:

```mermaid
sequenceDiagram
  autonumber
  participant API as Resource server
  participant SPA as Client
  participant AZ as authorize
  participant SU as /Account/StepUp

  API-->>SPA: 401 insufficient_user_authentication (acr_values, max_age)
  SPA->>AZ: authorize with acr_values / max_age
  AZ->>AZ: compare required_acr = max(client, scope, runtime) vs session
  alt satisfied
    AZ-->>SPA: code (no challenge)
  else needs step-up
    AZ-->>SU: challenge
    SU->>SU: re-auth / MFA, rotate sid, raise acr/amr
    SU-->>AZ: elevated session
    AZ-->>SPA: code
  end
```

### 5.5 Theming and branding (the OSS override seam)

Two branding tiers must not be conflated: tenant-level (Login/Logout/StepUp show which
tenant's IdP this is, resolved by host/path from `TenantBranding`) and client-level
(Consent shows which app is asking, from the OpenIddict `Application`). A default Bootstrap
5 theme ships so a deployment runs out of the box. Consumers restyle without forking core
through three override points, which **this design defines**: config-level (logo/color/name
via config/env), Razor **view-override** (a consumer `_Layout` or view wins over the default
by `RazorViewEngine` precedence), and a `wwwroot/theme/` assets folder. The packaging
decision's extension-point list covers backend ports and handlers only and does not yet
name a UI theming seam, so these three points currently have **no ADR home**; that gap is
an open item below rather than a citation, because crediting them to a decision that does
not mention them would be worse than naming the gap.

Branding depth in v1 is deliberately bounded: a logo, a primary and accent colour, a
display name, and support, privacy, and terms links. Custom fonts and full per-tenant page
templates are **not** in v1. Tenant-supplied raw CSS is likewise not in the v1 schema
(`ThemeJson` carries design tokens); if it is ever added, it inherits the same sandbox
conditions the email templating engine has: a `<style>` block only, no `<script>` and no
inline event handlers, a controlled CSP `style-src`, and no `url()` to a foreign host,
since that is an exfiltration and tracking channel. **One of those conditions was
foreclosed on 2026-08-01 and is left in place as the record of a constraint that tightened**:
ADR-0091 parameter D admits no nonce and no hash on `style-src`, so "a `<style>` block only"
is no longer available. Tenant raw CSS, if it is ever added, has to arrive the same way the
tokens now do, as a served stylesheet under `style-src 'self'`, or reopen ADR-0091. That is
the stricter and cheaper of the two, because a served response can be validated once before
it is stored rather than on every render. A branding change is
an `admin_config_change` security event committed synchronously in the same transaction
(03 / ADR-0008), and branding is per-tenant data under tenant isolation. Asset URLs are
https-only with SSRF and mixed-content guards.

### 5.6 Localization

Culture is resolved from `Accept-Language` (`RequestLocalizationMiddleware`) with a
per-tenant default-culture override, and an explicit user culture cookie wins over both.
All Razor pages use `.resx` plus `IStringLocalizer<T>` (no hard-coded strings; validation
messages localized too). The fallback chain is the same as the email subsystem (10):
requested culture then neutral culture then the `en` floor that always renders, warning
once on a missing key. The UI and email render the same language within one flow. Supported
cultures are configuration-driven per deployment; RTL is out of scope for v1.

### 5.7 Abuse defense on UI surfaces

A risk-triggered `IChallengeProvider` (CAPTCHA / Turnstile / proof-of-work) is applied to
the login, password-reset, device-verification, and signup surfaces (not always-on, and
disabled in Development); failures are scoped per source IP alongside per-account lockout,
and the break-glass account is exempt. The provider mechanics and the risk thresholds are
owned by ADR-0042 (deferred numbers), referenced here. The reset/resend endpoints keep the
constant-time anti-enumeration contract from 10.

### 5.8 The error-state inventory

| # | State | Required UX |
|---|---|---|
| E1 | Lockout at login | uniform "invalid credentials", never "account locked"; any lockout notice goes by email |
| E2 | Disabled user | same uniform message (the `CanSignInAsync` **override** gate, 08 section 7; disable-not-delete) |
| E3 | External-IdP callback failure | friendly retry / choose-another; log server-side, never dump the raw IdP error |
| E4 | Expired/invalid confirm-email token | dedicated state plus resend (anti-enumeration) |
| E5 | Expired/invalid reset token | dedicated state plus request-new link; no precise reason |
| E6 | `prompt=none` failure | error redirect to client (`login_required` / `consent_required`); HTML Error page only when no valid redirect_uri |
| E7 | Unknown/invalid scope | `invalid_scope` to client; if no redirect, generic Error page, never echo the raw scope value |

General rule: the end-user error is a generic message plus a correlation id; technical
detail goes only to the log/audit lane (`ISecurityEventSink` for security-relevant cases),
and all copy passes through localization.

## 6. Dependencies and wiring

### 6.1 Middleware ordering

The committed pipeline (01) resolves tenant before authentication and OpenIddict, but does
not yet include the UI's own stages. This design inserts them as: `ForwardedHeaders` then
`UseMultiTenant()` (before auth) then the ticket-store-backed **auth cookie** then
**security headers / CSP** then **request localization** then routing then
authentication/authorization then **antiforgery** then the OpenIddict middleware and
endpoints. This extends the 01 composition and is called out there as a build-time item.

```mermaid
flowchart TB
  classDef tenant fill:#dae8fc,stroke:#6c8ebf,color:#000
  classDef ui fill:#fff2cc,stroke:#d6b656,color:#000
  classDef eng fill:#d5e8d4,stroke:#82b366,color:#000

  FH["ForwardedHeaders"]:::tenant
  MT["UseMultiTenant (before auth)"]:::tenant
  CK["Auth cookie (ITicketStore-backed)"]:::ui
  SH["Security headers / CSP"]:::ui
  LOC["Request localization"]:::ui
  RT["Routing"]:::ui
  AUTH["AuthN / AuthZ"]:::ui
  AF["Antiforgery"]:::ui
  OI["OpenIddict middleware + endpoints"]:::eng

  FH --> MT --> CK --> SH --> LOC --> RT --> AUTH --> AF --> OI
```

### 6.2 Key libraries and patterns

Razor Pages on ASP.NET Core, now owned by **ADR-0072**, which records why Blazor is not
used for this surface: Blazor Server requires session affinity that the no-sticky-session
deployment forbids, its circuits hold per-user server memory at odds with the externalized
session state, and both its WebAssembly and its server-side documented CSP baselines are
looser than Razor Pages needs. Blazor static server-side rendering is the option to
evaluate first if this surface ever becomes genuinely interactive. Bootstrap 5 is the default CSS framework (CSS-variable driven so the CSP stays
strict, no npm/Tailwind build step; Tailwind is a later option). `IStringLocalizer` /
`.resx` for localization, `RequestLocalizationMiddleware` for culture resolution, and the
built-in antiforgery and data-protection stacks. All are part of ASP.NET Core or
permissive (MIT/Apache-2.0/BSD) per ADR-0026.

Patterns applied (ADR-0066): **Humble Object / pass-through controller** (the interaction
service holds no protocol logic), **Strategy** (`ConsentType` handling, `IChallengeProvider`,
external-provider selection), **Adapter** (external IdP handlers), **Template Method** (the
Razor view-override theming seam), and deny-by-default (returnUrl and scope validation).

## 7. Error handling, edge cases, invariants

### 7.1 Antiforgery (distinct from OAuth CSRF and the BFF)

An antiforgery token is required on every interactive form POST (Login, Consent
Accept/Deny, Logout, 2FA, Register) via `@Html.AntiForgeryToken()` plus
`[ValidateAntiForgeryToken]`. The machine-facing OAuth endpoints carry
`[IgnoreAntiforgeryToken]` and must not have antiforgery. This server-rendered-form profile
is distinct from OAuth-layer state/PKCE CSRF and from the BFF's JS/SPA CSRF profile (custom
header plus strict CORS, ADR-0029).

The split is finer than "UI pages on, OAuth endpoints off", and the engine's own sample
(`velusia.cs` in the upstream reference tree, read at source rather than paraphrased)
shows why. **`POST /connect/authorize` carries both policies at once**: the authorize entry
action, which accepts GET and POST so a client can arrive by either, is
`[IgnoreAntiforgeryToken]`, while the consent-form submits bound to the same route are
`[ValidateAntiForgeryToken]`, and the two are discriminated by a form-value selector rather
than by route. `POST /connect/token` ignores antiforgery. Two consequences follow, and both
are easy to get wrong: a blanket `AutoValidateAntiforgeryToken` over an area containing the
authorize controller breaks the machine entry, and a blanket ignore on the authorize route
strips protection from the consent submit, which is the one form on that route a hostile
page would most want to forge.

### 7.2 Cookie matrix

| Cookie | Attributes |
|---|---|
| SSO / session | `__Host-` prefix, `Secure`, `HttpOnly`, `SameSite=Lax`, `Path=/` |
| External-login correlation / nonce | `Secure`, `HttpOnly`, `SameSite=None`, no `__Host-` prefix |

The correlation cookie is `SameSite=None` because the external callback is a cross-site
POST/redirect; the SSO cookie stays `Lax` because it still transmits on a top-level POST
navigation. This reconciles with `response_mode=form_post` (a cross-site POST-back needs
`SameSite=None; Secure` on any cookie read in that request). The invariant is enforced by
the fail-fast `core-cookie-attributes` startup self-check (04) and an OWASP ASVS L2 V3 test
(which also asserts the `sid` is reissued after primary auth), per ADR-0043 / ADR-0062.

### 7.3 Open-redirect guard

Every `returnUrl` and `post_logout_redirect_uri` is validated with `Url.IsLocalUrl` for
internal redirects or against the client's registered redirect allow-list for external
ones, applied consistently across Login, Logout, Consent, ExternalLogin, Redirect, StepUp,
and the tenant switcher; the helpers live in `Extensions.cs`. Registered redirect URIs are
exact-match https with no wildcard (client-registration owns that policy, ADR-0035).

### 7.4 Security headers and CSP

A `SecurityHeadersAttribute` applies CSP, `X-Frame-Options`, and `X-Content-Type-Options`
to all UI pages; the concrete CSP policy values are finalized in the observability/security
hardening phase (referenced, deferred). Theming must not loosen the CSP (no
`unsafe-inline`; colors come from CSS variables or a nonce).

**Who owns which half of that, because until 2026-08-01 this section named no owner and
[20](20-testing.md) section 10 recorded the policy as ownerless.** The **strictness** is
decided: ADR-0072 parameter C rules that "theming must never loosen the Content Security
Policy" and that a theme requiring `unsafe-inline` is "rejected rather than accommodated",
and that ADR's consequences commit the login surface to "no `unsafe-inline`, no
`unsafe-eval`, no `wasm-unsafe-eval` in `script-src`". That is the whole reason the rendering
stack is server-rendered Razor with no client runtime, so the sentence above is ADR-0072
being applied here, not a rule this design invents.

**The other half was closed later the same day by
[ADR-0091](../adr/0091-browser-facing-response-headers.md), and the paragraph above understated
what was missing.** It said the concrete directive values remained unowned and that no ADR named
`X-Frame-Options`, `frame-ancestors`, or clickjacking, which was accurate as a measurement over
`docs/adr/` that morning. What it did not know is that the obvious strict policy **breaks this
page's own protocol**: `response_mode=form_post` returns HTML the engine writes, ending in an
inline submit script and posting cross-origin to the client, so `script-src 'self'` and
`form-action 'self'` each stop authorization dead with a blank page and a clean server log.
ADR-0091 therefore replaces the single policy this section assumed with **three profiles**
selected by response class, and four consequences land directly on this design:

- The `SecurityHeadersAttribute` named above applies the **UI** profile, not one global policy,
  and the `form_post` response takes the Protocol HTML profile instead.
- `frame-ancestors 'none'` with `X-Frame-Options: DENY`, on the strength of ADR-0014 and
  ADR-0019 having already removed every iframe this design once had (section 5.3). A per-tenant
  framing allowlist is rejected rather than deferred, so `TenantBranding` gains no such field.
- The theming clause above offers "CSS variables **or a nonce**". The nonce half is now
  foreclosed: ADR-0091 parameter D admits no nonce anywhere, and the per-tenant tokens of
  section 5.5 are served as a **stylesheet response** rather than an inline `<style>` block, which
  is ADR-0072 parameter E's external-files rule applied to style.
- Two of ADR-0043's startup invariants now assert the policy, so a consumer who weakens it
  through configuration fails startup rather than serving.
- The deferral in the first paragraph, that the values "are finalized in the
  observability/security hardening phase", is **discharged**. They are decided at ADR level
  instead, which is where they belonged: a build-time task with no owner is how this sat open for
  a month. Two residuals survive as a single Pre-GA checklist entry under Security, the
  enumerated `Permissions-Policy` allowlist and the acceptance of `img-src` admitting any https
  origin so the external `LogoUri` of section 4 renders.

## 8. Security and multi-tenancy notes

- **Open redirect** is the classic login/logout/consent vulnerability; `returnUrl` and
  post-logout redirects are untrusted input and are always validated (above).
- **Anti-enumeration** is uniform across login (E1/E2), reset, and resend; error copy never
  reveals account existence or state.
- **Session fixation** is defended by minting a new `sid` at primary auth and rotating it
  on step-up.
- **External-token trust:** sensitive claims come from the local record, never the external
  token; the linking key is `(provider, sub)`, never an unverified email.
- **Antiforgery, cookie pinning, and CSP** are the standing per-page protections, with the
  cookie invariant asserted at startup and in an ASVS test.
- **Reflected injection:** the Error/consent pages never echo raw scope or IdP error values.
- The whole UI surface is held to OWASP ASVS 5.0 Level 2, with each security test tagged to
  its requirement id (ADR-0062).

## 9. Testing

- End-to-end: client → login → (MFA/passkey) → consent → code → token.
- Persistent consent: remember creates a permanent authorization and silent `prompt=none`
  renew succeeds; a grants-page revoke makes the next request re-prompt.
- Tenant switch: silent `prompt=none` top-level redirect, no re-login, single-tenant token.
- Step-up: triggered by `acr_values`/`max_age`/`prompt`; `sid` rotates.
- Single logout: back-channel by `sid` ends exactly one session (not all `sub` sessions);
  `logout_token` has `typ=logout+jwt`, the events member, and no `nonce`; tenant-switch is a
  top-level redirect, not an iframe.
- Open redirect: a non-local, non-allow-listed `returnUrl` is rejected.
- Cookies: SSO `__Host-`+Lax and correlation `SameSite=None` both present; external login
  with `response_mode=form_post` completes without cookie loss; `sid` reissued after primary
  auth (ASVS V3).
- **Response headers, per profile (ADR-0091).** Each of the three profiles is asserted on a
  representative response, and no response carries a `nonce-` source. The load-bearing case is
  `response_mode=form_post`, which is a **browser** test rather than a header assertion: the
  flow must complete under the enforced Protocol HTML profile, and the negative case must show
  that the same response under the UI profile produces **no navigation**, since that is the
  blank-page failure ADR-0091 exists to prevent and it is invisible to a server-side assertion.
  Alongside it, a build-time check asserts the script hash derived from the pinned engine
  package equals the one the profile carries, so a template change on a bump fails the build
  instead of the login. Seam S36 in [22](22-openiddict-seam-catalogue.md) section 5.2 is this
  dependency; these are its contract tests.
- Antiforgery: interactive form POSTs require a valid token; machine OAuth endpoints do not.
- Error states: E1-E7 render the specified copy and behavior.
- Localization: a missing key falls to the `en` floor and warns once; a missing tenant
  template falls to global.

## 10. Open and build-time items

- **UI package placement:** the UI lives in the `Nami.Identity` reference host; a dedicated
  `Nami.Identity.UI` assembly would be a new ADR.
- **The UI theming seam has no ADR home.** The three override points are defined in this
  design, but the packaging decision that catalogues extension points lists backend ports
  and handlers only. Either that list gains a UI-theming entry or the seam gets its own
  ADR; until then the seam is documented here and owned by nothing, which is the state
  worth fixing rather than papering over with a citation.
- **Middleware pipeline:** the security-headers/CSP, request-localization, and antiforgery
  stages are added to the 01 composition (extends that pipeline).
- **`logo_uri` / `client_uri`:** confirm the read path with 02 and add the config-DX mapper
  entries (following `cors_origins`); no new client column.
- **Deferred to GA (Pre-GA checklist):** per-scope required-`acr` and AAL thresholds
  (ADR-0013); credential-hardening thresholds surfaced in copy (ADR-0028); passkey
  attestation policy (ADR-0028); challenge/abuse thresholds (ADR-0042/0038); consent
  policy-version governance driving the receipt hash (ADR-0053); redirect_uri guardrail
  thresholds (ADR-0035); ASVS L2 coverage sign-off (ADR-0062).
- **Deferred to other docs:** the consent-receipt schema (17 / ADR-0053). **The CSP policy
  values left this list on 2026-08-01**: they were deferred to "the hardening phase", which was
  not a document and not an owner, and [ADR-0091](../adr/0091-browser-facing-response-headers.md)
  decides them instead (section 7.4). The two residuals are a Pre-GA checklist entry, not a
  deferral to another document.
- **Deferred to v2:** dynamic per-tenant IdP (the single login call-site through
  `IExternalProviderQuery`, ADR-0034); logout extensibility (ADR-0019).

## 11. Sources

- ADRs: ADR-0019 (single logout), ADR-0004 (persistent consent), ADR-0003 (sessions),
  ADR-0001 (multi-tenancy/tenant UI), ADR-0002 (external login), ADR-0013 (step-up),
  ADR-0028 (passkeys/user management), ADR-0029 (BFF boundary), ADR-0043 (cookie invariant),
  ADR-0053 (consent receipt), ADR-0008 (audit), ADR-0042 (abuse defense), ADR-0062 (ASVS),
  ADR-0035 (redirect_uri guardrails), ADR-0014 (device / CIBA
  de-scope), ADR-0020 (admin-UI boundary), ADR-0034 (dynamic IdP v2), ADR-0010
  (delegated-admin approval boundary).
- Design docs: [04 core protocol](04-core-protocol.md) (authorize/consent/logout,
  `prompt=none`, revocation), [08 user management](08-user-management.md) (login/MFA/passkey/
  federation, claims, sessions, change-email), [10 email](10-email-notification.md) (reset/
  confirm anti-enumeration, tokens, i18n floor, outbox chassis), [03 audit](03-audit.md)
  (two lanes, `admin_config_change`), [02 data](02-data.md) (`TenantBranding`,
  `ServerSideSessions`, `Application.Properties`), [01 foundations](01-foundations.md)
  (composition and middleware ordering).
- [Architecture](../architecture/README.md): containers (UI in the reference host),
  runtime views 1 (auth code) and 6 (BFF token custody).
- [Pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

---

[Prev: Email and notification](10-email-notification.md) · [Index](README.md) · Next: [Key management and rotation](12-key-management.md)
