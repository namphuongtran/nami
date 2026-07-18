---
status: "accepted"
date: 2026-06-29
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: byte-level verification of the OpenIddict 7.5.0 source tag (sender-constrained token support); RFC 8705, 9449, 9126, 9101, 8693, 9396
informed: all contributors, via this repository
---

# Build both mTLS and DPoP sender-constrained tokens, and deliberately de-scope FAPI-specific protocols

## Context and Problem Statement

OpenIddict 7.5.0 supports some advanced standards natively and others not at all. Nami must decide its protocol scope: which sender-constrained token mechanisms to build, what to deliberately de-scope, and what to wait for on the OpenIddict roadmap. This ADR replaces two dangling references to step-up/advanced-protocol ADRs that were never written.

A verified fact settled the sender-constrained question. A byte-level read of the OpenIddict 7.5.0 source tag (2026-06-29) established that **OpenIddict 7.5.0 has no DPoP at all — neither issuance nor validation — only mTLS**. There is no `jkt`/`ath`/`htm`/`htu`/`dpop+jwt` constant; `CreateConfirmationClaim` stamps only `x5t#S256` (mTLS); the validation stack is Bearer-only and throws `ID2196` on a `jkt`. An intermediate draft had claimed "DPoP issuance is native, only validation is missing"; that was wrong (it relied on a GitHub issue whose author self-corrected and closed it within hours, with no maintainer confirmation), and the source read overrode it. Mainstream commercial identity servers support both mTLS (RFC 8705) and DPoP (RFC 9449, promoted to their core tier in a recent major version), so an mTLS-only posture would be below commercial-grade for sender-constrained tokens.

## Decision Drivers

* Commercial-grade parity for sender-constrained tokens: mTLS-only leaves public SPA/mobile clients without proof-of-possession.
* Public clients cannot present a TLS client certificate from JavaScript, and per-device certificates are heavy on mobile, so a proof mechanism that fits public clients is needed.
* Reduce attack surface and build cost by de-scoping standards with no current use case.
* Prefer native OpenIddict grants; build only where a standard is genuinely absent.

## Considered Options

* Sender-constrained tokens: mTLS only, DPoP only, or both.
* Other standards to build now, wait for, or de-scope: JARM, RAR, front-channel logout + `check_session_iframe`, EdDSA, CIBA, DCR, back-channel logout, JAR, dynamic per-tenant IdP.

## Decision Outcome

Chosen: **build both mTLS and DPoP**, and de-scope the FAPI-specific standards below.

Fixed parameters of the decision:

* **mTLS (RFC 8705) is the baseline** sender-constrained mechanism for confidential/M2M clients that have PKI: native on both issuance and validation, enforced by `cnf`/`x5t#S256` at the resource server, over internal PKI plus a reverse proxy that forwards `x5t#S256`. Unchanged.
  * **mTLS deployment trust boundary**: both models are supported, defaulting to terminate-and-forward, where a TLS-terminating proxy performs the mTLS handshake and forwards the client certificate, and the app uses `AddCertificateForwarding` with a mandatory `KnownProxies`/`KnownNetworks` allow-list that rejects an unforwarded or spoofed certificate header; the alternative is L4 TLS pass-through with Kestrel `ClientCertificateMode.RequireCertificate`. The trusted-proxy IP list is an Ops/Security ratify item.
* **DPoP (RFC 9449) is decided-build** (2026-06-29), for public SPA/mobile clients, reaching parity with commercial servers that ship both. Because OpenIddict 7.5.0 has neither side, this means building **both** handlers via the OpenIddict event-handler model:
  1. **Issuance handler** (server, at `/token`): read the `DPoP` proof header, compute the RFC 7638 JWK thumbprint, stamp `cnf.jkt`, and advertise `dpop_signing_alg_values_supported`.
  2. **Validation handler** (resource server): accept `Authorization: DPoP <token>`, validate the `jkt`-bound proof per RFC 9449 §4.3, and reject a DPoP-bound token presented as Bearer (§7.2), with a cross-node `jti` replay cache and a nonce.
  * Security caveat: a non-extractable WebCrypto key resists token exfiltration but does **not** stop in-place XSS abuse (a signing oracle), so the real mitigation for a SPA is the BFF, not DPoP alone.
* **De-scoped (not built)**: JARM, RAR (RFC 9396), front-channel logout plus `check_session_iframe` (third-party-cookie deprecation), and EdDSA. Rationale: no FAPI or special use case, and a smaller surface.
* **JAR (RFC 9101) is de-scoped**: OpenIddict 7.5 hard-rejects the `request` parameter, and PAR (native) already pushes parameters server-to-server, covering most of the integrity benefit; mTLS plus issuer identification is enough for high-assurance non-FAPI. Build it (intercept parameter validation, JWS-verify against the client JWKS, strict `typ`) only if Nami enters FAPI or a client requires a signed request object.
* **CIBA is skipped**: OpenIddict has neither support nor a roadmap item, the build is large and unbounded, and there is no clear use case yet. Revisit if a real decoupled-device authentication flow appears.
* **Wait roadmap**: DCR (RFC 7591/7592) waits for OpenIddict 8.0 (issue #2404, re-targeted from 7.6, which shipped as maintenance without DCR); dynamic per-tenant IdP is deferred (ADR-0002, later opened as the additive v2 feature ADR-0034). **Back-channel logout is superseded by ADR-0019**: rather than only waiting for 8.0, Nami builds an interim OIDC back-channel logout (minting a `logout_token` and pushing it to each RP's `backchannel_logout_uri`) on the server-side session store (ADR-0003), because front-channel logout is effectively dead (third-party-cookie deprecation) and the session store already exists; it migrates to the 8.0 native implementation when that ships.
* **Native wire grants** (kept as-is): authorization code plus PKCE, client credentials (M2M via `private_key_jwt`, ADR-0009), refresh (ADR-0004), device code, PAR (RFC 9126, per-client via `Requirements.Features.PushedAuthorizationRequests`), and introspection/revocation/end-session. Token exchange (RFC 8693) uses the native grant, but the `act`/subject-actor resolution is Nami's own code, not native.

### Consequences

* Good, because confidential/M2M clients get a clear mTLS baseline and public clients get DPoP, so sender-constrained coverage is commercial-grade; the FAPI-specific de-scopes are justified by the absence of a use case.
* Bad, because DPoP is a double build (issuance plus validation handlers plus a replay cache), roughly twice the initial estimate; the decision to build it stands for parity.
* mTLS requires internal PKI infrastructure and a reverse proxy that forwards `x5t#S256`, which the deployment must provide.

### Confirmation

* The DPoP conclusion is grounded in a byte-level read of the OpenIddict 7.5.0 source tag (issuance = none, validation = none, mTLS = present via `cnf.x5t#S256` and `UseClientCertificateBoundAccessTokens`), which overrode a mistaken, self-closed GitHub issue.
* Roadmap facts verified against the OpenIddict 7.5.0 tag: JARM, RAR, CIBA, `check_session`, DCR, and back-channel logout are absent; DCR and back-channel logout are targeted at 8.0 (issues #2404 and #2175).
* Verify-before-build: the DPoP mini-spec still requires research into OpenIddict's issuance config for `cnf.jkt`, the custom validation-handler API (event-handler replacement), and the replay cache (`jti`/nonce plus `iat` skew); and the internal PKI plus `x5t#S256`-forwarding proxy must be verified for mTLS.

## Pros and Cons of the Options

### Sender-constrained: mTLS only

* Good, because it is fully native in OpenIddict 7.5.0 (no custom handlers).
* Bad, because public SPA/mobile clients cannot use it (no client certificate from JS), leaving a proof-of-possession gap and falling below commercial-grade parity.

### Sender-constrained: DPoP only

* Good, because it covers public clients.
* Bad, because confidential/M2M clients with PKI are better served by native mTLS, and dropping mTLS would discard native functionality.

### Sender-constrained: both mTLS and DPoP (chosen)

* Good, because it fits each client type (mTLS for confidential/M2M, DPoP for public) and matches commercial-grade coverage.
* Bad, because DPoP must be built on both sides, roughly doubling the effort.

## More Information

* Original decision 2026-06-28; DPoP was re-opened and decided-build on 2026-06-29. The sender-constrained tie-break was settled by reading the source rather than trusting a GitHub issue.
* Reference point: mainstream commercial identity servers document both mTLS (RFC 8705) and DPoP (RFC 9449), with DPoP in their core tier as of a recent major version; this is the parity bar Nami is matching.
* Deferred to a post-v1 wave (proposed, no ADR yet): an advanced resource-isolation policy layer atop the native RFC 8707 resource indicators (per-tenant/per-API audience scoping); revisit as product APIs multiply. The FAPI message-signing tier (JAR/JARM/RAR) de-scoped above is tracked in ADR-0056 (proposed). The native RFC 8707 resource indicators here are the enabling hook for ADR-0064 (proposed), which supports Nami as the authorization server for MCP servers.
* Related decisions: ADR-0002 (external IdP integration; dynamic per-tenant federation deferred), ADR-0003 (server-side session store, the basis for interim back-channel logout), ADR-0004 (refresh grant), ADR-0009 (`private_key_jwt` for M2M), ADR-0019 (single logout strategy, which supersedes the back-channel wait), ADR-0034 (dynamic per-tenant external IdP, the additive v2 feature), ADR-0056 (the FAPI 2.0 deferral that would build the de-scoped message-signing tier). The DPoP mini-spec is a separate design document.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. References to a specific commercial identity server and its documentation URL were generalized, and the deployment-name placeholder was removed; OpenIddict issue numbers and RFC references are retained.
