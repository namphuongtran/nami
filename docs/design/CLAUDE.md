# CLAUDE.md for `docs/design/`

The root [`../../CLAUDE.md`](../../CLAUDE.md) carries the evidence rule and the content
rules; [`../CLAUDE.md`](../CLAUDE.md) carries the layer authority order, the layer-scoped
number trap, the resolving-citation trap, and how to read the external design corpus. All
of it applies here and is not repeated.

[`README.md`](README.md) in this folder is the authority on this layer's own conventions:
its altitude (C4 level 4 plus the internal contract), the IEEE 1016 viewpoint sequence,
the decision rule that sends a genuinely new decision to an ADR, and the **two numbering
axes**, a bare number being a document in this layer while `Phase NN` is a corpus build
phase. Read it first. What follows is only what it does not carry, which is what a sweep
of this layer's cross-references found.

## This is the layer with bare numeric pointers, and that is where its defects are

Two separate sweeps, two days apart, both recorded in git:

- **2026-07-27** (`895edd4`) counted **271** bare numeric pointers in this folder and found
  **five wrong**.
- **2026-07-29** (`cc982f3`) counted **155** bare cross-document pointers here and **zero**
  in `../architecture/`, which links every cross-reference as a markdown link.

Every figure above is the size of one sweep on one day, not a live total. Re-run before
citing any of them forward.

The invariant those numbers support is the actionable part:

- **Prefer a markdown link to a bare number.** The architecture layer is the proof that a
  layer can hold to it. A link is checkable by a tool and a bare number is not.
- **Write `(section 6)` when a section is meant.** No screen can distinguish `(6)` meaning
  document `06` from `(6)` meaning section 6 without reading, and two of the sweep's
  ambiguous hits were exactly that.

## Auditing a numeric pointer: start with the file's self-contradictions

The cheapest signal in every one of the five wrong pointers was **a document disagreeing
with itself**:

- `13` gave path (c) to `06` in its table and to `08` in the heading eight lines later.
- `08` cited the email subsystem as `10` in prose and `07` inside a mermaid participant.

**Judge a pointer against the target's *topic*, never against the fact that the file
exists.** Two of the five sat where no link checker can reach, one inside a fenced block
and one as a bare number in prose.

**Expect heavy regex noise, and never let the extractor's count stand in for the
judgement.** `Art.17(3)`, `AC-2(2)`, `PostgreSQL 18`, `.NET 10`, `FromHours(8)`,
`FromMinutes(15)`, `section N`, `runtime view N`, and `p(95)` all match a
document-pointer pattern. Extract by machine, then read every hit.

## Two counts in this folder that are known not to reconcile

Recorded here so the next reader does not spend the sweep again, and **not** silently
adjusted, because it is not established which reading was intended:

- `22-openiddict-seam-catalogue.md:419` states that **nine** of the thirty-seven seams
  have no contract test yet, while the two "to build" rows in that section list **eight**
  (S11 to S15, and S25, S26, S31).
- Seams S5, S28, S29, S30 and S32 appear nowhere in that document's section 9 table.

Resolving either one means deciding what the section is counting, which is a change to the
document's claim rather than a typo fix.
