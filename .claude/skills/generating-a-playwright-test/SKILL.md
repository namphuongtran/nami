---
name: generating-a-playwright-test
description: Use when turning a scenario into a Playwright browser test in the Nami repository, and when adopting published record-then-generate guidance such as the Playwright MCP test-generation workflow. This skill covers the authoring procedure only, and today no step of that published procedure can run here. No browser-driving tool is decided, no MCP server is configured, and no admin app exists to drive. The scenario is already written in three places, so asking for one is the first mistake. Read writing-playwright-tests for what the test itself must look like.
---

# Generating a Playwright test from a scenario

Read this before driving a browser, before adding an MCP server, and before asking anyone for a
scenario. It holds the authoring **procedure** and nothing else.

[`../writing-playwright-tests/SKILL.md`](../writing-playwright-tests/SKILL.md) is the one to read
first. It owns what a Playwright test here must look like: the package twin, the namespace, the
`ValueTask InitializeAsync` signature, the admin-only scope, the licence contradiction, and the
browser-matrix absence. Behind it,
[`../writing-tests/SKILL.md`](../writing-tests/SKILL.md) owns the seven-suite table, the
Given / When / Then form, and the no-assertion-library rule.
[`../../rules/csharp.md`](../../rules/csharp.md) loads on any `.cs` file.
[`../../../tests/CLAUDE.md`](../../../tests/CLAUDE.md) holds the traps learned inside `tests/`,
and it is **not** re-injected after `/compact`. None of that is restated below.

ADR-0025 parameter E is the authority on Playwright's scope, and ADR-0060 on the test strategy.
Neither is restated.

## What exists today, measured

Measured on 2026-08-07 at `10df955`. **No step of the published procedure can run here.** That is
the finding, not a caveat on it.

The workflow this skill was scoped from has five mechanical steps. Each one is blocked or wrong:

| The published step | Why it cannot run |
|---|---|
| Drive the interface with the tools of a Playwright MCP server | No MCP server is configured. There is no `.mcp.json`, and `playwright` returned zero hits across tracked `.json`, `.yml`, `.yaml`, `.props`, `.slnx`, and `.csproj` files |
| Ask the user for a scenario when none is given | The scenario is already written, in three places. See the section after next |
| Emit a TypeScript test that uses `@playwright/test` | Wrong stack. `../writing-playwright-tests/SKILL.md` names the C# package and its namespace, and both differ from the plainly named one |
| Save the generated file in the tests directory | Which directory is deferred. ADR-0060:69 says "when the test projects land (from M1), confirm this taxonomy against the real suites and adjust the naming/structure guidance to what the code shows" |
| Execute the file and iterate until the test passes | Nothing to execute. `src/` holds one project, `Nami.Identity.Abstractions`, which is a class library. There is no admin app, no Playwright pin, and no browser install step in `.github/workflows/ci.yml` |

So every rule below is derived from an accepted decision or read at an artifact. None was learned
by getting something wrong here, and that difference matters to a later reader. A
decision-derived rule has never been tested by use.

## "MCP" already names something else in this repository

Do not read an MCP mention here as a development tool. ADR-0064 is "Support Nami as the OAuth
authorization server for MCP servers" (ADR-0064:9), and it is `proposed` rather than accepted.

Searched on 2026-08-07 at `10df955`, case-insensitive over every tracked file outside
`.claude/skills/`. `mcp server` returned five files, and all five are that authorization-server
role: `docs/adr/0014-advanced-protocol-scope.md`,
`docs/adr/0064-mcp-authorization-server-support.md`, `docs/adr/README.md`,
`docs/architecture/18-decisions-index.md`, and
`docs/architecture/19-evolution-and-extensions.md`. **None is a tool this repository runs.**
`playwright mcp` and `.mcp.json` returned zero hits each.

So a Playwright MCP server is a tool this repository has not adopted. The next section is what
adopting one costs.

## The scenario is written, so do not ask for one

The published workflow asks the user for a scenario. Here that question produces a second
scenario competing with the one three documents already agree on.

| Source | What it holds |
|---|---|
| ADR-0025:62 | "UI end-to-end tests for the admin app use Playwright (login, propose, a second user approving with step-up, then executed, asserting no token in the browser)" |
| ADR-0060:62 | The same flow in Given / When / Then form, which is the form the test takes |
| `docs/design/16-admin-app.md`:241-244 | The same flow, plus eight further items the suite owes, including the ETag-conflict interaction and a back-channel logout |

ADR-0060:62 is the one to copy, because it is already written as a scenario:

> *Given* a proposal created by one admin, *when* a second admin approves it with step-up MFA,
> *then* the action executes and no token is exposed to the browser (ADR-0020, ADR-0025).

`../writing-playwright-tests/SKILL.md` records inventing a scenario as a trap. This section says
what to do instead, and it does not restate the trap.

## Adopting a browser-driving tool is a recorded procedure, not a setup step

A Playwright MCP server, or any other tool that drives a browser to author a test, is a separate
process rather than a package. So the restore-graph licence scan cannot see it. ADR-0026 section
C says exactly that, and says what stands in:

> **An external-tool inventory.** A tool executed as a separate process (a load-test binary, a
> conformance-suite container image) is not a package and is not in the restore graph, so the
> scanner cannot see it at all. Every such tool is listed in `docs/DEPENDENCY-LICENSES.md` with
> its licence, **where that licence was read**, and the date. The inventory is human-maintained
> in the same change that introduces the tool.

Read at ADR-0026:55. Four obligations follow. **Each is written down, and none is enforced by a
check that runs today.** The licence-scan gate is owed under ADR-0026 section C, and ADR-0026:59's
completeness check rides on it. So skipping any of the four produces a green build.

1. **A row in the inventory, written in the same change.** The table is
   [`../../../docs/DEPENDENCY-LICENSES.md`](../../../docs/DEPENDENCY-LICENSES.md) section 2, and
   its columns are Tool, Version read, Role, Licence, Boundary, Read at, Date, and Decision
   (line 40 on 2026-08-07). Four rows exist and each one is filled in.
2. **A boundary classification, which is load-bearing.** `execute-only` or `shippable`, per
   ADR-0026:59. A tool that only drives a browser while a test is authored falls on the
   execution side of ADR-0026:58, which is the rule this applies rather than a row anyone has
   written. Putting the same tool inside a distribution artifact would be conveying, and
   ADR-0026:58 says "No tool may be bundled without a decision recorded as an ADR".
3. **The licence read at the distributed artifact, with a date.** Not at a badge, and not from
   another document here. ADR-0026:57 is the rule, and the Playwright licence contradiction that
   `../writing-playwright-tests/SKILL.md` records is what happens when it is skipped.
4. **A candidate goes to section 6 first.** `docs/DEPENDENCY-LICENSES.md`:364 states the
   promotion rule: a tool moves into section 2 "in the change that first runs it", when there is
   a pinned version to read a licence against instead of a default branch.

And one route that is closed. ADR-0026:38 says "**Not named above is not permitted, and the
remedy is a different package rather than an exception** ... A package or tool whose licence
appears in none of the three lists is not adopted; the answer is to choose something else."
Adding a licence to a list is an amendment to that ADR, and the section C exception process is
named there as **not** the route.

**So the honest answer to "shall I add the Playwright MCP server" is that it is an ADR plus an
inventory row, not a configuration file.**

## The one idea worth importing, and this repository already writes it twice

Strip the tooling from the published workflow and one instruction survives: do not write the test
from the scenario text, observe the running interface first. That is right, and it is not new
here.

- ADR-0060:60: "A test asserts **observable behavior**, never implementation detail: it exercises
  a public entry point and asserts an observable outcome, and does not assert private internals,
  call counts, or structure."
- `tests/CLAUDE.md`:32: "plant a violation and watch the assertion fail before believing a new
  rule".

The second was learned by a real failure in this repository rather than reasoned from. An
ArchUnitNET rule written in the positive form **passed** over a planted, called `Newtonsoft.Json`
reference (`tests/CLAUDE.md`:18-32). A green suite proved nothing. Import the published idea by
pointing at these two, not by restating it as though it arrived from outside.

## The procedure, once it is unblocked

Not runnable today. Written now so that the first person who can run it does not re-derive the
gates. Steps 0, 2, and 6 are stops rather than choices.

0. **Confirm the surface is the admin console.** ADR-0025:62 scopes Playwright to the admin app.
   The login, consent, and logout pages are a different question, and
   `../writing-playwright-tests/SKILL.md` records why a test there passes and is still wrong.
   Any other surface stops here and is raised as an ADR.
1. **Take the scenario from its owner.** Copy ADR-0060:62. Check it against
   `docs/design/16-admin-app.md`:241-244, which lists what the suite owes beyond that one flow.
   Do not ask for a scenario and do not invent one.
2. **Stop, because no tool is decided for driving the browser.** Adopting one is the previous
   section. This is the step the published workflow assumes is free.
3. **Drive the running admin console and record what you observed.** Selectors, the order of
   interactions, and what the interface actually renders. The record is the input to step 4.
4. **Write the test in C# from the record**, in Given / When / Then form, against the package
   `../writing-playwright-tests/SKILL.md` names.
5. **Watch it fail before believing it passes.** Break the precondition the scenario turns on,
   which here is the second approver, and confirm the assertion fails. Then restore it.
6. **Assert the observable outcome, and stop there.** For this scenario the outcome is "**no
   access token in any browser response**" (`docs/design/16-admin-app.md`:242). A rendered tree
   is structure, and ADR-0060:60 excludes it.

Step 5 is the published workflow's "iterate until the test passes", reversed. A test that has
only ever been green has not been shown to test anything.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is
written into it (`docs/CLAUDE.md`). All were run on 2026-08-07 at `10df955`, case-insensitive
over every tracked file outside `.claude/skills/`.

- **No tool is decided for driving a browser to author a test.** `playwright mcp`, `.mcp.json`,
  `code generation`, `record the browser`, `browser automation`, `test generation`,
  `generate a test`, `scaffold a test`, `trace viewer`, and `inspector` returned **zero** hits
  each. `codegen` returned one hit, `.gitignore`:271, which is `orleans.codegen.cs` in the
  standard ignore template and is not a Playwright tool. `mcp server` returned the five
  authorization-server files listed above. So there is no tool to name and none to quote.
- **No admin console exists to drive.** `src/` holds `Nami.Identity.Abstractions` and nothing
  else. Steps 3 through 6 above have no subject until that changes.
- **Playwright has no row in the licence record.** `playwright` returned **zero** hits in
  `docs/DEPENDENCY-LICENSES.md` on 2026-08-07, while ADR-0026:61 lists it in the
  confirmed-permissive set. `../writing-playwright-tests/SKILL.md` holds the artifact reading:
  the package declares MIT and the same nupkg bundles Apache-2.0. ADR-0026:61, ADR-0025:73, and
  `docs/design/20-testing.md:335` were each corrected to name both on that date. What is still
  missing is the licence-record row, owed under ADR-0026 section C.

The browser matrix, the headed default, the trace and video policy, the version, and the
end-to-end project name are **not** repeated here.
`../writing-playwright-tests/SKILL.md` records all five with its own searches.

A genuinely new decision here is raised as an ADR, never settled inside a test file or inside
this skill (`docs/CLAUDE.md`, the authority order).

## Who owns which question

| Question | Authority |
|---|---|
| What a Playwright test here must look like, and every Playwright-specific trap | [`../writing-playwright-tests/SKILL.md`](../writing-playwright-tests/SKILL.md), then ADR-0025 parameter E |
| Everything general to a test here | [`../writing-tests/SKILL.md`](../writing-tests/SKILL.md), then ADR-0060 and `docs/design/20-testing.md` |
| The scenario itself | ADR-0060:62, checked against `docs/design/16-admin-app.md` section 9 |
| What "observable behavior" excludes | ADR-0060:60 |
| Whether a browser-driving tool may be adopted, and what the adoption owes | ADR-0026 section C, and `docs/DEPENDENCY-LICENSES.md` sections 2 and 6 |
| Whether a tool may be bundled rather than only executed | ADR-0026:58, which requires an ADR |
| What MCP means in this repository | ADR-0064, which is `proposed` |
| Where the end-to-end project lands, and under which name | ADR-0060:69, deferred to M1 |

## Which tool reads a browser-driving claim at its source

Nothing in this repository pins a Playwright version, so every API claim about the MCP server or
about `Microsoft.Playwright` is a claim about someone else's current release. Read it before
writing it. Use `context7`: `resolve-library-id`, then `query-docs`. For the MCP server itself,
read the package or repository that publishes it, and read the licence at the distributed
artifact under ADR-0026:57.

**A tool is a source, never an authority.** The published workflow this skill was scoped from
does not override an accepted ADR. Where the two disagree, stop and flag both with file and
line.
