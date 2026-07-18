#!/usr/bin/env bash
# ADR / docs guardrail (neutral, public). Run by CI and available locally.
# Checks: template placeholders, ADR cross-reference integrity, ADR index/status.
# Contains no competitor/real-name logic; that is a local, git-ignored concern
# (see scripts/README.md).
#
# Portable to macOS bash 3.2 and the Ubuntu CI runner: no mapfile, no
# associative arrays, no GNU-only `xargs -r`. Assumes tracked markdown paths
# have no spaces/newlines (true in this repo), so `git ls-files` output is
# word-split intentionally; see the plan for the `-z` fallback if that changes.
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

# --- Check 2: ADR cross-reference integrity ---
adr_md=$(git ls-files 'docs/adr/*.md')
nums=$(grep -hoE 'ADR-[0-9]{4}' $adr_md 2>/dev/null | sed 's/ADR-//' | sort -u)
for n in $nums; do
  if ! ls docs/adr/${n}-*.md >/dev/null 2>&1; then
    locs=$(grep -nE "ADR-${n}" $adr_md 2>/dev/null || true)
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

# --- Report ---
if [ "${#problems[@]}" -gt 0 ]; then
  echo "ADR/docs guardrail FAILED — ${#problems[@]} problem(s):"
  printf '  - %s\n' "${problems[@]}"
  exit 1
fi
echo "ADR/docs guardrail OK."
exit 0
