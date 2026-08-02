---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: DPO (the minimum event catalog, retention, the concrete SIEM/WORM destination, and the PII-redaction policy await Security/DPO ratification)
informed: all contributors, via this repository
---

# Make the audit subsystem first-class, tamper-evident, and delivery-guaranteed

## Context and Problem Statement

An early plan called the audit trail "immutable", but that was only an adjective with no mechanism behind it. OpenIddict has no built-in security-event abstraction (unlike some commercial identity servers), and ordinary `ILogger` logging is not enough for a security audit: it is lossy, mutable, and tends to miss the negative paths (failures, denials, errors). For sensitive data and an ISMS posture, the audit trail must cover failure/denied/error events, be tamper-evident, and never drop a critical event. How should Nami build audit?

## Decision Drivers

* A sensitive-data and ISMS posture requires audit that covers negative paths, resists tampering, and never loses a critical event.
* Audit must be distinct from diagnostic logging, because the two have different integrity and delivery guarantees.
* The audit destination must stay cloud-agnostic, matching the direction set in ADR-0006.
* Every security event carries tenant context per ADR-0001; the audit store is global and tenant-tagged by default, with a possible separate store/SIEM destination for hard-isolated Silo tenants.

## Considered Options

* Structured `ILogger` logging used as the audit trail
* A first-class `ISecurityEventSink` with a typed catalog, a hash-chain, and a delivery guarantee

## Decision Outcome

Chosen option: "A first-class `ISecurityEventSink`", because `ILogger` cannot provide tamper-evidence or a delivery guarantee and does not distinguish audit from diagnostics.

Fixed parameters of the decision:

* **`ISecurityEventSink` plus a typed event catalog** covering success, failure, denial, and error: login success/failure, lockout, token issued/revoked, consent grant/revoke, refresh reuse detected, admin config change, key rotation, force-logout, key purge, erasure, `client_auth_failure` (a credential-stuffing signal on `/token`), and `unhandled_exception`/system-error (so the Error category is covered, not just operational failures).
* **Emit from multiple seams**: OpenIddict event handlers (token/authorize), `SignInManager` (login/lockout), and the admin application layer (privileged CRUD).
* **Tamper-evidence**: append-only storage (INSERT-only grant, no UPDATE/DELETE), a **keyed** hash-chain, forwarding to an external WORM/SIEM via an outbox, and a periodic integrity-check job. The chain is `RecordHash = HMAC_k(PrevHash || canonical(fields))`, and all three properties are load-bearing: the HMAC is **keyed with an application-held key** rather than a bare `SHA-256`, so an attacker who can write to the database still cannot recompute a valid chain (append-only grants do not stop a superuser, which is exactly why the chain exists); the operands are **prev-first** (`PrevHash`, then the canonical record), the standard hash-chain convention, so an independent verifier can reproduce it; and the record is canonicalized to **TEXT** before hashing, because PostgreSQL `jsonb` does not preserve input byte order. The genesis `PrevHash` is 32 zero bytes, not a string.
* **Delivery guarantee**: security-critical events (token issued/revoked, admin config change, key rotation) commit **synchronously in the same transaction** as the action; the rest go asynchronously through the outbox but are still not lost (a sink being down must not create a blind spot).
* **PII/secret redaction** in the payload (claim minimization; never log raw secrets, tokens, or PII). This is reconciled with right-to-erasure (ADR-0016 option **A.4-2**, crypto-shred): redaction removes secrets/tokens and non-essential PII, but the subject identifiers required for accountability and provenance (`actor_sub`, `on_behalf_of_subject`) cannot be redacted without losing traceability, so they are stored as **per-subject ciphertext inside the hashed payload**. That keeps `record_hash` stable when the DEK is destroyed at erasure. Precondition: the audit log's PII-identifier column is ciphertext-at-write. In short: redact-out the non-essential, encrypt-in-place the erasure-relevant identifiers.

* **Bulk audit egress is one gated path, and what leaves has to be verifiable by whoever receives it (added 2026-08-02).** Audit is read through the admin viewer (design [16](../design/16-admin-app.md) section 3) and forwarded continuously to a WORM/SIEM destination by the outbox above, and between them those two cover archival, analysis, and investigation. Neither covers the one case left: handing a defined record set to a party who gets no console and is not wired into that destination, meaning an external auditor, a regulator, a litigation hold, or a tenant assembling evidence for its own auditor. A bulk export therefore exists, and four things about it are fixed here.
  * **Every export is dual-control, and there is no below-threshold direct path.** `POST /audit/export` always raises the `audit-export` proposal and answers `202`. The purpose above does not vary with size: a five-hundred-row evidence package is the same personal-data egress to the same outside party as a fifty-thousand-row one, and anyone able to call a direct path already has the viewer, so the small path added a second contract without adding a capability. Four design documents described that path and none of them, nor the corpus OpenAPI they came from (which declares only `202`, `401`, and `403`), ever said what it returned. The specification that was never written is the evidence that it had no distinct job.
  * **The artifact carries the chain, not the read DTO.** `AuditEntryDto` (design [15](../design/15-admin-api.md) section 3.8) carries neither `RecordHash` nor `PrevHash`, and `GET /audit/chain-status` is the server asserting that its own chain is intact. An auditor can use neither, for the same reason this ADR keys the chain at all. The export therefore carries the per-row hashes and the canonical fields the chain is computed over, which is what makes the prev-first operand order above usable by the "independent verifier" it was chosen for rather than only by Nami.
  * **Nothing bearing personal data is stored at rest to make this work.** Approval mints a single-use, time-boxed **export grant** bound to the frozen filter, and the proposer redeems it in one transfer that streams from the audit store. There is no generated file, so there is no second copy of audit personal data, no artifact retention window to ratify, and no object store to isolate per tenant. The invariant is one successful egress per grant; whether a transfer that fails mid-stream may be retried inside the grant's window is a build-time detail and not a relaxation of it.
  * **The egress is audited, not only the approval.** Because the data moves at redemption rather than at approval, `proposal.executed` on its own would leave a bulk personal-data egress with no record of the egress. A distinct `audit.export.delivered` event records the redemption, carrying the grant id, the digest of the frozen filter (ADR-0081), the row count actually transferred, and the actor. It is a proposed addition to the minimum catalog and inherits the Security and DPO ratification the rest of that catalog awaits.

### Consequences

* Good, because the audit trail is trustworthy for investigation and compliance, supports abuse detection, and provides tamper-evident evidence.
* Bad, because it adds a store, an outbox, and an integrity job, and the synchronous writes add latency on the token path (only for critical events), which must be measured and optimized.
* This replaces the `IEventSink` → `ILogger` approach in the earlier patterns design with an `IAuditSink`, and it is the foundation for alerting and the admin audit view.

### Confirmation

* **Two separate lanes** (per ADR-0022): the audit lane is `ISecurityEventSink`/`IAuditSink` (hash-chain plus delivery guarantee, with a cloud-agnostic sink to WORM/SIEM); the diagnostic lane is `ILogger` plus OpenTelemetry. Audit **never** routes through the OpenTelemetry/`ILogger` pipeline (which lacks tamper-evidence and a delivery guarantee); the two lanes are joined only by a correlation/trace id.
* The sink is swappable (a local database plus a forwarder to a SIEM/WORM destination).
* DPO sign-off covers retention and audit content (no excess PII).

## Pros and Cons of the Options

### Structured `ILogger` logging used as the audit trail

Reuse the diagnostic logging pipeline as the audit record.

* Good, because it needs no new infrastructure.
* Bad, because it is lossy, mutable, offers no tamper-evidence or delivery guarantee, and does not separate audit from diagnostics, so it fails the security bar.

### A first-class `ISecurityEventSink` (chosen)

A dedicated typed event sink with a hash-chain, an outbox delivery guarantee, and coverage of the negative paths.

* Good, because it is tamper-evident, delivery-guaranteed, and covers failures/denials/errors.
* Bad, because it adds a store, an outbox, and an integrity job, and puts some synchronous latency on the token path for critical events.

## More Information

* Original decision: 2026-06-28. The sink abstraction plus per-target adapter (cloud-agnostic) is accepted; the minimum event catalog, which events commit synchronously, the concrete SIEM/WORM destination, retention, and the PII-redaction policy await Security/DPO ratification.
* The audit destination is cloud-agnostic via a port plus an outbox forwarder, with per-target adapters (for example Azure Log Analytics immutable, AWS S3 Object Lock, GCP, Elastic, Splunk, or an OSS target), matching the direction of ADR-0006; the application binds no specific SIEM.
* Nami's catalog covers a security-event subset and adds lockout, refresh reuse, key rotation, and erasure events; `client_auth_failure` and `unhandled_exception` ensure abuse signals and the Error category are captured, not just operational failures.
* **The bulk-export parameter was added 2026-08-02**, after a measurement found the route specified in design 15 with no response shape, its thresholds carrying a Security ratification in the source corpus that was lost on import, and this ADR, the authority for the subsystem, not mentioning export at all. The scoping question was asked before the specifying one and it moved the answer twice: most reasons for an export turned out to be served already by the WORM/SIEM forwarder or by the data-subject-rights mechanisms (ADR-0053 excludes audit from portability outright), which nearly retired the feature, and what survived was the independent-verifiability case that neither the viewer nor the read API can serve. Recorded because the reasoning is not recoverable from the parameter.
* Related decisions: ADR-0001 (tenant-tagged audit), ADR-0006 (cloud-agnostic destination direction), ADR-0016 (right-to-erasure crypto-shred, reconciled with the hash-chain), ADR-0022 (the separate diagnostic-logging lane), ADR-0081 (the `query`-class target guard the export proposal runs under, whose evaluation moment the export parameter moves to redemption), ADR-0079 and ADR-0020 (the proposal route rules and the application-layer dual-control this export is raised through), and ADR-0053 (the data-subject-rights suite, which is the mechanism a subject's own request goes through instead of this one).
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25. The reconcile upgraded the hash-chain formula from a bare `H()` to the keyed `HMAC_k` form with its three load-bearing properties. The bare form was a real weakness, not a notation shorthand: without an application-held key, anyone able to write audit rows could recompute the whole chain, which defeats the tamper-evidence this ADR exists to provide. The keying is the database design's recommendation, adopted in the audit detailed design; the prev-first operand order is the part all sources already agreed on. The source referenced the right-to-erasure reconciliation only by design-document number; it is generalized here to ADR-0016, which is that decision's ADR.
