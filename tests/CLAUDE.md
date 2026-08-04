# CLAUDE.md for `tests/`

The root [`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule, the content rules, and the
naming and style rules under ADR-0065. All of it applies here.
[`../docs/design/20-testing.md`](../docs/design/20-testing.md) is the authority on the taxonomy,
and ADR-0060 on the strategy. Neither is restated here. There is no `README.md` in this folder
yet.

This folder held nothing but `.gitkeep` until 2026-08-02. It now holds one project, and
everything below was found while landing it.

## A rule you have not failed on purpose is not enforcing anything

This is the third time this repository has learned it, and the first time in a test. ADR-0044's
Confirmation records it for `RS0017`, and ADR-0065's for `.editorconfig` severity. Here it is an
ArchUnitNET rule that reads correctly and checks nothing.

Measured on 2026-08-02 against a `Newtonsoft.Json` reference planted in
`Nami.Identity.Abstractions` and actually called:

| Formulation | Result |
|---|---|
| `OnlyDependOnTypesThat().ResideInNamespaceMatching(...)` | **passed** over the violation |
| `NotDependOnAnyTypesThat().DoNotResideInNamespaceMatching(...)` | failed, correctly |

Same architecture, same violation, same run. The cause is that `ArchLoader` was given one
assembly, so `Architecture.Types` holds only that assembly's types. A formulation that resolves
its *allowed* set out of that collection can never find a foreign type to reject. The dependency
is recorded either way: dumping the offending type's `Dependencies` showed
`Newtonsoft.Json.JsonConvert` in the list.

So **plant a violation and watch the assertion fail before believing a new rule**, and prefer the
negative form. Do not rewrite an existing rule into the sentence it reads as in English without
re-running that check. The readable one here is the one that passes over real breakage.

## Two facts for one rule, because they fail on different evidence

`DependencyRuleTests` asserts design `01` section 3.1 line 97 twice, through the type graph and
through the assembly reference table. Neither covers the rule alone, and the gap is measured
rather than theoretical. A package **referenced but not used by any type** passes both, because an
unused reference is elided from metadata. Closing that needs a check on the packed surface, which
needs a pack and does not exist yet.

**Do not "simplify" the two into one.** Deleting the reflection fact loses the only check that
sees a reference no type touches yet. Deleting the ArchUnitNET fact loses the one that scales to
the slice and layering rules ADR-0024 still owes.

## Never assert on the contents of a `.csproj`

`Nami.Identity.Abstractions` legitimately carries an analyzer `PackageReference` with
`PrivateAssets="all"`, and both facts are green with it in place, measured. A dependency test that
read the project file instead would fail on that correct project. And the obvious way to make it
pass would be deleting the analyzer that ADR-0044 parameter A rests on. Assert against the
compiled artifact.

## There is no `Microsoft.NET.Test.Sdk` and that is deliberate

`xunit.v3` drives Microsoft Testing Platform itself, and the test project sets
`TestingPlatformDotnetTestSupport`, so `dotnet test` needs neither it nor
`xunit.runner.visualstudio`. Measured on 2026-08-02: the two references alone run the suite, and
adding the pair took the restore graph from 23 packages to 28. Every added node is a licence read
owed under ADR-0026, recorded in
[`../docs/DEPENDENCY-LICENSES.md`](../docs/DEPENDENCY-LICENSES.md) section 3.1.

**The failure mode to know**: a test project that omits `TestingPlatformDotnetTestSupport` is
*skipped* by `dotnet test` rather than failing it. A new suite that quietly runs nothing looks
exactly like a new suite that passes.

## The xUnit integration package has a v2 twin with an almost identical name

Take `TngTech.ArchUnitNET.xUnitV3`. The plainly named `TngTech.ArchUnitNET.xUnit` at the same
version declares `xunit.assert 2.4.1`, which is xUnit v2, against ADR-0060's binding to what
`xunit.v3.assert` ships. ADR-0024, design `01` and design `20` all write the base name
`TngTech.ArchUnitNET`, and none of them picks between the variants. So the choice lives in
`Directory.Packages.props` with both nuspec readings beside it.

## What this project does not have, on purpose

- **No `PublicAPI.*.txt`.** ADR-0044 parameter A locks the surface of every public *package*, and
  nothing here is packed (`IsPackable` is false). The analyzer is referenced per-project in
  `src/`, not inherited, so nothing asks for the files.
- **No assertion library.** ADR-0060 settled this on 2026-08-02: assertions come from
  `xunit.v3.assert`. Adding one is a decision to reverse there, not a convenience to take here.
- **No source for the target-framework knob.** The project reads
  `$(NamiApplicationTargetFramework)`, the single-target one, and that is a choice recorded in its
  own `.csproj` rather than a rule being followed. ADR-0030 parameter B splits the two knobs into
  library and host, and a test project is neither. The choice becomes visible, and can first be
  shown wrong, when .NET 12 ships and the two knobs stop reading the same string.
