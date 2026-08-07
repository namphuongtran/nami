---
name: authoring-an-adr
description: Use when writing, importing, promoting, superseding, or reviewing an architecture decision record in the Nami repository. Adding one ADR touches up to five files and three of them are guardrail-enforced, so the common failure is a change that passes every check while a second index has silently drifted. Also use when a decision needs raising from a design or a code change, because a genuinely new decision is never settled inside the layer that surfaced it.
---

# Authoring an ADR here

Read this before drafting, and before promoting a status. It exists because the ADR file is the
obvious part of the change and the smallest part of the risk. Nine ADRs, `0078` to `0086`, drifted
out of a second index while every check stayed green.

This skill holds nothing that a loaded file already holds.
[`../../../docs/adr/CLAUDE.md`](../../../docs/adr/CLAUDE.md) holds the traps learned inside that
folder, and it is **not** re-injected after `/compact`, so re-read it if the session has been
compacted. [`../../../docs/adr/README.md`](../../../docs/adr/README.md) is the authority on the
format, the import range, the deferred-gate routing, and which corpus identifiers are external
provenance. [`../../../docs/CLAUDE.md`](../../../docs/CLAUDE.md) holds the layer authority order.
None of the three is restated here.

## What exists today, measured

Measured on 2026-08-07 at `10df955`. `docs/adr/` holds **96** files, `0000` through `0095`. Of
those, 90 carry `status: "accepted"` and 6 carry `status: "proposed"`. **51** carry
`stack-record: true`, so slightly more than half of the corpus also owns a row in the ADR-0061
table.

There is **no** template file. `ls docs/adr/ | grep -iE 'template|^0000'` matched only
`0000-use-markdown-architectural-decision-records.md`, which is a decision record rather than a
skeleton. It is the canonical example, and the skeleton below was read out of it.

## Step 0, which is a conversation and not a judgement call

**Confirm granularity and status with the user before drafting.** Prefer one focused ADR per
decision over a grab-bag document. `docs/adr/CLAUDE.md` states this as the first authoring
convention and calls it "a conversation step, not a judgement call to make alone".

Two consequences of getting the status wrong, both mechanical:

- A `proposed` ADR must stay implementation-open. Do not pin a specific third-party library in
  one. That is also a Check 4 trap, "since it looks like a stack entry and cannot be one".
- Promoting `proposed` to `accepted` can require adding **both** a `stack-record: true` marker and
  an ADR-0061 table row in the same change. "Neither is implied by the status edit."

## The five files, and which check catches which

`docs/adr/CLAUDE.md` calls this "the single easiest thing to get wrong". Work the list, do not
recall it.

| # | File | Enforced by | Applies when |
|---|---|---|---|
| 1 | The ADR, `docs/adr/NNNN-slug.md` | nothing; it is the thing being written | always |
| 2 | Its row in `docs/adr/README.md` | **Check 3**, both directions, and the status cell must equal the frontmatter `status:` | always |
| 3 | Its row in `docs/architecture/18-decisions-index.md` | **Check 7**, both directions | always |
| 4 | A row in `docs/adr/0061-technology-stack-of-record.md`, **and** `stack-record: true` | **Check 4**, both directions | the ADR picks a technology |
| 5 | An entry in `docs/PRE-GA-RATIFICATION-CHECKLIST.md` | **nothing mechanical** | the ADR defers a policy, a threshold, or a human sign-off |

**The row format is not cosmetic.** Both index checks match a row anchored at line start as
`| [NNNN](`. Check 3 reads the status from the row's **last** cell.

The three row shapes, read on 2026-08-07:

- `docs/adr/README.md:37` header is `| ADR | Title | Status |`.
- `docs/architecture/18-decisions-index.md:74` header is `| ADR | Decision | Views that cite it |`,
  and its link target is `../adr/NNNN-slug.md`.
- `docs/adr/0061-technology-stack-of-record.md:46` header is
  `| Layer / concern | Committed choice | Owning ADR |`, and the ADR numbers sit in the **last**
  cell, comma-separated and bare.

## Frontmatter and the section skeleton

Frontmatter, read at `docs/adr/0000-use-markdown-architectural-decision-records.md:1-7`, plus
`stack-record: true` where file 4 above applies:

```yaml
---
status: "accepted"
date: 2026-07-16
decision-makers: Nam Phuong Tran (@namphuongtran)
consulted: none (founder decision at project bootstrap)
informed: all contributors, via this repository
---
```

Sections, MADR 4.0.0 full template, read as the heading lines of that same file:

```text
# <Title>                          one H1, and the decisions index quotes it verbatim
## Context and Problem Statement
## Decision Drivers
## Considered Options
## Decision Outcome
### Consequences
### Confirmation
## Pros and Cons of the Options    one ### per option
## More Information
```

The optional sections "are kept whenever they carry real content; they may be dropped for trivial
decisions" (`0000:39`). The H1 matters beyond style, because
`scripts/check-decisions-index.py` compares the decisions-index "Decision" cell against the ADR's
own H1 title, which the index says it quotes rather than paraphrases.

## Three blind spots, so a green is not read as coverage

1. **Check 4 is blind to a shared omission.** It compares two lists both derived from this
   repository's own markup, so it catches a disagreement and not a joint absence. ADR-0061 states
   the limit in its own text: "A technology that Nami genuinely uses, but that has no row here
   **and** no marked ADR, produces two empty entries that agree perfectly, so the build passes."
   That already happened. Clustered background scheduling was load-bearing in ADR-0031 and had no
   row until 2026-07-25, guardrail green throughout.
2. **Check 7 verifies membership only.** It never reads the "Views that cite it" column.
   `scripts/check-decisions-index.py` is the rule that does, and it is a separate gate, run at
   `ci.yml:42` and by the pre-commit hook.
3. **File 5 has no mechanical check at all.** A deferral that never reaches
   `docs/PRE-GA-RATIFICATION-CHECKLIST.md` is invisible. "Deferrals are decisions, worth their own
   ADR or a checklist entry, never a silent gap" (`docs/adr/CLAUDE.md`).

## Where the generic answer is wrong here

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| Updating "the index" | There are **two** indexes plus a conditional third. "An ADR belongs to two indexes, not one" | `docs/adr/CLAUDE.md` |
| A lightweight or short-form ADR | MADR 4.0.0, **full** template | `docs/adr/README.md:5`, `0000:39` |
| Renumbering to make room, or inserting | "**never renumber an existing ADR**". The numbers are public identifiers the other two layers cited 2840 times, counted 2026-08-01 | `docs/adr/CLAUDE.md` |
| Citing an ADR number you plan to write | "**Do not forward-reference an ADR number that has not been written yet**". Check 2 fails on it across all tracked markdown | `docs/adr/CLAUDE.md` |
| Copying an imported fact verbatim | "Verify at source, do not copy verbatim ... re-check the fact and correct stale cross-references" | `docs/adr/CLAUDE.md` |
| Settling a new decision inside a design | A design "realizes decisions, it does not make them". Raise an ADR or a checklist entry | `docs/CLAUDE.md`, the authority order |
| Trusting a table over the ADR | "An index is never the authority ... When a table and an owning ADR disagree, the owning ADR wins and the table is the bug" | `docs/adr/CLAUDE.md`, and ADR-0061 says it of itself |
| A corpus identifier as a pointer | A `doc NN`, a `task N.NN`, and the spike and verification labels are external provenance that resolve to nothing here | `docs/adr/README.md`, "Identifiers borrowed from the design corpus" |
| One ADR covering several decisions | One focused ADR per decision, confirmed with the user first | `docs/adr/CLAUDE.md` |

## One conflict this skill records and does not resolve

Three sources disagree about which `status:` values exist, and no change here has exercised the
disagreement. Presenting it is the deliverable, per `CLAUDE.md`: stop and flag, quoting both sides,
rather than filling the gap from judgement.

| Source | What it says |
|---|---|
| `docs/adr/0000-use-markdown-architectural-decision-records.md:38` | Five: "`proposed`, `accepted`, `rejected`, `deprecated`, `superseded by ADR-NNNN`" |
| `docs/adr/CLAUDE.md`, under "Frontmatter" | Two: `"accepted"` or `"proposed"` |
| `scripts/check-adrs.sh:50` | Parses `^status:` with a pattern capturing a single lowercase word |

Measured 2026-08-07: only two values appear across the 96 files, 90 accepted and 6 proposed. The
consequence of the third row is concrete and untested. A frontmatter value of `superseded by
ADR-NNNN` would parse to the single word `superseded`, so the index cell would have to read exactly
`superseded` for Check 3 to pass. **That has never been run.** Before writing the first superseding
ADR, run it against a scratch file and record what happened. Do not assume either outcome.

## Verify it, in the order that makes the verification real

```bash
git add docs/adr docs/architecture/18-decisions-index.md
bash scripts/check-adrs.sh
python3 scripts/check-decisions-index.py
```

Stage first. `check-adrs.sh` reads `git ls-files`, so an unstaged file is invisible to Checks 1, 2,
5, and 6, and the script prints a `coverage warning:` above its verdict when that happens.

**One exception is easy to mistake for coverage.** Checks 2, 3, 4, and 7 enumerate ADR *files*
with on-disk globs rather than through git, so a **new ADR** is seen before it is staged. A new
design document, KB note, or `CLAUDE.md` gets no such treatment (`scripts/CLAUDE.md`).

`check-decisions-index.py --print-table` emits the correct rows for a human to apply. It never
writes to the index.

Then, after the prose is final, run `/refresh-citations` to find the pointers this change aged, and
use [`../checking-a-citation/SKILL.md`](../checking-a-citation/SKILL.md) to check that each target
supports its claim. Those are two jobs. No gate does either.

## Who owns which question

| Question | Authority |
|---|---|
| The format, the import range, and corpus identifiers | `docs/adr/README.md` |
| Traps learned inside `docs/adr/` | `docs/adr/CLAUDE.md`, not re-injected after `/compact` |
| The layer authority order, and where a new decision goes | `docs/CLAUDE.md` |
| What belongs in the stack of record, and what the check cannot see | ADR-0061 |
| Which views must be re-read when a decision changes | `docs/architecture/18-decisions-index.md`, checked by `scripts/check-decisions-index.py` |
| Deferred policies, thresholds, and human sign-offs | `docs/PRE-GA-RATIFICATION-CHECKLIST.md` |
| Naming and coding conventions an ADR may cite | ADR-0065 |
| What each guardrail check does and does not see | `scripts/README.md` |
| How to prove a new check is not inert | `.claude/skills/adding-a-ci-gate/SKILL.md` |
| Writing style, which applies to every ADR | `.claude/rules/writing-style.md` |

## Which tool reads an ADR's subject at its source

**A tool is a source, never an authority.** A vendor page does not override an accepted ADR. Where
an external source and an ADR disagree, stop and flag both with file and line.

| To read at source | Use | Why |
|---|---|---|
| A Microsoft or .NET claim | `microsoft-docs`: `microsoft_docs_search`, then `microsoft_docs_fetch` for depth | The evidence rule forbids inferring a default from a document merely near it |
| A library or SDK claim | `context7`: `resolve-library-id`, then `query-docs` | Training data can predate the pinned version, and this project pins |
| A package licence, before naming the package | The distributed artifact itself | Never a badge, and never another document in this repository. ADR-0092 records four cases where this project was wrong about a licence, in both directions |
| The external design corpus | The root document, never its digest | `docs/CLAUDE.md` records that a digest omitted every DPoP default its root document states |
