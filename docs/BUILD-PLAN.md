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
| PR-5 | `IAuditSink` and `ISecurityEventSink`, plus `AuditEvent`, `SecurityEvent`, and `AuditChainEntry` | No |
| PR-6 | `Nami.Identity.Core`: engine wiring, the builder, the first slice | PR-5 |

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
| `KeyRecord`'s members, and the C# form of `KeyScope`, which are what still block `ISigningKeyStore` | [`design/12-key-management.md`](design/12-key-management.md) section 3.2 | The rotation subsystem, which is the first thing that needs the key store |
| Bootstrap 5 is a stack-of-record entry with no licence row and no version pin | [`DEPENDENCY-LICENSES.md`](DEPENDENCY-LICENSES.md), and [`adr/0026-dependency-license-policy.md`](adr/0026-dependency-license-policy.md) section C | Before Bootstrap is taken, and no later than the first `.cshtml` |
| No colour palette, contrast target, or accessibility standard is decided for the end-user surface | [`adr/0072-ui-rendering-stack.md`](adr/0072-ui-rendering-stack.md) owns the surface; [`design/11-login-consent-ui.md`](design/11-login-consent-ui.md) section 5.5 bounds the tokens | Before the login pages ship |
| No tool is decided for driving a browser to author an end-to-end test, and adopting one is an inventory row rather than a configuration file | [`adr/0025-local-development-and-first-run.md`](adr/0025-local-development-and-first-run.md) parameter E scopes Playwright and names no authoring tool; [`adr/0026-dependency-license-policy.md:55`](adr/0026-dependency-license-policy.md) sets what the adoption owes | Before the first admin end-to-end test, M1 |
| Playwright still has no row in the licence record, and it is the first dependency here read to bundle a second licence behind a correctly declared one. The three prose sources were corrected on 2026-08-07; the record itself was not written | [`DEPENDENCY-LICENSES.md`](DEPENDENCY-LICENSES.md) section 3, where section 3.2 is the precedent: a package not yet adopted, recorded because documents here stated its licence wrongly | Before the Playwright pin is added |
| Two citation defects that `.claude/rules/localization.md` inherited rather than introduced, read at source 2026-08-07. **(a)** `razor.md:94` and `html-css.md:114` both cite `ADR-0092:147` for the four-item quote "Razor markup, SQL held outside C#, Dockerfiles, or GitHub Actions workflow definitions", whose fourth item completes at `ADR-0092:148`. **(b)** `razor.md:99` and `html-css.md:121` both say "The local hook stages `-- '*.md'`", but `scripts/hooks/pre-commit:25` reads a list that is **already** staged rather than staging anything. Four instances across two files. `localization.md` carries the corrected form of both, so three rules files now spell the same two facts two ways | [`../.claude/rules/razor.md`](../.claude/rules/razor.md) and [`../.claude/rules/html-css.md`](../.claude/rules/html-css.md), each for its own two lines | The next edit to either file. Out of scope on 2026-08-07 for two reasons: the approved spec for that increment covered `localization.md` only, and a parallel session had both files staged |

The DocFX row is an absence claim, so the search is recorded with it. Seven spellings were
searched across `docs/adr/` on 2026-08-03 and all seven returned nothing: `DocFX`, `docfx`,
`CS1591`, `1591`, case-insensitive `xml doc`, `GenerateDocumentationFile`, and
`documentation file`. `design/21-cicd-and-deployment.md:232-233` states the requirement; the
design layer realizes decisions and does not make them, so the entry has no owner.

**The accessibility row and the Bootstrap row arrived on 2026-08-07 with
[`../.claude/rules/html-css.md`](../.claude/rules/html-css.md), and both are absence claims, so both
carry their searches.** They are named here rather than placed, because two further rows landed
below them the same day and "the last two rows" stopped resolving to them. The accessibility row:
twenty-one spellings returned zero hits each over
every tracked file at `10df955`, listed in that file's "What is genuinely not decided" section, and
`WCAG` is one of them. Three places do resolve, and naming them here stops a later reader
re-finding them as coverage. `adr/0042-abuse-and-bot-defense.md:70` is a consequence of rejecting a
CAPTCHA, and `design/16-admin-app.md:27` and `:126` are the **admin console**, which "uses its own
theme" and is not the end-user surface. So design `16`:126 is the only accessibility posture decided
anywhere, and it does not reach the login pages. The Bootstrap row: `adr/0072-ui-rendering-stack.md`
line 103 credits ADR-0026 with requiring a permissive licence for Bootstrap, and searching
`adr/0026-dependency-license-policy.md` for `bootstrap`, `css`, `frontend`, `front-end`, `npm`, and
`javascript` returned zero hits the same day, so the rule is the general policy applied rather than
a specific one to quote. `DEPENDENCY-LICENSES.md` has no Bootstrap row: its only `bootstrap` hit is
line 114, inside the JMeter bundle enumeration.

**The browser-driving row and the Playwright licence row arrived on 2026-08-07 with
[`../.claude/skills/generating-a-playwright-test/SKILL.md`](../.claude/skills/generating-a-playwright-test/SKILL.md),
and the first is an absence claim, so it carries its search.** Ten spellings returned zero hits
each over every tracked file at `10df955`, outside `.claude/skills/`: `playwright mcp`,
`.mcp.json`, `code generation`, `record the browser`, `browser automation`, `test generation`,
`generate a test`, `scaffold a test`, `trace viewer`, and `inspector`. Two searches did return
hits and neither is a tool this repository runs, so naming them stops a later reader re-finding
them as coverage. `codegen` returned `.gitignore:271`, which is `orleans.codegen.cs` in the
standard ignore template. `mcp server` returned five files, and all five are the
authorization-server role ADR-0064 proposes rather than a development tool. The Playwright
licence row is not an absence claim: `DEPENDENCY-LICENSES.md` still has **no** `playwright` hit,
and that missing row is the item.

## 3. Not verified

These are claims this repository has **not** established. None may be cited as fact until
read at source.

- **Does the options binder populate `required` members?**
  [`../src/CLAUDE.md:49`](../src/CLAUDE.md) records the question against
  `design/23-configuration-and-client-declaration.md` section 6. It needs the configuration
  packages, which are not referenced yet.
- **Is `ITenantStore` Nami's own port, or the multi-tenancy library's type of that name?**
  [`design/01-foundations.md`](design/01-foundations.md) section 3.3 states it either way changes
  the answer: if it is the library's, declaring it in `Abstractions` would put a
  third-party dependency inside the assembly that must depend on nothing. Answerable only
  against a restored package graph.
- **Does a `paths:` glob in `.claude/rules/` actually load the file when a matching file is
  edited?** [`../.claude/rules/csharp.md`](../.claude/rules/csharp.md) landed 2026-08-05 and its
  whole value depends on this. Two halves, and only the first is established. **The gating is
  real**: in the session that wrote the file, the two rules files with no `paths` field were
  present in context and [`../.claude/rules/build-and-ci.md`](../.claude/rules/build-and-ci.md),
  the only one carrying the field, was absent. **The matching is not**: reading
  `src/Nami.Identity.Abstractions/ScopeDefinition.cs` did not load `csharp.md`, and reading
  `.editorconfig` did not load `build-and-ci.md` either, so the observation says nothing about
  either glob. It is consistent with evaluation at session start rather than on file access, which
  would make a mid-session read the wrong test. The file carries `**/*.cs` plus the explicit
  `src/**/*.cs` and `tests/**/*.cs`, mirroring the `src/**/*.csproj` form already in
  `build-and-ci.md`, because no form is proven. Answerable by starting a session with a `.cs` file
  in play and reading what loaded. Until then, treat all **five** rules files carrying the field
  as best-effort in the same way a nested `CLAUDE.md` is after a `/compact`. Counted 2026-08-07
  with `grep -l "^paths:" .claude/rules/*.md`: `build-and-ci.md`, `csharp.md`, `html-css.md`,
  `localization.md`, and `razor.md`. The count was two until 2026-08-07, when
  [`../.claude/rules/razor.md`](../.claude/rules/razor.md) added the third,
  [`../.claude/rules/html-css.md`](../.claude/rules/html-css.md) added the fourth later the same
  day, and [`../.claude/rules/localization.md`](../.claude/rules/localization.md) added the fifth
  the same day again. **Two of the five tie for weakest case**, because neither glob has anything
  to match yet: `git ls-files '*.css' '*.html' '*.js'` and `git ls-files '*.resx'` both returned
  nothing on 2026-08-07, so `html-css.md` waits for the login surface and `localization.md` waits
  for the first resource file.
  **One adjacent observation from that session, recorded for its limits rather than its result.**
  Reading `docs/design/11-login-consent-ui.md` mid-session did inject `docs/CLAUDE.md` and
  `docs/design/CLAUDE.md`, so on-file-access loading is real in this harness and evaluation is not
  only at session start. That is the **folder `CLAUDE.md`** mechanism and not the `paths:`
  frontmatter, so it does not answer the question above. It removes one of the two explanations
  the row offers for the negative result, and nothing more.
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
