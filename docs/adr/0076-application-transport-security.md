---
status: "accepted"
date: 2026-07-26
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Microsoft Learn "Enforce HTTPS in ASP.NET Core" and the Kestrel endpoint-configuration and middleware-ordering guidance at the .NET 10 version (verified 2026-07-26); RFC 6797; ADR-0073 (the edge posture that names this gap), ADR-0043 (the startup self-check that enforces it), ADR-0027 (the two distribution paths), ADR-0070 (local-development TLS), ADR-0001 and ADR-0025 (the per-tenant issuer subdomain that makes includeSubDomains consequential)
informed: all contributors, via this repository
---

# Decide the application's own transport security: HSTS policy, the Kestrel TLS floor, and the transport-requirement guard

## Context and Problem Statement

ADR-0073 settled the **edge**: an L7 edge is assumed, it carries a TLS termination policy and HSTS, forwarded headers are processed only from trusted proxies and must run before HSTS, and a direct-to-internet fallback is stated explicitly. It also recorded, deliberately, what it did not settle: "the application's own transport-security settings (the HSTS parameters and the TLS floor Kestrel enforces when it terminates TLS itself) are not currently fixed by any ADR: ADR-0043's invariants cover PKCE, signing, JWE, cookies, and degraded mode, but not transport."

That gap is load-bearing in two places inside ADR-0073 itself. Parameter A expects the edge to carry HSTS "**consistent with the application's own settings**", and parameter D requires forwarded-header processing to run "**before HSTS**". Both sentences presuppose an application HSTS policy that no decision defines.

It is tempting to answer "the framework already handles this". The ASP.NET Core project template does emit `app.UseHsts()` in its non-Development branch, so a scaffolded application gets the middleware. **That answer does not survive contact with how Nami ships.** ADR-0027 distributes Nami on two paths: a `Nami.Identity` meta-package with a fluent builder, where **the consumer writes their own host** and nothing guarantees `UseHsts()` is ever called, and a reference host plus a `dotnet new nami-identity` template, where it would be. On the path where it is called, what a consumer gets is the **framework default**, which is a 30-day `max-age` with `includeSubDomains` and `preload` off. That is a default, not a policy, and for an identity provider the difference matters.

Three of the parameters are not obvious, which is why this is an ADR and not a table row.

**`includeSubDomains` is unusually consequential for Nami specifically.** Tenants are resolved from the host, with a per-tenant issuer subdomain such as `acme.id.example.com` (ADR-0001), served under a wildcard certificate (ADR-0025). An operator running Nami at `id.example.com` who enables `includeSubDomains` binds HSTS not only to Nami's tenant subdomains, which is desirable, but to **every** sibling under `example.com`, which Nami does not own and may not be HTTPS-ready.

**`preload` is not Nami's to decide at all.** It is not part of RFC 6797, it requires `includeSubDomains` and a long `max-age`, and it is a submission to a list maintained by browser vendors, made **for the operator's domain**. A product default here would commit someone else's domain to a state that is slow and awkward to leave.

**The TLS floor pulls two ways.** Kestrel's `SslProtocols` defaults to `SslProtocols.None`, which Microsoft describes as causing Kestrel "to use the operating system defaults to choose the best protocol", with the guidance "unless you have a specific reason to select a protocol, use the default". Pinning `Tls12 | Tls13` gives a guaranteed floor on an old host but silently **excludes TLS 1.4** when it exists, turning a security setting into a future outage.

Separately, and found while reconciling this gap: OpenIddict exposes `DisableTransportSecurityRequirement`, which switches off its HTTPS requirement. It is exactly the class of dangerous toggle ADR-0043 exists to catch, it appears in this repository only in a deployment design note, and **no ADR forbids it**.

## Decision Drivers

* ADR-0073 already depends on an application HSTS policy existing; the alternative to deciding one is leaving two of its parameters undefined.
* An identity provider is HTTPS-only by construction, so its transport posture should not be inherited from a general-purpose default.
* Nami ships to operators. A default whose blast radius lands on the operator's domain is not Nami's to choose.
* A security setting must not become a future availability incident.
* Whatever is decided has to be enforceable on the path where the consumer writes their own host.

## Considered Options

* Leave it to the framework default and the template.
* Fix the parameters in the fluent builder's safe defaults, and enforce them at startup.
* Push the whole question to the edge and have the application emit nothing.
* Pin `SslProtocols` explicitly to a known-good set.

## Decision Outcome

Chosen: "fix the parameters in the builder's safe defaults, and enforce them at startup." The framework default is a default rather than a policy and is absent entirely on the meta-package path; pushing everything to the edge contradicts ADR-0073's own defence-in-depth position and fails on the direct-to-internet fallback it defines; and pinning the protocol set trades a present-day guarantee for a future break.

### A. The application emits HSTS itself, edge or no edge (binding)

`UseHsts` is part of Nami's own pipeline, not only of the template, and it is registered by the fluent builder so the meta-package path gets it without the consumer knowing to ask. This is deliberate duplication with the edge: ADR-0073 B already establishes that the edge and the application are complementary layers rather than alternatives, and a header that two layers agree on costs nothing, while a header that only a misconfigured edge was supposed to send costs everything.

The middleware's position in the pipeline is **not** decided here. ADR-0073 D already requires forwarded-header processing to run before it, for the reason that a `Strict-Transport-Security` header is only meaningful on a request the application believes is HTTPS.

### B. HSTS parameters (binding defaults, two of them operator-owned)

| Parameter | Nami default | Why |
|---|---|---|
| `max-age` | **One year** | Microsoft advises starting at hours-to-a-day when an application is adopting HTTPS "in case you need to revert the HTTPS infrastructure to HTTP". **That caution does not apply here**: an identity provider has no HTTP mode to revert to. The issuer is `https`, redirect URIs are `https`, and the session cookies carry `Secure`, which ADR-0043 already enforces. Deploying Nami over plain HTTP is not a supported state, so the rollback the short `max-age` protects does not exist |
| `includeSubDomains` | **Off**, operator opt-in | Nami is normally hosted **at** a subdomain, so enabling this reaches sibling domains the operator owns and Nami does not. Documented as recommended where the deployment owns the whole zone, which is the case Nami's own tenant subdomains fall into |
| `preload` | **Off**, and recorded as the **operator's decision, not Nami's** | Not in RFC 6797; it is a submission to a browser-vendor list for the operator's domain, and it presupposes `includeSubDomains`. A product cannot consent on an operator's behalf to a state that is difficult to leave |
| `ExcludedHosts` | framework defaults retained (loopback) | Removing them would break the local loop that ADR-0070 defines |
| Development | **HSTS not emitted** | Microsoft: HSTS "isn't recommended in development because the HSTS settings are highly cacheable by browsers". A cached year-long policy on `localhost` outlives the experiment that set it |

Every one of these is overridable by the consumer through the ordinary configuration path, whose precedence chart (environment over secret store over `appsettings.{Env}` over `appsettings`) ADR-0031 fixes. What is binding is the **default**, because the default is what an adopter who reads nothing will run.

### C. The TLS floor is asserted, not pinned (binding)

Kestrel's `SslProtocols` is **left at its default**, so the operating system continues to choose the best protocol and a future TLS version is available the day the host supports it.

The floor is enforced the other way round: where the application terminates TLS itself, a startup assertion **fails fast if `SslProtocols` has been explicitly configured to permit anything below TLS 1.2**. This gives the guarantee that matters, that Nami never negotiates a deprecated protocol version, without owning the list of acceptable ones. Where a proxy terminates TLS the assertion does not apply, because there is no TLS at the application to constrain; the floor is then the edge's, which ADR-0073 A assigns and Ops ratifies.

This is the "specific reason to select a protocol" Microsoft's guidance leaves room for, applied as narrowly as it can be: an identity provider must not negotiate TLS 1.0 or 1.1 even on a host whose defaults still allow them.

### D. The transport-security requirement may not be disabled outside Development (binding)

OpenIddict's `DisableTransportSecurityRequirement` is **forbidden in any environment other than Development**. It is not a tuning knob; switching it off removes the HTTPS requirement from an authorization server, which invalidates the assumptions behind ADR-0043's cookie invariants, ADR-0005's plain readable access token, and the `https` issuer every relying party pins.

This is stated here rather than left to the deployment design, because a rule that only a design document carries is a rule an ADR-level reader will not find. It is separate from access-token encryption, which ADR-0005 turns off deliberately and which is not a dangerous toggle.

### E. Enforcement (binding)

Three rows are added to ADR-0043's startup self-check table, in its existing "executable enforcement of a decision owned elsewhere" category:

| Invariant | Assertion |
|---|---|
| `hsts-enabled-outside-dev` | the HSTS middleware is registered and `max-age` is at least the product default, outside Development |
| `tls-floor` | where the application terminates TLS, no explicitly configured protocol below TLS 1.2 is permitted |
| `transport-security-required` | `DisableTransportSecurityRequirement` is off outside Development |

Putting them in ADR-0043 rather than inventing a second check keeps one place where the application refuses to serve, and matches how ADR-0004 and ADR-0005 already have their invariants enforced there.

### F. What is deliberately left open

The **edge's** TLS policy and cipher set stay where ADR-0073 A and E put them: an operator infrastructure choice ratified by Ops. This ADR governs only what the application does with its own transport, and the two must be consistent, which is what ADR-0073 A asks for and what this ADR finally makes checkable.

### Consequences

* Good, because the two ADR-0073 parameters that assumed an application HSTS policy now have one, so the edge has something concrete to be consistent with.
* Good, because the meta-package path gets the policy from the builder rather than from a template the consumer never used, which is the path where the gap was real.
* Good, because the parameters whose blast radius lands on the operator's domain are the operator's, stated as such rather than defaulted quietly.
* Good, because the TLS floor is guaranteed without pinning a protocol list that a future version would have to break.
* Good, because a dangerous toggle that only a deployment design mentioned is now a decision with a startup guard behind it.
* Bad, because a one-year `max-age` is unforgiving of an operator who misconfigures TLS at first deploy; accepted, because the state it locks out, serving an identity provider over plain HTTP, is not a supported state, and because `ExcludedHosts` and the Development exclusion keep the local loop clear.
* Bad, because `includeSubDomains` off by default means the common single-zone deployment is weaker than it could be until an operator opts in; accepted as the safer direction to be wrong in, since the opposite default reaches domains Nami does not own.
* Bad, because asserting a floor rather than pinning a set means the protocol list is still the operating system's; mitigated by the assertion catching the only case Nami can be blamed for, which is an explicit configuration that permits a deprecated version.

## Pros and Cons of the Options

### Framework default plus the template

* Good, because it costs nothing and covers the scaffolded case.
* Bad, because it covers only one of ADR-0027's two distribution paths, and on that path it supplies a general-purpose default rather than an identity-provider policy. It also leaves ADR-0073's "consistent with the application's own settings" pointing at nothing.

### Builder defaults plus startup enforcement (chosen)

* Good, because it reaches both distribution paths, states the operator-owned parameters as operator-owned, and lands the enforcement where the project already refuses to serve on a weakened configuration.
* Bad, because it adds three invariants to maintain; accepted, since the alternative is transport being the one part of the security posture with no owner.

### Push everything to the edge

* Good, because the edge is where TLS is terminated in the reference deployment anyway.
* Bad, because ADR-0073 B explicitly rejects treating the two layers as alternatives, and ADR-0073 C defines a direct-to-internet fallback in which there is no edge to push to.

### Pin `SslProtocols` to a known-good set

* Good, because the floor is then unambiguous and visible in configuration.
* Bad, because it excludes protocol versions that do not exist yet, converting a security setting into a scheduled break. Microsoft's own guidance is to use the default unless there is a specific reason.

## More Information

* **Why this ADR exists.** ADR-0073 named this gap in its own More Information on 2026-07-25 rather than adopting the settings silently, and the architecture layer carried it as the second of eight load-bearing claims with no owning decision. This ADR closes it. The `DisableTransportSecurityRequirement` rule was found in the same pass: the design corpus carries it as a non-functional requirement and as a CI configuration test, this repository had it only in a deployment design note, and no ADR forbade it.
* **External verification, 2026-07-26, on Microsoft Learn at the .NET 10 version.** The HSTS defaults are a 30-day `max-age` with `includeSubDomains` and `preload` off and loopback addresses in `ExcludedHosts`; `UseHsts` "isn't recommended in development because the HSTS settings are highly cacheable by browsers"; the initial-rollout guidance is to use a short `max-age` "in case you need to revert the HTTPS infrastructure to HTTP"; `preload` "isn't part of the RFC 6797 HSTS specification"; the project template emits `app.UseHsts()` in the non-Development branch; and Kestrel's `SslProtocols` default of `SslProtocols.None` "causes Kestrel to use the operating system defaults to choose the best protocol", with the advice to use the default absent a specific reason.
* Related decisions: ADR-0073 (the edge posture, the forwarded-header ordering this ADR does not restate, and the direct-to-internet fallback where the application's own floor is the only one), ADR-0043 (the startup self-check that carries the three invariants in section E), ADR-0027 (the two distribution paths that make the framework default insufficient), ADR-0031 (the config-precedence chart by which a consumer overrides these defaults), ADR-0052 (the safe-defaults-plus-override posture this follows, and where OpenIddict's deny-by-default verbosity is already recorded), ADR-0070 (local-development TLS, which the Development exclusion and the retained `ExcludedHosts` protect), ADR-0001 and ADR-0025 (the per-tenant issuer subdomain and its wildcard certificate, which make `includeSubDomains` consequential), ADR-0005 (the plain readable access token whose safety assumes transport security), ADR-0014 (the mTLS carve-out, where the edge is inside the trust boundary).
* Authored fresh for this repository. The design corpus states the edge-side policy ("TLS 1.2+, modern cipher suite, HSTS at the edge consistent with the app") and forbids `DisableTransportSecurityRequirement` in production, but records no application-side HSTS parameters and no Kestrel floor; those are decided here for the first time.
