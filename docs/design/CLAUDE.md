# CLAUDE.md for `docs/design/`

The root [`../../CLAUDE.md`](../../CLAUDE.md) carries the evidence rule and the content rules.
[`../CLAUDE.md`](../CLAUDE.md) carries the layer authority order, the layer-scoped number trap,
the resolving-citation trap, and how to read the external design corpus. All of it applies here,
and it is not repeated.

[`README.md`](README.md) in this folder is the authority on this layer's own conventions:

- its altitude (C4 level 4 plus the internal contract);
- the IEEE 1016 viewpoint sequence;
- the decision rule that sends a genuinely new decision to an ADR;
- the **two numbering axes**. A bare number is a document in this layer, while `Phase NN` is a
  corpus build phase.

Read it first. What follows is only what it does not carry, which is what a sweep of this layer's
cross-references found.

## This is the layer with bare numeric pointers, and that is where its defects are

Two separate sweeps, two days apart, both recorded in git:

- **2026-07-27** (`895edd4`) counted **271** bare numeric pointers in this folder and found
  **five wrong**.
- **2026-07-29** (`cc982f3`) counted **155** bare cross-document pointers here and **zero** in
  `../architecture/`, which links every cross-reference as a markdown link.

Every figure above is the size of one sweep on one day, not a live total. Re-run before citing
any of them forward.

The invariant those numbers support is the actionable part.

- **Prefer a markdown link to a bare number.** The architecture layer is the proof that a layer
  can hold to it. A link is checkable by a tool, and a bare number is not.
- **Write `(section 6)` when a section is meant.** No screen can distinguish `(6)` meaning
  document `06` from `(6)` meaning section 6 without reading, and two of the sweep's ambiguous
  hits were exactly that.

## Auditing a numeric pointer: start with the file's self-contradictions

The cheapest signal in every one of the five wrong pointers was **a document disagreeing with
itself**:

- `13` gave path (c) to `06` in its table and to `08` in the heading eight lines later.
- `08` cited the email subsystem as `10` in prose and `07` inside a mermaid participant.

**Judge a pointer against the target's *topic*, never against the fact that the file exists.**
Two of the five sat where no link checker can reach, one inside a fenced block and one as a bare
number in prose.

**Expect heavy regex noise, and never let the extractor's count stand in for the judgement.**
`Art.17(3)`, `AC-2(2)`, `PostgreSQL 18`, `.NET 10`, `FromHours(8)`, `FromMinutes(15)`,
`section N`, `runtime view N`, and `p(95)` all match a document-pointer pattern. Extract by
machine, then read every hit.

## The count that would not reconcile, resolved 2026-08-01, and how it resolved

This section used to record two unreconciled counts in `22-openiddict-seam-catalogue.md` section
9. It claimed **nine** seams had no contract test while its rows named **eight**. And five seams
(S5, S28, S29, S30, S32) appeared in no row at all. It said resolving either meant deciding what
the section was counting rather than fixing a typo. That was right, and the resolution is worth
keeping, because **the arithmetic was the wrong end to pull**.

- **The nine was correct and the rows were incomplete.** S5 carries a `Test` column naming a
  "key-load test", and that string occurs nowhere else in the repository. `12-key-management.md`
  section 9 lists eight test groups to build, and none of them loads a certificate. So S5 is the
  ninth untested seam, and its row had never been written. Lowering nine to eight, which is what
  checking the arithmetic alone suggests, would have deleted a real gap.
- **The five were absent for four different reasons, so "five seams appear in no row" was one
  finding only in shape.** One was the omission above. S30 is a grouping over S6 to S9 and would
  double-count. S29's condition is not met, self-service client registration being v2.1. S28 and
  S32 are tested elsewhere, as an acceptance test and in `07-authorization.md` section 9
  respectively.
- **What was actually broken was a claim, not a number.** Section 9 said it mapped **one to one**
  onto the registry. It covered thirty-two of thirty-seven, and not even the same thirty-two, since
  it dropped S5 and added S31. Five carried no `Test` column at all, because the build-interims
  table has different columns.
- **Every figure in this section is that day's, and the registry moved later the same day.** It
  went to thirty-eight rows, and thirty-three with a `Test` column, when the `form_post` response
  markup was registered as S36. The five interims are the part that did not move. They are also the
  only part worth carrying forward as a shape rather than a number. A row with no `Test` column is
  not an untested seam. It is a row in a table with different columns, and conflating the two is
  what started this.

Two habits generalise from it. **A count and a list are different artifacts, and disagreeing means
one of them was derived and the other transcribed.** Find out which before trusting either. And
**an absence proved by grep is only as good as the pattern.** S32 was nearly filed as an untested
seam. A search for the word actor, and for `act` followed by a space, missed an assertion that
writes `` `act` `` in backticks under a "confused deputy" label. That near miss is recorded in that
document's section 10.

Note also that the pointer this section used to carry, `:419`, had drifted to section 8 by the
time it was resolved. **Prefer a section number to a line number when citing inside this
folder**, since these documents grow in the middle.
