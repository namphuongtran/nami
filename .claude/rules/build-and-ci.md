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

Traps in the files that decide whether a gate bites. Each was measured, and each is a way
a control reads as enforced while enforcing nothing. The root
[`../../CLAUDE.md`](../../CLAUDE.md) carries the rules that apply everywhere; this file
loads only when one of the files above is in play.

## A severity is matched against the file a diagnostic is REPORTED IN

Not against the project it belongs to. `RS0017` reports a stale entry inside
`PublicAPI.Unshipped.txt`, so its location is a `.txt` file and no `.editorconfig`
section reaches it. Measured 2026-08-02, four placements were tried and four left it at
its default of warning: `[*.cs]`, a section naming both API files explicitly, `[*]`, and
a root `.globalconfig` added through an MSBuild item. Only a `.globalconfig` inside the
project's own directory worked, and that is per-project.

It is therefore `<WarningsAsErrors>` in `Directory.Build.props`. **Do not tidy it into
`.editorconfig` for symmetry; the symmetry is the bug.** What it uniquely covers is a
public member deleted from the code with its API-file lines left behind, which is
ADR-0044 parameter B's MAJOR-breaking direction, and which built green at exit 0 while
`RS0017` was a warning.

**Before trusting any severity line, break the rule and read the exit code.** The
placement that looks right is the one that fails quietly.

## `.editorconfig` and `Directory.Build.props` are one mechanism, not two

Error severity in `.editorconfig` fails nothing on its own for `IDExxxx` rules;
`EnforceCodeStyleInBuild` in `Directory.Build.props` is what turns it into a build
failure, measured. Editing either without the other is how the ruleset goes quiet while
still reading as enforced, and `scripts/test-editorconfig.sh` exists to catch it.

`RS00xx` rules are the opposite shape: they are ordinary analyzer diagnostics and need no
such property. Measured with `-p:EnforceCodeStyleInBuild=false` against an undeclared
public member, `dotnet build` still exited 1 on `RS0016`. Do not assume either shape for
the other.

## `global.json` is a pin that can be inert, and the inert shape is the one the corpus writes

Measured 2026-08-02 on SDK 10.0.301:

| `version` | `rollForward` | Result |
|---|---|---|
| `10.0.999` | `disable` | exit 155, a real pin |
| `9.0.x` | `disable` | **exit 0** on a machine with no 9.0 SDK |
| `10.0.100` | `latestFeature` | resolves 10.0.301 |

A `version` string the SDK cannot parse makes the whole `sdk` block inert. The design
corpus writes `"10.0.x"`, so copying it would produce a pin that constrains nothing. This
repository writes `10.0.100` with `latestFeature`, a parseable floor rather than a
wildcard; a wildcard is not available, because `rollForward` is what expresses the range.

## The markdownlint version is coupled to the action, not chosen

`npx --yes markdownlint-cli2@0.23.1` must match the version bundled by the version-pinned
action in `ci.yml` (ADR-0086 parameter B), so bump both or neither. That coupling is the
half of ADR-0086 which survived its parameter A reversal on 2026-08-02, and it is the
half that caught the only real drift this repository has had.

`.markdownlint-cli2.jsonc` sets `gitignore: true` so the glob reads the file set CI reads.
Without it the same command also walks the git-ignored draft areas and reports errors CI
can never see.

## Every `uses:` in `ci.yml` is a full version tag

`@v7.0.1`, never `@v7`, never a branch, never a commit SHA. `check-adrs.sh` Check 8c
fails the build on any other form. The floating-major form is what silently changed the
bundled linter under this workflow once already. The residual risk of a movable release
tag is stated in ADR-0086's Consequences, which also names M1 as the point to re-open it.

## Central Package Management: what is written is a floor

`Version="5.6.0"` in `Directory.Packages.props` restores as the constraint `>= 5.6.0` and
resolves to `5.6.0` only because NuGet takes the lowest match, read from
`obj/project.assets.json`. Exact pinning is `[5.6.0]`, and since 2026-08-02 ADR-0021
parameter A requires that form of OpenIddict and its sub-packages and of nothing else, so
**a file mixing the two forms is correct and must not be tidied into one**.

A bracket bounds the direct constraint and nothing beneath it, so it is not a reproducible
restore: transitive versions still move. No decision covers that yet; the same parameter
names it as owed before M1.

Never put `Version=` on a `PackageReference` (`NU1008`), and never omit the row
(`NU1010`). Both exit 1, both measured.
