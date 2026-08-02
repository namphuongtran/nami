---
status: "accepted"
stack-record: true
date: 2026-08-02
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0062 (the ASVS baseline that left the analyzer choice open), ADR-0026 (the permissive-only dependency policy and the external-tool inventory this decision populates), ADR-0065 (the analyzer-at-error-severity posture this reuses), ADR-0051 (the release pipeline the scans gate), ADR-0060 (the CI composition), ADR-0061 (the stack of record, which had no row for any of this)
informed: all contributors, CI owners, and any adopter reproducing the security gates
---

# Pin the five CI security scans, and take no third-party SAST because the pinned SDK ships one

## Context and Problem Statement

[ADR-0062](0062-owasp-asvs-security-baseline.md) requires static analysis and dependency
scanning in CI and then declines to say what runs them: *"The specific analyzers are an open,
replaceable choice, not pinned here."* Design [20](../design/20-testing.md) section 6 repeats
that sentence, and design [21](../design/21-cicd-and-deployment.md) writes the CD stages as
slash-alternatives: SAST as CodeQL or Semgrep, the dependency scan as Trivy or OWASP
Dependency-Check, the container scan as Trivy or Grype, plus a gitleaks secret scan and an
OWASP ZAP DAST pass.

An open choice is defensible. What was not defensible is the state underneath it, and three
things surfaced on 2026-08-02 when the licences were finally read at source and recorded in
[`DEPENDENCY-LICENSES.md`](../DEPENDENCY-LICENSES.md) sections 2 and 6.

* **Nine executables were named across the pipeline and two were in the inventory.**
  [ADR-0026](0026-dependency-license-policy.md) section C's second limb requires every
  executable used in the pipeline to be inventoried, and fails CI when one is absent, precisely
  because the restore-graph scan is structurally blind to them. Seven were absent. Nothing was
  failing yet only because no pipeline runs before M1.
* **Two of the nine are not permissive, and neither had ever appeared in a table.** The CodeQL
  CLI is under proprietary terms, not an OSI licence, and Semgrep is LGPL-2.1. Both are SAST
  candidates, so the one stage nobody had verified was the one where both options were
  encumbered.
* **A tool choice had been made in the wrong layer.** Design 21's tool table fills the decision
  column with "this doc" for gitleaks, where every other row names an ADR. The design layer
  realizes decisions and does not make them, so that entry had no owner.

The question this ADR answers is therefore narrower than "which scanners". It is: what does
each stage run, what licence does each carry, and which stages does nothing cover.

## Decision Drivers

* **Every tool permissive, read at source, and inventoried before it is named.** The project has
  now been wrong about a licence in four recorded cases, in both directions.
* **Fewest tools that cover the stages.** Each tool is a licence to re-verify at adopt time, an
  inventory row, and a runtime in CI. Two tools covering four stages beats four covering four.
* **No gate an adopter cannot run.** Nami is redistributable. A security gate that works only
  for this repository, on one host, under one visibility setting, is a gate the project cannot
  honestly tell a consumer to rely on.
* **Prefer what the pinned stack already carries.** [ADR-0060](0060-testing-strategy.md) closed
  the assertion-library question by measuring the framework before shopping for a package. The
  same method applies here and produces the same shape of answer.
* **State the coverage gaps.** A scan list that reads as complete while a class of input goes
  unexamined is worse than a shorter list that says so.

## Considered Options

* **A. One tool per stage from the permissive candidates, and no third-party SAST**: the SDK's
  own security analyzers carry that stage.
* **B. As A, plus Semgrep for SAST**, under an ADR-0026 section C exception for LGPL.
* **C. As A, plus the CodeQL CLI for SAST.**
* **D. Leave the choice open**, as ADR-0062 does today, and decide at M1.

## Decision Outcome

Chosen option: **A**. The five stages below are binding.

### 1. SAST: the .NET SDK's security analysis axis, and no third-party tool

**The measurement that decided this, read in the SDK installed locally at 10.0.301 on
2026-08-02.** .NET carries an `AnalysisLevelSecurity` property separate from the ordinary
`AnalysisLevel`, accepting compound values. Its own targets file documents the form
(`Sdks/Microsoft.NET.Sdk/analyzers/build/Microsoft.CodeAnalysis.NetAnalyzers.targets`, in the
comment at lines 504 to 509): *"'5-all' - Indicates core AnalysisLevelSecurity = '5' with 'all'
the 'Security' rules enabled by default."*

The shipped configuration for the all-plus-warnaserror combination,
`build/config/analysislevelsecurity_10_all_warnaserror.globalconfig`, sets **94 diagnostics, all
of them at `severity = error`**, among them 17 distinct `CA3xxx` rules and 50 distinct `CA5xxx`
rules. Counted from that file. Quoted from it:

```text
# CA3001: Review code for SQL injection vulnerabilities
dotnet_diagnostic.CA3001.severity = error
# CA3003: Review code for file path injection vulnerabilities
dotnet_diagnostic.CA3003.severity = error
# CA3012: Review code for regex injection vulnerabilities
dotnet_diagnostic.CA3012.severity = error
```

**A severity setting is not an analyzer, so presence was verified separately.** A globalconfig
can name a rule that nothing implements, in which case the line is inert. The shipped
`Microsoft.CodeAnalysis.NetAnalyzers.dll` contains the rule titles above and the taint dataflow
engine itself: the types `TaintedDataAnalysis`, `TaintedDataAnalyzerBase`,
`TaintedDataAbstractValue` and their supporting domain types are present in the assembly. The
CA3xxx family is source-to-sink taint analysis, which is what a SAST tool is taken for.

This costs no dependency and adds no licence to the record: the analyzers ship inside the .NET
SDK, whose `LICENSE.txt` at the install root is the MIT License (read at 10.0.301, 2026-08-02),
and the runtime version is already governed by [ADR-0030](0030-dotnet-version-upgrade.md).
[ADR-0065](0065-coding-and-naming-conventions.md) already runs analyzers at error severity in
the build, so this is a wider rule set on an existing gate rather than a new gate.

**No third-party SAST tool is taken, and the two candidates are rejected for different
reasons.** Semgrep is LGPL-2.1 at both its root `LICENSE` and its `cli/LICENSE`, which ADR-0026
section A routes through the exception process rather than banning: it is available, at the cost
of a standing exception, and it is the named candidate if this decision is reversed. The CodeQL
CLI is rejected on a stronger ground than its licence class. Its terms grant CI use only with an
Open Source Codebase *"hosted and maintained on GitHub.com"*, and forbid every other automated
analysis context absent a paid GitHub Advanced Security licence. Nami satisfies that grant today
and would not be in breach. The objection is the driver above: adopting it would bind the
security gate to this project remaining public on one host, and would hand any adopter who forks
privately a gate they cannot run.

### 2. Dependency vulnerability scan: Trivy

Apache-2.0, read at `aquasecurity/trivy` `LICENSE` on 2026-08-02. This is distinct from the
ADR-0026 section C licence-scan gate, which reads licences rather than vulnerabilities, and from
the Dependabot scaffolding ADR-0062 already names. It is blocking, as design 21 requires. OWASP
Dependency-Check is Apache-2.0 as well and stays recorded as a verified alternative.

### 3. Secret scan: gitleaks

MIT, read at `gitleaks/gitleaks` `LICENSE` on 2026-08-02. **This ADR is its owner**, which is the
substantive part of this clause: the tool was already in the pipeline with a design document
listed as its decision-maker, and this moves that choice into the layer that makes decisions.

### 4. Container scan: Trivy

The same tool as stage 2, deliberately. One tool covering both stages means one licence to
re-verify at adopt time and one inventory row instead of two, and design 21 already names Trivy
for both. Grype is Apache-2.0 and stays recorded as a verified alternative.

### 5. DAST: OWASP ZAP, against staging

The root licence is Apache-2.0, and the bundle is the point: `LEGALNOTICE.md` in the same
repository lists thirty bundled components, of which seven are outside ADR-0026 section A's
permissive set, including `javahelp` under GPL with the classpath exception and four in the LGPL
family. This is answerable, and only answerable, under section C's conveying-versus-executing
paragraph: ZAP runs as a separate process against a staging deployment and enters no distributed
artifact. It is classified `execute-only`, and the enumeration of its bundled components is owed
again at adopt time against the exact released version, per `DEPENDENCY-LICENSES.md` section 7.

### What none of these cover, stated rather than implied

The SDK analyzers see C#. They do not see Razor markup, SQL held outside C#, Dockerfiles, or
GitHub Actions workflow definitions. Three of those four are covered elsewhere: secrets by
gitleaks across the tree, the image by Trivy, and dependency vulnerabilities by Trivy again.
**Workflow definitions were covered by nothing here.** Untrusted input reaching a workflow
expression is a real class of supply-chain defect, and [ADR-0086](0086-pin-ci-actions-by-commit-sha.md)
addresses which actions run rather than what a workflow does with input. That was recorded as a
Pre-GA ratification item rather than solved by adding a tool whose licence would then have to be
carried for one stage.

### 6. Workflow definitions: two bright lines in the existing guardrail, and no sixth tool (binding, added 2026-08-02)

This closes the gap above. It is deliberately **not** numbered among the five scans and the
title is unchanged, because it adds no tool and no stage: it is two checks inside
`scripts/check-adrs.sh`, the guardrail that already runs as a blocking CI job.

**The measurement came first, and it moved the question.** Read on 2026-08-02, this repository
has one workflow file, forty-one lines. Across the whole `.github/` tree there are **zero**
`${{ }}` expressions of any kind, **zero** `pull_request_target`, `workflow_run`,
`issue_comment`, or `workflow_dispatch` triggers, two `run:` steps that are literal command
strings, a top-level `permissions: contents: read`, both `uses:` pinned by SHA, and no
reference to any secret. **A tool bought today would find nothing.** What makes the gap real is
not the present state but the next one: design [21](../design/21-cicd-and-deployment.md)
section on the release pipeline signs keyless through GitHub Actions OIDC, which means a job
carrying `id-token: write`, and `.github/dependabot.yml` already opens weekly pull requests
against `.github/workflows/` itself.

**So the decision is a regression guard rather than a finder**, and the two rules are chosen so
that neither needs a judgement about which inputs are trusted:

* **No `${{ ... }}` inside any `run:` script.** Interpolation into a shell is the injection
  vector, and the standard mitigation is to pass the value through `env:` and reference it as a
  shell variable. The rule therefore **enforces the mitigation** instead of trying to classify a
  value as dangerous, which is the part that genuinely needs a tool.
* **No `pull_request_target:` and no `workflow_run:` trigger.** Both combine write-scoped
  permissions and secrets with code the proposer controls. Neither exists today, so this costs
  nothing now and makes adding one a deliberate exception with a reason rather than a line in a
  diff nobody reads twice.

**What the checks do not see, stated in the script itself and repeated here** because a green
that is read as "the workflows are safe" would be worse than no check: interpolation into an
action's `with:` inputs, the scope of a `permissions:` block, composite actions and reusable
workflows defined in other repositories, and anything at all about what a pinned action does.
The green is a statement about two constructs.

**Proven before wiring, per the rule in `scripts/CLAUDE.md` that a check never run against the
bug it exists for is not known to work.** A workflow was written carrying an inline `run:`
interpolation, the same interpolation inside a block scalar, a `pull_request_target` trigger,
and four constructs that must **not** trip: an `if:` expression, a `with:` expression, an `env:`
expression, and a `run:` reading that `env:` value as a shell variable. Untracked it produced
nothing, since the check reads the git index; staged it produced exactly three problems and left
the four alone, including the `env:`-then-`$VAR` form that is the mitigation the rule exists to
push people toward. Writing it also found that the script's untracked-file coverage warning
named only markdown, so a new workflow would have been invisible **and** unannounced, which is
the same false-green shape that warning was added for; it now covers workflows too.

**That proof was then made permanent, because a one-off proof was not enough here.** The rules
match with `awk`, and the CI runner's awk is a different implementation from the one they were
authored against, so a green guardrail on a clean tree proves only that the awk parses: a clean
tree has no violation to match. `scripts/test-check-adrs.sh` plants the violations on every run,
in a throwaway worktree, and CI runs it beside the guardrail. **Building it turned up a false
green of its own**, which is worth recording because the shape recurs: the first version ran the
guardrail inside a worktree checked out at `HEAD`, so deleting the block-scalar rule from the
working tree left the test green, it having tested the committed script rather than the edited
one. Found by breaking the subject deliberately rather than by reading the test.

**The reversal condition is recorded and no candidate is named, which is deliberate.** Reverse
this when the bright lines stop being able to hold the surface, concretely when a `with:`-input
flow or a `permissions:` scope question becomes a real one rather than a hypothetical, or when
M1's release pipeline lands and the re-read below finds two rules insufficient. The replacement
would then be a dedicated GitHub Actions analyser.

**Naming one here was considered and dropped.** Two exist that a reader will think of, and
writing their names would have cost nothing except accuracy: **no licence has been read for
either**, so the entry would have looked like the Semgrep reversal record for SAST while being a
different thing. Semgrep is named because it was read at source, LGPL-2.1 at both its root
`LICENSE` and its `cli/LICENSE`, and it carries a row in `DEPENDENCY-LICENSES.md` with that
reading and its date. A name with no reading behind it converts an open research task into
something that looks settled, which is the failure this repository treats as worse than an
omission. So the condition is written and the search is left honestly open: reversing starts by
reading a licence at source and adding the row, exactly as the five tools above did.

### Consequences

* Good, because every stage now names one tool, each licence was read at source and is in the
  inventory the ADR-0026 gate checks completeness against, so the pipeline can no longer be
  green on tools nobody recorded.
* Good, because the SAST stage costs nothing: no package reference, no new licence, no new CI
  runtime, and no coupling to a host or a visibility setting. An adopter who forks this project
  privately runs exactly the same SAST gate.
* Good, because using one tool for two stages halves the re-verification and inventory burden
  that this project has repeatedly failed to keep up with.
* Good, because the reversal condition is written down with its candidate **for the five stages
  above**, so a later change there is a stack-row edit and a recorded exception rather than a
  research task. **Section 6 is the exception and says so**: its condition is recorded and no
  candidate is named, because none has had its licence read, so reversing it genuinely is a
  research task and pretending otherwise would be the more expensive error.
* Bad, because the SDK analyzers are C#-only and shallower than a dedicated SAST engine on
  cross-file flows. Accepted, with the gap named above rather than papered over, and with
  Semgrep pre-identified if a concrete finding shows the gap is real.
* ~~Bad, because workflow-definition analysis is left uncovered until a Pre-GA decision.~~
  **Closed 2026-08-02 by section 6**, and the shape of the close is worth noting: the same
  argument that took the SDK's own analyzers for SAST, that a capability already present costs
  no licence, applied a second time to a guardrail script this repository already runs. The
  cost stays at zero and the coverage is two constructs rather than a tool's whole rule set,
  which section 6 says out loud.
* Bad, because concentrating two stages on Trivy makes a single project's licence drift a
  two-stage problem. Mitigated by both alternatives staying verified with read locations and
  dates, so replacing it is a swap rather than a search.
* Neutral, because no ASVS level, threshold, or release gate defined elsewhere changes.

### Confirmation

* Licence rows exist for every tool named here, with the read location and the date, in
  `DEPENDENCY-LICENSES.md` sections 2 and 6. A tool moves from section 6 to section 2 in the
  same change that first runs it in the pipeline.
* **M1**: the exact property combination that selects the `_warnaserror` variant of the security
  globalconfig was **not** read and is not guessed here. Confirm it against a real build when the
  first project lands, and record the result. The companion this named, "the C# `.editorconfig`
  ruleset that ADR-0065 already defers to M1", **stopped being one on 2026-08-02**, when that
  ruleset landed early against a throwaway fixture built by `scripts/test-editorconfig.sh`. The
  item here does not move with it: an MSBuild property combination that selects a globalconfig
  variant is a property of a **real** project's build, and a fixture written to exercise a style
  ruleset would answer it only by accident.
* **At adopt time**: enumerate ZAP's bundled components against the exact released version, per
  the `DEPENDENCY-LICENSES.md` section 7 maintenance rule, rather than relying on the reading
  taken here against the repository's default branch.
* ~~**Pre-GA**: ratify or accept the workflow-definition coverage gap.~~ **Ratified 2026-08-02**
  as section 6. The standing obligation that replaces it: the two rules are a **regression
  guard measured against a workflow set of one file**, so re-read them when the release
  pipeline lands at M1, which is when a job first carries `id-token: write` and when the four
  remaining CI security stages become workflow steps.

## Pros and Cons of the Options

### A. Permissive tools per stage, SAST from the SDK (chosen)

* Good, because the SAST capability is already paid for and carries no licence, no host coupling
  and no adopter-visible difference.
* Good, because every remaining tool is permissive with the licence read at source.
* Bad, because C#-only analysis is narrower than a dedicated engine, and the workflow surface is
  left open.

### B. As A, plus Semgrep under an LGPL exception

* Good, because it adds cross-file and cross-language rules that Roslyn cannot express, and
  LGPL-2.1 on an execute-only CI tool is the easiest version of the exception case.
* Bad, because it buys that before any concrete gap has been demonstrated, and a standing
  exception has to be re-explained to every reader of ADR-0026.
* Retained as the named reversal candidate rather than rejected.

### C. As A, plus the CodeQL CLI

* Good, because its .NET dataflow analysis is the deepest of the options considered, and its
  free grant genuinely covers this project as it exists today.
* Bad, and decisively, because the grant is conditional on the codebase being open source *and*
  hosted on GitHub.com. That makes the security gate a property of where the repository lives
  rather than of the project, and it silently excludes any adopter with a private fork.
* Bad, because it is not an OSI licence, so it sits outside ADR-0026 section A's allow-list and
  would need an exception to be argued on top of the coupling above.

### D. Leave the choice open

* Good, because it defers a decision that will be easier once code exists to scan.
* Bad, because the openness was not the problem. Seven executables were missing from the
  inventory the ADR-0026 gate checks, and the two tools nobody had verified were the two that
  are not permissive. Deferring again would have preserved both.

## More Information

* Related decisions: ADR-0062 (the ASVS baseline whose open analyzer choice this closes),
  ADR-0026 (the permissive-only policy, the external-tool inventory, and the
  conveying-versus-executing rule that makes the DAST stage answerable), ADR-0065 (the existing
  analyzers-at-error posture this widens), ADR-0030 (the runtime version that determines which
  SDK ships the analyzers), ADR-0051 (the release pipeline these scans gate), ADR-0086 (actions
  pinned by SHA, adjacent to the workflow gap named above but not the same question), ADR-0060
  (the CI composition), and ADR-0061 (the stack of record, which gains a row).
* **Method note, worth keeping because it nearly produced a wrong answer.** Searching the shipped
  analyzer assembly for the literal string `CA3001` returned nothing, which would have supported
  the opposite conclusion. The rule identifiers are not stored as literals; the rule titles and
  the dataflow engine types are. An absence found by one search pattern is a claim about the
  pattern. What was searched is written into the decision above for that reason.
* **The question changed shape when the pinned toolchain was measured first**, which is the
  second time in two days: ADR-0060 turned "which assertion library" into "do we need one" the
  same way. A stage written as "tool X or tool Y" quietly asserts that a tool is needed, and
  that assertion is the one worth testing before comparing the candidates.
* Licences read at source on 2026-08-02, each opened rather than inferred: the .NET SDK
  (`LICENSE.txt` at the 10.0.301 install root), Trivy, Grype, OWASP Dependency-Check, gitleaks,
  OWASP ZAP (`LICENSE` and `LEGALNOTICE.md`), Semgrep (`LICENSE` and `cli/LICENSE`), the CodeQL
  query packs and the CodeQL CLI (two repositories, two different licences). Read locations and
  dates are in `DEPENDENCY-LICENSES.md` section 6.
* Authored fresh for this repository, not imported from the design corpus.
