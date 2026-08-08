---
paths:
  - "**/*.cs"
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Writing C# here

This file loads when a `.cs` file is in play. It holds no coding standard, because two other
things already do. ADR-0065 adopts the Microsoft naming and C# coding conventions **by
reference** (`0065:37`), and `.editorconfig` is the machine-checked rules-of-record that wins
over any prose, including ADR-0065's own (`0065:92`).

What this file holds is the gap between those two and a model's default answer: the places where
the ordinary .NET answer is **wrong for Nami**, and the decision that overrides it. Read it as a
list of traps, not as a style guide.

## Where the generic .NET answer is wrong here

Each row was read at its source on 2026-08-05. The middle column quotes enough of the decision
to survive a line shift, so a drifted pointer reads as drift rather than as a different claim.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| `camelCase` private fields, no prefix | `_camelCase`, with `s_camelCase` static and `PascalCase` const carve-outs | `.editorconfig:96-102`, ADR-0065:87 |
| SQLite or the EF In-Memory provider in tests | "SQLite is never substituted"; Testcontainers PostgreSQL 18 | ADR-0060:38, ADR-0025:60 |
| Any engine but PostgreSQL | "PostgreSQL as the single supported engine" | ADR-0037:35 |
| Serilog | `Microsoft.Extensions.Logging` with source-generated `LoggerMessage` | ADR-0022:39 |
| Application Insights, or any named backend | "emits OTLP and mandates no production backend" | ADR-0063:38 |
| An unprefixed custom instrument name | "Every custom instrument is namespaced `nami.identity.`" | ADR-0085:110 |
| `<LangVersion>latest</LangVersion>` | "LangVersion is deliberately ABSENT"; the default derives from the target framework | `Directory.Build.props:79-88` |
| A literal `<TargetFramework>net10.0</TargetFramework>` | `$(NamiLibraryTargetFrameworks)`, or `$(NamiApplicationTargetFramework)` for an application | `src/CLAUDE.md:210`, ADR-0030:39 |
| `Services/`, `DTOs/`, `Validators/` | "Organize the Application layer by feature slice", `Features/<Area>/<UseCase>/` | ADR-0024:44, ADR-0065:77 |
| A repository or unit-of-work wrapper | "introduced to solve a demonstrated problem, never preemptively" | ADR-0066:42 |
| An interface with one implementation, for layering | "a port must have at least two real reasons to exist" | ADR-0024:39 |
| A fluent assertion package | "Nami takes no fluent-assertion package"; use what `xunit.v3.assert` ships | ADR-0060:47 |
| `Microsoft.NET.Test.Sdk` | Absent on purpose; dropping `TestingPlatformDotnetTestSupport` **skips** tests silently | `tests/CLAUDE.md:56-67` |
| `OnlyDependOnTypesThat(...)` in an ArchUnitNET rule | Prefer the negative form; the positive one passed over a real violation | `tests/CLAUDE.md:12-34` |
| `/healthz`, one probe route, or a detail body | "Two endpoints, never one": `/health/live` and `/health/ready`, status code only | ADR-0080:76-93 |
| RFC 7807 | "Errors are RFC 9457 problem details with a machine-readable code" | ADR-0079:162 |
| Swashbuckle or NSwag | "the built-in .NET 10 OpenAPI", plus a committed snapshot diffed in CI | ADR-0020:42, ADR-0087:49-51 |
| Bearer-only token validation | `ValidTypes = ["at+jwt"]` and a minimal claim set; reject a DPoP-bound token sent as Bearer | ADR-0005:36, ADR-0014:40 |
| Hand-rolled refresh-token rotation | "never call `DisableRollingRefreshTokens()`"; keep OpenIddict's native mechanics | ADR-0004:33 |
| A cloud PaaS deployment target | "not locked to any cloud"; container image, Helm, and OpenTofu | ADR-0023:37, ADR-0006:31 |
| `UseSnakeCaseNamingConvention()` | Considered and rejected; tables and columns are PascalCase | ADR-0065:85 |

**The first row is the only one where the generic answer is wrong about its own source, so it
needs a separate warning.** Third-party C# guidance often writes "use camelCase for private
fields" and stops there. Microsoft's own page does not: it says "Use camel casing
("camelCasing") when naming `private` or `internal` non-constant fields, and prefix them with
`_`", and separately "Private and internal non-constant instance fields start with an underscore
(`_`)". Its example is `private IWorkerQueue _workerQueue;`, and the same page pairs that with
`s_` for private statics. Read it at
<https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names#naming-conventions>,
never from a document that summarizes it.

So `.editorconfig:96-102` **is** the Microsoft convention, not a house deviation from it. A
suggestion to drop the underscore is a misquote of the baseline ADR-0065:37 adopts, and it was
raised and rejected on that evidence on 2026-08-05. Treat the same suggestion the same way next
time, and say which page you read.

### What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so the searches are
written into it (`docs/CLAUDE.md:51-96`).

- **No validation library is chosen.** Searched 2026-08-05, case-insensitive over the whole
  repository: `fluentvalidation` returned nothing at all; `validation library` returned two hits,
  both the resource-server token-validation sense (`design/22:165`, `design/04:47`); `dataannotation`
  and `data annotation` returned four hits, all of them **options** validation
  (`.ValidateDataAnnotations()`), not model validation. ADR-0061:40's framework-native-first rule
  biases against a third-party library here, but it does not decide it.
- **XML doc comments and `CS1591` are owned by no ADR.** `design/21-cicd-and-deployment.md:232`
  states the requirement, and the design layer realizes decisions rather than making them.
  This is an absence claim, so it carries its own search rather than pointing at one. Seven
  spellings over `docs/adr/`, case-insensitively, each returning **zero** files on 2026-08-07:
  `DocFX`, `docfx`, `CS1591`, `1591`, `xml doc`, `GenerateDocumentationFile`, and
  `documentation file`.
- **Local-variable and parameter naming are unruled.** `.editorconfig` declares naming symbols
  for `field` three times and `method` once, and none for `local` or `parameter` (grepped
  `applicable_kinds` on 2026-08-05). The C# default happens to be `camelCase`, so nothing is
  broken, but no rule enforces it and none is implied.

## Two enforcement edges that read as total and are not

Both are recorded already. Both are the kind of coverage a generic answer assumes.

1. **The `Async` suffix rule matches the `async` modifier, not the return type.** So `public
   async Task Poll()` is caught and `public Task Poll()` is not. A naming symbol has no
   return-type filter, so the uncovered half is a review matter. ADR-0065:86.
2. **`AnalysisMode` is `Recommended`, and `CA1819` is not in it.** Public array-returning members
   produce no warning today. Do not read a green build as approval of one. `src/CLAUDE.md:238`,
   ADR-0094.

## Who owns which question

| Question | Authority |
|---|---|
| Casing, layout, analyzable naming | `.editorconfig`, which wins over ADR prose (ADR-0065:92) |
| Naming a linter cannot check | ADR-0065:76-88, and note the three distinct name forms at `0065:82` |
| Target framework and language version | ADR-0030; the knob is `Directory.Build.props` |
| Analyzer breadth | ADR-0094: `Recommended`, not `All` |
| Warnings as errors | ADR-0093, with the restore-time `NU190x` carve-out |
| Public API surface | ADR-0044; a public type is two files (`src/CLAUDE.md:147-177`) |
| Dependency rule, ports, and slices | ADR-0024, enforced by the ArchUnitNET suite |
| Whether a pattern is warranted at all | ADR-0066 |
| Test taxonomy and naming | ADR-0060, and `docs/design/20-testing.md` |
| Traps in `src/` and `tests/` | `src/CLAUDE.md`, `tests/CLAUDE.md`, neither re-injected after `/compact` |
| Traps in build and CI files | `.claude/rules/build-and-ci.md` |
| What each gate checks, and why | `scripts/README.md` |

## Which tool reads a claim at its source

**A tool is a source, never an authority.** A Microsoft Learn page does not override an accepted
ADR. Where an external source and an ADR disagree, stop and flag both with file and line, and do
not fill the gap from judgement (`CLAUDE.md:26`).

| To read at source | Use | Why |
|---|---|---|
| A Microsoft or .NET claim | `microsoft-docs`: `microsoft_docs_search`, then `microsoft_docs_fetch` for depth | The evidence rule forbids inferring a Microsoft default from a document that is merely near it. The underscore row above is what happens when someone does. |
| A library or SDK claim (OpenIddict, Npgsql, EF Core, xunit.v3) | `context7`: `resolve-library-id`, then `query-docs` | Training data can predate the pinned version |
| Why a gate passed or failed | The `dotnet-msbuild` binlog tools, via `binlog-generation` then `binlog-failure-analysis` | Nine gates exist and four are self-tests, so a green build is not a green gate |
| A package licence | The distributed artifact itself | Never a badge, and never another document in this repository (`CLAUDE.md:131-133`) |

The heavier .NET skills earn their place as `src/` fills, and it holds **nine** public types
(five sealed classes, two interfaces, and two enums, counted 2026-08-08; the count was six on
2026-08-05). Reach for
`dotnet-data:optimizing-ef-core-queries` when the persistence adapter lands,
`dotnet-test:test-gap-analysis` and its siblings once there is a suite worth auditing, and
`dotnet-diag` once there is a measured hot path. Invoking one before its subject exists produces
advice about nothing.
