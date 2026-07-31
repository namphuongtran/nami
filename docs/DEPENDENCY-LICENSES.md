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
   same distribution is not. Badges and repository-metadata APIs read only the root file.

Each row below therefore records **where the licence was read and on what date**, not just the
licence name. A licence with no source is an assertion, and assertions are what went wrong.

## 2. External tools, outside the restore graph

These are executed as separate processes and are never compiled into, linked against, or
shipped inside any Nami artifact. Bundling any of them into a distribution artifact (the
reference host image, the Helm chart, the NuGet meta-package, the `dotnet new` template) would
change the question from execution to conveying, and would need a new decision.

| Tool | Role | Licence | Read at | Date | Decision |
|---|---|---|---|---|---|
| Apache JMeter | Load and soak testing, the SLO release gate | Apache-2.0 | `apache/jmeter` `master` `LICENSE` | 2026-08-01 | [ADR-0078](adr/0078-load-test-tooling.md) |
| OIDF conformance suite | OpenID certification profiles, self-hosted image | MIT | `openid/conformance-suite` `LICENSE.txt` (GitLab, `master`) | 2026-08-01 | [ADR-0027](adr/0027-packaging-and-distribution.md) |

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

Licences for the three above were verified at nuget.org on 2026-07-25 at versions 4.2.0 and
8.1.0. See ADR-0026 section E for the exact scope of that naming exception and what it does not
cover.

## 4. Rejected packages, with the evidence

Recorded because a forbidden-package list that will not say which package is forbidden cannot be
acted on (ADR-0026 section E), and because each of these was at some point recorded in this
repository as acceptable.

| Package | Claimed here as | Actually | Read at | Date | Outcome |
|---|---|---|---|---|---|
| NBomber | Apache-2.0 | **NBomber License Agreement v3.0**, commercial | `LICENSE` inside the NBomber 6.5.0 nupkg | 2026-08-01 | Removed, [ADR-0078](adr/0078-load-test-tooling.md) |
| k6 | AGPL, kept as "a dev-time tool, not a dependency" with no decision behind it | **AGPL-3.0**, section 13 present. Correctly identified, but the carve-out was never decided | `grafana/k6/LICENSE.md` | 2026-08-01 | Removed rather than carved out, [ADR-0078](adr/0078-load-test-tooling.md) |
| FluentAssertions | (already excluded) | Commercial from v8 | Excluded before this log existed | 2026-07 | Not taken; assertion library is an M1 pick under section A |
| Gatling | Apache-2.0, per repository metadata | Core Apache-2.0, **but the standard report module is proprietary**: "No code modification is authorised, no re-use of the code, no copying of all or any part of the code is allowed" | `license/LICENSE.gatling-highcharts.specific.txt` | 2026-08-01 | Not taken, [ADR-0078](adr/0078-load-test-tooling.md) |
| MediatR, AutoMapper, MassTransit | n/a | Moved to commercial licensing | ADR-0026 section A | 2026-07-04 | Forbidden by name, ADR-0026 |

The three quoted verbatim details matter:

* **NBomber**, section 2.7: "NBomber is not free for organizational use. Any use by, for, or on
  behalf of an organization ... requires a valid Commercial Subscription." Its nuspec declares
  `<license type="file">` with `requireLicenseAcceptance`, never an SPDX expression, which is
  itself a signal worth checking on any package.
* **k6**, AGPL section 13, obliges a **modified** version interacting with users over a network
  to offer its source. Nami never modified k6, which is why the case was arguable rather than
  clear, and why it needed a decision rather than a parenthetical.
* **Gatling**: the trap was one directory below the root `LICENSE.txt`. A repository-metadata
  API reported the project as Apache-2.0 and was not wrong about the file it read.

## 5. Maintenance rule

* A new dependency or tool is added to this file **in the same change** that introduces it, with
  the licence read at source and the date recorded. Not "verify later".
* A licence is never recorded from a badge, a repository-metadata API field, a package page
  summary, or another document in this repository. Open the licence text of the thing that is
  actually distributed.
* Re-verify at adopt time. ADR-0026 already says a licence "can change again in either
  direction", and this project has now been wrong in both directions: a commercial package
  recorded as permissive, and a permissive package recorded as the wrong permissive licence.
* When code lands at M1, cross-check this file against `Directory.Packages.props` so
  completeness is measured against what is actually referenced rather than against prose. This
  is the same durable fix [ADR-0061](adr/0061-technology-stack-of-record.md) prescribes for the
  stack-of-record table, and for the same reason: two lists derived from prose can agree with
  each other while both omit the same thing.
