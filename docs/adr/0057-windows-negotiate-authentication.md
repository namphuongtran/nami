---
status: "proposed"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the v2 roadmap/backlog and production-readiness register of the design corpus; ASP.NET Core Negotiate authentication; the SPNEGO/Kerberos mechanism
informed: all contributors, via this repository
---

# Support Windows integrated authentication (Negotiate/Kerberos) through a demand-driven extension

> **Status: proposed.** This records a deliberately deferred feature and the intended approach, so the deferral is explicit rather than forgotten. It is not accepted or scheduled; when its trigger fires it earns a full ADR and design before any build, and this ADR is then superseded or promoted to accepted.

## Context and Problem Statement

Nami v1 authenticates users locally and federates external identity providers over OIDC (a static host-level set in ADR-0002, dynamic per-tenant OIDC in ADR-0034), with SAML/WS-Federation deferred (ADR-0055). Windows integrated authentication (Negotiate/SPNEGO over Kerberos) is the standard single sign-on in on-premises Active Directory environments, where a domain-joined user expects to reach the application without a separate login. It was left out of v1 because it is an on-premises enterprise need rather than a general or cloud one. This ADR does not build it now; it records that it was consciously deferred, with the intended approach and trigger, so a future maintainer sees a decision rather than an oversight.

## Decision Drivers

* On-premises AD single sign-on is required only by on-premises enterprise customers, not the general or cloud case, so v1 should not carry it.
* When built, it must be additive and non-breaking to a frozen v1, reusing the existing authentication-scheme registration rather than forking it.
* The protocol work is provided by the framework's first-party Negotiate handler, so no third-party dependency is introduced; the exact approach is confirmed at the wave.
* Negotiate/Kerberos carries real deployment prerequisites (a domain-joined host or gateway, service principal names, proxy configuration) that the wave must document.

## Considered Options

* Build Windows authentication in v1
* Never support Windows authentication (OIDC/SAML only)
* Defer to a demand-driven wave with a recorded intended approach and trigger

## Decision Outcome

Proposed option: "Defer to a demand-driven wave with a recorded intended approach and trigger." The intended shape, to be confirmed by a full ADR and design when the wave is picked up:

* **Trigger**: an on-premises Active Directory customer needs integrated Windows single sign-on.
* **Scope**: inbound Negotiate (SPNEGO over Kerberos) authentication, provisioning or linking the Windows identity into the global identity store through membership, exactly as OIDC external login does (ADR-0001, ADR-0002), never a per-tenant identity.
* **Delivery seam**: registered as one more authentication scheme alongside the existing external-provider schemes (ADR-0002, ADR-0034), so it is additive and does not touch the frozen v1 core.
* **Mechanism**: the framework's first-party ASP.NET Core Negotiate handler provides the protocol, so no third-party dependency is introduced; the exact wiring is confirmed at the wave.
* **Deployment prerequisites**: the wave documents the domain-join or gateway model, the required service principal names, and the reverse-proxy configuration, as an Ops requirement.

### Consequences

* Good, because the on-premises AD gap now has a recorded, triggered plan, it is small (the framework handler does the protocol work), and it introduces no third-party dependency.
* Good, because reusing the authentication-scheme seam keeps it additive, with no v1 impact.
* Bad, because Negotiate/Kerberos has real deployment complexity (domain join, service principal names, proxy handling) that the wave must document and that constrains where it can run.
* Neutral, because this is proposed, not committed: there is no v1 impact and no build until the trigger fires and a full ADR/design is done.

### Confirmation

* When the trigger fires, the feature follows the standard flow (brainstorm, a full ADR plus design, then build), and this ADR is promoted to accepted or superseded by that ADR.
* v1 (ADRs 0000 through 0054) remains frozen; Windows authentication enters only as an additive, non-breaking scheme over the existing seam.

## Pros and Cons of the Options

### Build Windows authentication in v1

* Good, because it would serve on-premises AD customers at launch.
* Bad, because it is an on-premises-only need with deployment prerequisites that do not fit the cloud-first v1, so it would add surface for a minority case.

### Never support Windows authentication

* Good, because it keeps the authentication surface to OIDC (and, later, SAML).
* Bad, because it forecloses on-premises Active Directory customers who expect integrated Windows SSO.

### Defer to a demand-driven wave with a recorded approach (proposed)

* Good, because v1 stays cloud-first and bounded while the deferral is explicit, triggered, and pre-scoped over the existing scheme seam with a first-party mechanism.
* Bad, because it is a living record that must be promoted and fully designed (with its deployment prerequisites) when the trigger fires.

## More Information

* Recorded from the v2 roadmap/backlog and production-readiness register of the design corpus, which size this as small because the framework handler carries the protocol. It is a proposal, not a commitment.
* Related decisions: ADR-0002 (external IdP integration and the account-provisioning/linking model this reuses), ADR-0034 (the dynamic authentication-scheme-provider seam), ADR-0001 (global identity plus membership, the provisioning model), and ADR-0055 and ADR-0056 (sibling proposed, deferred extensions).
* Authored in this repository in 2026-07 as a proposed, deferred feature; the enterprise on-premises framing is generalized (no vendor named), and the ASP.NET Core Negotiate handler (a first-party framework component, not a third-party dependency) and the SPNEGO/Kerberos mechanism are named factually for identification only.
