#!/usr/bin/env python3
"""Dump every numeric cross-reference in docs/design next to the file it resolves to.

This is a REVIEW AID, not a gate, and the distinction is the point. The design layer
cites sibling documents by bare number in prose ("DPoP internals (06)"), which no link
checker sees. The failure this exists for is a pointer that resolves to a document that
*exists* but is the *wrong* one, which happens after every renumber and which no
mechanical check can catch: judging whether "(06)" should have been "(14)" needs the
topic, not the number.

So this script does not pass or fail. It prints pointer-to-title pairs and you read
them. Deliberately kept out of scripts/check-adrs.sh: a gate that stayed green on the
bug it was written for would convert an unchecked claim into a confident one.

Usage:  python3 scripts/review/design-pointer-audit.py [file ...]
        (no arguments audits every docs/design/*.md)
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DESIGN = ROOT / "docs" / "design"

# Bare numeric pointers in prose: "(06)", "in 12", "detailed in 08", "17 and 18".
# Two shapes, because this layer uses both. The first revision of this script matched
# only "in NN" and parenthesised numbers, reported zero, and was blind to "owned by 13",
# which is the form that carried most of the stale pointers a renumber left behind. A
# checker that stays green on the class it exists for is worse than no checker.
PAREN = re.compile(r"\((\d{2})(?:\s+and\s+(\d{2}))?(?:,[^)]{0,60})?\)")
BARE = re.compile(
    r"\b(?:in|by|to|see|from|design|doc)\s+(\d{2})\b"
    r"(?!\s*[-–]|\s*(?:percent|minutes?|seconds?|hours?|days?|weeks?|months?|ms\b|%))"
)
CODE_FENCE = re.compile(r"^\s*```")
# Inside a fence, comment lines still carry real cross-references: this layer annotates
# its DDL and C# with "mechanism in 15". Skipping whole fences hid two such pointers,
# so comments are scanned and code is not.
FENCE_COMMENT = re.compile(r"^\s*(--|//|#|\*|/\*)")


def index_titles() -> dict[str, str]:
    """Map a two-digit design number to its title from the README index."""
    titles: dict[str, str] = {}
    row = re.compile(r"^\|\s*(?:\[(\d{2})\][^|]*|(\d{2}))\s*\|\s*([^|]+?)\s*\|")
    for line in (DESIGN / "README.md").read_text().split("\n"):
        m = row.match(line)
        if m:
            titles[m.group(1) or m.group(2)] = m.group(3)
    return titles


def audit(path: Path, titles: dict[str, str]) -> int:
    found: dict[str, list[int]] = {}
    in_code = False
    for lineno, line in enumerate(path.read_text().split("\n"), 1):
        if CODE_FENCE.match(line):
            in_code = not in_code
            continue
        if in_code and not FENCE_COMMENT.match(line):
            continue
        for pat in (PAREN, BARE):
            for m in pat.finditer(line):
                for num in filter(None, m.groups()):
                    found.setdefault(num, []).append(lineno)

    self_num = path.name[:2]
    try:
        shown = path.resolve().relative_to(ROOT)
    except ValueError:
        shown = path
    print(f"\n{shown}  (self: {self_num})")
    if not found:
        print("  no numeric pointers")
        return 0
    for num in sorted(found):
        title = titles.get(num)
        if title is None:
            mark, title = "UNRESOLVED", "no such row in the README index"
        elif num == self_num:
            mark, title = "self-ref", title
        else:
            mark = "->"
        lines = ", ".join(str(n) for n in found[num][:8])
        more = f" (+{len(found[num]) - 8} more)" if len(found[num]) > 8 else ""
        print(f"  ({num}) {mark:11} {title:45} lines {lines}{more}")
    return sum(1 for n in found if n not in titles)


def exhaustive(path: Path) -> None:
    """List every two-digit number with the word before it, classifying nothing.

    The pattern above encodes which phrasings this layer happens to use, which is a
    guess, and the guess has been wrong twice: the first revision missed "owned by 13",
    the second missed "for 07", "from 08", and "are 07's". This mode makes no guess. It
    is noisier and it is the one to run after a renumber.
    """
    pat = re.compile(r"(\S+)\s+(\d{2})\b")
    in_code = False
    for lineno, line in enumerate(path.read_text().split("\n"), 1):
        if CODE_FENCE.match(line):
            in_code = not in_code
        for m in pat.finditer(line):
            prev, num = m.group(1), m.group(2)
            if not ("01" <= num <= "21"):
                continue
            start = max(0, m.start() - 38)
            flag = "code" if in_code and not FENCE_COMMENT.match(line) else "    "
            print(f"  {lineno:5} {flag} [{prev} {num}] ...{line[start:m.end() + 20]}")


def main() -> int:
    titles = index_titles()
    args = [a for a in sys.argv[1:] if a != "--exhaustive"]
    if "--exhaustive" in sys.argv[1:]:
        files = [Path(a) for a in args] if args else sorted(DESIGN.glob("[0-9][0-9]-*.md"))
        for f in files:
            print(f"\n{f.name}")
            exhaustive(f)
        print(
            "\nEvery two-digit number in range, unclassified. Most are not pointers "
            "(versions, counts, RFC sections, roadmap phases). Read them all anyway: "
            "this mode exists because guessing the phrasing has failed twice."
        )
        return 0
    files = [Path(a) for a in args] if args else sorted(DESIGN.glob("[0-9][0-9]-*.md"))
    unresolved = sum(audit(f, titles) for f in files)
    print(
        f"\nRead each pointer against its title. Numbers that resolve are not "
        f"necessarily correct.\nUnresolved numbers (a real error, and the only thing "
        f"here that is mechanical): {unresolved}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
