# CLAUDE.md for `src/`

The root [`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule, the content rules, and the
naming and style rules with ADR-0065 as their authority. All of it applies here, and it is not
repeated. There is no `README.md` in this folder yet. When there is one, it becomes the
authority on this layer's own conventions, and this file keeps only the traps.

This folder held nothing but `.gitkeep` until 2026-08-02. It now holds one project, and almost
everything below is a trap found while landing it rather than a rule inherited from somewhere.

## The evidence rule bites hardest here, because a signature is a claim

A document can leave a detail out and still read as complete. **C# cannot.** Writing a type
forces a decision on every member, every return type, and every nullable annotation. So a
design that omits one leaves a gap that the compiler makes you fill. Filling it from judgement
and shipping it unmarked is how an invented decision enters the codebase wearing the design's
authority.

This is not hypothetical, and it changed what the first project contains. The plan was to land
`ISecretResolver`, on the strength of
[`../docs/design/09-federation-and-claims-profile.md:81-83`](../docs/design/09-federation-and-claims-profile.md),
which gives it as `GetSecretAsync(string reference, CancellationToken) string`. **That cannot
be written.** A method named `…Async` returning a bare `string` is not a C# signature.

The missing piece is not a convention that can be assumed. The same design layer writes
`ValueTask~AuditChainEntry~` and `ValueTask` explicitly at
[`../docs/design/03-audit.md:61,65`](../docs/design/03-audit.md), so it says the task type when
it means one. The omission is an omission. `ScopeDefinition` was landed instead, because
[`../docs/design/23-configuration-and-client-declaration.md:89-93`](../docs/design/23-configuration-and-client-declaration.md)
gives all three of its members **and** their nullability, with that diagram annotating `string?`
on other members in the same block.

So **before writing a type, check that the source fixes every member you are about to write.**
It is not enough that the type is named somewhere. Where the source does not fix them, the port
or the DTO is not ready to land, and saying so is the deliverable.

## Choices with no source, made anyway, and where they are written down

Some cannot be avoided once a file exists. The rule is not to avoid them. The rule is that each
one is recorded as a choice, with its reason and its open verification, and never presented as
sourced.

- **`required` on the non-nullable members of `ScopeDefinition`.** The design marks them
  non-nullable and says a missing required value must fail at start-up
  ([`23:454`](../docs/design/23-configuration-and-client-declaration.md)). But it enforces that
  with `.ValidateDataAnnotations()` at `23:357`, not with the C# `required` modifier. `required`
  was chosen for two reasons. It is the construct that states exactly "must be supplied". And it
  produces no CS8618 without inventing a default value, which `= string.Empty` would.
  **Not verified**: that the options binder at `23:356` populates `required` members. That needs
  the configuration packages, which land with Central Package Management. It is an open item
  rather than a settled fact.
- **`init` rather than `set` on the three audit DTOs, landed 2026-08-05.** The bullet above
  chose `set` for `ScopeDefinition` because a configuration binder writes that type. Nothing
  binds `AuditEvent`, `SecurityEvent`, or `AuditChainEntry`, and design 03 section 3 states no
  accessor either way. `init` was chosen because it makes an event immutable once the sink has
  hashed it, which is the property the chain exists to give. **The open verification above does
  not widen to these three**: no binder touches them, so the `required` question there stays
  about `ScopeDefinition`.
- **`sealed class` rather than `record` for those three.** No source states a shape. A record
  synthesizes `ToString()` over every property, and `ActorSubCiphertext` and `SourceIpHash` are
  exactly the values design 03 keeps off every other lane, so the generated `ToString()` would
  be a leak wearing a convenience. It also adds roughly ten synthesized lines per type to
  `PublicAPI.Unshipped.txt`, and ADR-0044 then owes compatibility on every one of them.
- **No `= default` on the `CancellationToken` of either sink.** A default value is public API
  under ADR-0044, and removing one later is breaking. An explicit token also forces the caller
  on the fail-closed critical path to supply one, which is the path design 03 section 5.3
  describes.
- **The folder taxonomy inside `Abstractions` is flat**, and no source states one. Ten ports
  flat in one folder will be wrong. Inventing the grouping from one type would be worse. Settle
  it when the catalogue lands, not before. Counted 2026-08-08, the folder holds nine public
  types: five sealed classes, two interfaces, and two enums. That is two of the ten ports,
  three of their DTOs, and the four-type definition model. The count was six on 2026-08-05,
  before the definition model landed.

**Five more choices landed 2026-08-08 with `ClientDefinition`, `ClientFlow`, and
`ClientAuthMethod`.** All three are transcribed from the class diagram in design 23 section 3,
which fixes every member and its nullability. What the diagram does not fix is listed here.

- **`set` rather than `init` on all seventeen members.** This follows `ScopeDefinition` and
  not the audit DTOs, for the reason recorded above: a configuration binder writes this type.
  `23:356` binds `List<ClientDefinition>` from `Nami:Clients` exactly as it binds the scope
  list. **This is also a departure**: the external design corpus writes `init` on every member
  at `13-configuration-dx.md:76-95`.
- **`required` on `ClientId` and `DisplayName`, and `= []` on the four `string[]` members.**
  All six are non-nullable in the diagram and none has a stated default, so the two answers
  had to be chosen apart. The real criterion is whether the member has a meaningful empty
  value. `ClientId` and `DisplayName` do not, so `required` applies, which is the same
  reasoning as `ScopeDefinition`. The arrays do: empty grants nothing, which is the
  deny-by-default value. **Half of the array reason is sourced and half is inferred, and an
  earlier draft of this bullet bundled them behind one pointer.** What `23` section 5.2 gives
  `ClientCredentials` is the token endpoint only and **no response type**. Its table has no
  redirect-URI column, so it says nothing about redirect URIs either way. The step from there
  to `= []` is the inference that a flow which never reaches a browser cannot use one, so
  `required` would reject a client the design calls legal.
  **`ScopeDefinition.Resources` answers the same question the other way**, as `required
  string[]` with no initializer, and both types come from class diagrams in the same
  document. The divergence is deliberate under the criterion above, since a scope granting
  access to no resource is arguably not a scope, but it was not noticed when
  `ScopeDefinition` landed. Read it as an open consistency question rather than a settled
  split.
- **`Flow` has no stated default anywhere, and now carries `= ClientFlow.Code` anyway.**
  Design 23 states a default for six members in the prose under its diagram and states none
  for this one. The initializer writes down what C# would do regardless, so it changes no
  behaviour and makes no claim. It is recorded here because the line itself must not be read
  later as evidence that a default was decided.
- **Both enums carry explicit ordinals, and the two stated defaults are initializers.**
  This reverses the shape this increment first landed. `ClientAuthMethod.PrivateKeyJwt` sat
  at ordinal 0 **because** design 23 section 3 makes it the default, "so the secure choice is
  the one you get by omission", and nothing else expressed that. A reorder would then have
  moved every undeclared client onto the weaker credential, and `PublicAPI.Unshipped.txt`
  would have shown it only as `= 0` becoming `= 1`, which does not read as a security change.
  Two things fixed it, both free while `PublicAPI.Shipped.txt` holds no entries. The default
  moved onto the property as `= ClientAuthMethod.PrivateKeyJwt`, which is transcription
  because design 23 states it. And both enums now write their ordinals, so the source agrees
  with the contract file. **The binder is the second reason for the ordinals**: the Microsoft
  configuration binder accepts a numeric string for an enum member, so a settings file can
  carry `"Flow": 3`, and an unwritten ordinal would let a reorder repoint it silently.
- **One member design 23 names was deliberately not written.** `23:153` lists
  `BackchannelLogoutUri` in its "Definition field" column, and its own class diagram at
  `23:70-88` does not declare it. Three other sources put it on the Application write path
  instead: `design/15-admin-api.md:133` and `:141` carry it on `ApplicationDto` and
  `ApplicationPolicyDto`, and `adr/0019-single-logout-strategy.md:49` calls it "a new field on
  the Application". The corpus agrees, and records the move at `25-design-admin-api.md:305`.
  So the type has seventeen members and not eighteen, and the contradiction is filed as a
  BUILD-PLAN row against design 23 rather than resolved here.

## Where a source exists and this repository does not simply copy it

The section above is for a gap. This one is for the two other shapes, and they are recorded
differently because a reader has to be able to tell them apart. One is a **departure**, where a
source says something and a second source overrules it. The other is a **forced inference**,
where no source states the answer but only one answer is possible.

- **Departure: the parameter names `auditEvent`, `securityEvent`, and `cancellationToken`.**
  The external design corpus this layer was reconciled from writes `e` and `ct`, and gives the
  token a `= default`. ADR-0065 adopts the Microsoft naming conventions by reference, and three
  Microsoft Learn pages read on 2026-08-05 rule against both spellings: "DO use descriptive
  parameter names", "DO NOT use abbreviations or contractions as part of identifier names", and
  avoid single-letter names except as loop counters. A parameter name is public API under
  ADR-0044, because a named argument binds to it, so this is a decision and not formatting.
- **Forced inference: the audit DTOs live here and not in `Nami.Identity.Contracts`.** Design
  01 section 3.1 gives that package the shared DTOs and gives this one the ports, which reads
  as ambiguous for a DTO that only a port uses. The same section settles it anyway:
  `Abstractions` depends on nothing, so a type in a port's signature cannot come from a package
  `Abstractions` may not reference.

## A public type is two files, and the second one is not optional

Since 2026-08-02 every project under `src/` carries `PublicAPI.Shipped.txt` and
`PublicAPI.Unshipped.txt` beside its `.csproj`. `Microsoft.CodeAnalysis.PublicApiAnalyzers`
fails the build when a public member is missing from them (ADR-0044 parameter A). The practical
consequences, all measured:

- **Do not hand-write the entries.** Build, then copy the exact signature out of the `RS0016`
  message. The analyzer's spelling is not guessable. An array of non-nullable strings is
  `string![]!`. A property is two lines (`.get -> T!` and `.set -> void`), plus a line for the
  type and one for the constructor. That is eight lines for a three-property class.
- **New surface goes in `Unshipped`, never in `Shipped`.** `Shipped` is what a release promoted,
  and it is immutable within a major. Nothing has been released, so `Shipped` holds exactly one
  line today, the `#nullable enable` header.
- **That header is project-wide, not per-file, and this is a trap.** With it present in `Shipped`
  only, deleting it from `Unshipped`, where all the entries are, left the build green and silent.
  So a review that checks the file the entries are in cannot tell whether nullability is still
  being versioned. Both files carry it, so keep it that way.
- **`required` is invisible to the analyzer.** It asks for `Name.set -> void` either way, so
  adding `required` to a shipped member is a breaking change that will not appear in the API
  diff. It is not invisible to the *assembly*, though. That is why ADR-0044's Confirmation routes
  it to that ADR's second compat layer rather than to a hand-kept manifest. **What binds
  here: the modifier is allowed only while `PublicAPI.Shipped.txt` holds no entries.** Promoting
  anything into `Shipped` is the event that has to answer this, and a type carrying `required` is
  the thing that will be asked about.
- **The gates disagree by one diagnostic.** `RS0016` fails both `dotnet build` (exit 1) and
  `dotnet format --verify-no-changes` (exit 2). `RS0017`, the stale-entry one, fails only the
  build, because it is set through `<WarningsAsErrors>` in `Directory.Build.props` and format
  reads `.editorconfig`. Both files say why at length. The short version: a severity is matched
  against the file the diagnostic is reported in. `RS0017` is reported inside the API text file,
  where no `.editorconfig` section this repository tried could reach it.

## An analyzer reference does not break "Abstractions depends on nothing", but only because of one attribute

`PrivateAssets="all"` on the `PackageReference` is load-bearing, and it was proven so. The
analyzer's own nuspec declares `developmentDependency=true`, which reads as settling the
question and does not. Packed without `PrivateAssets`, the produced
`Nami.Identity.Abstractions.nuspec` carried a real
`<dependency id="Microsoft.CodeAnalysis.PublicApiAnalyzers" version="5.6.0" …/>`, so every
consumer would have restored it. With it, the dependency group packs empty. Both readings came
out of the built `.nupkg`.

So when the architecture test lands, **assert against the packed surface or the compile-time
references, not against the presence of a `PackageReference` item.** A build-only reference is
legitimate and the rule still holds. A test that reads the csproj would fail on a correct file,
and it would be "fixed" by deleting the analyzer.

## Versions live in `Directory.Packages.props`, and what is written there is a floor

Never put `Version=` on a `PackageReference`. Central Package Management is on, and that is
`NU1008`. Omitting the row entirely is `NU1010`. Both exit 1, and both were measured.

Read the constraint rather than the number. `Version="5.6.0"` restores as `>= 5.6.0` in
`obj/project.assets.json`, and it resolves to `5.6.0` only because NuGet takes the lowest match.
Exact pinning is `[5.6.0]`, which ADR-0021 parameter A requires of OpenIddict and its
sub-packages and of nothing else. So this file is meant to mix the two forms, and no row uses
the bracket yet.

## `Directory.Build.props` and `.editorconfig` are one mechanism, and the knob is two properties

Both facts are in the root `CLAUDE.md` and in ADR-0065 and ADR-0030. What belongs here is what
a project file has to do about them.

- **Write `<TargetFrameworks>$(NamiLibraryTargetFrameworks)</TargetFrameworks>`, never a literal
  framework.** A library reads the library property, and an application reads
  `$(NamiApplicationTargetFramework)`. They are two properties because ADR-0030 parameter B
  multi-targets libraries and single-targets the host. Both read `net10.0` today, so writing a
  literal looks identical and is wrong the day .NET 12 ships. Proven by breaking it: setting the
  knob to `net99.0` fails the build with NETSDK1045, so the project genuinely reads it.
- **Do not set `LangVersion` in a project.** Measured on SDK 10.0.301, a `net10.0` project
  reports `LangVersion 14.0` with nothing set anywhere, because the default is derived from the
  target framework. `latest` would make it float with the installed SDK and break that
  derivation. `Directory.Build.props` says the same at more length.

## The gates read this folder now, and two of them are not one gate

`dotnet build` and `dotnet format --verify-no-changes` both run in CI against the solution. They
are not interchangeable, which is measured and recorded in ADR-0065's Confirmation. The format
path needs no `EnforceCodeStyleInBuild`, it reports whitespace as `WHITESPACE` rather than
`IDE0055`, and it exits 2 rather than 1. Against a planted `badlyNamed` private field, the build
path exits 1 and the format path exits 2, both on IDE1006.

**`dotnet format` also fixes.** Run it without `--verify-no-changes` before pushing, rather than
hand-editing to satisfy the diagnostic.

**A third gate watches the gates themselves.** `scripts/test-public-api-gate.sh` breaks a
throwaway project six ways and asserts each break is caught. So a severity deleted from
`.editorconfig` or `Directory.Build.props` reddens CI even when nothing in `src/` changed. If it
fails on a change that only touched code here, read it as a report about the three config files
rather than about the code. The failing part names which file.

**`AnalysisMode` is `Recommended`, and CA1819 is not in it.** Re-measured 2026-08-08 on SDK
10.0.301: **nine** public `byte[]` and `string[]` members across `AuditEvent`, `SecurityEvent`,
`AuditChainEntry`, `ScopeDefinition`, and `ClientDefinition` produce no CA1819, and the whole
build reports zero warnings. The same reading on 2026-08-05 counted five, before
`ClientDefinition` added four. So "properties should not return arrays" is a review matter here
and not a gate. The designs state `byte[]` and `string[]`, which is why the members are shaped
that way, but do not read the green build as the analyzer having approved them.
