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
| PR-7 | `Nami.Identity.Core`: engine wiring, the builder, the first slice | **Yes**, on two things named in section 2: the builder's two types have no member stated anywhere, and the engine has no version pin and no licence row |

**What happened to PR-5 and PR-6 on 2026-08-08, stated plainly because an earlier draft of
this paragraph got it wrong.** The two rows went for two different reasons and only one of
them is the section 4 rule.

- **PR-5 was the audit pair and their three DTOs. It landed on 2026-08-05 as `1511e8e`, and
  its row should have gone with that commit.** [`../src/CLAUDE.md`](../src/CLAUDE.md) recorded
  the outcome at the time, so section 4's rule applied three days before the row was actually
  deleted. The row outlived its increment through five further edits to this file. That is
  the miss this file's maintenance rule exists to prevent, and naming it is the point.
- **PR-6 was `Nami.Identity.Core`, and it has never meant anything else.** It was created with
  that scope on 2026-08-02 in `6dd786c`. It has no outcome, so section 4's rule does not reach
  it. It was **renumbered to PR-7**, carrying the same scope text, once the two blockers in
  section 2 were established. An earlier draft said PR-6 had been the definition model and
  that both rows fell to the section 4 rule. Both halves were false, and the definition model
  was unplanned work that no row ever asked for.

Numbers are not reused. PR-6 is retired rather than reassigned.

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
| `NamiIdentityOptions` and `INamiIdentityBuilder` are named and never defined, so `Nami.Identity.Core` cannot be written. This is an absence claim and carries its search below | [`design/01-foundations.md`](design/01-foundations.md) section 3.4, and an ADR if the members turn out to be decisions rather than a transcription | Before `Nami.Identity.Core` |
| The engine has no version pin and no licence row, which blocks `Core` a second and independent time. `Directory.Packages.props` carries no OpenIddict `PackageVersion` row, and [`DEPENDENCY-LICENSES.md`](DEPENDENCY-LICENSES.md) returned zero hits for `openiddict` on 2026-08-08 | [`adr/0021-openiddict-version-adaptation.md`](adr/0021-openiddict-version-adaptation.md) parameter A for the bracket pin, and [`adr/0026-dependency-license-policy.md`](adr/0026-dependency-license-policy.md) for the licence read at the distributed artifact | Before `Nami.Identity.Core` |
| [`design/23-configuration-and-client-declaration.md:153`](design/23-configuration-and-client-declaration.md) lists `BackchannelLogoutUri` in its "Definition field" column, and its own class diagram at `23:70-88` does not declare it. Three sources put the field on the Application write path instead: [`design/15-admin-api.md:133`](design/15-admin-api.md) and `:141` carry it on `ApplicationDto` and `ApplicationPolicyDto`, and [`adr/0019-single-logout-strategy.md:49`](adr/0019-single-logout-strategy.md) calls it "a new field on the Application". So `ClientDefinition` was landed with seventeen members and not eighteen | [`design/23-configuration-and-client-declaration.md`](design/23-configuration-and-client-declaration.md) section 4, which is the table that carries the row | The next edit to design 23, or the admin API increment, whichever comes first. Out of scope on 2026-08-08 because correcting a design is its own decision, and the approved plan for that increment was to flag it |
| [`design/23-configuration-and-client-declaration.md`](design/23-configuration-and-client-declaration.md) section 9 lists seven test bullets and **none covers the definition model's own defaults**, which section 8 of the same document calls the entire security argument for the layer. The tests now exist and the design still does not ask for them, so what is left is the document owing an eighth bullet. The other six bullets stay blocked on the mapper, the seeder, or the configuration binder | [`design/23-configuration-and-client-declaration.md`](design/23-configuration-and-client-declaration.md) section 9 owns the test list | The next edit to design 23, or the mapper, whichever comes first |
| Whether a test asserting a safe **default** is a "security test" under [`adr/0062-owasp-asvs-security-baseline.md:43`](adr/0062-owasp-asvs-security-baseline.md), and so owes an ASVS 5.0 requirement identifier. The twelve unit facts landed 2026-08-08 carry none. The reason is that both that clause and [`design/20-testing.md:87-91`](design/20-testing.md) give **negative** tests as their examples, and ASVS 5.0 renumbered its chapters, so guessing an identifier is the defect `design/20-testing.md:358` names | [`adr/0062-owasp-asvs-security-baseline.md:81`](adr/0062-owasp-asvs-security-baseline.md), which owns the tagging as a build-time item | The first negative security test, or the ASVS 5.0 self-assessment, whichever comes first |
| ADR-0060 owes a confirmation of the test taxonomy against the real suites at M1. Two of its seven types now exist, unit and architecture, so the item is **started and not closed**. Part of it is already visible: the architecture suite's two method names are not Given/When/Then, which `adr/0060-testing-strategy.md:61` and `adr/0065-coding-and-naming-conventions.md:88` make binding. They were not renamed on 2026-08-08, because `adr/0060-testing-strategy.md:76` records that "behavior, not implementation" is a judgment call that "some genuinely white-box tests (a hash-chain link, a handler order) strain" and an ArchUnitNET rule check is not a scenario | [`adr/0060-testing-strategy.md:69`](adr/0060-testing-strategy.md), and [`design/20-testing.md`](design/20-testing.md) section 10 | The integration suite, which is the first one needing a container |
| `AccessTokenType` is a closed two-value domain (`jwt` or `reference`) typed as `string`, while two other closed sets in the same class diagram are enums. No invariant in section 5.1 checks it, and an unrecognized value most plausibly reads as `jwt`, which silently returns a client the operator opted into reference tokens to a self-contained JWT. Case sensitivity is undecided as well | [`design/23-configuration-and-client-declaration.md`](design/23-configuration-and-client-declaration.md) section 3 for the type, and section 5.1 for the missing invariant | Before the ADR-0039 token-type handler, and before any promotion to `PublicAPI.Shipped.txt`, because ADR-0044 makes the type change MAJOR after that |
| Five invariants that no section 5.1 rule covers: the `AccessTokenType` domain, `AuthMethod` agreeing with the credential actually present, `AbsoluteRefreshLifetime` being positive and inside the ADR-0004 ceiling, `AllowedCorsOrigins` being scheme, host, and port with no path, and a code-flow client having at least one redirect URI. Each is constructible today and passes all seven stated invariants | [`design/23-configuration-and-client-declaration.md`](design/23-configuration-and-client-declaration.md) section 5.1, which is the fail-closed list | The mapper |
| **Not verified**: whether `.ValidateDataAnnotations()` on `AddOptions<List<ClientDefinition>>()` validates the list's elements at all. `design/23:356-357` wires it and `23:454` requires a missing value to fail at start-up, but `ClientDefinition` carries no data-annotation attribute, so the call may read as enforcement while checking nothing. That is this repository's inert-gate pattern arriving through a design rather than through a script | [`design/23-configuration-and-client-declaration.md`](design/23-configuration-and-client-declaration.md) section 6 | When the configuration packages land, which is also what unblocks the `required`-binder question in section 3 |

**The builder row is an absence claim, so it carries its search.** Counted 2026-08-08 with
`git grep -c` over every tracked file **except this one**, which is a plain substring that
over-counts and never under-counts: `NamiIdentityOptions` returned 2 and `INamiIdentityBuilder`
returned 2, all four hits in [`design/01-foundations.md`](design/01-foundations.md). **The
exclusion is load-bearing and was added after a review.** Writing this row put both terms into
this file twice each, so a re-run over every tracked file now returns 4 apiece and the claim
would falsify itself. [`../.claude/rules/localization.md`](../.claude/rules/localization.md)
already carried the same exclusion for the same reason. The method was proved
on a term known present in the same run, `ClientDefinition` returning 11 across 3 files. The
two lines are `01:110`, a package table cell naming the type, and `01:305`, prose naming it
inside a signature. Neither gives a member, a type, or a nullability. Section 3.4 is a table
of option names, defaults, and owning ADRs, and not a class diagram, so it fixes what the
options mean and not what the type is. This is the same shape as the `ISecretResolver` finding
in [`../src/CLAUDE.md`](../src/CLAUDE.md): the type is named everywhere and writable nowhere.

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
  the same day again. **Three of the five tie for weakest case**, because no glob in any of the
  three matches a tracked file yet. Counted 2026-08-07: `git ls-files '*.css' '*.html' '*.js'`,
  `git ls-files '*.cshtml' '*.cshtml.cs'`, and `git ls-files '*.resx'` each returned nothing, and
  no tracked path contains `wwwroot`, `Resources/`, or `Pages/`. So `html-css.md` waits for the
  login surface, `razor.md` waits for the first page, and `localization.md` waits for the first
  resource file. Only `csharp.md` and `build-and-ci.md` match anything today: 7 `.cs` files, 2
  `.csproj`, 2 `.props`, and 1 workflow file.
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
