---
name: writing-playwright-tests
description: Use when writing, reviewing, or planning a Playwright browser test in the Nami repository, or when adopting published Playwright .NET guidance. Nami pins xunit.v3, so the plainly named Microsoft.Playwright.Xunit package is the wrong one and its lifecycle signature does not compile here. Playwright is scoped to the admin console only, no browser tool is decided for the login surface, and three documents disagree about the licence. This skill names each trap with the decision or the artifact read that overrides it.
---

# Writing Playwright tests here

Read this before adding a Playwright package, choosing a base class, or porting a published
Playwright .NET example. It exists for the reason
[`../writing-tests/SKILL.md`](../writing-tests/SKILL.md) gives for itself: a paths-scoped rule
cannot help you yet, because no Playwright file exists to trigger one.

**This skill holds only the Playwright delta.** Everything general to a test here is already in
[`../writing-tests/SKILL.md`](../writing-tests/SKILL.md), and that skill is the one to read
first. It owns the seven-suite table, Given / When / Then naming, the no-assertion-library rule,
the `TestingPlatformDotnetTestSupport` skip trap, the absent `Microsoft.NET.Test.Sdk`, the
licence cost of every added package, and the `TngTech.ArchUnitNET` twin. None of those is
restated below. [`../../rules/csharp.md`](../../rules/csharp.md) loads on any `.cs` file and owns
casing, the target-framework knob, and the analyzer breadth.
[`../../../tests/CLAUDE.md`](../../../tests/CLAUDE.md) holds the traps learned inside `tests/`,
and it is **not** re-injected after `/compact`.

ADR-0025 parameter E is the authority on Playwright, and ADR-0060 on the strategy that cites it.
Neither is restated here.

## What exists today, measured

Measured on 2026-08-07 at `10df955`. **No Playwright test, package pin, or CI step exists.**

- `Directory.Packages.props:112-116` pins three packages, and none of them is Playwright.
- `Nami.Identity.slnx` lists one test project, `Nami.Identity.ArchitectureTests`.
- `.github/workflows/ci.yml:221` is the only test invocation in the workflow, and it installs no
  browser. Playwright downloads its browsers through a separate step that is not written yet.

So every row below is derived from an accepted decision or read at a distributed artifact, and
none was learned by getting something wrong here. A decision-derived rule has never been tested
by use, and that difference matters to a later reader.

The package readings are dated the same day, taken from each `.nuspec` and from the assembly
metadata of each package at **1.61.0**, which was the latest version on that date. Re-read them
before relying on a version number, because a later release may move any of it.

## Where the generic Playwright answer is wrong here

Each row was read at its source on 2026-08-07. The middle column quotes enough of the decision to
survive a line shift, so a drifted pointer reads as drift rather than as a different claim. Design
`11` is cited by **section**, because `docs/design/CLAUDE.md` records that these documents grow in
the middle.

| A generic answer reaches for | Nami decided, or the artifact says | Read at |
|---|---|---|
| `Microsoft.Playwright.Xunit`, the plainly named package | It declares `xunit.extensibility.core 2.8.0`, which is xUnit **v2**. The twin Nami needs is `Microsoft.Playwright.Xunit.v3`, which declares `xunit.v3.extensibility.core 1.0.1` | both `.nuspec` at 1.61.0; ADR-0060:37-39 |
| `using Microsoft.Playwright.Xunit;` | The v3 package exports the namespace `Microsoft.Playwright.Xunit.v3`. The package id and the namespace both change, not just the id | assembly metadata at 1.61.0 |
| `public override async Task InitializeAsync()` | The v3 base classes declare `ValueTask InitializeAsync()`. The `Task` form is the v2 signature and it does not compile against the v3 package | assembly metadata at 1.61.0 |
| `await Test.StepAsync(...)`, to group interactions | No such API. `StepAsync` returned **zero** occurrences in all four assemblies at 1.61.0 | measured 2026-08-07 |
| `using static Microsoft.Playwright.Assertions;` beside a `PageTest` base class | Redundant there. `Expect` is an instance method on `PlaywrightTest`, which `PageTest` inherits through `ContextTest` and `BrowserTest` | assembly metadata at 1.61.0 |
| Playwright for the login, consent, or logout pages | Scoped to "UI end-to-end tests for the **admin app**". No document names a browser tool for the end-user surface | ADR-0025:62, design `16`:241, design `20`:259 |
| A large inline `ToMatchAriaSnapshotAsync` tree as the main assertion | A test "asserts **observable behavior**, never implementation detail ... does not assert private internals, call counts, or structure" | ADR-0060:60 |
| Taking Playwright as Apache-2.0, or as MIT, on one document's word | Both are present. All four 1.61.0 `.nuspec` files declare `<license type="expression">MIT</license>`, and the `Microsoft.Playwright` nupkg bundles `playwright-core` at Apache-2.0. One licence is not the answer. See the flag below | ADR-0025:73, ADR-0026:61, design `20`:335, and the artifact |
| Assuming a browser is available in CI | One test step, no install step, no browser cache | `.github/workflows/ci.yml:221` |
| A test that needs JavaScript to reach the login form | "The login and consent flow completes with client scripting unavailable", except the passkey path | ADR-0072:73 |
| Asserting a security header from the browser test | The header assertions are their own obligation per profile. The browser test exists for the case a header assertion **cannot** see | ADR-0091:420-425, design `11` section 9 |
| A fresh scenario invented for the admin suite | The scenario is already written, and it is the same one in three places | ADR-0025:62, ADR-0060:62, design `16`:241-244 |

Do not re-derive the assertion-library rule, the naming form, the runner packages, or the cost of
adding a package. [`../writing-tests/SKILL.md`](../writing-tests/SKILL.md) holds all four.

## Two of those rows need more than a row

**1. The package twin is the same trap this repository already took, with the naming reversed.**
`tests/CLAUDE.md:73-79` records it for ArchUnitNET under the heading "The xUnit integration
package has a v2 twin with an almost identical name", and notes that ADR-0024, design `01`, and
design `20` "all write the base name `TngTech.ArchUnitNET`, and none of them picks between the
variants". Playwright is in exactly that state: ADR-0025:62, ADR-0060:39, ADR-0061:66, and design
`20`:259 all write "Playwright" with no package identifier. The difference is which twin looks
right. For ArchUnitNET the correct package is the odd-looking `xUnitV3` suffix, so the plain name
is visibly a choice. For Playwright the **v2** package holds the plain name, and published
guidance names it directly, so the wrong one is the one that reads as correct. When the pin is
added, put both `.nuspec` readings beside it in `Directory.Packages.props`, the way the
ArchUnitNET pair is recorded at `Directory.Packages.props:98-105`.

This trap fails at compile time, which is the good case. The next one does not.

**2. A Playwright test on the login surface passes, and it is still wrong.** ADR-0025:62 scopes
Playwright to the admin app. Design `11` section 9 nevertheless demands a browser test on the
end-user surface, and it says why a server-side assertion cannot stand in: for
`response_mode=form_post` the flow "must complete under the enforced Protocol HTML profile, and
the negative case must show that the same response under the UI profile produces **no
navigation**, since that is the blank-page failure ADR-0091 exists to prevent and it is invisible
to a server-side assertion". ADR-0091:422-425 states the same obligation. **Neither names a
tool.** So writing that test in Playwright is a reasonable guess and it is not a decision this
repository has taken. The test will pass and read as coverage of an authorised tier. Raise it as
an ADR rather than settling it in a test file (`docs/CLAUDE.md`, the authority order).

## A licence record to correct, and no document holds the whole answer

Three documents disagree, and **none of the three is complete.** The package declares one
licence and it distributes a second one inside itself.

| Source | Says |
|---|---|
| ADR-0025:73 | "Playwright (Apache-2.0) covers .NET UI end-to-end testing" |
| ADR-0026:61 | "Playwright (Apache-2.0)", in the confirmed-permissive list |
| design `20`:335 | "**MIT** (read at 1.59.0, from the `playwright-dotnet` repository; the JavaScript `playwright` project is Apache-2.0, and the two are not the same package)" |

Read at the artifact on 2026-08-07, at 1.61.0, from the nuget.org flat container.

- `Microsoft.Playwright`, `Microsoft.Playwright.Xunit`, `Microsoft.Playwright.Xunit.v3`, and
  `Microsoft.Playwright.TestAdapter` each declare `<license type="expression">MIT</license>` in
  their own `.nuspec`. All four were read on that date.
- The `Microsoft.Playwright` nupkg holds **112 entries** under `.playwright/`. One of them,
  `.playwright/package/package.json`, declares `"name": "playwright-core"` with
  `"license": "Apache-2.0"`. Beside it, `.playwright/node/LICENSE` carries the Node.js licence
  and covers six platform binaries. The package also ships `NOTICE` and
  `ThirdPartyNotices.txt`, neither of which was read on that date.
- The three companion packages bundle **none** of it. `.playwright/` returned 0 entries in each
  of the other three nupkgs on the same date.

So both licences apply to what Nami would take, and each document is right about a different
thing. The two ADRs name the licence of the bundled JavaScript project, which really is
Apache-2.0 and really is inside the nupkg. Design `20` names the licence the package declares
for itself. Design `20` is wrong on a second point, though: it says the JavaScript project and
the .NET package "are not the same package", and the file listing above shows the .NET package
**contains** it.

ADR-0026:57 requires exactly this reading, as item 3 of its verification method: "A licence is
verified by reading the licence text of the thing actually distributed, including companion
modules. A repository's root `LICENSE` can be permissive while a module shipped beside it is
not, and repository-metadata APIs read only the root file." MIT and Apache-2.0 are both inside
ADR-0026:35, so neither needs an exception. Whether the Node.js bundle licence classifies
inside section A was **not** determined on 2026-08-07: only the first four lines of
`.playwright/node/LICENSE` were read, and that file aggregates third-party terms.

**This package defeats the heuristic the repository drew last time.**
`docs/DEPENDENCY-LICENSES.md` section 3.2 settled MinVer's licence against the same shape of
three-way disagreement, and ended with a rule: "a build-time or tool package with no
`<dependencies>` is a prompt to unpack it, not evidence that it is a leaf."
`Microsoft.Playwright` at 1.61.0 **does** declare `<dependencies>`, with three entries under
`.NETStandard2.0`. It reads as fully declared, and it still bundles an entire npm package and a
Node runtime that none of those three entries names. So an empty dependency element cannot be
the prompt to unpack here.

Three consequences. Do not quote either ADR for this licence, and do not quote design `20` as
settling it either. Record both licences when the pin is added, the way section 3.2 records
MinVer's bundle. And correcting the three documents spans three layers and has not been done,
so it is owed rather than done.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is
written into it (`docs/CLAUDE.md`). All were run on 2026-08-07 at `10df955`, case-insensitive over
tracked markdown, C#, project, build, workflow, and JSON files.

- **No browser matrix, no headed or headless default, and no trace, video, or screenshot
  policy.** `headless`, `chromium`, `webkit`, `browser matrix`, `playwright install`, `trace on`,
  `screenshot`, `video`, `BaseURL`, and `base url` returned **zero** hits each. `firefox` returned
  one hit, ADR-0019:15, about front-channel logout and third-party cookies, which is not a test
  setting. So there is no browser set to target and none to quote.
- **No Playwright version anywhere.** ADR-0025, ADR-0026, ADR-0061, and design `20` all name the
  tool without a version, and `Directory.Packages.props` has no row. Every version in this file is
  a reading of 1.61.0 on one date, never a pin this repository has taken.
- **Which tool drives the end-user browser test**, per the second escalated item above.
- **The E2E project name and layout.** ADR-0060:69 defers it: "when the test projects land (from
  M1), confirm this taxonomy against the real suites and adjust the naming/structure guidance to
  what the code shows". This is why the skill is a skill. A `paths:` glob would have to guess a
  project name that no document has chosen.

The test-double question is **not** repeated here. `../writing-tests/SKILL.md` records it with its
searches, and `.claude/rules/razor.md:119-123` records the same finding independently.

## Who owns which question

| Question | Authority |
|---|---|
| Everything general to a test here: the suite table, naming, assertions, runner packages | [`../writing-tests/SKILL.md`](../writing-tests/SKILL.md), then ADR-0060 and design `20` |
| Playwright's scope, and the admin end-to-end scenario | ADR-0025 parameter E |
| What the admin suite must cover, item by item | `docs/design/16-admin-app.md` section 9 |
| What the login and consent surface must cover, and why one case needs a browser | `docs/design/11-login-consent-ui.md` section 9, by section |
| The three response profiles, and the `form_post` blank-page failure | ADR-0091, and seam S36 in design `22` |
| Whether the flow must work without client script | ADR-0072:73 |
| Whether a Playwright package may be added at all, and under which licence | ADR-0026, and `docs/DEPENDENCY-LICENSES.md` |
| Where the version is pinned, and in which form | `Directory.Packages.props`, where a bare version "is a floor, not a pin" |
| Traps learned inside `tests/` | `tests/CLAUDE.md`, not re-injected after `/compact` |
| Casing, target framework, analyzer breadth | [`../../rules/csharp.md`](../../rules/csharp.md), and `.editorconfig` |

**A tool is a source, never an authority.** Published Playwright guidance, including the document
this skill was scoped from, does not override an accepted ADR. Where the two disagree, stop and
flag both with file and line.
