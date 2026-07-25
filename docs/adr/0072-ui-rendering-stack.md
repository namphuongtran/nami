---
status: "accepted"
stack-record: true
date: 2026-07-25
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Microsoft Learn on Blazor hosting and deployment and on enforcing a Content Security Policy for Blazor, both verified at the .NET 10 version on 2026-07-25; ADR-0020 (the admin split), ADR-0031 and ADR-0041 (statelessness and the concurrency target), ADR-0039 and ADR-0040 (cache coherence and the Redis posture), ADR-0028 (native passkeys), ADR-0043 (the cookie and hardening invariants), ADR-0070 (terminate-and-forward)
informed: all contributors, via this repository
---

# Render the human-facing UI as server-rendered Razor with no client runtime: Razor Pages for login and consent, MVC Razor for admin, and Blazor deliberately deferred

## Context and Problem Statement

OpenIddict ships no user interface and, unlike the commercial engines, no interaction-service abstraction, so every page a human sees is Nami's to build. There are two distinct human-facing surfaces: the **end-user surface** (login, consent, logout, passkey enrollment, account management, error pages) and the **admin surface**.

ADR-0020 decided the admin surface: two projects, a REST API plus an MVC Razor backend-for-frontend, chosen so the token never reaches the browser. It never addressed the end-user surface. As a result the end-user rendering technology, Razor Pages plus Bootstrap 5, existed only inside a detailed design that is still a draft, and the ADR-0061 stack-of-record table listed it under a combined "Admin and login UI" row attributed to ADR-0020, which does not decide it. That was found and corrected on 2026-07-25 while writing the architecture layer, and it left a real gap: a load-bearing technology choice with no decision record.

The gap matters beyond bookkeeping, because the obvious modern alternative is Blazor and nothing recorded why Nami does not use it. A contributor will raise it, and "we always did it this way" is not an answer. The question this ADR settles is therefore: what renders each human-facing surface, and specifically, is Blazor appropriate for an authorization server's login page?

## Decision Drivers

* **The login page is the highest-value target in the system.** It is where credentials are typed, it is the page attackers most want to inject script into or frame, and it is the page most often cloned for phishing. Its Content Security Policy should be as strict as the platform allows, not as strict as a framework permits.
* **Stateless horizontal scale with no session affinity is a stated invariant**, not a preference: the deployment view specifies a load balancer with no sticky session, and ADR-0031 makes statelessness Factor VI and VIII with session state externalized per ADR-0003.
* **Redis is an accelerator only** and must fail open, and the hot path carries no mandatory synchronous Redis hit (ADR-0039, ADR-0040). Any design that needs a shared backplane merely to render a page violates that posture.
* **Login, consent, and logout are one-shot form pages.** They live for seconds and need almost no client interactivity, so a component runtime has little to amortize.
* **The concurrency target is roughly 10k concurrent users** (ADR-0041), so per-user server-side resident state is a capacity question, not a detail.
* **Consumers must be able to rebrand without a build step**, because the reference host is meant to be turnkey (ADR-0027) and a theme that requires an npm or framework build is a barrier.
* **Progressive enhancement**: sign-in should not depend on a client runtime downloading and starting successfully, including in locked-down enterprise browsers.

## Considered Options

* Server-rendered Razor with no client framework runtime: Razor Pages for the end-user surface, MVC Razor for admin
* Blazor Server (interactive server rendering) for the end-user surface
* Blazor WebAssembly for the end-user surface
* Blazor static server-side rendering (.NET 8 and later) for the end-user surface

## Decision Outcome

Chosen option: **server-rendered Razor with no client framework runtime**. The fixed parameters are:

* **A. The end-user surface is Razor Pages**, with no client-side framework runtime: login, consent, logout, passkey enrollment, account management, and the error inventory, decoupled from the protocol engine by a thin interaction service.
* **B. The admin surface stays MVC Razor** over the backend-for-frontend, unchanged from ADR-0020. Carrying two server-rendered Razor technologies is accepted deliberately: the gap between MVC and Razor Pages is small (same runtime, same view engine, same security primitives), and it is far smaller than introducing a third rendering model.
* **C. Bootstrap 5 is the default CSS framework**, CSS-variable driven with no npm or Tailwind build step, so a consumer restyles by overriding variables. **Theming must never loosen the Content Security Policy**; a theme that would require `unsafe-inline` is rejected rather than accommodated. Tailwind remains an adopter's own choice, not a shipped default.
* **D. Blazor is deferred, not rejected in principle.** It is a reasonable future candidate for the **admin** surface, where interactivity is richer, sessions are long, users are internal and few, and session affinity is tolerable. It is not adopted for the **end-user** surface. The revisit trigger is in More Information.
* **E. No client-side JavaScript framework** on the end-user surface. Scripting is limited to what a capability genuinely requires (notably the WebAuthn browser API for passkeys), added as external files that the policy permits by source, never as inline script.

### Why Blazor is not used for the end-user surface

Three verified facts decide it, and none of them is a matter of taste.

* **Blazor Server requires session affinity, which Nami's topology forbids.** Interactive server rendering drives UI updates, event handling, and JavaScript calls over a SignalR connection, and Microsoft's hosting guidance requires session affinity on every platform it documents: Application Request Routing affinity on Azure App Service and IIS, sticky sessions on Azure Container Apps, and, for Kubernetes, an ingress annotated `nginx.ingress.kubernetes.io/affinity: "cookie"`. Nami's deployment view specifies the opposite, a load balancer with **no sticky session**. The alternatives are to enable affinity, which breaks a stated invariant, or to add a connection backplane, which makes a shared service mandatory for rendering a page and contradicts ADR-0040's accelerator-only Redis posture.
* **The circuit cost is real at Nami's target.** Microsoft's own planning baseline is approximately **250 KB per circuit** for a minimal application, offered as roughly **1.3 GB of server memory for 5,000 concurrent users** (about 273 KB per user). Extrapolated to the ADR-0041 target of roughly 10k concurrent users, that is on the order of **2.6 GB of resident server memory for circuits alone**, before any application state. For a server that deliberately externalizes session state (ADR-0003) so that nodes stay disposable, holding per-user server-side render state is paying twice for the thing the architecture spent effort removing.
* **Blazor's own documented Content Security Policy is looser than a Razor Pages page can be.** Client-side Blazor requires `'wasm-unsafe-eval'` in `script-src` for the runtime to function at all. Less obviously, the starting policy Microsoft documents for **server-side** Blazor in .NET 10 is also `script-src 'self' 'wasm-unsafe-eval' 'unsafe-hashes' 'sha256-...'` with `style-src https:`, because a Blazor Web App renders an inline event handler in its navigation component and an inline import map. Razor Pages requires none of `wasm-unsafe-eval`, `unsafe-hashes`, or a broadened `style-src`. On the one page in the entire product where the policy should be tightest, adopting Blazor means starting from a looser baseline and working back.

Blazor **static server-side rendering** (.NET 8 and later) is the honest middle option and is why this ADR says "deferred" rather than "rejected": it has neither a circuit nor a WebAssembly runtime, so the first two objections do not apply. It is not chosen because it buys nothing for one-shot form pages while still bringing a component model and an enhanced-navigation script to a surface that needs neither, and because the inline import map keeps the policy question alive. If the end-user surface ever becomes genuinely interactive, this is the option to evaluate first.

This decision does **not** rest on relative popularity. No adoption comparison between Razor Pages and Blazor is asserted here, because none was verified and none is needed: the three facts above are decisive on their own.

### Consequences

* Good, because the login surface can run a genuinely strict policy: no `unsafe-inline`, no `unsafe-eval`, no `wasm-unsafe-eval` in `script-src`, which is the strongest available posture on the product's most attacked page.
* Good, because nothing about rendering a page depends on session affinity, a backplane, or resident per-user server memory, so the nodes stay disposable exactly as ADR-0031 and ADR-0003 intend.
* Good, because sign-in degrades gracefully: the core flow is form posts and redirects, so it does not depend on a client runtime starting successfully.
* Good, because rebranding is CSS-variable overrides with no build step, which keeps the turnkey promise of ADR-0027.
* Bad, because the product carries two server-rendered technologies (MVC Razor for admin, Razor Pages for the end-user surface); accepted as the smaller cost, and both share the runtime, view engine, and security primitives.
* Bad, because rich client-side interactivity is not available on the end-user surface without revisiting this decision; accepted, because these pages do not need it.
* Bad, because Nami must map the native .NET 10 passkey endpoints itself, since they are not auto-mapped outside the Blazor template. This cost is already recorded in ADR-0028 and was already being paid before this ADR existed; naming it here makes it a known consequence rather than a surprise.

### Confirmation

* A test asserts the end-user surface's `script-src` contains none of `unsafe-inline`, `unsafe-eval`, or `wasm-unsafe-eval`, and that a theme cannot introduce them.
* A scale-out test runs the login and consent flow across multiple instances behind a load balancer with **no** session affinity, and it passes.
* The login and consent flow completes with client scripting unavailable, except for the passkey path, which needs the browser API by definition.
* Rebranding the reference host requires no npm, bundler, or framework build step.

## Pros and Cons of the Options

### Server-rendered Razor with no client runtime (chosen)

* Good, because it permits the strictest policy, needs no session affinity or backplane, holds no per-user server-side render state, and degrades gracefully.
* Good, because theming is variable overrides with no build toolchain, which suits a product consumers restyle.
* Bad, because it offers no rich interactivity and leaves the product with two server-rendered technologies.

### Blazor Server (interactive server rendering)

* Good, because it offers a rich component model with all logic on the server and no client runtime download.
* Bad, because it requires session affinity on every documented hosting platform, which contradicts the no-sticky-session deployment invariant; a backplane alternative contradicts the accelerator-only Redis posture; it holds roughly 250 KB of resident state per user, which is material at 10k concurrent users; and its documented starting policy needs `unsafe-hashes` and an inline-handler hash.

### Blazor WebAssembly

* Good, because rendering moves to the client and the server becomes a pure API.
* Bad, because it requires `'wasm-unsafe-eval'` in `script-src`, which is exactly the relaxation the login page should not make; it ships a runtime download onto the most latency-sensitive and first-impression page in the product; and it buys nothing for one-shot form pages.

### Blazor static server-side rendering

* Good, because it avoids both the circuit and the WebAssembly runtime, so the two strongest objections do not apply, and it is the natural upgrade path if the surface becomes interactive.
* Bad, because it adds a component model and an enhanced-navigation script to pages that need neither, and its inline import map keeps a policy exception in play for no functional gain today.

## More Information

* **Revisit trigger.** Re-open this ADR if any of the following becomes true: the end-user surface acquires a genuinely interactive requirement that form posts cannot serve; the deployment invariant changes so that session affinity becomes acceptable; Blazor static server-side rendering removes its remaining inline-script requirements; or the admin surface is rebuilt, in which case Blazor should be evaluated on its merits for that surface alone, since parameter D scopes this decision per surface rather than product-wide.
* **Evidence.** The Blazor session-affinity requirement (Azure App Service and IIS Application Request Routing, Azure Container Apps sticky sessions, and the Kubernetes ingress cookie-affinity annotation), the SignalR connection for UI updates, and the circuit-memory planning baseline of roughly 250 KB per circuit and 1.3 GB for 5,000 concurrent users were verified on Microsoft Learn's Blazor hosting and deployment guidance at the .NET 10 version on 2026-07-25. The `'wasm-unsafe-eval'` requirement for client-side Blazor and the documented server-side Blazor starting policy including `'unsafe-hashes'` and the navigation-component hash were verified on Microsoft Learn's Blazor Content Security Policy guidance at the same version and date.
* **Related decisions:** ADR-0020 (the admin split and the MVC Razor backend-for-frontend this ADR leaves unchanged), ADR-0029 (the backend-for-frontend package), ADR-0003 and ADR-0031 (externalized session state and the statelessness invariant this decision protects), ADR-0041 (the concurrency target the circuit-memory calculation uses), ADR-0039 and ADR-0040 (cache coherence and the accelerator-only Redis posture a backplane would violate), ADR-0043 (the cookie and hardening invariants the surface must satisfy), ADR-0028 (native passkeys, including the endpoints Nami maps itself), ADR-0013 (step-up, which the surface renders), ADR-0019 (the logout surface), ADR-0026 (Bootstrap and any future CSS dependency must be permissively licensed), ADR-0027 (the turnkey reference host that theming serves), and ADR-0061 (the stack-of-record row this ADR now owns).
* **Correction it closes.** ADR-0061's stack table previously described the end-user surface as MVC Razor and attributed it to ADR-0020. The row now cites this ADR for the end-user surface, so the stack table's rule that every entry has an owning ADR holds again.
* Authored 2026-07-25 for this repository, prompted by the gap the architecture layer exposed. Third-party technologies are named factually for identification; no commercial competitor is named.
