---
name: checking-a-citation
description: Use when verifying that a citation supports the claim attached to it, before committing prose that cites an ADR number or a file and line, after editing a file that other documents point into, and whenever writing that no source says something. No gate in this repository reads a claim against its cited source, so this is the manual procedure that stands in for the missing check. The failure mode is not a broken link. It is a pointer that resolves to a real file which does not hold the claim.
---

# Checking a citation here

Read this before asserting that a source says something, and before writing that no source does.
It exists because the citation defect this repository keeps producing passes every mechanical
check it has.

This skill holds nothing that a loaded file already holds. The root
[`../../../CLAUDE.md`](../../../CLAUDE.md) carries the evidence rule and is injected every session
and after `/compact`. [`../../../docs/CLAUDE.md`](../../../docs/CLAUDE.md) carries the two
sections this procedure operationalizes, "The dangerous citation is the one that resolves" and "A
line number ages, and the edit that ages it is usually your own". Neither is restated here. What
follows is the sequence: what to open, in what order, and how to record the result.

## Why there is a procedure and not a gate

`scripts/check-adrs.sh` Check 2 matches a four-digit ADR reference and confirms that
`docs/adr/NNNN-*.md` exists. It never opens the file. `docs/CLAUDE.md` records the search behind
that claim: on 2026-08-03, across `check-adrs.sh` and `check-decisions-index.py`, for a
digit-and-colon pattern, for `line number`, and for a trailing colon-digits pattern, with no hits.

So a pointer that has aged, and a pointer that was always wrong, both look exactly like a pointer
that is fine, in green CI, forever.

## What exists today, measured

Measured on 2026-08-07, with `git grep -ohE 'ADR-[0-9]{4}'` counting occurrences including
repeats, over the top level of each folder: `docs/design/` carries **1416** ADR citations and
`docs/architecture/` carries **1659**.

That is the size of one count on one day, not a live total. The method is written into the claim
so it can be re-run rather than quoted forward. `docs/adr/CLAUDE.md` carries 2840 counted
2026-08-01 by its own method, and a different number is what a later count is expected to produce.

**The count of file-and-line pointers is owned elsewhere, so it is not repeated here.**
[`../../commands/refresh-citations.md`](../../commands/refresh-citations.md) carries it with its
own pattern. One number for one thing, in one place.

## The four defect classes

Each has a recorded instance, so none of these is hypothetical.

| Class | What it looks like | The recorded instance |
|---|---|---|
| **Resolves, claim absent** | The file exists and does not contain the claim | A concrete Content-Security-Policy cited to ADR-0062, "which never mentions it". At that point no ADR fixed the directive values; ADR-0091 does, from 2026-08-01 |
| **True fact, wrong owner** | The claim is correct and the pointer names the wrong decision. "The same defect at lower cost" | The audit chain key cited to ADR-0009 when ADR-0008 is what requires it |
| **Invented outright** | Support manufactured for a correct-sounding claim | One of the three found in the 2026-07-29 sweep (`cc982f3`), which read every ADR citation in two layers |
| **Was right, has aged** | Correct when written, wrong now, usually because of your own edit | Sixteen went stale at once in the increment that added ADR-0093 and ADR-0094; the repair is `aa1667f`, twenty corrected and four re-confirmed |

The first three are caught by reading at source **before** writing. The fourth is caught only by
re-reading **after** editing, which is the step an increment treats as finished business. They need
different habits, so do not run one pass and call it both. The fourth also has its own command,
`/refresh-citations`, and the section below hands off to it.

## Two shapes to suspect before any tool runs

From the root `CLAUDE.md` evidence rule, and worth checking by eye first because they are cheap:

1. **A citation at the end of a compound sentence.** The pointer attaches to one clause and reads
   as covering all of them. Split the sentence and ask which clause the source actually holds.
2. **A bundle of items behind one reference.** A list of five with one citation at the end is five
   claims, and the source usually holds three of them.

## The procedure

Work it in this order. Steps 1 and 2 are cheap and remove most candidates.

1. **Read the citing sentence and name the claim it makes.** Write the claim as one sentence with
   one idea. A compound claim cannot be checked as a unit.
2. **Screen by keyword overlap to rank candidates, then read every hit.** `docs/CLAUDE.md` states
   the limit in the same breath: "Overlap cannot tell a wrong claim written in its ADR's own
   vocabulary from a right one." Ranking is what overlap is for. It is not a verdict.
3. **Open the cited file and find the sentence that holds the claim.** If you cannot quote it,
   the citation has failed, whatever the file contains near it.
4. **Ask whether the source is the owner.** A true fact with the wrong owner is still a defect. If
   another file states it and the cited one merely repeats it, cite the owner.
5. **Record the finding with both sides quoted at file and line, and show it before editing.** The
   evidence rule is explicit: "Present both sides with file and line before editing. The user
   decides on evidence, not on a summary."

## Writing that no source says something

An absence claim is a claim about a search, and it inherits that search's blind spots. It is the
harder half, because the reader has nothing to open.

The worked case: `docs/design/20-testing.md` carried "no ADR in this repository mentions
Content-Security-Policy" until 2026-08-01, while ADR-0072 named it on five lines, one of them a
binding parameter. No amount of re-reading the design would have surfaced it, because ADR-0072
spells it unhyphenated and never abbreviates it. A search for the hyphenated header name, or for
the three-letter abbreviation, returns nothing.

So, before writing that nothing says X:

1. **Enumerate the spellings X can take.** The hyphenated form, the spaced form, the abbreviated
   form, and the vocabulary an author would use instead of the term.
2. **Run each one, and note which folders each covered.**
3. **Write the searches into the claim**, not into a commit message. "An absence with its method
   attached can be re-checked, and a bare absence gets quoted forward."
4. **Name the near misses.** A hit that turned out to be a different sense is worth listing, so a
   later reader does not re-find it and read it as coverage.

## Re-reading after your own edit

**Class 4 has its own command, so run it rather than improvising.** `/refresh-citations` finds the
pointers an increment aged, and its steps carry the "re-derive last" rule, the
open-all-not-a-sample rule, and the three-way report that separates drifted from gone from
re-confirmed. Read
[`../../commands/refresh-citations.md`](../../commands/refresh-citations.md). Nothing in it is
repeated here.

What that command does **not** carry is the writing habit that reduces the work next time.
**Prefer an anchor that survives an edit.** A quoted phrase, a section heading, or a named property
still identifies its target after lines move above it. A bare number does not. Where the pointer
must be numeric, quote enough of the target alongside it that a drift reads as drift instead of as
a different claim (`docs/CLAUDE.md`).

The two jobs are separate on purpose. `/refresh-citations` answers "did this pointer move". This
skill answers "does the target support the claim". A pointer resolving to the right line is no
evidence at all about the second question, and that command says so about itself.

## Where the generic answer is wrong here

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| Confirming the link resolves | "The dangerous citation is the one that resolves ... a green checker is not evidence about what a citation says" | `CLAUDE.md`, evidence rule |
| A keyword-overlap score as the verdict | Ranking only. Overlap cannot separate a wrong claim in the right vocabulary from a right one | `docs/CLAUDE.md` |
| Editing the document so the checker passes | Never. Fix the checker, record the finding as legitimate, or delete the checker. `scripts/review/` was deleted on 2026-07-27 for exactly this, and one of its three screens had caused a document to be weakened | `CLAUDE.md`, and `scripts/CLAUDE.md` |
| Treating a checker's false positive as noise | "A false positive in a checker is a defect in the checker" | `CLAUDE.md`, evidence rule |
| Reading "X is set to Y" as telling you the default | Two claims, and the second needs a second source. "A stated value is not a known default" | `CLAUDE.md`, evidence rule |
| Quoting a count forward | Any figure produced by running something is a measurement with a date. Re-run it. Never edit a dated measurement to match today, because that stops it being evidence | `CLAUDE.md`, evidence rule |
| A bare number in a cross-layer reference | A document number is layer-scoped. `21` is performance in `architecture/` and CI/CD in `design/`, so judge a pointer against its target's directory | `docs/CLAUDE.md` |
| Writing "three sources agree" | Name them. "Write in file and line instead of a total you did not run" | `CLAUDE.md`, evidence rule |
| Filling a gap when the sources disagree | Stop and flag it, quoting both sides at file and line. Saying "not verified" out loud is the deliverable | `CLAUDE.md` |

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is written
into it. Both were run on 2026-08-07 with `git grep -in` over every tracked file.

- **No tool checks a claim against its source, and no document plans one.** `citation check`,
  `citation screen`, `claim check`, `citation sweep`, and `citation audit` returned **zero** hits
  each. `citation-keyword` returned one, `scripts/CLAUDE.md:107`, which names
  `citation-keyword-screen.py` as one of the three screens deleted from `scripts/review/` on
  2026-07-27. `.claude/commands/gate.md:66` states the position directly: a green gate "does not
  cover whether a citation that resolves actually supports its claim, which no tool here checks."
  So the absence is a recorded decision rather than a gap. Whether a replacement is ever built is
  undecided, and building one would have to answer why the last one bent the evidence it existed
  to protect.
- **No cadence exists for a repository-wide sweep.** `sweep` returned 11 hits, all of them records
  of a sweep already run: the citation sweep of 2026-07-29 at `docs/CLAUDE.md:54`, a glossary sweep
  of 2026-08-01 at `docs/architecture/24-glossary.md:244`, and a design-layer sweep of the same
  date at `docs/adr/0027-packaging-and-distribution.md:82`. None schedules a future one. `periodic`
  and `quarterly` returned 23 and 18 hits, and every one read is a disaster-recovery drill, a key
  rotation, or a consent-lifetime question, not a documentation sweep.

## Who owns which question

| Question | Authority |
|---|---|
| The evidence rule itself | Root `CLAUDE.md`, injected every session |
| The two citation sections this procedure runs | `docs/CLAUDE.md` |
| The layer authority order, so you can tell which side is the bug | `docs/CLAUDE.md` |
| Which views must be re-read when a decision changes | `docs/architecture/18-decisions-index.md`, checked by `scripts/check-decisions-index.py` |
| Cross-references between ADRs, and never renumbering | `docs/adr/CLAUDE.md`, and `.claude/skills/authoring-an-adr/SKILL.md` |
| Corpus identifiers, which resolve to nothing here | `docs/adr/README.md` |
| What each guardrail check does, and what it does not see | `scripts/README.md` |
| Why deleting a checker is an available remedy | `scripts/CLAUDE.md` |
| Writing style | `.claude/rules/writing-style.md` |

## Which tool reads a claim at its source

**A tool is a source, never an authority.** Where an external source and an accepted ADR disagree,
stop and flag both with file and line, and do not fill the gap from judgement.

| To read at source | Use | Why |
|---|---|---|
| A claim inside this repository | `Read` the file at the cited lines, and `git grep` for the spellings | An excerpt returned by a search is not the sentence; open the file |
| A Microsoft or .NET claim | `microsoft-docs`: `microsoft_docs_search`, then `microsoft_docs_fetch` for depth | The rule forbids inferring a default from a document merely near it |
| A library or SDK claim | `context7`: `resolve-library-id`, then `query-docs` | Training data can predate the pinned version |
| A package licence | The distributed artifact itself | Never a badge, and never another document in this repository |
| The external design corpus | The root document, never its digest, and `reference/openiddict-source/` for an upstream claim | A digest omitted every DPoP default its root document states |

When a search returns nothing, that is a result about the search. Write down what you ran.
