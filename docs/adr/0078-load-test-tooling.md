---
status: "accepted"
stack-record: true
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0026 (the permissive-only dependency policy this decision applies), ADR-0041 (the SLO release gate the tool enforces), ADR-0060 (the testing strategy that lists load and soak as a layer), ADR-0061 (the stack of record that had no row for this technology)
informed: all contributors, CI owners, and anyone reproducing the SLO gate
---

# Adopt Apache JMeter as the load-test tool, replacing k6 and NBomber

## Context and Problem Statement

The load-test tool is load-bearing: [ADR-0041](0041-nfr-targets-and-slo-release-gate.md)
makes it a formal release gate that fails the build when p95 or p99 breaches, and
[ADR-0060](0060-testing-strategy.md) lists load and soak as a testing layer. Two tools were
named across the docs, and **both were unusable under
[ADR-0026](0026-dependency-license-policy.md), for two different reasons, and neither was
caught by the license gate.**

* **NBomber was recorded as Apache-2.0 and is not.** Its license is the NBomber License
  Agreement v3.0, verified in the `LICENSE` file shipped inside the NBomber 6.5.0 package on
  2026-08-01: *"NBomber is not free for organizational use. Any use by, for, or on behalf of an
  organization ... requires a valid Commercial Subscription"*, and separately it forbids
  providing the software as SaaS. The package's nuspec declares `<license type="file">` with
  `requireLicenseAcceptance`, never an SPDX expression. It is therefore a section A violation,
  and the repository had asserted the opposite. Removed in the same change that opened this ADR.
* **k6 is genuinely open source but is AGPL-3.0**, verified at `grafana/k6/LICENSE.md` on
  2026-08-01 with section 13 (remote network interaction) present. ADR-0026 section A bans
  viral copyleft (GPL, AGPL) because Nami is redistributable, SaaS, and multi-tenant.

**Why the gate did not catch either, which is the more important part of this context.**
ADR-0026 section C defines the gate as reading the license of every package from the **NuGet
restore graph**. That is structurally incapable of seeing an external tool: k6 is a standalone
Go binary run as a separate process and is not in the graph at all. And NBomber, though it is a
NuGet package, was recorded in prose as Apache-2.0, so no human reading the docs had reason to
doubt it. **The tool also had no row in the [ADR-0061](0061-technology-stack-of-record.md)
stack table**, which is precisely the blindness ADR-0061 itself documents: a technology with no
row there and no marked ADR produces two empty entries that agree, so the guardrail stays
green. This ADR closes that omission by existing and by adding the row.

There is a real technical constraint on any replacement. The tool must drive an **open
workload model** (arrival-rate driven), not a closed one (fixed virtual-user count). A closed
model produces **coordinated omission**: it backs off exactly when the server struggles, which
hides tail latency and would make the p99 gate report a number that is not the truth. This
property is the reason a load tool was chosen deliberately rather than picked for familiarity.

## Decision Drivers

* **Permissive license with no companion-module trap.** Not merely a permissive root `LICENSE`,
  but permissive across whatever the distributed bundle actually contains.
* **Open workload model**, to keep the p99 figure honest (coordinated omission).
* **Correlated multi-step flows.** Nami's scenarios are OAuth flows: obtain an authorization
  code, exchange it, then rotate a refresh token. Each step consumes a value produced by the
  previous response, so a tool that can only replay a static target list cannot express them.
* **CI enforceability**: percentile thresholds plus a non-zero exit on breach.
* **Minimal new runtime surface** in CI.
* **Resistance to license drift.** Two of the tools considered here changed or revealed their
  license terms in a way the repository did not notice. Structural protection is worth more
  than a one-time check.

## Considered Options

* **A. Apache JMeter** (Apache-2.0)
* **B. Vegeta** (MIT)
* **C. Locust** (MIT)
* **D. Gatling** (Apache-2.0 core, proprietary report module)
* **E. Keep k6 and add an execute-versus-convey carve-out to ADR-0026**

## Decision Outcome

Chosen option: **A, Apache JMeter**, because it is the only candidate that satisfies the
license driver **structurally** rather than by inspection, while still meeting the open-model
and correlated-flow requirements.

**The license argument, which is the decisive one.** JMeter is an Apache Software Foundation
project, and the ASF third-party licensing policy states, verified at
`apache.org/legal/resolved.html` on 2026-08-01: *"Apache projects may not distribute Category X
licensed components, in source or binary form; in ASF source code or in convenience binaries."*
GPL 1, 2, 3 and AGPL 3 are named Category X, as are non-commercial and field-of-use
restrictions. So the failure mode that disqualified Gatling **cannot occur in an ASF release**:
a proprietary companion module could not be distributed in one. JMeter's own license was read
at source (`apache/jmeter` `master` `LICENSE`, Apache License Version 2.0, 2026-08-01).

State the limit of that argument honestly: the ASF policy permits a project to *rely on* a
Category X component during development for an optional feature that users obtain separately.
It is therefore a guarantee about what is **distributed**, not a guarantee that nothing
Category X is touched anywhere. The release bundle's own `LICENSE` and `NOTICE` are still
checked at adopt time (see Confirmation).

**Corrected 2026-08-02: the limit stated above is real but it is not the load-bearing one, and
the paragraph above guarded the wrong flank.** It warned about a Category X component being
*used* in development. The limit that matters is that the same ASF policy **expressly permits
Category B in the very artifact this decision consumes**, read at the same source on 2026-08-02:
*"Any Category B licensed works may be included in binary-only form in Apache Software
Foundation convenience binaries."* Category B is weak copyleft, and ADR-0026 section A is
stricter than Category X: it forbids GPL and AGPL outright, but it also does not list EPL, CDDL,
OFL, CC-BY-SA or Apache-1.1 in any of its three buckets. So the ASF guarantee eliminates exactly
the class that was already banned and leaves untouched the class that needs a decision. The
adopt-time read in Confirmation was therefore not a formality, and doing it early proved it:
JMeter 5.6.3's own release `LICENSE` declares fourteen SPDX identifiers, of which **nine
components sit outside section A's permissive set**, enumerated with their identifiers in
[`docs/DEPENDENCY-LICENSES.md`](../DEPENDENCY-LICENSES.md) section 2.1.

**This does not reverse the decision, and the reason is the classification rather than the
licences.** Nami runs an unmodified JMeter as a separate process against its own service and
ships none of it, so every one of the nine is answerable as execution rather than conveying,
which is the distinction ADR-0026 section C draws and enforces with the `execute-only` boundary
check. What does change is the question put to Legal before GA, which was written as an
inventory confirmation and is now a named-component judgement; the Pre-GA checklist entry
carries the corrected wording. It should also be said plainly that **no candidate here would have
survived this test better**, because none of them was ever tested on it: a packaged load tool
bundles a hundred-odd components whoever publishes it, and this enumeration was performed on the
chosen option only. What the options above were separated on is a different and worse defect,
one candidate's own licence (option E) and another's proprietary companion module (option D),
and neither finding says anything about the bundle behind an option that passed.

**The open-model requirement is met in core, not by a plugin.** Verified present in
`apache/jmeter` `master` on 2026-08-01: `PreciseThroughputTimer.java`, in the package
`org.apache.jmeter.timers.poissonarrivals`, and `ConstantThroughputTimer.java`, both under
`src/components`. The package name states the mechanism: arrivals follow a Poisson process, so
throughput is driven by an arrival rate rather than by thread count. No third-party plugin is
required, which matters because a plugin would reintroduce exactly the companion-module license
surface this decision is avoiding.

**Correlated multi-step flows** are native to JMeter through its extractor post-processors,
which is what lets one sampler consume a value from a previous response.

**Thresholds are unchanged.** This ADR changes the tool, not a single target. The gate remains
p95 under 200 ms and p99 under 500 ms on the token endpoint, error rate under 0.5%, non-zero
exit on breach, run in an open model against a discarded warm-up, exactly as ADR-0041 sets it.

**Left open deliberately, rather than invented.** The concrete mechanism by which the gate
asserts percentiles from a JMeter result file and exits non-zero is **not specified here**,
because it has not been built or verified. JMeter has no direct equivalent of a declarative
`abortOnFail` threshold block, so writing one would be a fabricated API. It is an M1 item,
recorded in Confirmation. A missing mechanism marked as open is worth more than a plausible one
that does not exist.

### Consequences

* Good, because the license position is structural **against Category X**, so the failure that
  disqualified two candidates cannot recur here without anyone repeating the check. Qualified
  2026-08-02: it is structural against that class only, and the bundle carries nine components
  outside section A's permissive set that the `execute-only` boundary, not the ASF policy, is
  what answers.
* Good, because the open-model property is satisfied by core rather than by a plugin, removing
  a companion-dependency surface.
* Good, because the tool now has a row in the ADR-0061 stack table and a marked ADR, so the
  guardrail can see it. It could not before.
* Bad, because CI gains a **JVM** runtime for a .NET project. This is a real cost and was
  accepted, because every candidate that met the correlated-flow requirement carried either a
  JVM or a Python runtime, and the licence-clean ones with no extra runtime (Vegeta) could not
  express the OAuth flows.
* Bad, because JMeter's native test plan format is XML, which reviews poorly in a pull request.
  Writing the plan through a code-level DSL is preferred where one is available, and the choice
  is an M1 item rather than a decision here.
* Neutral, because no NFR target, SLO, error budget, or burn-rate policy changes.

### Confirmation

* The **external-tool limb** of the license gate covers this tool. The ADR-0026 section C
  restore-graph scan cannot see any external binary, so an external-tool inventory that is
  license-checked separately is what makes this decision auditable rather than assumed.
* An entry in `docs/DEPENDENCY-LICENSES.md` recording the tool, its license, the verification
  source, and the date.
* **M1 items**, both stated as open rather than guessed: the mechanism that asserts percentiles
  from the JMeter result file and fails the build, and whether the test plan is authored through
  a code-level DSL instead of raw XML.
* **The shipped release's own `LICENSE` and `NOTICE` were read on 2026-08-02, ahead of M1 rather
  than at it**, at version 5.6.3, and the result is in
  [`docs/DEPENDENCY-LICENSES.md`](../DEPENDENCY-LICENSES.md) section 2.1: nine bundled components
  outside ADR-0026 section A's permissive set, no AGPL. This item was written because relying on
  the ASF policy alone is not enough, and it was right. It does not close: at M1 the read is
  repeated against whatever version is actually pinned, and what that read has to produce is a
  **diff** against section 2.1 rather than a fresh investigation.

## Pros and Cons of the Options

### A. Apache JMeter (chosen)

* Good, because ASF policy forbids distributing Category X components, making the licence
  position structural against that class. It permits Category B in a convenience binary, so the
  bundle still had to be enumerated, and was (section 2.1 of the licence record).
* Good, because the open arrival-rate model is in core (`PreciseThroughputTimer`, verified).
* Good, because extractors express correlated multi-step OAuth flows natively.
* Bad, because it adds a JVM to CI.
* Bad, because raw test plans are XML.

### B. Vegeta

* Good, because MIT is the cleanest licence considered, it is a single Go binary, and it has no
  companion module in which a trap could hide.
* Good, because it is rate-based by construction, so the open model is not something to
  configure correctly but the only thing it does.
* Good, because it adds no runtime to CI.
* Bad, and decisively, because it replays a target list and does not carry values between
  requests, so the authorization-code exchange and refresh rotation that make up Nami's
  scenarios would have to be hand-built around it. That converts a tool choice into a
  maintained harness.

### C. Locust

* Good, because MIT is clean and Python expresses correlated flows readably.
* Bad, because its model is user-based and therefore closed by default. Approximating an
  arrival rate through per-user pacing is not the same as an arrival-rate model, and it is weak
  at exactly the property that made the tool choice deliberate.
* Bad, because it adds a Python runtime.

### D. Gatling

* Good, because the core is genuinely Apache-2.0 (root `LICENSE.txt` and `NOTICE.md`, read
  2026-08-01) with a native open model and native correlated flows. On features it was the
  closest match to k6.
* Bad, and disqualifying, because the standard report-generation module is **not open source**.
  `license/LICENSE.gatling-highcharts.specific.txt`, read at source on 2026-08-01: *"GatlingCorp
  grants this license allowing the use, free-of-charge of the report generation module as
  delivered. No code modification is authorised, no re-use of the code, no copying of all or
  any part of the code is allowed."* It is governed by French law with the Nanterre court named.
  Free of charge is not permissive: no modification and no reuse fails the OSI definition, which
  places it in the source-available-non-OSS class that ADR-0026 section A bans alongside BSL and
  SSPL. The bundle also ships `LICENSE.jmh.gpl2.txt` (GPL-2.0) plus EPL and MPL dependencies.
* Bad, because the survivable form of this option is "use the Apache-2.0 core and never the
  report module", which is an **exception that has to be remembered**. A documented exception is
  discovered by reading; a remembered one is discovered by failing.
* Worth recording as a method lesson: the GitHub API reported this repository as `Apache-2.0`,
  because it reads only the root licence file. The trap was one directory down. A licence check
  must look at the distributed bundle and its companion modules, not the root `LICENSE`.

### E. Keep k6 with an execute-versus-convey carve-out

* Good, because k6 is technically the best fit and the ban's own rationale does not reach it:
  ADR-0026 bans copyleft to avoid being forced to open the redistributed or SaaS product, which
  is reasoning about linking and conveying. Running an unmodified external binary in CI against
  our own service neither modifies nor conveys it, and AGPL section 13 obliges a **modified**
  version. This is the option the design corpus chose.
* Bad, because it requires a Legal answer before it is safe to rely on, and it leaves a standing
  exception to maintain and to re-explain to every reader of ADR-0026.
* Bad, because the carve-out has a sharp edge that is easy to cross: putting the k6 binary into
  any shipped artifact (the reference host image, the Helm chart, the NuGet meta-package, the
  `dotnet new` template) **is** conveying, and AGPL would then apply in full.
* Rejected by Nam on 2026-08-01, in favour of removing the question rather than answering it.

## More Information

* Licences verified at source on 2026-08-01, each read rather than inferred: NBomber
  (`LICENSE` inside the 6.5.0 nupkg, plus the nuspec licence declaration), k6
  (`grafana/k6/LICENSE.md`), JMeter (`apache/jmeter` `master` `LICENSE`), Gatling (root
  `LICENSE.txt`, `NOTICE.md`, and `license/LICENSE.gatling-highcharts.specific.txt`), Vegeta and
  Locust (their `LICENSE` files read directly). Artillery was noted as MPL-2.0 and not pursued,
  because ADR-0026 routes MPL through the exception process and this decision exists to avoid
  needing one.
* Related decisions: ADR-0026 (the permissive-only policy and the licence gate, which this ADR
  applies and whose external-tool blindness it exposes), ADR-0041 (the SLO release gate and
  every threshold, unchanged), ADR-0060 (the testing strategy layer this tool sits in),
  ADR-0061 (the stack of record, which gains a row for this technology), and ADR-0027 (the
  distribution artifacts that define what conveying means for this project).
* This ADR supersedes the naming of k6 and NBomber wherever the docs carried them. It does not
  supersede any target, threshold, or method in ADR-0041.
* Method note worth keeping, because it cost two wrong candidates. A licence is not verified by
  a badge, an API field, or a root `LICENSE` file. NBomber's trap was in prose the repository
  wrote about it; Gatling's was in a sibling directory. Both were found only by opening the
  actual licence text of the actual distributed thing.
