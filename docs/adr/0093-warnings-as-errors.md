---
status: "accepted"
date: 2026-08-03
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0065 (the analyzers-at-error posture and the measured local-versus-CI argument this reuses), ADR-0044 (the public-API analyzers already riding WarningsAsErrors), ADR-0092 (the SDK security-analysis axis, whose owed property question the withholding comment this ADR replaces already names as adjacent), ADR-0021 and ADR-0030 (the upgrade cadence whose deprecations this converts into build breaks), ADR-0026 (the dependency policy whose gate reads licences rather than vulnerabilities, which is why the restore-time carve-out routes elsewhere)
informed: all contributors, and any adopter building this repository
---

# Every warning fails the build, with the restore-time vulnerability codes carved out

## Context and Problem Statement

`TreatWarningsAsErrors` has been withheld from this repository on purpose, and the file that
withholds it records why. `Directory.Build.props:95-100` reads:

> TreatWarningsAsErrors is NOT set. The corpus task 1.4 asks for it, no ADR does, and turning
> it on in the same change that lands the first project would make every future warning a
> build break before anyone has seen what warnings this codebase produces. It is a separate
> decision with a separate measurement, and ADR-0092's `_warnaserror` globalconfig item is
> adjacent to it. Measured default: false.

`docs/BUILD-PLAN.md:57` carries the matching owed row, pointing at that same line, with the
trigger written as "Open; no ADR asks for it, the design corpus does".

Both halves of the withholding have now expired, and each expired for a different reason.
The first, "no ADR does", is what this ADR is. The second, "before anyone has seen what
warnings this codebase produces", was a statement about an empty `src/`: the first project
landed on 2026-08-02, so there is finally something to measure the property against, and the
measurement below is that turning it on today costs nothing at all.

What is not in question is whether a warning can fail a build here, because two narrower
versions of this rule already run. `Directory.Build.props:62` promotes one named diagnostic,
`RS0017`, through `<WarningsAsErrors>`, which is where ADR-0044 parameter A had to put it
(`0044:81`). `.editorconfig` plus `EnforceCodeStyleInBuild` makes `IDE1006` and `IDE0055`
build failures, which ADR-0065 states as its agreed core of "exactly two diagnostics"
(`0065:41`) and as one mechanism spanning two files (`0065:43`). So the open question is
narrower than it looks: it is whether the **default** for an unnamed warning is fail or pass,
and today it is pass, one diagnostic at a time being lifted out of it by hand.

## Decision Drivers

* **A warning nobody reads is a defect with a delay.** The whole cost of a warning is that it
  is survivable, so it survives, and the pre-alpha window is when a codebase can adopt the
  strict posture for free.
* **Adopt it while the cost is measured rather than argued.** This repository has been wrong
  before by assuming what a build does, and the fix each time was to run it. The property is
  worth taking now precisely because the number is available and the number is zero.
* **One gate, not two paths that disagree.** ADR-0065 measured what a gate that is green
  locally and red in CI costs, and the cost lands on the contributor (`0065:102`).
* **A gate must not be able to block the build before anything can be compiled.** A promotion
  that happens at restore is categorically different from one that happens at compile,
  because there is no code to fix yet when it fires.
* **State the escape hatch, because the pressure is predicted.** ADR-0065 names the risk that
  arrives with real code as "the pressure to weaken a rule rather than fix the code that trips
  it" (`0065:109`). A strict gate with no legitimate, narrow exemption route invites the
  illegitimate broad one.

## Considered Options

* **A. `TreatWarningsAsErrors=true` in `Directory.Build.props`**, repo-wide, local and CI
  alike, with the restore-time audit codes exempted.
* **B. A CI-only `-warnaserror`**, leaving the local build permissive.
* **C. Leave it unset**, and keep promoting diagnostics one at a time as an ADR names each.

## Decision Outcome

Chosen option: **A**. The five parameters below are binding.

### A. Repo-wide in `Directory.Build.props`, not a CI-only flag

`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props`, so it is
inherited by every project and applies to the contributor's `dotnet build` exactly as it
applies to CI.

**The CI-only form is rejected on an argument this repository has already measured rather
than on taste.** ADR-0065 kept both of its enforcement paths for the same reason and wrote
down what happens when they diverge (`0065:102`): removing the build-side half "silences
`dotnet build` completely while the format path keeps reporting", so "a CI gate built on the
format path alone stays green through either break, and the cost lands on contributors, whose
local build goes quiet while CI does not". A CI-only `-warnaserror` builds that divergence in
deliberately. The contributor sees a clean build, pushes, and learns from the runner. This
project decided against that shape once already, and the second instance is not different
enough to decide differently.

### B. Tests are included

No carve-out for `tests/`. Test code is code that has to keep compiling across the upgrade
cadence in parameter E, and a warning suppressed by directory is a suppression nobody
re-reads. Where a specific test genuinely needs a warning, parameter D is the route.

### C. The restore-time audit codes are exempt

```xml
<WarningsNotAsErrors>$(WarningsNotAsErrors);NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
```

The `$(...)` prefix appends rather than replaces, which is the same shape and the same reason
as the `WarningsAsErrors` line at `Directory.Build.props:62`: it preserves anything a project
or a later property sets.

Two reasons, and they are independent.

**Blocking on dependency vulnerabilities already has an owner, and it is not this ADR.**
ADR-0092 stage 2 takes Trivy for the dependency vulnerability scan and makes it blocking
(`0092:116-121`), and that section is explicit that this is a different question from
ADR-0026's licence gate, "which reads licences rather than vulnerabilities" (`0092:118-119`).
Promoting `NU1901` to `NU1904` here would put a second blocking gate on one question, with a
different severity policy, in a file that names no owner for it.

**The promotion happens at restore, which is earlier than the point where it can be acted
on.** Measured 2026-08-03 on SDK 10.0.301 (see below), an advisory with no patched version
available would fail `dotnet restore`, so nothing can be compiled, tested, or bisected while
the maintainer works out what to do about it. A compile-time warning stops the build after
the code is readable to the tools; a restore-time one stops it before.

**The carve-out is exactly those four codes, and the boundary is deliberate.** Other
restore-time warnings stay promoted, so a restore can still fail under this exemption.
`NU1510`, the package-pruning advisory, is the measured case: the second fixture below raises
it alongside `NU1904`, and with the exemption in place `NU1904` is demoted while `NU1510`
stays an error and the restore exits 1. That is intended, because `NU1510`'s remedy is
available immediately, it asking for a redundant `PackageReference` to be removed. It is the
absence of an available remedy, not the restore phase alone, that earns an exemption.

### D. The exemption mechanism is a per-project `<NoWarn>` with a comment

A warning that must be tolerated is named in the project that tolerates it, in a `<NoWarn>`
carrying a comment that says which diagnostic, why, and what would let it be removed. **Never
a broad list in `Directory.Build.props`**, because a repo-wide suppression is a silent
retraction of this decision for every project that inherits it.

This is the root `CLAUDE.md` rule applied to a build rather than to a document: "Never edit a
document to silence a checker" (`CLAUDE.md:112`). ADR-0065 predicts the pressure that produces
that edit and names it as the risk arriving with real code (`0065:109`). A narrow, commented,
per-project exemption is the legitimate version of that pressure's outlet, and its existence
is what makes the broad version indefensible rather than merely discouraged.

### E. The upgrade-churn consequence is stated, not discovered

An OpenIddict or .NET bump that deprecates an API stops being a warning and becomes a build
break. That is intended, and it is the point rather than a side effect: ADR-0021's per-release
playbook already instructs the project to "clear obsolete warnings on 7.5 now" ahead of the
8.0 bump, which removes all obsolete members (`0021:44`), and ADR-0030's parameter F reads the
.NET, ASP.NET Core, and EF Core breaking changes at each LTS bump (`0030:42`). Under this ADR
those obsolete warnings cannot be carried quietly to the bump that deletes them. The escape
hatch during an upgrade in progress is parameter D, per project, with a comment naming the
bump.

### The measurements, 2026-08-03, .NET SDK 10.0.301

Every figure here is a measurement with a date. Re-run it rather than cite it forward.

**The cost of turning it on today is zero.** The solution builds identically with and without
the property:

```text
dotnet build Nami.Identity.slnx                                -> 0 Warning(s), 0 Error(s), exit 0
dotnet build Nami.Identity.slnx -p:TreatWarningsAsErrors=true  -> 0 Warning(s), 0 Error(s), exit 0
```

**The property genuinely promotes a plain compiler warning.** A throwaway `net10.0` project
with one unused local, rebuilt so the compiler actually runs:

| Properties | `CS0219` | Exit |
|---|---|---|
| none | warning | 0 |
| `TreatWarningsAsErrors=true` | error | 1 |

**It also reaches restore, and the NuGet audit is already on by default.**
`NuGet.targets:73-82` in the installed SDK sets `NuGetAudit` to `true`, `NuGetAuditLevel` to
`low`, and, for `net10.0` and above, `NuGetAuditMode` to `all`, each guarded by an
`'$(X)' == ''` condition, which is what makes them defaults rather than settings. The file's
own comment on the level reads: *"Report all severity vulnerabilities (low severity and
higher). Allowed values are: low, moderate, high, critical"*. `NuGet.targets:946-948` puts
`TreatWarningsAsErrors`, `WarningsAsErrors` and `WarningsNotAsErrors` on the restore graph
entry that `_GenerateRestoreProjectSpec` (`NuGet.targets:877`) returns, and
`NuGet.targets:199` hands that item set to the `RestoreTask` declared on the line above it.
That is the mechanism by which a compiler-facing property reaches a restore-time code.

**Those three line numbers were corrected on 2026-08-03, and the correction is worth keeping
rather than quietly applying.** This paragraph first cited `NuGet.targets:264-266`, which does
carry those same three property names and has nothing to do with restore: they are attributes
on `CheckForDuplicateNuGetItemsTask` inside the `CollectPackageReferences` target
(`NuGet.targets:251`), whose `LogCode` is `NU1504` (`NuGet.targets:262`), a duplicate
`PackageReference` check. A pointer that resolves to real lines carrying the right words is
exactly the shape the root `CLAUDE.md` warns about, and this one passed every mechanical check
and a review before the read that found it. Every `NuGet.targets` line number here was read in
SDK 10.0.301 and moves with the SDK.

**Two fixtures were measured, and the second one is the one worth reading.** Both are
throwaway `net10.0` projects with the properties set **in the project file**, which is not
incidental; see the method note below.

The first isolates the audit code. `Newtonsoft.Json` 12.0.1 carries a known high-severity
advisory and is not prunable on `net10.0`, so `NU1903` fires alone:

| Properties | `NU1903` | Restore exit |
|---|---|---|
| none | warning | 0 |
| `TreatWarningsAsErrors=true` | `error NU1903: Warning As Error` | 1 |
| plus the parameter C exemption | warning | 0 |

That third row is parameter C doing the job it exists for: the code is demoted and the
restore succeeds.

The second fixture shows the boundary. `System.Text.Encodings.Web` 4.5.0 carries a known
critical advisory **and** is prunable on `net10.0`, so it raises `NU1510` alongside `NU1904`:

| Properties | `NU1904` | `NU1510` | Restore exit |
|---|---|---|---|
| none | warning | warning | 0 |
| `TreatWarningsAsErrors=true` | error | error | 1 |
| plus the parameter C exemption | warning | **error** | **1** |

**In that third row the exemption works and the restore still fails**, and the two facts are
not in tension. `NU1904` is demoted exactly as intended; `NU1510` is not, because it is not
one of the four codes, so the restore exits 1 on the pruning advisory alone. That is the
decided behaviour rather than a defect in the carve-out: parameter C exempts the codes whose
remedy may not exist yet, and `NU1510`'s remedy is to delete a line. Both tables are here
because the first one on its own would invite the reading that parameter C means "a
vulnerable dependency cannot fail restore", and against this second fixture that reading is
wrong.

**Method note, recorded because it produced a wrong measurement before review caught it.**
The carve-out cannot be passed on the command line: `;` is MSBuild's CLI property separator,
so `dotnet restore -p:"WarningsNotAsErrors=NU1901;NU1902;NU1903;NU1904"` exits 1 with
`MSBUILD : error MSB1006: Property is not valid. Switch: NU1902` and never reaches restore.
Escaping it as `%3B` does pass, and that is the shape that invites the real error, because a
command line long enough to hide a fifth code in still looks like the decided configuration.
Every row above was taken from a project file instead, which is also how
`Directory.Build.props` will set it.

### What this ADR deliberately does not write

**`NuGetAudit`, `NuGetAuditMode` and `NuGetAuditLevel` are not set.** The measurement above
read them at their defaults in the SDK's own targets file, and those defaults are already the
strictest values the properties accept: auditing on, the lowest severity threshold, and
transitive rather than direct-only. Writing them into `Directory.Build.props` could only
restate a default, which rots against the SDK without adding enforcement.

That is the precedent `Directory.Build.props:75-84` set for `LangVersion`, and the reasoning
is quoted from it rather than reinvented: writing the value "could only restate that default,
which rots against the SDK", and the alternative of writing a floating value "would make the
language version float with whatever SDK is installed". The same file records that this is
ADR-0065's "write only what deviates" rule applied to a second file; this ADR is the third.

**The analyzer breadth that decides how many diagnostics this gate reads is not decided
here.** This ADR fixes what happens to a warning, not which rules produce one. That is a
separate axis with a separate cost profile and it is decided separately, alongside this one.
The two are worth keeping apart because they fail differently: this parameter is measured at
zero cost today and changes nothing until a warning appears, while a breadth change can turn
existing, unchanged code red on the day it lands.

### Consequences

* Good, because the default for an unnamed warning flips from pass to fail, so the project
  stops needing an ADR per diagnostic to make a real defect stop a build. The two diagnostics
  ADR-0065 promoted and the one ADR-0044 promoted remain correct and remain where they are;
  they are now the floor rather than the whole set.
* Good, because it costs nothing today, measured, so nobody has to weigh a strict gate against
  a backlog of existing warnings. That is a property of the date rather than of the decision,
  and it is the reason the decision is taken now.
* Good, because local and CI cannot diverge on this rule, which is the failure mode ADR-0065
  measured and kept both of its paths to avoid (`0065:102`).
* Good, because a deprecation introduced by an OpenIddict or .NET bump becomes visible at the
  moment it is introduced rather than at the release that deletes it (`0021:44`, `0030:42`).
* Bad, because every future warning, including one from a rule the project did not choose and
  a compiler version it did not pick, is now a stop-work item. Accepted: parameter D is the
  route, and it is deliberately narrow enough to be visible in a diff.
* Bad, because an upgrade in progress is noisier under this rule than under a warning-only
  build, so an SDK or package bump may need a per-project `<NoWarn>` that is then owed a
  removal. That is the pressure ADR-0065 predicted (`0065:109`), arriving on schedule, and the
  answer is a comment naming the trigger rather than a permanent line.
* Bad, because the carve-out in parameter C means a restore-time vulnerability advisory does
  **not** fail the build here, and a reader could take a green build as a statement about
  dependency vulnerabilities. It is not one. ADR-0092 stage 2 is where that gate lives, and
  until it is wired at M1 the NuGet audit warning is a warning and nothing blocks on it.
* Neutral, because nothing about the analyzer rule set, the severity of any named diagnostic,
  or any existing gate changes. This ADR moves one property.

### Confirmation

* **`scripts/test-warnings-as-errors.sh` is the mechanism, and it is green.** Written and run
  on 2026-08-03 on SDK 10.0.301, reporting `warnings-as-errors self-test OK` at exit 0. It runs
  in CI as its own job, `Warnings-as-errors gate self-test`, rather than as a step in
  `Solution build`, because a red there would mean the gate stopped biting and not that the code
  is wrong. Part 2 is this ADR's parameter A, a `CS0219` on an unused local measured to be a
  warning at exit 0 without the property and an error at exit 1 with it; Part 5 is parameter C.
* **The gate was broken on purpose before it was believed**, on 2026-08-03 on SDK 10.0.301, five
  times and each reverted, because a gate never broken is not known to bite. Deleting
  `TreatWarningsAsErrors` moves 7 assertions, across Parts 2, 3, 4 and 5, cascading, which is
  correct rather than noisy: it is the property that turns every other axis from a warning into a
  failure, and Part 2 fires first and names it. Dropping the four `NU19xx` codes from the
  parameter C carve-out moves 4, in Part 5 alone. The other three breaks belong to ADR-0094 and
  to ADR-0092 section 1, and `scripts/README.md` records all five in one table.
* **`Solution build` cannot stand in for any of this, which is why the self-test is a gate of its
  own.** Measured the same day and SDK, the solution builds `0 Warning(s)` with the four
  properties and `0 Warning(s)` without them, so the ordinary build is green whether or not the
  gate is armed and says nothing either way.
* **The parameter C exemption is asserted on the evaluated property, not end to end.** Proving
  the demotion against a live advisory needs a network restore and advisory data that can
  change under the test. What the two tables above record is a statement about 2026-08-03 and
  those two packages; what the self-test can hold permanently is that the four codes are in
  `WarningsNotAsErrors` and that the `$(...)` append was not replaced by a bare assignment.
* **Standing obligation, with a trigger rather than a date: re-read whether this gate is
  liveable as `src/` fills.** ADR-0065 states the distinction this rests on, that a rule
  proven against a fixture is "known to work and **not** known to be liveable" (`0065:109`),
  and the zero measured above is a fact about one project of three properties. The point to
  re-read it is when real code first produces a warning this ADR turns into a build break, and
  the question then is whether parameter D was used or whether parameter C was widened.
* **Revisit parameter C when ADR-0092 stage 2's dependency scan lands at M1.** The carve-out
  is justified by that gate existing to take the question. If M1 arrives without it, the
  carve-out is a gap rather than a division of labour, and the reasoning has to be re-taken.

## Pros and Cons of the Options

### A. Repo-wide in `Directory.Build.props`, with the restore codes exempt (chosen)

* Good, because the contributor's build and CI enforce the same rule, so a red in CI is never
  the first time anyone sees the diagnostic.
* Good, because the measured cost of adopting it is zero, so it is adopted on evidence rather
  than on the argument that it will be cheaper now than later.
* Bad, because it is the strictest of the three and every exemption from here is a per-project
  edit somebody has to justify in a diff.

### B. CI-only `-warnaserror`

* Good, because a contributor's inner loop stays quiet, and a warning does not stop local
  iteration on work in progress.
* Bad, and decisively, because that quiet is the defect. ADR-0065 measured this exact shape
  and recorded that the cost lands on contributors, "whose local build goes quiet while CI
  does not" (`0065:102`). Deciding it the other way here would contradict a measurement this
  repository already holds.
* Bad, because the flag lives in a workflow rather than in the build, so an adopter who forks
  and builds the solution gets a different build from the one CI gates.

### C. Leave it unset

* Good, because it preserves the status quo, which is defensible: two ADRs have promoted three
  diagnostics deliberately and each promotion is recorded with its reason.
* Bad, because it does not scale past a handful of rules. Every diagnostic worth failing on
  needs its own decision, and the ones nobody thought to name stay survivable by default,
  which is the whole class this ADR is about.
* Bad, because the two reasons `Directory.Build.props:95-100` gives for withholding are both
  spent, so leaving it unset would now be a decision with no stated ground rather than a
  deferral with one.

## More Information

* **No `stack-record: true` marker, and no row in ADR-0061, deliberately.** This ADR
  introduces no technology: it sets an MSBuild property on an SDK ADR-0030 already chose, and
  `0061:68` already carries the "Code style and conventions" row naming `.editorconfig` plus
  .NET analyzers and `dotnet format`, owned by ADR-0065. This is said out loud because the
  absent marker would otherwise read as an oversight, and because adding the marker without
  adding a table row fails guardrail Check 4 in the other direction. `docs/adr/CLAUDE.md`
  records the wider trap: that check compares two lists derived from this repository's own
  markup, so it is blind to a shared omission and its green is not evidence of coverage.
* Related decisions: ADR-0065 (the enforcement posture, the two-path measurement this reuses,
  and the "write only what deviates" rule), ADR-0044 (parameter A, whose `RS0017` already rides
  `WarningsAsErrors` in the same file), ADR-0092 (the SDK security-analysis axis and, at stage
  2, the dependency-vulnerability gate parameter C defers to), ADR-0021 and ADR-0030 (the
  upgrade cadences whose deprecations this converts into build breaks), ADR-0026 (the
  dependency policy, whose gate reads licences and not vulnerabilities, which is why parameter
  C does not route there), and ADR-0060 (the CI composition the gate runs inside).
* **The `docs/BUILD-PLAN.md:57` row is closed by this ADR**, per that file's own maintenance
  rule that a row is deleted when its owner records the outcome and is never marked done. The
  deletion lands with the change that sets the property, not with this one, because until the
  property exists the outcome is decided and not yet recorded.
* **The withholding comment at `Directory.Build.props:95-100` is rewritten rather than
  deleted** when the property lands. It is the record of why the property was absent while the
  repository had no code to measure against, and that record is worth more than the two lines
  it costs.
* Authored fresh for this repository, not imported from the design corpus. The corpus asks for
  the property in its foundations task 1.4, which is what `Directory.Build.props:95` names and
  what made this an owed decision rather than a new idea; the carve-out, the exemption
  mechanism, and the measurements are this repository's.
