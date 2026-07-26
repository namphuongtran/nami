---
status: "accepted"
date: 2026-07-25
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Ops (the concrete edge stack and the trusted-proxy ranges are theirs to ratify); Microsoft Learn on configuring ASP.NET Core for proxy servers and load balancers, verified at the .NET 10 version on 2026-07-25, including Microsoft Security Advisory CVE-2018-0787 on unvalidated forwarded host headers; ADR-0040 and ADR-0042 (the in-application overload and abuse layers), ADR-0043 (the cookie invariants), ADR-0001 (host-based tenant resolution), ADR-0014 (mTLS terminate-and-forward), ADR-0070 (the terminate-and-forward topology)
informed: all contributors, via this repository
---

# Assume an L7 edge in front of the deployment, define the direct-to-internet fallback, and process forwarded headers only from trusted proxies

## Context and Problem Statement

Nami is internet-facing by nature: the authorization, token, and login endpoints must be reachable by browsers and clients, which makes them a standing target for credential stuffing, bot traffic, and volumetric attack. The reference deployment has always been shaped as though an L7 edge (a web application firewall, a content delivery network, or a reverse proxy) sits in front of it, and ADR-0070 already establishes terminate-and-forward as the topology, mirroring production, with production TLS operator-supplied.

That assumption was never written down. Until this ADR, the repository contained **no** statement of what the edge is expected to do, what happens when there is none, or what the application must configure to be correct behind one. Two consequences follow, and both are real rather than theoretical:

* **The reference host is consumed by third parties who self-host it** (ADR-0027). If the security responsibilities assumed to sit at the edge are never stated, an adopter deploying straight to the internet inherits them silently and unknowingly.
* **Behind a terminating proxy, an unconfigured application is silently wrong, not visibly broken.** When HTTPS is terminated at the edge and proxied onward over HTTP, the original scheme is lost and must be carried in a header, and ASP.NET Core does **not** process those headers by default outside the IIS out-of-process integration. Nothing fails loudly; the application simply computes from the wrong values.

This ADR records the assumption, the fallback, and the required configuration. It deliberately does **not** select an edge product.

## Decision Drivers

* The identity endpoints are the front door of every dependent application, so volumetric protection is not optional in a real deployment.
* ADR-0040 keeps rate-limiting (fairness, answered with 429) distinct from load-shedding (capacity, answered with 503). Neither is volumetric absorption, and neither pretends to be: an in-process limiter still terminates the connection it rejects.
* An adopter must be able to tell which posture they are in, and choose it consciously rather than by default.
* Getting forwarded headers wrong degrades three separate controls quietly, so the failure mode must be documented rather than discovered.
* The edge product is an operator's infrastructure choice and must not be dictated by an open-source identity provider.

## Considered Options

* Leave the edge assumption implicit, as it was
* State the assumption, define the direct-to-internet fallback, and pin the forwarded-header requirements, without naming an edge product
* Require an L7 edge as a supported-configuration precondition
* Implement the edge's protections inside the application

## Decision Outcome

Chosen option: **state the assumption, define the fallback, and pin the forwarded-header requirements**. Requiring an edge is rejected because Nami cannot dictate a consumer's topology and it would make a perfectly legitimate small or on-premises deployment non-conformant. Implementing the protections in-process is rejected as the wrong layer: a web application firewall in application code duplicates a mature control badly, and volumetric traffic must be dropped before it reaches the process, not after. The fixed parameters are:

* **A. The reference deployment assumes an L7 edge**, expected to carry: TLS termination policy with a modern floor and cipher set, HTTP Strict Transport Security consistent with the application's own settings, IP reputation and bot filtering, geographic and per-IP velocity rules, request body and header size caps, and L7 denial-of-service absorption with TLS offload.
* **B. The edge and the application are complementary layers, not alternatives.** The edge absorbs **volumetric** traffic and blocks known-bad sources before the process is reached. The application enforces **per-user and per-client fairness** (ADR-0040) plus lockout, challenge, and anti-automation (ADR-0042). Removing either does not leave the other sufficient, and neither is a substitute for the other.
* **C. The direct-to-internet fallback is explicit.** With no edge, the responsibilities do not disappear, they relocate: to Kestrel's own limits (maximum request body size, header count and size limits, concurrent connection limits, and request timeouts) plus the in-application rate limiting and lockout of ADR-0040 and ADR-0042, at a **materially lower** volumetric ceiling. This statement, with a hardening checklist, must appear in the reference-host deployment documentation so an adopter chooses knowingly.
* **D. Forwarded headers are processed, and only from trusted proxies.** Where the application runs behind a terminating proxy, forwarded-header processing must be enabled explicitly and restricted to known proxies or networks, and it must run early in the pipeline, before HSTS and before anything that depends on the scheme or the client address. The forwarded host must be validated against known-good values rather than trusted as received.
* **E. The concrete edge stack is not named here.** It is an operator infrastructure choice, ratified by Ops together with the trusted-proxy ranges that parameter D depends on.
* **F. The mTLS endpoint is a carve-out.** Sender-constrained mTLS (ADR-0014) requires either terminate-and-forward, where the edge validates the client certificate and passes it on a header that only a trusted proxy may set, or TLS passthrough to the application. This is the one place where the edge is inside the trust boundary rather than in front of it, which is why ADR-0014 already defers the trusted-proxy IP list to a Security and Ops sign-off.

### Why parameter D is load-bearing

Forwarded-header processing is not a tidiness item. Because ASP.NET Core does not enable it by default outside the IIS out-of-process integration, and because the default option set forwards nothing when the middleware is enabled without options, the natural failure is a deployment that appears healthy while three controls are quietly wrong:

* **The scheme is wrong, so the cookie invariants break.** ADR-0043 enforces that the core session and correlation cookies carry `Secure` and a `__Host-` or `__Secure-` prefix. Those depend on the request being seen as HTTPS. Proxied over HTTP with no forwarded scheme, the application believes it is serving plain HTTP, and the invariant it was built to guarantee is defeated by deployment topology rather than by code. Issuer and redirect URI generation compute from the same wrong scheme.
* **The client address is wrong, so per-IP defenses collapse into a global one.** With no forwarded client address, every request appears to originate from the proxy. Per-IP rate limiting and the anti-automation signals of ADR-0042 then see one client for the whole internet, which converts a per-attacker limit into a shared limit that legitimate users trip first. This is worse than having no limit, because it degrades into a self-inflicted denial of service under exactly the attack it was meant to stop.
* **The host is attacker-influenced, which reaches tenant isolation.** Nami resolves the tenant from the host or path and never from a token claim (ADR-0001). A forwarded host header that is trusted without validation is therefore an input to tenant resolution, and Microsoft's own guidance flags an elevation-of-privilege advisory (CVE-2018-0787) for systems where the proxy does not restrict host headers to known-good values. Trusting it blindly turns a header into a tenant selector.

The trust restriction is the other half: forwarded headers are attacker-supplied unless a trusted proxy is the only party that can set them, which is why the framework's own IIS integration restricts itself to a single localhost proxy for precisely this reason.

### Consequences

* Good, because an adopter can now tell which posture they are deploying into, and the responsibilities that move in a direct-to-internet deployment are enumerated rather than assumed.
* Good, because the three quiet failure modes behind a proxy are documented, so they are checked at deployment instead of discovered during an incident.
* Good, because it names no product, so the decision holds across clouds and on-premises and does not constrain an operator's infrastructure.
* Good, because it makes explicit that the in-application controls of ADR-0040 and ADR-0042 were never intended as volumetric protection, closing a gap where someone might have assumed they were.
* Bad, because the security posture of a deployment now depends partly on infrastructure this project does not ship and cannot test in CI; mitigated by the deployment-documentation requirement and the Ops ratification, but not eliminated.
* Bad, because it adds a configuration surface that is easy to get wrong in a way that fails silently; mitigated by parameter D being a stated requirement with a startup-visible consequence rather than advice.
* Neutral, because this ADR carries no `stack-record` marker and adds no row to the ADR-0061 stack table: it commits to no technology. The edge is operator-chosen by design, and recording a non-choice as a stack entry would misrepresent the table.

### Confirmation

* The reference-host deployment documentation contains the edge assumption and the Kestrel-hardening fallback checklist, cross-referenced rather than duplicated.
* In a proxied deployment, the application reports the request as HTTPS and emits `Secure` and prefixed cookies, and the discovery document's issuer uses the external scheme and host.
* In a proxied deployment, per-IP rate limiting distinguishes two different clients arriving through the same proxy.
* Forwarded headers presented by a source outside the configured trusted proxies or networks are not honored.
* A direct-to-internet profile has the Kestrel body, header, connection, and timeout limits set to non-default values.

## Pros and Cons of the Options

### Leave the edge assumption implicit (status quo)

* Good, because it requires no work and no new document.
* Bad, because self-hosting adopters inherit unstated security responsibilities, the quiet forwarded-header failures stay undocumented, and the project cannot say what its own reference deployment assumes. This is the "silent gap" that the repository's conventions specifically reject.

### State the assumption, define the fallback, pin the forwarded-header rules (chosen)

* Good, because it makes the responsibility split explicit, stays product-neutral, and converts three latent misconfigurations into checkable requirements.
* Bad, because part of the posture rests on infrastructure outside this repository's control and testing.

### Require an L7 edge

* Good, because it would give one predictable, strong baseline.
* Bad, because an open-source identity provider cannot dictate a consumer's topology, and it would declare legitimate small and on-premises deployments unsupported for no proportionate gain.

### Implement the edge's protections in the application

* Good, because it would be self-contained and testable in CI.
* Bad, because it is the wrong layer: volumetric traffic must be dropped before the process, a reimplemented web application firewall is a weak one, and it would put Nami in the business of maintaining threat intelligence and bot signatures.

## More Information

* **Ratification (Ops, before production).** The concrete edge stack and its control configuration, and the trusted-proxy addresses or networks that parameter D and the ADR-0014 mTLS carve-out both depend on. Tracked in the [Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).
* **Provenance.** The edge posture and its control list were recorded in the design corpus's non-functional-requirements document as an explicit assumption at a pre-implementation review dated 2026-07-13, and were carried into this repository's architecture layer on 2026-07-25 as an assumption with no owning decision; this ADR is that decision. The forwarded-header behaviour was verified independently on Microsoft Learn's proxy and load-balancer guidance at the .NET 10 version on 2026-07-25: that a proxied HTTPS request loses its original scheme unless it is forwarded, that `X-Forwarded-Proto` sets the request scheme and `X-Forwarded-For` sets the remote address, that the middleware is not enabled by default outside the IIS out-of-process integration and forwards nothing if enabled without options, that it must run before HSTS, that the IIS integration restricts itself to a single localhost proxy because of spoofing concerns, and the CVE-2018-0787 advisory on unvalidated forwarded host headers.
* **Deliberately not decided here, and since closed.** The application's own transport-security settings (the HSTS parameters and the TLS floor Kestrel enforces when it terminates TLS itself) were not fixed by any ADR when this one was written: ADR-0043's invariants covered PKCE, signing, JWE, cookies, and degraded mode, but not transport. This ADR did not silently adopt them; it recorded the gap so it was a known open item rather than an assumed one. **ADR-0076 decided them on 2026-07-26**, which is what parameter A's "consistent with the application's own settings" now refers to and what parameter D's before-HSTS ordering now orders against.
* **Related decisions:** ADR-0040 (rate-limiting versus load-shedding, the in-application layer this complements), ADR-0042 (anti-automation and abuse defense, which depends on a correct client address), ADR-0043 (the cookie invariants a wrong scheme defeats), ADR-0001 (host-based tenant resolution, which a trusted host header feeds), ADR-0014 (mTLS terminate-and-forward and its trusted-proxy list), ADR-0070 (the terminate-and-forward topology, established for local development to mirror production), ADR-0027 (the reference host whose documentation must carry the assumption and the fallback), ADR-0041 (the availability target the edge protects), and ADR-0049 (per-tenant validation, which the isolation argument protects).
* Authored 2026-07-25 for this repository. Categories of edge product are described generically and no vendor is endorsed; standards, advisories, and framework APIs are named factually for identification.
