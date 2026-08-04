# CLAUDE.md for `docs/adr/`

The root [`../../CLAUDE.md`](../../CLAUDE.md) carries the evidence rule and the content rules.
[`../CLAUDE.md`](../CLAUDE.md) carries the layer authority order and how to read the external
design corpus. Both apply here, and they are not repeated.

[`README.md`](README.md) in this folder is the authority on four things:

- the format (MADR 4.0.0, full template, `NNNN-short-title-with-dashes.md`);
- the import range;
- the deferred-gate routing;
- which corpus identifiers are external provenance rather than pointers into this repository.

Read it. What follows is what it does not carry.

## Adding one ADR touches up to five files, and three of them are guardrail-enforced

This is the single easiest thing to get wrong. The ADR itself is the only obvious one, and the
checks that catch the rest have already been green while a second index drifted.

| # | File | Enforced by |
|---|---|---|
| 1 | the ADR, `docs/adr/NNNN-slug.md` | nothing; it is the thing being written |
| 2 | its row in [`README.md`](README.md) | **Check 3**, both directions, and the row's status cell must equal the frontmatter `status:` |
| 3 | its row in [`../architecture/18-decisions-index.md`](../architecture/18-decisions-index.md) | **Check 7**, both directions |
| 4 | a row in [`0061-technology-stack-of-record.md`](0061-technology-stack-of-record.md) **and** `stack-record: true` in the frontmatter, if the ADR picks a technology | **Check 4**, both directions |
| 5 | an entry in [`../PRE-GA-RATIFICATION-CHECKLIST.md`](../PRE-GA-RATIFICATION-CHECKLIST.md), if the ADR defers a policy, a threshold, or a human sign-off | nothing mechanical |

**An ADR belongs to two indexes, not one.** Check 7 exists because nine ADRs, `0078` to `0086`,
drifted out of `../architecture/18-decisions-index.md` while every other check stayed green. The
check's own comment names why that shape is dangerous: "nothing fails, so nothing is noticed"
(`scripts/check-adrs.sh:118`).

Both index checks match a row anchored at line start as `| [NNNN](`, so the row format is not
cosmetic. Check 3 reads the status from the row's **last** cell. Check 7 verifies membership only,
never the "Views that cite it" column. That column is regenerated from the views by the snippet in
that file's own section 1.

## Frontmatter

`status:` (`"accepted"` or `"proposed"`), `date`, `decision-makers`, `consulted`, `informed`, plus
`stack-record: true` where item 4 above applies. The `status` value must match the ADR's index
row, and the guardrail enforces this in both directions.

**Proposed ADRs are not stack entries and carry no marker**
(`0061-technology-stack-of-record.md:82`). So promoting an ADR from `proposed` to `accepted` can
require adding both a marker and a table row in the same change. Neither is implied by the status
edit.

## What Check 4 cannot see, so do not read its green as coverage

ADR-0061's maintenance rule is machine-enforced, and the ADR itself states the limit at
`0061-technology-stack-of-record.md:84`. The guardrail "compares two lists that are both derived
from this repository's own markup ... It therefore catches a *disagreement* between them and is
blind to a *shared omission*." A technology Nami genuinely uses, with no table row **and** no
marked ADR, produces two empty entries that agree perfectly, and the build passes.

That has already happened. Clustered background scheduling was load-bearing in ADR-0031, and it
was named in the container view and in five detailed designs. It had no row until 2026-07-25, with
the guardrail green throughout. The same ADR names the durable fix, which is a reconciliation
against
`Directory.Packages.props` once code exists at M1. Until then, "adding a row is a human step in
the same change that introduces the technology".

## Cross-references

Use `ADR-NNNN`. Every such reference must resolve to a real `docs/adr/NNNN-*.md` file, enforced by
Check 2 across **all** tracked markdown rather than only this folder. **Do not forward-reference
an ADR number that has not been written yet**, and **never renumber an existing ADR**. The numbers
are the public identifiers the other two layers cite 2840 times, counted on 2026-08-01 (1247 in
`../design/`, 1593 in `../architecture/`).

## Authoring conventions, learned the hard way

- **Confirm granularity and status with the user before drafting.** Prefer one focused ADR per
  decision over a grab-bag document. This is a conversation step, not a judgement call to make
  alone.
- **Verify at source, do not copy verbatim.** When importing or citing, re-check the fact and
  correct stale cross-references rather than transcribing them.
- **Proposed and deferred ADRs stay implementation-open.** Do not pin a specific third-party
  library in a `proposed` ADR. Record the decision and leave the mechanism open. A pinned library
  in a proposed ADR is also a Check 4 trap, since it looks like a stack entry and cannot be one.
- **Deferrals are decisions**, worth their own ADR or a checklist entry, never a silent gap.
- **An index is never the authority.** ADR-0061 says it of itself, and the decisions index says it
  of itself. When a table and an owning ADR disagree, the owning ADR wins and the table is the bug.
