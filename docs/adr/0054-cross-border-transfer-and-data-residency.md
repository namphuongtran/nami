---
status: "accepted"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: DPO and Legal (whether a given transfer is lawful, the assessment content and filing, and a tenant's residency classification are theirs to ratify); Vietnam's Law on Personal Data Protection (Law 91/2025/QH15) and Decree 356/2025/ND-CP, the Law on Data (60/2024/QH15), and GDPR Chapter V, verified against current sources on 2026-07-18
informed: all contributors, via this repository
---

# Make data residency and cross-border personal-data transfer first-class, jurisdiction-profiled controls

## Context and Problem Statement

Nami is cloud-agnostic and can store and process data in any region (ADR-0006), and it is multi-tenant with a shared Pool tier and a dedicated Silo tier (ADR-0001). Some jurisdictions constrain where personal data may physically reside and require assessing or registering any transfer abroad, and none of the existing ADRs address where a tenant's data resides or the obligation to assess and record transfers. Two concrete regimes make this pressing:

* Vietnam's Law on Personal Data Protection (Law 91/2025/QH15, effective 1 January 2026) and its Decree 356/2025/ND-CP treat storing or processing Vietnam-collected personal data on servers, cloud services, or platforms **outside** Vietnam as a cross-border transfer, requiring a Cross-Border Transfer Impact Assessment filed with the Ministry of Public Security's cybersecurity department (A05) within 60 days of the first transfer, updated periodically, and retained by the operator. Vietnam's Law on Data (60/2024/QH15) additionally restricts exporting "core" and "important" data.
* GDPR (Chapter V) instead permits a transfer under an adequacy decision or a safeguard such as standard contractual clauses.

Because Nami is open source and deployed across many jurisdictions, this cannot be hardcoded to one country's rule; it must be jurisdiction-pluggable. As with the erasure and data-subject-rights ADRs, this builds the mechanism and the evidence and does **not** claim legal compliance; the legal determinations are the DPO's and Legal's.

## Decision Drivers

* Where personal data physically resides is a compliance-relevant, per-tenant and per-jurisdiction property, not an incidental deployment detail.
* Some jurisdictions require a filed, periodically updated transfer assessment (Vietnam), others a transfer safeguard (GDPR); the product must produce the evidence for whichever applies.
* The design must stay jurisdiction-agnostic, with each jurisdiction a ratified profile, since the same OSS runs in many markets.
* Mechanism only: whether a transfer is lawful and the filing itself are DPO/Legal determinations.

## Considered Options

* Ignore residency: one global region, storage location treated as an operations detail
* Residency-aware placement plus a cross-border transfer register, driven by a per-jurisdiction profile

## Decision Outcome

Chosen option: "Residency-aware placement plus a cross-border transfer register, driven by a per-jurisdiction profile". The fixed parameters are:

* **A. Residency is a first-class placement control aligned to the tenant tier.** A residency-constrained tenant runs in the **Silo** tier pinned to an in-jurisdiction region or cloud, with its own database and key-scope (ADR-0001, ADR-0033), so its personal data does not leave the jurisdiction. The **Pool** tier is used only for tenants with no residency constraint, or as an in-jurisdiction pool. Residency is recorded on the tenant registry (ADR-0001) as a ratified tenant attribute, and provisioning places the tenant accordingly (ADR-0017).
* **B. A cross-border transfer register.** Every flow of personal data across a jurisdiction boundary is recorded (the data categories, the origin and destination jurisdiction, the purpose, the legal basis or transfer mechanism, and timestamps) and appended to the tamper-evident audit chain (ADR-0008), so the operator can produce and periodically update whatever assessment a jurisdiction requires.
* **C. A jurisdiction profile carries the transfer rule and target.** For Vietnam, a Cross-Border Transfer Impact Assessment filed with MPS/A05 (a 60-day initial filing, periodic update, on-premises retention); for GDPR, transfer under an adequacy decision or a safeguard such as standard contractual clauses. The same profile also carries the sensitive-data categories and the breach authority and deadline used by ADR-0053, so all jurisdiction-specific parameters live in one place.
* **D. Mechanism only, with an explicit out-of-scope note.** Whether a specific transfer is permitted, the assessment content, the filing itself, and a tenant's residency classification are DPO/Legal determinations, not the software's. The Law on Data's "core" and "important" data export-approval regime concerns national data classes beyond an identity provider's personal-data role; it is flagged as out of scope and to be confirmed by Legal only if a deployment stores such classes.

### Consequences

* Good, because in-jurisdiction storage is achievable through Silo placement plus region pinning, so a Vietnam-resident (or other residency-bound) tenant can keep its data in-country.
* Good, because the transfer register produces the evidence a jurisdiction's assessment needs, and the jurisdiction profile keeps the design correct for GDPR, Vietnam, and future markets without hardcoding any one rule.
* Bad, because residency constrains topology: a residency-bound tenant cannot be pooled in a foreign region, which reduces pooling efficiency and may require in-country infrastructure.
* Bad, because the transfer register and the per-jurisdiction profiles are ongoing to maintain, and the register only helps if every genuine cross-border flow is actually recorded.
* Neutral, and stated plainly, because this does not deliver compliance: the lawfulness of a transfer, the assessment filing, and a tenant's residency classification are DPO/Legal's; this builds the mechanism and the evidence.

### Confirmation

* A residency-constrained tenant provisions Silo in the configured in-jurisdiction region, and its erasure/DSR data-map (ADR-0016, ADR-0053) shows no store outside that region.
* The transfer register captures a cross-jurisdiction flow with its data-category and legal-basis fields, on the audit chain.
* The active jurisdiction profile drives the correct breach authority and deadline (ADR-0053) and the correct transfer rule (a Vietnam profile yields the MPS/A05 assessment obligation; a GDPR profile yields the adequacy/safeguard path).

## Pros and Cons of the Options

### Ignore residency (one global region)

* Good, because it is the simplest to operate, with free choice of region for efficiency.
* Bad, because it cannot honor a residency constraint or produce a transfer assessment, so it is unusable where a jurisdiction restricts where personal data may reside or requires a filing.

### Residency-aware placement plus a transfer register and jurisdiction profile (chosen)

* Good, because it makes in-jurisdiction storage and the required transfer evidence achievable while staying jurisdiction-agnostic.
* Bad, because it constrains deployment topology and adds a register and per-jurisdiction profiles to maintain.

## More Information

* Authored 2026-07-18 in response to the requirement to support Vietnam's regime alongside GDPR. The Vietnam facts were verified against current sources on 2026-07-18: the Personal Data Protection Law (Law 91/2025/QH15, effective 1 January 2026) and Decree 356/2025/ND-CP; the earlier Decree 13/2023/ND-CP it replaces; and the Law on Data (60/2024/QH15, effective 1 July 2025) for the core/important-data export regime. These are cited as legal references; the ADR does not claim compliance and makes no legal determination.
* Related decisions: ADR-0001 (Pool/Silo tiers and the tenant registry where residency is recorded), ADR-0006 (cloud-agnostic storage and the region choice this constrains), ADR-0017 (tenant provisioning that places a tenant in a region), ADR-0033 (the per-Silo key scope that aligns with in-region isolation), ADR-0008 (the tamper-evident audit chain backing the transfer register), and ADR-0016 and ADR-0053 (erasure and data-subject rights, whose data-map bounds what crosses a border and whose jurisdiction profile this shares).
* Open follow-ups (DPO/Legal to ratify before production): the residency classification of each tenant; whether a given transfer is lawful and under which mechanism; the assessment content and the filing to the relevant authority; and, only if the deployment stores such classes, the Law on Data core/important-data export-approval path.
* Authored in this repository in 2026-07; Vietnamese and EU legal instruments are named factually as references, and no commercial competitor is named.
