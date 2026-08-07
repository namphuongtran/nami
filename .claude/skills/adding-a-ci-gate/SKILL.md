---
name: adding-a-ci-gate
description: Use when adding or changing anything that is supposed to fail a build in the Nami repository. That includes a check inside scripts/check-adrs.sh, a new scripts/test-*.sh self-test, a job or step in ci.yml, an MSBuild property, an .editorconfig severity, and the pre-commit hook. Four of this repository's nine gates are self-tests, and each was written after a real defect where a control read as enforced while enforcing nothing. The generic answer, which is to add the check and read the green, is the failure mode.
---

# Adding a CI gate here

Read this before writing a check, and before believing one. It exists because the expensive
failure here is not a gate that fails wrongly. It is a gate that passes while reading as armed.

This skill holds nothing that a loaded file already holds.
[`../../rules/build-and-ci.md`](../../rules/build-and-ci.md) holds the measured traps in the files
that decide whether a gate bites, and it loads on any workflow, props, or `.editorconfig` file.
[`../../../scripts/CLAUDE.md`](../../../scripts/CLAUDE.md) holds the traps learned inside that
folder, and it is **not** re-injected after `/compact`, so re-read it if the session has been
compacted. [`../../../scripts/README.md`](../../../scripts/README.md) is the authority on what each
gate checks and why. None of the three is restated here.

## What exists today, measured

Measured on 2026-08-07 at `10df955`, by reading `.github/workflows/ci.yml` and
`scripts/hooks/pre-commit`. Nine gates run in CI. Four are self-tests. The hook runs two of the
nine, which is why `CLAUDE.md` says a green hook is not a green build.

| # | Gate | Run at | Self-test | In the hook |
|---|---|---|---|---|
| 1 | markdownlint-cli2 over `**/*.md` | `ci.yml:26` | no | no |
| 2 | `scripts/check-adrs.sh` | `ci.yml:36` | no | yes, `pre-commit:11` |
| 3 | `scripts/check-decisions-index.py` | `ci.yml:42` | no | yes, `pre-commit:17` |
| 4 | `scripts/test-check-adrs.sh` | `ci.yml:50` | **yes** | no |
| 5 | `scripts/test-editorconfig.sh` | `ci.yml:80` | **yes** | no |
| 6 | `scripts/test-public-api-gate.sh` | `ci.yml:102` | **yes** | no |
| 7 | `scripts/test-warnings-as-errors.sh` | `ci.yml:125` | **yes** | no |
| 8 | `dotnet build` and `dotnet format --verify-no-changes` | `ci.yml:158`, `ci.yml:170` | no | no |
| 9 | `dotnet test` | `ci.yml:210` | no | no |

The hook also runs a local name scrub over **staged** markdown, which is not one of the nine and
is not a CI gate. It reads a git-ignored `scripts/.local/name-denylist`, so it does nothing at all
on a clone that has not created one.

**There are two different nines, and confusing them is easy.** The nine above are gates.
`scripts/README.md:7-15` separately enumerates nine **checks inside `check-adrs.sh`**, which are
all one gate, number 2. `scripts/CLAUDE.md` uses the second sense.

## The four questions, in order

Ask these before writing the check. Three of the four are cheap, and each was learned from a
defect rather than added as a precaution.

1. **What is this check's input set?** Not what it matches. `check-adrs.sh` builds its markdown
   input from `git ls-files`, so a file that has never been `git add`-ed is invisible.
   `scripts/CLAUDE.md` states the rule: "when a check joins this script, ask what its input set is
   before asking what it matches. The coverage warning is a list of input sets, not a property of
   the script."
2. **Does the coverage warning name that input set?** Check 8 and Check 9 each arrived without an
   entry, so a new workflow and an untracked `NuGet.config` would have been unread **and**
   unannounced. `scripts/CLAUDE.md`: "Two for two, so assume the next check will need its own
   warning entry rather than checking whether it does."
3. **What would have to break for this to go quiet?** Then write that break down as a test. That
   is the sentence `CLAUDE.md` closes its gate section with, and the four self-tests are the four
   answers already given.
4. **Where is the result read, and does it state what the check cannot see?** "A check that cannot
   see a whole class must say so where its result is read" (`scripts/CLAUDE.md`). Check 4 is the
   model, because ADR-0061 states the blind spot in its own text. Check 7's comment does the same
   for the "Views that cite it" column.

## Where the generic answer is wrong here

Each row was read at its source on 2026-08-07. The middle column quotes enough of the decision to
survive a line shift, so a drifted pointer reads as drift rather than as a different claim.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| Adding the check and reading the green | A green on a clean tree proves only that the matcher parses, "since a clean tree has nothing to match" | `scripts/README.md`, under `test-check-adrs.sh` |
| `set -euo pipefail` | `set -uo pipefail`, with **no** `-e`, and problems accumulate in an array printed at the end, so one run reports every problem | `scripts/CLAUDE.md`, "Constraints that are load-bearing" |
| A per-line `grep && fail` negative assertion | Assert a **count**. "Every hard-coded line number in the first run was off by one ... The four `grep && fail` negatives passed against lines that do not exist and said nothing" | `scripts/CLAUDE.md`, "A negative assertion fails open" |
| A worktree at `HEAD` for isolation | Copy the **working-tree** subject in, and its inputs too. A worktree at `HEAD` tested the committed script, so deleting a rule outright still produced a green | `scripts/CLAUDE.md`, and `scripts/README.md` under `test-check-adrs.sh` |
| A probe project in a temp directory | Inside the repository, so it inherits the real `.editorconfig` and the real `Directory.Build.props` rather than copies that could drift | `scripts/README.md`, under `test-editorconfig.sh` |
| Exit non-zero when the toolchain is missing | Skip with **exit 0**, and say out loud that a skip is not a pass | `scripts/test-editorconfig.sh:69-73` |
| A step inside `Solution build` | A separate job. "A red here means the gate stopped biting, not that the code is wrong, and the two should not arrive under one name" | `scripts/README.md`, under `test-public-api-gate.sh` |
| A severity line in `.editorconfig` | For an `IDExxxx` rule that fails nothing on its own. `EnforceCodeStyleInBuild` is what turns it into a build failure, and `RS00xx` rules are the opposite shape | `.claude/rules/build-and-ci.md` |
| A probe at the repository root as full coverage | MSBuild walks **up**, so no edit under `src/` or `tests/` can reach a root probe. Part 6 of the warnings self-test evaluates the real projects for that reason | `scripts/README.md`, under `test-warnings-as-errors.sh` |
| Asserting exit 1 to prove a new case | "An exit code is a property of the whole script, so a later case asserting `exit 1` proves nothing while an earlier case's violation is still staged" | `scripts/README.md`, under `test-check-adrs.sh` |
| `mapfile`, an associative array, or `xargs -r` | Portable to macOS bash 3.2 and the Ubuntu runner. The Mac version is **older** than CI's, so "it worked locally" is the wrong direction of confidence | `scripts/CLAUDE.md` |
| A literal broken reference as an illustration | Check 2 has no escape hatch, so describe the shape instead. Check 5's pattern is built from the codepoint for the same reason, which keeps the script pure ASCII | `scripts/CLAUDE.md` |
| Editing the document so the checker passes | Never. Fix the checker, or record the finding as legitimate. If neither is cheap, delete the checker. `scripts/review/` was deleted on 2026-07-27 for exactly this | `CLAUDE.md` evidence rule, and `scripts/CLAUDE.md` |

## The self-test skeleton

Modelled on `scripts/test-editorconfig.sh`, which is the fullest example. Read that file rather
than copying this outline blindly.

```bash
#!/usr/bin/env bash
# A header that states: why this exists, what would make the subject inert,
# each break experiment with its failure count, and the portability note.
set -uo pipefail
cd "$(git rev-parse --show-toplevel)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "<name> self-test SKIPPED: no dotnet SDK on PATH."
  echo "  This is a skip, not a pass. The subject is unverified in this run."
  exit 0
fi

probe=".<name>-probe"
cleanup() { rm -rf "$probe"; }
trap cleanup EXIT
rm -rf "$probe"
mkdir -p "$probe" || { echo "<name> self-test FAILED: cannot create $probe"; exit 1; }

fails=0
fail() { echo "  FAIL: $1"; fails=$((fails + 1)); }
```

The substring expectation helper, `scripts/test-editorconfig.sh:191-197`, and the discipline that
counts rather than greps per line:

```bash
expect() {
  if printf '%s\n' "$bad_out" | grep -qF "$2"; then
    :
  else
    fail "$1 not caught (expected a diagnostic containing: $2)"
  fi
}
```

Then a control part first, one part per way the subject can go inert, and a report that exits 1
with the failure count or 0 with a descriptive line. Group the parts **by the failure**, not by
the decision: the warnings self-test has four properties in one `PropertyGroup` and each can be
silenced alone, so it has six parts across three ADRs.

Add the probe directory to `.gitignore` as a backstop. The `trap` is what actually removes it.

## Break experiments, which are the point rather than a formality

A self-test is not known to work until the subject has been broken on purpose and the test has
gone red. Record each break with its failure count in the script header, because the **pattern
across breaks** carries more than any single number. The four in `test-editorconfig.sh:41-44`
show it: two breaks produce failures only on the build path, which is the asymmetry Part 3 exists
for.

Two results are worth writing down even though they look like nothing.

- **A break that correctly changes nothing.** Reordering the naming rules produced 0 failures, and
  that green is right, because declaration order is not load-bearing. Finding that out is how a
  false claim in the header was removed before it stayed committed.
- **A break that failed to break.** One edit threw before writing, so the unmodified file passed.
  "A break experiment that fails to break reports the same green as a healthy subject, so check
  that the subject actually changed before believing a green"
  (`scripts/test-editorconfig.sh:57-61`).

## Wiring it into CI

Follow `ci.yml`'s existing shape rather than inventing one. Read the `C# style ruleset` job at
`ci.yml:52-80` as the template. Four constraints bind, and three of them fail the build:

- Every `uses:` is a full version tag, `@vX.Y.Z`. A floating major, a branch, a partial version,
  and a commit SHA are all rejected by Check 8c.
- No `${{ ... }}` expression appears inside any `run:` script. Pass the value through `env:` and
  read it as a shell variable.
- No `pull_request_target:` and no `workflow_run:` trigger.
- A gate needing a .NET SDK is its own job, because it needs `actions/setup-dotnet` and because a
  red in it means something different from a red in `Solution build`.

The fourth is a convention rather than a check. The first three are ADR-0092 section 6 and
ADR-0086, enforced as Check 8, and `scripts/test-check-adrs.sh` is what proves they still match on
the runner's awk.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is
written into it (`docs/CLAUDE.md`). Both were run on 2026-08-07 with `git grep -in` over every
tracked file.

- **No gate reads whether a citation supports its claim, and this is stated rather than
  accidental.** `docs/CLAUDE.md` records the searches: on 2026-08-03, across `check-adrs.sh` and
  `check-decisions-index.py`, for a digit-and-colon pattern, for `line number`, and for a trailing
  colon-digits pattern, with no hits. See
  [`../checking-a-citation/SKILL.md`](../checking-a-citation/SKILL.md), which is the manual
  procedure that stands in for the missing gate.
- **No self-test covers gates 1, 2 outside Checks 8 and 9, 3, 8, or 9.** `test-check-adrs.sh`
  asserts Checks 8 and 9 only, stated in its own README section. So Checks 1 to 7 are proven by
  the tree containing violations, which a clean tree does not. Whether that is worth closing is
  undecided, and closing it would be four more self-tests rather than one.

## Who owns which question

| Question | Authority |
|---|---|
| What each gate checks, how to run it, and the hook | `scripts/README.md` |
| Traps learned inside `scripts/` | `scripts/CLAUDE.md`, not re-injected after `/compact` |
| Traps in the files that decide whether a gate bites | `.claude/rules/build-and-ci.md` |
| Which security scan runs at which stage | ADR-0092, and `.claude/skills/ci-security-scans/SKILL.md` |
| Warnings as errors, and the restore-time carve-out | ADR-0093 |
| Analyzer breadth | ADR-0094 |
| The C# style ruleset that gate 5 tests | ADR-0065, and `.editorconfig`, which wins over ADR prose |
| The public-API lock that gate 6 tests | ADR-0044 parameter A, and parameter B for the delete direction |
| Action pinning | ADR-0086 |
| The markdownlint version, which is coupled and not chosen | `.claude/rules/build-and-ci.md`, and ADR-0086 parameter B |
| The test suite that gate 9 runs | ADR-0024 for the architecture rules, ADR-0060 for the strategy |

## Which tool reads a gate claim at its source

**A tool is a source, never an authority.** Where an external source and an accepted ADR disagree,
stop and flag both with file and line, and do not fill the gap from judgement.

| To read at source | Use | Why |
|---|---|---|
| Why a build passed or failed a rule | The `dotnet-msbuild` binlog tools, through `binlog-generation` then `binlog-failure-analysis` | Reading the console output is how an incremental build's reused silence gets mistaken for a pass |
| What a property actually evaluated to | `dotnet msbuild -getProperty:<name>`, and `-t:Rebuild` when comparing values | A property-only change moves no compilation input, so MSBuild skips the compiler and reports the previous run's result |
| Which globalconfig the SDK included | `dotnet msbuild -t:CoreCompile -getItem:EditorConfigFiles` | ADR-0092 used exactly this to correct a claim it had reasoned rather than measured |
| An MSBuild or analyzer behaviour | `microsoft-docs`: `microsoft_docs_search`, then `microsoft_docs_fetch` | Every figure in this area moves with the SDK version, so it is a dated measurement and not a constant |

Pass each `-p:` as its own shell argument. Two in one argument silence every analyzer at exit 0,
measured 2026-08-03 and recorded in `.claude/rules/build-and-ci.md`.
