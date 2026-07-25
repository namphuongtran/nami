---
status: "proposed"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the OpenID Shared Signals Framework, CAEP, and RISC final specifications (approved by the OpenID Foundation membership on 2025-09-02, announced 2025-09-16), plus SET (RFC 8417) and its push/poll delivery (RFC 8935/8936), verified 2026-07-18 and re-verified at the source 2026-07-25; the enabling decisions ADR-0039, ADR-0003, ADR-0019, ADR-0008, ADR-0048, ADR-0013
informed: all contributors, via this repository
---

# Support continuous access evaluation via the OpenID Shared Signals Framework (Nami as transmitter)

## Context and Problem Statement

An OAuth or OIDC access token is valid for its whole lifetime. When something changes mid-session (a user is disabled, a credential is compromised, a device falls out of compliance) the relying parties holding that token do not find out until it expires, which can be minutes to an hour. The industry answer is continuous access evaluation: an identity provider pushes a real-time signal so consumers can revoke access immediately. The OpenID Shared Signals Framework (SSF), with its CAEP and RISC profiles, standardizes this. The OpenID Foundation membership **approved all three as Final Specifications on 2025-09-02** (85 approve, 1 object, 25 abstain), announced in a press release dated 2025-09-16 that names Apple, IBM, and Okta as technology leaders having already adopted the protocols.

**Standard maturity is not the same as ecosystem readiness, and that gap is the reason this ADR is `proposed` rather than accepted.** The operational picture as researched for ADR-0071 (2026-07-21): one vendor runs a production transmitter **and** receiver; a major OSS identity server ships only an **experimental transmitter, off by default, with no receiver at all**; another leans on its own proprietary channel instead; and there is **no first-party or widely adopted .NET transmitter or receiver library**, so building the transmitter means hand-rolling security-event-token issuance against RFC 8417/8935/8936. Adoption by large vendors therefore does not yet imply that receivers exist to consume what Nami would emit.

Nami already has the internal machinery that these signals would carry: cross-node revocation (ADR-0039), the server-side session store (ADR-0003), the back-channel-logout fan-out that already pushes a `logout_token` to each relying party (ADR-0019), the delivery-guaranteed outbox (ADR-0008), and pull-based introspection (ADR-0048). What is missing is a standard way to push those events to external receivers. This ADR records the direction and scope for supporting SSF, primarily with Nami as the signal transmitter. It is `proposed`: recorded now, accepted when demand is real, because the fit is strong but internal demand is unproven.

## Decision Drivers

* Strategic fit: Nami is the identity provider, so it is the natural source of session and credential signals.
* Reuse: the events already exist internally (revocation, session store, logout fan-out, audit outbox); SSF is the standard envelope, not new machinery.
* Standards-based interop: emit a standard SET over a standard stream rather than invent a Nami-specific webhook.
* Multi-tenant correctness: a signal must be tenant-scoped so it cannot cross tenant boundaries.
* Demand-driven: continuous access evaluation is an enterprise and zero-trust need whose demand inside Nami's user base is not yet proven.

## Considered Options

* Do nothing: rely on token lifetime plus the existing revocation and introspection.
* Support Nami as an SSF transmitter (emit CAEP and RISC events) as a demand-driven extension.
* Build the full transmitter and receiver now, ahead of demand.

## Decision Outcome (proposed)

Proposed: "support Nami as an SSF transmitter, demand-driven", scoped first to the transmitter role. Building ahead of demand is rejected for now; doing nothing leaves the mid-session-change gap that CAEP exists to close.

* **Role: transmitter first.** Nami emits Security Event Tokens (SET, RFC 8417) over SSF streams, delivered by push (RFC 8935) or poll (RFC 8936) to receivers that have registered a stream. The receiver role (Nami consuming signals from an external IdP to revoke a federated session, ADR-0002/0034) is a plausible later phase, out of initial scope.
* **Events, mapped to existing triggers.** CAEP `session-revoked` from a session-store revocation (ADR-0003/0039); `credential-change` from a password or credential update (ADR-0028); `assurance-level-change` from an MFA or step-up change (ADR-0013); `device-compliance-change` and `token-claims-change` where a signal source exists; and optionally RISC account-level events such as `account-disabled` from an admin action. These are the same state changes Nami already records and acts on internally.
* **Delivery reuses the guaranteed outbox.** SET emission rides the delivery-guaranteed outbox pattern (ADR-0008) so a signal is not lost, complementing (not replacing) pull-based introspection (ADR-0048): introspection is a resource server asking, SSF is Nami telling.
* **Multi-tenant scoping (the key correctness point).** Streams, subjects, and SETs are tenant-scoped: Nami has a per-tenant issuer (ADR-0001/0049), a receiver subscribes within one tenant, and every SET carries the tenant and issuer binding so a signal for tenant A can never revoke a session in tenant B. This is the same shared-keyset-is-not-a-boundary reasoning as ADR-0049, applied to signals.
* **Implementation open.** No library is pinned; whether OpenIddict or the ecosystem offers SET/SSF support or Nami builds the transmitter is confirmed at build time. This is not a replacement for token expiry or introspection; it is an addition.

### Consequences

* Good, because it closes the mid-session-change gap with a standard consumers already implement, reusing Nami's existing revocation, session, and outbox machinery.
* Good, because scoping to the transmitter role first keeps it focused and matches Nami's position as the signal source.
* Good, because leaving it `proposed` avoids building ahead of demand while recording the direction and the enabling hooks.
* Bad, because tenant-scoped streams add real design work (subject identification, stream management, per-tenant authorization of receivers); accepted as the core of the eventual build.
* Bad, because it is another asynchronous delivery surface to secure and operate; mitigated by reusing the outbox and by the demand-driven trigger.

## Pros and Cons of the Options

### Do nothing

* Good, because token lifetime plus existing revocation and introspection already bound exposure.
* Bad, because the exposure window between a change and token expiry stays open, which is exactly what zero-trust consumers reject.

### SSF transmitter, demand-driven (chosen, proposed)

* Good, because it is standards-based, reuses existing machinery, and triggers on real demand.
* Bad, because tenant-scoped streams and a second async surface are real work; accepted for a `proposed` record.

### Full transmitter and receiver now

* Good, because it would be complete.
* Bad, because it builds two complex surfaces ahead of proven demand, against the demand-driven posture of the other extension ADRs.

## More Information

* This is a sibling to the demand-driven `proposed` extension ADRs (ADR-0064 MCP, ADR-0055 SAML/WS-Federation, ADR-0056 FAPI 2.0, ADR-0057 Windows/Negotiate): a recorded direction, accepted when demand is real, pinning no library.
* Related decisions: ADR-0039 (cross-node revocation, the internal mechanism SSF externalizes), ADR-0003 (the session store that sources `session-revoked`), ADR-0019 (the back-channel-logout fan-out, the closest existing push-to-relying-party pattern), ADR-0008 (the delivery-guaranteed outbox SET emission reuses), ADR-0048 (pull-based introspection, the complement to this push model), ADR-0013 (MFA assurance, the source of `assurance-level-change`), ADR-0028 (credential changes), ADR-0001/0049 (per-tenant issuer and the isolation reasoning applied to signals), and ADR-0002/0034 (federation, the later receiver-role phase).
* **Boundary with ADR-0071**, which is easy to confuse with this one: ADR-0071 is an **accepted, spike-proven** Nami-shaped CloudEvents stream over a message broker for **backend microservices**, whereas this ADR is a **standards-based** transmitter emitting security-event tokens to **external receivers**. They are complementary: the same internal state changes feed both, and ADR-0071's ecosystem research is the evidence recorded above for why this one stays `proposed`. If both ship, they are two encodings of one event source, not two event sources.
* Standards verified 2026-07-18 and re-verified at the primary source 2026-07-25: OpenID SSF, CAEP, and RISC were approved as Final Specifications on **2025-09-02** (the earlier `2025-09-16` recorded here was the press-release date, not the approval date), plus SET (RFC 8417) and push/poll delivery (RFC 8935/8936); named factually for identification.
* Revisit trigger: stays `proposed` until Nami actively develops continuous access evaluation, or until the ecosystem gap above closes (a maintained .NET SET/SSF library appears, or real receivers exist among Nami's consumers). At that point, confirm the then-current SSF/CAEP/RISC profiles, re-check the library and receiver situation rather than assuming it improved, and settle the tenant-scoped stream and subject-identification design, before accepting or building.
* Authored fresh for this repository.
