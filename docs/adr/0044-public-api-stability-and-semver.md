---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Microsoft.CodeAnalysis.PublicApiAnalyzers; Semantic Versioning 2.0; OpenIddict's own major-version migration precedent
informed: all contributors, via this repository
---

# Treat the public API as a versioned seam governed by an analyzer-gated SemVer and deprecation policy

## Context and Problem Statement

Nami ships as a set of NuGet packages, so consumers depend on its public API exactly as Nami depends on OpenIddict's. That makes the public surface a seam, and the same discipline applied to the OpenIddict seam (ADR-0021) has to be applied to Nami's own surface. A silently broken API loses community trust, and "be careful in review" does not prevent an accidental break from shipping. There was no recorded policy for how the API surface is locked, how versions are bumped, how deprecations are staged, how the consumer-implemented ports may evolve, how OpenIddict's own breaking changes are kept off the consumer's plate, or how the wire contracts version independently of the assemblies. (The productization document sketched this as "ADR-27 §D", but ADR-0027 is only about packaging/distribution, so the API-lifetime contract had no proper home.)

## Decision Drivers

* An accidental public-API break must be impossible to merge without it showing in review.
* Consumers must read Nami's SemVer, not OpenIddict's; an OpenIddict break must not surface as an unexplained Nami break.
* Ports that consumers implement are the most dangerous surface (adding a member breaks every implementer) and need stricter rules than ordinary classes.
* Deprecation must give consumers a migration window, never a same-version removal.
* Wire contracts (DTOs) evolve on a different clock than the assemblies and need their own versioning.

## Considered Options

* Best-effort/informal API stability enforced by code review
* An analyzer-gated formal policy: a locked surface, SemVer rules, staged deprecation, port-evolution rules, OpenIddict isolation, and independent wire-contract versioning

## Decision Outcome

Chosen option: "An analyzer-gated formal policy", so that every API change is deliberate, reviewable, and correctly versioned. The fixed parameters are:

* **A. Lock the surface with analyzers.** Every public package uses `Microsoft.CodeAnalysis.PublicApiAnalyzers` with `PublicAPI.Shipped.txt` (released, immutable within a major) and `PublicAPI.Unshipped.txt` (pending) per project, with `RS0016`/`RS0017` (and nullable-annotation `RS0037`) set to **ERROR**. A PR cannot change the surface without updating the API file, so every API delta appears in the diff. Nullable annotations live in the API file, so nullability is versioned too. On release, `Unshipped` is promoted to `Shipped` and is immutable until the next major.
* **B. SemVer rules.** Additive changes (new method/overload/optional-parameter, new type, new option with a default) are MINOR; bug/perf/doc fixes with no API change are PATCH; removals, renames, signature/return changes, observable behavior changes (for example a changed default token lifetime), adding a member to a consumer-implemented interface, and tightening nullability are MAJOR. Behavior change is breaking even when the API shape is unchanged, caught by the contract-regression suite (ADR-0021 discipline). All packages move in lock-step on one version, and the meta-package pins a compatible range.
* **C. Deprecation policy.** Removing an API takes two steps across at least one minor: mark `[Obsolete("use X; see <migration url>", DiagnosticId = "NAMIxxxx")]` at minor N (consumers get a warning plus a migration link and a stable diagnostic id they can suppress deliberately), then remove only at the next MAJOR. Never obsolete-and-remove in the same version.
* **D. Ports are stricter (Abstractions is the most dangerous API).** `Nami.Identity.Abstractions` holds the ports consumers implement (`ISigningKeyStore`, `ITenantStore`, `ICheckAccess`, `IEmailDispatcher`, ...). Adding a member to such an interface is breaking for implementers, so a shipped port is extended only by a default interface method, a new `IXxxV2 : IXxx`, or an optional capability interface, never by adding a bare member. A port change is weighed like a database-schema change.
* **E. Isolate consumers from OpenIddict's breaking changes.** Consumers see Nami's SemVer, not OpenIddict's: when OpenIddict ships a breaking change, Nami absorbs it behind its own surface and bumps its own MAJOR with a migration guide, so the consumer reads only Nami's guide. OpenIddict types are not leaked into Nami's public API where it can be avoided; where a re-export is deliberate, it is documented and treated as part of Nami's versioned surface. The meta-package pins the compatible OpenIddict range so consumers cannot drift it.
* **F. Wire contracts version independently.** The admin DTO assembly (`Nami.Identity.Admin.Contracts`) uses a `V1` namespace so the wire contract versions separately from assembly SemVer: an added optional field is non-breaking within `V1`, a breaking wire change is a parallel `V2`, and enums are serialized as strings for wire stability.
* **G. Telemetry names are part of the contract.** Custom OpenTelemetry meter/metric names are consumer-facing (renaming them breaks dashboards), so they are kept stable, changed only under the same rules, and carry a decommission marker if OpenIddict ships native telemetry (ADR-0022, ADR-0021).
* **H. Process and CI.** A PR that changes the public API must update `PublicAPI.Unshipped.txt` (analyzer-enforced); the pre-publish CI gate requires zero analyzer errors, contract-regression passing, no obsolete-and-remove in one version, and an updated changelog; each MAJOR ships an upgrade guide. `Microsoft.DotNet.ApiCompat` is available as an optional second-layer binary/source compat check.

### Consequences

* Good, because an accidental API break cannot merge silently: the analyzer fails the build and forces the delta into the review diff.
* Good, because consumers are shielded from OpenIddict's churn and read a single, predictable Nami SemVer with a migration guide per major.
* Good, because the consumer-implemented ports, the wire contracts, and even the metric names each get evolution rules matched to how they actually break.
* Bad, because maintaining the `Shipped`/`Unshipped` files, the deprecation windows, and the port-evolution discipline is real ongoing overhead on every change.
* Bad, because default-interface-method and `IXxxV2` port evolution is more awkward than simply adding a member, which is the price of not breaking implementers.
* Neutral, because behavior-change-as-breaking depends on the contract-regression suite (ADR-0021) actually covering the behavior in question.

### Confirmation

* CI fails a PR that alters the public surface without updating the API file (RS0016/RS0017 as errors).
* CI fails a release that obsoletes and removes an API in the same version, or that lacks a changelog entry or a major-version upgrade guide.
* A contract-regression test catches an observable behavior change even when the API shape is unchanged.
* An OpenIddict major bump is shown to surface as a Nami major with a migration guide, not as an unexplained break.

## Pros and Cons of the Options

### Best-effort/informal API stability enforced by review

* Good, because it needs no tooling or per-project API files.
* Bad, because human review misses surface changes, so breaks ship silently; there is no forcing function, no staged deprecation, and no isolation from OpenIddict's churn.

### Analyzer-gated formal policy (chosen)

* Good, because API changes are mechanically forced into review, correctly versioned, staged for deprecation, and isolated from the underlying dependency's breaks.
* Bad, because it is ongoing overhead and makes port evolution deliberately more awkward.

## More Information

* This policy is recorded from the productization design (doc 28 §4). The version *number* itself (one root git tag producing a lock-step version across all packages) is a packaging concern under ADR-0027, with the specific build-versioning tool a build-time detail; this ADR governs what may change and how it is versioned, not how the number is generated. The support/EOL window (how many majors are supported, and for how long) is tracked with the .NET upgrade cadence (ADR-0030) and remains open.
* Related decisions: ADR-0021 (the OpenIddict seam discipline this applies to Nami's own surface, and the contract-regression suite that catches behavior-breaks), ADR-0022 (the telemetry names treated as contract in G), ADR-0024 (the Abstractions/ports layering whose ports are the stricter surface in D), ADR-0027 (packaging/distribution, including the version-number tooling and the meta-package pin), ADR-0030 (the .NET/OpenIddict upgrade cadence that drives many majors), and ADR-0065 (the coding and naming conventions that consolidate this ADR's port, wire-contract, diagnostic-id, and telemetry-naming rules).
* Authored in this repository in 2026-07 to record the settled public-API stability policy as an ADR; neutral tools and standards (PublicApiAnalyzers, Semantic Versioning, `Microsoft.DotNet.ApiCompat`, OpenIddict as the isolated dependency) are named factually for identification only, and no commercial competitor is named.
