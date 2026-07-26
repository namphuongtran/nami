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
- **A false positive in a checker is a defect in the checker**, not noise to work
  around, and the same holds for a claim that survives only because nobody checked it.

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
- `docs/architecture/`, the SAD: the coherent picture across views. **23 files, `01` to
  `23`, no gaps, in the arc42 template's chapter sequence** (problem, solution strategy,
  C4 L1-L3 and runtime and deployment, seven cross-cutting files, decisions, quality,
  risks; arc42's section 12 glossary is `docs/GLOSSARY.md`, one level up, because it
  also serves the ADRs and the designs). arc42 is CC BY-SA 4.0 and is credited in
  `docs/architecture/README.md`: only the section sequence is used, no arc42 text is
  reproduced. It **never** introduces a decision; it synthesizes the ADRs and points
  into the designs, and it is the bug when it disagrees with either. **The file number
  is the reading order**, so inserting a chapter means renumbering the tail and
  rewriting every link, which is a deliberate act, not a casual one.
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
