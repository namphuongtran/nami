---
status: "accepted"
stack-record: true
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Legal (for unclear license boundaries and the product's redistribution model); public license classifications
informed: all contributors, via this repository
---

# Restrict dependencies to permissive OSS licenses, enforced by a CI license-scan gate

## Context and Problem Statement

The project has repeatedly chosen for license freedom in a piecemeal way: an Apache-2.0 protocol engine over a commercial identity server; PostgreSQL (OSS) over a commercial database; and OpenTofu (MPL) over Terraform (BSL, which is not OSS). The common reason is that the product may be redistributed, run as SaaS, and serve multiple tenants, so it must not be locked to a paid license, forced open by viral copyleft, or caught by a "switched-to-commercial" trap (several popular .NET packages recently moved to paid licensing). A single unified policy is needed instead of deciding package by package.

## Decision Drivers

* The product is redistributable, SaaS, and multi-tenant, so it must avoid paid-license lock-in, viral copyleft, and source-available-non-OSS licenses.
* A bad license should be caught at PR time, not after a deep build.
* Consistency across the whole license-freedom line.
* The rule must cover transitive dependencies, not only direct ones.

## Considered Options

* No policy, choosing per package
* Permissive-OSS-only, enforced by a CI license-scan gate with a controlled exception process
* Allow all OSS, including copyleft

## Decision Outcome

Chosen option: "Permissive-OSS-only, enforced automatically", because choosing per package risks a commercial, copyleft, or BSL package slipping in and being found late, while allowing copyleft risks a viral (GPL/AGPL) obligation on a redistributed product.

**A. Allowed (allow-list).**

* Permissive: MIT, Apache-2.0, BSD-2/3-Clause, MS-PL, the PostgreSQL License, Unlicense/CC0 (most of the current stack).
* Case-by-case, needing Architect and Legal approval recorded as an exception: MPL-2.0 and LGPL (file/dynamic-link scope), usually fine as an unmodified library, but only after confirming no open-source obligation propagates to the product.
* Forbidden: commercial/paid or dual-license with a paid tier (including "free up to a threshold, then paid"), for example a commercial identity server (already avoided), and popular .NET utility libraries that have moved to commercial licensing (verify currency at adopt time, since a license can change again); viral copyleft (GPL, AGPL); and source-available-non-OSS licenses (BSL, SSPL, the Elastic License, "Commons Clause"), with BSL already avoided in ADR-0023.

**B. Derived design principles.**

* Avoid a mediator library, since a widely-used one moved to commercial licensing; the ADR-0024 vertical slice uses plain handlers with no mandatory mediator, and if a mediator is ever needed, a verified OSS-permissive alternative is chosen.
* For object mapping, use hand-mapping or the source-generated Mapperly (MIT) rather than a commercial mapper.
* Prefer built-in .NET and `Microsoft.Extensions.*` (MIT) before adding a third party.
* The policy applies to transitive dependencies too, not only direct ones.

**C. Enforcement (the core mechanism).**

* A CI license-scan gate reads the license of every package (direct and transitive) from the restore graph and fails the build if any license falls outside the allow-list; it runs on every PR and every dependency bump (matching the contract-regression cadence of ADR-0021).
* Central Package Management is the single place that declares versions, which makes scanning and pinning straightforward.
* Exception process: a case-by-case or otherwise special package needs Architect approval (plus Legal for anything copyleft or commercial), recorded in `docs/DEPENDENCY-LICENSES.md` (package, license, reason, approver, date); there are no silent exceptions.
* An SBOM (CycloneDX) is generated in CI for license and supply-chain audit.

**D. Current confirmed-permissive list (re-verify each at adopt time).** OpenIddict (Apache-2.0); the Npgsql / EF Core PostgreSQL provider (PostgreSQL/MIT); Finbuckle.MultiTenant (Apache-2.0); Duende.AccessTokenManagement and Duende.AccessTokenManagement.OpenIdConnect (both Apache-2.0 at 4.2.0), which are free packages published from the vendor's separate FOSS repository and are a different product from its paid identity server, together with their transitive Duende.IdentityModel (Apache-2.0 at 8.1.0), Microsoft.Extensions.Caching.Hybrid, Microsoft.Extensions.Http.Resilience, and Microsoft.Extensions.Telemetry.Abstractions (all MIT); thomasduft/openiddict-ui as an OpenIddict permissions/UI pattern (MIT); OpenTelemetry .NET (Apache-2.0); MailKit/MimeKit (MIT); Fluid/Scriban (MIT/BSD); Testcontainers (MIT); FusionCache (MIT); TngTech.ArchUnitNET (Apache-2.0), with the original NetArchTest.Rules unmaintained since 2021 and an MIT fork available as a drop-in; MinVer (Apache-2.0); Microsoft.SourceLink.GitHub (MIT); Playwright (Apache-2.0); and Quartz.NET (Apache-2.0). Each is re-verified at adopt time, because a license can change, which is the reason for the gate.

**E. Naming a dependency in documentation.** A dependency is recorded by its **real package identifier**, even when that identifier carries the name of a company whose commercial products this project deliberately does not name. Concealing the identifier would make the dependency record factually wrong, leave a contributor unable to reproduce the restore graph, and defeat the section C gate, which matches on exact package IDs. This applies only to packages actually depended on. Product comparison, parity framing, a vendor's internal source or type references, its issue tracker and blog posts, and commercial packages this project rejects remain described generically, because naming those serves no dependency-record purpose. The local tooling that enforces this split is described in `scripts/README.md`.

### Consequences

* Good, because it makes the license-freedom line consistent, keeps commercial/viral/BSL dependencies out, catches a bad license at PR time rather than after a deep build, and produces an SBOM for audit.
* Good, because it proactively avoids the "switched-to-commercial" trap through principle B plus the gate.
* Bad, because it sometimes means hand-writing something (a mediator or a mapper) instead of using a convenient commercial library, a small cost for license freedom.
* Bad, because the allow-list, the scan tool, and the exception log must be maintained, and license-detection false positives need manual review.

### Confirmation

* The project line is consistent: an Apache-2.0 protocol engine over a commercial identity server, PostgreSQL over a commercial database, and OpenTofu (MPL) over Terraform (BSL) in ADR-0023. BSL, SSPL, and the Elastic License are source-available and not OSI-approved. Several widely-used .NET utility libraries moved to commercial licensing around 2024-2025 as the cautionary example; this is verified at adopt time rather than asserted permanently. Mapperly (MIT) is an OSS source-generated mapper. OSS license-scan tools and the CycloneDX SBOM standard exist.
* Build-time follow-ups: wire the CI license-scan gate (choosing a tool), create the `docs/DEPENDENCY-LICENSES.md` exception log, and generate the CycloneDX SBOM. Legal confirms the copyleft/redistribution boundary for the product's distribution model where needed.

## Pros and Cons of the Options

### No policy, choosing per package

* Good, because it needs no upfront machinery.
* Bad, because a commercial, copyleft, or BSL package can slip in and be discovered late, forcing rework or a license cost.

### Permissive-OSS-only with a CI gate (chosen)

* Good, because it is consistent, automated, and catches problems at PR time, with an SBOM for audit.
* Bad, because it costs an allow-list, a scan tool, and an exception log, and occasionally means writing code instead of using a commercial library.

### Allow all OSS, including copyleft

* Good, because it maximizes available libraries.
* Bad, because a viral GPL/AGPL obligation on a redistributed, SaaS, multi-tenant product is a serious risk.

## More Information

* Original decision 2026-07-04, accepted with defaults: the allow-list and forbidden list, and a copyleft stance where MPL/LGPL are case-by-case with approval (not hard-banned, but routed through the exception process, and through Legal when the boundary is unclear).
* The BFF token-management dependency is **settled, not open**: Duende.AccessTokenManagement.OpenIdConnect 4.2.0 (Apache-2.0) for the interactive OpenID Connect case used by ADR-0020 and ADR-0029, which brings Duende.AccessTokenManagement 4.2.0 in transitively. An earlier draft of this ADR recorded it as "to be selected, non-commercial and not vendor-branded". That was an artifact of over-generalizing during the 2026-07 import rather than a decision this project made, and it is corrected here under section E. Both packages, and their transitive Duende.IdentityModel 8.1.0, were verified Apache-2.0 at nuget.org on 2026-07-25 and target net10.0, which ADR-0030 requires. Note for ADR-0040: the base package registers its own retry-only pipeline on its own named clients, which that ADR now accounts for.
* Related decisions: ADR-0023 (OpenTofu MPL over Terraform BSL), ADR-0024 (the vertical slice using plain handlers, avoiding a commercial mediator), ADR-0030 (the .NET target that the section D pins must support), ADR-0040 (the outbound resilience posture, which section D's token-management packages interact with), and ADR-0067 (the AI-assisted-development policy that requires AI output to pass this permissive-only line).
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25. The 2026-07 import had over-generalized: it turned a settled token-management dependency into an open choice and invented a "not vendor-branded" constraint this project never decided. Section E now states the naming rule, and section D names the packages with their verified versions and licenses. A commercial identity server is still generalized, as are the vendor's commercial packages. The "switched-to-commercial" utility libraries remain described by category; naming them is a separate judgment left to the packaging and licensing reconcile, since they are packages this project forbids rather than depends on.
