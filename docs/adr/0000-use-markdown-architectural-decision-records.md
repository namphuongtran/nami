---
status: "accepted"
date: 2026-07-16
decision-makers: Nam Phuong Tran (@namphuongtran)
consulted: none (founder decision at project bootstrap)
informed: all contributors, via this repository
---

# Use Markdown Architectural Decision Records (MADR) with the full template

## Context and Problem Statement

Nami is built decision-first: its architecture was designed as a corpus of 35 architecture decision records before the first line of product code, and "decisions are public" is a stated project principle. As those decisions are imported into this repository and new ones are made, we need one consistent, reviewable, tooling-friendly format for recording them. Which ADR format should the project standardize on?

## Decision Drivers

* Decisions must be public and reviewable by the community: format should be plain markdown, diffable in pull requests, and renderable on GitHub and the future docs site.
* The project's working discipline requires options to be presented with tradeoffs before a decision is made; the format should force "considered options" and "pros and cons" to be written down, not implied.
* 35 existing ADRs will be imported and translated; the format must accommodate rich context, evidence links, and consequence notes without loss.
* A widely adopted convention lowers the barrier for new contributors and works with existing tooling (log4brains, adr-tools style generators, IDE templates).
* Records need machine-readable metadata (status, date, deciders) for indexing.

## Considered Options

* MADR 4.0.0, full template
* MADR 4.0.0, minimal template
* Nygard-style ADRs (original 2011 format)
* Free-form design documents, no fixed ADR format

## Decision Outcome

Chosen option: "MADR 4.0.0, full template", because it is the only option that structurally enforces the project's present-options-with-tradeoffs discipline (dedicated Considered Options and Pros and Cons sections), carries YAML metadata for indexing, and is a widely adopted community standard with tooling support.

Conventions adopted with it:

* ADRs live in `docs/adr/`, named `NNNN-short-title-with-dashes.md` with a four-digit, monotonically increasing number.
* This ADR is `0000`. The 35 ADRs imported from the original design corpus keep their numbers one-to-one: original ADR-01 becomes `0001`, ..., ADR-35 becomes `0035`. New decisions continue from `0036`.
* Statuses: `proposed`, `accepted`, `rejected`, `deprecated`, `superseded by ADR-NNNN`. Accepted ADRs are binding until superseded; a decided behavior changes only through a superseding ADR.
* Optional template sections (Decision Drivers, Consequences, Confirmation, Pros and Cons, More Information) are kept whenever they carry real content; they may be dropped for trivial decisions.
* `docs/adr/README.md` is the index of all ADRs and is updated in the same pull request as any ADR change.

### Consequences

* Good, because every decision, including this one, shows its alternatives and reasoning in a uniform place, which doubles as project governance documentation.
* Good, because imported ADRs and new ADRs will look identical to readers and tools.
* Bad, because reformatting the 35 imported ADRs into MADR sections adds translation effort per document.
* Bad, because the full template is heavyweight for small decisions; mitigated by allowing optional sections to be dropped.

### Confirmation

Pull request review confirms compliance: any PR adding or changing an ADR must follow the template, update the index in `docs/adr/README.md`, and use the next free number. Nonconforming ADRs are not merged.

## Pros and Cons of the Options

### MADR 4.0.0, full template

The [MADR](https://adr.github.io/madr/) full template, as published at `adr/madr` tag 4.0.0.

* Good, because Considered Options and Pros and Cons are first-class sections, matching how this project makes decisions.
* Good, because YAML frontmatter (status, date, decision-makers) enables indexing and tooling.
* Good, because MADR is a maintained community standard with an ecosystem (log4brains, IDE snippets, generators).
* Neutral, because optional sections give flexibility but rely on author judgment.
* Bad, because it is the most verbose option; trivial decisions carry template overhead.

### MADR 4.0.0, minimal template

Same convention, but only Context, Considered Options, and Decision Outcome.

* Good, because low friction for small decisions.
* Bad, because it omits Pros and Cons and Consequences, exactly the sections that encode this project's evidence-and-tradeoffs discipline.
* Bad, because the imported ADRs contain rich rationale that would have no natural home.

### Nygard-style ADRs (original 2011 format)

Michael Nygard's Title/Status/Context/Decision/Consequences format.

* Good, because simple, widely known, and the historical default.
* Neutral, because prose-only structure works well for narrative decisions.
* Bad, because it has no explicit Considered Options or Pros and Cons sections; alternatives end up buried in prose or omitted.
* Bad, because no structured metadata for indexing.

### Free-form design documents, no fixed ADR format

Keep writing design docs of arbitrary shape.

* Good, because zero constraints on authors.
* Bad, because decisions become hard to find, compare, and supersede; no status lifecycle.
* Bad, because contradicts the stated project principle that decisions are recorded and public in a uniform way.

## More Information

* MADR project: <https://adr.github.io/madr/>, template used: <https://github.com/adr/madr/blob/4.0.0/template/adr-template.md>
* The original design corpus (35 ADRs plus phase docs and verification records) predates this repository; ADRs are being imported, translated to English, and reformatted to MADR one by one, each reviewed and approved by the maintainer before merge.
* Revisit if the template proves too heavy in practice; a superseding ADR may adopt the minimal template for defined categories of decisions.
