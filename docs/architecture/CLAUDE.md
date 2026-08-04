# CLAUDE.md for `docs/architecture/`

The root [`../../CLAUDE.md`](../../CLAUDE.md) carries the evidence rule and the content rules.
[`../CLAUDE.md`](../CLAUDE.md) carries the layer authority order, the layer-scoped number trap,
and the resolving-citation trap. All of it applies here.

[`README.md`](README.md) in this folder is the authority on this layer's own conventions:

- the C4 levels;
- the arc42 section sequence and its attribution;
- the ISO/IEC/IEEE 42010 correspondence structure;
- the rule that this layer is the bug when it disagrees with an ADR or a design;
- the file table.

Read it first. What follows is what it does not carry.

**One constraint from that README is a licence obligation, and it is repeated here rather than
pointed at.** arc42 is CC BY-SA 4.0. This layer uses **only its section sequence**. No arc42
text, explanation, or diagram is reproduced, and the attribution in [`README.md`](README.md) is
the condition on that use. Do not paste arc42 prose into a view, and do not remove or weaken the
attribution.

## This layer links every cross-reference, and that is a property worth keeping

A sweep on 2026-07-29 (`cc982f3`) counted **155** bare cross-document pointers in `../design/`
and **zero** here. That is the whole reason this layer's cross-references are mechanically
checkable and the design layer's are not. **Write a markdown link, never a bare number**, and
write `(section 6)` when a section rather than a document is meant.

**Inserting a chapter is a deliberate act, not a casual one.** The file number is the reading
order, and the files run `01` to `24` with no gaps. So inserting one renumbers the tail and
invalidates every link into it, including the prose pointers no checker can see. The design layer
chose to **append** its tail for exactly this reason.

## Every ADR needs a row here, and the table is generated rather than written

Guardrail **Check 7** requires that every ADR have a row in
[`18-decisions-index.md`](18-decisions-index.md), bidirectionally. It exists because nine ADRs,
`0078` to `0086`, drifted out of that file while every other check stayed green.
`docs/adr/README.md` is the forward index. This one is the reverse index, and it answers "which
views must I re-read when this decision changes".

Two things follow for anyone editing a view here.

- **Section 2's table is derived from the views, not hand-maintained.** Adding or removing an ADR
  reference anywhere in a view changes what it should contain, so **run the checker**, which
  compares rather than prints:

  ```bash
  python3 scripts/check-decisions-index.py
  ```

  A spot-check of a few rows has given a false green here before. That is why the printed snippet
  that used to live in section 1 was replaced by a script that exits non-zero. **Since 2026-08-02
  that script is a CI gate and runs in the pre-commit hook.** So running it here is now about
  finding the drift before the push, rather than about whether it gets found at all. Guardrail
  Check 7 is not a substitute and never was. It passes on a row whose cells are wrong, and that was
  demonstrated rather than assumed before the wiring. **Do not write a concrete four-digit ADR
  reference into this file.** The old snippet globbed every markdown file in this folder, so one
  written here emitted `CLAUDE.md` as a phantom view. The script excludes it now, but the habit is
  what the numbered-view rule protects.
- Check 7 verifies **membership only**, never the "Views that cite it" column. Its green says
  nothing about whether the cell contents are current.

The generator's own caveat is stated at `18-decisions-index.md:40-42`. It counts **any** mention of
an ADR number in a view, including one inside that view's own `Sources` list. So a listed view is
one that *touches* the decision, not necessarily one that depends on it.

## Two things about this layer that are true but not written in its README

Both were found on 2026-08-01 by enumerating all 24 views, and both are recorded rather than
silently changed.

- **`18-decisions-index.md:11-13` says every view ends with a `Sources` section naming the ADRs it
  rests on. Twenty-three do; one does not, deliberately.**
  `11-cross-cutting-concepts.md` has no `Sources` section and does not need one. Its entire body
  is a table of owning decisions, so a second list would be the duplicate summary that file's own
  opening paragraph warns against. **`06-domain-model.md` was the other, and gained one on
  2026-08-02.** It is worth knowing how that resolved, because this entry predicted a filter and
  there was none to apply. Each of the nine inline citations (`0001`, `0003`, `0010`, `0020`,
  `0024`, `0033`, `0058`, `0059`, `0084`) was tested with the decisions index's own question:
  would this view become wrong if the decision changed? **All nine passed.** The judgement was
  still worth making rather than skipping, and it found one thing a copy would not have. The
  Core-IdP row credits ADR-0020 for the Proposal being the only rich aggregate, which is right
  about what the Proposal is. But *why it is the only one* is ADR-0059's aggregate gate, and that
  had no owner named. A `Sources` list that merely equals the inline citations should say so, or
  the next reader cannot tell reading from transcription.
- **The footer nav convention is `[Prev: Title](file) · [Index](README.md) · Next:
  [Title](file)`, once per file, no arrows.** On 2026-08-01 `06-domain-model.md` was the only file
  with two nav footers, and the only one using an arrow style. The stray one pointed `Prev` at
  `04-system-context.md` when the reading order is `05` to `06` to `07`. It was deleted in the
  same change that added this file. A duplicated footer is the cheapest instance of the general
  rule that **a document disagreeing with itself is the first place to look for a wrong pointer**.
