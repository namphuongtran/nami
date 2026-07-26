#!/usr/bin/env python3
"""Flag an ADR citation whose sentence names an identifier the ADR never mentions.

The defect this exists for: citing an ADR because its *topic* feels like the right home
for a claim, without reading it. On 2026-07-26 two design documents cited ADR-0043, the
startup-self-check ADR, for the rule that the database role must be NOSUPERUSER. ADR-0043
contains no such invariant; ADR-0037 owns it. Nothing in the repository could have caught
that, because the citation resolved to a real ADR and the claim was true. Only the pairing
was wrong, which `CLAUDE.md` names as the most common defect in this repository.

Method, deliberately narrow so the output is readable: for each sentence containing an
`ADR-NNNN` reference, take the backticked identifiers in that sentence and check whether
each appears anywhere in the cited ADR. A backticked identifier is the most checkable part
of a claim and usually the load-bearing one.

This is a REVIEW AID, not a gate. A miss is not proof of error: an ADR can own a rule
without using the same identifier, and a design can legitimately name a type the decision
never had to mention. The output is a reading list, ordered so the suspicious pairs come
first. Judging them is not mechanizable, which is why this is not wired into
check-adrs.sh.

Usage:  python3 scripts/review/citation-keyword-screen.py [file ...]
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ADR_DIR = ROOT / "docs" / "adr"
DESIGN = ROOT / "docs" / "design"

ADR_REF = re.compile(r"ADR-(\d{4})")
TICKED = re.compile(r"`([^`]+)`")
# Identifiers too generic to carry information about whether the pairing is right.
NOISE = {
    "true", "false", "null", "text", "uuid", "jsonb", "bytea", "int", "boolean",
    "timestamptz", "src/", "public", "draft", "reviewed", "planned", "at+jwt",
    "nami.", "vX.Y.Z", "CONTRIBUTING.md", "README.md", "main", "true.", "false.",
}


def adr_text(number: str) -> str | None:
    hits = sorted(ADR_DIR.glob(f"{number}-*.md"))
    return hits[0].read_text() if hits else None


def sentences(text: str) -> list[tuple[int, str]]:
    """Split into sentence-ish units keeping a line number, good enough for prose."""
    out: list[tuple[int, str]] = []
    for lineno, line in enumerate(text.split("\n"), 1):
        for part in re.split(r"(?<=[.;:])\s+", line):
            if part.strip():
                out.append((lineno, part))
    return out


def screen(path: Path) -> int:
    cache: dict[str, str | None] = {}
    flagged = 0
    rows: list[str] = []
    for lineno, sentence in sentences(path.read_text()):
        refs = sorted(set(ADR_REF.findall(sentence)))
        if not refs:
            continue
        # A claim cites one or two decisions. A sentence citing more is a references list,
        # where the identifiers belong to the document rather than to any one ADR.
        if len(refs) > 2:
            continue
        idents = [
            i for i in TICKED.findall(sentence)
            if i.lower() not in NOISE and len(i) > 3 and not i.startswith("ADR-")
        ]
        if not idents:
            continue
        for number in refs:
            if number not in cache:
                cache[number] = adr_text(number)
            body = cache[number]
            if body is None:
                rows.append(f"  {lineno:5} ADR-{number} DOES NOT EXIST")
                flagged += 1
                continue
            missing = [i for i in idents if i not in body]
            if missing and len(missing) == len(idents):
                rows.append(
                    f"  {lineno:5} ADR-{number} mentions none of: {', '.join(missing[:4])}"
                )
                flagged += 1
    if rows:
        print(f"\n{path.name}")
        print("\n".join(rows))
    return flagged


def main() -> int:
    args = sys.argv[1:]
    files = [Path(a) for a in args] if args else sorted(DESIGN.glob("[0-9][0-9]-*.md"))
    total = sum(screen(f) for f in files)
    print(
        f"\nSuspicious citation pairings to read: {total}. A pairing is suspicious when the "
        f"cited ADR mentions none of the identifiers in the sentence citing it. Read each "
        f"one; some are legitimate."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
