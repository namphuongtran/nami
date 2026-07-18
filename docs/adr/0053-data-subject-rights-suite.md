---
status: "accepted"
date: 2026-07-06
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: DPO and Legal (the response SLAs, the high-risk threshold, the jurisdiction wording, the Art.30 record content, DPIA execution, and consent policy-version governance must be ratified before production); GDPR Art.7(1), Art.15/16/18/20/21, Art.30/33/34/35; EDPB
informed: all contributors, via this repository
---

# Build the data-subject-rights suite (access, portability, rectification, restriction, objection), consent receipts, and breach hooks as reusable mechanisms

## Context and Problem Statement

ADR-0016 reconciled erasure (Article 17) with the immutable audit chain, but a data subject has more rights than erasure, and the product also carries breach and record-keeping obligations. Access (Art.15), portability (Art.20), rectification (Art.16), restriction (Art.18), and objection (Art.21), plus demonstrable consent (Art.7(1)) and the breach duties (Art.33/34) with the processing register (Art.30) and DPIA (Art.35), were not designed anywhere. Handling each request out of band is inconsistent, misses hard constraints (rectifying data must not rewrite the tamper-evident audit chain), and cannot produce demonstrable consent evidence. As with ADR-0016, this ADR builds the *mechanism* and explicitly does **not** claim GDPR compliance; the controlling policy is whatever the DPO and Legal ratify.

## Decision Drivers

* Cover the data-subject rights beyond erasure, plus demonstrable consent and the breach/record obligations, with consistent in-product mechanisms.
* Reuse the machinery already built: the erasure data-map, the audit hash-chain, dual-control, the email subsystem, and the retention schedule.
* Preserve audit tamper-evidence: a rectification must not rewrite the append-only chain.
* Treat at-rest encryption and crypto-shred as a genuine lever that reduces the Art.34 notification obligation.
* Build mechanism only; the SLAs, thresholds, jurisdiction wording, and governance are the DPO's and Legal's to ratify, and are not waived.

## Considered Options

* Handle each right manually and out of band as requests arrive
* Build a unified data-subject-rights subsystem that reuses the erasure data-map, the audit chain, dual-control, and email

## Decision Outcome

Chosen option: "A unified data-subject-rights subsystem". The fixed mechanisms are:

* **A. Access (Art.15).** An `ISubjectDataExportService` read-saga walks the **same data-map as erasure** (identity, memberships, consents/authorizations, sessions, and audit-about-the-subject) and assembles a `SubjectAccessReport` (JSON): a copy of the personal data per store plus the eight required metadata blocks (purposes, categories, recipients, retention, rights, right-to-complain, source, automated decision-making). It runs under dual-control and step-up (ADR-0013), audits `dsar.access.fulfilled`, delivers safely, and redacts any cross-subject data.
* **B. Portability (Art.20).** A `SubjectPortabilityExport` is a narrow subset of the access export: only the data the subject *provided* (profile fields, consents) under a consent or contract legal basis and processed automatically, as structured machine-readable JSON. It excludes derived, audit, and security data. Direct transmission to another controller is optional and skipped in v1. The distinction is explicit: access is everything the subject may see; portability is only provided, consent/contract-basis data.
* **C. Rectification (Art.16).** Identity and profile data is mutable and is corrected through self-service and admin-assisted edits (dual-control for sensitive fields), propagated to derived read-models and caches, and audited as `subject.rectified`. The audit hash-chain is **never rewritten**: rectification appends a correction-note event rather than editing the original row, preserving tamper-evidence (ADR-0008).
* **D. Restriction (Art.18).** A `ProcessingRestriction` state (in the control plane, tenant-columned; reason, scope, started/lifted) puts the subject into store-only mode: `CanSignInAsync` is false, no new tokens are issued, and the data is neither processed further nor erased. It interacts with the erasure saga (restriction is the alternative while a dispute is contested) and with consent (no consent-based new processing while restricted), and lifting it is audited.
* **E. Objection (Art.21).** The core authentication processing is contract or legal-obligation based, where Art.21 does not apply; objection applies to *optional* processing (analytics, non-essential notifications, marketing), where an objection flag stops the objected processing while essential authentication continues. Direct marketing is an absolute stop, and it reuses the suppression list of the email subsystem (ADR-0038); any legitimate-interest balancing is routed to the DPO.
* **F. Consent receipts (Art.7(1)).** On granting consent, an immutable, hash-chained consent-receipt event is emitted through the audit sink (ADR-0008) carrying the subject, client, tenant, scope set, purpose, legal basis, policy-version hash, timestamp, locale, and method; revocation emits a `consent.revoked` receipt. The mutable authorization is the current state; the receipt chain is the historical evidence.
* **G. Breach and records (Art.33/34/30/35).** A breach-scope assembler queries the audit hash-chain and the security-event taxonomy to compute the breach nature and affected-subject count and pre-fill the Art.33 notification (the ~72-hour authority notice), backed by an append-only breach register. An Art.34 severity gate notifies subjects on high risk, exempting data rendered unintelligible (encrypted or crypto-shredded), and fans out through the email priority lane (ADR-0038). An Art.30 record-of-processing stub is pre-filled from what the system knows (mandatory here, since health-adjacent special-category data has no small-organization exemption), and a DPIA-needed flag ships an input-pack for the DPO to execute.

The design/ratify boundary is explicit: A-G are the mechanisms (built here); the response SLAs, the Art.15 source/automated-decision wording, whether portability direct-transmit is offered, the Art.34 high-risk threshold and the supervisory-authority and deadline for the deployment's jurisdictions, the final Art.30 content, DPIA execution, and consent policy-version governance are DPO/Legal ratifications required before GA and are not waived.

### Consequences

* Good, because every major data-subject right, demonstrable consent, and the breach/record obligations have a consistent mechanism that reuses the erasure data-map, the audit chain, dual-control, and email, rather than ad-hoc handling.
* Good, because rectification preserves tamper-evidence (append, never rewrite), restriction is a real enforced state (no sign-in, no new token), consent is demonstrable via immutable receipts, and encryption/crypto-shred measurably reduces the Art.34 obligation.
* Bad, because it is a sizable subsystem (five rights plus consent receipts plus breach tooling) to build.
* Bad, and stated plainly, because it does not deliver compliance: the SLAs, thresholds, jurisdiction wording, Art.30 content, DPIA execution, and consent governance are DPO/Legal determinations that gate GA.
* Neutral, because it sits on top of ADR-0016: the erasure data-map, the crypto-shred, and the per-subject key stay there, and this ADR is the rights layer above them.

### Confirmation

* Tests: DSAR completeness across every store in the data-map with no cross-subject leak; portability returns the provided-data subset only; consent receipts are immutable; the breach-scope assembler is accurate; restriction blocks sign-in and token issuance; rectification appends a correction note rather than rewriting the chain; and objection stops optional processing while leaving essential authentication.
* Legal citations are primary-source (the GDPR article text), but the final wording, SLAs, thresholds, and jurisdiction are DPO/Legal's; nothing here asserts compliance.

## Pros and Cons of the Options

### Handle each right manually and out of band

* Good, because it needs no build.
* Bad, because it is inconsistent, cannot guarantee completeness across stores, risks rewriting the audit chain on a rectification, produces no demonstrable-consent evidence, and does not scale to breach timelines.

### A unified data-subject-rights subsystem (chosen)

* Good, because it is consistent, reuses existing machinery, preserves tamper-evidence, and turns the breach duties into assembler-assisted mechanisms.
* Bad, because it is a substantial build and still requires DPO/Legal ratification before it means compliance.

## More Information

* Recorded from the erasure/provisioning design (doc 23 Part C and §C.3bis), closing the access/portability/consent findings and adding rectification/restriction/objection; the mechanisms were designed 2026-07-05 to 2026-07-06. The per-subject data-encryption-key scheme behind crypto-shred is part of ADR-0016 (Article 17), which this ADR builds on rather than restates.
* Open follow-ups (DPO/Legal to ratify before GA): the Art.12 response SLAs; the Art.15 source and automated-decision wording; whether portability offers direct transmission; the Art.34 high-risk threshold and the jurisdictions' supervisory authority and deadline; the final Art.30 record content; DPIA execution; the consent policy-version governance; and the Art.18 restriction scope and Art.21 legitimate-interest balancing policy.
* Related decisions: ADR-0016 (erasure, whose data-map, crypto-shred, and per-subject key this reuses), ADR-0008 (the immutable audit chain behind consent receipts, the breach register, and the append-only rectification note), ADR-0007 (key-compromise, which the breach-scope assembler ties into), ADR-0005 (claim minimization, which shrinks the PII surface), ADR-0009 and ADR-0010 (dual-control and non-cascading capabilities gating access and sensitive rectification), ADR-0013 (step-up for a data-subject request), ADR-0038 (the email subsystem and suppression list used for breach notification and objection), and ADR-0001 (Pool/Silo traversal of the data-map).
* Authored in this repository in 2026-07 to record the settled data-subject-rights design as an ADR; it builds mechanism only and does not claim GDPR compliance. GDPR articles and EDPB guidance are cited factually as legal references, and no commercial competitor is named.
