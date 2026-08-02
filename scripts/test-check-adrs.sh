#!/usr/bin/env bash
# Self-test for check-adrs.sh Check 8 (GitHub Actions workflow hygiene).
#
# Why this exists as a permanent test rather than a one-off proof. scripts/CLAUDE.md
# holds that a check never run against the bug it exists for is not known to work.
# Check 8 was proven that way on the author's machine, which runs the BWK awk that
# ships with macOS; CI runs Ubuntu, whose awk is a different implementation. A green
# guardrail on a clean tree proves only that the awk parses and raises no false
# positive. It says nothing about whether the matching works there, because a clean
# tree has nothing to match. This test supplies the bug, so the runner's own awk has
# to catch it on every run, and so does any future edit to that awk.
#
# It runs in a throwaway `git worktree` checked out at HEAD, which has its own index
# and working tree. The real ones are never written to, including when this script
# fails part-way, and the trap removes the worktree on any exit path.
#
# Portable to macOS bash 3.2 and the Ubuntu runner, like the script it tests: no
# mapfile, no associative arrays, no GNU-only flags.
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"

tmp=$(mktemp -d) || { echo "self-test: mktemp failed"; exit 1; }
wt="$tmp/wt"
cleanup() { git worktree remove --force "$wt" >/dev/null 2>&1; rm -rf "$tmp"; }
trap cleanup EXIT

if ! git worktree add --detach "$wt" HEAD >/dev/null 2>&1; then
  echo "self-test FAILED: could not create a git worktree at $wt"
  exit 1
fi

# The worktree is checked out at HEAD, so it carries the **committed** guardrail. Copy
# the working-tree copy over it, or this file tests the version you are not editing.
# That is not a hypothetical: the first version of this script omitted this line, and
# deleting Check 8's block-scalar detection outright still produced a green self-test,
# because the worktree was running the old script from HEAD. A test that passes on a
# deliberately broken subject is the exact false green this whole check exists to avoid,
# and it took a break-it experiment rather than a read-through to find.
cp "$(pwd)/scripts/check-adrs.sh" "$wt/scripts/check-adrs.sh" || {
  echo "self-test FAILED: could not stage the working-tree guardrail into the worktree"
  exit 1
}

fails=0
fail() { echo "  - $1"; fails=$((fails + 1)); }

# --- Case 1: HEAD is clean, so the guardrail must pass in the worktree ---
# Without this the whole test could pass on a script that fails unconditionally.
( cd "$wt" && bash scripts/check-adrs.sh >/dev/null 2>&1 )
if [ "$?" -ne 0 ]; then
  fail "the guardrail does not pass on a clean checkout of HEAD, so case 2 proves nothing"
fi

# --- Case 2: a workflow carrying three violations and four look-alikes ---
# The four that must NOT trip are the point of the test. A rule that flagged every
# `${{` in a workflow would catch the three below and also forbid the mitigation it
# exists to push people toward, which is passing a value through `env:` and reading
# it as a shell variable.
cat > "$wt/.github/workflows/zz-selftest.yml" <<'YAML'
name: Self test
on:
  pull_request_target:
    branches: [main]
jobs:
  a:
    runs-on: ubuntu-latest
    steps:
      - run: echo "${{ github.event.pull_request.title }}"
      - run: |
          echo start
          echo "${{ github.head_ref }}"
          echo end
      - if: ${{ github.event_name == 'push' }}
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          ref: ${{ github.sha }}
      - env:
          TITLE: ${{ github.event.pull_request.title }}
        run: |
          echo "$TITLE"
YAML

out=$( cd "$wt" && git add .github/workflows/zz-selftest.yml && bash scripts/check-adrs.sh 2>&1 )
rc=$?

[ "$rc" -eq 1 ] || fail "expected exit 1 on the planted workflow, got $rc"

runhits=$(printf '%s\n' "$out" | grep -c 'interpolated into a run:')
trighits=$(printf '%s\n' "$out" | grep -c 'proposer-controlled input')

# These two counts are the load-bearing assertions. They are what proves the four
# look-alikes did not trip: any of them firing would push runhits above 2. The
# per-line checks below are diagnostics that say *which* one moved, and the negative
# ones among them **fail open** if a line number drifts, so they must never be relied
# on alone. That is not theory: on the first run of this file every hard-coded line
# number was off by one, and only the positive assertion below reported it while all
# four negatives passed against lines that do not exist.
[ "$runhits" -eq 2 ] || fail "expected 2 run-interpolation findings, got $runhits (more means a look-alike tripped; fewer means a real one was missed)"
[ "$trighits" -eq 1 ] || fail "expected 1 privileged-trigger finding, got $trighits"

# Positive, line-anchored: two findings of the right count could still be the wrong two.
printf '%s\n' "$out" | grep -q 'zz-selftest.yml:9$'  || fail "the inline run: interpolation on line 9 was not reported"
printf '%s\n' "$out" | grep -q 'zz-selftest.yml:12$' || fail "the block-scalar run: interpolation on line 12 was not reported"
printf '%s\n' "$out" | grep -q 'zz-selftest.yml:3:'  || fail "the pull_request_target trigger on line 3 was not reported"

# Negative diagnostics, one per look-alike, so a count failure above says which regressed.
printf '%s\n' "$out" | grep -q 'zz-selftest.yml:14$' && fail "an if: expression was flagged; only run: scripts are in scope"
printf '%s\n' "$out" | grep -q 'zz-selftest.yml:17$' && fail "a with: input expression was flagged; it is outside the stated scope"
printf '%s\n' "$out" | grep -q 'zz-selftest.yml:19$' && fail "an env: expression was flagged; that is the sanctioned mitigation"
printf '%s\n' "$out" | grep -q 'zz-selftest.yml:21$' && fail "a run: reading a shell variable was flagged; no expression is present"

# --- Case 3: untracked means unread, and the script must say so rather than pass quietly ---
cat > "$wt/.github/workflows/zz-untracked.yml" <<'YAML'
name: Untracked
on:
  push:
jobs:
  a:
    runs-on: ubuntu-latest
    steps:
      - run: echo "${{ github.head_ref }}"
YAML
out3=$( cd "$wt" && bash scripts/check-adrs.sh 2>&1 )
printf '%s\n' "$out3" | grep -q 'zz-untracked.yml' || fail "an untracked workflow produced no coverage warning, which is the false green the warning exists for"
printf '%s\n' "$out3" | grep -q 'untracked workflow file(s) were NOT read by Check 8' || fail "the coverage warning does not name Check 8 as the check that did not read it"

# --- Report ---
if [ "$fails" -gt 0 ]; then
  echo "check-adrs self-test FAILED: ${fails} assertion(s) above."
  exit 1
fi
echo "check-adrs self-test OK: Check 8 catches 3 planted violations, leaves 4 look-alikes alone, and warns on untracked."
exit 0
