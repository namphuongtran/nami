#!/usr/bin/env bash
# Self-test for check-adrs.sh Check 8 (GitHub Actions workflow hygiene) and Check 9
# (dependency-source hygiene, the two premises ADR-0095 rests on).
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

# The same trap, one layer out, and it took Check 8c to expose it. Copying the
# script is not enough when a check reads a file that is also being edited: 8c
# landed in the same change that fixed every `uses:` it rejects, so the worktree
# carried HEAD's workflows, the new check found seven real violations in them,
# and case 1 and case 4 both failed for a reason that had nothing to do with
# either. **A self-test's subject is the script AND its input.** Copy both, or a
# check cannot be introduced in the same commit as the fix it demands.
cp "$(pwd)"/.github/workflows/*.yml "$wt/.github/workflows/" 2>/dev/null || {
  echo "self-test FAILED: could not stage the working-tree workflows into the worktree"
  exit 1
}

# Check 9's input is the tracked build files, so they are staged for the same reason
# the workflows above are. Without this, a change that fixed a floating version and
# added the rule forbidding it in one commit would test the rule against HEAD's
# unfixed file, which is the trap the paragraph above records one layer out.
cp "$(pwd)"/Directory.Packages.props "$wt/Directory.Packages.props" 2>/dev/null || {
  echo "self-test FAILED: could not stage the working-tree Directory.Packages.props into the worktree"
  exit 1
}
cp "$(pwd)"/Directory.Build.props "$wt/Directory.Build.props" 2>/dev/null || {
  echo "self-test FAILED: could not stage the working-tree Directory.Build.props into the worktree"
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
        uses: actions/checkout@v7.0.1
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

# --- Case 4: Check 8c, action references that are not a full version tag ---
# Four violations and four look-alikes, on the same reasoning as case 2: a rule
# that flagged every `uses:` would catch the four below and also forbid the exact
# form ADR-0086 requires. The `@v7` line is the one that matters most, because it
# is the form that already changed this repository's linter under an unchanged
# workflow file, and it differs from the sanctioned form by four characters.
cat > "$wt/.github/workflows/zz-pins.yml" <<'YAML'
name: Pins
on:
  push:
    branches: [main]
jobs:
  a:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/checkout@main
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1
      - uses: actions/checkout@v7.0
      - uses: actions/checkout@v7.0.1
      - uses: actions/setup-dotnet@v6.0.0 # latest stable on 2026-08-02
      - uses: ./.github/actions/local-thing
      - uses: docker://alpine:3.20
YAML

out4=$( cd "$wt" && git add .github/workflows/zz-pins.yml && bash scripts/check-adrs.sh 2>&1 )
rc4=$?

[ "$rc4" -eq 1 ] || fail "expected exit 1 on the planted pin violations, got $rc4"

pinhits=$(printf '%s\n' "$out4" | grep -c 'not a full version tag')

# The count is the load-bearing assertion, for the reason case 2 records: the four
# negatives below fail open if a line number drifts. Four is exactly the violations
# planted, so a fifth means a look-alike tripped and a third means one was missed.
[ "$pinhits" -eq 4 ] || fail "expected 4 pin findings, got $pinhits (more means a look-alike tripped; fewer means a real one was missed)"

printf '%s\n' "$out4" | grep -q 'zz-pins.yml:9:'  || fail "the floating major @v7 on line 9 was not reported"
printf '%s\n' "$out4" | grep -q 'zz-pins.yml:10:' || fail "the branch reference @main on line 10 was not reported"
printf '%s\n' "$out4" | grep -q 'zz-pins.yml:11:' || fail "the commit SHA on line 11 was not reported"
printf '%s\n' "$out4" | grep -q 'zz-pins.yml:12:' || fail "the partial version @v7.0 on line 12 was not reported"

printf '%s\n' "$out4" | grep -q 'zz-pins.yml:13:' && fail "a full version tag was flagged; that is the form ADR-0086 requires"
printf '%s\n' "$out4" | grep -q 'zz-pins.yml:14:' && fail "a full version tag with a trailing comment was flagged"
printf '%s\n' "$out4" | grep -q 'zz-pins.yml:15:' && fail "a local ./ action was flagged; it is outside the stated scope"
printf '%s\n' "$out4" | grep -q 'zz-pins.yml:16:' && fail "a docker:// reference was flagged; ADR-0051 section D governs image digests"

( cd "$wt" && git rm -q --cached .github/workflows/zz-pins.yml >/dev/null 2>&1; rm -f .github/workflows/zz-pins.yml )

# Case 2's planted workflow is still staged at this point, and it has to go before any
# later case asserts an exit code. Found by running these cases against a guardrail with
# no Check 9 at all: the two "expected exit 1" assertions below PASSED, because case 2's
# three violations were still in the index and failing the run on their own. An exit code
# is a property of the whole script, so it is only evidence about the case at hand when
# the case at hand is the only violation staged. The count assertions were the ones that
# reported the missing check, which is the same lesson the paragraph above case 2 records
# about negatives failing open, arriving by a different route.
( cd "$wt" && git rm -q --cached .github/workflows/zz-selftest.yml >/dev/null 2>&1; rm -f .github/workflows/zz-selftest.yml )

# --- Case 5: Check 9a, floating versions in a build file (ADR-0095 parameter B) ---
# Two violations and four look-alikes. The comment look-alike is the load-bearing one
# and it is not hypothetical: Directory.Packages.props opens with a ninety-line comment
# that discusses version forms, so a rule matching any asterisk in the file would flag
# the documentation explaining the rule. ADR-0095 parameter B states the same boundary
# in prose, because that ADR quotes a floating version in order to forbid it.
cat > "$wt/zz-selftest.props" <<'XML'
<Project>
  <!-- A documented example must not trip the rule: Version="7.*" is what
       parameter B forbids, and saying so here has to stay legal. -->
  <ItemGroup>
    <PackageVersion Include="A.Floating" Version="7.*" />
    <PackageVersion Include="B.Override" VersionOverride="1.2.*" />
    <PackageVersion Include="C.Bracket" Version="[5.6.0]" />
    <PackageVersion Include="D.Plain" Version="5.6.0" AssemblyVersion="1.0.*" />
  </ItemGroup>
</Project>
XML

out5=$( cd "$wt" && git add zz-selftest.props && bash scripts/check-adrs.sh 2>&1 )
rc5=$?

[ "$rc5" -eq 1 ] || fail "expected exit 1 on the planted floating versions, got $rc5"

floathits=$(printf '%s\n' "$out5" | grep -c 'floating version')

# The count is the load-bearing assertion, for the reason cases 2 and 4 record: the
# negatives below fail open if a line number drifts.
[ "$floathits" -eq 2 ] || fail "expected 2 floating-version findings, got $floathits (more means a look-alike tripped; fewer means a real one was missed)"

printf '%s\n' "$out5" | grep -q 'zz-selftest.props:5:' || fail "the floating Version=\"7.*\" on line 5 was not reported"
printf '%s\n' "$out5" | grep -q 'zz-selftest.props:6:' || fail "the floating VersionOverride on line 6 was not reported"

printf '%s\n' "$out5" | grep -q 'zz-selftest.props:2:' && fail "an asterisk inside an XML comment was flagged; a rule cannot forbid its own explanation"
printf '%s\n' "$out5" | grep -q 'zz-selftest.props:3:' && fail "the second line of the XML comment was flagged"
printf '%s\n' "$out5" | grep -q 'zz-selftest.props:7:' && fail "the bracket form was flagged; it is the form ADR-0021 parameter A requires"
printf '%s\n' "$out5" | grep -q 'zz-selftest.props:8:' && fail "a wildcard in a differently-named attribute was flagged; the rule anchors on whitespace before Version= so AssemblyVersion is out of scope, and line 8 carries a legal Version on the same line to prove the anchor rather than the line"

( cd "$wt" && git rm -q --cached zz-selftest.props >/dev/null 2>&1; rm -f zz-selftest.props )

# --- Case 6: Check 9b, a package source configuration file (ADR-0095 parameter C) ---
# Two violations, one at the root and one nested, and one look-alike whose name merely
# starts the same way. The two spellings differ in case on purpose: the rule matches
# case-insensitively because the filename is written both ways in the wild.
mkdir -p "$wt/build"
printf '%s\n' '<configuration />' > "$wt/NuGet.config"
printf '%s\n' '<configuration />' > "$wt/build/nuget.config"
printf '%s\n' '<configuration />' > "$wt/NuGet.config.example"

out6=$( cd "$wt" && git add NuGet.config build/nuget.config NuGet.config.example && bash scripts/check-adrs.sh 2>&1 )
rc6=$?

[ "$rc6" -eq 1 ] || fail "expected exit 1 on the planted package source config, got $rc6"

cfghits=$(printf '%s\n' "$out6" | grep -c 'package source configuration file')
[ "$cfghits" -eq 2 ] || fail "expected 2 package-source findings, got $cfghits (a third means the .example look-alike tripped)"

printf '%s\n' "$out6" | grep -q 'NuGet.config.example' && fail "a file whose name merely starts with the config name was flagged"

( cd "$wt" && git rm -q --cached NuGet.config build/nuget.config NuGet.config.example >/dev/null 2>&1
  rm -f NuGet.config build/nuget.config NuGet.config.example; rmdir build 2>/dev/null )

# --- Case 7: untracked means unread for Check 9 too, and it must say so ---
# The same false green case 3 covers for workflows. The coverage block named markdown
# and workflows only, so Check 9 arrived carrying this blind spot open, exactly as
# Check 8 did on 2026-08-02. scripts/CLAUDE.md states the generalisation: when a check
# joins this script, ask what its input set is before asking what it matches.
printf '%s\n' '<Project><ItemGroup><PackageVersion Include="Z" Version="9.*" /></ItemGroup></Project>' > "$wt/zz-untracked.props"
out7=$( cd "$wt" && bash scripts/check-adrs.sh 2>&1 )
printf '%s\n' "$out7" | grep -q 'zz-untracked.props' || fail "an untracked build file produced no coverage warning, which is the false green the warning exists for"
printf '%s\n' "$out7" | grep -q 'untracked dependency file(s) were NOT read by Check 9' || fail "the coverage warning does not name Check 9 as the check that did not read it"
rm -f "$wt/zz-untracked.props"

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
echo "check-adrs self-test OK: Check 8 catches 3 planted hygiene violations and 4 planted pin violations, Check 9 catches 2 planted floating versions and 2 planted source configs, together they leave 13 look-alikes alone, and both warn on untracked."
exit 0
