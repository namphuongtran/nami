---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the Microsoft .NET Framework Design Guidelines (naming) and the C# coding-conventions and identifier-names guidance (verified 2026-07-18); the scattered naming decisions in ADR-0024, ADR-0027, ADR-0032, ADR-0044
informed: all contributors, via this repository
---

# Adopt the Microsoft naming and C# coding conventions as an enforced baseline, tailored to Nami

## Context and Problem Statement

Nami's naming and style decisions are real but scattered and partial. ADR-0024 fixes the `Nami.Identity.*` assemblies, namespaces, and the vertical-slice folder layout; ADR-0044 fixes the `I`-prefixed ports, the versioned wire-contract namespace, and the `NAMIxxxx` diagnostic-id format; ADR-0032 fixes the config-key shape. But no ADR adopts a coding standard, and the `.editorconfig` **was**, when this was decided, a fifteen-line whitespace stub with no C# naming or style rules (the C# section landed 2026-08-02; the tense is fixed rather than the sentence, because the problem this ADR was written against is what the paragraph is describing). There is no single answer to "how do we name and style C# here", and nothing makes the answer enforced rather than a matter of review-time opinion.

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

* **`.editorconfig` is the machine-checked rules-of-record** for casing, layout, and analyzable naming. The agreed core naming and style diagnostics are set to **error** severity, the same posture ADR-0044 already uses for the public-API analyzers, and the agreed core is exactly two diagnostics: `IDE1006` for naming and `IDE0055` for layout, both auto-fixable by `dotnet format`.

  **Error severity alone does not fail a build, and the ruleset was authored around that** (clause added 2026-08-02, when the ruleset landed and the sentence above turned out to promise something the file cannot deliver by itself). Measured against .NET SDK 10.0.301: an `.editorconfig` carrying both diagnostics at `error`, against a file violating both, produced `Build succeeded` and exit 0. Failing the build additionally requires **`EnforceCodeStyleInBuild`**, which is an MSBuild property rather than an editorconfig key, so it lives in `Directory.Build.props` and the two files are one mechanism rather than two. Two further measurements shaped the file and are recorded here because each is a way for it to read as enforced while being inert: a per-rule `dotnet_naming_rule.<name>.severity = error` **never reaches the build** and only `dotnet_diagnostic.IDE1006.severity` does, which is why the 389-line ruleset that `dotnet new editorconfig` writes (all 19 naming rules at `suggestion`, no `IDE1006` line) was not vendored; and severity **cannot be tiered inside `IDE1006`**, since a rule declared `suggestion` still fails the build under an error `IDE1006`, so every naming rule in the file reads `error` as a description rather than as a separate choice. `scripts/test-editorconfig.sh` asserts all of this on every CI run, because until the first project lands at M1 nothing else in the repository exercises the ruleset.
* **CI enforces it**: `dotnet format --verify-no-changes` (or the equivalent analyzer gate) runs alongside the other build jobs (ADR-0060), and the .NET code-style and naming analyzers run in the normal build.
* **Public-API naming** stays governed by ADR-0044's `PublicApiAnalyzers`; **architectural naming and dependency rules** (namespace roots, no cross-slice references) stay governed by the ArchUnitNET suite (ADR-0024). This ADR adds the general code-style and naming layer beneath both.

### Nami-specific tailoring (binding; consolidated from where it was scattered)

* **Assemblies and namespaces**: rooted at `Nami.Identity.*`; a namespace matches its folder and assembly; no `Common` god-namespace (ADR-0024/0027). The ratified set splits into **libraries**, which are published to NuGet, and **applications**, which are not:

  | Libraries (NuGet) | Purpose |
  | --- | --- |
  | `Nami.Identity` | Meta-package: the default stack in one reference, the consumer entry point |
  | `Nami.Identity.Abstractions` | The ports, the dependency-inversion centre; depends on nothing |
  | `Nami.Identity.Core` | Protocol-server wiring, claims, consent, tokens; the `AddNamiIdentity()` builder |
  | `Nami.Identity.Users` | ASP.NET Core Identity, passkeys, MFA, user lifecycle (ADR-0028) |
  | `Nami.Identity.EntityFrameworkCore` | Persistence, provider-neutral |
  | `Nami.Identity.EntityFrameworkCore.PostgreSQL` | The PostgreSQL provider (ADR-0037) |
  | `Nami.Identity.MultiTenant` | Tenant resolution and per-tier store routing (ADR-0001) |
  | `Nami.Identity.Keys` | Key store and rotation (ADR-0011/0012) |
  | `Nami.Identity.Keys.Azure`, `.Keys.Aws`, `.Keys.Gcp`, `.Keys.Vault` | Optional cloud key and secret adapters (ADR-0006/0009) |
  | `Nami.Identity.OpenTelemetry` | Telemetry wiring (ADR-0022) |
  | `Nami.Identity.Validation` | The resource-server validation edge, embedded in the **consumer's** API process (ADR-0049) |
  | `Nami.Identity.Bff` | Backend-for-frontend core (ADR-0029) |
  | `Nami.Identity.Bff.Yarp` | The remote proxy, shipped as its own package |
  | `Nami.Identity.Contracts` | DTOs shared with the core IdP; zero dependencies |
  | `Nami.Identity.Admin.Contracts` | Admin DTOs and problem codes; referenced only by the admin projects |

  | Applications (not on NuGet) | Purpose |
  | --- | --- |
  | `Nami.Identity.Host` | The runnable reference identity host (ADR-0027) |
  | `Nami.Identity.Admin.Api` | The admin REST API (ADR-0020) |
  | `Nami.Identity.Admin.App` | The admin MVC Razor front end (ADR-0020) |

  Three naming rules follow from that split and are binding. **An application sets `IsPackable=false`**: it is distributed as a container image and, for the host, a `dotnet new` template, never as a package a consumer references, so `Nami.Identity` stays unambiguously "the thing you add" and `Nami.Identity.Host` unambiguously "the thing that runs". **The cloud adapters are named after the port they adapt, not after one vendor's product**: `.Keys.Azure`, `.Keys.Aws`, `.Keys.Gcp`, `.Keys.Vault`, because only one of those providers has a product called Key Vault and naming the family after it would be wrong for the other three and redundant for the fourth. And **`Nami.Identity.Validation` is a consumer-side library**, not one of Nami's hosts, so it is packaged and versioned for external consumption even though nothing in this repository runs it.
* **Ports and interfaces**: `I`-prefixed and living in `Nami.Identity.Abstractions`; extended only via `IXxxV2` or a default interface method, never a bare added member, and only where a port has a real reason to exist (ADR-0044/0024/0058).
* **Vertical-slice folders**: `Features/<Area>/<UseCase>/`, grouping request, handler, validator, and response; not technical folders such as `Services/`, `DTOs/`, `Validators/` (ADR-0024).
* **Configuration keys**: `Nami:Section:Key` in configuration, `Nami__Section__Key` as the environment form, and a short `NAMI_X` alias for common toggles; avoid mixed-case single-underscore keys (ADR-0032). **These keys are also a stable public contract under ADR-0044 parameter I** (clause added 2026-08-01), so a rename is a MAJOR and a removal takes the same two-step window an API removal takes, an operator's configuration file being as downstream of Nami as a compiler is. The clause is recorded because its absence made this the one value-shaped entry in this list that said what a key looks like and not what changing it costs, while the protocol-URN bullet below carried exactly that sentence.
* **Wire contracts**: a versioned namespace (`...Contracts.V1`), enums serialized as strings, additive-only within a version (ADR-0044).
* **Diagnostic ids**: `NAMIxxxx` for `[Obsolete]` messages and any Nami analyzer (ADR-0044).
* **Telemetry**: meter and metric names are stable and treated as contract, under a `nami.`-rooted naming scheme (ADR-0022/0044).
* **Protocol URNs and claim values** use the **lowercase product form `nami.identity`**, not the organization name: `urn:nami.identity:<value>`, for example the assurance levels `urn:nami.identity:aal1|aal2|aal3` (ADR-0013). These strings are on the wire, so they are a stable public contract under ADR-0044 and cannot be changed without a version bump. Recorded here because substituting the organization form (`urn:nami:...`) is a mistake this project has already made once and propagated across four documents; the three name forms are distinct and not interchangeable: `Nami.Identity.*` for assemblies and namespaces, `Nami:...` for configuration keys, and `nami.identity` for URNs and other lowercase wire identifiers.
* **Authorization capability identifiers** use **lowercase snake_case**: `manage_users`, `view_audit`, `delete_tenant`, `re_delegate` (ADR-0010). Like the URNs above, these are values rather than prose: they are stored in the `CapabilityCatalog` table, they are the argument of the capability attribute, and they form the policy name (`Capability:manage_users`), so a casing change is a data and policy-name change rather than an editorial one. Recorded here for the same reason as the URN rule: the kebab-case form had already appeared in one ADR while the design and the rest of the corpus used snake_case, and nothing in this ADR forbade the drift.
* **Dual-control proposal action types are a different namespace and use kebab-case**: `delete-application`, `delete-tenant`, `suspend-tenant`, `revoke-all-tokens`, `audit-export`. They are `ActionType` values on the proposal resource and appear in the published OpenAPI schema, so they are a wire contract under ADR-0044 (ADR-0020). The overlap is deliberate and must not be "corrected": the capability `delete_tenant` (who may) and the action type `delete-tenant` (what is being proposed) are distinct values in distinct namespaces, and normalizing either one to match the other would break a policy name or an API contract.
* **Database identifiers**: tables and columns are **PascalCase**, which is what EF Core, OpenIddict, and ASP.NET Core Identity already generate (`AspNetUsers`, `OpenIddictApplications`, `DataProtectionKeys`), so the model needs no renaming layer. PostgreSQL folds an unquoted identifier to lower case, so EF emits them quoted and **every hand-written statement must quote them too** (`"TenantId"`): the row-level-security policies, the outbox drain, DBA sessions, and dashboard queries. Indexes are `IX_<Table>_<detail>`. Two categories are deliberately **not** PascalCase. Objects EF never maps, meaning row-level-security policies and database roles, are snake_case (`tenant_isolation`, `nami_identity_app`). And an identifier that is a wire or specification name is kept verbatim in the case the specification uses (`client_id`, the CloudEvents attributes `tenantid` / `type` / `time`, and `Properties` dictionary keys). Applying `UseSnakeCaseNamingConvention()` across the model was considered and **rejected**: it overrides the framework defaults and renames every framework-owned table for no functional gain. Recorded here for the same reason as the two rules above, and for one more: ADR-0003 already corrects column names to "the PascalCase identifier convention that the data design owns", but the data design did not carry the rule, so the attribution pointed at no owner while a mixed `Roles_JSON` form spread through five documents across two layers.
* **Asynchronous methods**: the `Async` suffix, per the Microsoft guideline, enforced. **Enforcement has a hard edge and it is half the rule** (recorded 2026-08-02): the naming symbol matches on the `async` **modifier**, so `public async Task Poll()` is caught and `public Task Poll()` returning a task without the modifier is not. A naming symbol has no return-type filter, so the uncovered half is a review matter rather than a gap `.editorconfig` can close, and saying "enforced" without this made the coverage sound total.
* **Private instance fields**: `_camelCase`, following the Microsoft C# convention; this is the deliberate house choice, recorded so it is not re-litigated. **The word "instance" costs two more rules** (recorded 2026-08-02): a private constant and a private static are both private fields, so the rule reaches them unless each is carved out first. Measured by deleting each carve-out in turn, a private constant is then required to be `s_maxRetries` (the symbol matcher counts a constant as static) and a private static to be `_counter`. Both carve-outs take the Microsoft baseline this ADR adopts by reference, `PascalCase` for a private constant and `s_camelCase` for a private static, read in the SDK's own `dotnet new editorconfig` output. Their **position** in the file is not what does this: reordering was tested and changed nothing, because the more specific symbol specification wins regardless.
* **Test naming**: behavior-first, Given/When/Then, per the testing strategy (ADR-0060).

### Where the rules live (index versus authority)

Machine-enforceable style and naming rules are authored in `.editorconfig`, which is what CI enforces; if this ADR's prose and `.editorconfig` ever disagree on an enforceable rule, `.editorconfig` is authoritative and the tailoring list here is reconciled to it. Conventions that a linter cannot check (config-key shape, folder layout, diagnostic-id format) are authored here and in their owning ADRs and are checked in review and, where possible, by the ArchUnitNET suite. This mirrors ADR-0061's index-versus-authority split.

### Confirmation

**The C# `.editorconfig` ruleset landed 2026-08-02, ahead of M1**, together with `Directory.Build.props` and `scripts/test-editorconfig.sh`, which is a CI gate. This paragraph previously said the ruleset waits for the first code "because they need a `.csproj` and analyzer packages to validate", and that reason bundled two things of which only the second still holds. Analyzer packages belong to ADR-0044 parameter A (`RS0016`/`RS0017`/`RS0037`) and are genuinely M1 work, because they need real public surface and per-project API files. The naming and style layer this ADR owns needs **one** `.csproj` and the SDK, and the self-test supplies both on every run: it builds a throwaway project against the real `.editorconfig` and `Directory.Build.props`, asserts a compliant fixture is clean, and asserts a violating fixture fails on all four naming rules and on formatting.

What remains at M1 is therefore two items rather than the whole ruleset. **Point the same rules at real projects**, since a fixture proves the rules fire and says nothing about how much existing code they would reject, there being none. And **point the gate at the solution**: `dotnet format --verify-no-changes` alongside the build jobs, plus the ArchUnitNET rules for the namespace and slice conventions, which no fixture can stand in for. The `dotnet format` **mechanism** is no longer part of that item, because the self-test asserts it directly as of 2026-08-02; what is left is the wiring, which needs projects.

**Both enforcement paths are asserted, and keeping both is a decision rather than belt-and-braces.** They are not one gate under two names: `dotnet format --verify-no-changes` needs no `EnforceCodeStyleInBuild`, reports whitespace as `WHITESPACE` rather than `IDE0055`, and exits 2 rather than 1. The consequence was measured by breaking each half in turn. Removing `EnforceCodeStyleInBuild`, or removing `dotnet_diagnostic.IDE1006.severity`, silences `dotnet build` completely while the format path keeps reporting all four naming violations. So a CI gate built on the format path alone stays green through either break, and the cost lands on contributors, whose local build goes quiet while CI does not. **Do not retire the property as redundant when the format gate lands**, which is the reading the two paths invite once both exist.

### Consequences

* Good, because contributors get one recorded, enforced standard, so style stops being a review-time debate and the scattered project rules are finally discoverable in one place.
* Good, because it reuses the Microsoft standard by reference instead of duplicating it, so there is nothing to keep in sync with upstream.
* Good, because enforcement is real (analyzers plus `dotnet format` at error severity), not a document nobody runs.
* ~~Bad, because the machine-enforced ruleset does not exist until M1, so the ADR is direction-setting until then.~~ **Closed 2026-08-02**: the ruleset exists and a CI gate proves it fires. What is left is narrower and is stated in the Confirmation, and it comes with a new cost worth naming, because the mitigation is not free either. The ruleset is proven against a fixture written to exercise it, so it is known to work and **not** known to be liveable: nothing yet says how much real code it would reject, and the risk that moves to M1 is the pressure to weaken a rule rather than fix the code that trips it. That pressure is what the root `CLAUDE.md` rule against editing a document to silence a checker exists for, applied to a ruleset.
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
* ~~Build-time follow-up: author the C# `.editorconfig` ruleset and wire the `dotnet format` / analyzer gate at M1.~~ **The ruleset was authored 2026-08-02**; the remaining M1 half is in the Confirmation. Keeping the tailoring list here reconciled with the file is still standing, and the direction of that reconcile is fixed by the index-versus-authority section above: `.editorconfig` wins and this list is the bug.
* **The ruleset writes only what deviates, and that is a rule about the file rather than a preference** (2026-08-02). `IDE1006` ships default rules and enforces the `I` prefix on interfaces and PascalCase on types and members with **zero** `dotnet_naming_*` entries present, measured. Restating those defaults would contradict this ADR's own "adopted by reference, not copied" and would rot against the SDK. So an absent rule in that file means the baseline already covers it, and adding one is a claim that it does not.
* **`Directory.Build.props` now exists**, carrying one property, which reaches into a follow-up ADR-0030 owns: that ADR makes the file "the target-framework knob" and lists creating it among its own build-time items. The knob is **not** here. `TargetFramework`, `TargetFrameworks`, and `LangVersion` stay ADR-0030's to set when the projects they configure exist, and so do MinVer and the reproducible-build properties, which that ADR records at its parameter list rather than this one. What landed early is the one property without which this ADR's error severities do nothing.
* **The database-identifier rule was added on 2026-07-26**, during the detailed-design reconcile, and is adopted from the design corpus, whose data model states the same convention and records the same rejected alternative. Two things made it a decision rather than an editorial note: nothing in this ADR covered database identifiers at all, and ADR-0003 was already citing the convention as owned by the data design, which did not state it. The drift it addresses was enumerated, not estimated, and the first enumeration was itself too low: the mixed `Roles_JSON` form appeared in four detailed designs (data, audit, authorization, erasure) **and** in the architecture layer's data view, which is five documents across two layers.
* Authored fresh for this repository, apart from the database-identifier rule noted above.
