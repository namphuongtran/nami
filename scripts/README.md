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
- GitHub Actions workflow hygiene, two rules, added 2026-08-02 as the no-new-dependency half of the workflow-analysis gap ADR-0092 names: no `${{ ... }}` expression appears inside any `run:` script, and no workflow uses a `pull_request_target:` or `workflow_run:` trigger. **Read the scope before reading the green.** Interpolating into a shell is the injection vector and passing the value through `env:` is the standard mitigation, so the first rule enforces the mitigation rather than judging whether a value is trusted, which is the part that would need a real analyser. What neither rule sees: interpolation into an action's `with:` inputs, the scope of a `permissions:` block, composite actions and reusable workflows in other repositories, and what a pinned action does once it runs (ADR-0086 governs *which* action code runs, and nothing here governs its behaviour).

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

It creates a throwaway `git worktree` at `HEAD`, copies the **working-tree** guardrail into
it, plants a workflow carrying three violations and four look-alikes, and asserts that
exactly the three are reported. The worktree is removed on every exit path, so neither the
real working tree nor the real index is ever written to.

It exists because Check 8 matches with `awk`, and the awk on the CI runner is a different
implementation from the one the check was authored against. A green guardrail on a clean
tree proves only that the awk parses, since a clean tree has nothing to match; this supplies
the bug so the matching is proven on the awk that runs it, on every run rather than once.

Two properties are load-bearing and easy to lose in an edit. **It copies the working-tree
guardrail into the worktree**, because a worktree at `HEAD` otherwise tests the committed
script rather than the one being edited: the first version of this file omitted that copy,
and deleting Check 8's block-scalar detection outright still produced a green. And **the two
finding-count assertions are what prove the look-alikes did not trip**; the per-line checks
are diagnostics, and the negative ones among them pass vacuously if a line number drifts,
which also happened on the first run.

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
