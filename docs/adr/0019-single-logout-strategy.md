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
* **Mechanism**: when a session (`sid`) ends, whether by active logout, revoke, or absolute expiry, Nami mints an OIDC Back-Channel Logout token (a `logout_token` JWT carrying `sub`/`sid` and the `events` claim) and pushes it to each registered `backchannel_logout_uri` of the RPs in that session, so each RP ends its own session server-side.
* **First-party SPAs use a BFF**: the SPA delegates authentication to a server-side BFF, and the BFF receives the back-channel logout. It is the same BFF used by the DPoP design (ADR-0014).
* **Migration**: when OpenIddict 8.0 ships native back-channel logout, Nami migrates to it; the interim implementation is designed to be replaceable.
* **Two mandatory fixes, applied regardless of the option chosen**: drop the front-channel iframe logout from the design (it is dead), keeping `end-session` as a top-level redirect; and make tenant-switch `prompt=none` a top-level redirect rather than an iframe, so it survives cookie-blocking.

### Consequences

* Good, because browser RPs get real single logout that is durable through third-party-cookie deprecation, at commercial-grade parity.
* Good, because it reuses the existing session store, does not wait for 8.0, and can migrate to the native 8.0 implementation later.
* Bad, because of the extra build: minting the `logout_token`, an RP `backchannel_logout_uri` registry (a new field on the Application), at-least-once delivery with retry, and a BFF for first-party SPAs.
* Bad, because RPs must support a back-channel logout endpoint; a legacy front-channel-only RP falls back to bounded logout (the access-TTL) for that group, which is documented. **That bound is conditional, corrected 2026-08-01:** it holds only because the refresh grant denies a token whose session row is gone (ADR-0003, executed in design [04](../design/04-core-protocol.md)). Without that gate a normal logout revokes the *session* and not the RP's *tokens*, so an RP that never received its `logout_token` could keep minting access tokens until the ADR-0004 8-hour refresh ceiling. The worst case is not fifteen minutes but nearly eight hours, for a user who logs out shortly after logging in.
* Security: the `logout_token` must be validated correctly (`iss`/`aud`/`sid`/`events`, and never repurposed), with a `jti` replay guard.

### Confirmation

* The browser third-party-cookie deprecation is widely documented across the identity industry (verification V11); OpenID Connect Back-Channel Logout 1.0 is the target spec; and OpenIddict issue #2175 records the maintainer stating that back-channel logout needs a session store, which Nami already has.
* Tests: a logout causes every RP in the session to receive the back-channel token and end its session; a legacy RP is bounded to at most the access-TTL, and that test must **exercise the refresh grant** after the logout rather than only waiting out the access token, since it is the refresh denial that produces the bound and a test that skips it would pass with the gate removed; and the `logout_token` validation and replay guard are exercised.

## Pros and Cons of the Options

### A. Accept bounded logout

* Good, because it is the simplest option and needs no new build.
* Bad, because it is not true single logout: an RP retains access until its token expires, up to the access-TTL, and only where the refresh grant is gated on the session (ADR-0003); ungated, the retained access runs to the ADR-0004 8-hour refresh ceiling instead.

### B. Interim back-channel logout plus a BFF (chosen)

* Good, because it delivers true single logout, reuses the existing session store, and remains valid through cookie deprecation.
* Bad, because it is a real build (token minting, an RP registry, reliable delivery, and a BFF).

### C. Wait for OpenIddict 8.0 native

* Good, because it would avoid building an interim.
* Bad, because it carries timing risk and leaves only bounded logout until 8.0 arrives, despite the session store already making the interim feasible.

## More Information

* Original decision: 2026-07-01. This supersedes the earlier "wait for 8.0" stance recorded in ADR-0014 for back-channel logout.
* Build follow-ups: mint the `logout_token` and push it to each RP `backchannel_logout_uri`; store `backchannel_logout_uri` as `Application.Properties["backchannel_logout_uri"]` (**settled 2026-08-01**: the engine has no native field, since `OpenIddictApplicationDescriptor` exposes only `PostLogoutRedirectUris` plus the `Properties` dictionary and `OpenIddictConstants.cs` carries no back-channel constant, so this reuses the `cors_origins` pattern and needs **no migration**; it is https-only and SSRF-validated, and a null value declares that the relying party accepts bounded logout); drop front-channel and make tenant-switch a top-level redirect; add the logout page and tenant switcher as top-level redirects (no iframe); have the BFF receive back-channel logout for SPAs; and add the tests above.
* **The industry posture this deliberately does not adopt as the primary path, recorded so it is not "simplified" back in.** Both a leading OSS identity server and a commercial .NET server deliver back-channel logout **in-request and best-effort**: the former posts a logout token to each registered client URI when it detects logout, and the latter invokes its notification service when the logout page signs the cookie out, over a plain HTTP client. Neither documents a delivery guarantee, a retry, or what happens when a relying party's URI is unreachable (checked against their own documentation on 2026-07-25). That is a defensible point on the reliability-versus-latency curve, not an oversight, but it is the point this ADR's at-least-once requirement rejects: an unreachable relying party would simply stay signed in with nothing recording that it did. Anyone tempted to drop the outbox for a direct post is choosing their guarantee, not simplifying ours.
* **Available as an optimization rather than a fallback: an opportunistic immediate dispatch.** The outbox bounds *worst-case* delivery but makes the *common* case wait for the next relay claim. A best-effort dispatch attempted right after the response is written, with the outbox row already committed, would make the common case near-immediate while the guarantee still comes entirely from the outbox: a failed or skipped attempt changes nothing, because the row is still pending. This is the same shape the email design already uses for its most critical message, a priority lane described there as sync-with-fallback (ADR-0038). Two constraints make it an option rather than a default: it must **not** reintroduce blocking the interactive logout on N relying-party calls, which is the latency the outbox exists to remove, and it must not become a second delivery path with its own retry semantics, because then a relying party could receive two logout tokens governed by two different rules. Whether to build it is a design-time call, and the outbox is correct without it.
* Deferred to a post-v1 wave (proposed, no ADR yet): minor logout extensibility (upstream logout notification, a custom redirect writer, and login/logout context) over this logout design; revisit on demand.
* **Corrected 2026-08-01: this ADR's fallback bound was wrong, and it was wrong in the direction that makes a risk look smaller than it is** (design corpus, review finding H-33). Several statements here and downstream described the fallback as "session revoke plus a 15-minute access-token TTL". Normal logout revokes the **session**, not the relying party's **tokens**. `RevokeBySubjectAsync`, which does revoke authorizations and tokens, belongs to force-logout, an administrative action owned by user management. Nothing tied the refresh grant to the session's existence, so the real bound was the ADR-0004 8-hour refresh ceiling, roughly thirty times the number this ADR published. The gate that makes the 15-minute bound true was already required by ADR-0003 and simply had no design carrying it; it is now in design [04](../design/04-core-protocol.md). The lesson worth keeping is the shape rather than the number. The claim was asserted in seven places across all three layers (this ADR at the option description, the test list and option A's drawback; architecture runtime view 14 twice and risk R7; and design 14's parity-boundary paragraph), and no single site was wrong *locally*, because each was faithfully restating the one before it. A defect that is only visible as the absence of a mechanism cannot be found by reading the claim; it is found by asking which mechanism produces the bound, and here nothing did.
* Related decisions: ADR-0003 (the server-side session store this builds on) and ADR-0014 (whose back-channel-logout entry is updated from "wait for 8.0" to "build interim per this ADR"); the BFF is the same one used by the DPoP design in ADR-0014. ADR-0068 (proposed) would generalize this push-to-relying-party pattern into standard Shared Signals events. **ADR-0071 reuses this seam without changing it**: where session-end already fires a back-channel logout, the v2 change-event feature adds one outbox emit at the same point so a backend consumer that is not an OIDC relying party also learns of it. The logout logic here is unchanged, and the two are deliberately different audiences rather than two mechanisms for one audience.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. References to a commercial identity vendor and its blog and BFF documentation were generalized; the OpenID Connect specification, the OpenIddict issue, and the neutral vendor reference (WSO2) are retained.
