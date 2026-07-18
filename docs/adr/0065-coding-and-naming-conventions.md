---
status: "accepted"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the Microsoft .NET Framework Design Guidelines (naming) and the C# coding-conventions and identifier-names guidance (verified 2026-07-18); the scattered naming decisions in ADR-0024, ADR-0027, ADR-0032, ADR-0044
informed: all contributors, via this repository
---

# Adopt the Microsoft naming and C# coding conventions as an enforced baseline, tailored to Nami

## Context and Problem Statement

Nami's naming and style decisions are real but scattered and partial. ADR-0024 fixes the `Nami.Identity.*` assemblies, namespaces, and the vertical-slice folder layout; ADR-0044 fixes the `I`-prefixed ports, the versioned wire-contract namespace, and the `NAMIxxxx` diagnostic-id format; ADR-0032 fixes the config-key shape. But no ADR adopts a coding standard, and the `.editorconfig` is a fifteen-line whitespace stub with no C# naming or style rules. There is no single answer to "how do we name and style C# here", and nothing makes the answer enforced rather than a matter of review-time opinion.

For an OSS project that wants outside contributions, an unstated, unenforced style is a tax: PRs bikeshed over casing and layout, the scattered project-specific rules are undiscoverable, and inconsistency accretes. This ADR adopts the Microsoft guidelines as the baseline by reference, makes them machine-enforced, and consolidates the Nami-specific tailoring into one place. It does not transcribe Microsoft's naming tables; those live upstream and in `.editorconfig`.

## Decision Drivers

* Consistency across contributors without per-PR debate.
* Enforcement over aspiration: rules a build checks, not rules a reviewer must remember.
* Reuse the industry standard rather than invent or transcribe one.
* One findable place for the project-specific conventions that a generic guide cannot cover.

## Considered Options

* No standard: rely on code review plus the scattered existing rules.
* Adopt the Microsoft guidelines by reference, enforce them with `.editorconfig` plus analyzers, and record the Nami-specific tailoring.
* Author a full bespoke rulebook in the ADR, transcribing the Microsoft tables plus custom rules.

## Decision Outcome

Chosen: "adopt by reference, enforce, and record the tailoring." Transcribing the Microsoft tables into an ADR is rejected (it would duplicate upstream and rot); no-standard is rejected (unenforced style does not hold).

### Baseline by reference (binding)

The Microsoft .NET Framework Design Guidelines for naming (capitalization, general naming, assemblies and DLLs, namespaces, types, members, parameters, resources) and the C# coding-conventions and identifier-names guidance are Nami's baseline. They govern anything this ADR and its `.editorconfig` do not explicitly override. They are adopted by reference, not copied.

### Enforcement is the mechanism (binding)

* **`.editorconfig` is the machine-checked rules-of-record** for casing, layout, and analyzable naming. The agreed core naming and style diagnostics are set to **error** severity so a violation fails the build, the same posture ADR-0044 already uses for the public-API analyzers.
* **CI enforces it**: `dotnet format --verify-no-changes` (or the equivalent analyzer gate) runs alongside the other build jobs (ADR-0060), and the .NET code-style and naming analyzers run in the normal build.
* **Public-API naming** stays governed by ADR-0044's `PublicApiAnalyzers`; **architectural naming and dependency rules** (namespace roots, no cross-slice references) stay governed by the ArchUnitNET suite (ADR-0024). This ADR adds the general code-style and naming layer beneath both.

### Nami-specific tailoring (binding; consolidated from where it was scattered)

* **Assemblies and namespaces**: rooted at `Nami.Identity.*` (the `Nami.Identity` meta-package plus `Core`, `Abstractions`, `Users`, `Bff`, `Admin.Api`, `Admin.App`, `Contracts`, `Admin.Contracts`); a namespace matches its folder and assembly; no `Common` god-namespace (ADR-0024/0027).
* **Ports and interfaces**: `I`-prefixed and living in `Nami.Identity.Abstractions`; extended only via `IXxxV2` or a default interface method, never a bare added member, and only where a port has a real reason to exist (ADR-0044/0024/0058).
* **Vertical-slice folders**: `Features/<Area>/<UseCase>/`, grouping request, handler, validator, and response; not technical folders such as `Services/`, `DTOs/`, `Validators/` (ADR-0024).
* **Configuration keys**: `Nami:Section:Key` in configuration, `Nami__Section__Key` as the environment form, and a short `NAMI_X` alias for common toggles; avoid mixed-case single-underscore keys (ADR-0032).
* **Wire contracts**: a versioned namespace (`...Contracts.V1`), enums serialized as strings, additive-only within a version (ADR-0044).
* **Diagnostic ids**: `NAMIxxxx` for `[Obsolete]` messages and any Nami analyzer (ADR-0044).
* **Telemetry**: meter and metric names are stable and treated as contract, under a `nami.`-rooted naming scheme (ADR-0022/0044).
* **Asynchronous methods**: the `Async` suffix, per the Microsoft guideline, enforced.
* **Private instance fields**: `_camelCase`, following the Microsoft C# convention; this is the deliberate house choice, recorded so it is not re-litigated.
* **Test naming**: behavior-first, Given/When/Then, per the testing strategy (ADR-0060).

### Where the rules live (index versus authority)

Machine-enforceable style and naming rules are authored in `.editorconfig`, which is what CI enforces; if this ADR's prose and `.editorconfig` ever disagree on an enforceable rule, `.editorconfig` is authoritative and the tailoring list here is reconciled to it. Conventions that a linter cannot check (config-key shape, folder layout, diagnostic-id format) are authored here and in their owning ADRs and are checked in review and, where possible, by the ArchUnitNET suite. This mirrors ADR-0061's index-versus-authority split.

### Confirmation

Build-time: the full C# `.editorconfig` ruleset and the analyzer package references land with the first code (M1), because they need a `.csproj` and analyzer packages to validate; until then this ADR sets the direction and the tailoring. Once code exists, `dotnet format` and the analyzers enforce it in CI, and the ArchUnitNET rules enforce the namespace and slice conventions.

### Consequences

* Good, because contributors get one recorded, enforced standard, so style stops being a review-time debate and the scattered project rules are finally discoverable in one place.
* Good, because it reuses the Microsoft standard by reference instead of duplicating it, so there is nothing to keep in sync with upstream.
* Good, because enforcement is real (analyzers plus `dotnet format` at error severity), not a document nobody runs.
* Bad, because the machine-enforced ruleset does not exist until M1, so the ADR is direction-setting until then; accepted, and mitigated by the build-time-confirmation posture used elsewhere (ADR-0024).
* Bad, because a strict error-severity style gate can be friction for contributors; mitigated by `dotnet format` auto-fixing most of it and by keeping only the agreed core at error severity.

## Pros and Cons of the Options

### No standard

* Good, because it needs no setup.
* Bad, because unenforced style does not hold, PRs bikeshed, and the scattered project rules stay undiscoverable.

### Adopt by reference, enforce, record tailoring (chosen)

* Good, because it is the industry baseline, machine-enforced, with the Nami-specific rules consolidated and nothing duplicated from upstream.
* Bad, because the ruleset lands at M1 and a strict gate adds some friction; both accepted and mitigated as above.

### Bespoke rulebook transcribed into the ADR

* Good, because everything would be in one document.
* Bad, because it duplicates Microsoft's guidance, rots as upstream evolves, and still needs `.editorconfig` to be enforced, so the prose adds maintenance without adding enforcement.

## More Information

* Related decisions: ADR-0024 (assemblies, namespaces, vertical-slice folders, and the ArchUnitNET rules), ADR-0027 (the package set the namespaces mirror), ADR-0032 (config-key shape), ADR-0044 (public-API analyzers, ports, wire-contract versioning, diagnostic ids, telemetry-name stability), ADR-0060 (the CI that runs the style gate and the test-naming convention), ADR-0058 (why a port needs a real reason), and ADR-0061 (the index-versus-authority pattern this ADR reuses).
* Baseline references (named factually, adopted by reference): the Microsoft .NET Framework Design Guidelines naming pages (capitalization conventions, general naming conventions, names of assemblies and DLLs, namespaces, types, members, parameters, and resources) and the C# program-structure, identifier-names, and coding-conventions pages.
* Build-time follow-up: author the C# `.editorconfig` ruleset and wire the `dotnet format` / analyzer gate at M1; keep the tailoring list here reconciled with it.
* Authored fresh for this repository.
