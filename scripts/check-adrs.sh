#!/usr/bin/env bash
# ADR / docs guardrail (neutral, public). Run by CI and available locally.
# Checks: template placeholders, ADR cross-reference integrity, ADR index/status,
# ADR-0061 stack-of-record table membership, the no-em-dash style rule,
# design-corpus test identifiers, and architecture decisions-index membership.
# Contains no competitor/real-name logic; that is a local, git-ignored concern
# (see scripts/README.md).
#
# Portable to macOS bash 3.2 and the Ubuntu CI runner: no mapfile, no
# associative arrays, no GNU-only `xargs -r`. Assumes tracked markdown paths
# have no spaces/newlines (true in this repo), so `git ls-files` output is
# word-split intentionally; see the plan for the `-z` fallback if that changes.
# ADR-file existence/enumeration (Checks 2-3) uses on-disk globs, not git
# ls-files: identical to CI's tracked-only checkout, and locally it also
# surfaces an untracked ADR before it is committed. Where a check reads
# *references* rather than ADR files (Checks 1, 2, 5, 6) the input is the tracked
# markdown set, so a dangling ADR number is caught in any layer that cites one.
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"
readme="docs/adr/README.md"

problems=()
add() { problems+=("$1"); }

# --- Check 1: template placeholders across all tracked markdown ---
md=$(git ls-files '*.md')
ph=$(grep -Fn -e '{Product}' -e '{Company}' -e '{domain}' $md 2>/dev/null || true)
if [ -n "$ph" ]; then
  while IFS= read -r l; do add "placeholder token: $l"; done <<< "$ph"
fi

# --- Check 2: ADR cross-reference integrity (all tracked markdown) ---
# Scope is every tracked markdown file, not just docs/adr: the architecture and
# detailed-design layers carry far more ADR references than the ADRs do, and an
# ADR number that resolves nowhere is the same defect wherever it is written.
nums=$(grep -hoE 'ADR-[0-9]{4}' $md 2>/dev/null | sed 's/ADR-//' | sort -u)
for n in $nums; do
  if ! ls docs/adr/${n}-*.md >/dev/null 2>&1; then
    locs=$(grep -nE "ADR-${n}" $md 2>/dev/null || true)
    while IFS= read -r l; do add "dangling ADR-${n} -> $l"; done <<< "$locs"
  fi
done

# --- Check 3: ADR index / status consistency ---
# 3a: every ADR file has an index row whose status matches its frontmatter
for f in docs/adr/[0-9][0-9][0-9][0-9]-*.md; do
  num=$(basename "$f" | cut -c1-4)
  fm=$(grep -m1 -E '^status:' "$f" | sed -E 's/^status:[[:space:]]*"?([a-z]+)"?.*/\1/')
  row=$(grep -E "^\| \[${num}\]\(" "$readme" || true)
  if [ -z "$row" ]; then
    add "ADR ${num} (${f}) has no index row in ${readme}"
    continue
  fi
  idxst=$(sed -E 's/.*\|[[:space:]]*([a-z]+)[[:space:]]*\|[[:space:]]*$/\1/' <<< "$row")
  if [ "$idxst" != "$fm" ]; then
    add "ADR ${num} status mismatch: frontmatter '${fm}' vs index '${idxst}'"
  fi
done
# 3b: every index row maps to an existing ADR file
while IFS= read -r row; do
  num=$(sed -E 's/^\| \[([0-9]{4})\].*/\1/' <<< "$row")
  ls docs/adr/${num}-*.md >/dev/null 2>&1 || add "index row ADR ${num} in ${readme} has no matching file"
done < <(grep -E '^\| \[[0-9]{4}\]\(' "$readme")

# --- Check 4: ADR-0061 stack-of-record table membership (bidirectional) ---
# Every ADR marked `stack-record: true` must appear in the ADR-0061 table, and
# every ADR number in that table must carry the marker. This enforces ADR-0061's
# maintenance rule so the stack table and the ADRs it indexes cannot drift apart.
stackfile=$(ls docs/adr/0061-*.md 2>/dev/null)
marked=$(for f in $(grep -lE '^stack-record:[[:space:]]*true' docs/adr/[0-9][0-9][0-9][0-9]-*.md 2>/dev/null); do basename "$f" | cut -c1-4; done | sort -u)
tabled=$(grep -E '^\| .* \| .* \| [0-9]' "$stackfile" 2>/dev/null | sed -E 's/.*\| ([0-9, ]+) \|$/\1/' | tr ',' '\n' | tr -d ' ' | grep -E '^[0-9]{4}$' | sort -u)
for n in $marked; do
  echo "$tabled" | grep -qx "$n" || add "ADR ${n} is marked 'stack-record: true' but is missing from the ADR-0061 stack table"
done
for n in $tabled; do
  echo "$marked" | grep -qx "$n" || add "ADR ${n} is in the ADR-0061 stack table but is missing the 'stack-record: true' frontmatter marker"
done

# --- Check 5: no em dash in tracked markdown (project style rule) ---
# Use a comma, colon, or parentheses instead. The pattern is built from the
# codepoint so this file stays pure ASCII and cannot trip its own check.
emdash=$(printf '\xe2\x80\x94')
em=$(grep -Fn -e "$emdash" $md 2>/dev/null || true)
if [ -n "$em" ]; then
  while IFS= read -r l; do add "em dash (use a comma, colon, or parentheses): $l"; done <<< "$em"
fi

# --- Check 6: no design-corpus test identifiers in tracked markdown ---
# Shapes like 9.T16, 9.K6, 8.K3a, 25.T1 and 9.6c are the design corpus's test
# labels. They read as pointers into a numbered test register this repository does
# not have, and unlike a dangling ADR-NNNN nothing else can see them. State an
# obligation by what it asserts and list it in docs/design/20-testing.md instead;
# the convention is in docs/adr/README.md.
# The NN.T / NN.K limb is deliberately NOT restricted to the "9." family. The
# corpus numbers its test labels by owning document, so 8.K* (key management) and
# 25.T* (admin API) exist alongside 9.T*/9.K*, and the two halves are written on
# the same line there: the corpus ADR-0011 reads "tasks 8.K3a/b/c, 9.K3/9.K6". An
# import that took only the 9.K half is exactly how the 8.K half arrives next time.
# A digit-dot-T/K-digit shape cannot collide with a version number or a section
# reference, so widening this limb costs no false positives.
# Deliberately NOT matched, so the zero above is readable: a bare "9.5" or "8.0"
# (version numbers), "section 5.6" (a real section), and any N.NN task number,
# because those overlap with legitimate prose and would be false positives. The
# trailing-letter limb stays scoped to "9." for that reason.
corpustests=$(grep -nE '\b[0-9]{1,2}\.(T|K)[0-9]+[a-z]?\b|\b9\.[0-9]+[a-z]\b' $md 2>/dev/null || true)
if [ -n "$corpustests" ]; then
  while IFS= read -r l; do add "design-corpus test identifier (name what the test asserts, and list it in the testing design): $l"; done <<< "$corpustests"
fi

# --- Check 7: architecture decisions-index membership (bidirectional) ---
# docs/adr/README.md is not the only index an ADR must appear in. The SAD's
# reverse index (docs/architecture/18-decisions-index.md) answers "which views
# must I re-read when this decision changes", and an ADR with no row there is
# indistinguishable from one that does not exist. Check 3 could not see this,
# and nine ADRs (0078-0086) had drifted out of it while every other check
# stayed green, which is precisely the shape of failure a second index has:
# nothing fails, so nothing is noticed.
# Only membership is checked, not the "Views that cite it" column. That column
# is regenerated from the views themselves (the generator is printed in that
# file's section 1), and re-implementing it here in portable bash would be a
# second, weaker copy of a rule that already has one. State the limit so the
# pass is not read as covering the cell contents.
archidx="docs/architecture/18-decisions-index.md"
if [ -f "$archidx" ]; then
  for f in docs/adr/[0-9][0-9][0-9][0-9]-*.md; do
    num=$(basename "$f" | cut -c1-4)
    grep -qE "^\| \[${num}\]\(" "$archidx" || add "ADR ${num} (${f}) has no row in ${archidx}"
  done
  while IFS= read -r row; do
    num=$(sed -E 's/^\| \[([0-9]{4})\].*/\1/' <<< "$row")
    ls docs/adr/${num}-*.md >/dev/null 2>&1 || add "index row ADR ${num} in ${archidx} has no matching file"
  done < <(grep -E '^\| \[[0-9]{4}\]\(' "$archidx")
else
  add "missing ${archidx} (Check 7 cannot run)"
fi

# --- Report ---
if [ "${#problems[@]}" -gt 0 ]; then
  echo "ADR/docs guardrail FAILED: ${#problems[@]} problem(s):"
  printf '  - %s\n' "${problems[@]}"
  exit 1
fi
echo "ADR/docs guardrail OK."
exit 0
