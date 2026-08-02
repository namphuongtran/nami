# CLAUDE.md for `scripts/`

The root [`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule and the content rules,
including **never edit a document to silence a checker** and **a false positive in a
checker is a defect in the checker**. Both are rules about this folder's output and they
live at the root because the file that gets wrongly edited is a document, not a script.

[`README.md`](README.md) in this folder is the authority on what each of the eight checks
does, on how to run them, and on the opt-in pre-commit hook with its git-ignored denylist
and allowlist. Read it. What follows is what it does not carry.

## Run `git add` before you run the guardrail, or its green covers less than it looks

`check-adrs.sh` builds its markdown input as `md=$(git ls-files '*.md')`, which lists the
**git index**. A markdown file that has never been added is therefore invisible to Checks 1,
2, 5 and 6, and the script used to print `ADR/docs guardrail OK.` while having read nothing
you just wrote. That false green fired once, and the script now **announces the gap**
instead: it lists untracked markdown above the verdict, in both the passing and the
failing case, because a FAILED list is equally incomplete.

**Check 8 reads the index the same way, and the warning did not cover it for the length of
one edit.** Added 2026-08-02 for GitHub Actions workflows, it inherited the same blind spot
and the warning still named only markdown, so a new workflow would have been unread **and**
unannounced. Found by writing the check rather than by it firing, and fixed in the same
change. The generalisation is cheap and worth carrying: **when a check joins this script,
ask what its input set is before asking what it matches**, because the coverage warning is a
list of input sets and not a property of the script.

It warns rather than fails, deliberately. An untracked work-in-progress file is legitimate
mid-edit, and failing on one would make the script unrunnable exactly when it is most
useful, which is how a check gets skipped. **The warning is not a substitute for staging**:
until you `git add`, the verdict genuinely says nothing about those files.

Proven on 2026-08-01 rather than assumed: an untracked file containing both an em dash and
an `ADR-` reference whose four digits match no file produced **no problem at all**, and the
same file staged produced both. A check that has never been run against the bug it exists
for is not known to work.

That last sentence is written without a literal dangling reference on purpose, and the
reason generalises. **Check 2 has no escape hatch, so prose cannot quote a broken
reference as an example.** Describe the shape instead. The same constraint already shaped
three other files: `README.md` beside this one names the corpus test-label families by
prefix "because writing a whole identifier would trip this very check", the root
`CLAUDE.md` describes the placeholder tokens without their braces, and Check 5's pattern is
built from a codepoint so the script stays ASCII. Rewording an illustration is not the
forbidden move: what is forbidden is weakening a **claim** to pass a check, which is what
cost `scripts/review/` its life below.

The exception, and it is easy to mistake for coverage: Checks 2, 3, 4 and 7 enumerate ADR
*files* with on-disk globs rather than through git, so a **new ADR** is seen even before it
is staged. The script's own header says so at `check-adrs.sh:13-16`. A new design doc, KB
note, or `CLAUDE.md` gets no such treatment.

A staged deletion is the other edge: the path is still in the index while the file is gone
from disk, and the `2>/dev/null || true` on each grep turns that into a silent skip rather
than an error.

## Constraints on `check-adrs.sh` that are load-bearing

- **Portable to macOS bash 3.2 and the Ubuntu runner.** No `mapfile`, no associative
  arrays, no GNU-only flags such as `xargs -r`. The version on a developer's Mac is older
  than the one in CI, so "it worked locally" is the wrong direction of confidence here.
- **Pure ASCII, deliberately.** The em-dash check builds its pattern from the codepoint so
  the script cannot fail against itself. Do not paste the character into this file or into
  any script beside it.
- **Check 6 names the corpus test-label families by prefix** for the same reason: writing a
  whole identifier would trip the very check being described.
- **`set -uo pipefail`, with no `-e`, and problems accumulate in an array printed at the
  end.** That is not an oversight. The script is meant to report *every* problem in one
  run, so a contributor fixes a batch rather than rediscovering the next failure on each
  push. Preserve that if you add a check.
- It runs `cd "$(git rev-parse --show-toplevel)"` first, so it works from any directory.

## Writing or changing a check

- **A checker's anchor is part of its coverage claim**, and the full statement of that rule
  is in [`../docs/CLAUDE.md`](../docs/CLAUDE.md), because throwaway screens get written
  during documentation work far more often than they get written here. The short version:
  state what a screen does *not* match, inside the screen, or its zero will be read as
  absence.
- **A check that cannot see a whole class must say so where its result is read.** Check 4
  is the model: ADR-0061 states in its own text that the check is blind to a shared
  omission, so the pass is not mistaken for completeness. Check 7's comment does the same
  for the "Views that cite it" column.
- **Deleting a checker is an available remedy, and it has been used.** On 2026-07-27,
  commit `a1d6bf7` removed `scripts/review/` and its three Python screens
  (`citation-keyword-screen.py`, `design-pointer-audit.py`, `horizontal-drift-screen.py`)
  in the same change that restored seven values a merge had dropped. The screens went
  because one of them had caused a document to be weakened to make it pass. A tool that
  bends the evidence it exists to protect is worse than none.
- Keep this folder **neutral and public**. Competitor and real-name logic is a local,
  git-ignored concern under `scripts/.local/`, never committed and never mirrored into a
  public denylist.
