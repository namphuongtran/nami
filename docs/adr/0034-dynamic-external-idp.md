---
status: "accepted"
date: 2026-07-09
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security (ratifying the self-service threat model before the v2 build) and DPO (if an external IdP handles sensitive PII)
informed: all contributors, via this repository
---

# Open dynamic per-tenant external IdP federation as a v2 self-service, OIDC-only feature via a dynamic scheme provider

## Context and Problem Statement

ADR-0002 fixed v1 as a static external-IdP set at the host level (configured at boot, for example Entra ID or Google shared across the host) and explicitly deferred dynamic per-tenant IdP "until a real B2B need arises, and then build a dynamic scheme provider". A design skeleton was left (following the dynamic-identity-providers / federation-gateway pattern of a commercial identity server: an identity-provider store plus a dynamic scheme provider), unscheduled. The v2 need (Nam, 2026-07-09) is for a tenant to define, from the admin, which external IdPs they want to integrate with their identity — self-service "bring-your-own-IdP" B2B — rather than a host-level hard assignment. This is a v2 scope, not part of the decision-complete v1.

## Decision Drivers

* A real self-service B2B need: tenants define their own external IdPs.
* Do not overturn the v1 federation model or its security hardening.
* No-restart dynamic scheme registration.
* Handle the heightened threat model of a runtime, user-input Authority.

Inherited constraints that are not overturned: the integration model stays handler-based ASP.NET Core Identity external login (ADR-0002), so a dynamic IdP is still a dynamic `OpenIdConnectOptions` scheme fed into `SignInManager.GetExternalLoginInfoAsync()`, not the OpenIddict client stack; identity stays global with per-tenant membership (ADR-0001), so an external login provisions/links into a global identity and membership binds the tenant while the IdP config is per-tenant control-plane; and all v1 federation-security (SSRF hardening, RFC 9207 issuer anti-mix-up, anti-takeover linking by (provider, subject), a DENY-by-default external-claim allow-list, and secrets in the secret store) carries over and is tightened, because the Authority is now runtime user input.

## Considered Options

* Actor: delegated tenant-admin self-service; product-operator only; or a hybrid (tenant enters, operator approves).
* Protocol: OIDC-only; OIDC plus generic OAuth2; or SAML/WS-Fed.
* Runtime mechanism: a dynamic authentication scheme provider; a per-request proxy handler; or semi-dynamic config reload.

## Decision Outcome

Chosen: open v2 scope for a dynamic external IdP that is per-tenant, self-service, OIDC-only, using a dynamic scheme provider.

* **Actor = delegated tenant-admin self-service** (ADR-0010), which is the real B2B need and the heaviest threat model, so the design is built around it (the Authority is runtime user input). Operator-only would not be true self-service, and a hybrid adds workflow state without enough benefit for v2.
* **Protocol = OIDC-only** (discovery-based), because `/.well-known` auto-fetches the endpoints and JWKS, it covers essentially all enterprise IdPs (Entra ID, Okta, Google Workspace, Ping, Keycloak), and it reduces the SSRF surface to a single URL type. Generic OAuth2 is rare for B2B SSO and adds fields and claim-mapping complexity (rejected for v2), and SAML/WS-Fed is out of scope (a separate post-v1 effort with a dedicated SAML library).
* **Mechanism = a dynamic authentication scheme provider:** a `DynamicAuthenticationSchemeProvider` **decorates** (does not replace) the default, an `IConfigureNamedOptions<OpenIdConnectOptions>` is name-scoped (`oidc-{tenant}-{alias}`, early-returning for other names), and a custom `IOptionsMonitorCache` is invalidated by version, giving no-restart registration (matching ADR-0011); multi-node cache coherence follows the established mechanism. The framework `OpenIdConnectHandler` still handles the protocol, discovery, JWKS, and issuer. A per-request proxy handler was rejected as fighting the framework, and semi-dynamic config reload was rejected as not being real self-service from the database.
* **IdP config is global control-plane:** a `TenantIdentityProvider` entity with a `TenantId` foreign key, protected by two-layer tenant isolation (an EF named filter plus Postgres forced RLS). It is not placed in the OpenIddict context, because it is control metadata rather than an OpenIddict entity.
* **Secrets (ADR-0009):** a tenant-admin enters the secret, which is written to the secret store through the port while the database holds only a `ClientSecretRef`; it is never plaintext, the UI is masked and write-only, and it is audited.
* **Security (tightened from v1):** two-stage SSRF validation (at the admin API write-time and at options-build), a test-connection through an egress guard, and rate-limiting; RFC 9207 issuer plus a state-to-scheme binding; an anti-takeover cross-tenant gate (the hardest item); a DENY-by-default external-claim allow-list (a tenant-admin cannot map a security claim); and audit on every create/update/enable/secret-set (ADR-0008).
* **Spike-first:** spike A-8 must prove that a scheme resolves at runtime from the store, that a no-restart rebuild works, that two tenants are isolated, and that a callback reaches `GetExternalLoginInfoAsync` with the correct provider/subject, before the build begins.

Impact on v1 is additive and non-breaking if the design constraints hold (decorate rather than replace, name-scoped named options, a migration that only adds a table, and the new anti-takeover gate applying only to dynamic IdPs while the v1 static path is unchanged). The one real v1 touch-point is the login-page external-button enumeration: v1 shows the global external set, and v2 must show the static-global set unioned with the tenant's dynamic set while hiding other tenants' IdPs, handled through an `IExternalProviderQuery` seam that is deferred with an awareness note (one call-site changes when v2 lands). No breaking change forces a v1 fix now.

### Consequences

* Good, because it reaches commercial-grade federation-gateway parity on OSS, delivers real self-service B2B, and is no-restart.
* Good, because it reuses a great deal: the tenant isolation, the secret store (ADR-0009), the claim choke-point, the issuer/SSRF hooks, and the cache-coherence mechanism.
* Bad, because the threat surface grows with a user-input Authority, so the SSRF and anti-takeover defenses must be tight and spike A-8 runs first.
* Bad, because of the one login-page touch-point (deferred, with a solution and a placeholder).
* Bad, because the scheme-provider and options-cache machinery is significant new code.

### Confirmation

* OIDC discovery covers the mainstream enterprise IdPs (Entra ID, Okta, Google Workspace, Ping, Keycloak), matching a commercial server's in-memory OIDC-provider capability.
* v2-build follow-ups: run spike A-8 before the first phase; Security ratifies the self-service threat model (whether an operator host allow/deny list is needed for the SSRF guardrail) and the DPO is consulted if an external IdP handles sensitive PII; and it is confirmed whether the self-service secret path needs operator dual-control (currently a per-tenant secret is self-service with audit and the port, and is not treated as a global IAM change).

## Pros and Cons of the Options

### Actor

* **Delegated tenant-admin self-service (chosen)** — good, because it meets the real B2B need; bad, because it carries the heaviest threat model, which the design must center on.
* **Product-operator only** — good, because it is safer; bad, because it is not true self-service.
* **Hybrid (tenant enters, operator approves)** — good, because it balances the two; bad, because it adds workflow state.

### Protocol

* **OIDC-only (chosen)** — good, because discovery covers nearly all enterprise IdPs and minimizes the SSRF surface; bad, because it excludes the rare non-OIDC B2B case.
* **OIDC plus generic OAuth2** — good, because it is broader; bad, because it is rare for B2B SSO and adds manual-endpoint and claim-mapping complexity.
* **SAML/WS-Fed** — good, because some enterprises still use it; bad, because it is a separate, larger effort, so it is out of scope here.

### Runtime mechanism

* **Dynamic scheme provider (chosen)** — good, because it is no-restart and lets the framework handler own the protocol; bad, because it is real machinery and needs spike A-8.
* **Per-request proxy handler** — good, because it is conceptually simple; bad, because it fights the framework (swapping Authority/correlation per request) and is risky.
* **Semi-dynamic config reload** — good, because it is close to the v1 static model; bad, because it is not real self-service from the database.

## More Information

* Original decision 2026-07-09 (v2 scope-opened). This re-opens the dynamic per-tenant IdP that ADR-0002 deferred, inheriting the ADR-0002 handler-based model rather than overturning it, and it is the additive v2 feature referenced by ADR-0014.
* Related decisions: ADR-0001 (multi-tenant Pool/Silo, with the IdP config as per-tenant control-plane), ADR-0002 (the federation model this inherits and the deferral this re-opens), ADR-0008 (audit on every create/update/enable/secret-set), ADR-0009 (the secret store for the client secret), ADR-0010 (delegated admin, the self-service actor), ADR-0011 (the same no-restart hot-reload ethos), and ADR-0013 (acr/amr/step-up). The implementation mini-spec is a separate design document.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial identity server's federation-gateway feature and its in-memory-provider API were generalized; the enterprise-IdP examples (Entra ID, Okta, Google Workspace, Ping, Keycloak) and a dedicated SAML library are retained as neutral references; and the type and scheme names are Nami's own.
