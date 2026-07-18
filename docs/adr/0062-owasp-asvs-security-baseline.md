---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OWASP ASVS 5.0.0 (released 2025-05-30) and the OWASP API Security Top 10 (2023 edition), verified 2026-07-18; the existing security ADRs (ADR-0028, ADR-0042, ADR-0043, ADR-0047, ADR-0009, ADR-0014) and the testing strategy (ADR-0060)
informed: all contributors, via this repository
---

# Adopt OWASP ASVS as the security-verification baseline

## Context and Problem Statement

Nami makes many security decisions (hardening invariants in ADR-0043, abuse defense in ADR-0042, credential hardening in ADR-0028, least-privilege secret access in ADR-0009, the authorization engine in ADR-0047), and several of them already cite OWASP guidance in passing. But no ADR names a security-verification standard as the baseline the product is held to, so there is no shared answer to "how do we know the security surface is covered", no common vocabulary for a reviewer, and no way to tell whether a security test suite is comprehensive or merely a pile of ad-hoc cases.

For an identity provider, an unstructured security posture is a real risk: the threats are well understood and catalogued by the industry, and reinventing that catalogue is both wasteful and error-prone. This ADR adopts the OWASP standards as Nami's security-verification baseline, names the assurance level, ties the baseline to the testing strategy, and routes ratification to the pre-GA security sign-off. It builds a verification mechanism; it does not assert a certification.

## Decision Drivers

* A shared, industry-recognized baseline so security coverage is measurable, not a matter of opinion.
* A common vocabulary (requirement identifiers) for reviewers, contributors, and downstream adopters.
* Reuse of a maintained threat catalogue rather than a home-grown one.
* A pragmatic level: high enough for an IdP, without demanding a paid certification before the project has shipped.

## Considered Options

* No formal standard: rely on ad-hoc security review plus the tactical security ADRs.
* Adopt OWASP ASVS (with a named level) plus the API Security Top 10, self-verified and mapped to tests.
* Pursue a formal third-party certification or audit (paid ASVS assessment or penetration test) as the baseline.

## Decision Outcome

Chosen: "adopt OWASP ASVS plus the API Security Top 10, self-verified and mapped to tests." A formal third-party audit is deferred (it is valuable but premature for a pre-alpha OSS project, and can be added later without changing this baseline); an unstructured posture is rejected as unsafe for an IdP.

### The baseline (binding)

* **OWASP ASVS 5.0 Level 2 is the product-wide floor.** Level 2 is the appropriate assurance level for an application that holds credentials and issues tokens. **Level 3 applies to the highest-assurance components**: key management and signing (ADR-0005/0006/0011/0033), token issuance and validation, the dual-control admin path (ADR-0020), and tenant isolation (ADR-0049). This is ADR-0058's pragmatism guardrail applied to security: raise the bar where the blast radius is largest, do not impose L3 ceremony everywhere.
* **The OWASP API Security Top 10 (2023) is the threat checklist for the API and protocol surfaces.** Its top risks map directly onto decisions already made: Broken Object Level and Object Property Level Authorization (API1/API3) onto per-tenant authorization and the `ICheckAccess` engine (ADR-0047) and resource-server tenant isolation (ADR-0049); Broken Authentication (API2) onto credential hardening and MFA (ADR-0028/0013) and the hardening invariants (ADR-0043); Unrestricted Resource Consumption (API4) onto rate-limiting and abuse defense (ADR-0040/0042); Improper Inventory Management (API9) onto the versioned public-API seam and self-service registration (ADR-0044/0035).
* **The baseline tracks the current stable OWASP edition.** ASVS 5.0 and API Security Top 10 2023 are the current editions; on a major OWASP release the baseline is re-mapped, the same pinned-and-tracked discipline ADR-0061 applies to the stack. Note that ADR-0043's citation of "ASVS V3 (session management)" used the 4.x chapter numbering; it maps to the 5.0 equivalent when its tests are written.

### How it is verified (binding)

* **Security tests carry their ASVS requirement identifier.** Under the behavior-first testing strategy (ADR-0060), a security test names the ASVS requirement it verifies, so the suite doubles as ASVS coverage evidence and a reviewer can see which requirements are exercised. The existing security-relevant negative tests (the spoofed client-cert rejection, the cross-tenant validation failure) are the first entries.
* **Coverage is self-assessed and documented.** Nami self-assesses against L2 (and L3 for the components above) and records the coverage; ASVS is a self-verification standard, and Nami does not claim an external certification it has not undergone.
* **Tooling.** Static analysis and dependency scanning run in CI alongside the license-scan gate (ADR-0026) and the coordinated-disclosure/CVE process (ADR-0045); dependency scanning is already scaffolded (`.github/dependabot.yml`, NuGet ecosystem enabled at M1). The specific analyzers are an open, replaceable choice, not pinned here.
* **Ratification.** L2 self-assessment coverage complete (and the API Top 10 mapped) is a pre-GA security sign-off item; GA is blocked until it is ratified (the Pre-GA Ratification Checklist).

### Consequences

* Good, because security coverage becomes measurable against a recognized catalogue, with a shared requirement vocabulary for reviewers and adopters.
* Good, because it reuses a maintained threat catalogue instead of a home-grown one, and the top API risks already have owning decisions, so this ADR mostly names and ties together what exists.
* Good, because the level is pragmatic (L2 floor, L3 where it matters) and does not block the project on a paid audit.
* Good, because mapping tests to requirements makes the testing strategy's security tests auditable rather than anecdotal.
* Bad, because self-assessment is weaker than an external audit; mitigated by keeping a formal audit as a named later addition and by the CVE process for what slips through.
* Bad, because pinning to specific OWASP editions invites rot; mitigated by the track-the-current-edition rule and the re-map-on-major-release discipline.

## Pros and Cons of the Options

### No formal standard

* Good, because it needs no adoption work and the tactical ADRs already cover a lot.
* Bad, because coverage is unmeasurable and reviewer-dependent, and gaps are invisible until exploited, which is unacceptable for an IdP.

### OWASP ASVS plus API Security Top 10, self-verified (chosen)

* Good, because it is the industry baseline, gives a shared vocabulary, reuses a maintained catalogue, and ties cleanly into the testing strategy and the pre-GA gate.
* Bad, because self-assessment is not an external audit; accepted, with a formal audit named as a later addition.

### Formal third-party certification or audit

* Good, because it is the strongest assurance.
* Bad, because it is premature and costly for a pre-alpha OSS project; deferred, not rejected, and it layers on top of this baseline when the time comes.

## More Information

* Related decisions: ADR-0043 (hardening invariants, already ASVS-citing), ADR-0042 (abuse defense, OWASP anti-automation), ADR-0028 (credential hardening, HIBP), ADR-0047 and ADR-0049 (authorization and tenant isolation, the BOLA/BOPLA surface), ADR-0040 (overload protection, resource consumption), ADR-0013 (MFA assurance), ADR-0009 (secret least-privilege), ADR-0060 (the testing strategy that carries the ASVS mapping), ADR-0026 (license-scan and dependency policy), ADR-0045 (coordinated disclosure and CVEs), ADR-0044 and ADR-0035 (the API-inventory surface), and ADR-0058 (the pragmatism guardrail applied to assurance levels).
* Standards verified on 2026-07-18: OWASP ASVS 5.0.0 (2025-05-30) and OWASP API Security Top 10 (2023 edition). Both are named factually for identification; no commercial competitor is named.
* Build-time follow-up: as each security test is written (from M1), tag it with its ASVS 5.0 requirement identifier and complete the L2 coverage record for the pre-GA security sign-off.
* Authored fresh for this repository.
