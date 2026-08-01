---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: the `form_post` response template read from the shipped `OpenIddict.Server.AspNetCore` assembly at the pinned 7.5.0 and compared against 7.4.0 (2026-08-01); the HTML Standard on the `noscript` element (2026-08-01); MDN on the CSP `style-src-attr` directive and on `X-Frame-Options` (2026-08-01); `Microsoft.AspNetCore.Watch.BrowserRefresh` in .NET SDK 10.0.301 (2026-08-01); ADR-0072 (the rendering stack that owns the policy's strictness), ADR-0043 (the startup self-check that enforces this), ADR-0014 and ADR-0019 (the iframe dependencies both already removed), ADR-0073 and ADR-0076 (the edge and application split this follows), ADR-0021 (the version-sensitive seam this adds)
informed: all contributors, via this repository
---

# Fix the browser-facing response-header baseline as three profiles, deny framing outright, and admit by hash the one inline script Nami does not write

## Context and Problem Statement

ADR-0072 parameter C owns the **strictness** of the Content Security Policy: "Theming must never
loosen the Content Security Policy", a theme requiring `unsafe-inline` is "rejected rather than
accommodated", and that ADR's consequences commit the login surface to "no `unsafe-inline`, no
`unsafe-eval`, no `wasm-unsafe-eval` in `script-src`". It does not fix a single directive value,
and it does not mention framing at all.

That left the concrete values with no owner, and three documents restated the strictness while
recording the gap: design [11](../design/11-login-consent-ui.md) section 7.4 applies a security
headers filter and defers the values, design [19](../design/19-observability-capacity-slo.md)
notes that "no ADR in this repository owns the policy values", and design
[20](../design/20-testing.md) section 10 carries it as an open item. A fourth,
[architecture 13](../architecture/13-security-architecture.md) section 7, records that the
directive values "are decided nowhere".

**The gap is not bookkeeping, because the obvious strict policy silently breaks the protocol.**
`response_mode=form_post` is supported (ADR-0043 reconciles the cookie invariants with it), and
its response is an HTML page the **engine** writes, not a Razor page Nami controls. Read from
the shipped assembly at the pinned version, that page is:

```html
<!doctype html>
<html>
<body>
<form name="form" method="post" action="{redirect_uri}">
<input type="hidden" name="{name}" value="{value}" />
<noscript>Click here to finish the authorization process: <input type="submit" /></noscript>
</form>
<script>document.form.submit();</script>
</body>
</html>
```

Two directives that any hardening guide would recommend each break it on their own. `script-src
'self'` blocks the inline script, and `form-action 'self'` blocks the POST, which is cross-origin
to the client's redirect URI by definition. Design 11 section 6.1 places the security-headers
stage **before** the OpenIddict middleware, so Nami's header does apply to this response.

**The `noscript` fallback does not rescue it.** The HTML Standard makes `noscript` depend on
whether scripting is *disabled for the document*, not on whether an individual script ran: it
"represents nothing if scripting is enabled, and represents its children if scripting is
disabled". A script blocked by policy returns early from its own preparation and does not change
the document's scripting state. So the user receives a **blank page** carrying a hidden form that
never submits, with nothing in the server log, on the last hop of a successful authorization.
That is a worse outcome than having no policy at all.

Two further questions looked like trade-offs and were not, which is why this ADR settles them
cheaply rather than deferring them again:

* **Nothing in Nami needs to be framable.** ADR-0014 de-scoped "front-channel logout plus
  `check_session_iframe`", and ADR-0019 records why: front-channel logout "depends on cross-site
  iframes and third-party cookies", and the same root cause "breaks silent `prompt=none` via an
  iframe (tenant-switch and silent renew)". ADR-0019 then dropped the front-channel iframe
  outright and made tenant-switch `prompt=none` "a top-level redirect rather than an iframe, so
  it survives cookie-blocking". Every consumer of framability was removed by decisions taken for
  unrelated reasons.
* **A nonce buys nothing on Nami's own pages.** ADR-0072 parameter E already limits scripting to
  "what a capability genuinely requires ... added as external files that the policy permits by
  source, never as inline script", so `script-src 'self'` needs neither a nonce nor a hash there.

So the question this ADR settles is: what are the concrete browser-facing response headers, on
which responses, and how does a policy strict enough to be worth having avoid breaking the one
response Nami does not render?

## Decision Drivers

* **The login page is the highest-value target in the system** (ADR-0072's first driver). Its
  policy should be as strict as the platform allows.
* **A policy that breaks the protocol is worse than no policy**, because its failure mode here is
  a blank page with a clean server log, which is the hardest class of defect to attribute.
* **Every exception is a door somebody later walks through.** A nonce on `style-src` does not
  loosen the policy on paper, but it creates a mechanism a theme or a consumer view override can
  reuse, which is the drift ADR-0072 parameter C exists to prevent.
* **It must be enforceable on the path where the consumer writes their own host.** ADR-0027
  parameter A ships a meta-package plus a fluent builder, so nothing guarantees a consumer calls
  any particular middleware; this is the same reasoning ADR-0076 parameter A gives for emitting
  HSTS from the application.
* **A header whose blast radius lands on the operator's infrastructure is the operator's**, stated
  as such rather than defaulted quietly (the ADR-0076 parameter B precedent).
* **An unauthenticated POST endpoint on an identity provider is a surface, not a feature.**

## Considered Options

* One policy applied uniformly to every response
* Three policy profiles, chosen by response class
* Push the browser-facing headers to the edge and have the application emit nothing
* A nonce-based policy, with a per-request nonce on `script-src` and `style-src`
* Ship `Content-Security-Policy-Report-Only` first and enforce in a later release

## Decision Outcome

Chosen: **three policy profiles, enforced from the first release, with no nonce anywhere and
exactly one hash.** One uniform policy cannot be both strict enough for the login page and
permissive enough for the engine-written `form_post` page, so it collapses to the weaker of the
two everywhere. Pushing the headers to the edge contradicts ADR-0073 parameter B's
complementary-layers position and fails on the direct-to-internet fallback ADR-0073 parameter C
defines. A nonce adds a reusable relaxation mechanism to buy something that is not needed. And
Report-Only-first is a tool for discovering the inline usage of an application that already
exists, which does not describe a product whose inline usage is zero by decision and whose source
tree is empty.

### A. The application emits the browser-facing headers itself, edge or no edge (binding)

They are registered by the fluent builder, so the meta-package path gets them without the
consumer knowing to ask. ADR-0073 parameter A enumerates six things the edge is expected to
carry, and **the only response header among them is HSTS**, which ADR-0076 parameter A already
places in the application's own pipeline for this exact reason. Every header this ADR decides is
absent from that list. This follows ADR-0076 parameter A exactly, including its reasoning that a
header two layers agree on costs nothing while a header only a misconfigured edge was supposed to
send costs everything.

The middleware's position is already fixed by design 11 section 6.1, before request localization
and before the OpenIddict middleware. That position is what makes parameter B necessary.

### B. Three profiles, chosen by response class (binding)

| Profile | Applies to | Policy |
|---|---|---|
| **UI** | the Razor Pages end-user surface and the MVC Razor admin surface (ADR-0072 parameters A and B) | `default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' https:; font-src 'self'; connect-src 'self'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'` |
| **Protocol HTML** | only the `form_post` authorization response | `default-src 'none'; script-src '<the hash in D>'; form-action <the validated redirect URI>; frame-ancestors 'none'; base-uri 'none'` |
| **API** | the versioned admin API (ADR-0090), the token, userinfo, discovery, and JWKS endpoints, and the introspection and revocation endpoints ADR-0048 isolates | `default-src 'none'; frame-ancestors 'none'; base-uri 'none'` |

The API profile is not decoration, though it is defence in depth rather than a fix for a known
hole: a browser can be navigated directly to any of these responses, and `default-src 'none'` on a
response that should never load or execute anything costs nothing to state and closes the case
where a content type is wrong or a value is reflected.

### C. Framing is denied outright, on every profile (binding)

`frame-ancestors 'none'` in the policy, plus `X-Frame-Options: DENY` as the companion for agents
that predate `frame-ancestors`. Nothing in Nami is designed to be framed (the two decisions quoted
in the Context), so this costs no capability today.

**A per-tenant framing allowlist is rejected rather than deferred**, for four reasons that a later
reader should not have to rediscover:

1. It would force `X-Frame-Options` to be dropped for **every** tenant, including tenants that
   never embed. The header cannot express an origin allowlist at all: its only other values are
   `DENY` and `SAMEORIGIN`, which both contradict one, and `ALLOW-FROM` is obsolete in a way that
   is worse than unsupported, since a modern browser meeting it "will ignore the header
   completely". So the failure mode of trying is not a weaker allowlist but no anti-framing
   protection from that header for anyone.
2. It converts a security header into per-tenant data an administrator types, so a stale or
   over-broad entry becomes clickjacking on the page where passwords are entered. It would then
   need its own validation, its own audit trail, and its own tenant-isolation tests.
3. Embedded login is an anti-pattern of the redirect-based model Nami implements: a user typing
   credentials inside a frame whose surrounding page the relying party controls has no way to
   verify what they are typing into.
4. It may silently disable passkeys. Whether `navigator.credentials.get()` in a cross-origin
   frame requires the parent to delegate a permissions policy was **not verified** when this ADR
   was written, and that question is part of the revisit trigger rather than an assertion here.

`frame-ancestors 'self'` is also rejected: nothing needs same-origin framing either, so it is
parameter C with one unused hole.

### D. No nonce anywhere, and exactly one hash (binding)

* **`script-src 'self'` on the UI profile, with no nonce and no hash.** ADR-0072 parameter E
  already forbids inline script on Nami's own pages, so there is nothing to admit.
* **`style-src 'self'` on the UI profile, with no nonce and no hash.** The per-tenant theme is
  served as a **stylesheet response**, not emitted as an inline `<style>` block. `ThemeJson`
  carries design tokens rather than raw CSS (design 11 section 5.5), so the tokens become CSS
  custom properties in a cacheable response keyed by tenant and by a theme version that the
  existing `admin_config_change` event on a branding change already gives something to bump. This
  is ADR-0072 parameter E's own rule ("external files that the policy permits by source") applied
  to style rather than a new principle. The **stylesheet** is what becomes cacheable, not the page:
  parameter F puts `no-store` on the login page itself, so an inline block would have been fetched
  again on every render while a served theme is fetched once per tenant and theme version.
* **The one hash is the engine's `form_post` submit script**, on the Protocol HTML profile only,
  because Nami does not render that page and has no way to place a nonce in it. At the pinned
  version the script's text is `document.form.submit();`, whose SHA-256 is
  `sha256-j7OoGArf6XW6YY4cAyS3riSSvrJRqpSi1fOF9vQ5SrI=`.
* **That value is computed at build time from the pinned package and asserted, never
  hard-coded.** The measurement in More Information shows only that two versions agree; it says
  nothing about the next one, and a single whitespace change would invalidate it.
* **Client-side style must be applied through CSS properties, never through the `style`
  attribute.** The admin branding screen has a live preview that renders a sample card from
  tokens being typed (design [16](../design/16-admin-app.md) section 5.2), which cannot be a
  served stylesheet because the values are not yet saved. `element.style.setProperty(...)` is not
  governed by the policy, while `element.setAttribute('style', ...)` and
  `element.style.cssText = ...` are both blocked by it. The last of those is the trap, because
  assigning `cssText` is the natural way to apply a whole token set at once. This distinction is
  documented at MDN rather than stated in the specification text, so it also carries a browser
  test under Confirmation.

### E. `form-action` is set per response, to the redirect URI resolved for that request (binding)

On the Protocol HTML profile, `form-action` names the actual destination of that one response,
and it is drawn from the `redirect_uri` the engine resolved for the request rather than from
anything in the response body. That is **stricter** than omitting `form-action`, which is the
other way to keep `form_post` working, and it is why this profile does not weaken the directive
but narrows it to a single URI.

The strictness depends on one property of the engine that is **not asserted here**: that an
unregistered `redirect_uri` never reaches this response in the first place. ADR-0035 fixes the
registration side of that, but only for **self-service** registration and in weaker terms than
this parameter needs, since it requires a `redirect_uri` that is "https, no wildcard or loopback
abuse, a per-client allow-list" and leaves the concrete policy for Security to ratify. The
"exact-match" wording used elsewhere in this repository is design
[11](../design/11-login-consent-ui.md) section 7.3's, not ADR-0035's. So the engine's own
rejection of an unregistered value is a verify-before-build item under Confirmation rather than a
claim made here.

On the UI profile `form-action 'self'` is correct, because every form on those pages posts back
to Nami.

### F. The rest of the browser-facing set (binding, with one build-time item)

| Header | Value | Why |
|---|---|---|
| `X-Content-Type-Options` | `nosniff`, all profiles | a JSON or text response must never be sniffed into HTML |
| `X-Frame-Options` | `DENY`, all profiles | the companion to parameter C |
| `Referrer-Policy` | `no-referrer`, all profiles | an authorization request URL carries `client_id`, `state`, `redirect_uri`, and any `login_hint`. `strict-origin-when-cross-origin` would still disclose the tenant's issuer host to every image and stylesheet origin, and no Nami surface needs a referrer |
| `Cross-Origin-Opener-Policy` | `same-origin` on the UI profile | severs the opener relationship for the pages where credentials are entered |
| `Cache-Control` | `no-store` on any response carrying credentials, tokens, or authenticated content | recorded here so the browser-facing set has one home rather than being implied by each design |
| `Permissions-Policy` | **deny by default**; the enumerated allowlist is a build-time item | the posture is binding and matches parameter C: features are not delegated to embedded content. The concrete feature list depends on what the passkey path needs (ADR-0028) and is not fixed here, because guessing feature identifiers is exactly the kind of unsourced precision this repository treats as a defect |

`img-src` is `'self' https:` rather than `'self'` because tenant and client logos are external
https URLs, validated https-only with an SSRF and mixed-content guard on render (design 11
sections 4 and 5.2). Proxying logos through Nami would allow `img-src 'self'`, which is stricter,
at the cost of widening exactly the SSRF surface that guard exists for. That trade is a revisit
trigger, not a v1 default.

### G. Development differs in `connect-src` only (binding)

This is deliberately unlike the usual advice to relax a policy for local development, and the
measurement in More Information is why it is affordable. The .NET hot-reload middleware injects
an **external, same-origin** `<script src="...">`, which `script-src 'self'` already permits, and
it carries no nonce of its own. What it needs is the WebSocket its injected script opens back to
the watch server, which listens on a different port. So Development adds that origin to
`connect-src` and **changes nothing else**: `script-src` and `style-src` stay identical to
Production, which means the policy that ships is the policy that was developed against.

### H. Report-Only is an operator mechanism, not a rollout stage, and Nami ships no collector (binding)

The enforced policy is on from the first release. Report-Only remains available and is **off by
default**, emitted with `Reporting-Endpoints` only when the operator configures a report URI
through the ordinary configuration path whose precedence ADR-0031 fixes. Its two real uses are
long-lived rather than transitional: an adopter discovering that their theme or Razor view
override violates the policy, and Nami trialing a future tightening alongside the enforced
baseline, since both headers may be sent at once.

**Nami does not ship a report collector, and the report endpoint is the operator's URI.** An
unauthenticated POST endpoint that any browser can target is an amplification surface that would
have to be brought under ADR-0040's overload protection, and the reports carry the URL the user
was viewing, so they are a new data path this ADR does not classify against ADR-0022. This follows
the same split ADR-0076 parameter B applied to HSTS `preload`: a parameter whose blast radius
lands on the operator's infrastructure is stated as the operator's rather than defaulted.

### I. Enforcement (binding)

Two rows are added to ADR-0043's startup self-check table, in its existing "executable
enforcement of a decision owned elsewhere" category:

| Invariant | Assertion |
|---|---|
| `csp-no-relaxation` | the UI profile's `script-src` and `style-src` carry none of `unsafe-inline`, `unsafe-eval`, `wasm-unsafe-eval`, or `unsafe-hashes`, and no `nonce-` source, in every environment including Development |
| `framing-denied` | every profile sets `frame-ancestors 'none'` and `X-Frame-Options: DENY` |

Both are startup assertions rather than tests alone for the reason ADR-0076 parameter B gives:
every value here is overridable by the consumer through configuration, so what has to be defended
is not the default but the running instance.

**The `form_post` hash is deliberately not a startup row.** A startup check cannot issue an
authorization request, so it cannot see the response the hash exists for. It is a contract test
instead, and the engine-written template it depends on is a version-sensitive seam of exactly the
class ADR-0021 requires to fail CI before production. Recording that seam in the catalogue
([22](../design/22-openiddict-seam-catalogue.md)) is a build follow-up of this ADR.

### J. What is deliberately left open

* The enumerated `Permissions-Policy` allowlist (parameter F), and the acceptance of `img-src`
  admitting any https origin rather than proxying logos (parameter F). Both are a single entry on
  the [Pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md) under Security, because
  a deferral with no owner is how the policy values stayed unowned for a month in the first place.
* Whether WebAuthn in a cross-origin frame needs a delegated permissions policy. This was **not
  verified** when this ADR was written. It does not affect anything decided here, because
  parameter C denies framing outright; it becomes a blocking question only if parameter C is
  reopened, and the revisit trigger says so.

### Consequences

* Good, because the login page runs the strictest **script and style** policy the platform allows:
  no nonce, no hash, no `unsafe-*` source, and `default-src 'none'` as the floor. `img-src` is the
  one directive that is not at its tightest, and it is called out below rather than absorbed here.
* Good, because the one response that a strict policy would have broken is now the one response
  with its own profile, and the profile is *tighter* than a permissive compromise would have been,
  since it names both the exact script and the exact form destination.
* Good, because a failure mode that produces a blank page and a clean server log was found before
  any code existed, rather than during a penetration test or by an adopter.
* Good, because a theme has no nonce mechanism available to reuse, which is the drift ADR-0072
  parameter C forbids in prose and this ADR now forbids structurally: there is nothing to reuse.
* Good, because the policy developed against is the policy that ships: only `connect-src` differs
  in Development.
* Bad, because three profiles are more to build and to test than one policy, and a response served
  under the wrong profile is a defect that a single-policy design could not have. Accepted, and it
  is why Confirmation asserts a profile per response class rather than asserting a policy exists.
* Bad, because the `form_post` hash couples Nami to a string inside a dependency's response
  template, which can change on any bump. Accepted, because the coupling is made visible: it is
  computed from the pinned package, asserted by a test, and registered as an ADR-0021 seam. The
  alternative, omitting `script-src` on that response, is a permanent hole instead of a
  version-tracked one.
* Bad, because `img-src` admits any https origin in v1, so a compromised logo host can serve
  images into the login page. Accepted as bounded (images cannot execute) and tracked as a revisit
  trigger.
* Neutral, because parameter C forecloses embedded login. Nothing today uses it, and the revisit
  trigger states what reopening it would cost.

### Confirmation

* A test asserts the exact policy of each of the three profiles on a representative response of
  each class, and that no response carries a `nonce-` source.
* **A browser test drives a real `response_mode=form_post` authorization to completion** under the
  enforced Protocol HTML profile. Its negative case asserts that the same response under the UI
  profile produces no navigation, so the blank-page failure this ADR exists to prevent is itself
  covered by a test rather than only described.
* The hash is computed from the pinned package at build time and a test fails if it differs from
  the value the policy carries, which also catches the case where a bump changes the template.
* A test asserts the admin branding preview applies tokens through `setProperty`, and that neither
  `cssText` nor `setAttribute('style', ...)` appears in the shipped script.
* A test asserts the per-tenant theme stylesheet is tenant-scoped, including its cache key, under
  the tenant-isolation suite.
* Startup fails when either ADR-0043 row is violated, including in Development.
* **Verify-before-build:** confirm at the pinned version that the engine rejects an unregistered
  `redirect_uri` before any `form_post` response is written, which is the property parameter E's
  per-response `form-action` rests on. Tracked with the other pinned-version reads under ADR-0021.
* These tests carry their OWASP ASVS requirement identifiers, per ADR-0062.

## Pros and Cons of the Options

### One policy applied uniformly to every response

* Good, because it is the least to build, the least to get wrong per response, and the easiest to
  state in documentation.
* Bad, because it cannot be both strict enough for the login page and permissive enough for the
  engine-written `form_post` page, so in practice it becomes the weaker one everywhere: an
  `unsafe-inline` or a broad `form-action` applied to the page where credentials are typed.

### Three profiles chosen by response class (chosen)

* Good, because each response class gets the tightest policy that class can carry, and the
  exception is confined to the one response that needs it.
* Bad, because it adds a routing concern (which profile applies) that can itself be wrong, and it
  triples what the tests must cover.

### Push the browser-facing headers to the edge

* Good, because it costs the application nothing and an edge can apply headers uniformly.
* Bad, because ADR-0073 parameter A does not list browser-facing headers among the edge's
  responsibilities, parameter B holds that the layers are complementary rather than alternatives,
  and parameter C's direct-to-internet fallback would then have no policy at all. An edge also
  cannot know which `redirect_uri` the engine resolved for a given request, so parameter E is not
  expressible there.

### A nonce-based policy on `script-src` and `style-src`

* Good, because it admits inline content without `unsafe-inline`, and it is the conventional
  answer for a server-rendered application.
* Bad, because Nami has no inline content of its own to admit (ADR-0072 parameter E), so it buys
  nothing while leaving a relaxation mechanism that a theme or a view override can reuse, and it
  puts a per-request value into responses that would otherwise be identical. It also cannot reach
  the one page that does need an exception, because the engine writes that page.

### Report-Only first, then enforce

* Good, because it cannot break anything, and it is the correct migration path for an existing
  application whose inline usage is unknown.
* Bad, because it describes a situation Nami is not in: there is no code yet, and the inline usage
  is zero by decision with a test already asserting it (ADR-0072's Confirmation). Its value also
  depends on somebody reading the reports, which means a collector, which is a surface an identity
  provider should not add for a transitional benefit.

## More Information

* **Revisit trigger.** Re-open this ADR if a genuine requirement to embed a Nami surface in
  another origin appears. Reopening means deciding three things **together**, not relaxing one
  directive: how a framing allowlist is validated and audited, that `X-Frame-Options` is dropped
  product-wide as a consequence, and whether WebAuthn still functions in a cross-origin frame
  (the unverified question in parameter J). Separately, re-open if logos become proxied, so
  `img-src` can drop `https:`.
* **Evidence, and the class of each source.**
  * The `form_post` template was read from the shipped assembly
    `lib/net10.0/OpenIddict.Server.AspNetCore.dll` at **7.5.0**, the pinned version (ADR-0061),
    obtained from `api.nuget.org` on 2026-08-01, and compared against **7.4.0** from the local
    package cache. The two assemblies differ (SHA-256 prefixes `be3708cf6e8e82ea` and
    `417988be70cfd668`, with the literals at different offsets), and the template, the
    `<noscript>` text, and the script literal `<script>document.form.submit();</script>` are
    identical in both. This is a machine-readable source of the strongest class available here,
    and it still supports only the claim that these two versions agree.
  * The `noscript` behaviour is quoted from the **HTML Standard**, verified 2026-08-01. The
    conclusion that a policy-blocked script does not show `noscript` content follows from the
    element depending on the document's scripting state while the policy check returns from that
    script's own preparation. It is a specification reading, so it is also a test.
  * The CSSOM distinction in parameter D (`setProperty` permitted, `setAttribute('style', ...)`
    and `cssText` blocked) is documented at **MDN**, verified 2026-08-01. It is **not** stated in
    the specification text: the CSP Level 3 living document was read on the same date and contains
    no note about the CSSOM either way. Documentation is a weaker class of source than a
    specification, which is why parameter D carries a browser test rather than resting on the
    reading.
  * The `X-Frame-Options` limitation in parameter C is quoted from **MDN**, verified 2026-08-01:
    the header accepts only `DENY`, `SAMEORIGIN`, and the obsolete `ALLOW-FROM`, and of the last
    it says a modern browser "will ignore the header completely". Documentation again rather than
    specification text, and it is load-bearing only for a rejected option.
  * The Development finding in parameter G was read from
    `Microsoft.AspNetCore.Watch.BrowserRefresh.dll` in .NET SDK **10.0.301** on 2026-08-01: the
    injected markup is `<script src="..."></script>`, the assembly contains no `nonce` literal,
    and the embedded injection script opens `new WebSocket(url, protocol)` against the watch
    server's own address.
  * **The design corpus fixes no values.** It names these headers in four places and defers the
    policy every time, and the document it defers to carries only a checklist row naming the same
    three headers again. So this ADR is original to this repository rather than an import, and
    there was no corpus value to copy.
* **Related decisions:** ADR-0072 (parameter C, which owns the policy's strictness, and parameter
  E, whose no-inline rule this ADR applies to style as well as script), ADR-0043 (the startup
  self-check that carries the two invariants in parameter I, and whose cookie row already
  reconciles with `form_post`), ADR-0014 and ADR-0019 (the de-scoped `check_session_iframe` and
  the dropped front-channel iframe, which together are why parameter C costs no capability),
  ADR-0073 (the edge posture whose parameter A does not list browser-facing headers, and whose
  parameter C fallback is why the application must emit them), ADR-0076 (the precedent this ADR
  follows three times: the application emits the header itself, operator-owned parameters are
  stated as such, and enforcement lands in ADR-0043), ADR-0021 (the seam mechanism the `form_post`
  hash is registered under), ADR-0027 (the meta-package path where a consumer writes their own
  host), ADR-0031 (the configuration precedence a consumer overrides through, and which is why
  the invariants guard the instance rather than the default), ADR-0035 (the self-service
  registration guardrail on `redirect_uri` that parameter E relies on, and whose limits parameter
  E states), ADR-0048 (the introspection and revocation endpoints the API profile covers),
  ADR-0028 (the passkey path whose needs parameter F's allowlist depends on), ADR-0040 (the
  overload protection a report collector would have to enter), ADR-0090 (the versioned API base
  path the API profile covers), and ADR-0062 (the ASVS baseline the tests roll up to).
* **What this closes.** Four documents recorded this gap and each becomes wrong when this ADR is
  accepted: design 11 section 7.4 ("applied by this design on no recorded decision"), design 19
  ("no ADR in this repository owns the policy values"), design 20 section 10 (the open item), and
  architecture 13 section 7 ("decided nowhere"). Reconciling them is the companion change to this
  one and is deliberately separate, so this ADR can be read before the layers that cite it move.
* Authored 2026-08-01 for this repository, prompted by the ownership gap the design and
  architecture layers had recorded in four places. Third-party technologies and specifications are
  named factually for identification; no commercial competitor is named.
