# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

Nami is an open-source, multi-tenant OAuth 2.0 / OpenID Connect identity provider
for .NET, built on OpenIddict, an Apache-2.0 alternative to commercial identity
servers. It is in **pre-alpha**: the architecture is fully designed and its risk
spikes were validated with runnable code, but that code lives in a separate design
corpus. This repo currently holds the **decision records, governance, and docs
scaffolding**; application source lands under `src/` starting at milestone M1
(`src/.gitkeep` is the placeholder). The only executable code today is the docs
guardrail (`scripts/`).

Because the product is expressed as decisions, the ADR corpus in `docs/adr/` **is**
the architecture. Read the relevant ADRs before proposing changes to behavior they
govern, accepted ADRs are binding until superseded.

## Commands

```bash
# Docs guardrail: the CI gate. Must pass before any docs/ADR change merges.
bash scripts/check-adrs.sh

# Markdown lint: same pinned version CI runs (do not float this version).
npx --yes markdownlint-cli2@0.23.0 "**/*.md"

# Enable the opt-in local pre-commit hook (guardrail + local name-scrub). Per clone.
git config core.hooksPath scripts/hooks
```

There is no build or test suite yet; the .NET build/test/license-scan CI gates are
added when the solution lands (see the comment at the end of `.github/workflows/ci.yml`).

## The ADR corpus (the core of the repo)

- **Format:** [MADR 4.0.0](https://adr.github.io/madr/) full template. Start from
  `docs/adr/0000-*.md`. Files are `NNNN-short-title-with-dashes.md`.
- **Numbering:** `0000`–`0035` were imported one-to-one from the original design
  corpus and keep their original numbers; **new decisions continue from `0036`**.
  Never renumber an existing ADR.
- **Frontmatter** carries `status:` (`"accepted"` or `"proposed"`), `date`,
  `decision-makers`, `consulted`, `informed`. The `status` value must match the
  ADR's row in the index; the guardrail enforces this.
- **Index:** every ADR has a row in `docs/adr/README.md` with a Status column.
  Adding an ADR means adding its index row in the same change.
- **Deferred gates:** several ADRs defer a policy, threshold, or human sign-off to
  before GA. Those are consolidated in `docs/PRE-GA-RATIFICATION-CHECKLIST.md`.
  When an ADR defers something, add or update its checklist entry.
- **Cross-references** use `ADR-NNNN`. Every such reference must resolve to a real
  `docs/adr/NNNN-*.md` file (guardrail-enforced). Do not forward-reference an ADR
  number that has not been written yet.

### Authoring conventions for ADRs (learned constraints)

- **Verify at source, don't copy verbatim.** When importing or citing, re-check the
  fact and correct stale cross-references rather than transcribing.
- **Proposed / deferred ADRs stay implementation-open.** Do not pin a specific
  third-party library in a `proposed` ADR ("consider to build later if needed"):
  record the decision, leave the mechanism open.
- **Deferrals are decisions** worth their own ADR or a checklist entry, not silent gaps.
- Confirm granularity and status with the user before drafting; prefer one focused
  ADR per decision over grab-bag documents.

## Evidence rule (non-negotiable, applies to every layer)

**Never write a claim you have not read at its source, and never infer one from a
title, a filename, or a neighbouring document.** This rule is absolute and outranks
fluency: an unsourced sentence that reads well is worse than an omission, because a
later reader cannot tell the two apart.

- **Quote before you assert.** Before writing that a document says X, open it and
  read the line. Citing `ADR-NNNN` for a fact means that fact is *in* `ADR-NNNN`,
  not merely adjacent to its subject. Attribution is part of the claim: a true fact
  with the wrong owner is a defect, and it has been the single most common defect in
  this repository.
- **Show the evidence before making the change.** When reconciling, contradicting, or
  correcting anything, present both sides with file and line before editing. The user
  decides on evidence, not on a summary.
- **Count what you counted.** Never write "the second time", "three sources agree",
  "every X" unless you have enumerated them. Say "in `file:line`" instead of a tally
  you did not run.
- **Where this repo says more than its source, name the real source.** An addition
  that improves on the corpus still needs a verifiable origin. Inventing support for
  a correct-sounding claim is the worst failure mode available here.
- **Say "not verified" out loud.** Uncertainty is reportable and cheap; a fabricated
  citation is neither. If a fact cannot be sourced, either leave it out or mark it
  explicitly as an open item.
- **A stated value is not a known default.** A source that says "X is set to Y" tells you
  nothing about what X would be if nobody set it. Writing "Y is set explicitly because the
  default is weaker" is a *second* claim needing a *second* source, and it is the shape
  self-generated errors take here: the source stated a value, the rationale was invented
  around it. Read the default, or say only what the source says.
- **Renumbering invalidates every cross-reference, including the prose ones.** A `(07)`
  written in text is a citation with no link checker behind it. After any renumber, re-read
  each numeric pointer against the index and confirm the *topic* matches, since a pointer
  to a file that exists but is the wrong one passes every mechanical check.
- **A document number is layer-scoped, so the same digits name different documents.** `21`
  is performance-and-scalability in `docs/architecture/` and CI/CD-and-deployment in
  `docs/design/`. A bare number is therefore only readable inside its own layer, and a
  cross-layer reference has to be judged against its target's *directory*, not its digits.
  Two consequences, both already paid for: a reader who crosses layers mis-resolves the
  number, and a checker that assumes one layer reports clean on the other. Prefer the
  slug form for cross-layer links, and note that a slug label encodes the number twice,
  so it goes stale twice.
- **Audit a numeric pointer against the target's *topic*, and start with the file's
  self-contradictions.** A sweep of all 271 bare numeric pointers in `docs/design/` found
  five wrong, and the cheapest signal in every case was a document disagreeing with itself:
  `13` gave path (c) to `06` in its table and to `08` in the heading eight lines later, and
  `08` cited the email subsystem as `10` in prose and `07` inside a mermaid participant.
  Two of the five sat where no link checker can reach, one inside a fenced block and one as
  a bare number in prose. Also expect regex noise, since `Art.17(3)`, `AC-2(2)`,
  `PostgreSQL 18`, `.NET 10`, and `FromHours(8)` all look like pointers: extract by machine,
  then judge every hit by reading, and never let the extractor's count stand in for the
  judgement.
- **A checker's anchor is part of its coverage claim.** A pattern that requires the target
  to start where the link opens matches same-directory links and silently passes every
  `../other-layer/` one. State what a screen does *not* match, in the screen, or its zero
  will be read as absence.
- **Never edit a document to silence a checker.** This is the rule the deleted `scripts/review/`
  screens were removed for breaking: a keyword check flagged a correct citation, and the
  response was to drop `: true` from the claim value `memberships_truncated: true` so the
  check would pass. The document became less implementable to make a tool quieter. If a
  checker is wrong, fix the checker or record the finding as legitimate; if neither is cheap,
  delete the checker. A tool that bends the evidence it exists to protect is worse than none.
- **A false positive in a checker is a defect in the checker**, not noise to work
  around, and the same holds for a claim that survives only because nobody checked it.
  A checker that stays green on the bug it was written for is worse than none, because it
  converts an unchecked claim into a confident one.
- **Read the corpus root document, not its digest.** The design corpus has two layers: the
  numbered root documents `01` to `34`, and the `DD/` folder that summarizes them. The
  implementable detail lives in the root: `DD/` carries about 1400 lines of fenced code and
  the root documents carry about 2500, and `DD-24` contains **none** of the DPoP defaults
  that root `24-design-dpop.md` states. Reading `DD/` first and treating it as sufficient
  silently drops values (a proof-validity window, a per-client flag, an advertised algorithm
  set). `DD/` is an index of what exists and which decisions apply; the root document is the
  source. Where the two disagree, follow the pointer to whichever document the corpus itself
  names as owner and verify there, since the corpus contradicts itself in places.
- **The corpus states its own reading order; follow it.** Its `CLAUDE.md` defines a **five-part
  bundle** per phase, "phase-doc + mini-spec + ADR + verification V-file + register entry",
  and warns that a phase's information is spread across all five. It also sets a strict
  layering: root `01`-`31` are the implementer source (`01`-`09` phases, `10`-`16`
  cross-cutting, `17`-`31` mini-specs), `adr/` holds the decisions with `decisions/` as their
  MADR conversion, `PRODUCTION-READINESS-REGISTER.md` tracks open items by bucket (A spike,
  B test, C ratify, D pick), and **`knowledge-based/` is evidence, not an implementer
  source**. Two things are easy to miss and both matter: spike-proven reference code is
  **embedded in the mini-specs** (and runnable under `spike-harness/`), and
  **`reference/openiddict-source/` holds 23 files of OpenIddict 7.5.0 upstream source**,
  checked in precisely so a claim can be read at source. The local NuGet cache carries only
  7.4.0, so that tree is the only offline way to verify a 7.5.0 default. Use it: it settled
  `RefreshTokenReuseLeeway`'s 30-second default on first use.

## Non-negotiable content rules

These are legal/OSS constraints and the CI guardrail + local hook enforce parts of them:

- **Never name the direct commercial competitor** (or its vendor) and **never name
  real client organizations** in any committed/public file. Generalize such
  references. The real-name list is deliberately kept **local and git-ignored**
  (`scripts/.local/name-denylist`, checked by the opt-in pre-commit hook), do not
  commit it, and do not add a public denylist of those names (publishing the list
  would itself leak the names and demotivate contributors).
- **One exception, decided 2026-07-25: OSS packages Nami actually depends on keep
  their real package identifiers**, even when the identifier carries the vendor's
  name. Hiding a dependency's identifier makes the dependency record factually wrong
  and unusable by the ADR-0026 license-scan gate, which needs exact package IDs. This
  covers `Duende.AccessTokenManagement`, `Duende.AccessTokenManagement.OpenIdConnect`,
  and their transitive `Duende.IdentityModel` (all Apache-2.0 at 4.2.0 / 8.1.0,
  verified at nuget.org 2026-07-25, published from the vendor's separate FOSS
  repository, not its commercial line). It does **not** cover product comparison,
  parity framing, the vendor's internal source or type references, its issue tracker
  and blog posts, or commercial packages Nami rejects (such as its BFF package): those
  stay generalized. The exemptions live in the git-ignored
  `scripts/.local/name-allowlist` (see `scripts/README.md`); the policy itself belongs
  in ADR-0026.
- **No template placeholders** in tracked markdown: the curly-brace `Product`,
  `Company`, and `domain` tokens must never appear (guardrail Check 1). Note that
  `scripts/README.md` deliberately describes these tokens in prose to avoid tripping
  its own check, don't reintroduce the literal braces.
- **Permissive dependencies only** (MIT/Apache-2.0/BSD-class). No copyleft,
  source-available, or commercial packages. Enforced by policy (ADR-0026) and, once
  code exists, a CI license-scan gate.
- **No em dash** in prose you write for this project (user preference).

## The guardrail (`scripts/check-adrs.sh`)

Neutral, public, run by CI (`adr-guardrail` job) and the local hook. Five checks:
placeholder tokens, ADR cross-reference integrity, index/status consistency,
ADR-0061 stack-of-record table membership (bidirectional), and the no-em-dash style
rule. Checks 1, 2, and 5 read **all tracked markdown**, not just `docs/adr/`: the
architecture and design layers cite far more ADR numbers than the ADRs do, and a
number that resolves nowhere is the same defect wherever it is written. Checks 3 and
4 are ADR-corpus-scoped by nature. The em-dash pattern is built from its codepoint so
the script stays pure ASCII and cannot trip its own check.
It is written for **portability to macOS bash 3.2 and the Ubuntu runner**: no
`mapfile`, no associative arrays, no GNU-only flags; ADR enumeration uses on-disk
globs. Preserve that portability if you edit it. The local hook
(`scripts/hooks/pre-commit`) additionally runs the git-ignored name-scrub.

## Docs layout and the KB boundary

- `docs/adr/`, settled decisions (MADR). One decision → one ADR.
- `docs/architecture/`, the SAD: the coherent picture across views. **24 files, `01` to
  `24`, no gaps, covering the arc42 template's twelve sections in order** (problem,
  solution strategy, C4 L1-L3 plus runtime and deployment, seven cross-cutting files,
  decisions, quality, risks, glossary). arc42 is CC BY-SA 4.0 and is credited in
  `docs/architecture/README.md`: only the section sequence is used, no arc42 text is
  reproduced. It **never** introduces a decision; it synthesizes the ADRs and points
  into the designs, and it is the bug when it disagrees with either. **The file number
  is the reading order**, so inserting a chapter means renumbering the tail and
  rewriting every link, which is a deliberate act, not a casual one.
- `docs/architecture/24-glossary.md` is arc42 section 12 and defines vocabulary for
  **all three layers**, not only for the architecture. It lives inside that folder
  because arc42 puts the glossary in the architecture document; an entry names the
  document of record rather than owning the term, so defining `stack of record` there
  leaves ADR-0061 the authority. Do not narrow its scope to architecture terms.
- `docs/design/`, per-feature detailed designs, the authority for implementation detail.
- `docs/kb/notes/`, a lesson, how-something-works, or gotcha that is **not** a decision.
- `docs/kb/research/`, deeper investigation, usually preceding an ADR, linking to it.
- KB files use their own frontmatter (`title`, `tags`, `created`, `related`), no H1,
  and link with `[[slug]]`. See `docs/kb/README.md`.
- Rule of thumb: **decision → ADR; durable knowledge to reference → KB.**

## Git and contribution workflow

- **DCO sign-off on every commit** (`git commit -s`); this repo uses the DCO, not a CLA.
- **Conventional Commits** (`feat:`, `fix:`, `docs:`, `test:`, `ci:`, `chore:`, …);
  the changelog is generated from these.
- Branch for changes; commit or push only when asked. This project's convention is
  **one ADR per commit** when importing/authoring ADRs.
- Naming and coding conventions: **ADR-0065 is the authority** (Microsoft naming +
  C# conventions adopted by reference, enforced via `.editorconfig` + analyzers,
  with the Nami tailoring). Quick reference: assemblies under `Nami.Identity.*`;
  config keys `Nami:X` (env `Nami__X`), env alias `NAMI_X`. The machine-enforceable
  rules live in `.editorconfig` (the C# ruleset lands with the first code at M1).

## Ephemeral working areas (git-ignored, local-only)

`docs/superpowers/` (specs + plans), `.superpowers/` (SDD ledgers/briefs/reports),
and `docs/kb/.scratch/` are git-ignored working artifacts, never published.
Clean with `git clean -Xfd docs/superpowers .superpowers docs/kb/.scratch`.
