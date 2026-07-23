---
status: draft
created: 2026-07-23
tags: [design, admin, ui, bff, razor, mvc, step-up]
---

# Admin App (detailed design)

## Purpose and scope

The administration front end (`Nami.Identity.Admin.App`): an MVC Razor **BFF** that
consumes the [Admin API](12-admin-api.md). It holds **no business logic** and never talks to
managers or the database directly; it renders screens, carries the user's session, and proxies
to the API with a server-side token. The access token **never reaches the browser** (ADR-0029).

In scope: the OIDC client and session; the UI design (layout, navigation, components, paging,
theming, accessibility); the screen inventory; the step-up UX; the error/UX conventions; and
the front-end security (antiforgery, CSP, no-token-in-browser, back-channel logout).

Out of scope, referenced not redefined: the API surface, DTOs, CRUD semantics, dual-control
saga, RBAC, bootstrap, and break-glass ([Admin API](12-admin-api.md)); the step-up *enforcement*
and the challenge *page mechanics* (05 / 08); the BFF package internals (ADR-0029 / the BFF
design). The end-user login/consent UI (08) is a different application.

## Decisions realized

| Decision | What this design applies |
|---|---|
| ADR-0020 | The Admin App is a presentation-only MVC Razor BFF that consumes the Admin API; no business logic, no direct data access |
| ADR-0029 | Confidential-client BFF built on the shared `Nami.Identity.Bff` package; token stays server-side; antiforgery mandatory |
| ADR-0003 / ADR-0019 | Server-side session cookie; receives back-channel logout |
| ADR-0013 (ref) | Consumes the 401 step-up challenge (RFC 9470) during approvals |

## OIDC client and session

The App is a **confidential OIDC client of the IdP** (dogfooding): authorization code + PKCE,
`client_secret` migrating to `private_key_jwt`, scopes `openid profile admin-api`, exact-match
redirect URIs. The session is a `__Host-` HttpOnly/Secure/SameSite=Lax cookie over the
server-side session store (03); it receives back-channel logout (validating
`iss`/`aud`/`sid`/`events` with a `jti` replay guard) so an admin's IdP logout ends the console
session. Token management uses an OSS-permissive, provider-agnostic OIDC BFF
access-token-management library (Apache-2.0): the user's access and refresh tokens live
server-side and are auto-refreshed, and a typed `AdminApiClient` attaches the bearer to each
API call. Login requires the admin role and MFA; the current `acr`/`amr` are surfaced so the
UI can show the session's assurance level. It is built on the shared `Nami.Identity.Bff`
package (which must not depend on the `Admin.*` assemblies).

## UI design

- **Stack.** ASP.NET Core MVC + Razor views, Bootstrap 5, **minimal JavaScript** — standard
  form POSTs with antiforgery, no SPA framework. Changing the theme or CSS never touches the
  flow (the engine-decoupled principle from 08). A single `AdminApiClient` maps the API's
  ProblemDetails to UI errors and carries the ETag through edit round-trips.
- **Layout and navigation.** One `_Layout` with a left nav grouped by area (Clients, Scopes,
  Grants/Tokens, Users, Roles, Tenants, Branding, Approvals, Audit, Sessions), a top bar with
  the signed-in admin, the current assurance level, and a **tenant context** indicator; a
  breadcrumb; and a persistent "proposals awaiting me" badge. Global-admin vs tenant-admin nav
  is filtered by the caller's policies/grants (a tenant-admin sees only their subtree).
- **Lists.** Server-side paging and filtering (the API's `?page=&size=` + `X-Total-Count`);
  no client-side data grids pulling whole tables. Explicit filter controls (no free-form
  query). Each row exposes the actions the caller is allowed (others hidden, not just
  disabled).
- **Forms and components.** Reusable partials per resource. The Clients form renders
  Permissions/Requirements as **grouped checkboxes** (endpoints, grant types, response types,
  scopes, PKCE/PAR toggles) — never raw JSON. Every mutation is a confirm dialog; a
  proposal-generating action additionally requires a **justification** field. A **diff viewer**
  (the approval inbox) shows the proposed payload against current, with the `TargetETag` status.
  An `ETag`/`If-Match` is carried on every edit; a 409 offers reload-diff.
- **Theming.** By design the console uses its **own** theme (it is an operator console, not
  a tenant-facing surface), so per-tenant branding (`TenantBranding`) is *managed* here but
  not *applied* to the console itself. The Branding
  screen has a client-side **live preview** that renders a sample login card from the entered
  design tokens (never executing tenant CSS). The CSP stays strict (no `unsafe-inline`;
  CSS-variable-driven), reusing the `SecurityHeadersAttribute` posture from 08.
- **Accessibility and feedback.** Semantic HTML, labelled form controls, keyboard-navigable
  tables and dialogs; every result surfaces a correlation id for audit tracing; secrets and key
  material are never displayed (a newly created secret is shown once, not stored client-side).

## Screens

| Screen | Content | Note |
|---|---|---|
| Dashboard | health, audit chain-status badge, proposals awaiting me, key days-to-expiry | |
| Clients (Applications) | list/search per tenant; grouped-checkbox Permissions form; secret-rollover wizard (parallel keys, no downtime); CORS-origins editor | hardest screen |
| Scopes | CRUD + resources/audience | |
| Grants and Tokens | list by subject/client; single revoke; revoke-all → proposal | |
| Users | CRUD, lock/unlock, reset; **force-logout**; lifecycle **Invite** (+ pending-approval state), **Disable/Enable**, **Offboard** (dual-control, irreversible warning); **Passkeys panel** (metadata + remove-confirm) | lifecycle in 06 |
| Roles | CRUD + claims | |
| Tenants | registry; provision wizard (→ proposal, per-step status); **Suspend/Resume** (→ proposal, semantics warning); `Identifier` read-only post-provision; Memberships editor; Delegated-admin grant picker (capability + subtree + expiry; dangerous → proposal warning) | bodies in 13 |
| Branding | design-token form (colors/fonts), https-only logo URL (ProblemDetails validation), live preview | |
| Approval Inbox | proposals awaiting me / mine / history; diff + justification + `TargetETag`; **Approve = step-up**; Reject/Cancel | dual-control core |
| Audit viewer | taxonomy filter; chain-verify badge; controlled export (small filtered = direct, bulk = dual-control) | |
| Sessions | list / revoke server-side sessions | |
| Account/StepUp | re-auth / MFA challenge landing | reuses 08's page |

There is no IdentityProvider screen in v1 (dynamic per-tenant external IdP is v2, ADR-0034),
and no API-Resource/Identity-Resource screen (OpenIddict does not model them; audiences are
managed via Scopes).

## Step-up UX (RFC 9470)

When an API call returns **401** `WWW-Authenticate: ... acr_values="urn:nami:aal2|aal3"`, a
`StepUpChallengeHelper` branches on the 401, saves the return URL and the pending action, and
issues an OIDC challenge to the IdP with `acr_values`/`max_age` (and `prompt=login` if needed)
as a **top-level redirect** (never an iframe, ADR-0019). After MFA the App returns with a
higher-`acr` token (the token-management library picks it up) and retries the action. Dangerous
actions show a "requires AAL2/AAL3" hint before the button.

## Error and UX conventions

The `AdminApiClient` maps each ProblemDetails `code` to a message and action: a **transient 409**
(the data changed during an edit) offers "reload and re-diff"; a **`target_changed` 409** on a
post-approval execute is different — the proposal is now `Failed` (terminal, single-use), so the
UI shows the failure with `FailReason`/`FailDetail`, notifies the proposer (in-app + email), and
offers **re-propose only** (a new proposal pre-filled with a fresh `TargetETag` and a
`PriorProposalId` link) — never a reload-diff retry. `428` (missing `If-Match`) is a front-end
bug (auto-attach). `admin_requires_actor` should never occur from the App (only from
misconfigured tooling). Every mutation confirms; a proposal-generating one collects a
justification; the correlation id is shown for tracing.

## Security

- **Token never in the browser:** the BFF holds it server-side; a test asserts no
  `access_token` appears in any response.
- **Antiforgery** on every state-changing form POST (the server-rendered-form profile, distinct
  from the JS/SPA custom-header CSRF profile, ADR-0029).
- **Strict CSP** (no `unsafe-inline`), `SecurityHeadersAttribute` reused from 08; open-redirect
  guard (`IsLocalUrl`/allow-list) on every `returnUrl`.
- **No secret/key display;** a newly created secret is shown once and not persisted
  client-side; the live-preview never executes tenant-supplied CSS.
- **Back-channel logout receiver** ends the console session when the IdP revokes it.

## Testing strategy

Playwright end-to-end (09 §9.3/9.4): login → propose → a **second** user approves with step-up
→ executed; the ETag-conflict UX; **no access token in any browser response**; the
invite/disable/offboard flows; passkey removal; a suspend→resume round trip; branding save +
preview + a rejected http/private-IP logo; and a back-channel-logout that ends the session.

## Open and build-time items

- The tenant-provisioning and offboard/erasure flows the wizards drive have their **bodies** in
  13; the App only renders their status.
- Localization of the admin console copy (shares the 08 i18n approach) is a build-time item.
- The BFF package split (`.Bff` / `.Bff.Yarp`) is finalized at M1 (ADR-0029/0027).
- The concrete OIDC BFF token-management package (Apache-2.0, provider-agnostic) is pinned in
  the implementation plan, not this design doc (which stays vendor-neutral), and its current
  status (maintained, not relocated/archived) is web-verified at that time (ADR-0021 spirit).

## References

- ADRs: ADR-0020 (admin architecture), ADR-0029 (BFF), ADR-0003 (sessions), ADR-0019
  (back-channel logout), ADR-0013 (step-up, referenced).
- Design docs: [Admin API](12-admin-api.md) (the backend it consumes), [08 login/consent/logout
  UI](08-login-consent-ui.md) (the `SecurityHeadersAttribute`, step-up page, open-redirect
  guard, i18n), [05 authorization](05-authorization.md) (step-up enforcement), [13 GDPR erasure
  and tenant provisioning] (the saga bodies the wizards drive).
- [Architecture](../architecture/README.md): containers (`Admin.App`), runtime view 2
  (dual-control with step-up).
- [Pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).
