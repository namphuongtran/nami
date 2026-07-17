# nami Knowledge Base

Durable, referable knowledge from working sessions: lessons learned, how a
subsystem behaves, gotchas, and research. This is not where decisions live
(those are ADRs) and not where raw drafts live (those stay local).

## Layout

- `notes/`: atomic knowledge, one topic per file.
- `research/`: deeper investigations, usually preceding an ADR.
- `.scratch/`: local-only raw capture, git-ignored, created on demand.

## What goes where

| Artifact | Holds |
| --- | --- |
| `docs/adr/` | A settled decision with consequences (MADR). |
| `docs/kb/notes/` | A lesson, a how-something-works, or a gotcha that is not a decision. |
| `docs/kb/research/` | A deep investigation, usually preceding an ADR and linking to that ADR. |

One-line rule: decision → ADR; knowledge to reference → KB.

## Writing a note

Every curated file starts with frontmatter:

```yaml
---
title: <human title>
tags: [openiddict, multi-tenant]
created: 2026-07-17
related: [[some-note]], [[0021-openiddict-version-adaptation]]
---
```

- One note, one topic. Split it when it grows to cover two.
- Do not add an H1 heading; the frontmatter `title` is the note's title.
- Link notes and ADRs with `[[slug]]` (the ADR slug is its filename without
  the extension).
- Keep it lint-clean: `npx --yes markdownlint-cli2@0.23.0 "**/*.md"` must
  report 0 errors (the pinned version matches CI).

## Raw capture

Write session scratch into `docs/kb/.scratch/YYYY-MM-DD.md` (git-ignored,
create the folder on demand). At the end of a session, distill the keepers into
`notes/` or `research/`. Clean scratch with `git clean -Xfd docs/kb/.scratch`.
