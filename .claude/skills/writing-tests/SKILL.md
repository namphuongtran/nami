---
name: writing-tests
description: Use when writing, reviewing, or planning a test in the Nami repository, including choosing which of the seven suites a requirement belongs to. Nami pins xunit.v3 on Microsoft Testing Platform, takes no assertion library, names tests Given / When / Then, and never substitutes SQLite for Testcontainers PostgreSQL. Generic xUnit guidance is wrong here on several counts, and this skill names each one with the decision that overrides it.
---

# Writing tests here

Read this before choosing a suite, adding a package, or naming a test method. It exists because
a paths-scoped rule cannot help you yet: [`../../rules/csharp.md`](../../rules/csharp.md) loads
only once a `.cs` file is in play, and the decisions below are needed **before** the file exists.

This skill holds nothing that file already holds. Naming, target framework, analyzer breadth, and
the tool table are there, and it loads beside this skill on any `.cs` file, including
`tests/**/*.cs`. [`../../../tests/CLAUDE.md`](../../../tests/CLAUDE.md) holds the traps learned
inside that folder, and it is **not** re-injected after `/compact`, so re-read it if the session
has been compacted.

ADR-0060 is the authority on the strategy, and
[`../../../docs/design/20-testing.md`](../../../docs/design/20-testing.md) on the taxonomy.
Neither is restated here.

## What exists today, measured

Measured on 2026-08-07. `tests/` holds **one** project, `Nami.Identity.ArchitectureTests`, and
the folder held nothing but a placeholder until 2026-08-02 (`tests/CLAUDE.md:9-10`).

Six of the seven suites in the taxonomy have no code. So most rows below are derived from an
accepted decision rather than learned by getting something wrong, and the difference matters to a
later reader: a decision-derived rule has never been tested by use. The rows sourced to
`tests/CLAUDE.md` are the exception, and each of those was learned by a real failure.

Enforcement stands at one rule of five. ADR-0024:55 lists rules (a) through (e), and ADR-0024:74
records that only the dependency rule is live: "Rules (a) through (e) above are otherwise
untouched and remain unenforced, because the projects they constrain do not exist yet."

## Which suite are you writing

Answer this first. "A unit test with xUnit" is **one row of seven**, and generic xUnit guidance
describes that row only. The table is `docs/design/20-testing.md:52-60`, and ADR-0060:35-43 is
the decision that consolidates it. Each row keeps its own owner.

| Type | What it covers | Tools | Owner |
|---|---|---|---|
| Unit | Domain logic and handlers in isolation, no container | xUnit | ADR-0025 |
| Integration | The real pipeline (multi-tenant filter, row-level security, applied migrations) through `WebApplicationFactory<Program>`; Redis for backplane and replay | Testcontainers PostgreSQL 18 | ADR-0025 |
| End-to-end | The protocol path, and the admin UI | xUnit plus `WebApplicationFactory` plus Testcontainers; Playwright | ADR-0025 |
| Architecture | The dependency rule and slice decoupling | `Nami.Identity.ArchitectureTests`, TngTech.ArchUnitNET | ADR-0024 |
| Contract-regression | Each OpenIddict seam's behavior on the pinned version, on every OpenIddict and .NET bump | xUnit | ADR-0021, ADR-0030 |
| Load and soak | The NFR targets on p95 and p99, the SLO gate, and the canary | Apache JMeter, plus a hand-written xUnit concurrency test where a .NET-side gate is wanted | ADR-0078, ADR-0041 |
| Conformance | OpenID certification profiles | OIDF conformance suite, self-hosted | ADR-0027 parameter F |

Two suites are **merge-blocking gates** rather than one suite among many: the multi-tenant
isolation suite (`docs/design/20-testing.md:157-168`) and the three conformance profiles
(`docs/design/20-testing.md:183-190`, decided at ADR-0027:44).

## Where the generic xUnit answer is wrong here

Each row was read at its source on 2026-08-07. The middle column quotes enough of the decision to
survive a line shift, so a drifted pointer reads as drift rather than as a different claim.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` | "BOTH absent on purpose"; `xunit.v3` ships Microsoft Testing Platform support | `tests/Nami.Identity.ArchitectureTests/Nami.Identity.ArchitectureTests.csproj:20-27`, `tests/CLAUDE.md:56-67` |
| Trusting a green `dotnet test` on a new project | A project that omits `TestingPlatformDotnetTestSupport` is "*skipped* by `dotnet test` rather than failing it" | `tests/CLAUDE.md:66-67` |
| A fluent assertion package | "Nami takes **no** fluent-assertion package"; assert with what `xunit.v3.assert` ships | ADR-0060:45-47, `docs/design/20-testing.md:64-66` |
| `MethodName_Scenario_ExpectedBehavior` | "named and structured as **scenarios**, in Given / When / Then form" | ADR-0060:61, `docs/design/20-testing.md:80-84` |
| Mocking a dependency and asserting the call count | A test "does not assert private internals, call counts, or structure" | ADR-0060:60, `docs/design/20-testing.md:109-111` |
| SQLite, or the EF In-Memory provider | "**SQLite is never substituted** for the database in any test" | ADR-0060:38, ADR-0025:60, `docs/design/20-testing.md:62-64` |
| `[ProjectName].Tests` naming | Project names come from the taxonomy, not from the project under test; the one that exists is `Nami.Identity.ArchitectureTests` | ADR-0024:55, `docs/design/20-testing.md:57` |
| `TngTech.ArchUnitNET.xUnit`, the plainly named twin | "THE xUnit INTEGRATION PACKAGE NAME IS NOT COSMETIC"; the twin declares `xunit.assert 2.4.1`, which is xUnit v2 | `Directory.Packages.props:98-103`, `tests/CLAUDE.md:69-75` |
| `OnlyDependOnTypesThat(...)` in an ArchUnitNET rule | Prefer the negative form; the positive one **passed** over a planted, called violation | `tests/CLAUDE.md:12-34` |
| Believing a new rule because the suite is green | "plant a violation and watch the assertion fail before believing a new rule" | `tests/CLAUDE.md:33-34` |
| Adding a package "only for tests" | Every graph node is a licence read owed under ADR-0026; the measured cost of one convenience pair was 23 restore-graph nodes to 28 | `tests/CLAUDE.md:60-63`, `Directory.Packages.props:62-69` |
| An illustrative company name in a fixture | "`tenant-a` and `tenant-b`, never an illustrative company name" | `docs/design/20-testing.md:101` |
| Real keys, certificates, or personal data in a fixture | Generated for the test, "so a test tree can never become a credential leak" | `docs/design/20-testing.md:102` |
| A security test with no requirement label | "A security test names the ASVS requirement it verifies" | `docs/design/20-testing.md:87-91`, ADR-0062:43 |
| Numbering an ASVS chapter `V2` or `V3` | That is the ASVS **4.x** scheme; the 4.x numbers are mapped to 5.0 when the test is written | `docs/design/20-testing.md:173`, ADR-0062 |
| Reading a `.csproj` to assert a dependency rule | "Never assert on the contents of a `.csproj`"; assert against the compiled artifact | `tests/CLAUDE.md:48-54` |
| Writing the implementation first | Protocol and security code is written test-first, with the failing behavior test before the implementation | ADR-0060:54-56, `docs/design/20-testing.md:110-115` |

Do not re-derive the underscore-field rule, the target-framework knob, the language version, or
the analyzer breadth. `.claude/rules/csharp.md` holds all four, and it loads beside this skill.

## Two edges that read as coverage and are not

Both are recorded already. Both are the kind of completeness a generic answer assumes.

1. **The architecture suite is two facts for one rule, and neither covers the rule alone.** One
   reads the type graph, the other the assembly reference table. A package **referenced but not
   used by any type** passes both, because an unused reference is elided from metadata. Closing
   that needs a check on the packed surface, which needs a pack and does not exist yet. Do not
   "simplify" the two into one. `tests/CLAUDE.md:36-46`, ADR-0024:76.
2. **The startup secure-invariant enumeration in the design is a restatement, and it has drifted
   twice.** ADR-0043's table is the one to diff against, not the prose copy.
   `docs/design/20-testing.md:117-155` says so about itself.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is
written into it (`docs/CLAUDE.md`). All were run on 2026-08-07 with `git grep -ni` over every
tracked file, excluding `.claude/rules/`, which already records the same finding.

- **No test-double library, and the vocabulary is a fake behind a port rather than a package.**
  `Moq`, `NSubstitute`, `FakeItEasy`, `AutoFixture`, `Bogus`, and `spy` returned **zero** hits
  each. `mock` returned one hit, `.gitignore:364`, a comment in the standard ignore template
  naming a commercial vendor's mocking-tool configuration file. `test double` returned one hit,
  `docs/design/03-audit.md:353`, which is an obligation on adapters rather than a library. Every
  `stub` and `fake` hit is a design sense, not a test-library sense. What **is** decided bounds
  the question without answering it: ADR-0058:52 says "Any adapter must be fully substitutable
  behind its port, which is what makes the cloud-agnostic swaps and in-process test fakes safe",
  and ADR-0024:40 treats "no need for an in-process fake" as a reason a port is **not** warranted.
  So the recorded seam is a hand-written in-process fake behind a port. Whether a mocking package
  is ever taken is undecided, and taking one is an ADR, never a convenience taken in a test file.
- **No coverage number.** `docs/design/20-testing.md:302` and `:315` both say coverage meets "the
  agreed line" on the security-relevant paths and neither names a figure. Searched the same day:
  `coverlet`, `code coverage`, `line coverage`, `branch coverage`, and `coverage threshold`
  returned **zero** hits each across tracked markdown, workflow, and build files. So there is no
  threshold to enforce and none to quote.
- **Which target-framework knob a test project takes.** The one project reads
  `$(NamiApplicationTargetFramework)`, and its own `.csproj:9-19` records that as a choice with no
  source rather than a rule being followed. ADR-0030 parameter B splits the knobs into library and
  host, and a test project is neither.

A genuinely new decision here is raised as an ADR, never settled inside a test file or inside this
skill (`docs/CLAUDE.md`, the authority order).

## Who owns which question

| Question | Authority |
|---|---|
| The taxonomy, behavior-first naming, and test-first for protocol and security code | ADR-0060, and `docs/design/20-testing.md` |
| Containers, `WebApplicationFactory`, Playwright, and the CI composition | ADR-0025 |
| The dependency rule, the five architecture rules, and what is enforced today | ADR-0024:55 and ADR-0024:74 |
| The multi-tenant isolation gate | `docs/design/20-testing.md` section 5.3, ADR-0049 |
| The security-test catalogue, and the ASVS labelling rule | `docs/design/20-testing.md` section 5.4, ADR-0062 |
| The conformance profiles, and certification against conformance | ADR-0027 parameter F |
| Load, soak, and the SLO gate | ADR-0078, ADR-0041 |
| Which spike proved what, so a test is not read as covering more than it does | `docs/design/20-testing.md` section 5.6 |
| Where each test type runs | `docs/design/20-testing.md` section 5.8 |
| Whether a package may be added at all | ADR-0026, and `docs/DEPENDENCY-LICENSES.md` |
| Traps learned inside the folder | `tests/CLAUDE.md`, not re-injected after `/compact` |
| Casing, target framework, analyzer breadth | `.claude/rules/csharp.md`, and `.editorconfig` |

## Which tool reads an xUnit v3 claim at its source

**This repository pins `xunit.v3` 3.2.2** (`Directory.Packages.props:115`). Most published xUnit
guidance, including the file this skill was scoped from, is written against xUnit v2, and the two
differ in the API surface a test touches directly.

So read the pinned version before writing anything about `ITestOutputHelper`, dynamic skipping,
`TheoryData`, or a custom `DataAttribute`. Use `context7`: `resolve-library-id`, then
`query-docs`. **None of those four was verified when this skill was written on 2026-08-07**, and
nothing here asserts them. A tool is a source, never an authority: where a tool and an accepted
ADR disagree, stop and flag both with file and line.
