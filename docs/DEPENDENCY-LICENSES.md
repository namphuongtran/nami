# Dependency licence record and exception log

> **What this file is.** [ADR-0026](adr/0026-dependency-license-policy.md) section C requires an
> exception log and states that "there are no silent exceptions". This is that log, widened to
> also carry the licences that the CI licence gate **cannot** check, because a record of what
> the gate cannot see is worth more than a record of what it can.
>
> **What this file is not.** It is not the dependency manifest. Once code lands at M1, the
> authoritative list of compiled dependencies is `Directory.Packages.props` and the CI
> licence-scan gate that reads the restore graph. This file records decisions, exceptions, and
> the out-of-graph tools, all of which outlive any particular manifest.

## 1. Why this file has to exist

The ADR-0026 section C gate reads the licence of every package from the **NuGet restore graph**.
That leaves three blind spots, and every licence defect this project has actually had fell into
one of them:

1. **External tools.** A load-test binary, a conformance-suite container image. Not NuGet
   packages, not in the graph, invisible to the scan. The gate's silence about them is not
   evidence of compliance.
2. **Licences asserted in prose.** A package can be recorded in a design document as permissive
   and simply not be. No scanner reads prose, and a reader has no reason to doubt a table.
3. **Companion modules.** A project's root `LICENSE` can be permissive while a module in the
   same distribution is not. Badges and repository-metadata APIs read only the root file. This
   is the blind spot that has now been hit twice, on Gatling and on a code-quality server
   (section 4 both times), and the second time the non-permissive module was declared in the
   distribution's own build manifest, which is a file no licence tool reads.

Each row below therefore records **where the licence was read and on what date**, not just the
licence name. A licence with no source is an assertion, and assertions are what went wrong.

## 2. External tools, outside the restore graph

These are executed as separate processes and are never compiled into, linked against, or
shipped inside any Nami artifact. Bundling any of them into a distribution artifact (the
reference host image, the Helm chart, the NuGet meta-package, the `dotnet new` template) would
change the question from execution to conveying, and would need a new decision.

| Tool | Role | Licence | Boundary | Read at | Date | Decision |
|---|---|---|---|---|---|---|
| Apache JMeter | Load and soak testing, the SLO release gate | Apache-2.0 | `execute-only` | `apache/jmeter` `master` `LICENSE` | 2026-08-01 | [ADR-0078](adr/0078-load-test-tooling.md) |
| OIDF conformance suite | OpenID certification profiles, self-hosted image | MIT | `execute-only` | `openid/conformance-suite` `LICENSE.txt` (GitLab, `master`) | 2026-08-01 | [ADR-0027](adr/0027-packaging-and-distribution.md) |

**The `Boundary` column is checked, not merely declared** (ADR-0026 section C). CI fails when a
tool classified `execute-only` appears in the file list of a published artifact, and it fails
when an executable used in the pipeline is **missing from this table**. The second direction is
the one that catches the case this whole file exists for: a tool nobody recorded is exactly
what the restore-graph scan reports clean on, so an inventory with no completeness check is
another control that reads as coverage while inspecting nothing. Both tools above are
`execute-only`; neither is permissive-only by luck, but the classification is what makes the
distinction enforceable rather than a sentence someone has to remember.

JMeter carries a second, structural assurance worth recording: it is an Apache Software
Foundation project, and the ASF third-party policy states that "Apache projects may not
distribute Category X licensed components, in source or binary form; in ASF source code or in
convenience binaries" (read at `apache.org/legal/resolved.html`, 2026-08-01), with GPL 1/2/3 and
AGPL 3 named as Category X. The limit of that assurance is stated in ADR-0078: it governs what
is distributed, not what may be relied on during development, so the shipped release's own
`LICENSE` and `NOTICE` are still read at adopt time.

## 3. Named exceptions under ADR-0026

| Package | Licence | Reason it is an exception | Approver | Date |
|---|---|---|---|---|
| `Duende.AccessTokenManagement` | Apache-2.0 | Section E naming rule: recorded by its real package identifier even though the identifier carries the name of a vendor whose commercial products this project does not name. Concealing it would make the dependency record wrong and defeat the section C gate, which matches on exact package IDs. Published from the vendor's separate FOSS repository, not its commercial line. | Architect | 2026-07-25 |
| `Duende.AccessTokenManagement.OpenIdConnect` | Apache-2.0 | As above. | Architect | 2026-07-25 |
| `Duende.IdentityModel` | Apache-2.0 | As above; transitive through the two packages above. | Architect | 2026-07-25 |
| `Yarp.ReverseProxy` | MIT | Not an exception to the policy: recorded because it was named as a dependency in ADR-0029, ADR-0024, ADR-0061 and design [24](design/24-bff.md) with its licence asserted only in prose, which is blind spot 2 above. Read at the `.nuspec` (`<license type="expression">MIT</license>`, version 2.3.0, repository `github.com/dotnet/yarp`) rather than from a badge. | Architect | 2026-08-01 |

Licences for the three above were verified at nuget.org on 2026-07-25 at versions 4.2.0 and
8.1.0. See ADR-0026 section E for the exact scope of that naming exception and what it does not
cover.

## 4. Rejected packages and tools, with the evidence

Recorded because a forbidden-package list that will not say which package is forbidden cannot be
acted on (ADR-0026 section E). Most rows were at some point recorded in this repository as
acceptable, which is the expensive case. Not all: FluentAssertions was excluded before this log
existed, and the four Sonar rows were evaluated and rejected on 2026-08-02 having never been
recorded here at all. A rejection that cost nothing still earns a row, because the next reader's
question is "was this looked at", and an empty file answers no.

| Package | Claimed here as | Actually | Read at | Date | Outcome |
|---|---|---|---|---|---|
| NBomber | Apache-2.0 | **NBomber License Agreement v3.0**, commercial | `LICENSE` inside the NBomber 6.5.0 nupkg | 2026-08-01 | Removed, [ADR-0078](adr/0078-load-test-tooling.md) |
| k6 | AGPL, kept as "a dev-time tool, not a dependency" with no decision behind it | **AGPL-3.0**, section 13 present. Correctly identified, but the carve-out was never decided | `grafana/k6/LICENSE.md` | 2026-08-01 | Removed rather than carved out, [ADR-0078](adr/0078-load-test-tooling.md) |
| FluentAssertions | (already excluded) | Commercial from v8 | Excluded before this log existed | 2026-07 | Not taken. This cell used to add that the replacement was an M1 pick from section 5; ADR-0060 closed that on 2026-08-02 by taking no assertion library at all, so the exclusion here stands on its own licence ground and nothing is pending behind it |
| Gatling | Apache-2.0, per repository metadata | Core Apache-2.0, **but the standard report module is proprietary**: "No code modification is authorised, no re-use of the code, no copying of all or any part of the code is allowed" | `license/LICENSE.gatling-highcharts.specific.txt` | 2026-08-01 | Not taken, [ADR-0078](adr/0078-load-test-tooling.md) |
| MediatR, AutoMapper, MassTransit | n/a | Moved to commercial licensing | ADR-0026 section A | 2026-07-04 | Forbidden by name, ADR-0026 |
| `SonarAnalyzer.CSharp` 10.31.0.145097 | n/a, never recorded here | **SONAR Source-Available License v1.0**, source-available and not OSS: it defines "Competing" as "marketing a product or service as a substitute for the functionality or value of SonarQube" | `licenses/LICENSE.txt` inside the distributed nupkg; its nuspec declares `<license type="file">licenses\LICENSE.txt</license>`, never an SPDX expression | 2026-08-02 | Not taken. Added to the ADR-0026 section C deny-list, because the `type="file"` declaration is the case a name check exists for |
| `org.sonarsource.dotnet:sonar-csharp-plugin` 10.31.0.145097 | n/a | SSALv1, the same licence and the same version as the NuGet package above | `<licenses><license><name>SSALv1</name>` in the POM at Maven Central | 2026-08-02 | Not taken. This is the artifact the server bundles, see the row below |
| SonarQube server, the container distribution | n/a | Root `LICENSE.txt` is LGPL-3.0 and `NOTICE.txt` points only back at it, but the distribution **bundles** the SSALv1 C# plugin above | root `LICENSE.txt`, `README.md` and `NOTICE.txt` of `SonarSource/sonarqube` at `master`; the bundling read at `sonar-application/bundled_plugins.gradle` line 2, `bundledPlugin "org.sonarsource.dotnet:sonar-csharp-plugin"` | 2026-08-02 | Not taken, even for development-environment-only use |
| `dotnet-sonarscanner` 11.2.1 | n/a | LGPL-3.0, which section A allows only case-by-case with Architect and Legal approval | `licenses/LICENSE.txt` inside the distributed nupkg | 2026-08-02 | Not taken. Its own licence was never the obstacle; it is moot once the analyzer it feeds is not taken |

The four quoted verbatim details matter:

* **NBomber**, section 2.7: "NBomber is not free for organizational use. Any use by, for, or on
  behalf of an organization ... requires a valid Commercial Subscription." Its nuspec declares
  `<license type="file">` with `requireLicenseAcceptance`, never an SPDX expression, which is
  itself a signal worth checking on any package.
* **k6**, AGPL section 13, obliges a **modified** version interacting with users over a network
  to offer its source. Nami never modified k6, which is why the case was arguable rather than
  clear, and why it needed a decision rather than a parenthetical.
* **Gatling**: the trap was one directory below the root `LICENSE.txt`. A repository-metadata
  API reported the project as Apache-2.0 and was not wrong about the file it read.
* **SonarQube**: Gatling's shape again, and the reason it is worth a second entry is that
  "development environment only" looked like it disposed of the question and did not. The
  distribution bundles nineteen language plugins as of the read above, only the C# one was
  checked, and that one is SSALv1 while the root licence is LGPL-3.0. For a .NET project the C#
  plugin is the whole reason to run the server, so the permissive root licence describes the
  part nobody would use. Two habits follow. **Read the manifest that does the bundling**, here a
  build script in the distribution's own repository, because it is the only place the
  composition is stated and no licence tool parses it. And **an `execute-only` classification is
  a claim about a boundary, not about a licence**: it settles nothing until the composition of
  the thing being executed is known, which is a limit worth carrying back to the two rows in
  section 2 that currently hold that classification.

## 5. Verified alternatives, not yet taken

A package can be verified before it is needed, and recording that verification is worth more
than repeating it. The rows below are **not adopted**, and as of 2026-08-02 they are not
pending either: ADR-0060 decided Nami takes **no** assertion library, because xUnit v3's own
assertions already carry what the pick existed for. The rows stay because the reason to keep a
verification does not end when the answer is no. They make a later reversal cost a re-verify
rather than a research task, and they stop anyone re-deriving a licence this project has
already read. **The status column below means "verified, not taken" in that sense, not
"shortlisted".**

| Package | Version read | Licence | Read at | Date | Status |
|---|---|---|---|---|---|
| `AwesomeAssertions` | 9.5.0 | Apache-2.0 | `<license type="expression">Apache-2.0</license>` in the `.nuspec` inside the distributed nupkg (repository `github.com/AwesomeAssertions/AwesomeAssertions`, commit `bff44eb6`) | 2026-08-01 | Verified, not taken |
| `Shouldly` | 4.3.0 | BSD-3-Clause | `<license type="expression">BSD-3-Clause</license>` in the `.nuspec` inside the distributed nupkg (repository `github.com/shouldly/shouldly`, commit `cb48f40b`) | 2026-08-01 | Verified, not taken |

Two details are recorded because of what this project has already been caught by.

* **Both declare an SPDX expression, not a licence file.** NBomber in section 4 declared
  `<license type="file">`, which is what let a commercial licence sit behind a permissive
  assumption. An SPDX expression is machine-readable and is what the section C scan can act on,
  so the declaration *form* is itself a signal and is recorded here alongside the value.
* **`AwesomeAssertions` states an intent never to relicense**, in the README shipped inside the
  package: "The license will never change, not even to MIT. We will only maintain the original
  Apache 2.0 license." It also describes itself as "a fork of FluentAssertions controlled by the
  community", which is why it is the natural fallback for the package section 4 rejected. **A
  stated intent is not an assurance**, and it does not replace the re-verify-at-adopt-time rule
  below; it is recorded as a promise, with its wording, so a later reader can weigh it as one.

Apache-2.0 is inside ADR-0026 section A's permissive set ("MIT, Apache-2.0, BSD-2/3-Clause,
MS-PL, the PostgreSQL License, Unlicense/CC0"). This is worth stating because three documents
had narrowed the assertion-library constraint to "MIT or BSD", which would have excluded the
Apache-2.0 candidate above for no reason the policy gives. Those were corrected in the same
change as this section.

## 6. Maintenance rule

* A new dependency or tool is added to this file **in the same change** that introduces it, with
  the licence read at source and the date recorded. Not "verify later".
* A licence is never recorded from a badge, a repository-metadata API field, a package page
  summary, or another document in this repository. Open the licence text of the thing that is
  actually distributed.
* **For anything distributed as a bundle, read its composition before its licence** (added
  2026-08-02). A container image, a server distribution, or a release archive can carry modules
  under different licences than its root file, and section 1's third blind spot has now been hit
  twice on exactly that. So the read is two steps: find where the distribution declares what it
  bundles, usually a build manifest or an assembly descriptor rather than anything a scanner
  looks at, then read the licence of each bundled part that the intended use actually exercises.
  This is owed to both `execute-only` rows in section 2, which were classified on their root
  licences and whose bundled parts have not been enumerated; do it at adopt time, when the exact
  released version is known, and record the enumeration alongside the licence.
* Re-verify at adopt time. ADR-0026 already says a licence "can change again in either
  direction", and this project has now been wrong in both directions: a commercial package
  recorded as permissive, and a permissive package recorded as the wrong permissive licence.
* When code lands at M1, cross-check this file against `Directory.Packages.props` so
  completeness is measured against what is actually referenced rather than against prose. This
  is the same durable fix [ADR-0061](adr/0061-technology-stack-of-record.md) prescribes for the
  stack-of-record table, and for the same reason: two lists derived from prose can agree with
  each other while both omit the same thing.
