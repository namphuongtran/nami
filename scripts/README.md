# scripts

## check-adrs.sh

Neutral ADR/docs hygiene checks, run in CI (`.github/workflows/ci.yml`) and locally:

- template placeholders (the curly-brace `Product` / `Company` / `domain` tokens) must not appear in tracked markdown;
- every `ADR-NNNN` reference in **any** tracked markdown resolves to a `docs/adr/NNNN-*.md` file, not only those written inside `docs/adr/`, because the architecture and design layers cite far more ADR numbers than the ADRs themselves do;
- the ADR index in `docs/adr/README.md` matches the files, and each ADR's frontmatter `status:` matches its index row;
- every ADR marked `stack-record: true` in its frontmatter appears in the ADR-0061 stack-of-record table, and every ADR cited in that table carries the marker (bidirectional);
- no em dash appears in tracked markdown (project style rule): use a comma, colon, or parentheses. The check builds the pattern from the codepoint, so this script stays pure ASCII and cannot fail against itself;
- no design-corpus test identifier appears in tracked markdown: the `9.T`, `8.K` and `25.T` families point into a numbered test register this repository does not have, so an obligation is stated by what it asserts and listed in `docs/design/20-testing.md` instead. The families are named by prefix here on purpose, because writing a whole identifier would trip this very check; `docs/adr/README.md` carries the full convention and the reason it is enforced;
- every ADR has a row in the architecture layer's reverse index, `docs/architecture/18-decisions-index.md`, and every row there resolves to a file (bidirectional). This is a second index, and the first one passing says nothing about it: nine ADRs had drifted out of this one while every other check was green. Membership only, never the "Views that cite it" column, which is regenerated from the views themselves;
- GitHub Actions workflow hygiene, **three** rules. Two were added 2026-08-02 as the no-new-dependency half of the workflow-analysis gap ADR-0092 names: no `${{ ... }}` expression appears inside any `run:` script, and no workflow uses a `pull_request_target:` or `workflow_run:` trigger. The third, 8c, landed the same day with ADR-0086 parameter A's reversal and is what makes that reversal a decision rather than a loosening: every `uses:` must be a full version tag, `@vX.Y.Z`, so a floating major (`@v7`), a branch (`@main`), a partial version (`@v7.0`) and a commit SHA are all rejected. The floating major is the form that actually moved this repository's linter under an unchanged workflow file, and it differs from the sanctioned form by four characters in a diff. Local (`./...`) and container (`docker://...`) references are out of scope, image digests being ADR-0051 section D. **Read the scope before reading the green.** Interpolating into a shell is the injection vector and passing the value through `env:` is the standard mitigation, so the first rule enforces the mitigation rather than judging whether a value is trusted, which is the part that would need a real analyser. What neither rule sees: interpolation into an action's `with:` inputs, the scope of a `permissions:` block, composite actions and reusable workflows in other repositories, and what a pinned action does once it runs (ADR-0086 constrains *which* action code runs, and since its parameter A was reversed on 2026-08-02 it constrains it by a movable tag, so even that is weaker than it reads; nothing here governs an action's behaviour).

Run locally:

```bash
bash scripts/check-adrs.sh
```

The checks that read tracked files from `git ls-files` (markdown for 1, 2, 5, 6; workflows
for 8) do not see a file that has never been `git add`-ed. The script prints a
`coverage warning:` listing any untracked markdown, and separately any untracked workflow,
**above** its verdict in both the passing and the failing case, so a green is never mistaken
for coverage it did not have. It warns rather than fails, because an untracked
work-in-progress file is legitimate mid-edit; staging is still what makes the verdict cover
it. CI cannot reach this case, its checkout being tracked-only.

## test-check-adrs.sh

A self-test for `check-adrs.sh` Check 8, run in CI alongside the guardrail itself:

```bash
bash scripts/test-check-adrs.sh
```

It creates a throwaway `git worktree` at `HEAD`, copies the **working-tree** guardrail and
the **working-tree** workflows into it, then plants two workflows: one carrying three
hygiene violations and four look-alikes, and one carrying four pin violations and four
look-alikes. It asserts that exactly the three and exactly the four are reported. The worktree is removed on every exit path, so neither the
real working tree nor the real index is ever written to.

It exists because Check 8 matches with `awk`, and the awk on the CI runner is a different
implementation from the one the check was authored against. A green guardrail on a clean
tree proves only that the awk parses, since a clean tree has nothing to match; this supplies
the bug so the matching is proven on the awk that runs it, on every run rather than once.

Three properties are load-bearing and easy to lose in an edit. **It copies the working-tree
guardrail into the worktree**, because a worktree at `HEAD` otherwise tests the committed
script rather than the one being edited: the first version of this file omitted that copy,
and deleting Check 8's block-scalar detection outright still produced a green. And **the two
finding-count assertions are what prove the look-alikes did not trip**; the per-line checks
are diagnostics, and the negative ones among them pass vacuously if a line number drifts,
which also happened on the first run.

The third was added on 2026-08-02 and is the same trap one layer out: **it copies the
working-tree workflows in as well**. Check 8c landed in the same commit that fixed every
`uses:` it rejects, so without that copy the worktree carried `HEAD`'s unfixed workflow,
the new rule found seven real violations in it, and two assertions failed for a reason
that had nothing to do with what they were testing. The generalisation is worth more than
the fix: **a self-test's subject is the script and its input**, and a check cannot
otherwise be introduced in the same change as the fix it demands.

## check-decisions-index.py

Checks the architecture layer's reverse index,
[`docs/architecture/18-decisions-index.md`](../docs/architecture/18-decisions-index.md),
against the files it is derived from. `check-adrs.sh` Check 7 verifies only that every ADR
has a **row** there; this verifies what the row says, which is the part a second index gets
wrong. It compares the "Views that cite it" column against the numbered views, and the
`Decision` column against each ADR's own H1 title, since that column quotes the title rather
than paraphrasing it.

```bash
python3 scripts/check-decisions-index.py                # compare; exits 1 on drift
python3 scripts/check-decisions-index.py --print-table   # emit the correct rows
```

It never writes to the index. Python 3 with no third-party packages, kept separate from
`check-adrs.sh` rather than folded into it: that script is deliberately portable bash, and
comparing two derived tables in bash 3.2 would be a weaker copy of a rule that can be
written once here.

**It is a CI gate as of 2026-08-02**, a second step in the `adr-guardrail` job, and it runs
in the pre-commit hook too. It was proven against the defect it exists for before being
wired, which is this folder's standing rule: blanking one view from a row's "Views that cite
it" cell left `check-adrs.sh` printing `ADR/docs guardrail OK.` while this exited 1. Until
then it had run only when someone remembered to, and the gap it covers is exactly the kind
nothing else notices, since a wrong cell in a table that exists reads as maintained.

The hook **skips** this check when `python3` is absent rather than failing, because the hook
is opt-in convenience and CI is the authority. A hook that refuses to run on a machine
missing an interpreter is a hook people turn off, which costs more than the check earns.

## test-editorconfig.sh

A self-test for the C# ruleset in [`../.editorconfig`](../.editorconfig) and for
[`../Directory.Build.props`](../Directory.Build.props) (ADR-0065), run in CI as its own job
because it needs a .NET SDK:

```bash
bash scripts/test-editorconfig.sh
```

It writes a throwaway project to `.editorconfig-probe/`, then asserts against **both**
enforcement paths: a compliant fixture must be build-clean and format-clean, and a violating
fixture must fail all four naming rules and the formatting rule under `dotnet build` and
again under `dotnet format --verify-no-changes`. The directory is removed on every exit path
and is git-ignored as a backstop. It **skips with exit 0 when `dotnet` is absent**, and says
out loud that a skip is not a pass.

**Both paths are asserted because they are not the same gate under two names.** ADR-0065
names `dotnet format --verify-no-changes` as what CI enforces, and the format path does not
need `EnforceCodeStyleInBuild`, reports whitespace as `WHITESPACE` rather than `IDE0055`, and
exits 2 rather than 1. Measured consequence: removing that property, or removing
`dotnet_diagnostic.IDE1006.severity`, silences `dotnet build` entirely while the format path
keeps reporting every naming violation. A gate built on the format path alone stays green
through both breaks, and every contributor's local build goes quiet.

The probe lives **inside the repository on purpose**. That is the only way it inherits the
real `.editorconfig` (which sets `root = true` at that level) and the real
`Directory.Build.props` (MSBuild walks up from the project directory) rather than a copy
that could drift from what is being edited. It is the same trap `test-check-adrs.sh` fell
into from the other side, and here the subject is the working tree by construction.

It exists because the ruleset landed on 2026-08-02, ahead of any C# in this repository, so
nothing else exercises it. Three ways it can be silently inert were found by measuring
rather than by reading, and each is a live assertion:

- A per-rule `dotnet_naming_rule.<name>.severity = error` does not reach the build. Only
  `dotnet_diagnostic.IDE1006.severity` does. Removing that one line reports 5 failures here,
  all of them on the build path.
- Severity of any kind fails nothing without `EnforceCodeStyleInBuild`, which is an MSBuild
  property rather than an editorconfig key. Removing it reports 8, again all build-path.
- The const and static carve-outs are what hold ADR-0065's rule to private *instance*
  fields. Delete either and the general rule takes over that kind of member, enforcing a
  convention no decision states. Deleting the general rule reports 3, spanning both paths.

Counts, not per-line greps, are what the assertions turn on, for the reason
[`CLAUDE.md`](CLAUDE.md) records: a negative assertion written per-line passes vacuously.

One claim this file used to carry was wrong and was caught by breaking the subject rather
than by review: naming-rule **declaration order is not load-bearing**. Moving the general
private-field rule above the other two left every field matched by the same rule as before,
so the more specific symbol specification wins regardless of position. The test correctly
stays green on that reorder, because there is nothing there to catch.

A second lesson came from a break that did not break. One of the experiments above was run
with an edit that threw before writing, so the unmodified ruleset was what got tested, and it
passed. **A failed break reports the same green as a healthy subject.** Confirm the subject
actually changed before reading anything into a green, which is the same rule the sibling
self-test learned from the other direction when its worktree tested the committed script
instead of the edited one.

## test-public-api-gate.sh

A self-test for the public-API lock (ADR-0044 parameter A) and for Central Package
Management (ADR-0026 section C), run in CI as its own job because it needs a .NET SDK:

```bash
bash scripts/test-public-api-gate.sh
```

Like the script above it writes a throwaway project, here to `.publicapi-probe/`, inside the
repository so it inherits the real [`../.editorconfig`](../.editorconfig),
[`../Directory.Build.props`](../Directory.Build.props) and
[`../Directory.Packages.props`](../Directory.Packages.props) rather than copies. The
directory is removed on every exit path and is git-ignored as a backstop, and it **skips
with exit 0 when `dotnet` is absent**, saying out loud that a skip is not a pass.

It is a different job from `Solution build` rather than a step in it, because it is not a
build of this repository: a red here means the gate stopped biting, not that the code is
wrong, and the two should not arrive under one name.

**It exists because one third of the gate was inert on the day it landed.** `RS0017` sat at
its default severity of warning, so a public member deleted from the code with its lines
left in the API file produced `2 Warning(s)`, `Build succeeded`, exit 0. That is the
MAJOR-breaking direction of ADR-0044 parameter B passing a gate that read as configured, and
nothing in the tree would have noticed it returning. Part 3 is that case.

Six breaks are asserted: a public member absent from the API file (`RS0016`, on the build
path and again under `dotnet format`), a stale API entry (`RS0017`, build path only), a
missing `#nullable enable` header (`RS0037`), a `Version` on a `PackageReference` (`NU1008`),
a package with no `PackageVersion` row (`NU1010`), and a pack without `PrivateAssets="all"`
declaring the analyzer as a real dependency. Part 1 is the control: a compliant fixture must
build clean, and it also proves CPM is supplying the version, since the probe's reference
carries none.

Proven by breaking the subject, five times, each reverted:

- Removing `dotnet_diagnostic.RS0016.severity` moves 2 assertions, all in Part 2.
- Removing `RS0017` from `WarningsAsErrors` moves 2, all in Part 3.
- Removing `dotnet_diagnostic.RS0037.severity` moves 2, all in Part 4.
- `ManagePackageVersionsCentrally = false` moves 8, across every part.
- Deleting the `PackageVersion` row moves 6, across every part.

The first three isolating cleanly is what makes a red readable: the failing part names the
file to open. The last two cascading is correct rather than noisy, since nothing can restore,
and Part 1's control fires first and says so.

**Part 4 needed a second attempt to be worth its assertion, and the reason generalises.** Its
first fixture was the compliant API file with the header deleted, which also fires `RS0016`,
so removing the `RS0037` severity left the build failing anyway and only the count assertion
noticed. The entries are now written unannotated so `RS0037` is the only diagnostic present.
**An exit code is a weak assertion when several rules watch one fixture**, and Parts 3 and 4
each carry an explicit check that the *other* diagnostic did not fire.

## test-warnings-as-errors.sh

A self-test for the warning-escalation gate, run in CI as its own job because it needs a
.NET SDK:

```bash
bash scripts/test-warnings-as-errors.sh
```

Four properties sit in one `PropertyGroup` in
[`../Directory.Build.props`](../Directory.Build.props) and each can be silenced alone, so the
parts are grouped by the failure rather than by the decision:

| Part | Subject | Owner | Fixture |
|---|---|---|---|
| 1 | the control: a compliant fixture must build clean | none | a plain `public sealed class` |
| 2 | `TreatWarningsAsErrors` | ADR-0093 parameter A | `CS0219`, an unused local |
| 3 | `AnalysisLevelSecurity` | ADR-0092 section 1 | `CA5392`, a `DllImport` with no `DefaultDllImportSearchPaths` |
| 4 | `AnalysisMode` | ADR-0094 | `CA1050`, a type outside any namespace |
| 5 | `WarningsNotAsErrors` | ADR-0093 parameter C | the evaluated property |
| 6 | all four, on the **real** projects | all three ADRs | the evaluated properties, no fixture |

Parts 1 to 5 assert against the throwaway probe, which is what lets them be behavioural. **Part
6 exists because that is also their blind spot.** A
`<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` in a real `.csproj`, or a
`src/Directory.Build.props` that does not `<Import>` the root one, disarms the gate for the only
code in the repository, and the probe cannot see either: MSBuild walks **up** from a project
directory, so a probe at the repository root inherits the root `Directory.Build.props` and no
edit under `src/` or `tests/` can reach it. Part 6 therefore evaluates all four properties on
every `.csproj` it discovers under `src/` and `tests/`, through each project's own import chain,
and asserts the discovered count first so an empty discovery cannot report a pass having checked
nothing. Measured 2026-08-03 on SDK 10.0.301 and reverted: with that override planted in
`src/Nami.Identity.Abstractions`, Part 6 reported it and `Solution build`, `dotnet test` and
`dotnet format --verify-no-changes` were all still green at exit 0.

Like the two scripts above it writes a throwaway project inside the repository, here to
`.warnaserror-probe/`, because MSBuild walks **up** from a project directory and that is the
only way the probe inherits the real `Directory.Build.props` rather than a copy that could
drift from what is being edited. The directory is removed on every exit path and is
git-ignored as a backstop, and it **skips with exit 0 when `dotnet` is absent**, saying out
loud that a skip is not a pass.

It is a different job from `Solution build` rather than a step in it, for the same reason the
public-API job gives: a red here means the gate stopped biting, not that the code is wrong.

**It exists because the ordinary build cannot see this gate at all.** Measured 2026-08-03 on
SDK 10.0.301, the solution builds `0 Warning(s)` with the four properties and `0 Warning(s)`
without them. There is no backlog for the switch to turn into a wall, which is what made
landing all four at once affordable, and it is also why `Solution build` is green either way
and says nothing about whether the gate is armed. Until this script existed, nothing in the
tree would have noticed any of the four being deleted.

Each fixture was measured to raise its diagnostic **alone**, which is the lesson
`test-public-api-gate.sh` Part 4 paid for, and each part asserts that isolation as well as the
exit code. Part 3 needed a second fixture for exactly that reason: `CA5351` on `MD5` is a
security-sounding rule that also sits in the `Recommended` tier, so the part passed both with
the inert bare `all` and with the property deleted outright while reading as armed. `CA5392`
is outside the overlap, and the script's comment carries the counted evidence.

Proven by breaking the subject, five times, each reverted. Taken first against Parts 1 to 5 and
**re-run in full on 2026-08-03 on SDK 10.0.301 when Part 6 was added**, because a count is a
measurement of a specific script and adding a part changes the subject rather than the answer.
The figure in brackets is what the same break moved before Part 6 existed:

- Deleting `TreatWarningsAsErrors` moves 9 assertions (was 7), across Parts 2, 3, 4, 5 and 6,
  cascading.
- Setting `AnalysisLevelSecurity` to the inert bare `all` moves 4 (was 2), Parts 3 and 6.
- Deleting `AnalysisLevelSecurity` outright moves 4 (was 2), Parts 3 and 6.
- Deleting `AnalysisMode` moves 4 (was 2), Parts 4 and 6.
- Dropping the four `NU19xx` codes from the carve-out moves 12 (was 4), Parts 5 and 6, four
  codes on the probe and four on each of the two real projects.

The other three properties isolating cleanly is what makes a red readable: the failing part
names the property to open, and where Part 6 also fires it names the same property and the
project it was overridden in. `TreatWarningsAsErrors` cascading is correct rather than noisy,
since it is the property that turns every other axis from a warning into a failure, and Part 2
fires first and says so. **The two `AnalysisLevelSecurity` breaks moving the same 4 assertions
is the finding worth keeping**: the bare `all` is indistinguishable from the property being
absent, because it
parses as a level rather than as a level-mode pair and names a globalconfig the SDK never
shipped, and the include is guarded by `Exists()` so nothing is logged.
`Directory.Build.props` carries that trace property by property, and
[`../.claude/rules/build-and-ci.md`](../.claude/rules/build-and-ci.md) carries it as a trap.

**What Part 5 does not cover, stated because its green is easy to over-read: it asserts the
value of `WarningsNotAsErrors`, not that NuGet honours it at restore.** That was measured
once, on 2026-08-03, and it lives in ADR-0093 with the two package fixtures it was taken
against. Asserting it on every run would need a network restore and a live advisory that can
change under the test, so the property is what this file checks and the behaviour is what the
ADR records.

## Pre-commit hook (opt-in, maintainers)

Enable once per clone:

```bash
git config core.hooksPath scripts/hooks
```

The hook runs `check-adrs.sh`. In addition, if you create a local, git-ignored
`scripts/.local/name-denylist` (one term per line; `#` comments and blank lines
ignored), the hook blocks a commit that introduces any of those terms in staged
markdown. That file lives under the git-ignored `scripts/.local/` directory and
is never committed, so nothing sensitive is published.

Terms are matched case-insensitively as whole words; use plain names and avoid
regular-expression metacharacters.

An optional companion file, `scripts/.local/name-allowlist`, exempts exact
identifiers that legitimately contain a denied term. The motivating case is an OSS
package the project actually depends on: a dependency record that hides the package
identifier is factually wrong and cannot drive the license-scan gate of
[ADR-0026](../docs/adr/0026-dependency-license-policy.md), while product comparison
and rejected commercial packages must still be generalized. For each denied term the
hook blanks every allowlisted identifier out of the matched lines and re-tests, so a
line that matched only because of an allowed identifier passes, and a line that also
carries a genuine mention still blocks. Allowlist entries are matched
**case-sensitively**: write package identifiers in their canonical casing, which is
also what the license scan needs.
