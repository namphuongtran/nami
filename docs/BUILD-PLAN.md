# Nami build plan

The work queue: what is being built next, and what is owed but not scheduled. Started
2026-08-02, after the fourth code increment, because the answer to "what is next" existed
only in a chat window and did not survive it.

**This file decides nothing.** Every row points at the ADR, design, or source file that
owns the item, and where a row and its owner disagree, **this file is the bug**. That is
the same rule [`adr/README.md`](adr/README.md) and
[`architecture/18-decisions-index.md`](architecture/18-decisions-index.md) state about
themselves, and it is stated here for the same reason: an index that starts answering
questions stops being an index.

What this file is **not**:

- Not the release gate. Human sign-offs live in
  [`PRE-GA-RATIFICATION-CHECKLIST.md`](PRE-GA-RATIFICATION-CHECKLIST.md).
- Not the milestone roadmap. M1 to M5 scope is the table in the root
  [`README.md`](../README.md).
- Not a record of what is enforced. Each ADR's Confirmation says whether its own mechanism
  is live, and that is the authority.

## 1. Next

In order. Each increment is one branch, small enough to review in one sitting.

| # | Scope | Blocked by |
|---|---|---|
| PR-5 | The ports that have a complete source, plus the DTOs they need | **Yes**, see below |
| PR-6 | `Nami.Identity.Core`: engine wiring, the builder, the first slice | PR-5 |

**PR-5 cannot start as written, and the blocker is documented rather than guessed.**
[`design/01-foundations.md:232`](design/01-foundations.md) states that none of the ten
ports in the catalogue can be written from this repository as it stands: four have no
members stated anywhere, three elide the task type on an `Async` member, three need a DTO
specified in another design, and one is a naming question. The same note says closing it is
per-port work owned by each port's own design, not an edit to that table. So the real next
step is to pick one port, read its owning design, and either write the contract there or
record what the design does not fix.

`ScopeDefinition` landed instead of a port for exactly this reason
([`../src/CLAUDE.md`](../src/CLAUDE.md) records how that was found: by trying to compile one
and failing).

## 2. Owed, with an owner and a trigger

Not scheduled. Each has a decision or a document that already names it.

| Item | Owner | Trigger |
|---|---|---|
| Whether `required` may stay on a public member | [`adr/0044-public-api-stability-and-semver.md:113`](adr/0044-public-api-stability-and-semver.md) | The first promotion of `Unshipped` to `Shipped` |
| Architecture rules (b) through (e): Application layering, slice decoupling, adapter placement, BFF isolation | [`adr/0024-architecture-style.md:55`](adr/0024-architecture-style.md) | When the projects they constrain exist |
| The licence-scan CI gate | [`adr/0026-dependency-license-policy.md`](adr/0026-dependency-license-policy.md) section C | M1 |
| Reconciling the stack-of-record table against `Directory.Packages.props` | [`adr/0061-technology-stack-of-record.md:84`](adr/0061-technology-stack-of-record.md) | M1, and it is no longer blocked: the manifest exists |
| The provenance and licence of `MSBuild.Caching.dll`, bundled in MinVer and declared in no `deps.json` | [`DEPENDENCY-LICENSES.md`](DEPENDENCY-LICENSES.md) section 3.2 | Before MinVer is adopted |
| Whether the `NU1901`-`NU1904` carve-out should be reversed once a blocking dependency-vulnerability gate exists | [`adr/0093-warnings-as-errors.md`](adr/0093-warnings-as-errors.md) parameter C | When ADR-0092 stage 2's Trivy scan lands, M1 |
| DocFX and `CS1591` at error on the public surface are stated by a design and owned by no ADR | [`design/21-cicd-and-deployment.md:232`](design/21-cicd-and-deployment.md) | M1 |

The DocFX row is an absence claim, so the search is recorded with it. Seven spellings were
searched across `docs/adr/` on 2026-08-03 and all seven returned nothing: `DocFX`, `docfx`,
`CS1591`, `1591`, case-insensitive `xml doc`, `GenerateDocumentationFile`, and
`documentation file`. `design/21-cicd-and-deployment.md:232-233` states the requirement; the
design layer realizes decisions and does not make them, so the entry has no owner.

## 3. Not verified

These are claims this repository has **not** established. None may be cited as fact until
read at source. MinVer's licence left this section on 2026-08-02: three documents disagreed,
the read was taken at the artifact, and the outcome is recorded by its owner in section 3.2 of
[`DEPENDENCY-LICENSES.md`](DEPENDENCY-LICENSES.md) rather than here.

- **Does the options binder populate `required` members?**
  [`../src/CLAUDE.md:49`](../src/CLAUDE.md) records the question against
  `design/23-configuration-and-client-declaration.md` section 6. It needs the configuration
  packages, which are not referenced yet.
- **Is `ITenantStore` Nami's own port, or the multi-tenancy library's type of that name?**
  [`design/01-foundations.md:256`](design/01-foundations.md) states it either way changes
  the answer: if it is the library's, declaring it in `Abstractions` would put a
  third-party dependency inside the assembly that must depend on nothing. Answerable only
  against a restored package graph.
- **A working-tree rewrite of `Nami.Identity.slnx` on 2026-08-02 has no identified cause.**
  The file was found rewritten with an empty `<Folder Name="/tests/" />`, dropping the test
  project. No commit carried that state. All eight gates plus the three self-tests were
  re-run against it and none reproduced it. The related failure mode was measured and is the
  reassuring direction: with the project directory absent, `dotnet build`, `dotnet test` and
  `dotnet format` all exit 1 rather than skipping silently. Recorded because an unexplained
  rewrite of the solution file is worth recognising on sight if it recurs.

## 4. Maintenance

- A row is added here **in the same change** that creates the item, in the shape the rest of
  this repository uses: what, who owns it, and what triggers it.
- A row is deleted when its owner records the outcome. It is never marked done here, because
  a second place recording completion is a second place to be wrong.
- Nothing in section 3 moves to section 1 or 2 on the strength of an argument. It moves when
  something has been read at source, and the row says where.
