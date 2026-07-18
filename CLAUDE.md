# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

Nami is an open-source, multi-tenant OAuth 2.0 / OpenID Connect identity provider
for .NET, built on OpenIddict — an Apache-2.0 alternative to commercial identity
servers. It is in **pre-alpha**: the architecture is fully designed and its risk
spikes were validated with runnable code, but that code lives in a separate design
corpus. This repo currently holds the **decision records, governance, and docs
scaffolding**; application source lands under `src/` starting at milestone M1
(`src/.gitkeep` is the placeholder). The only executable code today is the docs
guardrail (`scripts/`).

Because the product is expressed as decisions, the ADR corpus in `docs/adr/` **is**
the architecture. Read the relevant ADRs before proposing changes to behavior they
govern — accepted ADRs are binding until superseded.

## Commands

```bash
# Docs guardrail — the CI gate. Must pass before any docs/ADR change merges.
bash scripts/check-adrs.sh

# Markdown lint — same pinned version CI runs (do not float this version).
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
  ADR's row in the index — the guardrail enforces this.
- **Index:** every ADR has a row in `docs/adr/README.md` with a Status column.
  Adding an ADR means adding its index row in the same change.
- **Deferred gates:** several ADRs defer a policy, threshold, or human sign-off to
  before GA. Those are consolidated in `docs/PRE-GA-RATIFICATION-CHECKLIST.md` —
  when an ADR defers something, add or update its checklist entry.
- **Cross-references** use `ADR-NNNN`. Every such reference must resolve to a real
  `docs/adr/NNNN-*.md` file (guardrail-enforced) — do not forward-reference an ADR
  number that has not been written yet.

### Authoring conventions for ADRs (learned constraints)

- **Verify at source, don't copy verbatim.** When importing or citing, re-check the
  fact and correct stale cross-references rather than transcribing.
- **Proposed / deferred ADRs stay implementation-open.** Do not pin a specific
  third-party library in a `proposed` ADR ("consider to build later if needed") —
  record the decision, leave the mechanism open.
- **Deferrals are decisions** worth their own ADR or a checklist entry, not silent gaps.
- Confirm granularity and status with the user before drafting; prefer one focused
  ADR per decision over grab-bag documents.

## Non-negotiable content rules

These are legal/OSS constraints and the CI guardrail + local hook enforce parts of them:

- **Never name the direct commercial competitor** (or its vendor) and **never name
  real client organizations** in any committed/public file. Generalize such
  references. The real-name list is deliberately kept **local and git-ignored**
  (`scripts/.local/name-denylist`, checked by the opt-in pre-commit hook) — do not
  commit it, and do not add a public denylist of those names (publishing the list
  would itself leak the names and demotivate contributors).
- **No template placeholders** in tracked markdown: the curly-brace `Product`,
  `Company`, and `domain` tokens must never appear (guardrail Check 1). Note that
  `scripts/README.md` deliberately describes these tokens in prose to avoid tripping
  its own check — don't reintroduce the literal braces.
- **Permissive dependencies only** (MIT/Apache-2.0/BSD-class). No copyleft,
  source-available, or commercial packages. Enforced by policy (ADR-0026) and, once
  code exists, a CI license-scan gate.
- **No em dash** in prose you write for this project (user preference).

## The guardrail (`scripts/check-adrs.sh`)

Neutral, public, run by CI (`adr-guardrail` job) and the local hook. Three checks:
placeholder tokens, ADR cross-reference integrity, and index/status consistency.
It is written for **portability to macOS bash 3.2 and the Ubuntu runner** — no
`mapfile`, no associative arrays, no GNU-only flags; ADR enumeration uses on-disk
globs. Preserve that portability if you edit it. The local hook
(`scripts/hooks/pre-commit`) additionally runs the git-ignored name-scrub.

## Docs layout and the KB boundary

- `docs/adr/` — settled decisions (MADR). One decision → one ADR.
- `docs/kb/notes/` — a lesson, how-something-works, or gotcha that is **not** a decision.
- `docs/kb/research/` — deeper investigation, usually preceding an ADR, linking to it.
- KB files use their own frontmatter (`title`, `tags`, `created`, `related`), no H1,
  and link with `[[slug]]`. See `docs/kb/README.md`.
- Rule of thumb: **decision → ADR; durable knowledge to reference → KB.**

## Git and contribution workflow

- **DCO sign-off on every commit** (`git commit -s`); this repo uses the DCO, not a CLA.
- **Conventional Commits** (`feat:`, `fix:`, `docs:`, `test:`, `ci:`, `chore:`, …) —
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
and `docs/kb/.scratch/` are git-ignored working artifacts — never published.
Clean with `git clean -Xfd docs/superpowers .superpowers docs/kb/.scratch`.
