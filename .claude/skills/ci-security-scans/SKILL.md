---
name: ci-security-scans
description: Use when adding, debugging, or choosing a CI security scan in the Nami repository, and whenever code scanning, SAST, dependency scanning, secret scanning, container scanning, or DAST comes up. ADR-0092 pinned one tool to each of five stages on 2026-08-02, took no third-party SAST engine at all, and rejected the CodeQL CLI by name. Only stage 1 is live, and it is an MSBuild property rather than a workflow job. The generic answer to "set up code scanning" is a decision this repository already made and declined.
---

# CI security scanning here

Read this before writing a scan job, before naming a scanner, and before answering a question
about what this project scans. It exists because the generic answer is not merely different from
Nami's, it is an option ADR-0092 considered and rejected with a stated reason.

This skill holds nothing that a loaded file already holds.
[`../../rules/build-and-ci.md`](../../rules/build-and-ci.md) holds the measured traps in the
files that decide whether a gate bites, and it loads on any workflow, props, or `.editorconfig`
file. [`../adding-a-ci-gate/SKILL.md`](../adding-a-ci-gate/SKILL.md) holds how to prove a new gate
is not inert. ADR-0092 is the authority on the tools, and
[`../../../docs/design/21-cicd-and-deployment.md`](../../../docs/design/21-cicd-and-deployment.md)
on where they sit in the pipeline. Neither is restated here.

## What exists today, measured

Measured on 2026-08-07 at `10df955`. `.github/workflows/` holds **one** file, `ci.yml`, 214
lines, with seven jobs: Docs lint, ADR/docs guardrail, C# style ruleset, Public-API gate
self-test, Warnings-as-errors gate self-test, Solution build, and Tests.

**One of the five scan stages runs today, and it is not a job.** Stage 1 is a set of MSBuild
properties in `Directory.Build.props`, so it runs inside Solution build. The other four are owed
at M1. `ci.yml:212-213` carries the note that the ADR-0026 licence-scan gate arrives then too.

Zero `${{ }}` expressions exist anywhere under `.github/`, counted the same day, so section 6
below is a regression guard rather than a finder.

## The five stages, and the sixth clause that is not a stage

ADR-0092:67 records "Chosen option: **A**. The five stages below are binding."

| # | Stage | Tool | Licence | Status | Owner |
|---|---|---|---|---|---|
| 1 | SAST | The .NET SDK's own `AnalysisLevelSecurity` axis, and no third-party engine | MIT, inside the SDK | **live**, as an MSBuild property | ADR-0092 section 1 |
| 2 | Dependency vulnerabilities | Trivy, blocking | Apache-2.0 | owed, M1 | ADR-0092 section 2 |
| 3 | Secrets | gitleaks | MIT | owed, M1 | ADR-0092 section 3 |
| 4 | Container image | Trivy, the same tool deliberately | Apache-2.0 | owed, M1 | ADR-0092 section 4 |
| 5 | DAST | OWASP ZAP against staging, classified `execute-only` | Apache-2.0 at the root; the bundle needs ADR-0026 section C | owed, M1 | ADR-0092 section 5 |

Section 6 is **not** a sixth stage and says so in its own title: "it adds no tool and no stage: it
is two checks inside `scripts/check-adrs.sh`". The two rules are no `${{ ... }}` inside any `run:`
script, and no `pull_request_target:` or `workflow_run:` trigger. Both are live today as guardrail
Check 8.

**Stage 1 is a property, not a job, and the spelling is the whole finding.** The value is
`latest-all`. The bare `all` is inert: it produced exit 0 with `CA5351` never firing, even
alongside `TreatWarningsAsErrors=true`. Do not re-derive the mechanism here.
[`../../rules/build-and-ci.md`](../../rules/build-and-ci.md), under "`AnalysisLevelSecurity` takes
a compound value", holds the measurement, and `Directory.Build.props` holds the property-by-property
trace at the line numbers it was read at.

## Where the generic answer is wrong here

Each row was read at its source on 2026-08-07. The middle column quotes enough of the decision to
survive a line shift, so a drifted pointer reads as drift rather than as a different claim.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| A `codeql.yml` workflow, or GitHub default setup | Rejected. The grant covers CI use only for an Open Source Codebase "hosted and maintained on GitHub.com", so adopting it "would bind the security gate to this project remaining public on one host" | ADR-0092 section 1, and `docs/DEPENDENCY-LICENSES.md:393` "Not taken" |
| Semgrep, as the permissive-looking alternative | LGPL-2.1 "identically at the root `LICENSE` and at `cli/LICENSE`"; available only through the ADR-0026 section A exception process, and kept as the **named reversal candidate** | ADR-0092 section 1, `docs/DEPENDENCY-LICENSES.md:391` |
| Any third-party SAST engine | "SAST runs no third-party engine"; the SDK's own rule set carries the stage, "so the stage costs no dependency and no licence" | `docs/design/21-cicd-and-deployment.md:106-111`, ADR-0092 section 1 |
| `AnalysisLevelSecurity` set to `all` | `latest-all`. The bare mode word parses as the level, the mode falls back to `Default`, and the SDK looks for a globalconfig that was never shipped behind an `Exists()` guard | `.claude/rules/build-and-ci.md`, ADR-0092 Confirmation |
| `security-events: write`, and a SARIF upload | Neither exists. The workflow declares `permissions: contents: read` | `.github/workflows/ci.yml:9-10`; `sarif` returned zero hits over every tracked file on 2026-08-07 |
| Grype for containers, OWASP Dependency-Check for dependencies | Trivy for both. "One tool covering both stages means one licence to re-verify at adopt time and one inventory row instead of two." Both alternatives stay recorded as verified, not chosen | ADR-0092 sections 2 and 4, `docs/DEPENDENCY-LICENSES.md:387-388` |
| Treating the licence-scan gate as the dependency scan | Two different gates. Stage 2 "is distinct from the ADR-0026 section C licence-scan gate, which reads licences rather than vulnerabilities" | ADR-0092 section 2 |
| Dependabot as the dependency vulnerability scan | Dependabot is scaffolding, and it watches `github-actions` only until the first `.csproj` lands. The blocking scan is Trivy | ADR-0092 section 2, `.github/dependabot.yml` |
| Adding a scanner because it is useful | Every executable used in the pipeline is an inventory row under ADR-0026 section C's second limb. Nine were named and two were inventoried on 2026-08-02 | ADR-0092 Context, and `docs/DEPENDENCY-LICENSES.md` sections 2 and 6 |
| `${{ }}` inside a `run:` step of a new scan job | Guardrail Check 8 fails the build. Pass the value through `env:` and read it as a shell variable, which is the mitigation the rule exists to enforce | ADR-0092 section 6, `scripts/README.md:14` |
| A floating action tag such as `@v0` | Full version tag only, `@vX.Y.Z`. Check 8c fails the build on any other form, including a commit SHA | `.claude/rules/build-and-ci.md`, ADR-0086 |
| Reading a licence from a badge or from this repository | Read it at the distributed artifact. Every row above was opened rather than inferred, and the read location and date are recorded | ADR-0092 More Information, `CLAUDE.md` evidence rule |

## Two things that read as coverage and are not

Both are stated by ADR-0092 itself rather than inferred, and both are the kind of completeness a
generic answer assumes.

1. **The five stages leave a C#-shaped hole, named rather than papered over.** "The SDK analyzers
   see C#. They do not see Razor markup, SQL held outside C#, Dockerfiles, or GitHub Actions
   workflow definitions." Three of those four are reached by another stage. The fourth is section
   6, and section 6 covers two constructs rather than the surface.
2. **Section 6's green is a statement about two constructs.** What it does not see is listed in the
   ADR: interpolation into an action's `with:` inputs, the scope of a `permissions:` block,
   composite actions and reusable workflows defined in other repositories, and anything at all
   about what a pinned action does. A green there read as "the workflows are safe" would be worse
   than no check.

A third edge belongs to the SAST stage rather than to the list. `AnalysisLevelSecurity=latest-all`
reports `0 Warning(s)` against this solution, measured 2026-08-03. That is a fact about a solution
whose only hand-written C# is two files. It is not a claim that the 94 rules are liveable.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is written
into it (`docs/CLAUDE.md`). All were run on 2026-08-07 with `git grep -in` over every tracked file.

- **How the four owed stages are invoked.** `trivy-action`, `gitleaks-action`, `zap-action`,
  `trivy.yaml`, `trivy config`, `.gitleaks.toml`, and `zap-baseline` returned **zero** hits each.
  `aquasecurity` returned two, both the `aquasecurity/trivy` licence read location in
  `DEPENDENCY-LICENSES.md` and ADR-0092. So ADR-0092 names the tool for each stage and nothing
  names the action, the image, or the configuration file. Choosing one is an ADR-0026 inventory
  row and a licence read, never a line added to `ci.yml`.
- **No severity threshold exists for any scan.** `fail-on`, `scan threshold`, and `severity
  threshold` returned zero, one, and one hit. Neither hit is a scan: `docs/adr/0093-warnings-as-errors.md:245`
  is about analyzer properties. ADR-0092 section 2 says the dependency scan is blocking and does
  not say at what severity. So there is no threshold to enforce and none to quote.
- **No result-consumption story.** `sarif` and `security-events` returned zero and one hit, and the
  one is a frontmatter tag in `docs/design/03-audit.md:4` in the audit-events sense. So nothing
  decides where a finding is published, deduplicated, or dismissed.

A genuinely new decision here is raised as an ADR, never settled inside a workflow file or inside
this skill (`docs/CLAUDE.md`, the authority order).

## Reversing a stage, which is a recorded procedure rather than a judgement

ADR-0092 splits this in two, and the split is load-bearing.

- **For the five stages**, the reversal candidate is already named and its licence already read, so
  a change is "a stack-row edit and a recorded exception rather than a research task". Semgrep for
  SAST, Grype for containers, OWASP Dependency-Check for dependencies.
- **For section 6 there is deliberately no candidate**, and the ADR explains the omission: two
  tools exist that a reader will think of, and "**no licence has been read for either**", so naming
  one "converts an open research task into something that looks settled". Reversing section 6
  starts by reading a licence at source and adding the inventory row.

Reversing anything here also touches ADR-0061, because ADR-0092 carries `stack-record: true`. See
[`../authoring-an-adr/SKILL.md`](../authoring-an-adr/SKILL.md) for the five files that move
together.

## Who owns which question

| Question | Authority |
|---|---|
| Which tool runs each of the five stages, and why no third-party SAST | ADR-0092 |
| Where each scan sits in the pipeline, and which gates block a release | `docs/design/21-cicd-and-deployment.md` sections on quality gates and the CD scans |
| Whether a tool may be adopted at all, and the external-tool inventory | ADR-0026 section C, and `docs/DEPENDENCY-LICENSES.md` sections 2, 6, and 7 |
| The licence-scan gate, which is a different gate from stage 2 | ADR-0026 section C |
| The warning escalation that makes stage 1 fail a build | ADR-0093, with the restore-time `NU190x` carve-out |
| Analyzer breadth outside the security axis | ADR-0094: `Recommended`, not `All` |
| Action pinning, and the residual risk of a movable tag | ADR-0086 |
| The ASVS baseline these scans serve | ADR-0062, whose open analyzer choice ADR-0092 closed |
| Release supply-chain integrity, signing, and the rebuild cadence | ADR-0051 |
| Measured traps in the files that decide whether a gate bites | `.claude/rules/build-and-ci.md` |
| What each gate checks, and what it does not | `scripts/README.md` |
| How to prove a new gate is not inert | `.claude/skills/adding-a-ci-gate/SKILL.md` |
| What is owed and what triggers it | `docs/BUILD-PLAN.md`, a queue and never an authority |

## Which tool reads a scanner claim at its source

**A tool is a source, never an authority.** Where an external source and an accepted ADR disagree,
stop and flag both with file and line, and do not fill the gap from judgement.

| To read at source | Use | Why |
|---|---|---|
| A licence for a scanner | The distributed artifact itself, opened | Never a badge, never a summary, and never another document in this repository. ADR-0092 records four cases where this project was wrong about a licence, in both directions |
| A .NET analyzer or MSBuild claim | `microsoft-docs`: `microsoft_docs_search`, then `microsoft_docs_fetch` | The SAST stage is an SDK behaviour, and every figure in ADR-0092's Confirmation moves with the SDK version |
| Why a build passed or failed a security rule | The `dotnet-msbuild` binlog tools, through `binlog-generation` then `binlog-failure-analysis` | Stage 1 fails inside Solution build, so its evidence is a build log rather than a scan report |
| What the SDK actually applied | `dotnet msbuild -t:CoreCompile -getItem:EditorConfigFiles` | ADR-0092 used exactly this to find that an unset axis includes no security globalconfig at all, correcting an earlier claim that reasoned instead of measuring |

Every figure in this file that came from running something carries its date. Re-run it rather than
citing it forward.
