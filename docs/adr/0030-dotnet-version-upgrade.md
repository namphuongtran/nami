---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; the .NET support policy and release cadence (verified 2026-07-04 at Microsoft's sources)
informed: all contributors, via this repository
---

# Upgrade .NET on an LTS-to-LTS cadence, with multi-target packages and per-bump contract-regression

## Context and Problem Statement

Nami pins .NET 10 (LTS), the runtime foundation of the entire stack (ASP.NET Core, EF Core 10, OpenIddict 7.5, Npgsql, ASP.NET Core Identity, Finbuckle). The open question is what happens when .NET 11, 12, and later ship, which is the same gap ADR-0021 patched for OpenIddict.

The .NET cadence (verified 2026-07-04) is a major release every November, where an even version is LTS (three years of support) and an odd version is STS (two years, raised from eighteen months since .NET 9). So .NET 10 is LTS (supported to roughly November 2028), .NET 11 is STS (roughly November 2026 to November 2028), and .NET 12 is LTS (roughly November 2027 to November 2030). The key consequence is that 10 to 12, skipping 11, is seamless, because .NET 10 is supported until November 2028 and .NET 12 ships in November 2027, leaving no EOL gap.

This service adds four constraints: an identity/security-critical service must not run an EOL runtime (a hard invariant, not a preference); it publishes for users to self-host (OSS, ADR-0027), so consumers run their own runtime and the library must not lock them to one; a .NET major bump usually drags OpenIddict, EF, Npgsql, and Finbuckle along, so it must share one playbook with ADR-0021; and it shares the failure mode of an OpenIddict bump, where an uncontrolled bump breaks silently in production while pinning forever accrues security debt and forgoes features already used (native passkeys and UUIDv7 in .NET 10).

## Decision Drivers

* Never run an EOL runtime on a security-critical service (a hard invariant).
* Do not lock self-hosting consumers to a single runtime.
* Co-upgrade with OpenIddict, EF, and the rest under one playbook (the ADR-0021 sibling).
* A bump must be bounded and tested, never a silent production break or a mass rewrite.

## Considered Options

* Latest-always (upgrade every major at GA: 10, 11, 12)
* Pin .NET 10 forever
* LTS-anchored, with multi-target packages and per-bump contract-regression

## Decision Outcome

Chosen option: "LTS-anchored, with multi-target packages and per-bump contract-regression", in six mechanisms that parallel ADR-0021:

* **A. Host/deployable = LTS-to-LTS.** The reference host and Admin (ADR-0025) run LTS to LTS (.NET 10 to 12), skipping the 11 STS, which is only built and tested in the early-warning branch (E) and never shipped to production; a seamless 10-to-12 path with no EOL gap removes any incentive to take STS risk on a security-critical service. If an 11 STS ever carried a mandatory, non-back-portable feature (unlikely), a mini-ADR would evaluate it case by case rather than defaulting to the STS jump.
* **B. Published NuGet packages = multi-target current-LTS plus next-LTS.** The library packages multi-target (currently `net10.0`, adding `net12.0` when .NET 12 ships), so consumers on a newer runtime (including .NET 11 STS users) are not blocked, while keeping the previous LTS for one beat so consumers have time to upgrade. The host is single-target on the current LTS. A target framework is dropped only when its LTS reaches EOL (for example dropping `net10.0` after November 2028), recorded via SemVer.
* **C. The target framework is one knob (build-time).** `Directory.Build.props` centralizes `<TargetFramework>`/`<TargetFrameworks>` and `<LangVersion>`, so changing the runtime touches one place rather than every csproj; the SDK/runtime image pin in the Dockerfile (ADR-0025) and `global.json` (`rollForward`) stay in sync; and Central Package Management (ADR-0026) keeps every `Microsoft.*`, EF, and Npgsql package aligned with the runtime major, bumped lock-step.
* **D. Contract-regression plus the full suite on every SDK bump, reusing the ADR-0021 infrastructure.** Each .NET bump runs the same contract-regression suite (seams S1 through S34), the full unit/integration suite (Testcontainers on PostgreSQL 18, ADR-0025), and OIDC conformance; a bump that breaks a contract or conformance fails the build. API discipline: no preview/RC APIs, and runtime-version-dependent APIs are isolated behind an abstraction (ports, ADR-0024) so multi-targeting does not sprawl into `#if` directives (using `#if NET12_0_OR_GREATER` minimally, preferring a polyfill or adapter). Forward-only features: capabilities already used from the current runtime (native passkeys/WebAuthn in .NET 10 per ADR-0028, `Guid.CreateVersion7()` from .NET 9, and native metrics) only advance and never drop below that minimum.
* **E. CI early-warning, mirroring the OpenIddict 8.0-preview spike.** A non-gating branch builds and tests on the next preview/RC and on STS (for example when a .NET 11 preview lands), reporting API and behavior breaks early and feeding the playbook in F, so speculation is replaced by a real migration list that de-risks the next LTS bump.
* **F. Support-window watch, co-upgrade, and a migration playbook.** A quarterly roadmap-watch re-verifies the current LTS EOL date and the next LTS GA date at Microsoft's source ("verifications have a shelf-life"), never letting a runtime reach EOL before the upgrade begins (invariant #1). Each LTS bump reads the .NET, ASP.NET Core, and EF Core breaking changes; bumps the target framework and the Central Package Management versions lock-step (OpenIddict, EF, Npgsql, Identity, Finbuckle) in the same beat as ADR-0021; runs the suite plus conformance; runs on staging first; and swaps to native where the bump unlocks it (coordinated with ADR-0021). Deployment is dual-control.

### Consequences

* Good, because a runtime bump becomes a bounded, tested, EOL-seamless event rather than a mass rewrite or a security gap; OSS consumers are not locked to one runtime (multi-target); and a shared playbook with OpenIddict (ADR-0021) reduces duplication.
* Good, because skipping STS means fewer upgrade beats, which fits a stability-first security service.
* Bad, because multi-targeting enlarges the build/test matrix (mitigated by a CI matrix and by adding `net12.0` only when an LTS ships, never for STS), and the roadmap-watch and early-warning branch must be maintained (a small cost against a production break or an EOL runtime).
* Bad, because skipping STS delays STS-only features by about a year (accepted, with a mini-ADR exception if one is ever mandatory).

### Confirmation

* The .NET support policy and cadence (LTS three years, STS two years, November releases, even is LTS), verified 2026-07-04: .NET 10 LTS to November 2028, .NET 11 STS November 2026 to November 2028, .NET 12 LTS from November 2027, with STS raised to two years since .NET 9.
* This applies the ADR-0021 framework (pin plus contract-regression plus playbook) to the runtime layer; the two ADRs form one external-version-adaptation family, and the seam catalogue's adjacent-stack tier already covers EF, Npgsql, and Finbuckle.
* Versioning uses MinVer (Apache-2.0; a git tag yields one version across the whole graph, fitting lock-step) with a reproducible stack (deterministic and CI builds, SourceLink, and symbol packages).

## Pros and Cons of the Options

### Latest-always (every major at GA)

* Good, because it gets new features earliest.
* Bad, because it means an annual upgrade and accepting STS (shorter support, faster cadence) for a security service, a high risk and cost.

### Pin .NET 10 forever

* Good, because it is maximally stable in the short term.
* Bad, because it violates the no-EOL-runtime invariant once .NET 10 reaches EOL in November 2028, losing security patches.

### LTS-anchored with multi-target packages (chosen)

* Good, because the runtime stays supported with a seamless EOL handoff, consumers are not locked to one runtime, and the bump is bounded and tested under a shared playbook.
* Bad, because it costs a larger build matrix and the discipline of a roadmap-watch and an early-warning branch.

## More Information

* Original decision 2026-07-04: the host is LTS-anchored (10 to 12, skipping 11 STS); published packages multi-target the current and next LTS; enforcement is the target-framework knob plus per-bump contract-regression/conformance plus the early-warning branch plus a quarterly roadmap-watch; and the versioning tool is MinVer.
* Build-time follow-ups: create `Directory.Build.props` (the target-framework knob), `global.json` roll-forward, and the CI matrix (a single-LTS host and multi-target packages) plus the early-warning job on the next preview, folded into the shared contract-regression suite that runs on every .NET and OpenIddict bump; and the quarterly Ops roadmap-watch of EOL/GA dates.
* Related decisions: ADR-0018 (the version-sensitive Finbuckle-times-EF composition, co-upgraded), ADR-0021 (the sibling OpenIddict version-adaptation that shares the playbook and suite), ADR-0022 (OpenTelemetry), ADR-0024 (ports isolating runtime-version-dependent APIs), ADR-0025 (the Dockerfile SDK/runtime pin), ADR-0026 (lock-step CPM pinning), ADR-0027 (multi-target published packages), and ADR-0028 (native passkeys as a forward-only .NET 10 feature).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. There are no competitor references; .NET, the Microsoft stack, MinVer, and the tooling are retained as neutral technical references.
