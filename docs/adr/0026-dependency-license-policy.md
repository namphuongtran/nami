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
* Forbidden: commercial/paid or dual-license with a paid tier (including "free up to a threshold, then paid"), for example a commercial identity server (already avoided) and the .NET utility libraries that moved to commercial licensing, namely **MediatR**, **AutoMapper**, and **MassTransit** (verify currency at adopt time, since a license can change again in either direction); viral copyleft (GPL, AGPL); and source-available-non-OSS licenses (BSL, SSPL, the Elastic License, "Commons Clause"), with BSL already avoided in ADR-0023.

**B. Derived design principles.**

* **Build the pattern in-house rather than swapping in another library.** For the capabilities the commercial libraries above provide (mediator, object mapping, message bus), Nami's default is a **small first-party implementation of the design pattern it actually needs**, scoped to that need, rather than importing a general-purpose third-party package and inheriting its licensing risk. The ADR-0024 vertical slice already needs no mediator at all: it uses plain handlers. If a mediator is ever genuinely required, Nami writes one.
* The exception is a package already confirmed permissive in section D and adopted deliberately: source-generated mapping via Mapperly (MIT) is preferred over hand-mapping where mapping volume justifies it. Adopting any *new* third-party package for one of these capabilities is a section C exception, not a default.
* Prefer built-in .NET and `Microsoft.Extensions.*` (MIT) before adding a third party.
* The policy applies to transitive dependencies too, not only direct ones.

**C. Enforcement (the core mechanism).**

* A CI license-scan gate reads the license of every package (direct and transitive) from the restore graph and fails the build if any license falls outside the allow-list; it runs on every PR and every dependency bump (matching the contract-regression cadence of ADR-0021).
* Central Package Management is the single place that declares versions, which makes scanning and pinning straightforward.
* Exception process: a case-by-case or otherwise special package needs Architect approval (plus Legal for anything copyleft or commercial), recorded in `docs/DEPENDENCY-LICENSES.md` (package, license, reason, approver, date); there are no silent exceptions.
* An SBOM (CycloneDX) is generated in CI for license and supply-chain audit.
* **A second limb, because the restore-graph scan is structurally blind to three things** (added 2026-08-01, after all three blind spots produced real defects; see More Information). The scan above is necessary and not sufficient, and its silence must never be read as coverage:
  1. **An external-tool inventory.** A tool executed as a separate process (a load-test binary, a conformance-suite container image) is not a package and is not in the restore graph, so the scanner cannot see it at all. Every such tool is listed in `docs/DEPENDENCY-LICENSES.md` with its licence, **where that licence was read**, and the date. The inventory is human-maintained in the same change that introduces the tool, and is confirmed complete before GA.
  2. **A package-name deny-list**, checked independently of any licence parsing: `FluentAssertions`, `NBomber`, `MediatR`, `AutoMapper`, `MassTransit`. A name check does not depend on the scanner correctly locating and interpreting a licence file, so it still fires when licence detection is wrong or absent. This list is the section A named-package list, mechanised.
  3. **The distributed bundle, not the root licence file.** A licence is verified by reading the licence text of the thing actually distributed, including companion modules. A repository's root `LICENSE` can be permissive while a module shipped beside it is not, and repository-metadata APIs read only the root file.
* **Conveying versus executing.** Running an unmodified external tool in CI against our own service is execution. Placing that tool inside a distribution artifact (the reference host image, the Helm chart, the NuGet meta-package, the `dotnet new` template) is **conveying**, and the artifact then inherits the tool's licence obligations in full. No tool may be bundled without a decision recorded as an ADR. This distinction is stated because the policy above bans licence *classes* without it, which made an execute-only case unanswerable by the policy's own terms.

**D. Current confirmed-permissive list (re-verify each at adopt time).** OpenIddict (Apache-2.0); the Npgsql / EF Core PostgreSQL provider (PostgreSQL/MIT); Finbuckle.MultiTenant (Apache-2.0); Duende.AccessTokenManagement and Duende.AccessTokenManagement.OpenIdConnect (both Apache-2.0 at 4.2.0), which are free packages published from the vendor's separate FOSS repository and are a different product from its paid identity server, together with their transitive Duende.IdentityModel (Apache-2.0 at 8.1.0), Microsoft.Extensions.Caching.Hybrid, Microsoft.Extensions.Http.Resilience, and Microsoft.Extensions.Telemetry.Abstractions (all MIT); thomasduft/openiddict-ui as an OpenIddict permissions/UI pattern (MIT); OpenTelemetry .NET (Apache-2.0); MailKit/MimeKit (MIT); Fluid/Scriban (MIT/BSD); Testcontainers (MIT); FusionCache (MIT); TngTech.ArchUnitNET (Apache-2.0), with the original NetArchTest.Rules unmaintained since 2021 and an MIT fork available as a drop-in; MinVer (Apache-2.0); Microsoft.SourceLink.GitHub (MIT); Playwright (Apache-2.0); and Quartz.NET (Apache-2.0). Each is re-verified at adopt time, because a license can change, which is the reason for the gate.

**E. Naming a dependency in documentation.** A dependency is recorded by its **real package identifier**, even when that identifier carries the name of a company whose commercial products this project deliberately does not name. Concealing the identifier would make the dependency record factually wrong, leave a contributor unable to reproduce the restore graph, and defeat the section C gate, which matches on exact package IDs. The rule is scoped narrowly: what stays generalized is only what the project's content policy requires, namely the **direct commercial competitor and its vendor**. That covers product comparison, parity framing, the vendor's internal source or type references, its issue tracker and blog posts, and its commercial packages that Nami rejects. It does **not** extend to unrelated commercial libraries: a forbidden package from another vendor is named plainly in section A, because a policy that will not say which package is forbidden cannot be followed. The local tooling that enforces this split is described in `scripts/README.md`.

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
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25. The 2026-07 import had over-generalized: it turned a settled token-management dependency into an open choice and invented a "not vendor-branded" constraint this project never decided. Section E now states the naming rule, and section D names the packages with their verified versions and licenses. A commercial identity server is still generalized, as are that vendor's commercial packages.
* **Section C gained a second limb on 2026-08-01, and each of its three parts exists because that blind spot had already produced a defect**, not because it was foreseen. (1) *External tools:* the load-test tool was an external binary, invisible to the restore-graph scan, and it was AGPL-3.0 in violation of section A for as long as it was named. (2) *Licences asserted in prose:* NBomber was recorded in this repository's own tables as Apache-2.0 and is in fact commercial ("not free for organizational use ... requires a valid Commercial Subscription", read in the `LICENSE` inside its 6.5.0 package), so no scanner and no reader had reason to catch it. The OIDF conformance suite was recorded as Apache-2.0 and is MIT, which broke nothing but was equally unsourced. (3) *Companion modules:* Gatling was considered as the replacement and a repository-metadata API reported it Apache-2.0, which is true of its root licence file; the standard report module one directory below carries a proprietary licence forbidding modification and reuse. All three were found by reading licence text at source, and none would have been found by the gate as section C originally defined it. The conveying-versus-executing paragraph was added in the same change, because the original policy banned licence classes with no way to answer a tool that is executed and never shipped, which left a real question formally unanswerable. See ADR-0078 for the tool decision that surfaced all of this, and `docs/DEPENDENCY-LICENSES.md` for the evidence with dates.
* The switched-to-commercial utility libraries are now named in section A (MediatR, AutoMapper, MassTransit), resolving a judgment briefly left open earlier the same day. Two things settled it: they are not the competitor or its vendor, so the content policy never reached them, and a forbidden-package list that refuses to name the packages cannot be acted on. Section E was narrowed in the same change, because as first written it lumped every rejected commercial package into the generalize bucket and so over-reached onto vendors the content policy does not cover.
* The stance on those capabilities is **build in-house, not substitute another library** (section B), confirmed by the maintainer on 2026-07-25: Nami does not take these packages, and where it needs the capability it implements the pattern itself, scoped to that need.
