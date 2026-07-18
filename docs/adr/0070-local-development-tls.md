---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ASP.NET Core dev-certs and Docker HTTPS guidance, mkcert, and local reverse-proxy TLS practice (verified 2026-07-18); ADR-0025 (the dev loop), ADR-0014 (terminate-and-forward), ADR-0043 (HTTPS and cookie invariants)
informed: all contributors, via this repository
---

# Serve HTTPS in local development with a locally-trusted cert behind a terminating reverse proxy

## Context and Problem Statement

An identity provider effectively requires HTTPS everywhere: the issuer must be `https`, OIDC redirect URIs are `https`, and the session and antiforgery cookies carry `Secure`, which the hardening invariants already enforce (ADR-0043). This is true at runtime and therefore also in local development, for both Nami's own inner loop and for anyone running the product locally. ADR-0025 settled how the dev stack runs (docker-compose plus Testcontainers plus `dotnet run`) and ADR-0014/0025 settled the mTLS client-certificate boundary and production tenant-subdomain TLS, but nothing settled the basic question: how does the IdP serve `https` locally, and how is that certificate trusted?

The trust question is the hard part and is specific to an IdP. Beyond the browser, an IdP makes back-channel server-to-server calls: the BFF calls the IdP (ADR-0029), and a resource server calls the JWKS and introspection endpoints (ADR-0048/0049). A self-signed certificate that only the browser trusts will make those back-channel calls fail TLS validation. So a local TLS setup must be trusted on both sides, the host and browser and the containers, or the flows silently break. This ADR fixes the local-development TLS approach as a documented standard, so it is neither folklore for the Nami team nor a stumbling block for adopters.

## Decision Drivers

* OAuth/OIDC needs HTTPS locally: `https` issuer and redirect URIs, `Secure` cookies (ADR-0043).
* The certificate must be trusted on both sides, because an IdP makes back-channel calls that fail on an untrusted cert.
* Framework-native first for the inner loop (ADR-0061): use the ASP.NET Core dev certificate.
* A production-like local topology, so local mirrors the deployment model (ADR-0014).
* One recipe that adopters can reuse, cross-platform including Linux.

## Considered Options

* Run plain HTTP locally and relax the HTTPS requirement in dev.
* Serve HTTPS directly from Kestrel with a trusted dev or mkcert certificate mounted in, no proxy.
* Serve HTTPS through a terminating reverse proxy with a locally-trusted certificate, forwarding to the app.

## Decision Outcome

Chosen: "a terminating reverse proxy with a locally-trusted certificate for the container stack, and the ASP.NET Core dev certificate for the plain `dotnet run` inner loop." Plain HTTP is rejected (it breaks OIDC semantics, trains bad habits, and diverges from production); direct-Kestrel-HTTPS is a supported lighter alternative but not the default because it is less production-like.

* **Inner loop (`dotnet run` on the host).** Use the ASP.NET Core dev certificate: `dotnet dev-certs https --trust`. Kestrel serves HTTPS directly. This is the framework-native path (ADR-0061), zero extra infrastructure.
* **Container stack and reference host.** A terminating reverse proxy performs TLS and forwards to the app over the internal network, a single `https` entry point. Traefik is the default in the compose stack (label-driven, container-native); Caddy (automatic local HTTPS) and nginx are drop-in alternatives. This mirrors the production terminate-and-forward model (ADR-0014), so the local topology matches deployment.
* **Trust on both sides (binding, the crux).** The certificate is issued by a locally-trusted CA, either the ASP.NET Core dev CA (trusted with `dotnet dev-certs https --trust`) or an mkcert root CA, and that root is added to the containers' trust store (mounted CA bundle) so the browser and the back-channel callers (BFF to IdP, resource server to JWKS and introspection) all validate it. A bare, untrusted self-signed certificate is not acceptable, because it breaks back-channel validation.
* **Linux caveat.** `dotnet dev-certs https --trust` is supported only on macOS and Windows; on Linux, use mkcert (which registers a trusted local CA in the OS store and browsers) or the distribution's trust mechanism. The reference setup documents both paths.
* **Issuer, cookies, and tenants.** The local issuer is `https`, cookies carry `Secure` (ADR-0043), and local tenant addressing uses the path-based form (`localhost/tenant`) per ADR-0025, so no wildcard certificate is needed locally.
* **Adopter guidance, not production.** The reference host and quickstart ship the proxy-plus-cert recipe so `docker compose up` yields a working HTTPS IdP, and adopters running the product locally use the same recipe. Production TLS is operator-supplied (real ACME or cert-manager certificates, a wildcard for tenant subdomains, the ADR-0025 production note); this ADR is local and dev only.
* **Tooling, not a shipped dependency.** dev-certs, mkcert, and the proxy image are dev-time tooling pulled from upstream, not dependencies compiled into Nami's artifacts, so ADR-0026 governs them only as the (permissive: Traefik MIT, Caddy Apache-2.0, nginx BSD, mkcert BSD) tools they are, the same dependency-versus-tooling distinction as ADR-0063.

### Consequences

* Good, because local development gets working, trusted HTTPS with a single documented recipe that both the Nami team and adopters reuse, and the topology matches production.
* Good, because the trust-on-both-sides rule prevents the classic failure where the browser works but back-channel calls silently fail on an untrusted cert.
* Good, because the inner loop stays zero-infrastructure (dev-certs on Kestrel) while the container stack is production-like (proxy), so each layer uses the right tool.
* Bad, because the container path adds a reverse proxy and a CA-trust step to the local setup; mitigated by shipping the recipe and by the direct-Kestrel alternative for those who want less.
* Bad, because Linux trust is not one command; mitigated by documenting mkcert as the cross-platform path.

## Pros and Cons of the Options

### Plain HTTP locally

* Good, because it is the least setup.
* Bad, because it breaks `https`-issuer and `Secure`-cookie semantics, diverges from production, and hides exactly the TLS problems local dev should surface.

### Direct Kestrel HTTPS with a mounted trusted cert

* Good, because it is simple, has no proxy, and is Microsoft's documented default.
* Bad, because it is less production-like than a terminating proxy and still needs the both-sides trust step; kept as the supported lighter alternative.

### Terminating reverse proxy with a locally-trusted cert (chosen)

* Good, because it matches the production terminate-and-forward model, gives a single HTTPS entry, and is the recipe adopters will use in production anyway.
* Bad, because it adds a proxy and a CA-trust step locally; mitigated by shipping the recipe and keeping the direct-Kestrel alternative.

## More Information

* Related decisions: ADR-0025 (the local dev loop this extends, and the path-based local tenant addressing), ADR-0014 (the terminate-and-forward proxy model reused here), ADR-0043 (the `https` and `Secure`-cookie invariants that make local TLS mandatory), ADR-0029 (the BFF-to-IdP back-channel that needs the trusted cert), ADR-0048/0049 (the resource-server-to-JWKS/introspection back-channel), ADR-0027/0031 (the reference host and 12-factor edge that ship the recipe), ADR-0061 (framework-native-first, favoring dev-certs for the inner loop), and ADR-0063 (the dependency-versus-tooling distinction reused for dev-certs/mkcert/the proxy image).
* Verified 2026-07-18: `dotnet dev-certs https --trust` (macOS/Windows only), Microsoft's Docker Compose HTTPS guidance (mount the dev cert), mkcert (locally-trusted CA in OS store and browsers), and reverse-proxy-plus-mkcert as the common production-like local pattern. Tools named factually; all are permissive-licensed.
* Authored fresh for this repository.
