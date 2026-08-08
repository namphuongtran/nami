# CLAUDE.md for `src/`

The root [`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule, the content rules, and the
naming and style rules under ADR-0065. All applies, none is repeated. When this folder gets a
`README.md` it becomes the authority, and this file keeps only the traps.

## The evidence rule bites hardest here, because a signature is a claim

A document can leave a detail out and still read as complete. **C# cannot.** Writing a type forces
a decision on every member, every return type, and every nullable annotation. So a design that
omits one leaves a gap the compiler makes you fill, and filling it from judgement then shipping it
unmarked is how an invented decision enters the codebase wearing the design's authority.

This changed what the first project contains. `ISecretResolver` was the plan, on the strength of
[`../docs/design/09-federation-and-claims-profile.md:81-83`](../docs/design/09-federation-and-claims-profile.md),
which gives it as `GetSecretAsync(string reference, CancellationToken) string`. **That cannot be
written**: a method named `…Async` returning a bare `string` is not a C# signature. The omission is
an omission rather than a convention to assume, because the same layer writes
`ValueTask~AuditChainEntry~` and `ValueTask` explicitly at
[`../docs/design/03-audit.md:61,65`](../docs/design/03-audit.md) when it means them.
**`ScopeDefinition` landed instead**, because
[`23:89-93`](../docs/design/23-configuration-and-client-declaration.md) gives all three of its
members **and** their nullability, that diagram annotating `string?` on other members in the same
block. So **before writing a type, check that the source fixes every member you are about to
write.** It is not enough that the type is named somewhere. Where the source does not, the port or
DTO is not ready to land, and saying so is the deliverable.

## Choices with no source, made anyway

Some cannot be avoided once a file exists. The rule is that each is recorded as a choice, with its
reason and its open verification, and never presented as sourced.

- **`required` on the non-nullable members of `ScopeDefinition`.** The design marks them
  non-nullable and says a missing required value must fail at start-up (`23:454`), but it enforces
  that with `.ValidateDataAnnotations()` at `23:357`, not with the C# modifier. `required` states
  exactly "must be supplied", and produces no CS8618 without inventing a default value the way
  `= string.Empty` would. **Not verified**: that the options binder at `23:356` populates
  `required` members. That needs the configuration packages.
- **`init` rather than `set` on the three audit DTOs** (2026-08-05). Nothing binds `AuditEvent`,
  `SecurityEvent`, or `AuditChainEntry`, and design 03 section 3 states no accessor either way.
  `init` makes an event immutable once the sink has hashed it, the property the chain exists to
  give. The open verification above does not widen to these three.
- **`sealed class` rather than `record` for those three.** No source states a shape. A record
  synthesizes `ToString()` over every property, and `ActorSubCiphertext` and `SourceIpHash` are
  the values design 03 keeps off every other lane, so that `ToString()` would be a leak wearing a
  convenience. It also adds roughly ten synthesized lines per type to `PublicAPI.Unshipped.txt`,
  and ADR-0044 then owes compatibility on each.
- **No `= default` on the `CancellationToken` of either sink.** A default value is public API under
  ADR-0044 and removing one later is breaking. An explicit token also forces the caller on the
  fail-closed critical path to supply one, the path design 03 section 5.3 describes.
- **The folder taxonomy is flat**, and no source states one. Ten ports flat in one folder will be
  wrong; inventing the grouping from one type would be worse. Settle it when the catalogue lands.
  Re-counted 2026-08-08 after `Nami.Identity.Core` landed: **thirteen** public types across the two
  projects, being nine in `Abstractions` (five sealed classes, two interfaces, two enums) and four
  in `Core` (one sealed class, one static class, one interface, one enum). The `Abstractions` count
  was six on 2026-08-05.

**Four choices landed 2026-08-08 with `Nami.Identity.Core`**, and unlike the definition model these
are not transcriptions: ADR-0096 decided the shape, and what follows is what building it still
forced.

- **`FrameworkReference` to `Microsoft.AspNetCore.App` rather than four `PackageReference` items.**
  `AddNamiIdentity` needs `IServiceCollection`, `IValidateOptions<T>`, `ValidateOnStart()` and
  `BindConfiguration()`, and in a plain `Microsoft.NET.Sdk` library every one of those is a separate
  package. Taking them from the shared framework costs zero `PackageVersion` rows and zero licence
  reads, and design 01's library table already carries ".NET and ASP.NET Core, MIT, ADR-0030". The
  engine does the same: `OpenIddict.Server.AspNetCore` 7.5.0's nuspec declares a frameworkReferences
  group for net8.0, net9.0 and net10.0, read 2026-08-08. **No source states either choice.**
- **The namespace is `Nami.Identity.Core` and not `Microsoft.Extensions.DependencyInjection`.** The
  usual .NET answer puts an `IServiceCollection` extension in the latter so it needs no `using`.
  ADR-0065 requires that "a namespace matches its folder and assembly", so this is a **departure
  from the framework convention that a decision here overrides**, not a gap.
- **The validator reports both missing values rather than the first.** No source asks for either
  behaviour. An operator missing two values should learn both at the first boot.
- **The configuration section bound is `Nami:Protocol`, and the constant is private.** Design 04
  section 6 owns those key names and says so. A public constant here would be a second public
  spelling of a contract that design already owns, and ADR-0044 parameter I versions both.

**What is NOT recorded as a choice, because it is measured:** the twelve property types, their
nullability, their accessors, the absent `required`, the enum ordinals, and the two initializers
that must stay absent. ADR-0096 fixes all of them, and each default has a unit fact.

**Five more choices landed 2026-08-08 with `ClientDefinition`, `ClientFlow`, and
`ClientAuthMethod`**, all transcribed from the class diagram in design 23 section 3, which fixes
every member and its nullability. What the diagram does not fix:

- **`set` rather than `init` on all seventeen members.** This follows `ScopeDefinition` and not the
  audit DTOs, for the reason above: a configuration binder writes this type, and `23:356` binds
  `List<ClientDefinition>` from `Nami:Clients` exactly as it binds the scope list. **Also a
  departure**: the external corpus writes `init` on every member at `13-configuration-dx.md:76-95`.
- **`required` on `ClientId` and `DisplayName`, and `= []` on the four `string[]` members.** All
  six are non-nullable with no stated default, so the answers had to be chosen apart. The criterion
  is whether the member has a meaningful empty value. The two scalars do not; the arrays do, since
  empty grants nothing, the deny-by-default value. **Half of the array reason is sourced and half
  inferred, and an earlier draft bundled both behind one pointer.** `23` section 5.2 gives
  `ClientCredentials` the token endpoint only and **no response type**, and its table has no
  redirect-URI column at all. The step to `= []` is the inference that a flow never reaching a
  browser cannot use one, so `required` would reject a client the design calls legal.
  **`ScopeDefinition.Resources` answers the same question the other way**, as `required string[]`,
  and both come from diagrams in one document. Read that as an open consistency question.
- **`Flow` has no stated default anywhere, and carries `= ClientFlow.Code` anyway.** Design 23
  states a default for six members and none for this one. The initializer writes down what C#
  would do regardless, so it changes no behaviour and makes no claim. Recorded because the line
  must not later be read as evidence that a default was decided.
- **Both enums carry explicit ordinals, and each stated default is an initializer.** This reverses
  what the increment first landed. `ClientAuthMethod.PrivateKeyJwt` sat at ordinal 0 **because**
  design 23 makes it the default, "so the secure choice is the one you get by omission", and
  nothing else expressed that. A reorder would have moved every undeclared client onto the weaker
  credential while the API diff showed only `= 0` becoming `= 1`, which does not read as a security
  change. Both fixes were free while `PublicAPI.Shipped.txt` holds no entries. **The binder is the
  second reason for the ordinals**: it accepts a numeric string for an enum member, so a settings
  file can carry `"Flow": 3`, and an unwritten ordinal would let a reorder repoint it silently.
- **One member design 23 names was deliberately not written.** `23:153` lists
  `BackchannelLogoutUri` in its "Definition field" column while its own class diagram at `23:70-88`
  does not declare it. Three sources put it on the Application write path instead:
  `design/15-admin-api.md:133` and `:141` carry it on `ApplicationDto` and `ApplicationPolicyDto`,
  and `adr/0019-single-logout-strategy.md:49` calls it "a new field on the Application". The corpus
  agrees at `25-design-admin-api.md:305`. So the type has seventeen members, and the contradiction
  is raised against design 23 rather than resolved here.

## Where a source exists and this repository does not simply copy it

The section above is for a gap. This one is for two other shapes, recorded apart because a reader
has to tell them apart. A **departure** is where a source says something and a second source
overrules it. A **forced inference** is where no source states the answer but only one is possible.

- **Departure: the parameter names `auditEvent`, `securityEvent`, and `cancellationToken`.** The
  corpus writes `e` and `ct` and gives the token a `= default`. ADR-0065 adopts the Microsoft
  naming conventions by reference, and three Microsoft Learn pages read 2026-08-05 rule against
  both spellings: "DO use descriptive parameter names", "DO NOT use abbreviations or contractions
  as part of identifier names", and avoid single-letter names except as loop counters. A parameter
  name is public API under ADR-0044, because a named argument binds to it, so this is a decision
  and not formatting.
- **Forced inference: the audit DTOs live here and not in `Nami.Identity.Contracts`.** Design 01
  section 3.1 gives that package the shared DTOs and this one the ports, which reads as ambiguous
  for a DTO only a port uses. The same section settles it: `Abstractions` depends on nothing, so a
  type in a port's signature cannot come from a package it may not reference.

## A public type is two files, and the second one is not optional

Since 2026-08-02 every project under `src/` carries `PublicAPI.Shipped.txt` and
`PublicAPI.Unshipped.txt` beside its `.csproj`. `Microsoft.CodeAnalysis.PublicApiAnalyzers` fails
the build when a public member is missing from them (ADR-0044 parameter A). All measured:

- **Do not hand-write the entries.** Build, then copy the exact signature out of the `RS0016`
  message. The analyzer's spelling is not guessable: an array of non-nullable strings is
  `string![]!`, a property is two lines, and that is eight lines for a three-property class.
- **New surface goes in `Unshipped`, never `Shipped`.** `Shipped` is what a release promoted and is
  immutable within a major. Nothing has been released, so it holds exactly one line, the
  `#nullable enable` header.
- **That header is project-wide, not per-file, and this is a trap.** With it present in `Shipped`
  only, deleting it from `Unshipped`, where all the entries are, left the build green and silent.
  So a review checking the file the entries are in cannot tell whether nullability is still being
  versioned. Keep it in both.
- **`required` is invisible to the analyzer.** It asks for `Name.set -> void` either way, so adding
  `required` to a shipped member is a breaking change that will not appear in the API diff. It is
  not invisible to the *assembly*, which is why ADR-0044's Confirmation routes it to that ADR's
  second compat layer. **What binds here: the modifier is allowed only while
  `PublicAPI.Shipped.txt` holds no entries.** Promoting anything into `Shipped` is the event that
  has to answer this.
- **An initializer and an enum ordinal are not the same thing to this gate.** Measured 2026-08-08:
  adding two property initializers and explicit values on both enums left the file byte-identical,
  because the analyzer already records ordinals and never records an initializer. So a changed
  default is invisible here, and a reordered enum is visible but unlabelled.
- **The gates disagree by one diagnostic.** `RS0016` fails both `dotnet build` (exit 1) and
  `dotnet format --verify-no-changes` (exit 2). `RS0017`, the stale-entry one, fails only the
  build, because it is set through `<WarningsAsErrors>` in `Directory.Build.props` while format
  reads `.editorconfig`. A severity is matched against the file the diagnostic is reported in, and
  `RS0017` is reported inside the API text file, which no `.editorconfig` section reached.

## An analyzer reference does not break "Abstractions depends on nothing", because of one attribute

`PrivateAssets="all"` on the `PackageReference` is load-bearing, and was proven so. The analyzer's
nuspec declares `developmentDependency=true`, which reads as settling the question and does not.
Packed without `PrivateAssets`, the produced `Nami.Identity.Abstractions.nuspec` carried a real
`<dependency id="Microsoft.CodeAnalysis.PublicApiAnalyzers" version="5.6.0" …/>`, so every consumer
would have restored it. With it the dependency group packs empty. Both readings came out of the
built `.nupkg`.

So **assert against the packed surface or the compile-time references, never against a
`PackageReference` item.** A build-only reference is legitimate and the rule still holds. A test
reading the csproj would fail on a correct file, and would be "fixed" by deleting the analyzer.

## Versions live in `Directory.Packages.props`, and what is written there is a floor

Never put `Version=` on a `PackageReference`: Central Package Management is on and that is
`NU1008`. Omitting the row entirely is `NU1010`. Both exit 1, both measured. Read the constraint
rather than the number. `Version="5.6.0"` restores as `>= 5.6.0`, resolving to
`5.6.0` only because NuGet takes the lowest match. Exact pinning is `[5.6.0]`, which ADR-0021
parameter A requires of OpenIddict and its sub-packages and of nothing else, so the file is meant
to mix both forms.

## `Directory.Build.props` and `.editorconfig` are one mechanism, and the knob is two properties

Both facts are in the root `CLAUDE.md`, ADR-0065, and ADR-0030. What belongs here is what a
project file has to do about them.

- **Write `<TargetFrameworks>$(NamiLibraryTargetFrameworks)</TargetFrameworks>`, never a literal
  framework.** An application reads `$(NamiApplicationTargetFramework)` instead. They are two
  properties because ADR-0030 parameter B multi-targets libraries and single-targets the host.
  Both read `net10.0` today, so a literal looks identical and is wrong the day .NET 12 ships.
  Proven by breaking it: setting the knob to `net99.0` fails with NETSDK1045.
- **Do not set `LangVersion` in a project.** Measured on SDK 10.0.301, a `net10.0` project reports
  `LangVersion 14.0` with nothing set anywhere, because the default derives from the target
  framework. `latest` would make it float with the installed SDK and break that derivation.

## The gates read this folder now, and two of them are not one gate

`dotnet build` and `dotnet format --verify-no-changes` both run in CI and are not interchangeable,
which is measured in ADR-0065's Confirmation. Format needs no `EnforceCodeStyleInBuild`, reports
whitespace as `WHITESPACE` rather than `IDE0055`, and exits 2 rather than 1. Against a planted
`badlyNamed` private field, build exits 1 and format exits 2, both on IDE1006.

**`dotnet format` also fixes.** Run it without `--verify-no-changes` before pushing, rather than
hand-editing to satisfy the diagnostic.

**A third gate watches the gates themselves.** `scripts/test-public-api-gate.sh` breaks a throwaway
project six ways and asserts each break is caught, so a severity deleted from `.editorconfig` or
`Directory.Build.props` reddens CI even when nothing in `src/` changed. A failure on a code-only
change is a report about those config files, and the failing part names which.

**`AnalysisMode` is `Recommended`, and CA1819 is not in it.** Re-measured 2026-08-08 on SDK
10.0.301: **nine** public `byte[]` and `string[]` members across the five DTOs produce no CA1819,
and the whole build reports zero warnings. The same reading counted five on 2026-08-05. So
"properties should not return arrays" is a review matter and not a gate. The designs state the
array types, which is why the members are shaped that way; the green build is not approval.
