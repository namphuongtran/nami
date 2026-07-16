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
* **Tamper-evidence**: append-only storage (INSERT-only grant, no UPDATE/DELETE), a hash-chain `record_hash = H(prev_hash || payload)`, forwarding to an external WORM/SIEM via an outbox, and a periodic integrity-check job.
* **Delivery guarantee**: security-critical events (token issued/revoked, admin config change, key rotation) commit **synchronously in the same transaction** as the action; the rest go asynchronously through the outbox but are still not lost (a sink being down must not create a blind spot).
* **PII/secret redaction** in the payload (claim minimization; never log raw secrets, tokens, or PII). This is reconciled with right-to-erasure (ADR-0016, crypto-shred): redaction removes secrets/tokens and non-essential PII, but the subject identifiers required for accountability and provenance (`actor_sub`, `on_behalf_of_subject`) cannot be redacted without losing traceability, so they are stored as **per-subject ciphertext inside the hashed payload**. That keeps `record_hash` stable when the DEK is destroyed at erasure. Precondition: the audit log's PII-identifier column is ciphertext-at-write. In short — redact-out the non-essential, encrypt-in-place the erasure-relevant identifiers.

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
* Bad, because it is lossy, mutable, offers no tamper-evidence or delivery guarantee, and does not separate audit from diagnostics — it fails the security bar.

### A first-class `ISecurityEventSink` (chosen)

A dedicated typed event sink with a hash-chain, an outbox delivery guarantee, and coverage of the negative paths.

* Good, because it is tamper-evident, delivery-guaranteed, and covers failures/denials/errors.
* Bad, because it adds a store, an outbox, and an integrity job, and puts some synchronous latency on the token path for critical events.

## More Information

* Original decision: 2026-06-28. The sink abstraction plus per-target adapter (cloud-agnostic) is accepted; the minimum event catalog, which events commit synchronously, the concrete SIEM/WORM destination, retention, and the PII-redaction policy await Security/DPO ratification.
* The audit destination is cloud-agnostic via a port plus an outbox forwarder, with per-target adapters (for example Azure Log Analytics immutable, AWS S3 Object Lock, GCP, Elastic, Splunk, or an OSS target), matching the direction of ADR-0006; the application binds no specific SIEM.
* Nami's catalog covers a security-event subset and adds lockout, refresh reuse, key rotation, and erasure events; `client_auth_failure` and `unhandled_exception` ensure abuse signals and the Error category are captured, not just operational failures.
* Related decisions: ADR-0001 (tenant-tagged audit), ADR-0006 (cloud-agnostic destination direction), ADR-0016 (right-to-erasure crypto-shred, reconciled with the hash-chain), ADR-0022 (the separate diagnostic-logging lane).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. The source referenced the right-to-erasure reconciliation only by design-document number; it is generalized here to ADR-0016, which is that decision's ADR.
