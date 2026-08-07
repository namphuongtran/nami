# CLAUDE.md

Rules that are true **everywhere**. Rules that are true only inside one folder live in that
folder, and the last section says where.

## Read the decision before you change anything

Nami's product is expressed as decisions. So the ADR corpus in `docs/adr/` **is** the
architecture. Before you brainstorm, implement, or fix anything, read the three layers that
govern it.

1. **The ADR that decides it.** Index: [`docs/adr/README.md`](docs/adr/README.md). Accepted
   ADRs are binding until superseded.
2. **The detailed design that realizes it.** Index:
   [`docs/design/README.md`](docs/design/README.md). A design never makes a decision. If one
   seems to, that is the bug.
3. **The architecture view that synthesizes it.** Index:
   [`docs/architecture/README.md`](docs/architecture/README.md). Also
   [`docs/architecture/18-decisions-index.md`](docs/architecture/18-decisions-index.md) maps
   every ADR to the views that cite it. That is the fastest route from a topic to the full
   set.

**Name what you read, by file and line.** An unnamed source cannot be checked. Naming it is
what makes the reading checkable rather than claimed.

**If a source is wrong, contradicts another layer, or does not cover the case: stop and flag
it, quoting both sides at file and line.** Do not fill the gap from judgement. Do not proceed
as if it were covered. Research it, or say "not verified" out loud. Saying so is the
deliverable. [`src/CLAUDE.md`](src/CLAUDE.md) records what skipping this costs. A port could
not be written at all, because its design omitted a return type. That was discovered only by
trying to write it.

## What this repository is

Nami is an open-source, multi-tenant OAuth 2.0 and OpenID Connect identity provider for .NET.
It is built on OpenIddict, it is Apache-2.0 licensed, and it is an alternative to commercial
identity servers.

It is **pre-alpha**. The architecture is fully designed, and its risk spikes were validated in
a separate design corpus. This repository holds the decision records, the governance, and the
docs scaffolding. `src/` began on 2026-08-02 with `Nami.Identity.Abstractions`, and `tests/`
on the same day with `Nami.Identity.ArchitectureTests`.

**Starting a session with no other instruction? Read
[`docs/BUILD-PLAN.md`](docs/BUILD-PLAN.md) first.** It holds what is built next and what
blocks it, what is owed with the trigger that comes due, and what has not been verified. It
exists because that answer used to live only in a conversation, and it did not survive one. It
is a queue that points at owners, never an authority. A row that disagrees with the ADR or the
design it cites is a bug in the row.

## Commands

The command list is in [`.claude/rules/commands.md`](.claude/rules/commands.md), which loads
every session. [`scripts/README.md`](scripts/README.md) is the authority on what each gate
checks and why.

**The hook runs two of the nine gates, so a green hook is not a green build.** That has
already produced a commit message claiming a self-test was green before it had been run.

**Four of the nine are self-tests, and that ratio is deliberate.** Each was written after a
real inert-gate defect, not as a precaution. The four defects were:

- an untracked file the guardrail could not see;
- a severity that failed nothing without an MSBuild property;
- an `RS0017` that no `.editorconfig` placement could reach;
- a warning gate whose subject produces no warnings at all, so the ordinary build is green
  whether or not it is armed.

**When you add a gate, ask what would have to break for it to go quiet, then write that
break down as a test.**

The test gate arrived on 2026-08-02 with the first suite, the architecture rules of ADR-0024.
The license-scan gate is still owed, and it arrives with M1.

## Evidence rule (non-negotiable, applies to every layer)

**Never write a claim you have not read at its source.** Never infer one from a title, a file
name, or a nearby document. This outranks fluency. An unsourced sentence that reads well is
worse than an omission, because a later reader cannot tell them apart.

- **Quote before you assert.** Citing `ADR-NNNN` for a fact means the fact is *in* that ADR,
  not next to its subject. A true fact with the wrong owner is a defect. It is the most common
  defect this repository has had.
- **The dangerous citation is the one that resolves.** A pointer to a file that exists passes
  every mechanical check. What fails is that the file lacks the claim. So a green checker is
  not evidence about what a citation says. Two shapes deserve suspicion before any tool runs.
  The first is a citation at the end of a **compound** sentence, where the pointer attaches to
  the wrong clause. The second is a bundle of items behind one reference.
- **Show the evidence before making the change.** Present both sides with file and line
  before editing. The user decides on evidence, not on a summary.
- **Count what you counted.** Never write "the second time", "three sources agree", or "every
  X" without naming them. Write "in `file:line`" instead of a total you did not run.
- **A measurement is dated.** Any figure produced by running something is a measurement with
  a date. Re-run it rather than citing it forward. Never edit a dated measurement to match
  today, because that stops it being evidence.
- **Where this repo says more than its source, name the real source.** Inventing support for a
  correct-sounding claim is the worst failure mode available here.
- **A stated value is not a known default.** A source saying "X is set to Y" says nothing
  about what X would be if nobody set it. "Y is set explicitly because the default is weaker"
  is a *second* claim, and it needs a *second* source. That is the shape self-generated errors
  take here.
- **"No source says X" is a claim about a search.** List the spellings X can take, and write
  down which searches you ran, inside the claim. A bare absence gets quoted forward. One with
  its method attached can be re-checked.
- **Never edit a document to silence a checker.** If a checker is wrong, fix the checker or
  record the finding as legitimate. If neither is cheap, delete the checker. A tool that bends
  the evidence it exists to protect is worse than none. `scripts/review/` was deleted for
  exactly this.
- **A false positive in a checker is a defect in the checker**, not noise to work around. So
  is a claim that survives only because nobody checked it.

## Non-negotiable content rules

Legal and OSS constraints. The CI guardrail and the local hook enforce parts of them.

- **Never name the direct commercial competitor or its vendor, and never name real client
  organizations**, in any committed or public file. Generalize instead. The real-name list is
  deliberately **local and git-ignored** (`scripts/.local/name-denylist`). Do not commit it.
  Do not add a public denylist either, because publishing the list would leak the names it
  exists to hide.
- **One exception, decided 2026-07-25: OSS packages Nami depends on keep their real package
  identifiers**, even when the identifier carries the vendor's name. Hiding it makes the
  dependency record wrong, and unusable by the ADR-0026 license-scan gate, which matches exact
  package IDs. It does **not** cover product comparison, parity framing, the vendor's internal
  source or type references, its issue tracker and blog posts, or commercial packages Nami
  rejects. Exemptions live in the git-ignored `scripts/.local/name-allowlist`, and the policy
  itself belongs in ADR-0026.
- **No template placeholders** in tracked markdown. The curly-brace `Product`, `Company`, and
  `domain` tokens must never appear (guardrail Check 1). `scripts/README.md` describes them in
  prose to avoid tripping its own check, so do not reintroduce the braces.
- **Permissive dependencies only** (MIT, Apache-2.0, or BSD-class). No copyleft,
  source-available, or commercial packages. ADR-0026 is the policy. Read a licence at the
  distributed artifact, never from a badge and never from another document in this repository.
- **No em dash** in prose you write for this project.

## How to write

Write so a reader whose first language is not English can follow on the first read.

- **Answer first**, then the detail.
- **One idea per sentence**, under about 20 words.
- **Lists and tables**, not paragraphs.
- **Simple words**, and the same word for one concept.
- **No contractions.** No em dash: guardrail Check 5 fails the build on one.

**Meaning outranks style.** Where a simpler sentence would weaken a claim, keep the claim and
split the sentence. The evidence rule above wins. The full rules, and the two style-guide
rules this repository declines, are in
[`.claude/rules/writing-style.md`](.claude/rules/writing-style.md).

## Git and contribution workflow

- **DCO sign-off on every commit** (`git commit -s`). This repo uses the DCO, not a CLA.
- **Conventional Commits.** The changelog is generated from them.
- **The maintainer works directly on `main`** (2026-08-02, replacing a branch-to-PR-to-merge
  flow that ran for four increments). Commit and push when an increment is finished and its
  gates are green, not before and not spontaneously. **The plan still needs approval before
  the work starts.** Only the branch and the pull request stopped being required. An outside
  contribution goes through a pull request as
  [`CONTRIBUTING.md`](CONTRIBUTING.md) describes, and nothing here changes that. One ADR per
  commit when authoring or importing ADRs.
- **Re-derive the citations before the increment is committed.** `/refresh-citations` lists every
  `file:line` pointer the increment may have aged, because no gate here reads a line number. The
  `checking-a-citation` skill owns the judgement about what a pointer must satisfy.
- **ADR-0065 is the authority on naming and coding conventions** (Microsoft conventions
  adopted by reference, with the Nami tailoring). Quick reference: assemblies under
  `Nami.Identity.*`; config keys `Nami:X` (env `Nami__X`), env alias `NAMI_X`.

## Where the rest of the rules live

A folder's `CLAUDE.md` loads when a file in that folder is read. It carries only the traps
learned by getting something wrong. **A rule belongs in a folder's `CLAUDE.md` only if the
mistake it prevents is made while editing a file in that folder.** A rule whose mistake
happens elsewhere stays here, however specialised it looks.

**Each folder's `README.md` is the authority on that layer's own conventions, and the
`CLAUDE.md` beside it does not restate them.** Two summaries of one thing drift, and the
shorter one wins by being read first. Read both.

| Working in | Traps | Authority on the layer |
|---|---|---|
| `docs/` (any) | [`docs/CLAUDE.md`](docs/CLAUDE.md) | [`docs/README.md`](docs/README.md) |
| `docs/adr/` | [`docs/adr/CLAUDE.md`](docs/adr/CLAUDE.md) | [`docs/adr/README.md`](docs/adr/README.md) |
| `docs/architecture/` | [`docs/architecture/CLAUDE.md`](docs/architecture/CLAUDE.md) | [`docs/architecture/README.md`](docs/architecture/README.md) |
| `docs/design/` | [`docs/design/CLAUDE.md`](docs/design/CLAUDE.md) | [`docs/design/README.md`](docs/design/README.md) |
| `docs/kb/` | none, deliberately | [`docs/kb/README.md`](docs/kb/README.md) |
| `scripts/` | [`scripts/CLAUDE.md`](scripts/CLAUDE.md) | [`scripts/README.md`](scripts/README.md) |
| `src/` | [`src/CLAUDE.md`](src/CLAUDE.md) | ADR-0065, and design `01` section 3.1 |
| `tests/` | [`tests/CLAUDE.md`](tests/CLAUDE.md) | ADR-0060, and design `20` |
| any `.cs` file, in any folder | [`.claude/rules/csharp.md`](.claude/rules/csharp.md) | ADR-0065, and `.editorconfig` |
| any `.cshtml` file, in any folder | [`.claude/rules/razor.md`](.claude/rules/razor.md) | ADR-0072, and design `11` |
| any `.resx` file, and `Resources/` | [`.claude/rules/localization.md`](.claude/rules/localization.md) | ADR-0038 parameter G, design `11` section 5.6, and design `10` section 5.4 |
| any `.css`, `.html`, or `.js` file, and `wwwroot/` | [`.claude/rules/html-css.md`](.claude/rules/html-css.md) | ADR-0091, ADR-0072, and design `11` |
| build and CI config | [`.claude/rules/build-and-ci.md`](.claude/rules/build-and-ci.md) | the files themselves |
| any prose, in any folder | [`.claude/rules/writing-style.md`](.claude/rules/writing-style.md) | the Microsoft Style Guide |

`docs/kb/` has none on purpose. Its README already carries the frontmatter shape, the no-H1
rule, the `[[slug]]` link form, and the routing rule. A file whose only content is "read the
README" is a drift surface that buys nothing.

**A nested `CLAUDE.md` is not re-injected after `/compact`; only this root file is.** So
anything that must always hold belongs here, and the folder files are best-effort by
construction. If a session has been compacted, re-read the folder file before trusting that
its traps are still in context.

**A skill is selected by a description match, not by a path**, and only its `description` is in
context until it is invoked. So a skill cannot carry anything that must always hold, and it must
not restate a rule that loads beside it. The skills live in `.claude/skills/`, and each one names
the owner it defers to rather than summarizing it.

Selecting on a description is what a rules file cannot do.
[`.claude/rules/csharp.md`](.claude/rules/csharp.md) needs a `.cs` file in play, and the choice
each skill below governs is made **before** that file exists. Nine skills, counted 2026-08-07:

| Skill | Use it when | Authority it names |
|---|---|---|
| [`authoring-an-adr`](.claude/skills/authoring-an-adr/SKILL.md) | Writing, importing, promoting, or superseding an ADR | [`docs/adr/README.md`](docs/adr/README.md), and [`docs/adr/CLAUDE.md`](docs/adr/CLAUDE.md) |
| [`checking-a-citation`](.claude/skills/checking-a-citation/SKILL.md) | Verifying that a cited source holds the claim, or writing that no source does | the evidence rule above, and [`docs/CLAUDE.md`](docs/CLAUDE.md) |
| [`writing-a-folder-readme`](.claude/skills/writing-a-folder-readme/SKILL.md) | Giving a folder its first `README.md`, or moving a convention between a README and the `CLAUDE.md` beside it | this section, and [`.claude/rules/writing-style.md`](.claude/rules/writing-style.md) |
| [`adding-a-ci-gate`](.claude/skills/adding-a-ci-gate/SKILL.md) | Adding or changing anything that is supposed to fail a build | [`scripts/README.md`](scripts/README.md), and [`scripts/CLAUDE.md`](scripts/CLAUDE.md) |
| [`ci-security-scans`](.claude/skills/ci-security-scans/SKILL.md) | Any code, dependency, secret, container, or DAST scanning question | ADR-0092, and design `21` |
| [`writing-tests`](.claude/skills/writing-tests/SKILL.md) | Writing a test, or choosing which of the seven suites it belongs to | ADR-0060, and design `20` |
| [`writing-playwright-tests`](.claude/skills/writing-playwright-tests/SKILL.md) | Writing a Playwright browser test, or porting a published example | ADR-0025 parameter E, and design `16` section 9 |
| [`generating-a-playwright-test`](.claude/skills/generating-a-playwright-test/SKILL.md) | Turning a scenario into a Playwright test, or adopting a record-then-generate workflow | `writing-playwright-tests`, then ADR-0025 parameter E |
| [`driving-a-browser-safely`](.claude/skills/driving-a-browser-safely/SKILL.md) | An agent is about to drive a live browser against a running surface, to record, debug, or demo | ADR-0067 |

**Observed on 2026-08-07, with the limit stated.** Every skill was listed as available while no
file under `.claude/skills/` had been read, and the list grew as skills were added. So selection by
`description` happens. That says nothing about the `paths:` question in
[`docs/BUILD-PLAN.md`](docs/BUILD-PLAN.md) section 3, which is a different mechanism and stays
open.

## Ephemeral working areas (git-ignored, local-only)

`docs/superpowers/` (specs and plans), `.superpowers/` (SDD ledgers, briefs, and reports), and
`docs/kb/.scratch/` are working artifacts, and they are never published. Clean them with
`git clean -Xfd docs/superpowers .superpowers docs/kb/.scratch`.

`.claude/worktrees/` is a fourth, and it behaves differently: each worktree under it contains its
**own** `.git`, so `git add .claude` stages a gitlink rather than the files. That was observed on
2026-08-07, with git printing `adding embedded git repository`, and it is why `.gitignore` now
names the parent. Remove one with `git worktree remove`, which is the tool that also updates git's
worktree list. Whether `git clean` reaches a nested repository was not tested.

**One measured side effect, because it changes how a gate's output reads.** While a worktree
existed, `markdownlint-cli2 "**/*.md"` reported 371 files. After the ignore entry landed, and with
the worktree gone, it reported 190, which equals `git ls-files '*.md' | wc -l` on the same tree.
So an inflated lint count is a real symptom, and `.claude/commands/gate.md` step 3 already tells a
reader to treat it as one.
