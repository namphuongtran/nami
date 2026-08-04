---
paths:
  - ".github/workflows/*.yml"
  - "global.json"
  - "*.props"
  - "Directory.Packages.props"
  - ".editorconfig"
  - ".markdownlint-cli2.jsonc"
  - "Nami.Identity.slnx"
  - "src/**/*.csproj"
---

# Build and CI configuration

Traps in the files that decide whether a gate bites. Each was measured, and each is a way a
control reads as enforced while enforcing nothing. The root
[`../../CLAUDE.md`](../../CLAUDE.md) carries the rules that apply everywhere. This file loads
only when one of the files above is in play.

## A severity is matched against the file a diagnostic is reported in

Not against the project it belongs to. `RS0017` reports a stale entry inside
`PublicAPI.Unshipped.txt`, so its location is a `.txt` file and no `.editorconfig` section
reaches it. Measured 2026-08-02, four placements were tried, and four left it at its default of
warning:

- `[*.cs]`;
- a section naming both API files explicitly;
- `[*]`;
- a root `.globalconfig` added through an MSBuild item.

Only a `.globalconfig` inside the project's own directory worked, and that is per-project.

It is therefore `<WarningsAsErrors>` in `Directory.Build.props`. **Do not tidy it into
`.editorconfig` for symmetry; the symmetry is the bug.** What it uniquely covers is a public
member deleted from the code with its API-file lines left behind. That is ADR-0044 parameter B's
MAJOR-breaking direction, and it built green at exit 0 while `RS0017` was a warning.

**Before trusting any severity line, break the rule and read the exit code.** The placement that
looks right is the one that fails quietly.

## `.editorconfig` and `Directory.Build.props` are one mechanism, not two

Error severity in `.editorconfig` fails nothing on its own for `IDExxxx` rules.
`EnforceCodeStyleInBuild` in `Directory.Build.props` is what turns it into a build failure, and
that was measured. Editing either without the other is how the ruleset goes quiet while still
reading as enforced, and `scripts/test-editorconfig.sh` exists to catch it.

`RS00xx` rules are the opposite shape. They are ordinary analyzer diagnostics and need no such
property. Measured with `-p:EnforceCodeStyleInBuild=false` against an undeclared public member,
`dotnet build` still exited 1 on `RS0016`. Do not assume either shape for the other.

## `global.json` is a pin that can be inert, and the inert shape is the one the corpus writes

Measured 2026-08-02 on SDK 10.0.301:

| `version` | `rollForward` | Result |
|---|---|---|
| `10.0.999` | `disable` | exit 155, a real pin |
| `9.0.x` | `disable` | **exit 0** on a machine with no 9.0 SDK |
| `10.0.100` | `latestFeature` | resolves 10.0.301 |

A `version` string the SDK cannot parse makes the whole `sdk` block inert. The design corpus
writes `"10.0.x"`, so copying it would produce a pin that constrains nothing. This repository
writes `10.0.100` with `latestFeature`, a parseable floor rather than a wildcard. A wildcard is
not available, because `rollForward` is what expresses the range.

## `AnalysisLevelSecurity` takes a compound value, and the bare mode word is inert

Same shape as the `global.json` row above: a value that parses as nothing and configures nothing.
Measured 2026-08-03 on SDK 10.0.301 against a project calling `MD5.HashData`, with the properties
set in a project file rather than on the command line:

| `AnalysisLevelSecurity` | `CA5351` | Exit |
|---|---|---|
| `latest-all` | `error CA5351` | 1 |
| `all` | never fires | **0** |

Both rows carry `TreatWarningsAsErrors=true`, so the difference is the spelling and nothing else.
The form the SDK wants is compound, `<level>-<mode>`, which its own comment in
`Microsoft.CodeAnalysis.NetAnalyzers.targets` states and ADR-0092 quotes.

**State the mechanism when you touch this, because the symptom invites the wrong fix.** The bare
`all` does not fail to parse. It parses as the **level**, since the prefix is assigned only when
the value contains a `-`. With no prefix the mode falls through to the literal `Default`. The SDK
then looks for `analysislevelsecurity_all_default.globalconfig`, which does not ship, every
shipped file being named for a numeric level. The include is guarded by `Exists()`, so MSBuild
rejects nothing, logs nothing, and applies no configuration. That is why the misspelling is silent
rather than loud. [`../../Directory.Build.props`](../../Directory.Build.props) carries the full
trace, property by property with the line numbers it was read at. Read it there rather than
re-deriving it.

`CodeAnalysisTreatWarningsAsErrors` is the neighbouring property, and it is deliberately **not**
set. It is what selects the `_warnaserror` variant of the shipped globalconfig, and it has no SDK
default. Measured 2026-08-03 on SDK 10.0.301 against a project outside this repository, it
evaluates to the empty string rather than to `false`. Both routes were measured the same day
reaching exit 1 on the same `CA5351` violation. So setting it as well would be two mechanisms for
one outcome, and `TreatWarningsAsErrors` covers every axis rather than the analyzer one.

`scripts/test-warnings-as-errors.sh` Part 3 is the standing check. It is the only thing in the tree
that notices the bare `all` coming back **behaviourally**, by a rule ceasing to be reported. Since
2026-08-03 that script's Part 6 also goes red on it. Part 6 asserts the evaluated spelling on each
real project under `src/` and `tests/`, which is the per-project override Part 3's probe cannot
see. Nothing outside that script notices either way.

## Two `-p:` flags in one shell argument silence every analyzer at exit 0

Measured 2026-08-03 on SDK 10.0.301, and it cost real time before it was understood. zsh does not
word-split an unquoted expansion, so `dotnet build $flags` with both flags in one variable arrives
as a **single** argument. MSBuild then reads `AnalysisMode` as
`Recommended -p:AnalysisLevelSecurity=latest-all` and `AnalysisLevelSecurity` as empty, verified
with `-getProperty`. A garbage `AnalysisMode` names a globalconfig that does not exist. So **both**
axes configure nothing, and every analyzer diagnostic disappears, including ones either property
reports on its own. Exit 0 with no diagnostic, which is indistinguishable from a gate that is
genuinely off. **Pass each `-p:` as its own argument.**

Related, and stated so nobody "fixes" it: `-p:A=1;B=2` **does** work. MSBuild splits a `-p:`
argument on `;` into `name=value` pairs, so that is two valid pairs. The same rule is why
`-p:WarningsNotAsErrors=NU1901;NU1902` fails with `MSB1006`, the bare `NU1902` having no `=`. One
rule, two consequences. The second is why a multi-valued property has to be set in a project file
rather than on the command line.

The third way to read a false green by hand is an incremental build after a property-only change.
Nothing in the compilation inputs moved, so MSBuild skips the compiler and reports the previous
run's silence. Add `-t:Rebuild` when comparing property values.

## The markdownlint version is coupled to the action, not chosen

`npx --yes markdownlint-cli2@0.23.1` must match the version bundled by the version-pinned action
in `ci.yml` (ADR-0086 parameter B), so bump both or neither. That coupling is the half of ADR-0086
which survived its parameter A reversal on 2026-08-02. It is also the half that caught the only
real drift this repository has had.

`.markdownlint-cli2.jsonc` sets `gitignore: true` so the glob reads the file set CI reads. Without
it the same command also walks the git-ignored draft areas and reports errors CI can never see.

## Every `uses:` in `ci.yml` is a full version tag

`@v7.0.1`, never `@v7`, never a branch, never a commit SHA. `check-adrs.sh` Check 8c fails the
build on any other form. The floating-major form is what silently changed the bundled linter under
this workflow once already. The residual risk of a movable release tag is stated in ADR-0086's
Consequences, which also names M1 as the point to re-open it.

## Central Package Management: what is written is a floor

`Version="5.6.0"` in `Directory.Packages.props` restores as the constraint `>= 5.6.0` and resolves
to `5.6.0` only because NuGet takes the lowest match, read from `obj/project.assets.json`. Exact
pinning is `[5.6.0]`, and since 2026-08-02 ADR-0021 parameter A requires that form of OpenIddict
and its sub-packages and of nothing else. So **a file mixing the two forms is correct and must not
be tidied into one**.

A bracket bounds the direct constraint and nothing beneath it, so it is not a reproducible
restore: transitive versions still move. No decision covers that yet, and the same parameter names
it as owed before M1.

Never put `Version=` on a `PackageReference` (`NU1008`), and never omit the row (`NU1010`). Both
exit 1, and both were measured.
