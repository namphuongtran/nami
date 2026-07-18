---
status: "accepted"
date: 2026-07-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: analysis of browser third-party-cookie deprecation (verification V11); OpenID Connect Back-Channel Logout 1.0; OpenIddict issue #2175
informed: all contributors, via this repository
---

# Achieve single logout with an interim back-channel logout on the session store, and drop front-channel

## Context and Problem Statement

Single sign-out (logout-everywhere) for browser relying parties faces three realities in 2026:

1. **Front-channel logout is effectively dead**: it depends on cross-site iframes and third-party cookies, which Safari and Firefox block by default and Chrome is following.
2. **OpenIddict 7.5 has no back-channel logout** (issue #2175, roadmap 8.0; the maintainer notes it "requires a new session entity, manager, and store").
3. The same root cause breaks silent `prompt=none` via an iframe (tenant-switch and silent renew).

Earlier designs relied on "front-channel plus end-session", which is no longer viable. `end-session` ends only Nami's own session; a relying party keeps its own cookie session until its token expires, so "logout everywhere" is not achieved for browser RPs. How should Nami implement single logout?

## Decision Drivers

* Real logout-everywhere for browser RPs, durable through third-party-cookie deprecation.
* Reuse the server-side session store already built in ADR-0003.
* Do not gate the capability on OpenIddict 8.0's timing.

## Considered Options

* **A. Accept bounded logout**: session revoke plus a 15-minute access-token TTL, so an RP loses access within the TTL rather than instantly.
* **B. Build an interim back-channel logout plus a BFF for SPAs**, reusing the existing session store and pushing OIDC Back-Channel Logout to each RP's `backchannel_logout_uri`.
* **C. Wait for OpenIddict 8.0's native back-channel logout.**

## Decision Outcome

Chosen option: "Build an interim back-channel logout plus a BFF for SPAs", because the foundation it needs already exists and it delivers true single logout without waiting on 8.0.

Fixed parameters of the decision:

* **The foundation already exists**: the server-side session store (ADR-0003) is exactly the "session entity, manager, and store" the maintainer said back-channel logout requires, so this is feasible now rather than after 8.0.
* **Mechanism**: when a session (`sid`) ends — an active logout, a revoke, or absolute expiry — Nami mints an OIDC Back-Channel Logout token (a `logout_token` JWT carrying `sub`/`sid` and the `events` claim) and pushes it to each registered `backchannel_logout_uri` of the RPs in that session, so each RP ends its own session server-side.
* **First-party SPAs use a BFF**: the SPA delegates authentication to a server-side BFF, and the BFF receives the back-channel logout. It is the same BFF used by the DPoP design (ADR-0014).
* **Migration**: when OpenIddict 8.0 ships native back-channel logout, Nami migrates to it; the interim implementation is designed to be replaceable.
* **Two mandatory fixes, applied regardless of the option chosen**: drop the front-channel iframe logout from the design (it is dead), keeping `end-session` as a top-level redirect; and make tenant-switch `prompt=none` a top-level redirect rather than an iframe, so it survives cookie-blocking.

### Consequences

* Good, because browser RPs get real single logout that is durable through third-party-cookie deprecation, at commercial-grade parity.
* Good, because it reuses the existing session store, does not wait for 8.0, and can migrate to the native 8.0 implementation later.
* Bad, because of the extra build: minting the `logout_token`, an RP `backchannel_logout_uri` registry (a new field on the Application), at-least-once delivery with retry, and a BFF for first-party SPAs.
* Bad, because RPs must support a back-channel logout endpoint; a legacy front-channel-only RP falls back to bounded logout (the access-TTL) for that group, which is documented.
* Security: the `logout_token` must be validated correctly (`iss`/`aud`/`sid`/`events`, and never repurposed), with a `jti` replay guard.

### Confirmation

* The browser third-party-cookie deprecation is widely documented across the identity industry (verification V11); OpenID Connect Back-Channel Logout 1.0 is the target spec; and OpenIddict issue #2175 records the maintainer stating that back-channel logout needs a session store, which Nami already has.
* Tests: a logout causes every RP in the session to receive the back-channel token and end its session; a legacy RP is bounded to at most the access-TTL; and the `logout_token` validation and replay guard are exercised.

## Pros and Cons of the Options

### A. Accept bounded logout

* Good, because it is the simplest option and needs no new build.
* Bad, because it is not true single logout: an RP retains access until its token expires, up to the access-TTL.

### B. Interim back-channel logout plus a BFF (chosen)

* Good, because it delivers true single logout, reuses the existing session store, and remains valid through cookie deprecation.
* Bad, because it is a real build (token minting, an RP registry, reliable delivery, and a BFF).

### C. Wait for OpenIddict 8.0 native

* Good, because it would avoid building an interim.
* Bad, because it carries timing risk and leaves only bounded logout until 8.0 arrives, despite the session store already making the interim feasible.

## More Information

* Original decision: 2026-07-01. This supersedes the earlier "wait for 8.0" stance recorded in ADR-0014 for back-channel logout.
* Build follow-ups: mint the `logout_token` and push it to each RP `backchannel_logout_uri`; add the `backchannel_logout_uri` field to the Application; drop front-channel and make tenant-switch a top-level redirect; add the logout page and tenant switcher as top-level redirects (no iframe); have the BFF receive back-channel logout for SPAs; and add the tests above.
* Deferred to a post-v1 wave (proposed, no ADR yet): minor logout extensibility (upstream logout notification, a custom redirect writer, and login/logout context) over this logout design; revisit on demand.
* Related decisions: ADR-0003 (the server-side session store this builds on) and ADR-0014 (whose back-channel-logout entry is updated from "wait for 8.0" to "build interim per this ADR"); the BFF is the same one used by the DPoP design in ADR-0014. ADR-0068 (proposed) would generalize this push-to-relying-party pattern into standard Shared Signals events.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. References to a commercial identity vendor and its blog and BFF documentation were generalized; the OpenID Connect specification, the OpenIddict issue, and the neutral vendor reference (WSO2) are retained.
