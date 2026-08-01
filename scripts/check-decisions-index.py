#!/usr/bin/env python3
"""Check the architecture layer's reverse decisions index against the views.

docs/architecture/18-decisions-index.md answers "which views must I re-read when this
decision changes". Its "Views that cite it" column is derived from the views, so it can
drift from them silently, and guardrail Check 7 deliberately does not look at it:
re-implementing this in portable bash would be a second, weaker copy of the rule.

This is that rule, implemented once. It compares three things against their sources:

  1. the "Views that cite it" cell against the views that actually mention the ADR;
  2. the "Decision" cell against the ADR's own H1 title, which the index says it quotes
     "not a paraphrase that could drift from it";
  3. that the numbered views are the only input, so a non-view markdown file added to
     docs/architecture/ (CLAUDE.md, for instance) can never appear as a phantom view.

Point 3 is not hypothetical. The generator printed inside the index used
glob('docs/architecture/*.md') with only README excluded, so docs/architecture/CLAUDE.md
entered its input set the day that file was created. It contributed nothing only because
it happens to carry no four-digit ADR reference.

Inherited caveat, restated because it is load-bearing rather than incidental: a mention is
any occurrence of the ADR number in a view, including one inside that view's own Sources
list or in a passing cross-reference. So a listed view is one that *touches* the decision,
not necessarily one that depends on it. For "what must I re-read" that is the right side to
err on, and changing it would change what the table means.

Exit status: 0 if the index agrees with the views, 1 on any drift, 2 on a usage error.
No third-party dependencies, and it never writes to the index. Use --print-table to emit
the correct rows for a human to apply.
"""

import argparse
import glob
import os
import re
import sys

ADR_GLOB = "docs/adr/[0-9][0-9][0-9][0-9]-*.md"
VIEW_GLOB = "docs/architecture/[0-9][0-9]-*.md"
INDEX = "docs/architecture/18-decisions-index.md"

# A row in section 2, anchored at line start, same shape guardrail Check 7 matches.
ROW = re.compile(r"^\| \[(\d{4})\]\(([^)]+)\) \| (.*?) \| (.*?) \|\s*$")
MENTION = re.compile(r"ADR-(\d{4})")
TITLE = re.compile(r"^# (.+)$", re.MULTILINE)

# The Decision cell is the ADR title truncated with a trailing ellipsis when too long.
ELLIPSIS = "..."


def read(path):
    with open(path, encoding="utf-8") as handle:
        return handle.read()


def views_by_adr():
    """Map ADR number -> sorted list of view numbers mentioning it."""
    mentions = {}
    for path in sorted(glob.glob(VIEW_GLOB)):
        view = os.path.basename(path)[:2]
        for num in MENTION.findall(read(path)):
            mentions.setdefault(num, set()).add(view)
    return {num: sorted(views) for num, views in mentions.items()}


def adr_titles():
    """Map ADR number -> its H1 title."""
    titles = {}
    for path in sorted(glob.glob(ADR_GLOB)):
        num = os.path.basename(path)[:4]
        found = TITLE.search(read(path))
        titles[num] = found.group(1).strip() if found else None
    return titles


def index_rows():
    """Map ADR number -> (decision cell, views cell) from the index table."""
    rows = {}
    for line in read(INDEX).splitlines():
        found = ROW.match(line)
        if found:
            num, _link, decision, cells = found.groups()
            rows[num] = (decision.strip(), cells.strip())
    return rows


def parse_views_cell(cell):
    if cell.upper() == "NONE":
        return []
    return [part.strip() for part in cell.split(",") if part.strip()]


def title_agrees(cell, title):
    """The cell is the title, or the title truncated with a trailing ellipsis."""
    if title is None:
        return False
    if cell == title:
        return True
    if cell.endswith(ELLIPSIS):
        return title.startswith(cell[: -len(ELLIPSIS)])
    return False


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--print-table",
        action="store_true",
        help="emit the correct rows instead of a report; never writes the index",
    )
    args = parser.parse_args()

    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    os.chdir(root)

    if not os.path.isfile(INDEX):
        print("missing %s" % INDEX, file=sys.stderr)
        return 2

    mentions = views_by_adr()
    titles = adr_titles()
    rows = index_rows()
    if not rows:
        print("parsed no rows from %s: the row format changed" % INDEX, file=sys.stderr)
        return 2

    if args.print_table:
        for num in sorted(titles):
            path = os.path.basename(glob.glob("docs/adr/%s-*.md" % num)[0])
            views = ", ".join(mentions.get(num, [])) or "NONE"
            print("| [%s](../adr/%s) | %s | %s |" % (num, path, titles[num], views))
        return 0

    problems = []
    for num in sorted(titles):
        expected = mentions.get(num, [])
        if num not in rows:
            problems.append(
                "ADR %s has no row in the index (guardrail Check 7 covers this too)" % num
            )
            continue
        decision, cell = rows[num]
        actual = parse_views_cell(cell)
        if actual != expected:
            missing = [v for v in expected if v not in actual]
            extra = [v for v in actual if v not in expected]
            detail = []
            if missing:
                detail.append("missing %s" % ", ".join(missing))
            if extra:
                detail.append("lists %s which no longer cites it" % ", ".join(extra))
            problems.append("ADR %s views drifted: %s" % (num, "; ".join(detail)))
        if not title_agrees(decision, titles[num]):
            problems.append(
                "ADR %s decision cell is not its title or a truncation of it" % num
            )

    for num in sorted(rows):
        if num not in titles:
            problems.append("index row ADR %s has no matching ADR file" % num)

    if problems:
        print("decisions-index check FAILED: %d problem(s):" % len(problems))
        for problem in problems:
            print("  - %s" % problem)
        print("Regenerate the rows with: python3 scripts/check-decisions-index.py --print-table")
        return 1

    print(
        "decisions-index OK: %d ADRs, views column agrees with the %d numbered views."
        % (len(titles), len(glob.glob(VIEW_GLOB)))
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
