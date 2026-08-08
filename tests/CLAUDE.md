# CLAUDE.md for `tests/`

The root [`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule, the content rules, and the
naming and style rules under ADR-0065. All of it applies here.
[`../docs/design/20-testing.md`](../docs/design/20-testing.md) is the authority on the taxonomy,
and ADR-0060 on the strategy. Neither is restated here. There is no `README.md` in this folder
yet.

This folder held nothing but `.gitkeep` until 2026-08-02. It now holds two projects:
`Nami.Identity.ArchitectureTests` from 2026-08-02, and `Nami.Identity.UnitTests` from
2026-08-08. Everything below was found while landing one of them, and a section carrying a date
says which. A section with no date predates the unit suite. Two of the seven suites the taxonomy
names existed at that second date, so ADR-0060's build-time confirmation of the taxonomy is
started rather than closed.

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

**Confirmed a second time on 2026-08-08, with a real engine package rather than a planted one.**
Seed S-008 added `OpenIddict.Server` and `OpenIddict.Server.AspNetCore` to `Nami.Identity.Core` and
wrote no code against either. The restore graph went from two nodes to ten, and the built
`Nami.Identity.Core.dll` carried **no `OpenIddict` string at all**. So the gap is not a corner case
of the fixture used to find it: eight new packages entered the graph and the compiled surface did
not move. **The plan for that seed said the facts would gain something to catch, and that was
wrong**, which is why the claim is now measured in two places rather than asserted in one.

## A namespace allow-list cannot police a library that puts its builders in yours

Learned 2026-08-08, wiring the engine in seed S-010, and it is the sharper half of the two-facts
rule above.

Read at the OpenIddict 7.6.0 upstream commit, **every** `Add*` and `Use*` extension and **every**
builder type is declared in `Microsoft.Extensions.DependencyInjection`, not in an `OpenIddict.*`
namespace. `OpenIddictBuilder`, `OpenIddictServerBuilder`, `OpenIddictCoreBuilder`, and
`OpenIddictEntityFrameworkCoreBuilder` are all in that one namespace. Only options and constants
types sit under `OpenIddict.*`.

So the type-graph fact is **structurally blind** to the violation that matters here.
`services.AddOpenIddict().AddCore(o => o.UseEntityFrameworkCore())` is exactly what design 01
section 3.1 forbids `Core` to contain, and it names no type outside the allow-list. **No widening of
that list could catch it, and no narrowing either.** Measured by planting that call: the fact stayed
green.

The assembly-reference fact is the only one that can see it, because it reads assembly names rather
than namespaces. **And its forbidden-prefix list did not catch it either**, because
`"OpenIddict.EntityFrameworkCore".StartsWith("Microsoft.EntityFrameworkCore")` is false and so is
`"OpenIddict.Quartz".StartsWith("Quartz")`. Two entries that look like they cover those packages
cover different ones. Three `OpenIddict.` prefixes were added and the plant then failed.

**Two habits generalise.** When a library declares its extension methods in a framework namespace,
a namespace rule cannot distinguish its allowed surface from its forbidden one, so the assembly-level
rule carries the whole weight and must be checked against that library's real assembly names. And a
forbidden-prefix list is a claim about string prefixes: write the assembly names you mean to reject
and test `StartsWith` against them, rather than trusting that a familiar-looking entry covers a
same-named package from a different vendor.

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

## What both projects do not have, on purpose

- **No `PublicAPI.*.txt`.** ADR-0044 parameter A locks the surface of every public *package*, and
  nothing here is packed (`IsPackable` is false). The analyzer is referenced per-project in
  `src/`, not inherited, so nothing asks for the files.
- **No assertion library.** ADR-0060 settled this on 2026-08-02: assertions come from
  `xunit.v3.assert`. Adding one is a decision to reverse there, not a convenience to take here.
- **No source for the target-framework knob.** Both projects read
  `$(NamiApplicationTargetFramework)`, the single-target one, and that is a choice recorded in
  each `.csproj` rather than a rule being followed. ADR-0030 parameter B splits the two knobs into
  library and host, and a test project is neither. The choice becomes visible, and can first be
  shown wrong, when .NET 12 ships and the two knobs stop reading the same string.
- **No source for either project name, and they are unsourced differently.**
  `Nami.Identity.ArchitectureTests` is named by ADR-0024's enforcement clause, so only its
  *placement* here was a choice. `Nami.Identity.UnitTests` is named by nothing: searched
  2026-08-08 with `git grep -c "UnitTests"` over all tracked files, zero hits, and design 20
  section 3.1's Unit row gives a tool and an owning ADR and no project. Do not cite the taxonomy
  for it.

## A Given/When/Then name fails the build, and the rule that stops it is not the naming ruleset

Learned 2026-08-08, landing the unit suite. Two accepted decisions collide, and the one that
looks responsible is not.

ADR-0060 requires tests "named and structured as scenarios, in Given / When / Then form", and
ADR-0065:88 restates it. Written out, `GivenX_WhenY_ThenZ` carries underscores. **The check that
rejects them is `CA1707`, not `IDE1006`.** `.editorconfig` declares naming symbols for `field`
and for `async` methods and none for a plain method, so the naming ruleset never reaches a test
name. `CA1707` arrives from `AnalysisMode` being `Recommended` (ADR-0094), and
`TreatWarningsAsErrors` makes it an error. Measured 2026-08-08 over a suite of fifteen test
methods: fifteen distinct `CA1707` errors, against none from the same build with
`-p:AnalysisMode=Default`. That matches the rule's own page, which lists it as not enabled by
default in .NET 10.

**The resolution is a per-project `<NoWarn>`, and an `.editorconfig` section is the wrong
answer even though it works.** This was written the wrong way first and reverted on evidence.
ADR-0093 parameter B rules on it directly: "No carve-out for `tests/` ... a warning suppressed
by directory is a suppression nobody re-reads. Where a specific test genuinely needs a warning,
parameter D is the route" (`0093:94-98`). ADR-0094:90-91 imports the same reasoning onto the
analyzer axis. Parameter D is "a per-project `<NoWarn>` with a comment ... that says which
diagnostic, why, and what would let it be removed" (`0093:133-136`). CA1707's own page sanctions
the exemption in its own words, "it's safe to suppress this warning for test code", and says
nothing about where to write it. So the ADRs decide the mechanism and the rule's page decides
only that an exemption is legitimate at all.

**Every future test project opts in for itself.** That is the property a directory glob would
have removed, and it is the whole content of parameter B.

**The collision is narrower than it looks, and no source requires the underscore.** ADR-0060:61
asks for names "structured as scenarios, in Given / When / Then form", and ADR-0065:88 restates
that without adding a spelling. `GivenXWhenYThenZ` in PascalCase satisfies both and needs no
exemption. The exemption buys legibility, not compliance, and that is what makes it removable.

**Three general shapes, worth more than the rule itself.** A naming question here has two
possible owners, the `.editorconfig` ruleset and the analyzer set, and reading only the first
gives a clean answer the build then contradicts. A suppression has an owning ADR even when the
diagnostic does not. And the mechanism a suppression is written in is itself a decision, so
reaching for the one that is easiest to write is how a config file comes to hold a rule.

## The unit suite is the only gate on a default, and this was measured

Design 23 section 8 calls the defaults of `ClientDefinition` the entire security argument for
that layer, and section 7 makes changing one a behaviour break under ADR-0044. Nothing
mechanical sees it. Measured 2026-08-08 on SDK 10.0.301 with `RequirePkce` silently losing its
`= true`: `dotnet build` reported `0 Warning(s)` and `0 Error(s)`, `dotnet format
--verify-no-changes` exited 0, and `PublicAPI.Unshipped.txt` was byte-identical by SHA-256. One
unit fact failed. That is the whole reason the suite exists.

**Each of its fifteen facts was watched to fail before being believed**, per the rule above about
a rule you have not failed on purpose. Eight single-property breaks failed exactly one fact each.
Swapping the two `ClientAuthMethod` ordinals failed two, and adding a `Password` member to
`ClientFlow` failed two, both read from the run log rather than inferred.

**Two traps came out of doing that.** `= false` is not the way to break a `true` default: it
trips `CA1805`, so the break is deleting the initializer. And an enum break cannot be tested
alone, because `RS0016` and `RS0017` fail the build first; the API text file has to be updated in
the same step, which is what a real reorder commit would do anyway.

**Four members the suite left out at first, found by review and measured 2026-08-08.** The
nullable ones. A non-null default on `ClientSecret` or `JwksJson` moves every undeclared client
onto the confidential branch of design 23 section 5.1 invariant 1, so invariant 2's forced proof
key never applies to it. That reaches the same control as `RequirePkce` by a second route, and
with `ClientSecret = "s3cret"` planted the build was green, the API file byte-identical, and all
fourteen facts then in the solution passed. **Read the whole class, not the members that look
interesting**: the type has seventeen members, fifteen have a default, and the two without one are
`required` so there is nothing to pin.

**One hole that stays open, and it is not closable this way.** Deleting the
`= ClientAuthMethod.PrivateKeyJwt` initializer from `ClientDefinition.AuthMethod` is caught by
nothing: build green, format green, no API diff, and every fact passes. The property fact
cannot see it, because the value survives through the enum's ordinal 0. That is correct
behaviour-first testing rather than a defect in the fact, and it is still a hole, because
`src/CLAUDE.md` records the initializer and the ordinal as a deliberate pair so that neither
alone decides the credential. Either half can now be removed with every gate green. **Do not
close it by asserting on the initializer through reflection**, which is the implementation detail
ADR-0060:60 forbids. The same holds for `Flow`.
