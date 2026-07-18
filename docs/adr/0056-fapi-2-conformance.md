---
status: "proposed"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: the v2 roadmap/backlog of the design corpus; the OpenID Foundation FAPI 2.0 profiles; RFC 9101 (JAR), RFC 9396 (RAR), and JARM
informed: all contributors, via this repository
---

# Support FAPI 2.0 high-assurance profiles through a demand-driven extension

> **Status: proposed.** This records a deliberately deferred feature and the intended approach, so the deferral is explicit rather than forgotten. It is not accepted or scheduled; when its trigger fires it earns a full ADR, design, FAPI conformance testing, and a spike before any build, and this ADR is then superseded or promoted to accepted.

## Context and Problem Statement

Nami v1 targets the OAuth 2.0 Security Best Current Practice (RFC 9700) and builds both mTLS and DPoP sender-constrained tokens (ADR-0014), but it deliberately de-scoped the FAPI-specific message-signing standards (JARM signed authorization responses, RAR rich authorization requests, and JAR signed request objects), because there is no current open-banking or FAPI-required use case (ADR-0014). FAPI 2.0 is the high-assurance profile that regulated sectors such as open banking and open finance mandate. This ADR does not build FAPI now; it records that FAPI 2.0 was consciously deferred, with the intended approach and trigger, so a future maintainer sees a decision rather than an oversight.

## Decision Drivers

* FAPI is required only by regulated/open-banking integrations, not by the general case, so v1 should not pay its cost or attack surface.
* Much of the FAPI 2.0 security profile is already covered by v1: both sender-constrained token methods FAPI accepts (mTLS and DPoP, ADR-0014) are built, and PAR, PKCE, and issuer identification are native; the real gap is the message-signing tier.
* Any build must be additive and non-breaking to a frozen v1, reusing the OpenIddict event-handler model already used for DPoP.
* No third-party library is pinned now; build-versus-library is a wave-time decision, and any library used must be OSS-permissive (ADR-0026).
* FAPI conformance is a formal certification with a sharp security surface, so a build is gated by conformance testing and a spike.

## Considered Options

* Build FAPI 2.0 in v1
* Never target FAPI
* Defer to a demand-driven wave with a recorded intended approach and trigger

## Decision Outcome

Proposed option: "Defer to a demand-driven wave with a recorded intended approach and trigger." The intended shape, to be confirmed by a full ADR and design when the wave is picked up:

* **Trigger**: an open-banking, open-finance, or otherwise FAPI-required integration or regulatory mandate.
* **Scope split**: the sender-constrained baseline is largely built already (ADR-0014 ships both mTLS and DPoP, the two methods FAPI accepts; PAR, PKCE, and `iss` are native), so the incremental work is the **message-signing tier** (JAR, JARM, RAR) that ADR-0014 currently de-scopes; that tier would be built for FAPI 2.0. (Making the built mTLS and DPoP routes formally FAPI-conformant is part of the wave, not a from-scratch effort.)
* **Delivery seam**: additive handlers over the OpenIddict event-handler model (the same model DPoP is built on, ADR-0014), with no change to the frozen v1 core.
* **Implementation approach is left open**: no third-party library is pinned now; build-versus-library is evaluated at the wave, and any library used must be OSS-permissive (ADR-0026).
* **Gate**: the wave runs the OpenID Foundation FAPI 2.0 conformance test plans plus a security spike before support is claimed.

### Consequences

* Good, because the regulated-market gap now has a recorded, triggered plan, and because the v1 sender-constrained baseline (both mTLS and DPoP) already covers much of the profile, the incremental build is scoped to the message-signing tier rather than a from-scratch effort.
* Good, because reusing the DPoP-style event-handler seam keeps it additive, with no v1 impact.
* Bad, because the message-signing tier (JAR/JARM/RAR) is large and high-assurance and must be conformance-gated, so it is not cheap when triggered.
* Neutral, because this is proposed, not committed: JAR/JARM/RAR remain de-scoped in ADR-0014 until this wave fires, and there is no v1 impact.

### Confirmation

* When the trigger fires, the feature follows the standard flow (brainstorm, a full ADR plus design, FAPI conformance testing, a security spike, then build), and this ADR is promoted to accepted or superseded by that ADR.
* v1 (ADRs 0000 through 0054) remains frozen; FAPI enters only as an additive, non-breaking wave over the existing sender-constrained/handler seam.

## Pros and Cons of the Options

### Build FAPI 2.0 in v1

* Good, because it would make Nami open-banking-ready at launch.
* Bad, because the message-signing tier is large and high-assurance for a requirement only regulated integrations have, materially delaying v1.

### Never target FAPI

* Good, because it keeps the protocol surface minimal.
* Bad, because it forecloses regulated open-banking/open-finance markets that mandate FAPI.

### Defer to a demand-driven wave with a recorded approach (proposed)

* Good, because v1 stays bounded while the deferral is explicit and pre-scoped (the mTLS baseline already covers much of it), leaving build-versus-library open.
* Bad, because it is a living record that must be promoted, fully designed, and conformance-tested when the trigger fires.

## More Information

* Recorded from the v2 roadmap/backlog of the design corpus. It is a proposal, not a commitment. The sender-constrained baseline is small-to-medium because ADR-0014 already ships both mTLS and DPoP; the message-signing tier (JAR/JARM/RAR) is the large, high-assurance part.
* Related decisions: ADR-0014 (the sender-constrained baseline FAPI reuses, and the JAR/JARM/RAR de-scopes this wave would reverse), ADR-0009 (`private_key_jwt` client authentication), ADR-0043 (the security hardening invariants a FAPI profile tightens further), ADR-0026 (the OSS-permissive license policy any library used would have to satisfy), and ADR-0055 (a sibling proposed, deferred federation extension).
* Authored in this repository in 2026-07 as a proposed, deferred feature; the regulated-market framing is generalized (no vendor named), and the OpenID Foundation FAPI profiles and the JAR/JARM/RAR standards are named factually for identification only. No third-party library is pinned; the build-versus-library choice is deferred to the wave.
* Revisit trigger: stays `proposed` until a regulated-market or high-assurance customer needs FAPI 2.0. At that point, confirm the then-current FAPI 2.0 Security Profile and Message Signing profiles (JAR/JARM/RAR), reconcile against what the mTLS and DPoP baseline already covers (ADR-0014), run FAPI conformance testing, and run a security spike, before accepting or building.
