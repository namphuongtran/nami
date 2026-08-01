---
status: "accepted"
date: 2026-07-21
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: DPO and Legal (PII in events, control DP.01, event retention); Security (per-consumer bus access control and payload PII minimization); Ops (broker sizing, dead-letter policy, alerting); primary-source research 2026-07-21 on the Shared Signals family, RFC 9068, OIDC back-channel logout, and the transactional-outbox pattern
informed: all contributors, via this repository
---

# Publish identity change events outward through a transactional outbox to a message broker, for backend consumers that are not OIDC relying parties

## Context and Problem Statement

Identity is where user profile, scope, role, membership, and account-status changes happen. When one of those changes, other services need to know so they can (a) **enforce security**, stopping trust in a token or session when a user is disabled or a role or scope is revoked, and (b) **sync data**, keeping a local projection of user, role, and scope. The requirement (2026-07-21) adds a non-negotiable direction: **events flow outward only.** Nami is a producer and never depends back on a business service. The worked example that prompted it: a subscription tier (trial, premium, standard) is billing-service data, so should it go into the JWT?

Reconciling this against v1 first, so nothing already built is rebuilt:

* **Security enforcement for OIDC relying parties is already solved in v1.** ADR-0019 settled building OIDC Back-Channel Logout 1.0 (minting a `logout_token` carrying `sub`/`sid`/`events` and pushing it to each relying party's `backchannel_logout_uri` when a session ends), on the server-side session store (ADR-0003), and ADR-0039 settled cross-node revocation. This decision therefore **reuses** that seam and does not re-propose it.
* **The real remaining gap** is an outbound integration-event stream so that a **backend microservice that is not an OIDC relying party** also learns of a change. Back-channel logout tells a relying party that a session ended; it gives an arbitrary backend service no event stream from which to build a projection or react in its own domain. That is the new build.

Scope is **v2**: additive, kill-switchable, and outside the decision-complete v1, in the same spirit as ADR-0034.

## Decision Drivers

* A backend consumer needs a reliable stream to enforce and to sync, without inverting the dependency direction.
* Do not rebuild what v1 has (back-channel logout, revocation propagation).
* No dual-write inconsistency: an event must be sent if and only if the state change commits.
* Portability: no broker lock-in and no cloud lock-in.
* OSS-only (ADR-0026), which rules out the commercial message-bus abstraction.
* Tenant isolation per ADR-0001/0033 must hold for events as it does for data.
* Additive, non-breaking, and kill-switchable for v1.
* Events carry PII, so the data flow needs DPO and Legal ratification.

## Inherited constraints, not reversed

* **Outward-only.** Nami does not subscribe to a business service to enrich a token synchronously. The dependency direction is business to Identity, never Identity to business.
* **Publish only facts Nami owns**: user, credential, MFA, role, scope grant, membership, session. Data owned by another service (a subscription tier) is out of scope.
* **Isolation per ADR-0033/0001**: a single stream plus a `TenantId` on every event plus a row-level-security-guarded outbox table. Deliberately **no per-tenant topic**.

## Considered Options

* Security-signal channel: reuse the v1 back-channel logout and mirror one event onto the bus, or build a standards-based Shared Signals transmitter now.
* Delivery mechanism: a transactional outbox, direct publish inside the request (dual write), or change-data capture.
* Broker coupling: a thin port with one reference adapter, or a ready-made message-bus abstraction library.
* Business-owned claims: keep them out of the token, or embed them with a short TTL.

## Decision Outcome

Chosen: **open v2 scope for outward change-event publishing over a transactional outbox to a message broker**, in two tracks that are both outward.

* **Track A, security signals for OIDC relying parties: reuse v1.** Back-channel logout (ADR-0019) and revocation propagation (ADR-0039) are unchanged. The only addition is that the **same lifecycle seam also writes one outbox security-event**, so a backend consumer that is not a relying party learns of it. No logout logic changes.
* **Track B, integration events for backend consumers: new.** Outbox, then a relay, then an `IMessageTransporter` port, then the broker.
* **Reliability is the transactional outbox.** In the same local transaction that writes the state change, one outbox row is written; a separate relay drains it and publishes. This is what guarantees a message is sent if and only if the transaction commits, so there is no dual-write window. Delivery is **at-least-once**, so **every consumer must be idempotent** on the event id.
* **Transport is a thin `IMessageTransporter` port plus one reference adapter** (`Azure.Messaging.ServiceBus`, MIT), with Kafka and RabbitMQ as extension points. Reliability lives in the outbox, which is broker-independent, **not** in the transporter. The port carries `OrderingKey` and `IdempotencyKey` as first-class members so an adapter maps them to native primitives (a session id and message id, a partition key and idempotent producer, or a routing key and message id), so swapping brokers does not silently break ordering or deduplication. One adapter ships deliberately, to avoid maintaining and testing three.
* **The commercial message-bus abstraction is rejected** under ADR-0026 section A: its current major version is commercially licensed, and pinning its last OSS version to keep using it is exactly the trap that policy exists to prevent. A thin port over an OSS SDK is the alternative, consistent with ADR-0026 section B (build in-house rather than substitute another library).
* **Envelope is CloudEvents 1.0** (CNCF), which is broker-agnostic and has ready bindings for the candidate brokers, keeping the event contract portable.
* **Topology is a single stream plus `TenantId` plus outbox row-level security**, matching the ADR-0033 shared-host model.
* **Versioned taxonomy** `identity.<entity>.<change>.vN`. The event *vocabulary* was informed by a mainstream identity server's diagnostic-event naming, but only the names: its events are fire-and-forget telemetry with no delivery guarantee, so the mechanism is deliberately not borrowed.
* **Emit seam** is the user-management and tenant-admin write path plus one custom OpenIddict pipeline handler (`IOpenIddictServerHandler<TContext>`, which is OpenIddict's only extension hook) for token and session lifecycle. This is the **same seam that triggers the v1 back-channel logout**.
* **Business-owned claims stay out of the token and out of scope.** RFC 9068 makes `exp` required and defines **no revocation before expiry**, so a business value that changes out of band either forces Nami to call the owning service at issuance (the coupling this decision exists to avoid) or leaves the consumer enforcing a stale entitlement until the token expires. The owning service publishes its own events and the consumer keeps a projection. A short TTL does not fix this: it narrows the stale window without removing either horn.
* **Ordering requires an explicit sequence column, not the UUIDv7 primary key.** The outbox row id is a UUIDv7 and doubles as the CloudEvents id and idempotency key, but the relay drains `ORDER BY seq` on a separate `seq bigint GENERATED ALWAYS AS IDENTITY` column, because .NET's `Guid.CreateVersion7()` is not monotonic **within** a millisecond (see ADR-0036). Both columns are needed and neither substitutes for the other.

### Consequences

* Good, because a backend consumer gets a reliable stream for both enforcement and sync while the one-way dependency direction holds.
* Good, because it reuses heavily: back-channel logout (ADR-0019), revocation (ADR-0039), row-level-security isolation (ADR-0033), and the audit provenance seam (ADR-0008).
* Good, because CloudEvents plus a port keeps it free of broker and cloud lock-in.
* Bad, because it is substantial new code: an outbox table, a multi-node relay, a transporter, and emit hooks.
* Bad, because at-least-once delivery makes consumer idempotency mandatory, which must be stated plainly in the integration guide rather than left as an implementation detail.
* Bad, because the standards-based Shared Signals track is deferred, so standard interop with another vendor's identity system needs a separate transmitter effort (ADR-0068).
* Bad, because event payloads carry PII, so the data flow needs DPO and Legal ratification; this ADR does not assert compliance.

### Confirmation

* **Gate spike A-9 ran and passed on 2026-07-21: 10/10** (8 core PostgreSQL tests plus 2 RabbitMQ tests, all offline via containers). It proved outbox atomicity (a rollback leaves zero rows, and an outbox insert failing mid-transaction rolls the whole transaction back), at-least-once redelivery with no loss across a crash between publish and mark, a multi-node drain with `FOR UPDATE SKIP LOCKED` across 4 relays and 500 rows with no double publish, tenant isolation that fails closed when the tenant setting is absent, a dead-letter path where a poison event never reaches the broker and does not block others, per-subject ordering, and a load run of 6,400 events across 32 writers and 6 relays with zero loss. The Service Bus round trip is opt-in and was not run in that pass; the adapter compiles and the round-trip test is authored.
* The spike surfaced **three findings that changed the design rather than confirming it**, which is why it ran first:
  1. A row-level-security policy comparing a **uuid** tenant column must use `NULLIF(current_setting(...), '')::uuid`, because a **pooled** connection returns an empty string rather than NULL after the transaction ends, and casting an empty string to uuid **throws** instead of failing closed. The scope of the trap is the tenant column's **type**, not the release. **Resolved on 2026-08-01 by removing the type rather than managing the trap: every tenant discriminator, including this ADR's v2 outbox, is now `varchar(64)` text holding `Tenants.Identifier`, so the rule survives with zero instances** (design [02](../design/02-data.md) section 4 keeps it preventively). Three reasons, and the third is the decisive one for this ADR:
     * `.IsMultiTenant()` composes only against a string column, because Finbuckle's tenant identity is a string; a `Guid` tenant property throws at model build, so a uuid discriminator could not use the auto-stamp and query filter at all. Probe-verified 2026-08-01 at `Finbuckle.MultiTenant.EntityFrameworkCore` 10.1.2.
     * A text comparison fails closed by non-match, so the `NULLIF` cast is no longer something anyone can forget.
     * **This column does not stay in the database.** It is published as the event envelope's tenant value, and consumers match it against the `tenant` claim they receive on access tokens, which design [09](../design/09-federation-and-claims-profile.md) defines as a single **string tenant identifier**. A uuid column therefore implied a uuid-to-identifier conversion step before publish **that no document described**. Moving to text deletes that undesigned step rather than documenting it.
     * Effect on spike A-9: **none of substance.** A-9 proved transactional atomicity, `FOR UPDATE SKIP LOCKED` multi-node drain, `seq` ordering, dead-lettering, the de-privileged relay role, and the broker round trip. None of those depends on the tenant column's type; only the policy predicate changes. Recorded as a spike-versus-production delta rather than re-run.
     (History of this entry, kept because it has now been corrected three times on the same point. An early draft said "v1 is unaffected, since its tenant column is text", corrected 2026-07-25: true of the OpenIddict discriminator, wrong about the control plane. A later draft listed four tables and flagged `TenantBranding` as an open item, closed 2026-07-26 by guarding it like every other per-tenant table, making the count five. The 2026-08-01 change above then moved all five to text, which is what makes the count zero. The direction of travel is worth noting: each correction found that the uuid form was *more* trouble than the last draft thought.)
  2. UUIDv7 is not intra-millisecond monotonic, which is why the `seq` column exists (above).
  3. Broker deduplication capability differs: one candidate broker has native duplicate detection keyed on the message id and another has none, proven by publishing the same id twice and observing two deliveries collapsed to one application. **Consumer-side inbox deduplication is therefore mandatory regardless of broker**, and swapping brokers does not change the consumer contract.
* The deferral of a Shared Signals transmitter is evidence-based, not a preference: the specifications only reached Final in September 2025, there is no first-party or widely adopted .NET transmitter or receiver library, and vendor support is uneven (one vendor has a production transmitter and receiver, another ships an experimental transmitter that is off by default with no receiver). See ADR-0068, which this evidence supports.
* Build-time ratifications: **DPO and Legal** on the PII in payloads flowing to the bus and its consumers, the data flow, and event retention (control DP.01); **Security** on per-consumer bus access control, whether configuring the broker connection is an IAM-class change needing dual-control, and thin-versus-fat payloads for PII minimization; **Ops** on broker sizing, dead-letter policy, and alerting.

## Pros and Cons of the Options

### Security-signal channel

* **Reuse the v1 back-channel logout and mirror one outbox event (chosen)**: good, because nothing new is built for the relying party and only one emit is added at an existing seam; bad, because backend consumers get a Nami-shaped event rather than a standard one.
* **Build a Shared Signals transmitter now**: good, because it is the interoperable standard and the natural long-term answer; bad, because hand-rolling Security Event Token issuance (RFC 8417) and its push/poll delivery (RFC 8935/8936) is significant non-reusable work with no .NET library and thin interop today, so it is deferred rather than abandoned (ADR-0068).

### Delivery mechanism

* **Transactional outbox (chosen)**: good, because the send happens if and only if the transaction commits; bad, because it adds a table, a relay, and at-least-once semantics the consumer must absorb.
* **Direct publish in the request (dual write)**: good, because it is the least machinery; bad, because a commit that succeeds while the publish fails (or the reverse) loses or skews events with no guarantee, which is the failure this decision exists to prevent.
* **Change-data capture**: good, because it needs no application-level emit; bad, because it is heavy infrastructure, couples consumers to the database schema, and makes attaching a `TenantId` and domain semantics awkward. Reconsidered only if throughput demands it.

### Broker coupling

* **Thin port plus one reference adapter (chosen)**: good, because reliability stays in the broker-independent outbox and ordering/deduplication semantics are explicit in the port; bad, because a second broker is an implementation task rather than a configuration switch.
* **A ready-made message-bus abstraction**: good, because it is turnkey; bad, because the current major version is commercially licensed and therefore rejected by ADR-0026.

### Business-owned claims in the token

* **Keep them out (chosen)**: good, because it avoids issuance-time coupling and stale entitlements, and respects data ownership; bad, because a consumer must maintain a projection.
* **Embed with a short TTL**: good, because it is convenient for the consumer; bad, because it either couples issuance to the owning service or serves stale data within the TTL, and Nami does not own the data.

## Impact on v1

**Additive and non-breaking.** The kill switch is simply not calling the feature's registration extension, so the outbox, relay, and transporter are never registered, v1 runs identically, and nothing is added to the hot issuance path. (The concrete extension-method name follows the builder convention of ADR-0027 and is settled when the code lands; it is deliberately not fixed here.) The touch points are all reuse with no logic change: the ADR-0019 back-channel-logout seam gains one outbox emit at the point where session-end already fires; the revocation seam (ADR-0039) gains an event emit beside it without replacing the internal enforcement; and the user-management and authorization write paths gain an outbox emit inside the existing transaction.

## More Information

* Original decision 2026-07-21, imported into this repository as ADR-0071 (the source corpus numbered it 0036, which is taken here by the database-key strategy; ADR numbers are never reused or renumbered).
* **Boundary with ADR-0068**, which is easy to confuse: this ADR is a **Nami-shaped CloudEvents stream over a broker, for backend microservices**, accepted and spike-proven. ADR-0068 is a **standards-based Shared Signals transmitter emitting security-event tokens to external receivers**, and stays `proposed`. They are complementary rather than overlapping, and the ecosystem evidence recorded here is precisely why ADR-0068 is not yet accepted.
* Related decisions: ADR-0001 (multi-tenancy, the `TenantId` every event carries), ADR-0003 (the session store behind the lifecycle seam), ADR-0004/0005 (token posture, why a business claim does not enter the token), ADR-0008 (audit, which shares the provenance seam but is a different subsystem and never conflated with event publishing), ADR-0019 (back-channel logout, reused not rebuilt), ADR-0024 (ports and adapters, the shape of the transporter port), ADR-0026 (OSS-only, which rejects the commercial bus abstraction), ADR-0033 (the isolation model the event topology follows), ADR-0036 (the UUIDv7 key strategy and its intra-millisecond caveat that forces the `seq` column), ADR-0037 (PostgreSQL, whose `FOR UPDATE SKIP LOCKED` the multi-node drain relies on), ADR-0039 (revocation propagation, the seam a security event mirrors), ADR-0040 (resiliency, which governs the relay's retry posture), and ADR-0068 (the deferred standards track).
* The implementation mini-spec (outbox schema, relay, taxonomy, envelope, emit hooks, error handling, and test matrix) is a separate design document.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial identity server's diagnostic-event API was generalized to its vocabulary contribution only; the commercial message-bus abstraction this decision rejects is described by category per ADR-0026 section A; CloudEvents, the broker SDKs, and the outbox pattern are retained as neutral technical references.
