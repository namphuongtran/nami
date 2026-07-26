#!/usr/bin/env python3
"""Report where the same load-bearing fact is stated with two different values.

The defect this exists for: a number, an ordering, or a count that is correct in one
document and quietly different in another. Vertical checks cannot see it, because each
statement resolves against its own decision; only comparing documents to each other does.
Every instance found by hand in this repository was of this shape:

  * the audit hash chain, prev-first in eight places and fields-first in one
  * the uuid-tenant-column list, five tables in the data design and four in two
    architecture chapters and one ADR
  * the resource-server invariant, three parts in one design and four in another
  * `ICheckAccess`, Phase 05 in the foundations design and Phase 06 in the roadmap

Method: for a curated register of facts, each with a pattern and the value it should
carry, report every tracked markdown file whose statement of that fact disagrees. The
register is deliberately hand-written rather than inferred, because a generic extractor
produces too much noise to read, and an unread report is worse than no report.

Adding a fact to the register is the point of maintenance here: when a review finds drift,
the fix is two commits, one for the drift and one for the register entry that would have
caught it.

This is a REVIEW AID, not a gate, on the same reasoning as the other two screens: a
minority value can be a legitimate exception, and only a reader can tell.

Usage:  python3 scripts/review/horizontal-drift-screen.py
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

# (label, expected, regex finding any statement of this fact, note)
# The regex must match BOTH the right and the wrong statement, so the disagreement shows.
REGISTER: list[tuple[str, str, str, str]] = [
    (
        "audit hash-chain operand order", "PrevHash first",
        r"HMAC[_a-zA-Z0-9-]*\(\s*(PrevHash|canonical)",
        "ADR-0008 states prev-first so an independent verifier can reproduce the chain",
    ),
    (
        "uuid-tenant-column table count", "five",
        r"v1 (?:already )?has (\w+)(?: of those)?\b|(\w+) v1 control-plane tables carry",
        "the data design is the single authority for this list",
    ),
    (
        "access-token lifetime", "15 minutes",
        r"(\d+)[- ]minute access[- ]token|access[- ]token[^.]{0,24}?(\d+)[- ]minute",
        "ADR-0004 and ADR-0005",
    ),
    (
        "refresh reuse leeway", "30 seconds",
        r"(\d+)[- ]second reuse leeway|reuse leeway[^.]{0,20}?(\d+)[- ]second",
        "ADR-0004; below the network-timeout band a retry causes a spurious logout",
    ),
    (
        "clock-skew tolerance", "60 seconds",
        r"ClockSkewTolerance[^.]{0,40}?(\d+)[- ]second|(\d+)[- ]second[^.]{0,30}ClockSkewTolerance",
        "one constant for every cross-node timestamp comparison",
    ),
    (
        "refresh absolute ceiling", "8 hours",
        r"absolute (\d+)h ceiling|(\d+)h absolute|absolute[^.]{0,16}?(\d+) hours?",
        "ADR-0004, matching the session ceiling in ADR-0003",
    ),
    (
        "OpenIddict pass-through option count", "six",
        r"(?:exactly )?(\w+) pass-through options",
        "verified at OpenIddict 7.5.0",
    ),
    (
        "OpenIddict token status count", "five",
        r"OpenIddict defines exactly (\w+) statuses|(\w+) native OpenIddict statuses",
        "verified in OpenIddictConstants.Statuses at 7.5.0",
    ),
    (
        "ADR-0043 invariant count", "eleven",
        r"ADR-0043's (three|four|five|six|seven|eight|nine|ten|eleven|twelve|\d+) invariants"
        r"|(three|four|five|six|seven|eight|nine|ten|eleven|twelve|\d+) invariants"
        r"[^.]{0,30}ADR-0043",
        "count them in the ADR before restating it",
    ),
    (
        "step-up challenge status", "401",
        r"(401|403)[^.]{0,40}?insufficient_user_authentication"
        r"|insufficient_user_authentication[^.]{0,40}?(401|403)",
        "RFC 9470: a 401, never a 403",
    ),
    (
        "schema-version gate status", "503",
        r"SchemaVersion[^.]{0,60}?(\d{3})|version-mismatched tenant with[^.]{0,20}?(\d{3})",
        "503 with Retry-After, never 404, or relying parties drop cached discovery",
    ),
    (
        "corpus build phases", "nine",
        r"which\s+has (three|four|five|six|seven|eight|nine|ten|eleven|twelve|\d+) phases",
        "the roadmap's phase-to-doc table lists 01 to 09",
    ),
]

WORD_VALUES = {
    "four": "four", "five": "five", "six": "six", "seven": "seven", "eight": "eight",
    "nine": "nine", "ten": "ten", "eleven": "eleven", "twelve": "twelve", "three": "three",
}


def tracked_markdown() -> list[Path]:
    out = subprocess.run(
        ["git", "ls-files", "*.md"], cwd=ROOT, capture_output=True, text=True, check=True
    ).stdout.split()
    return [ROOT / rel for rel in out]


def normalize(raw: str) -> str:
    low = raw.lower()
    return WORD_VALUES.get(low, low)


def blocks(text: str) -> list[tuple[int, str]]:
    """Blank-line-separated blocks with newlines flattened to spaces.

    Matching line by line was the first version and it was broken: this layer wraps prose
    at about 88 characters, so any fact spanning a wrap was invisible and the screen
    reported agreement it had not checked. That is the failure mode these screens exist to
    prevent, so it is called out here rather than fixed quietly.
    """
    out: list[tuple[int, str]] = []
    start, buf = 1, []
    for lineno, line in enumerate(text.split("\n"), 1):
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


def main() -> int:
    files = tracked_markdown()
    total_drift = 0

    for label, expected, pattern, note in REGISTER:
        rx = re.compile(pattern)
        seen: dict[str, list[str]] = {}
        for path in files:
            rel = path.relative_to(ROOT)
            for lineno, line in blocks(path.read_text()):
                for m in rx.finditer(line):
                    captured = next((g for g in m.groups() if g), None)
                    value = normalize(captured) if captured else normalize(m.group(0))
                    seen.setdefault(value, []).append(f"{rel}:{lineno}")
        if not seen:
            # No document states this fact. That is not a failure: the entry is a tripwire
            # for the day one does, which is when a count is most likely to be invented.
            print(f"  tripwire {label:36} nothing states it yet")
            continue
        norm_expected = normalize(expected.split()[0])
        odd = {v: locs for v, locs in seen.items() if not v.startswith(norm_expected)}
        agreeing = sum(len(locs) for v, locs in seen.items() if v.startswith(norm_expected))
        if odd:
            total_drift += sum(len(locs) for locs in odd.values())
            print(f"\n  DRIFT  {label}  (expected {expected}, {agreeing} sites agree)")
            print(f"         {note}")
            for value, locs in sorted(odd.items()):
                shown = ", ".join(locs[:5]) + (f" (+{len(locs) - 5})" if len(locs) > 5 else "")
                print(f"         states {value!r}: {shown}")
        else:
            print(f"  ok     {label:38} {agreeing} sites, all {expected}")

    print(
        f"\nDisagreeing statements to read: {total_drift}. A minority value can be a "
        f"legitimate exception; the register cannot tell, so read each one."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
