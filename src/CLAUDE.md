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
- **The folder taxonomy inside `Abstractions` is flat**, and no source states one. Ten ports
  flat in one folder will be wrong. Inventing the grouping from one type would be worse. Settle
  it when the catalogue lands, not before.

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
