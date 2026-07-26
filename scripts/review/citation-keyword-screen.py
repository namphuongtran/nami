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
# Entries MUST be lowercase: the membership test lowercases the candidate, so a mixed-case
# entry can never match. "vX.Y.Z" and "CONTRIBUTING.md" sat here for a batch doing nothing,
# and both kept showing up in the report they were added to suppress.
NOISE = {
    "true", "false", "null", "text", "uuid", "jsonb", "bytea", "int", "boolean",
    "timestamptz", "src/", "public", "draft", "reviewed", "planned", "at+jwt",
    "nami.", "vx.y.z", "contributing.md", "readme.md", "main", "true.", "false.",
}


def adr_text(number: str) -> str | None:
    hits = sorted(ADR_DIR.glob(f"{number}-*.md"))
    return hits[0].read_text() if hits else None


def split_outside_code(line: str) -> list[str]:
    """Split on sentence punctuation, but never inside a backticked span.

    Splitting blindly broke on a claim value that contains a colon,
    `memberships_truncated: true`, because the split landed inside the code span and left
    the fragment with an odd number of backticks. The identifier regex then paired the
    wrong backticks and reported half a table row as an "identifier", flagging a citation
    that was correct. A false positive is a defect in the checker, and this was its cause.
    """
    parts, buf, in_code = [], [], False
    i = 0
    while i < len(line):
        ch = line[i]
        if ch == "`":
            in_code = not in_code
            buf.append(ch)
        elif not in_code and ch == "|":
            # A table cell is its own claim. Treating a whole row as one unit paired an
            # ADR cited in one cell with identifiers named in another, which is a real
            # mis-pairing report about a correct document.
            parts.append("".join(buf))
            buf = []
        elif not in_code and ch in ".;:" and i + 1 < len(line) and line[i + 1].isspace():
            buf.append(ch)
            parts.append("".join(buf))
            buf = []
            while i + 1 < len(line) and line[i + 1].isspace():
                i += 1
        else:
            buf.append(ch)
        i += 1
    if buf:
        parts.append("".join(buf))
    return parts


def blocks(text: str) -> list[tuple[int, str]]:
    """Blank-line-separated blocks, newlines flattened, keeping the first line number.

    Reading line by line was the first version and it was wrong for the same reason the
    drift screen's first version was: this layer wraps prose at about 88 characters, so a
    backticked identifier can open on one line and close on the next. The line-wise reader
    then saw an odd number of backticks and paired the wrong ones, reporting the prose
    *between* two code spans as an identifier. Blocks make a wrapped span whole again.

    A fenced block is **not** a block in this sense. Joining a DDL statement or a mermaid
    diagram into one unit produced identifiers hundreds of characters long, which is noise
    that buries the real findings. Inside a fence each line stands alone instead, so an
    `// ADR-0013` comment next to a claim name is still checked while the diagram is not.
    """
    out: list[tuple[int, str]] = []
    start, buf, in_fence = 1, [], False
    for lineno, line in enumerate(text.split("\n"), 1):
        if line.lstrip().startswith("```"):
            if buf:
                out.append((start, " ".join(buf)))
                buf = []
            in_fence = not in_fence
            continue
        if in_fence:
            if line.strip():
                out.append((lineno, line.strip()))
            continue
        if line.strip():
            if not buf:
                start = lineno
            buf.append(line.strip())
        elif buf:
            out.append((start, " ".join(buf)))
            buf = []
    if buf:
        out.append((start, " ".join(buf)))
    return out


def sentences(text: str) -> list[tuple[int, str]]:
    """Split into sentence-ish units keeping a line number, good enough for prose."""
    out: list[tuple[int, str]] = []
    for lineno, block in blocks(text):
        for part in split_outside_code(block):
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
