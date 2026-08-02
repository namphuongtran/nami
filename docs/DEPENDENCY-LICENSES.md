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

| Tool | Version read | Role | Licence | Boundary | Read at | Date | Decision |
|---|---|---|---|---|---|---|---|
| Apache JMeter | **5.6.3** | Load and soak testing, the SLO release gate | Apache-2.0 at the root, over a bundle carrying thirteen further SPDX identifiers, enumerated in section 2.1 | `execute-only` | the `LICENSE` and `licenses/` tree inside the released `apache-jmeter-5.6.3.tgz` | 2026-08-02 | [ADR-0078](adr/0078-load-test-tooling.md) |
| OIDF conformance suite | **not pinned** | OpenID certification profiles, self-hosted image | MIT at the root, bundle not enumerated | `execute-only` | `openid/conformance-suite` `LICENSE.txt` (GitLab, `master`) | 2026-08-01 | [ADR-0027](adr/0027-packaging-and-distribution.md) |
| cosign (sigstore) | **not pinned** | Keyless signing and attestation of the image, the SBOM and the build provenance | Apache-2.0 at the root, bundle not enumerated | `execute-only` | `sigstore/cosign` `LICENSE`, default branch | 2026-08-02 | [ADR-0051](adr/0051-release-supply-chain-integrity.md) |
| CycloneDX for .NET | **not pinned** | The per-release SBOM | Apache-2.0 at the root, bundle not enumerated | `execute-only` | `CycloneDX/cyclonedx-dotnet` `LICENSE`, default branch | 2026-08-02 | [ADR-0026](adr/0026-dependency-license-policy.md) section C |

**The `Version read` column was added on 2026-08-02, and three of its four cells say `not
pinned` because that is true, not because the reading was lazy.** Section 6 states the rule
that a tool enters this section "in the change that first runs it, when there is a pinned
version to read a licence against instead of a default branch". That rule was written two
commits *after* two of the rows above were added (`d95acaf` after `91e47a6`, both on
2026-08-02), so it was never applied to the incumbents, and nothing in
`.github/workflows/ci.yml` runs any of the four today. The rows stay, because the check
ADR-0026 section C describes reads this table and a tool absent from it is the exact failure
this file exists for. What changes is that the table now distinguishes a licence read against
something that will actually be executed from one read against a branch that moves. **A row
with no version can be neither re-verified nor bundle-enumerated.** Until this change, section 5
was the only table in this file carrying a version column, and it is the one recording packages
this project deliberately did **not** adopt: the licences read most precisely were the ones
behind a decision to say no, while the inventory the boundary check actually reads had none.
Sections 3, 4 and 6 still have no version column either; that is worth less there, because none
of them carries a `Boundary` classification whose correctness depends on a composition.

The CycloneDX row is worth one sentence, because it is the blind spot wearing a disguise: it is
installed as a `dotnet tool`, so it *is* a NuGet package and still appears in no project's restore
graph. A tool that looks like a package is the case most likely to be assumed covered.

**The `Boundary` column is checked, not merely declared** (ADR-0026 section C). CI fails when a
tool classified `execute-only` appears in the file list of a published artifact, and it fails
when an executable used in the pipeline is **missing from this table**. The second direction is
the one that catches the case this whole file exists for: a tool nobody recorded is exactly
what the restore-graph scan reports clean on, so an inventory with no completeness check is
another control that reads as coverage while inspecting nothing. **All four rows above are
`execute-only`, and the classification is load-bearing rather than a formality**: section 2.1
enumerates one of the four bundles and finds nine components the permissive set does not cover,
every one of which is answerable as execution rather than conveying and by nothing else.

JMeter carries a second, structural assurance, and it is narrower than it reads. The ASF
third-party policy states that "Apache projects may not distribute Category X licensed
components, in source or binary form; in ASF source code or in convenience binaries" (read at
`apache.org/legal/resolved.html`, 2026-08-01), with GPL 1/2/3 and AGPL 3 named as Category X.
**The same policy expressly permits Category B**, read at the same source on 2026-08-02: "Any
Category B licensed works may be included in binary-only form in Apache Software Foundation
convenience binaries." Category B is where weak copyleft sits, and ADR-0026 section A is
stricter than Category X, so the ASF guarantee rules out precisely the class Nami forbids
outright and permits the class Nami routes through Legal. ADR-0078 stated a limit on this
argument, but the wrong one: it warned that a Category X component may be *relied on* during
development, and said nothing about Category B being *distributed* in the binary, which is what
section 2.1 found nine of.

### 2.1 What the JMeter bundle actually contains, enumerated at 5.6.3

Section 7's composition rule is owed to every `execute-only` row. This is the first row it has
been paid on, and it is paid against a pinned release rather than a branch. The enumeration cost
one archive, not a hundred lookups: `apache-jmeter-5.6.3.tgz` ships a `licenses/` tree carrying
the licence text of each bundled component under its group and version, and its root `LICENSE`
groups every component under an SPDX identifier. **Fourteen identifiers appear; six are inside
ADR-0026 section A's permissive set and eight are not.**

The split is by the identifier **as section A writes it**, and one case has to be called out or
the count is not reproducible. `MIT-0` is counted inside although section A names only `MIT`: the
text bundled here is headed "MIT No Attribution" and carries the grant and the warranty
disclaimer without the notice-retention clause, so it cannot be narrower than the licence section
A does name. `Apache-1.1` is counted outside on the same strict reading, and it is left outside
rather than waved through, because a name-based allow-list that starts accepting neighbouring
versions stops being checkable.

Two other places state part of the composition and neither is sufficient alone.
`src/dist/src/dist/expected_release_jars.csv`, read at tag `rel/v5.6.3`, lists **140 jars** and
is verified against the release archive by an upstream Gradle task described as "Verifies if
binary release archive contains the expected set of external jars"
(`src/dist/build.gradle.kts:185`), wired into `check` at `:276-278`. It covers jars only, while
the root `LICENSE` also declares bundled JavaScript, CSS and fonts used by the HTML report
(`bootstrap`, `jquery`, `datatables`, `flot`, `font-awesome`). **A jar manifest is not a
composition**: two of the nine components listed below appear nowhere in that CSV, checked by
searching it for each of the nine on 2026-08-02, and they are the two that are not jars,
`openiconlibrary` and `font-awesome-font`. Taking the machine-checked list for the whole would
have dropped exactly the `CC-BY-SA-3.0` and `OFL-1.1` findings, which is the more interesting
failure, since a manifest that upstream CI enforces is the one a reader is most likely to trust.

The nine components outside the permissive set, with the identifier quoted as the release's own
`LICENSE` writes it:

| SPDX, as the release declares it | Component | Where section A puts it |
|---|---|---|
| `MPL-2.0` | `net.sf.saxon:Saxon-HE:11.6`, `org.mozilla:rhino:1.7.14` | case-by-case, needing Architect and Legal approval |
| `CDDL-1.0 AND GPL-2.0-or-later WITH Classpath-exception-2.0` | `javax.mail:mail:1.5.0-b01` | the GPL leg is in the forbidden bucket; the classpath exception is not addressed |
| `EPL-1.0` | `junit:junit:4.13.2` | no bucket |
| `CDDL-1.0` | `com.sun.activation:javax.activation:1.2.0` | no bucket |
| `CC-BY-SA-3.0` | `openiconlibrary:openiconlibrary:` | no bucket |
| `OFL-1.1` | `font-awesome-font:font-awesome-font:4.2.0` | no bucket |
| `Apache-1.1` | `jcharts:jcharts:0.7.5` | no bucket; section A names Apache-2.0, and 1.1 is a different licence |
| Indiana University Extreme! Lab Software License | `io.github.x-stream:mxparser:1.2.2` | no bucket |

**There is no AGPL**, verified by searching the release's `LICENSE` and `NOTICE` for the word
Affero on 2026-08-02 and finding no occurrence. That is worth stating because AGPL is what
removed the previous load tool.

Three things follow, and the last is the one that changes what a person has to do.

* **The `execute-only` classification answers all nine, and now demonstrably rather than by
  assumption.** Nami runs an unmodified JMeter as a separate process against its own service and
  ships none of it, so each of the nine is answerable as execution rather than conveying, which
  is the same disposition section 6 records for OWASP ZAP. Section 4 already warned that an
  `execute-only` classification "settles nothing until the composition of the thing being
  executed is known". For this row the composition is now known.
* **Six of the nine fell into none of section A's three buckets, and that gap is now closed.**
  Section A allows a named set, routes MPL-2.0 and LGPL through Architect and Legal, and forbids
  commercial, viral copyleft and source-available. `EPL-1.0`, `CDDL-1.0`, `CC-BY-SA-3.0`,
  `OFL-1.1`, `Apache-1.1` and the Indiana University licence were in none of the three. ADR-0026
  gained two additions on the same date, and this row needed both. The **residual rule** gives
  silence a meaning: a licence in none of the lists means **the dependency is not taken**, and
  adding a name is an amendment to that ADR rather than a pull-request judgement. The **scope
  statement** is what keeps that rule from reading as a ban on JMeter: the lists classify what
  Nami *conveys*, and a component bundled inside an `execute-only` tool is answered by the
  boundary in section C. Without the first the six had no disposition; without the second the
  first would have reversed ADR-0078 by accident.
* **The reason recorded for dropping the previous load tool no longer describes this project's
  posture.** k6 was removed "rather than carved out" because it is AGPL, and the tool chosen in
  its place bundles a `GPL-2.0-or-later WITH Classpath-exception-2.0` leg plus four other
  copyleft licences, answerable only by the execute-versus-convey carve-out that ADR-0026
  section C does provide and does check. The decision is not inconsistent; the sentence claiming
  no copyleft question remained open was, and it is corrected in
  [`PRE-GA-RATIFICATION-CHECKLIST.md`](PRE-GA-RATIFICATION-CHECKLIST.md).

**This enumeration expires when the pinned version changes.** ADR-0078 already schedules the
read at M1; what it now inherits is a method and a baseline rather than a research task, and a
diff against this table is what the M1 read has to produce.

## 3. Named exceptions under ADR-0026

| Package | Licence | Reason it is an exception | Approver | Date |
|---|---|---|---|---|
| `Duende.AccessTokenManagement` | Apache-2.0 | Section E naming rule: recorded by its real package identifier even though the identifier carries the name of a vendor whose commercial products this project does not name. Concealing it would make the dependency record wrong and defeat the section C gate, which matches on exact package IDs. Published from the vendor's separate FOSS repository, not its commercial line. | Architect | 2026-07-25 |
| `Duende.AccessTokenManagement.OpenIdConnect` | Apache-2.0 | As above. | Architect | 2026-07-25 |
| `Duende.IdentityModel` | Apache-2.0 | As above; transitive through the two packages above. | Architect | 2026-07-25 |
| `Yarp.ReverseProxy` | MIT | Not an exception to the policy: recorded because it was named as a dependency in ADR-0029, ADR-0024, ADR-0061 and design [24](design/24-bff.md) with its licence asserted only in prose, which is blind spot 2 above. Read at the `.nuspec` (`<license type="expression">MIT</license>`, version 2.3.0, repository `github.com/dotnet/yarp`) rather than from a badge. | Architect | 2026-08-01 |
| `Microsoft.CodeAnalysis.PublicApiAnalyzers` **5.6.0** | MIT | Not an exception to the policy, and the same blind spot 2 as the row above: design [21](design/21-cicd-and-deployment.md) line 244 and design [01](design/01-foundations.md) line 427 both asserted MIT for it in prose, and neither is evidence. Read at the `.nuspec` fetched from the nuget.org flat container (`<license type="expression">MIT</license>`, `<repository … url="https://github.com/dotnet/roslyn" commit="c0573ed0a7dc3e3b4d2e70da47f97cc51a35524f" />`). Bundle composition read in the same step and it is not a bundle: the nuspec declares no `<dependencies>` element, and `obj/project.assets.json` after restore shows the target graph as the single node `Microsoft.CodeAnalysis.PublicApiAnalyzers/5.6.0`. | Architect | 2026-08-02 |

| `xunit.v3` **3.2.2** | Apache-2.0 | Not an exception. The test framework ADR-0060 binds the suite to. Read at its own `.nuspec` from the nuget.org flat container (`<license type="expression">Apache-2.0</license>`). | Architect | 2026-08-02 |
| `TngTech.ArchUnitNET.xUnitV3` **0.13.3** | Apache-2.0 | Not an exception. The architecture-test library ADR-0024 names, in its xUnit v3 integration variant. Read at its own `.nuspec` (`<license type="expression">Apache-2.0</license>`, `<repository … url="https://github.com/TNG/ArchUnitNET" commit="b25c4f940b1d067e97092783d0ef16e4fe12d8c3" />`). **The variant matters and no document in this repository chose it**: the plainly named `TngTech.ArchUnitNET.xUnit` at the same version declares `xunit.assert 2.4.1`, which is xUnit v2, while this one declares `xunit.v3.assert`. Both nuspecs read in the same step. | Architect | 2026-08-02 |

Licences for the first three rows were verified at nuget.org on 2026-07-25 at versions 4.2.0 and
8.1.0. See ADR-0026 section E for the exact scope of that naming exception and what it does not
cover.

**The analyzer row is the first entry in this file for a package this repository actually
compiles against**, added 2026-08-02 in the change that created `Directory.Packages.props`. Every
row above it records something decided, rejected, or asserted elsewhere; this one records a
restore that happens. Two consequences follow and neither is automatic yet. The section 7
maintenance rule and [ADR-0061](adr/0061-technology-stack-of-record.md)'s both defer the
completeness cross-check to "once code exists at M1", and the manifest they name now exists, so
that item moves from blocked to open. And the ADR-0026 section C licence-scan gate is still not
wired; when it is, note that a restore graph one node deep cannot demonstrate a scanner works,
so the gate needs a deliberate negative test rather than a green run.

### 3.1 The first restore graph worth enumerating, at 2026-08-02

The two test rows above are two `PackageReference` items and **twenty-three packages**. Read
from `tests/Nami.Identity.ArchitectureTests/obj/project.assets.json` after restore on
2026-08-02, the `net10.0` target holds twenty-four entries: the twenty-three below plus the
`Nami.Identity.Abstractions` project reference. Every licence here was read at that package's
own `.nuspec` on the nuget.org flat container on 2026-08-02, one request per package, and every
one carries an SPDX `<license type="expression">` rather than a licence file or a bare URL.

**Apache-2.0**, eleven: `TngTech.ArchUnitNET.xUnitV3` 0.13.3, `TngTech.ArchUnitNET` 0.13.3,
`xunit.v3` 3.2.2, `xunit.v3.assert` 3.2.2, `xunit.v3.common` 3.2.2, `xunit.v3.core.mtp-v1`
3.2.2, `xunit.v3.extensibility.core` 3.2.2, `xunit.v3.mtp-v1` 3.2.2, `xunit.v3.runner.common`
3.2.2, `xunit.v3.runner.inproc.console` 3.2.2, `xunit.analyzers` 1.27.0.

**MIT**, twelve: `CycleDetection` 2.0.0, `JetBrains.Annotations` 2025.2.2, `Mono.Cecil` 0.11.6,
`Newtonsoft.Json` 13.0.4, `System.ValueTuple` 4.6.1, `Microsoft.Testing.Platform` 1.9.1,
`Microsoft.Testing.Platform.MSBuild` 1.9.1, `Microsoft.Testing.Extensions.Telemetry` 1.9.1,
`Microsoft.Testing.Extensions.TrxReport.Abstractions` 1.9.1, `Microsoft.ApplicationInsights`
2.23.0, `Microsoft.Bcl.AsyncInterfaces` 6.0.0, `Microsoft.Win32.Registry` 5.0.0.

Both licences are on ADR-0026 section A's permissive list, so nothing here needs an exception.

**Two things this enumeration is worth more for than the verdict.** It is the first graph in
this repository that could exercise the section C licence-scan gate at all; the note above
about a one-node graph proving nothing about a scanner now has a counterpart to test against.
And it surfaced `Microsoft.ApplicationInsights`, which is in a test project's graph because
`Microsoft.Testing.Platform.MSBuild` auto-registers a telemetry extension rather than waiting
to be asked. Its licence is not the question; what it does is. The CI job disables it by
environment variable and says so at the job, and **no ADR rules on transmitting build
telemetry**, so that is a recorded choice rather than a decision being applied. That absence is
a claim about a search, so here is the search, run across `docs/adr/` on 2026-08-02:
`telemetry opt`, `OPTOUT`, `ApplicationInsights` and `Application Insights` return **nothing**;
`phone home` returns one line of ADR-0032; `opt-out` returns ADR-0032 plus ADR-0027, ADR-0043
and ADR-0091, and the last three are about response headers and startup switches rather than
telemetry. ADR-0032 is the only decision in range and it governs **Nami's own** opt-in
anonymous telemetry, not a build-time tool reporting to its vendor. What this search would miss
is an ADR discussing the idea without any of those spellings.

**ADR-0026 section D does not list this package**, which is not a defect in either document.
Section D is a list of packages confirmed permissive at the time it was written, not a gate;
the gate is section A, which classifies **licences**, and MIT is on its permissive list. This
is stated because section A's "not named above is not permitted" rule reads, at a glance, as
though it applied to package names.

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
  the thing being executed is known, which is a limit that applies to every `execute-only` row
  in section 2.

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

## 6. Pipeline scan tools

These rows were read on 2026-08-02 while
[ADR-0062](adr/0062-owasp-asvs-security-baseline.md)'s analyzer choice was still open, and the
reading is what closed it: a licence is an **input** to that choice rather than a consequence of
it, and two of the tools turned out not to be permissive before anyone weighed their merits.
[ADR-0092](adr/0092-ci-security-scan-tooling.md) then pinned each stage the same day, so the
`Status` column below records the outcome rather than a shortlist.

Nothing here has moved to section 2 yet, and that is deliberate rather than an oversight. Section
2 is the inventory ADR-0026 section C's second limb makes CI check for completeness, and its
subject is executables the pipeline actually runs. **A tool moves from this section to section 2
in the change that first runs it**, when there is a pinned version to read a licence against
instead of a default branch.

**That rule was written after the rows it would have held back, and saying so is cheaper than
letting a reader discover it.** All four of section 2's rows predate it, none is run by any
workflow that exists today, and until 2026-08-02 none carried a version at all, so applying the
rule literally would empty a table whose whole purpose is to be over-complete. The rule stands
for what comes next, section 2's `Version read` column records where each incumbent actually
stands, and **the promotion is what owes the bundle enumeration**: a row arriving here with a
pinned version and no composition read is the same unchecked classification in a newer table.

**One stage has no row here at all, and its absence is a decision rather than a gap.**
ADR-0092 section 6 covers GitHub Actions workflow definitions with two rules inside the
existing `scripts/check-adrs.sh` guardrail and takes **no tool**, so there is no licence to
read and nothing to inventory. A reader will think of one or two dedicated analysers that
could have gone here; **neither has had a licence read**, and neither is named in that ADR,
because a name with no reading behind it makes an open search look like finished research.
If the reversal condition in that section ever fires, the first step is a licence read at
source and a row in this table, before anything is chosen.

| Tool | Role in the pipeline | Licence | Read at | Date | Status |
|---|---|---|---|---|---|
| Trivy | Dependency scan and container scan | Apache-2.0 | `aquasecurity/trivy` `LICENSE`, default branch | 2026-08-02 | **Chosen for both stages** (ADR-0092) |
| Grype | Container scan, the alternative to Trivy | Apache-2.0 | `anchore/grype` `LICENSE`, default branch | 2026-08-02 | Verified alternative, not chosen |
| OWASP Dependency-Check | Dependency scan, the alternative to Trivy | Apache-2.0 | `dependency-check/DependencyCheck` `LICENSE.txt`, default branch | 2026-08-02 | Verified alternative, not chosen |
| gitleaks | Secret scan | MIT | `gitleaks/gitleaks` `LICENSE`, default branch | 2026-08-02 | **Chosen** (ADR-0092), which is also now its owning decision |
| OWASP ZAP | DAST pass against staging | Apache-2.0 at the root, but the distributed package bundles thirty third-party components and seven are outside section A's permissive set | `zaproxy/zaproxy` `LICENSE` and `LEGALNOTICE.md`, default branch | 2026-08-02 | **Chosen** (ADR-0092), answerable only as `execute-only` |
| Semgrep | SAST | **LGPL-2.1**, identically at the root `LICENSE` and at `cli/LICENSE` | `semgrep/semgrep`, `develop` | 2026-08-02 | Not chosen. Kept as ADR-0092's named reversal candidate; section A routes LGPL through the exception process with Legal |
| CodeQL, the query packs | The queries, not the thing that executes them | MIT | `github/codeql` `LICENSE`, default branch | 2026-08-02 | Verified, and not the artifact that matters |
| CodeQL CLI, the engine | SAST | **GitHub CodeQL Terms and Conditions**, a proprietary licence and not OSI-approved | `github/codeql-cli-binaries` `LICENSE.md`, default branch | 2026-08-02 | Not taken. Fails section A as a dual licence with a paid tier, and ADR-0092 rejects it on the host coupling as well |

Four details, quoted, because each one changes what the pending decision can choose from.

* **CodeQL is split across two repositories and only the one that does not execute is MIT.** The
  query repository's own `README.md` says so: "The CodeQL CLI (including the CodeQL engine) is
  hosted in a [different repository](https://github.com/github/codeql-cli-binaries) and is
  [licensed separately]". The CLI's terms grant CI use only "with an Open Source Codebase", and
  only "If the Open Source Codebase is hosted and maintained on GitHub.com"; the Restrictions
  section then forbids using it "in any other context ... during automated analysis, CI or CD"
  and "in connection with any codebase that is not an Open Source Codebase (e.g., code in a
  private repo in GitHub)", except where "your use of the Software is under a paid customer
  license for GitHub Advanced Security". **Nami today satisfies the free grant** and would not be
  in breach. The cost is not a breach, it is a coupling: adopting it binds the security gate to
  this project staying public on one host, and it gives an adopter who forks privately a gate
  they cannot run. That is a decision to take deliberately or not at all, and it is invisible if
  the only licence anyone reads is the MIT one on the query repository.
* **Semgrep declares LGPL-2.1 in both places it declares anything**, root and `cli/`, so there is
  no permissively-licensed subset to prefer. This does not forbid it: section A routes LGPL
  through the exception process rather than banning it, and an execute-only CI tool is the
  easiest version of that case. It does mean it cannot be adopted silently.
* **ZAP is the first thing the section 7 composition rule was applied to, and it earned the
  rule.** The root licence is Apache-2.0; `LEGALNOTICE.md` in the same repository lists thirty
  bundled components, of which `javahelp` (GPL with classpath exception), `jericho-html` (EPL /
  LGPL dual), `jfreechart` (LGPL), `jgrapht-core` (LGPL 2.1), `swingx-all` (LGPL 2.1), `xom`
  (LGPL) and `json-lib` (MIT plus the non-OSI "Good, Not Evil" clause) fall outside section A.
  Every one of them is answerable as execution rather than conveying, and not one is visible
  from the root licence file.
* **The four licences design 21 asserted in prose were all correct**, checked individually rather
  than as a group: CycloneDX Apache-2.0, cosign Apache-2.0, Trivy and Grype Apache-2.0, gitleaks
  MIT. Blind spot 2 did not fire. The instructive part is *where* the table stopped. The two
  tools it never listed, CodeQL and Semgrep, are the two that are not permissive, so a reader
  auditing only the recorded rows would have found nothing wrong and missed both. An inventory
  assembled from what someone already checked reports clean on precisely what nobody checked.

One item is left open rather than settled here, because it is a decision and this file does not
make them. **No ADR owns gitleaks.** Design 21's tool table fills its decision column with "this
doc", where every other row names an ADR, which puts a tool choice in the layer that realizes
decisions instead of the layer that makes them. It belongs to whatever closes ADR-0062's open
analyzer choice.

## 7. Maintenance rule

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
  Section 6 carries the first worked example, on OWASP ZAP, where the root licence is Apache-2.0
  and seven of thirty bundled components are not. This is owed to every `execute-only` row in
  section 2. **One of the four has been paid, at a pinned version, in section 2.1**, and it found
  nine components outside the permissive set behind an Apache-2.0 root. The other three are owed
  at promotion, when a version is pinned; **where each one declares its composition was located
  on 2026-08-02 so that the read is a read and not a search**, and the three are not the same
  shape:
  * **cosign** publishes an SBOM per released artifact as a release asset, verified at `v3.1.2`
    (`cosign-linux-amd64_3.1.2_linux_amd64.sbom.json`). The enumeration is a download.
  * **The OIDF conformance suite** declares its composition in **three** places, not one:
    `pom.xml` for the Java side, `package-lock.json` for the frontend, and a base image in its
    `Dockerfile` (`FROM eclipse-temurin:21`, read at `master`). A base image is itself a bundle,
    so this row nests.
  * **CycloneDX for .NET** declares its composition **nowhere a package reader would look**: the
    6.2.0 `.nuspec` carries `<license type="expression">Apache-2.0</license>` and **no
    `<dependencies>` element at all**, because a `dotnet tool` ships self-contained. The
    composition is the assembly set inside the package; at source it is the project's own
    `Directory.Packages.props`. This is the disguise the section 2 note describes, one layer
    deeper than that note goes.
* Re-verify at adopt time. ADR-0026 already says a licence "can change again in either
  direction", and this project has now been wrong in both directions: a commercial package
  recorded as permissive, and a permissive package recorded as the wrong permissive licence.
* When code lands at M1, cross-check this file against `Directory.Packages.props` so
  completeness is measured against what is actually referenced rather than against prose. This
  is the same durable fix [ADR-0061](adr/0061-technology-stack-of-record.md) prescribes for the
  stack-of-record table, and for the same reason: two lists derived from prose can agree with
  each other while both omit the same thing.
