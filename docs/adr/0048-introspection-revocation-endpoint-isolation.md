---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: OpenIddict 7.5.0 introspection behavior (`ValidateAuthorizedParty`, verified V14); RFC 7662 (introspection); RFC 7009 (revocation); RFC 9449 (DPoP)
informed: all contributors, via this repository
---

# Isolate the introspection and revocation endpoints with client authentication and native audience confinement

## Context and Problem Statement

Nami exposes token introspection (RFC 7662) and revocation (RFC 7009) so resource servers and clients can validate and revoke tokens. These are service-to-service endpoints, and getting their access control wrong is a serious hole: without isolation, one client could introspect or revoke another client's tokens, and introspection could be used as an oracle to learn whether a token exists. Two further details are load-bearing and easy to get wrong: DPoP-bound tokens need their `cnf.jkt` surfaced through introspection so a resource server can enforce proof-of-possession, and OpenIddict already confines introspection to the authorized party natively, so hand-rolling an owner-check controller (a common wrong-API mistake) would both reinvent and likely weaken that. None of the existing ADRs cover these endpoints (ADR-0004 is refresh-token mechanics).

## Decision Drivers

* A client must never introspect or revoke a token that is not its own.
* Introspection must not leak whether a token exists or is valid (uniform `active:false`, RFC 7662).
* DPoP-bound tokens need `cnf.jkt` in the introspection response so a resource server can enforce proof-of-possession.
* Confinement must use the engine's native mechanism, not a bespoke owner-check controller.
* The endpoints are unauthenticated and unpathed by default until explicitly configured.

## Considered Options

* Unauthenticated introspection with no owner confinement
* Client-authenticated endpoints, confined by a hand-rolled owner-check controller
* Client-authenticated endpoints, confined by OpenIddict's native authorized-party check

## Decision Outcome

Chosen option: "Client-authenticated endpoints, confined by OpenIddict's native authorized-party check". The fixed parameters are:

* **A. Client authentication is required, and the endpoints are explicitly pathed.** Both `/connect/introspect` and `/connect/revocation` require client authentication (`private_key_jwt` for machine-to-machine callers, ADR-0009, never a shared secret). Because OpenIddict 7.5 auto-paths only discovery and JWKS, both endpoints are enabled explicitly via `SetIntrospectionEndpointUris`/`SetRevocationEndpointUris`.
* **B. Audience confinement is native, not hand-rolled.** A client may only introspect or revoke a token whose audience is itself, enforced by OpenIddict's native `ValidateAuthorizedParty` (verified, V14). The decision is to enable that plus the client-auth permission, and explicitly **not** to write a custom owner-check controller, which is the most common wrong-API pattern in this domain; the native behavior is pinned and re-verified per bump (ADR-0021).
* **C. DPoP `cnf.jkt` in the introspection response.** For a DPoP-bound token, the introspection response must carry `cnf.jkt` so a resource server can enforce proof-of-possession through introspection; the response is either enriched with it or returns `active:false`.
* **D. Rate-limit, anti-enumeration, and a bounded cache.** The introspection endpoint is rate-limited per client, returns a uniform RFC 7662 `active:false` that does not reveal whether a token exists versus is merely not the caller's, and uses a bounded introspection-result cache (about 5 minutes) balanced against revoke-staleness.

Introspection is the path used where instant revocation is required (reference tokens); plain signed JWTs validated locally with a short 15-minute TTL are the default, and introspection is reserved for that instant-revocation need (ADR-0039).

### Consequences

* Good, because a client cannot inspect or kill another client's tokens, and introspection cannot be used to enumerate tokens.
* Good, because DPoP proof-of-possession is enforceable through introspection, and confinement uses battle-tested native code rather than a bespoke controller that could subtly leak.
* Bad, because reference-token clients must make an introspection HTTP call, which is why reference tokens are reserved for instant-revocation cases rather than used everywhere (ADR-0039).
* Bad, because the introspection-result cache trades a little revoke-staleness for load, a window that must be bounded and reasoned about against the revocation SLO.
* Neutral, because the confinement relies on OpenIddict's native `ValidateAuthorizedParty`, a version-sensitive behavior pinned under ADR-0021.

### Confirmation

* A client introspecting or revoking a token whose audience is another client is refused.
* Introspection returns a uniform `active:false` whether the token does not exist or simply is not the caller's, with no timing or shape difference.
* A DPoP-bound token's introspection response carries `cnf.jkt`.
* The per-client rate limit on introspection is enforced, and no custom owner-check controller exists in the codebase (the native check is used).

## Pros and Cons of the Options

### Unauthenticated introspection with no owner confinement

* Good, because it is the least configuration.
* Bad, because any caller could introspect or revoke any token and enumerate token existence; it is a direct security hole.

### Client-authenticated, confined by a hand-rolled owner-check controller

* Good, because it does add client auth and some confinement.
* Bad, because it reimplements a native capability in application code, which is the most common wrong-API mistake here and is easy to get subtly wrong (missing a claim, a comparison edge), weakening the very confinement it intends.

### Client-authenticated, confined by native authorized-party check (chosen)

* Good, because confinement is enforced by the engine's tested code, client auth uses `private_key_jwt`, and enumeration is closed by uniform `active:false`.
* Bad, because it depends on a version-sensitive native behavior and reference-token validation costs an HTTP hop.

## More Information

* This is recorded from the endpoint/service-communication design (doc 12 §1, §3), which is the single source of truth for introspection and fixed the three previously-missing load-bearing points (native confinement, DPoP `cnf.jkt` in the response, and rate-limit plus anti-enumeration plus a bounded cache). The resource-server-side per-tenant validation model in the same document is a separate decision.
* Related decisions: ADR-0004 (token posture; introspection is the validation path for reference tokens), ADR-0009 (`private_key_jwt` client authentication), ADR-0014 (DPoP sender-constrained tokens whose `cnf.jkt` this surfaces), ADR-0021 (the seam catalogue and contract-regression that pin the native `ValidateAuthorizedParty` behavior), and ADR-0039 (reference tokens and the instant-revocation path that uses introspection).
* Authored in this repository in 2026-07 to record the settled endpoint-isolation decision as an ADR; standards and the engine (RFC 7662, RFC 7009, RFC 9449, OpenIddict) are named factually for identification only, and no commercial competitor is named.
