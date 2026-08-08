# CLAUDE.md

Rules true **everywhere**. Rules true only inside one folder live in that folder, and the last
section says where.

**Every `CLAUDE.md` here has a 200-line budget.** A rule earns its place; the story of how it was
learned does not. Keep one sentence of the reasoning and leave the rest to the ADR, the design, or
the commit message that owns it.

## Read the decision before you change anything

Nami's product is expressed as decisions, so the ADR corpus in `docs/adr/` **is** the
architecture. Before you brainstorm, implement, or fix anything, read the three governing layers.

1. **The ADR that decides it** ([index](docs/adr/README.md)), binding until superseded.
2. **The design that realizes it** ([index](docs/design/README.md)). A design never decides. If
   one seems to, that is the bug.
3. **The architecture view that synthesizes it** ([index](docs/architecture/README.md)), plus
   [`18-decisions-index.md`](docs/architecture/18-decisions-index.md), which maps every ADR to
   the views citing it: the fastest route from a topic to the full set.

**Name what you read, by file and line.** An unnamed source cannot be checked, and naming it is
what makes the reading checkable rather than claimed.

**If a source is wrong, contradicts another layer, or does not cover the case: stop and flag it,
quoting both sides at file and line.** Do not fill the gap from judgement. Research it, or say
"not verified" out loud, because saying so is the deliverable. [`src/CLAUDE.md`](src/CLAUDE.md)
records the cost: a port could not be written at all, its design having omitted a return type.

## What this repository is

Nami is an open-source, multi-tenant OAuth 2.0 and OpenID Connect identity provider for .NET,
built on OpenIddict and Apache-2.0 licensed. It is **pre-alpha**: the architecture is fully
designed and its risk spikes were validated in a separate design corpus, so this repository is
mostly decision records, governance, and docs. `src/` and `tests/` both began 2026-08-02.

## The work queue is not a source

`docs/BUILD-PLAN.md` is the maintainer's own progress queue. **It is temporary and it will be
deleted.** Two rules follow, and the second is the one that has been broken twice.

- **Reading it is fine.** With no other instruction, read it first if it is still there. It is a
  queue pointing at owners, never an authority, so a row disagreeing with the ADR or design it
  cites is a bug in the row.
- **Never cite it, and never point a line number into it.** Not from an ADR, a design, a
  `CLAUDE.md`, a rules file, or a skill. Those outlive the queue, so a citation into it becomes a
  dangling pointer on a schedule nobody controls. Cite the ADR, the design, or the code that owns
  the fact. Where a search count would include the queue, exclude it and say so, as
  [`.claude/rules/localization.md`](.claude/rules/localization.md) does.

## Commands and gates

The command list is [`.claude/rules/commands.md`](.claude/rules/commands.md), which loads every
session. [`scripts/README.md`](scripts/README.md) is the authority on what each gate checks and
why. Neither is summarized here.

- **The hook runs two of the nine gates, so a green hook is not a green build.** That already
  produced a commit message claiming a self-test was green before it ran.
- **Four of the nine are self-tests**, each written after a real defect where a control read as
  enforced while enforcing nothing. A green build is not a green gate.
- **When you add a gate, ask what would have to break for it to go quiet, then write that break
  down as a test.** The licence-scan gate is still owed, and it arrives with M1.

## Evidence rule (non-negotiable, applies to every layer)

**Never write a claim you have not read at its source.** Never infer one from a title, a file
name, or a nearby document. This outranks fluency. An unsourced sentence that reads well is worse
than an omission, because a later reader cannot tell them apart.

- **Quote before you assert.** Citing `ADR-NNNN` means the fact is *in* that ADR, not next to its
  subject. A true fact with the wrong owner is a defect, and the most common one here.
- **The dangerous citation is the one that resolves.** A pointer to a file that exists passes
  every mechanical check; what fails is that the file lacks the claim. So a green checker is no
  evidence about what a citation says. Two shapes deserve suspicion before any tool runs: a
  citation at the end of a **compound** sentence, attaching to the wrong clause, and a bundle of
  items behind one reference.
- **Show the evidence before making the change**, with file and line for both sides. The user
  decides on evidence, not on a summary.
- **Count what you counted.** Never write "the second time", "three sources agree", or "every X"
  without naming them. Give `file:line` instead of a total you did not run.
- **A measurement is dated.** Re-run any figure rather than citing it forward, and never edit one
  to match today, because that stops it being evidence.
- **Where this repo says more than its source, name the real source.** Inventing support for a
  correct-sounding claim is the worst failure mode here.
- **A stated value is not a known default.** "X is set to Y" says nothing about what X would be if
  nobody set it. "Y is set explicitly because the default is weaker" is a *second* claim needing a
  *second* source. That is the shape self-generated errors take here.
- **"No source says X" is a claim about a search.** List the spellings X can take, and write which
  searches you ran inside the claim. A bare absence gets quoted forward; one carrying its method
  can be re-checked.
- **Never edit a document to silence a checker.** Fix the checker, or record the finding as
  legitimate; if neither is cheap, delete the checker. A tool that bends the evidence it exists to
  protect is worse than none, and `scripts/review/` was deleted for exactly this. **A false
  positive is a defect in the checker**, not noise to work around, and so is a claim surviving
  only because nobody checked it.

## Non-negotiable content rules

Legal and OSS constraints. The CI guardrail and the local hook enforce parts of them.

- **Never name the direct commercial competitor or its vendor, and never name real client
  organizations**, in any committed or public file. Generalize instead. The real-name list is
  deliberately **local and git-ignored** (`scripts/.local/name-denylist`). Do not commit it, and
  do not add a public one, because publishing the list leaks the names it exists to hide.
- **One exception, 2026-07-25: OSS packages Nami depends on keep their real package identifiers**,
  even when the identifier carries the vendor's name, because hiding it makes the dependency
  record wrong and unusable by the ADR-0026 licence-scan gate, which matches exact package IDs. It
  does **not** cover product comparison, parity framing, the vendor's own source or type
  references, its issue tracker and blog, or commercial packages Nami rejects. Exemptions live in
  the git-ignored `scripts/.local/name-allowlist`; ADR-0026 owns the policy.
- **No template placeholders** in tracked markdown: the curly-brace `Product`, `Company`, and
  `domain` tokens must never appear (guardrail Check 1). `scripts/README.md` describes them in
  prose to avoid tripping its own check, so do not reintroduce the braces.
- **Permissive dependencies only** (MIT, Apache-2.0, or BSD-class): no copyleft, source-available,
  or commercial packages (ADR-0026). Read a licence at the distributed artifact, never from a badge
  and never from another document here.
- **No em dash** in prose you write for this project.

## How to write

Write so a reader whose first language is not English can follow on the first read.

- **Answer first**, then the detail.
- **One idea per sentence**, under about 20 words.
- **Lists and tables**, not paragraphs.
- **Simple words**, and the same word for one concept.
- **No contractions.** No em dash: guardrail Check 5 fails the build on one.

**Meaning outranks style**, and the evidence rule wins. Where a simpler sentence would weaken a
claim, keep the claim and split the sentence. Full rules, and the two style-guide rules this
repository declines: [`.claude/rules/writing-style.md`](.claude/rules/writing-style.md).

## Git and contribution workflow

- **DCO sign-off on every commit** (`git commit -s`). This repo uses the DCO, not a CLA.
- **Conventional Commits.** The changelog is generated from them.
- **The maintainer works directly on `main`** (2026-08-02). Commit and push when an increment is
  finished and its gates are green, not before and not spontaneously. **The plan still needs
  approval before the work starts**; only the branch and the pull request stopped being required.
  An outside contribution goes through a pull request as
  [`CONTRIBUTING.md`](CONTRIBUTING.md) describes. One ADR per commit when authoring ADRs.
- **Re-derive the citations before committing.** `/refresh-citations` lists every `file:line`
  pointer the increment may have aged, because no gate here reads a line number. The
  `checking-a-citation` skill owns what a pointer must satisfy.
- **ADR-0065 is the authority on naming and coding conventions** (Microsoft conventions adopted by
  reference, plus the Nami tailoring): assemblies under `Nami.Identity.*`, config keys `Nami:X`
  (env `Nami__X`), env alias `NAMI_X`.

## Where the rest of the rules live

A folder's `CLAUDE.md` loads when a file in that folder is read, and carries only traps learned by
getting something wrong. **A rule belongs there only if the mistake it prevents is made while
editing a file in that folder**; a rule whose mistake happens elsewhere stays here, however
specialised it looks. **Each folder's `README.md` is the authority on that layer's own
conventions, and the `CLAUDE.md` beside it does not restate them**, because two summaries of one
thing drift and the shorter one wins by being read first. Read both.

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
| any `.cs` | [`csharp.md`](.claude/rules/csharp.md) | ADR-0065, and `.editorconfig` |
| any `.cshtml` | [`razor.md`](.claude/rules/razor.md) | ADR-0072, and design `11` |
| any `.resx`, and `Resources/` | [`localization.md`](.claude/rules/localization.md) | ADR-0038 parameter G, design `11` 5.6, design `10` 5.4 |
| `.css`, `.html`, `.js`, and `wwwroot/` | [`html-css.md`](.claude/rules/html-css.md) | ADR-0091, ADR-0072, and design `11` |
| build and CI config | [`.claude/rules/build-and-ci.md`](.claude/rules/build-and-ci.md) | the files themselves |
| any prose | [`.claude/rules/writing-style.md`](.claude/rules/writing-style.md) | the Microsoft Style Guide |

`docs/kb/` has none on purpose: its README already carries the frontmatter shape, the no-H1 rule,
the `[[slug]]` link form, and the routing rule, and a file whose only content is "read the README"
is a drift surface buying nothing.

**A nested `CLAUDE.md` is not re-injected after `/compact`; only this root file is.** So anything
that must always hold belongs here, the folder files are best-effort, and after a compaction you
re-read the folder file before trusting its traps are in context.

**A skill is selected by its `description`, which is the only part in context until it is
invoked.** So a skill must not hold anything that must always hold, and must not restate a rule
loading beside it. Skills live in `.claude/skills/`, each naming the owner it defers to, and they
are **not listed here**, because the harness already injects every name and description.

## Ephemeral working areas (git-ignored, local-only)

`docs/superpowers/`, `.superpowers/`, and `docs/kb/.scratch/` are working artifacts, never
published. Clean with `git clean -Xfd docs/superpowers .superpowers docs/kb/.scratch`.

`.claude/worktrees/` is a fourth and behaves differently: each worktree holds its **own** `.git`,
so `git add .claude` stages a gitlink rather than files. Remove one with `git worktree remove`.
Whether `git clean` reaches a nested repository is **not verified**.

**A worktree changes how a gate's output reads.** With one present,
`markdownlint-cli2 "**/*.md"` counted 371 files against 190 without it (2026-08-07). The count
should equal `git ls-files '*.md' | wc -l`; `.claude/commands/gate.md` step 3 treats a mismatch
as the symptom.
