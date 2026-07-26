# Nami Pre-GA Ratification Checklist

Every accepted ADR builds a **mechanism**; several defer a **policy, threshold, or sign-off** to a human owner (DPO, Legal, Security, Ops, Product) before general availability. This file consolidates those into one release gate. The ADR remains the source of each decision; this is the tracking index. **GA is blocked until every item below is ratified** (recorded with date and approver). Interim values may ship in pre-GA builds where an ADR marks them interim. This project builds mechanisms and does not itself assert legal compliance.

**Status key:** ☐ open · ☑ ratified (date, approver)

## DPO / Legal

- ☐ Audit: minimum event catalog, retention window, PII-redaction policy, SIEM/WORM destination ([ADR-0008](adr/0008-audit-subsystem.md))
- ☐ Erasure: whether crypto-shred satisfies Art.17; audit retention plus per-record Art.17(3) basis; legal-hold workflow; anonymise-in-place interpretation; Recital-66 replica retention ([ADR-0016](adr/0016-right-to-erasure.md))
- ☐ Data-subject rights: Art.12 SLAs; Art.15 source/automated-decision wording; portability direct-transmit yes/no; Art.34 high-risk threshold plus jurisdiction authority/deadline; Art.30 content; DPIA execution; consent policy-version governance; Art.18 scope plus Art.21 legitimate-interest balancing ([ADR-0053](adr/0053-data-subject-rights-suite.md))
- ☐ Cross-border / residency: per-tenant residency classification; lawfulness and mechanism of each transfer; transfer-assessment content and filing; Law-on-Data core/important-data export path if applicable ([ADR-0054](adr/0054-cross-border-transfer-and-data-residency.md))
- ☐ Telemetry / registration: telemetry data categories (no PII); registration PII retention; privacy notice; registration terms ([ADR-0032](adr/0032-usage-visibility-and-licensing-posture.md))
- ☐ HIBP breach-check: sending a hash prefix to an external service (DP.01) ([ADR-0028](adr/0028-user-management.md))
- ☐ Consent re-prompt cadence: whether a granted consent must expire and be re-asked periodically. ADR-0004 deliberately leaves consent lifetime unbounded (an OpenIddict permanent authorization persists until the user revokes it), so bounding it is a policy call, not a build gap. If it is required, the mechanism is an expiry stamped on `Authorization.Properties` plus a prune ([ADR-0004](adr/0004-refresh-token-posture.md))
- ☐ Deprovision key-escrow retention window and residency ([ADR-0017](adr/0017-tenant-provisioning-and-silo-migration.md))
- ☐ Change-event publishing (v2): PII in event payloads flowing to the broker and its consumers, the outward data flow, and event retention (DP.01) ([ADR-0071](adr/0071-identity-change-event-publishing.md))

## Security

- ☐ Break-glass (key compromise): authorized-personnel list; multi-node reload/cache-evict automation ([ADR-0007](adr/0007-key-compromise-break-glass-runbook.md))
- ☐ Admin break-glass (ISMS DP.01): custody, rotation cadence, unseal second-approver, alert recipients, network allow-list, drill cadence, FIDO2/CBA upgrade ([ADR-0015](adr/0015-admin-break-glass-and-first-admin-bootstrap.md))
- ☐ Secret-store: break-glass purge holder; two-approver process; per-cloud adapter capability matrix ([ADR-0009](adr/0009-secret-store-access-and-rollover.md))
- ☐ Delegated-admin: initial capability taxonomy; ReBAC-adoption timing ([ADR-0010](adr/0010-tenant-hierarchy-delegated-admin.md))
- ☐ MFA / assurance: AAL threshold per dangerous capability; per-scope required-`acr` list ([ADR-0013](adr/0013-mfa-assurance-and-step-up.md))
- ☐ Credential-hardening thresholds: length 12, PBKDF2 >= 210k (with DPO on HIBP) ([ADR-0028](adr/0028-user-management.md))
- ☐ Accepted risk: Pool-shared keyset (pool-group blast radius) ([ADR-0033](adr/0033-key-scope-isolation-model.md))
- ☐ Formal cryptoperiod (ISMS sign-off, with Ops): the signing, encryption, and retention periods stated as policy with a named owner. The build implements ADR-0011's 90/14/14; this ratifies it as the recorded cryptoperiod ([ADR-0011](adr/0011-no-restart-key-rotation.md), [ADR-0005](adr/0005-encryption-credential-lifecycle.md))
- ☐ Authorization SLO / timeout (with Ops) ([ADR-0047](adr/0047-authorization-decision-engine.md))
- ☐ Trusted-proxy IP list for mTLS (with Ops) ([ADR-0014](adr/0014-advanced-protocol-scope.md))
- ☐ Change-event publishing (v2): per-consumer bus access control; whether configuring the broker connection is an IAM-class change requiring dual-control; thin-versus-fat payload for PII minimization ([ADR-0071](adr/0071-identity-change-event-publishing.md))
- ☐ OWASP ASVS 5.0 Level 2 self-assessment coverage complete (L3 for key/token/dual-control/tenant-isolation), API Security Top 10 mapped ([ADR-0062](adr/0062-owasp-asvs-security-baseline.md))
- ☐ Independent penetration test: scope and rules of engagement agreed, a third party engaged, the test run against pre-production on synthetic data, and the findings ratified or accepted as risks. Scope covers the protocol endpoints, the admin API and admin application, tenant isolation (cross-tenant plus Pool shared keyset), and the break-glass paths; volumetric denial of service is out of scope ([ADR-0062](adr/0062-owasp-asvs-security-baseline.md), with [ADR-0033](adr/0033-key-scope-isolation-model.md), [ADR-0049](adr/0049-resource-server-per-tenant-validation.md), [ADR-0007](adr/0007-key-compromise-break-glass-runbook.md), [ADR-0015](adr/0015-admin-break-glass-and-first-admin-bootstrap.md))

## Ops

- ☐ Formal RTO/RPO targets, DR runbook, per-adapter capability matrix ([ADR-0006](adr/0006-disaster-recovery-key-material.md))
- ☐ Root-cert provisioning/rotation ceremony; per-environment cloud-protector adapter ([ADR-0012](adr/0012-key-bootstrap-and-dr-sequence.md))
- ☐ Public reference-host decision (owner / hosting / patch-cadence / cost) versus local-Docker-only ([ADR-0027](adr/0027-packaging-and-distribution.md))
- ☐ Change-event publishing (v2): broker sizing, dead-letter policy, and alerting; confirm the single-stream topology once real tenant numbers exist ([ADR-0071](adr/0071-identity-change-event-publishing.md))
- ☐ Database HA: the concrete failover mechanism (self-managed cluster manager versus managed HA); whether Redis durability is enabled, given the quantified proof-replay window it closes and the write-durability cost it adds; the read-replica trigger threshold if that lever is ever adopted; failover-drill cadence ([ADR-0074](adr/0074-database-ha-and-cache-durability.md))
- ☐ Edge posture: the concrete L7 edge stack (WAF, CDN, or reverse proxy) assumed in front of the reference deployment, plus the trusted-proxy addresses or networks that forwarded-header processing and the mTLS carve-out both depend on ([ADR-0073](adr/0073-edge-posture-and-forwarded-headers.md), with [ADR-0014](adr/0014-advanced-protocol-scope.md) for the mTLS trusted-proxy list)

## Product / Ops

- ☐ SLO numeric table plus error-budget policy ([ADR-0041](adr/0041-nfr-targets-and-slo-release-gate.md))
- ☐ Abuse and email throttle numbers (per-recipient caps) ([ADR-0042](adr/0042-abuse-and-bot-defense.md) / [ADR-0038](adr/0038-email-notification-subsystem.md))
- ☐ Overload-protection numbers: request timeouts, concurrency limits, and rate-limit windows and quotas, tuned against the capacity model. Distinct from the abuse and email caps above, which answer hostile volume rather than capacity ([ADR-0040](adr/0040-resiliency-and-overload-protection.md))
- ☐ Suppression store: hash-versus-encrypt and soft-bounce TTL (with DPO) ([ADR-0038](adr/0038-email-notification-subsystem.md))
- ☐ OpenID certification profile set plus budget/timing ([ADR-0027](adr/0027-packaging-and-distribution.md))

## Governance & launch (cross-cutting)

- ☐ Security disclosure window plus `security@` contact plus PGP key ([ADR-0045](adr/0045-security-disclosure-and-cve-policy.md))
- ☐ DCO versus CLA; self-govern versus software-foundation membership ([ADR-0046](adr/0046-governance-and-contribution-model.md))
- ☐ AI-contribution IP/DCO approach confirmed for the distribution model (with the IP-lawyer review) ([ADR-0067](adr/0067-ai-assisted-development-governance.md))
- ☐ **One IP-lawyer review of the public docs before public launch** (standing recommendation). In scope, among the rest: the architecture layer follows the **arc42** template's chapter sequence and credits it in [`docs/architecture/README.md`](architecture/README.md). arc42 is CC BY-SA 4.0, whose ShareAlike term attaches to adaptations of the licensed material; this repository reproduces none of arc42's text, explanations, or diagrams and adapts the chapter set, so the position taken is that the documents are not adaptations and remain Apache-2.0 with the credit given as attribution. That position is worth one lawyer's confirmation, not a redesign. **The same question applies to the detailed-design layer**, whose section sequence follows the **IEEE 1016** software-design-description viewpoints, credited in [`docs/design/README.md`](design/README.md). IEEE 1016 is a paid standard rather than an openly licensed one, so the position is narrower and worth stating: only the viewpoint vocabulary and the section ordering are used, no standard text is reproduced, and the layer was reconciled from this project's own design corpus rather than from the standard document

---

*Maintenance: when an ADR's ratify-pending item is signed off, tick it here and note it in that ADR's More Information. Any new ratify-pending item added to an ADR is mirrored here.*
