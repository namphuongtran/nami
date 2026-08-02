# CLAUDE.md for `src/`

The root [`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule, the content rules, and
the naming and style rules with ADR-0065 as their authority. All of it applies here and is
not repeated. There is no `README.md` in this folder yet; when there is one, it becomes the
authority on this layer's own conventions and this file keeps only the traps.

This folder held nothing but `.gitkeep` until 2026-08-02. It now holds one project, and
almost everything below is a trap found while landing it rather than a rule inherited from
somewhere.

## The evidence rule bites hardest here, because a signature is a claim

A document can leave a detail out and still read as complete. **C# cannot.** Writing a type
forces a decision on every member, every return type, and every nullable annotation, so a
design that omits one leaves a gap that the compiler makes you fill. Filling it from
judgement and shipping it unmarked is how an invented decision enters the codebase wearing
the design's authority.

This is not hypothetical and it changed what the first project contains. The plan was to
land `ISecretResolver`, on the strength of
[`../docs/design/09-federation-and-claims-profile.md:81-83`](../docs/design/09-federation-and-claims-profile.md),
which gives it as `GetSecretAsync(string reference, CancellationToken) string`. **That
cannot be written.** A method named `…Async` returning a bare `string` is not a C#
signature, and the missing piece is not a convention that can be assumed: the same design
layer writes `ValueTask~AuditChainEntry~` and `ValueTask` explicitly at
[`../docs/design/03-audit.md:61,65`](../docs/design/03-audit.md), so it says the task type
when it means one. The omission is an omission. `ScopeDefinition` was landed instead
because [`../docs/design/23-configuration-and-client-declaration.md:87-91`](../docs/design/23-configuration-and-client-declaration.md)
gives all three of its members **and** their nullability, that diagram annotating `string?`
on other members in the same block.

So: **before writing a type, check that the source fixes every member you are about to
write, not merely that the type is named somewhere.** Where it does not, the port or the
DTO is not ready to land, and saying so is the deliverable.

## Choices with no source, made anyway, and where they are written down

Some cannot be avoided once a file exists. The rule is not to avoid them; it is that each
one is recorded as a choice with its reason and its open verification, never presented as
sourced.

- **`required` on the non-nullable members of `ScopeDefinition`.** The design marks them
  non-nullable and says a missing required value must fail at start-up
  ([`23:454`](../docs/design/23-configuration-and-client-declaration.md)), but it enforces
  that with `.ValidateDataAnnotations()` at `23:357`, not with the C# `required` modifier.
  `required` was chosen because it is the construct that states exactly "must be supplied"
  and it produces no CS8618 without inventing a default value, which `= string.Empty` would.
  **Not verified**: that the options binder at `23:356` populates `required` members. That
  needs the configuration packages, which land with Central Package Management, and it is
  an open item rather than a settled fact.
- **The folder taxonomy inside `Abstractions` is flat**, and no source states one. Ten ports
  flat in one folder will be wrong; inventing the grouping from one type would be worse.
  Settle it when the catalogue lands, not before.

## `Directory.Build.props` and `.editorconfig` are one mechanism, and the knob is two properties

Both facts are in the root `CLAUDE.md` and in ADR-0065 and ADR-0030. What belongs here is
what a project file has to do about them:

- **Write `<TargetFrameworks>$(NamiLibraryTargetFrameworks)</TargetFrameworks>`, never a
  literal framework.** A library reads the library property and an application reads
  `$(NamiApplicationTargetFramework)`. They are two properties because ADR-0030 parameter B
  multi-targets libraries and single-targets the host; both read `net10.0` today, so writing
  a literal looks identical and is wrong the day .NET 12 ships. Proven by breaking: setting
  the knob to `net99.0` fails the build with NETSDK1045, so the project genuinely reads it.
- **Do not set `LangVersion` in a project.** Measured on SDK 10.0.301: a `net10.0` project
  reports `LangVersion 14.0` with nothing set anywhere, because the default is derived from
  the target framework. `latest` would make it float with the installed SDK and break that
  derivation. `Directory.Build.props` says the same at more length.

## The gates read this folder now, and two of them are not one gate

`dotnet build` and `dotnet format --verify-no-changes` both run in CI against the solution.
They are not interchangeable, which is measured and recorded in ADR-0065's Confirmation:
the format path needs no `EnforceCodeStyleInBuild`, reports whitespace as `WHITESPACE`
rather than `IDE0055`, and exits 2 rather than 1. Against a planted `badlyNamed` private
field, the build path exits 1 and the format path exits 2, both on IDE1006.

**`dotnet format` also fixes.** Run it without `--verify-no-changes` before pushing rather
than hand-editing to satisfy the diagnostic.
