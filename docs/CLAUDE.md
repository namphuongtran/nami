# CLAUDE.md for `docs/`

Rules shared by all four documentation layers. The root
[`../CLAUDE.md`](../CLAUDE.md) carries the evidence rule and the content rules, which
apply here too and are not repeated. [`README.md`](README.md) in this folder is the map
of what each layer contains.

Each layer also has its own `CLAUDE.md`, and none of them restates its layer's
`README.md`. Read the README for the layer's conventions and the `CLAUDE.md` for the
traps.

## The authority order, and what each layer may not do

Three of the four layers describe the same system at different altitudes, so a
disagreement between them is always a bug in a known one of them.

1. **`adr/` decides.** Accepted ADRs are binding until superseded
   (`adr/README.md:3`).
2. **`design/` realizes.** It is "governed by the [ADR corpus], which remains the
   authority: a design doc realizes decisions, it does not make them"
   (`design/README.md:9-12`). A genuinely new decision surfaced by a design is raised
   as an ADR or a Pre-GA checklist entry, never settled inside the design
   (`design/README.md:26-29`).
3. **`architecture/` synthesizes and never introduces.** It "points into them as the
   authoritative source, and where it disagrees with one of them, this layer is the bug"
   (`architecture/README.md:31-35`).
4. **`kb/` is neither.** It holds a lesson, a how-something-works, or a gotcha that is
   not a decision. Routing rule: decision to an ADR, durable knowledge to reference to
   the KB (`kb/README.md`).

`architecture/24-glossary.md` is arc42 section 12 and defines vocabulary for **all
three** upper layers, not only for the architecture. It lives in that folder because
arc42 puts the glossary in the architecture document; an entry names the document of
record rather than owning the term, so defining `stack of record` there leaves ADR-0061
the authority. Do not narrow its scope to architecture terms.

## A document number is layer-scoped, so the same digits name different documents

`21` is performance-and-scalability in `architecture/` and CI/CD-and-deployment in
`design/`. A bare number is therefore only readable inside its own layer, and a
cross-layer reference has to be judged against its target's *directory*, not its digits.
Two consequences, both already paid for: a reader who crosses layers mis-resolves the
number, and a checker that assumes one layer reports clean on the other. Prefer the slug
form for cross-layer links, and note that a slug label encodes the number twice, so it
goes stale twice.

**Renumbering invalidates every cross-reference, including the prose ones.** A `(07)`
written in text is a citation with no link checker behind it. After any renumber, re-read
each numeric pointer against the index and confirm the *topic* matches, since a pointer
to a file that exists but is the wrong one passes every mechanical check. Both numbered
layers treat insertion as a deliberate act for this reason, and `design/` chose to
**append** its tail rather than insert (`design/README.md:31-35`).

## The dangerous citation is the one that resolves

`ADR-0062` passes every mechanical check because the file exists; what fails is that the
ADR does not contain the claim. A sweep of every `ADR-NNNN` citation in `design/` and
`architecture/` on 2026-07-29 (`cc982f3`), **2606 of them at that point**, found this class
three times, and one of them was invented outright: the
concrete Content-Security-Policy was cited to ADR-0062, which never mentions it, and **no
ADR in the corpus does**. The other two were true facts with the wrong owner, which is the
same defect at lower cost: the audit chain key cited to ADR-0009 when ADR-0008 is what
requires it, and a closure-maintenance choice cited to ADR-0024 when no ADR rules on it.

Screen by keyword overlap between the citing sentence and the cited ADR to *rank*
candidates, then read every hit, since overlap cannot tell a wrong claim written in its
ADR's own vocabulary from a right one.

**That 2606 is the size of one sweep on one day, not a live total**, and it is dated for
that reason. Re-counted on 2026-08-01 the same two layers carry **2840** citations (1247 in
`design/`, 1593 in `architecture/`). Any figure in these instruction files that was
produced by running something is a measurement with a date, so re-run it rather than citing
it forward. A stale count reported as current is the same defect as an unsourced claim,
arriving by a different route, and this file shipped one: it carried 2606 as though it were
the total until the split on 2026-08-01 re-counted it.

**A checker's anchor is part of its coverage claim.** A pattern that requires the target
to start where the link opens matches same-directory links and silently passes every
`../other-layer/` one. State what a screen does *not* match, in the screen, or its zero
will be read as absence.

## Reading the external design corpus

The corpus is a separate tree, not part of this repository, and its path comes from the
maintainer's environment. It is the source these layers were reconciled from, and it
contradicts itself in places, so how it is read matters.

**Read the root document, not its digest.** The corpus has two layers: the numbered root
documents `00` to `34`, and the `DD/` folder that summarizes them. The implementable
detail lives in the root: `DD/` carries about 1400 lines of fenced code and the root
documents carry about 2500, and `DD-24` contains **none** of the DPoP defaults that root
`24-design-dpop.md` states. Reading `DD/` first and treating it as sufficient silently
drops values (a proof-validity window, a per-client flag, an advertised algorithm set).
`DD/` is an index of what exists and which decisions apply; the root document is the
source. Where the two disagree, follow the pointer to whichever document the corpus
itself names as owner and verify there.

**The corpus states its own reading order; follow it.** Its `CLAUDE.md` defines a
**five-part bundle** per phase, "phase-doc + mini-spec + ADR + verification V-file +
register entry", and warns that a phase's information is spread across all five. It also
sets a strict layering: root `01`-`31` are the implementer source (`01`-`09` phases,
`10`-`16` cross-cutting, `17`-`31` mini-specs), `adr/` holds the decisions with
`decisions/` as their MADR conversion, `PRODUCTION-READINESS-REGISTER.md` tracks open
items by bucket (A spike, B test, C ratify, D pick), and **`knowledge-based/` is
evidence, not an implementer source**. Two things are easy to miss and both matter:
spike-proven reference code is **embedded in the mini-specs** (and runnable under
`spike-harness/`), and `reference/openiddict-source/` holds checked-in OpenIddict 7.5.0
upstream source, precisely so a claim can be read at source.

**What that reference tree actually contains**, because the count is easy to get wrong in
either direction: 25 entries, of which **23 are `.cs` files of upstream source**, one is a
sample authorization controller (the only `.cs` file besides `OpenIddictApplicationDescriptor.cs`
with no licence header, and the one whose upstream provenance is therefore not
established), and one is `tree.json`. That `tree.json` is worth knowing about: it is the
GitHub git-tree response for `openiddict/openiddict-core` at commit
`aa7fac0996cb1c86c4310a005bdc66077eb53ba8`, so a claim read out of this tree can be tied
to an exact upstream commit rather than to a version string.

The local NuGet cache carries only 7.4.0, so that tree is the only offline way to verify a
7.5.0 default. Use it: it settled `RefreshTokenReuseLeeway`'s 30-second default on first
use.

**Corpus identifiers do not resolve here.** A `doc NN`, a `task N.NN`, a spike or
verification or research label, and the corpus test-label families named by prefix in
`adr/README.md` are all external provenance. The digits never transfer between the two
repositories, and the test-label families are guardrail-rejected outright.
