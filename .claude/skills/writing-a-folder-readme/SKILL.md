---
name: writing-a-folder-readme
description: Use when a folder with no README.md is getting one, when deciding whether a convention belongs in a folder README or in the CLAUDE.md beside it, and when a README is about to restate something an ADR owns. A folder README here is the authority on that layer's own conventions, not an introduction to it, and landing one changes what the CLAUDE.md beside it may hold. Two folders record that they are waiting for one. This is not for the root README.md, which is a different document with different rules.
---

# Writing a folder README here

Read this before creating a `README.md` in a folder that has none, and before moving a convention
between a README and the `CLAUDE.md` beside it. It exists because the decision is taken before any
file in the target folder is opened, so that folder's `CLAUDE.md` has not loaded yet.

This skill holds nothing that a loaded file already holds. The root
[`../../../CLAUDE.md`](../../../CLAUDE.md) carries the rule this skill operationalizes, under
"Where the rest of the rules live", and it is injected every session and after `/compact`.
[`../../rules/writing-style.md`](../../rules/writing-style.md) carries the prose rules and loads
every session. Neither is restated here. What follows is the boundary, and what a generic README
answer gets wrong on this side of it.

## What exists today, measured

Counted on 2026-08-07 at `10df955`, over the nine folders that carry either file.

| Folder | README | CLAUDE.md | README lines |
|---|---|---|---|
| root | yes | yes | 60 |
| `docs/` | yes | yes | 13 |
| `docs/adr/` | yes | yes | 134 |
| `docs/architecture/` | yes | yes | 233 |
| `docs/design/` | yes | yes | 134 |
| `docs/kb/` | yes | **no, deliberately** | 47 |
| `scripts/` | yes | yes | 346 |
| `src/` | **no** | yes | |
| `tests/` | **no** | yes | |
| `.claude/` | no | no | |

Three facts follow, and each is recorded by the file it is about.

- **Two folders are waiting.** `src/CLAUDE.md` and `tests/CLAUDE.md` each state that no README
  exists yet, and `src/CLAUDE.md` states what happens when one does: it "becomes the authority on
  this layer's own conventions, and this file keeps only the traps."
- **One folder has a README and no `CLAUDE.md` on purpose.** The root file gives the reason for
  `docs/kb/`: its README already carries the frontmatter shape, the no-H1 rule, the link form, and
  the routing rule, so a second file whose only content is "read the README" is a drift surface
  that buys nothing.
- **Frontmatter is split.** Two of the seven carry `status`, `created`, and `tags`:
  `docs/architecture/README.md:1-5` and `docs/design/README.md:1-5`. The other five carry none.

## Where the generic README answer is wrong here

Each row was read at its source on 2026-08-07. The middle column quotes enough of the decision to
survive a line shift, so a drifted pointer reads as drift rather than as a different claim.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| A README introduces the folder, so it summarizes what is in it | A folder README is "the authority on that layer's own conventions", and the file beside it "does not restate them", because "two summaries of one thing drift, and the shorter one wins by being read first" | root `CLAUDE.md`, under "Where the rest of the rules live" |
| Every folder should have both a README and a `CLAUDE.md` | `docs/kb/` has "none on purpose", because its README already carries the conventions | root `CLAUDE.md`, the paragraph beginning "`docs/kb/` has none on purpose" |
| Put the technology stack table in the README | Rejected option: "a docs table has no status and is easy to let rot". The README "may render a derived, friendly view of this table, but this ADR is the authority for the stack" | ADR-0061:105 and ADR-0061:115 |
| Omit LICENSE and CONTRIBUTING sections, because dedicated files exist | The root README carries both, as `## Contributing` and `## License` | `README.md:54,58` |
| Open with a logo or an icon | No image asset is tracked. Searched 2026-08-07 over `git ls-files` for `.svg`, `.png`, `.jpg`, `.jpeg`, `.ico`, and `.webp`: zero hits | the search above |
| GitHub admonition blocks for callouts | No tracked markdown uses one. Searched 2026-08-07 with `git grep -c '> \[!' -- '*.md'`: zero hits. Adopting it inside a README would introduce a convention by the wrong instrument | the search above |
| Take inspiration from exemplar READMEs in other repositories, and cite them | Naming other projects in a committed public file runs into the content rules, and the local scrub reads **staged** markdown only, so a contributor without it gets no warning | root `CLAUDE.md`, the content rules; `scripts/hooks/pre-commit` |
| An index README lists the files in the folder | `docs/README.md` gives each entry a purpose sentence instead, and says what the entry decides or does not decide | `docs/README.md:3-11` |
| A README is prose | Lists and tables, not paragraphs. The largest one here, `scripts/README.md`, is one H2 per script plus one for the hook | `.claude/rules/writing-style.md`, and `scripts/README.md` |

## Landing one changes two other files, and neither is optional

1. **The `CLAUDE.md` beside it is trimmed in the same change.** It keeps only the traps learned by
   getting something wrong inside that folder. Leaving the conventions in both is the drift the
   rule exists to prevent, and the shorter file wins by being read first.
2. **The root routing table's third column changes.** Its rows for `src/` and `tests/` currently
   name interim authorities, because no README exists to name. `src/` names ADR-0065 and a design
   section, and `tests/` names ADR-0060 and the testing design. Those cells become the new README.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so the search is written
into it. All were run on 2026-08-07 over tracked files.

- **Whether a folder README carries frontmatter.** Two of seven do and five do not. Searched
  `git grep -niE 'readme.*(frontmatter|front matter)|(frontmatter|front matter).*readme' -- '*.md'`:
  four hits, and every one is about an **ADR's** frontmatter `status:` matching its index row,
  which is a different subject. Nothing rules on a README's own frontmatter.
- **What `src/README.md` and `tests/README.md` should contain.** Searched
  `git grep -nE 'README' -- src/CLAUDE.md tests/CLAUDE.md`: each file says only that no README
  exists yet, and `src/CLAUDE.md` adds what such a file would become. Neither says what goes in it.
- **Whether any ADR governs folder README content.** Searched
  `git grep -nE 'README' -- 'docs/adr/0*.md'`: twelve hits across eight ADRs. Every one names a
  README as a **location** rather than setting a convention for one. The single exception is
  ADR-0061:115, and it constrains only the root README's relation to the stack table.
- **Whether `.claude/` gets either file.** It has neither today, and `.claude/commands/` has held
  files with no routing-table row since 2026-08-01.

A genuinely new decision here is raised as an ADR, never settled inside a README.

## Who owns which question

| Question | Authority |
|---|---|
| Whether a convention belongs in the README or the `CLAUDE.md` beside it | root `CLAUDE.md`, "Where the rest of the rules live" |
| The prose rules, and the two style-guide rules this repository declines | `.claude/rules/writing-style.md` |
| Sentence-level style, and the em dash and contraction bans | `.claude/rules/writing-style.md`, and guardrail Check 5 for the em dash only |
| Whether a claim in a README is supported by its citation | the `checking-a-citation` skill |
| What the technology stack table may say, and where | ADR-0061 |
| The KB note shape, the no-H1 rule, and the link form | `docs/kb/README.md` |
| What each of the nine gates checks | `scripts/README.md` |
| Traps learned inside a folder | that folder's `CLAUDE.md`, not re-injected after `/compact` |

When this skill and the root `CLAUDE.md` disagree, the root file wins and this skill is the bug.
