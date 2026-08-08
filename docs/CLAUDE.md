# CLAUDE.md for `docs/`

Rules shared by all four documentation layers. The root [`../CLAUDE.md`](../CLAUDE.md)
carries the evidence rule and the content rules. They apply here too, and they are not
repeated. [`README.md`](README.md) in this folder is the map of what each layer contains.

Each layer also has its own `CLAUDE.md`, and none of them restates its layer's `README.md`.
Read the README for the layer's conventions, and the `CLAUDE.md` for the traps.

## The authority order, and what each layer may not do

Three of the four layers describe the same system at different heights. So a disagreement
between them is always a bug in a known one of them.

1. **`adr/` decides.** Accepted ADRs are binding until superseded (`adr/README.md:3`).
2. **`design/` realizes.** It is "governed by the [ADR corpus], which remains the authority:
   a design doc realizes decisions, it does not make them" (`design/README.md:9-12`). A
   genuinely new decision surfaced by a design is raised as an ADR or a Pre-GA checklist
   entry. It is never settled inside the design (`design/README.md:26-29`).
3. **`architecture/` synthesizes and never introduces.** It "points into them as the
   authoritative source, and where it disagrees with one of them, this layer is the bug"
   (`architecture/README.md:31-35`).
4. **`kb/` is neither.** It holds a lesson, a how-something-works, or a gotcha that is not a
   decision. The routing rule: a decision goes to an ADR, and durable knowledge to reference
   goes to the KB (`kb/README.md`).

`architecture/24-glossary.md` is arc42 section 12 and defines vocabulary for **all three** upper
layers, not only the architecture; it sits in that folder because arc42 puts the glossary in the
architecture document. An entry names the document of record rather than owning the term, so
defining `stack of record` there leaves ADR-0061 the authority. Do not narrow its scope.

## A document number is layer-scoped, so the same digits name different documents

`21` is performance-and-scalability in `architecture/` and CI/CD-and-deployment in `design/`.
So a bare number is only readable inside its own layer. A cross-layer reference has to be
judged against its target's *directory*, not its digits.

Two consequences, both already paid for. A reader who crosses layers mis-resolves the number.
And a checker that assumes one layer reports clean on the other. Prefer the slug form for
cross-layer links, and note that a slug label encodes the number twice, so it goes stale
twice.

**Renumbering invalidates every cross-reference, including the prose ones.** A `(07)` written
in text is a citation with no link checker behind it. After any renumber, re-read each numeric
pointer against the index and confirm the *topic* matches. A pointer to a file that exists but
is the wrong one passes every mechanical check. Both numbered layers treat insertion as a
deliberate act for this reason, and `design/` chose to **append** its tail rather than insert
(`design/README.md:31-35`).

## The dangerous citation is the one that resolves

`ADR-0062` passes every mechanical check because the file exists. What fails is that the ADR
does not contain the claim. A sweep on 2026-07-29 (`cc982f3`) read every `ADR-NNNN` citation in `design/` and
`architecture/`, **2606 of them at that point**. It found this class three times, and one of
them was invented outright.

The concrete Content-Security-Policy was cited to ADR-0062, which never mentions it. At that point
**no ADR fixed the directive values**; ADR-0091 does, from 2026-08-01. The tense is deliberate,
because a finding dated to a sweep should not silently become a claim about today. The other two
were true facts with the wrong owner, the same defect at lower cost: the audit chain key cited to
ADR-0009 when ADR-0008 requires it, and a closure-maintenance choice cited to ADR-0024 when no ADR
rules on it.

Screen by keyword overlap between the citing sentence and the cited ADR to *rank* candidates, then
read every hit. Overlap cannot tell a wrong claim written in its ADR's own vocabulary from a right
one.

**That 2606 is the size of one sweep on one day, not a live total**, and it is dated for that
reason. Re-counted 2026-08-01, the same two layers carry **2840** citations (1247 in `design/`,
1593 in `architecture/`). Any figure produced by running something is a measurement with a date, so
re-run it rather than citing it forward. A stale count reported as current is the same defect as an
unsourced claim by another route, and this file shipped one: it carried 2606 as the total until
2026-08-01 re-counted it.

**A checker's anchor is part of its coverage claim.** A pattern that requires the target to
start where the link opens matches same-directory links, and silently passes every
`../other-layer/` one. State what a screen does *not* match, in the screen, or its zero will be
read as absence.

**"No ADR says X" is a claim about a search, and it inherits that search's blind spots.** It
is the mirror image of the resolving citation, and it is harder to catch, because the reader
has nothing to open. `design/20-testing.md` carried "no ADR in this repository mentions
Content-Security-Policy" until 2026-08-01, while ADR-0072 named it on five lines, one of them
a binding parameter. The reason no amount of re-reading `design/20` would surface it:
**ADR-0072 spells it "Content Security Policy" unhyphenated and never abbreviates it**. So a
search for the hyphenated header name or for `CSP` returns nothing.

So before writing that nothing says X, enumerate the spellings X can take. That means the
hyphenated form, the spaced form, the abbreviated form, and the vocabulary an author would use
instead. Then **write down which searches were run**, in the claim. An absence with its method
attached can be re-checked, and a bare absence gets quoted forward. The same failure in a
single layer is recorded in `design/CLAUDE.md`. That file reached it one increment earlier, and
caught it as a near miss rather than as a committed claim.

**A search that returns zero because the tool ignored your syntax is the worst kind of
absence.** Measured 2026-08-07 over `docs/adr/0021-openiddict-version-adaptation.md`, a file
that mentions OpenIddict on 18 lines: `git grep -cE "\bOpenIddict\b"` returns nothing and exits
1, while `git grep -cP` with the same pattern returns 18, the bracket form
`(^|[^A-Za-z])OpenIddict([^A-Za-z]|$)` returns 18, and a plain substring search returns 18. So
`git grep -E` does not honour `\b` in this clone, and an absence written with that form reports
zero for every term, whether the term is present or not. It reports it in exactly the shape a
real absence takes. Use `git grep -P`, or the bracket form, or a plain substring, which
over-counts and never under-counts. Prove the method on a term you know is present before you
trust a zero, and count with `-c` rather than reading a piped list, because a truncating pipe
produces the same false confidence at one remove. **The local hook is not affected.**
`scripts/hooks/pre-commit:29` calls `grep -HniE "\b${term}\b"`, which resolves to BSD
`/usr/bin/grep`, and that binary does honour `\b`. Verified 2026-08-07 against a fixture holding
`Acme` and `AcmeCorp`, where the word-boundary form matched only the first line.

## A line number ages, and the edit that ages it is usually your own

The section above is about a citation that was wrong when it was written. This one is about a
citation that was **right** when it was written and is wrong now. They need different habits.
The first is caught by reading at source before writing. The second only by re-reading after
editing, which is the step an increment treats as finished business.

**An increment spanning more than one commit can invalidate its own pointers.** On 2026-08-03,
in the increment that added ADR-0093 and ADR-0094, three things happened in order. Commits
`c875328` and `8832769` wrote `file:line` pointers that were correct when written. Commit
`da1af46` then edited the files those pointers point into. And nothing re-read the earlier two.

Sixteen went stale at once, found only by a whole-branch review. The repair is `aa1667f`, whose
message is the record: twenty corrected and four re-confirmed, all twenty-four re-derived
against the final tree.

**No gate sees this, and that is by design rather than by oversight.** Check 2 matches
`ADR-[0-9]{4}` and confirms that `docs/adr/NNNN-*.md` exists (`scripts/check-adrs.sh`, Check
2). It never reads a line number, and neither does anything else here. Searched on 2026-08-03
across `check-adrs.sh` and `check-decisions-index.py` for a digit-and-colon pattern, for
`line number`, and for a trailing `:[0-9]+`, with no hits. So a pointer that has aged looks
exactly like a pointer that is fine, in green CI, forever.

Three habits, in the order they pay:

- **Re-derive the numbers last**, after every prose change is final. This is not a preference.
  A correction is itself an edit. Fixing numbers before prose shifts the lines below each fix
  and requires a second pass, and that second pass is the one that gets skipped.
- **Re-read every pointer into a file you touched**, not only the ones you suspect. The sixteen
  above were found by opening all of them rather than by sampling.
- **Prefer an anchor that survives an edit.** A quoted phrase, a section heading, or a named
  property still identifies its target after lines move above it. A bare number does not. Where
  the pointer must be numeric, quote enough of the target alongside it that a drift reads as
  drift instead of as a different claim.

**A pointer at a file you are deleting from is a different problem, and it needs prose, not a
number.** Five of that increment's twenty were of this kind: ADR-0093 quoted, in the present
tense, a block of `Directory.Build.props` that the same increment deleted. A sentence asserting
what another file *currently* contains is a measurement, so it is dated, written in the past
tense, and names the commit it was true at. Done that way it stays true after the target changes
again, which is the whole point.

## Reading the external design corpus

The corpus is a separate tree, not part of this repository, and its path comes from the
maintainer's environment. It is the source these layers were reconciled from and it contradicts
itself in places, so how it is read matters.

**Read the root document, not its digest.** The corpus has two layers: the numbered root
documents `00` to `34`, and the `DD/` folder that summarizes them. The implementable detail
lives in the root. `DD/` carries about 1400 lines of fenced code, and the root documents carry
about 2500. And `DD-24` contains **none** of the DPoP defaults that root `24-design-dpop.md`
states. Reading `DD/` first and treating it as sufficient silently drops values: a
proof-validity window, a per-client flag, and an advertised algorithm set. `DD/` is an index of
what exists and which decisions apply, and the root document is the source. Where the two
disagree, follow the pointer to whichever document the corpus itself names as owner, and verify
there.

**The corpus states its own reading order, so follow it.** Its `CLAUDE.md` defines a
**five-part bundle** per phase: "phase-doc + mini-spec + ADR + verification V-file + register
entry". It warns that a phase's information is spread across all five. It also sets a strict
layering:

- root `01` to `31` are the implementer source (`01` to `09` phases, `10` to `16`
  cross-cutting, `17` to `31` mini-specs);
- `adr/` holds the decisions, with `decisions/` as their MADR conversion;
- `PRODUCTION-READINESS-REGISTER.md` tracks open items by bucket (A spike, B test, C ratify, D
  pick);
- **`knowledge-based/` is evidence, not an implementer source**.

Two things are easy to miss, and both matter. Spike-proven reference code is **embedded in the
mini-specs**, and it is runnable under `spike-harness/`. And `reference/openiddict-source/`
holds checked-in OpenIddict 7.5.0 upstream source, precisely so a claim can be read at source.

**What that reference tree actually contains**, because the count is easy to get wrong in
either direction. It has 25 entries, and **23 are `.cs` files of upstream source**. One is a
sample authorization controller. That one is the only `.cs` file besides
`OpenIddictApplicationDescriptor.cs` with no licence header, so its upstream provenance is not
established. One is `tree.json`.

That `tree.json` is worth knowing about. It is the GitHub git-tree response for
`openiddict/openiddict-core` at commit `aa7fac0996cb1c86c4310a005bdc66077eb53ba8`. So a claim
read out of this tree can be tied to an exact upstream commit rather than to a version string.

The local NuGet cache carries only 7.4.0, so that tree is the only offline way to verify a
7.5.0 default. Use it: it settled `RefreshTokenReuseLeeway`'s 30-second default on first use.

**Corpus identifiers do not resolve here.** A `doc NN`, a `task N.NN`, a spike or verification
or research label, and the corpus test-label families named by prefix in `adr/README.md` are
all external provenance. The digits never transfer between the two repositories, and the
test-label families are guardrail-rejected outright.
