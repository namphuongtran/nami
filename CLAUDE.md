# CLAUDE.md

Rules that are true **everywhere**. Rules true only inside one folder live in that
folder, and the last section says where.

## Read the decision before you change anything

Nami's product is expressed as decisions, so the ADR corpus in `docs/adr/` **is** the
architecture. Before you brainstorm, implement, or fix anything, read the three layers
that govern it:

1. **The ADR that decides it.** Index: [`docs/adr/README.md`](docs/adr/README.md).
   Accepted ADRs are binding until superseded.
2. **The detailed design that realizes it.** Index:
   [`docs/design/README.md`](docs/design/README.md). A design never makes a decision; if
   one seems to, that is the bug.
3. **The architecture view that synthesizes it.** Index:
   [`docs/architecture/README.md`](docs/architecture/README.md), and
   [`docs/architecture/18-decisions-index.md`](docs/architecture/18-decisions-index.md)
   maps every ADR to the views citing it, which is the fastest route from a topic to the
   full set.

**Name what you read, by file and line.** An unnamed source is unverifiable, and naming
it is what makes the reading checkable rather than claimed.

**If a source is wrong, contradicts another layer, or does not cover the case: stop and
flag it, quoting both sides at file and line.** Do not fill the gap from judgement, and
do not proceed as if it were covered. Research it, or say "not verified" out loud.
Saying so is the deliverable. [`src/CLAUDE.md`](src/CLAUDE.md) records what skipping this
costs: a port that could not be written at all, because its design omitted a return type,
discovered only by trying to write it.

## What this repository is

Nami is an open-source, multi-tenant OAuth 2.0 / OpenID Connect identity provider for
.NET built on OpenIddict, Apache-2.0 licensed, and an alternative to commercial identity
servers. It is **pre-alpha**: the architecture is fully designed and its risk spikes were
validated in a separate design corpus, while this repository holds the decision records,
governance, and docs scaffolding. `src/` began on 2026-08-02 with
`Nami.Identity.Abstractions`, and `tests/` on the same day with
`Nami.Identity.ArchitectureTests`.

**Starting a session with no other instruction? Read
[`docs/BUILD-PLAN.md`](docs/BUILD-PLAN.md) first.** It holds what is built next and what
blocks it, what is owed with the trigger that comes due, and what has not been verified. It
exists because that answer used to live only in a conversation and did not survive one. It
is a queue pointing at owners, never an authority; a row disagreeing with the ADR or design
it cites is a bug in the row.

## Commands

```bash
bash scripts/hooks/pre-commit                          # guardrail + decisions index + name scrub
bash scripts/check-adrs.sh                             # docs guardrail; reads the git INDEX, so `git add` first
python3 scripts/check-decisions-index.py               # verifies what each index row says, not just that it exists
bash scripts/test-check-adrs.sh                        # self-test: the guardrail
bash scripts/test-editorconfig.sh                      # self-test: the C# style ruleset
bash scripts/test-public-api-gate.sh                   # self-test: the public-API lock and CPM
bash scripts/test-warnings-as-errors.sh                # self-test: the warning gate and the two analyzer axes
npx --yes markdownlint-cli2@0.23.1 "**/*.md"           # version-coupled to ci.yml, see .claude/rules/build-and-ci.md
dotnet build Nami.Identity.slnx --nologo
dotnet test Nami.Identity.slnx --nologo                # architecture rules (ADR-0024)
dotnet format Nami.Identity.slnx --verify-no-changes   # drop the flag and it fixes

git config core.hooksPath scripts/hooks                # enable the local hook, once per clone
```

[`scripts/README.md`](scripts/README.md) is the authority on what each gate checks and
why; this list is not a summary of it.

**The hook runs two of the nine gates, so a green hook is not a green build.** That has
already produced a commit message claiming a self-test was green before it had been run.

**Four of the nine are self-tests, and that ratio is deliberate.** Each was written
after a real inert-gate defect rather than as a precaution: an untracked file the
guardrail could not see, a severity that failed nothing without an MSBuild property, an
`RS0017` that no `.editorconfig` placement could reach, and a warning gate whose subject
produces no warnings at all, so the ordinary build is green whether or not it is armed.
**When you add a gate, ask what would have to break for it to go quiet, then write that
break down as a test.**

The test gate arrived on 2026-08-02 with the first suite, the architecture rules of
ADR-0024. The license-scan gate is still owed and arrives with M1.

## Evidence rule (non-negotiable, applies to every layer)

**Never write a claim you have not read at its source, and never infer one from a title,
a filename, or a neighbouring document.** This outranks fluency: an unsourced sentence
that reads well is worse than an omission, because a later reader cannot tell them apart.

- **Quote before you assert.** Citing `ADR-NNNN` for a fact means the fact is *in* that
  ADR, not adjacent to its subject. A true fact with the wrong owner is a defect, and it
  is the most common defect this repository has had.
- **The dangerous citation is the one that resolves.** A pointer to a file that exists
  passes every mechanical check; what fails is that the file lacks the claim. A green
  checker is therefore not evidence about a citation's content. Two shapes deserve
  suspicion before any tool runs: a citation at the end of a **compound** sentence, where
  the pointer attaches to the wrong clause, and a bundle of items behind one reference.
- **Show the evidence before making the change.** Present both sides with file and line
  before editing. The user decides on evidence, not on a summary.
- **Count what you counted.** Never write "the second time", "three sources agree", or
  "every X" without enumerating them. Say "in `file:line`" instead of an unrun tally.
- **A measurement is dated.** Any figure produced by running something is a measurement
  with a date. Re-run it rather than citing it forward, and never edit a dated
  measurement to match today: that stops it being evidence.
- **Where this repo says more than its source, name the real source.** Inventing support
  for a correct-sounding claim is the worst failure mode available here.
- **A stated value is not a known default.** A source saying "X is set to Y" says nothing
  about what X would be if nobody set it. "Y is set explicitly because the default is
  weaker" is a *second* claim needing a *second* source, and it is the shape
  self-generated errors take here.
- **"No source says X" is a claim about a search.** Enumerate the spellings X can take
  and write down which searches you ran, inside the claim. A bare absence gets quoted
  forward; one with its method attached can be re-checked.
- **Never edit a document to silence a checker.** If a checker is wrong, fix the checker
  or record the finding as legitimate; if neither is cheap, delete the checker. A tool
  that bends the evidence it exists to protect is worse than none. `scripts/review/` was
  deleted for exactly this.
- **A false positive in a checker is a defect in the checker**, not noise to work around,
  and so is a claim that survives only because nobody checked it.

## Non-negotiable content rules

Legal and OSS constraints; the CI guardrail and the local hook enforce parts of them.

- **Never name the direct commercial competitor or its vendor, and never name real client
  organizations**, in any committed or public file. Generalize instead. The real-name
  list is deliberately **local and git-ignored** (`scripts/.local/name-denylist`); do not
  commit it, and do not add a public denylist, because publishing the list would leak the
  names it exists to hide.
- **One exception, decided 2026-07-25: OSS packages Nami depends on keep their real
  package identifiers**, even when the identifier carries the vendor's name, because
  hiding it makes the dependency record wrong and unusable by the ADR-0026 license-scan
  gate, which matches exact package IDs. It does **not** cover product comparison, parity
  framing, the vendor's internal source or type references, its issue tracker and blog
  posts, or commercial packages Nami rejects. Exemptions live in the git-ignored
  `scripts/.local/name-allowlist`; the policy itself belongs in ADR-0026.
- **No template placeholders** in tracked markdown: the curly-brace `Product`, `Company`,
  and `domain` tokens must never appear (guardrail Check 1). `scripts/README.md`
  describes them in prose to avoid tripping its own check; do not reintroduce the braces.
- **Permissive dependencies only** (MIT/Apache-2.0/BSD-class). No copyleft,
  source-available, or commercial packages. ADR-0026 is the policy; read a licence at the
  distributed artifact, never from a badge or from another document in this repository.
- **No em dash** in prose you write for this project.

## Git and contribution workflow

- **DCO sign-off on every commit** (`git commit -s`); this repo uses the DCO, not a CLA.
- **Conventional Commits**; the changelog is generated from them.
- **The maintainer works directly on `main`** (2026-08-02, replacing a branch-to-PR-to-merge
  flow that ran for four increments). Commit and push when an increment is finished and its
  gates are green, not before and not spontaneously: **the plan still needs approval before
  the work starts**, and it is only the branch and the pull request that stopped being
  required. An outside contribution goes through a pull request as
  [`CONTRIBUTING.md`](CONTRIBUTING.md) describes, and nothing here changes that. One ADR per
  commit when authoring or importing ADRs.
- **ADR-0065 is the authority on naming and coding conventions** (Microsoft conventions
  adopted by reference, with the Nami tailoring). Quick reference: assemblies under
  `Nami.Identity.*`; config keys `Nami:X` (env `Nami__X`), env alias `NAMI_X`.

## Where the rest of the rules live

A folder's `CLAUDE.md` loads when a file in that folder is read, and carries only the
traps learned by getting something wrong. **A rule belongs in a folder's `CLAUDE.md` only
if the mistake it prevents is made while editing a file in that folder**; a rule whose
mistake happens elsewhere stays here, however specialised it looks.

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
| build and CI config | [`.claude/rules/build-and-ci.md`](.claude/rules/build-and-ci.md) | the files themselves |

`docs/kb/` has none on purpose: its README already carries the frontmatter shape, the
no-H1 rule, the `[[slug]]` link form, and the routing rule, and a file whose only content
is "read the README" is a drift surface that buys nothing.

**A nested `CLAUDE.md` is not re-injected after `/compact`; only this root file is.** So
anything that must always hold belongs here, and the folder files are best-effort by
construction. If a session has been compacted, re-read the folder file before trusting
that its traps are still in context.

## Ephemeral working areas (git-ignored, local-only)

`docs/superpowers/` (specs + plans), `.superpowers/` (SDD ledgers/briefs/reports), and
`docs/kb/.scratch/` are working artifacts, never published. Clean with
`git clean -Xfd docs/superpowers .superpowers docs/kb/.scratch`.
