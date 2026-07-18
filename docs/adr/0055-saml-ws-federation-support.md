---
status: "proposed"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the v2 roadmap/backlog of the design corpus; SAML 2.0 and WS-Federation standards
informed: all contributors, via this repository
---

# Support SAML 2.0 and WS-Federation through a demand-driven federation extension

> **Status: proposed.** This records a deliberately deferred feature and the intended approach, so the deferral is explicit rather than forgotten. It is not accepted or scheduled; when its trigger fires it earns a full ADR, design, and (given the security surface) a spike before any build, and this ADR is then superseded or promoted to accepted.

## Context and Problem Statement

Nami v1 federates external identity providers over OIDC (a static host-level set in ADR-0002, and dynamic per-tenant OIDC in ADR-0034). SAML 2.0, and to a lesser extent WS-Federation, remain the protocols that many enterprise and public-sector/health customers still require for single sign-on, and their absence is the single largest gap between Nami and mainstream commercial identity servers. SAML was deliberately left out of v1 to keep the federation scope OIDC-first and bounded. The problem this ADR addresses is not how to build SAML now, but to record that it was consciously deferred, with the intended approach and the trigger, so a future maintainer sees a decision rather than an oversight and does not have to rediscover the approach under customer pressure.

## Decision Drivers

* The enterprise/health SAML requirement is real but not universal, so it should not delay a shippable OIDC-first v1.
* When built, it must be additive and non-breaking to a frozen v1, reusing the existing federation seam rather than forking it.
* No third-party library is pinned now; whether to build it or adopt a library is a wave-time decision, and any library used must be OSS-permissive (ADR-0026).
* SAML's security surface (signature wrapping, XML canonicalization, metadata trust) is notoriously error-prone, so a build must be gated by a security-focused design and spike, not treated as routine.

## Considered Options

* Build SAML/WS-Federation in v1
* Never support SAML/WS-Federation (OIDC-only forever)
* Defer to a demand-driven wave with a recorded intended approach and trigger

## Decision Outcome

Proposed option: "Defer to a demand-driven wave with a recorded intended approach and trigger." The intended shape, to be confirmed by a full ADR and design when the wave is picked up:

* **Trigger**: a customer (or a committed opportunity) requires SAML SSO.
* **Scope**: support both the identity-provider and service-provider roles; WS-Federation rides the same mechanism.
* **Delivery seam**: implement it over the same dynamic authentication-scheme-provider seam that dynamic OIDC federation uses (ADR-0034), so it is per-tenant, additive, and does not touch the frozen v1 core.
* **Identity model**: an external SAML assertion provisions or links into the global identity store through membership, exactly as OIDC external login does (ADR-0001, ADR-0002), never a per-tenant identity.
* **Implementation approach is left open**: no third-party library is pinned now. When the wave is picked up, build-your-own versus adopting a library is evaluated then, and any library used must be OSS-permissive (ADR-0026); the same seam covers WS-Federation either way.
* **Gate**: because of the security surface, the wave runs a security-focused design plus a spike (signature validation, canonicalization, metadata trust, replay) before build.

### Consequences

* Good, because the largest enterprise gap now has a recorded, triggered plan and a delivery seam, rather than being an implicit hole, while leaving the build-versus-library choice open until the wave.
* Good, because reusing the ADR-0034 scheme-provider seam keeps it additive and per-tenant, with no impact on v1.
* Bad, because SAML is a large feature (L) with a sharp security surface, so the wave is not cheap and must be spike-gated; recording the intent does not reduce that cost.
* Neutral, because this is proposed, not committed: there is no v1 impact and no build until the trigger fires and a full ADR/design/spike is done.

### Confirmation

* When the trigger fires, the feature follows the standard flow (brainstorm, a full ADR plus design, a security spike, then build), and this ADR is promoted to accepted or superseded by that ADR.
* v1 (ADRs 0000 through 0054) remains frozen; SAML enters only as an additive, non-breaking wave over the existing federation seam.

## Pros and Cons of the Options

### Build SAML/WS-Federation in v1

* Good, because it would close the enterprise gap at launch.
* Bad, because SAML is large and security-sensitive, so it would materially delay v1 for a requirement that only some customers have.

### Never support SAML/WS-Federation

* Good, because it keeps the federation surface small and OIDC-only.
* Bad, because it forecloses a large segment of enterprise and public-sector customers who still mandate SAML.

### Defer to a demand-driven wave with a recorded approach (proposed)

* Good, because v1 stays bounded and OIDC-first while the deferral is explicit, triggered, and pre-scoped over the existing seam.
* Bad, because it is a living record that must be promoted and fully designed when the trigger fires.

## More Information

* Recorded from the v2 roadmap/backlog of the design corpus, where SAML is the highest-value later-backlog item. It is a proposal, not a commitment; the suggested commercial sequencing makes SAML the most likely first demand-driven wave.
* Related decisions: ADR-0002 (external IdP integration and the account-linking/claim-allow-list invariants a SAML assertion must also pass), ADR-0034 (the dynamic per-tenant authentication-scheme-provider seam this reuses), ADR-0001 (global identity plus membership, the provisioning model), and ADR-0026 (the OSS-permissive license policy any library used at build would have to satisfy).
* Authored in this repository in 2026-07 as a proposed, deferred feature; the "largest gap versus commercial identity servers" framing is generalized (no vendor named), and the SAML 2.0 / WS-Federation standards are named factually for identification only. No third-party library is pinned; the build-versus-library choice is deferred to the wave.
