---
status: "accepted"
date: 2026-06-29
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: DPO and Legal (the retention window, the Art.17(3) basis per record, and the legal acceptability of crypto-shred must be ratified before production); GDPR Art.17, EDPB Guidelines 01/2025 and 02/2025, WP29 WP216, NIST SP 800-88 Rev.2
informed: all contributors, via this repository
---

# Reconcile GDPR right-to-erasure with the immutable audit chain using chain-over-commitments

## Context and Problem Statement

A data subject's records span three erasable planes (global identity, global control, and tenant operational) plus one append-only, hash-chained audit plane that **cannot** be deleted — deleting it would break tamper-evidence, violating ADR-0008 and the INSERT/SELECT-only database grant. GDPR Article 17 requires erasure. The tension is between the erasable planes and the immutable audit. This needs a long-term design, not a workaround such as turning off the hash-chain or quietly deleting a row and recomputing. Important framing: this ADR builds the *mechanism*; it does not claim GDPR compliance, and the controlling policy is whatever the DPO ratifies.

## Decision Drivers

* Satisfy Article 17 erasure without breaking audit tamper-evidence (ADR-0008).
* A durable design, not a workaround that disables or fakes the hash-chain.
* Reuse the machinery Nami already has: dual-control, force-logout, the hash-chain, key management, and claim minimization.
* Build the mechanism only; the legal determinations are the DPO's and Legal's to ratify.

## Considered Options

* Hard-delete everything, including the audit plane
* Keep the audit intact and refuse erasure
* Chain-over-commitments: keep PII out of the hashed payload, crypto-shred it, or anonymise in place while preserving the original hash

## Decision Outcome

Chosen option: "Chain-over-commitments", because it keeps the audit chain verifiable while still satisfying erasure, whereas hard-deleting the audit breaks tamper-evidence and refusing erasure violates Article 17.

The identity and operational planes are hard-deleted (the project is not event-sourced, so only the AuditLog is append-only — a significant advantage). Only the audit plane gets special handling, in order of legal preference:

* **(A.4-1) PII-outside-the-chain (preferred)**: the audit payload stores only an opaque `SubjectRef` plus an HMAC, and the `SubjectRef` → subject mapping lives in a separate, deletable table. Erasure deletes the mapping, so the chain never hashed real PII and stays both intact and non-attributable.
* **(A.4-2) Crypto-shred embedded PII**: for a field that must live in the event, encrypt it with a per-subject key held outside the audit store, and destroy that key on erasure. The record hash covers the ciphertext and does not change. The per-subject data-encryption key is governed by the key-management model (ADR-0006 and ADR-0009), which this ADR relies on rather than redefines.
* **(A.4-3) Anonymise-in-place while preserving the original hash** (last resort).
* **Erasure saga `ISubjectErasureService`**, idempotent and resumable: intake and guard (legal hold plus a retention split) → a dual-control gate (a non-cascading data-export/IAM-change-class capability, proposer ≠ approver, ADR-0009 and ADR-0010) → revoke live access **first** (`FindBySubjectAsync` → `TryRevokeAsync` per tenant, Pool filter or Silo connection, plus deleting server-side sessions) → delete tenant-operational data (FK-safe: tokens before authorizations) → delete global identity and control data → scrub the audit and append a `subject.erased` tombstone → verify (a `FindBySubject` returns empty and a chain recompute still validates).
* **Retention**: identity for the life of the account; tokens by TTL plus prune; audit obligation-bound (Art.17(3)(b)/(e) and Recital 65); diagnostic logs short and redacted; replica/SIEM erasure per Recital 66.

### Consequences

* Good, because erasure satisfies Article 17 while the chain still verifies, with no disabling of tamper-evidence; and it reuses existing machinery (dual-control per ADR-0009/0010, force-logout, the hash-chain per ADR-0008, key management per ADR-0006), while claim minimization (ADR-0005) shrinks the PII surface and makes erasure tractable.
* Bad, because crypto-shred (A.4-2) has only emerging, conditional legal support, so it is the fallback behind A.4-1 rather than the default.
* This decision depends on ADR-0008 (why the audit is immutable and the record-hash format), ADR-0005 (claim minimization), ADR-0009 and ADR-0010 (dual-control and non-cascading capabilities), ADR-0001 (Pool/Silo traversal), and ADR-0006 (the key-management model behind the crypto-shred key).

Crypto-shred legal nuance, to be read precisely: EDPB Guidelines 02/2025 (on blockchain) confirm that destroying the decryption key renders data "unintelligible", but with heavy caveats ("at least until the algorithm is broken … or if the key had already been compromised or leaked", and "encrypted personal data is still personal data"), and the erasure route the EDPB actually endorses is rendering data anonymous by deleting the off-chain identifying data, not a clean "key destruction equals erasure". That framing is blockchain-specific and the document is a public-consultation draft, not final. It must therefore **not** be read as a data-protection authority recognizing crypto-shred as Article 17 erasure in general; extending it beyond blockchain is an inference that needs DPO/Legal ratification (AES-256, irreversible, auditable, with the residual-ciphertext caveat and the fact that backups/replicas may still hold the key). Hence A.4-1 is preferred. Pseudonymisation (EDPB Guidelines 01/2025) remains personal data, so any surrogate must defeat singling-out and linkability (WP216) to count as true anonymisation.

### Confirmation

* Article 17 is not absolute: 17(3)(b) (legal obligation) and 17(3)(e) (legal claims), with Recital 65 (retain narrowly) and Recital 66 (erasure extends to copies).
* Standards reviewed: EDPB Guidelines 01/2025 on pseudonymisation (pseudonymised data is still personal data; the key is "additional information"); WP29 WP216 (anonymisation must defeat singling-out, linkability, and inference); NIST SP 800-88 Rev.2 (cryptographic erase counts as purge; Rev.1 was withdrawn on 2025-09-26). Chain-over-commitments is an established event-sourcing pattern.
* Verify-before-build: an erase-set verification plus a chain-verify recompute run as a CI/acceptance gate.

## Pros and Cons of the Options

### Hard-delete everything, including the audit plane

* Good, because it is the most literal reading of "erase everything".
* Bad, because it destroys tamper-evidence and non-repudiation (ADR-0008) and violates the INSERT/SELECT-only audit grant.

### Keep the audit intact and refuse erasure

* Good, because the audit chain is untouched.
* Bad, because it violates Article 17.

### Chain-over-commitments (chosen)

* Good, because the chain stays verifiable while erasure is satisfied, and it reuses existing dual-control, key, and hash-chain machinery.
* Bad, because it requires an ordered erasure saga and, for embedded PII, a crypto-shred whose legal standing is still emerging.

## More Information

* Original decision: 2026-06-29. This ADR builds the mechanism and explicitly does **not** claim GDPR compliance. The retention window, the Art.17(3) basis per record, and whether crypto-shred satisfies Article 17 await DPO/Legal ratification before production; this also resolves the open audit-retention item in ADR-0008 (the per-record Art.17(3) basis).
* Do not rely on a de-listed older EDPB opinion, and treat secondary "ICO/CNIL recognition" blog posts as secondary only.
* The erasure mechanism is jurisdiction-agnostic; the deletion right and the retention basis are per-jurisdiction policy (GDPR Article 17 and, for example, Vietnam's Personal Data Protection Law, Law 91/2025/QH15), ratified by DPO/Legal. Where the data being erased resides and whether it has crossed a border are addressed in ADR-0054.
* Open follow-ups (DPO/Legal to ratify): whether crypto-shred satisfies Article 17; the audit retention window and per-record basis; the legal-hold workflow; the anonymise-in-place interpretation; and SIEM/WORM replica retention (Recital 66).
* Related decisions: ADR-0001 (Pool/Silo traversal), ADR-0005 (claim minimization shrinks the PII surface), ADR-0006 (key management governing the crypto-shred key), ADR-0008 (immutable audit and record-hash format; this resolves its open retention item), ADR-0009 and ADR-0010 (dual-control and non-cascading capabilities), ADR-0053 (the broader data-subject-rights suite that reuses this erasure data-map, dual-control, and per-subject key), and ADR-0054 (cross-border transfer and residency of the data being erased).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. Legal and standards citations (GDPR articles, EDPB, WP29, NIST) are retained; the ADR-0006 reference is to the key-management model that would govern the per-subject crypto-shred key, which ADR-0006 does not itself define.
