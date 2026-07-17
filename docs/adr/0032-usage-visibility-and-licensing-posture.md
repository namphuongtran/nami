---
status: "accepted"
date: 2026-07-05
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Product; Legal and DPO (ratifying the data-protection and legal aspects is mandatory before ship, and this ADR does not itself judge compliance)
informed: all contributors, via this repository
---

# Gain usage visibility through free registration and opt-in telemetry, with an open-core-ready seam, keeping the core Apache-2.0

## Context and Problem Statement

The starting question was about a commercial identity server's license-key feature (free for non-production, a key for production), with the goal (set 2026-07-05) of seeing who uses the library (usage visibility) and monetizing later, if at all, via open-core.

A correction came from verifying that model (2026-07-05): a commercial identity server's license is a signed key that is **local**, validated at startup, does **not** phone home, and warns rather than blocks (a missing key only logs a warning and does not constrain the application), and its usage summary is local information for the operator, not sent back to the vendor. So a commercial license key does **not** "monitor who is using it"; it is a legal honor-license plus a nudge. Tracking usage is a different capability — phone-home telemetry — that such products deliberately avoid.

Two hard constraints follow. First, ADR-0027 fixed the core as Apache-2.0 and free, and Apache-2.0 permits free production use, so production cannot be gated while the core remains Apache-2.0 — a gate would be a license change, which is a Legal decision, not a technical one. Second, this is a security/identity product, so phone-home is extremely sensitive (community trust plus data-protection risk), and licensing must never block or degrade the IdP, because authentication is critical infrastructure.

## Decision Drivers

* Gain usage visibility for a self-hosted OSS product.
* Never gate, block, or degrade the IdP for licensing (critical infrastructure).
* Keep the core Apache-2.0 (reaffirming ADR-0027).
* Respect OSS trust and data protection: no covert or mandatory phone-home.
* Keep a monetization door open (open-core) without a rework or betraying the free positioning.

## Considered Options

* A production gate on the core
* Mandatory phone-home telemetry
* Consent-based visibility plus an open-core-ready seam, with the core staying Apache-2.0

## Decision Outcome

Chosen option: "Consent-based visibility plus an open-core-ready seam", with the core staying Apache-2.0 and free and with no production gate (reaffirming ADR-0027). A production gate conflicts with Apache-2.0 and risks critical infrastructure, and mandatory phone-home in an auth product is a trust-killer and a data-protection risk. Visibility comes from three consent-based channels plus one seam that opens the way to open-core.

* **A. Free registration key (a nudge, not a gate).** An operator optionally requests a free key (declaring an organization and email at registration) and configures it via `.AddNamiIdentity(o => o.RegistrationKey = ...)`, the config key `Nami:RegistrationKey` (environment `Nami__RegistrationKey`), or an optional `nami.registration.key` file. A missing key logs INFO once at startup, linking to the registration page — no warning-spam, no block, no degrade. The benefit is a registry of registered deployments: a real, honor-based, identity-level visibility channel that is legitimate because consent was given at registration.
* **B. Opt-in anonymous telemetry (off by default).** It is off by default and enabled only by explicit configuration — the config key `Nami:Telemetry:Enabled` (environment `Nami__Telemetry__Enabled=true`), with a short alias `NAMI_TELEMETRY=1`. When on, it sends aggregate, anonymized data to a Nami-operated endpoint: the product version, the .NET runtime, the feature flags in use, a bucketed tenant count (for example 1-10 or 11-100), and the instance count. Absolutely forbidden in the payload are issuer/tenant identity, `client_id`, user/`sub`, connection strings, key material, and any PII. There is an opt-out environment variable, and it is disclosed in the docs and a first-run notice; the telemetry client is OSS (ADR-0026); and the DPO ratifies the data categories before it can be enabled.
* **C. Passive (zero trust cost).** NuGet download statistics and GitHub stars/forks/dependents give coarse visibility with no code and no consumer impact.
* **D. Open-core-ready seam (built now, even though everything is free today).** An abstraction — `ILicenseContext`, `IProductRegistration`, and an operator-visible local `UsageSummary` (edition, registered organization, feature usage). Today the edition is "OSS/Community", every feature is free, and the registration key exists only for visibility (A). If open-core is ever adopted, the same seam issues a key for a separate premium package (for example advanced admin, a SIEM connector, dynamic IdP, a support SLA, or a hosted control plane), and the gate touches only the premium package and never the core, which stays Apache-2.0 forever. This keeps the monetization door open without a rework and without betraying the free positioning.

### Consequences

* Good, because visibility is obtained (registration plus opt-in telemetry plus passive stats) without a gate, without losing trust, and without reversing ADR-0027, and the seam is ready for a future open-core.
* Good, because critical-infrastructure safety is preserved: licensing never blocks or degrades the IdP.
* Bad, because visibility is best-effort and consent-based: only operators who register or opt into telemetry are seen, not everyone, which is the accepted price of respecting OSS trust and data protection.
* Bad, because it adds a registration service, a telemetry endpoint (both Nami-operated), and the seam code, which is small.
* Bad, because registration data and telemetry are data collection, so DPO/Legal must approve the data categories, retention, registration terms, and privacy notice before shipping.

### Confirmation

* A commercial identity server's licensing was verified (2026-07-05) as a local signed key with no phone-home that warns rather than blocks, confirming that a license key is not usage monitoring.
* Apache-2.0's grant includes free use and commercial rights, so production cannot be gated while the core remains Apache-2.0 (ADR-0027); a license change is a Legal decision.
* OSS telemetry norms: mainstream tooling (the .NET CLI, Serilog, VS Code) is opt-in/opt-out, disclosed, and anonymized, and security OSS generally avoids mandatory phone-home to keep trust.
* The open-core model (as practiced by GitLab, Grafana, and others before source-available relicensing) shows a free core plus a commercial premium is sustainable while keeping the core OSS.

## Pros and Cons of the Options

### A production gate on the core

* Good, because it would give the strongest usage leverage and a direct monetization path.
* Bad, because it conflicts with Apache-2.0 (ADR-0027), risks critical infrastructure by degrading auth, and would kill adoption.

### Mandatory phone-home telemetry

* Good, because it would give the most complete visibility.
* Bad, because covert or mandatory phone-home in an auth product destroys community trust and creates a data-protection risk.

### Consent-based visibility plus an open-core seam (chosen)

* Good, because it gains real visibility while keeping trust, the Apache-2.0 core, and critical-infrastructure safety, and it leaves an open-core door open.
* Bad, because visibility is only best-effort and consent-based, and the data-collection parts need DPO/Legal ratification.

## More Information

* The strategy and two defaults were ratified by Nam on 2026-07-05 (the Apache-2.0 core, registration (A), opt-in telemetry off by default (B), and the open-core seam (D); telemetry off by default, and a missing registration logging INFO once with no warning-spam). The data-protection and legal specifics remain mandatory ratifications before ship, and this ADR does not itself judge compliance.
* Open follow-ups: the DPO ratifies the telemetry data categories (anonymized, no PII), the registration PII (organization/email) retention, and the privacy notice; Legal ratifies the registration terms and, if open-core is later adopted, the commercial license for the premium package.
* Build-time: the `ILicenseContext`/`IProductRegistration`/`UsageSummary` seam, registration-key validation (a signed key with an embedded public key, warn-not-block), the opt-in OSS telemetry client, and the Nami-operated endpoint.
* Related decisions: ADR-0008 (audit, which is distinct from telemetry and never mixed with it), ADR-0022 (OpenTelemetry, whose infrastructure telemetry reuses), ADR-0026 (OSS-only, so the telemetry client must be OSS), and ADR-0027 (the Apache-2.0 packaging/distribution this reaffirms and extends with visibility and the open-core seam).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial identity server's licensing model was described generically (no vendor named, and its type names generalized); the product-name placeholder was set to the repository's `Nami.Identity.*` convention; and the open-core and telemetry-norm industry examples (GitLab, Grafana, the .NET CLI, and others) are retained as neutral factual precedent.
