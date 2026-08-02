# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in
this repository. It carries the rules that are true **everywhere**. The rules that are
true only inside one folder live in that folder, and the next section says where.

## Where the rest of the rules live

Claude Code reads this file at the start of a session, and reads a `CLAUDE.md` in a
subdirectory when it reads a file in that subdirectory. A rule placed in a folder
therefore does not exist until you are working in that folder, which fixes the split:

**A rule belongs in a folder's `CLAUDE.md` only if the mistake it prevents is made while
editing a file in that folder.** A rule whose mistake is made somewhere else stays here,
however specialised it looks. "Never edit a document to silence a checker" is a rule
about checkers and it lives here, not in `scripts/`, because the file that gets edited
is a document.

**Each folder's `README.md` is the authority on that layer's own conventions, and the
`CLAUDE.md` beside it does not restate them.** Two summaries of one thing drift and the
shorter one wins by being read first, which is stated at
`docs/architecture/11-cross-cutting-concepts.md:12-16` and was demonstrated by this file:
until 2026-08-01 it said ADRs `0000`-`0035` were imported from the design corpus, while
`docs/adr/0000-use-markdown-architectural-decision-records.md:37` says `0000` is this
repository's own decision and the imported range is `0001`-`0035`. So a folder's
`CLAUDE.md` carries only what its README does not: the traps learned by getting
something wrong. Read both.

| Working in | Instruction file | Authority on the layer itself |
|---|---|---|
| anywhere under `docs/` | [`docs/CLAUDE.md`](docs/CLAUDE.md) | [`docs/README.md`](docs/README.md) |
| `docs/adr/` | [`docs/adr/CLAUDE.md`](docs/adr/CLAUDE.md) | [`docs/adr/README.md`](docs/adr/README.md) |
| `docs/architecture/` | [`docs/architecture/CLAUDE.md`](docs/architecture/CLAUDE.md) | [`docs/architecture/README.md`](docs/architecture/README.md) |
| `docs/design/` | [`docs/design/CLAUDE.md`](docs/design/CLAUDE.md) | [`docs/design/README.md`](docs/design/README.md) |
| `docs/kb/` | none, deliberately | [`docs/kb/README.md`](docs/kb/README.md) |
| `scripts/` | [`scripts/CLAUDE.md`](scripts/CLAUDE.md) | [`scripts/README.md`](scripts/README.md) |
| `src/` | [`src/CLAUDE.md`](src/CLAUDE.md) | ADR-0065, and design `01` section 3.1 |

`docs/kb/` has no `CLAUDE.md` on purpose: its README already carries the frontmatter
shape, the no-H1 rule, the `[[slug]]` link form, and the routing rule, and a file whose
only content is "read the README" is a drift surface that buys nothing.

Because a subdirectory file may load without its parent, each `CLAUDE.md` under `docs/`
names `docs/CLAUDE.md` explicitly rather than assuming it is already in context.

## What this repository is

Nami is an open-source, multi-tenant OAuth 2.0 / OpenID Connect identity provider
for .NET, built on OpenIddict, an Apache-2.0 alternative to commercial identity
servers. It is in **pre-alpha**: the architecture is fully designed and its risk
spikes were validated with runnable code, but that code lives in a separate design
corpus. This repo currently holds the **decision records, governance, and docs
scaffolding**; application source lands under `src/` starting at milestone M1
(`src/.gitkeep` is the placeholder). The only executable code today is in `scripts/`:
the docs guardrail, and since 2026-08-02 a self-test for the C# style ruleset, which
generates the C# it checks because the repository has none to offer it.

**The first project landed 2026-08-02**, `Nami.Identity.Abstractions`, holding one type.
The paragraph above is therefore about to be wrong rather than already wrong: the ADR
corpus is still what this repository mostly is, and `src/` is one three-property class.
What changed is that the gates now read C# from this repository instead of from a fixture
they generate, and that `src/CLAUDE.md` exists, carrying the traps found by landing it.
`tests/` is still a placeholder, its location taken from the design corpus, which is the
only source that states one.

Because the product is expressed as decisions, the ADR corpus in `docs/adr/` **is**
the architecture. Read the relevant ADRs before proposing changes to behavior they
govern, accepted ADRs are binding until superseded.

## Commands

```bash
# The whole local gate, in one place. Also available as the /gate slash command.
bash scripts/hooks/pre-commit                          # guardrail + decisions index + name scrub
npx --yes markdownlint-cli2@0.23.1 "**/*.md"           # the one thing the hook omits

# Docs guardrail alone: a CI gate. Must pass before any docs/ADR change merges.
# It reads tracked files, so `git add` first. It now warns when untracked markdown
# exists rather than reporting a green that covered nothing.
bash scripts/check-adrs.sh

# The second CI gate, since 2026-08-02. Check 7 above verifies that every ADR has a
# row in the reverse index; this verifies what the row says. Guardrail-green with a
# wrong cell is the failure it exists for, and that is how it was proven before wiring.
python3 scripts/check-decisions-index.py

# The C# style ruleset, since 2026-08-02. Its own CI job, because it needs a .NET SDK.
# There is no C# here yet, so it builds a throwaway project in .editorconfig-probe/
# against the real .editorconfig and Directory.Build.props, and asserts the rules fire
# on BOTH paths: `dotnet build` and `dotnet format --verify-no-changes`. Those are not
# one gate under two names, and each catches a break the other sleeps through.
# Skips with exit 0 when dotnet is absent, and says a skip is not a pass.
bash scripts/test-editorconfig.sh

# The public-API gate, since 2026-08-02. Its own CI job, for the same SDK reason and
# for one more: a red here means the GATE stopped biting, not that the code is wrong.
# Writes a throwaway project to .publicapi-probe/ against the real .editorconfig,
# Directory.Build.props and Directory.Packages.props, then breaks it six ways and
# asserts each break is caught. It exists because one third of that gate was inert on
# the day it landed and nothing would have noticed it coming back.
bash scripts/test-public-api-gate.sh

# The solution build and the format gate, since 2026-08-02. One CI job, two steps,
# and NOT one gate under two names: the format path needs no EnforceCodeStyleInBuild,
# reports whitespace as WHITESPACE rather than IDE0055, and exits 2 rather than 1.
# Both read real C# as of the first project. Measured against a planted `badlyNamed`
# private field: build exits 1, format exits 2, both on IDE1006.
dotnet build Nami.Identity.slnx --nologo
dotnet format Nami.Identity.slnx --verify-no-changes   # drop the flag and it fixes

# Enable the opt-in local pre-commit hook (both gates + local name-scrub). Per clone.
git config core.hooksPath scripts/hooks
```

**The hook runs two of the seven gates**, so a green hook is not a green build. The
markdownlint line above, `scripts/test-editorconfig.sh`, `scripts/test-public-api-gate.sh`,
the solution build and the format gate are separate CI steps, and so is
`scripts/test-check-adrs.sh`. This has already produced a commit message that claimed a
self-test was green before it had been run.

**Three of the seven are self-tests, and that ratio is deliberate.** A gate that has never
been run against the bug it exists for is not known to work, and each of the three was
written after a real inert-gate defect rather than as a precaution: an untracked file the
guardrail could not see, a severity that failed nothing without an MSBuild property, and an
`RS0017` that no `.editorconfig` placement could reach. **When you add a gate, ask what would
have to break for it to go quiet, then write the break down.** A gate with no self-test is
the shape all three of those defects had.

**`global.json` is a pin that can be inert, and the shape that makes it inert is the
one the design corpus writes.** Measured 2026-08-02 on SDK 10.0.301: `10.0.999` with
`rollForward: disable` fails a build with exit 155, so a real pin bites; but `9.0.x`
with the same `disable` exits 0 on a machine carrying no 9.0 SDK, because a `version`
string the SDK cannot parse makes the whole `sdk` block inert. The corpus writes
`"10.0.x"`. This repository writes `10.0.100` with `latestFeature`, which is a parseable
floor rather than a wildcard, and a wildcard is not available: the `rollForward` key is
what expresses the range.

The lint version is not a preference: it is the version bundled by the version-pinned
action in `ci.yml` (ADR-0086 parameter B), so bump both or neither. That coupling is the
half of ADR-0086 that survived its parameter A reversal on 2026-08-02, and it is the half
that caught the only real drift this repository has had. **Every `uses:` in `ci.yml` is a
full version tag, `@v7.0.1`, never `@v7` and never a commit SHA**, and `check-adrs.sh`
Check 8c fails a build on any other form. `.markdownlint-cli2.jsonc`
sets `gitignore: true` so the glob reads the same file set CI reads; without it the
same command also walks the git-ignored draft areas and reports errors CI can never
see.

There is no build or test suite yet; the .NET build/test/license-scan CI gates are
added when the solution lands (see the comment at the end of `.github/workflows/ci.yml`).
CI does install a .NET SDK, for the style-ruleset job above, and that job is not one of
those gates: it reads no project in this repository, because there are none.

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
- **The dangerous citation is the one that resolves.** A pointer to a file that exists
  passes every mechanical check; what fails is that the file does not contain the claim.
  So a green checker is not evidence about a citation's content, and the two shapes that
  deserve suspicion before any tool runs are a citation at the end of a **compound**
  sentence, where the pointer silently attaches to the wrong clause, and a bundle of
  several items behind one reference, where usually one of them belongs elsewhere. The
  counts and the three instances found in this repository are in
  [`docs/CLAUDE.md`](docs/CLAUDE.md).
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
- **Never edit a document to silence a checker.** This is the rule the deleted
  `scripts/review/` screens were removed for breaking: a keyword check flagged a correct
  citation, and the response was to drop `: true` from the claim value
  `memberships_truncated: true` so the check would pass. The document became less
  implementable to make a tool quieter. If a checker is wrong, fix the checker or record
  the finding as legitimate; if neither is cheap, delete the checker. A tool that bends
  the evidence it exists to protect is worse than none.
- **A false positive in a checker is a defect in the checker**, not noise to work
  around, and the same holds for a claim that survives only because nobody checked it.
  A checker that stays green on the bug it was written for is worse than none, because it
  converts an unchecked claim into a confident one.

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

## Git and contribution workflow

- **DCO sign-off on every commit** (`git commit -s`); this repo uses the DCO, not a CLA.
- **Conventional Commits** (`feat:`, `fix:`, `docs:`, `test:`, `ci:`, `chore:`, ...);
  the changelog is generated from these.
- Branch for changes; commit or push only when asked. This project's convention is
  **one ADR per commit** when importing/authoring ADRs.
- Naming and coding conventions: **ADR-0065 is the authority** (Microsoft naming +
  C# conventions adopted by reference, enforced via `.editorconfig` + analyzers,
  with the Nami tailoring). Quick reference: assemblies under `Nami.Identity.*`;
  config keys `Nami:X` (env `Nami__X`), env alias `NAMI_X`. The machine-enforceable
  rules live in `.editorconfig`, whose C# section landed 2026-08-02 ahead of any code,
  and in `Directory.Build.props` beside it. **Those two files are one mechanism, not
  two.** Error severity in `.editorconfig` fails nothing on its own; the property in
  `Directory.Build.props` is what makes it fail a build, measured. Editing either
  without the other is how the ruleset goes quiet while still reading as enforced,
  which is what `scripts/test-editorconfig.sh` is there to catch.
- **A severity is matched against the file a diagnostic is REPORTED IN, which is not
  always a `.cs` file.** Learned 2026-08-02 landing the ADR-0044 public-API analyzers:
  `RS0017` reports a stale entry inside `PublicAPI.Unshipped.txt`, so `[*.cs]` never
  matches it, and neither did a section naming the API files, nor `[*]`, nor a root
  `.globalconfig` added through an MSBuild item. It sat at its default of warning while
  reading as configured, and the case it uniquely covers, a public member deleted with
  its API-file lines left behind, built green. It is now `<WarningsAsErrors>` in
  `Directory.Build.props`. **Before trusting any severity line, break the rule and read
  the exit code**, because the placement that looks right is the one that fails quietly.

## Ephemeral working areas (git-ignored, local-only)

`docs/superpowers/` (specs + plans), `.superpowers/` (SDD ledgers/briefs/reports),
and `docs/kb/.scratch/` are git-ignored working artifacts, never published.
Clean with `git clean -Xfd docs/superpowers .superpowers docs/kb/.scratch`.
