# CLAUDE.md for `scripts/`

The root [`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule and the content rules,
including **never edit a document to silence a checker** and **a false positive in a
checker is a defect in the checker**. Both are rules about this folder's output and they
live at the root because the file that gets wrongly edited is a document, not a script.

[`README.md`](README.md) in this folder is the authority on what each of the seven checks
does, on how to run them, and on the opt-in pre-commit hook with its git-ignored denylist
and allowlist. Read it. What follows is what it does not carry.

## Run `git add` before you run the guardrail, or its green means nothing

`check-adrs.sh:27` builds its input as `md=$(git ls-files '*.md')`, which lists the **git
index**. A markdown file that has never been added is therefore invisible to Checks 1, 2,
5 and 6, and the script prints `ADR/docs guardrail OK.` while having read nothing you just
wrote. This is a false green, not a limitation, and it has fired.

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
