---
status: "accepted"
date: 2026-08-03
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0021 parameter A (which named this gap and left it as a decision of its own), ADR-0026 (the licence policy whose scan reads the resolved graph, and which keeps the graph-visibility half of this problem), ADR-0092 (the security-scan family whose later stages are the likeliest origin of a future content-hash requirement), ADR-0030 (the SDK pin that first-party lock-file guidance would have constrained)
informed: all contributors, and any adopter building this repository
---

# Commit no package lock file, and hold the restore graph deterministic by forbidding floating versions and a second package source

## Context and Problem Statement

ADR-0021 parameter A named this gap and refused to settle it in passing. **Both quotations in
this section are dated readings rather than statements about the current tree**, because this
increment rewrites both passages: they were read in commit `6028b63`, the tree this ADR was
written against. In that tree `0021:41` read:

> **A bracket bounds the direct constraint and nothing beneath it**, so it does not make a
> restore reproducible: the versions OpenIddict's own dependencies resolve to are still
> whatever the graph allows on the day. Part C's suite can therefore pass over a graph that
> moved while every pin stayed put, and the same blind spot reaches ADR-0026, whose licence
> scan reads that graph. **No decision in this repository covers whole-graph determinism.**
> ... The candidate mechanism is a committed lock file with a locked-mode restore in CI, it
> is owed before M1, and it is a decision of its own rather than a corollary of this one.

Two things follow from that wording, and the second is the one that was nearly missed.
**What is owed is a decision**, so the item is closed by a record that covers whole-graph
determinism and not by the presence of a file. And the lock file is named as *the candidate
mechanism*, not as the answer, so **declining it closes the item exactly as legitimately as
adopting it**, on the single condition that the decline is recorded with what it costs. This
ADR is that record, and it declines.

The same open text was carried in a second place. In the same commit, `Directory.Packages.props`
stated it as "What a bracket still does not buy is a reproducible graph", closing with "That gap
is named in the same parameter, no decision in this repository covers it yet, and it is owed
before M1". **Both sites are stale from the moment this ADR is accepted**, and both are repaired
in this increment's second commit; at the commit carrying this ADR they still read as open, which
is why they are quoted in the past tense against a named commit rather than pointed at.

**Re-verified 2026-08-03, with the search written down, because an absence claim inherits
its search's blind spots.** Searched `docs/`, every tracked `*.props` and `*.targets`,
`.github/` and `scripts/` for nine spellings (`packages.lock.json`, `lock file`, `lockfile`,
`RestorePackagesWithLockFile`, `RestoreLockedMode`, `NU1608`, `NU1004`, `--locked-mode`,
`floating version`), case-insensitively: **two hits**, the `docs/BUILD-PLAN.md` row that owes
this decision and ADR-0021 parameter A itself. Nothing in the tree implements the mechanism
and nothing decides against it.

**That search has a demonstrated blind spot, and demonstrating it is worth more than the
count.** `Directory.Packages.props` carried the same open item in the same words and is **not**
among the two hits, because its paragraph says "reproducible graph" and never spells any of
the nine. It was found by reading the file, not by searching for the mechanism, which is the
general shape: a search for the *candidate* misses a site that describes the *problem*. What
this search would still miss is any document discussing reproducible restore in vocabulary
none of the nine words touches.

**The question is decidable now for a reason that is a property of the date rather than of
the argument.** `Directory.Packages.props` declares three package versions, all fixed, and
`docs/DEPENDENCY-LICENSES.md` section 3.1 enumerates the graph they produce: two
`PackageReference` items in the architecture-test project resolve to twenty-three packages,
read from that project's `obj/project.assets.json` on 2026-08-02. So the graph is finally
large enough that a claim about it means something, and small enough that the cost of every
option can be measured rather than argued.

## Decision Drivers

* **A restore should resolve the same closure twice.** That is the whole of what parameter A
  asked for, and today nothing in the repository asserts it beyond the direct edge.
* **Supply-chain integrity, stated as a threat rather than as a virtue.** The question is
  whether a package's *content* can change under a fixed version string, and whether a
  transitive addition can arrive unseen.
* **ADR-0026's licence policy reaches transitive packages, and its gate reads the resolved
  graph.** Section B says plainly that "the policy applies to transitive dependencies too,
  not only direct ones", and section C's gate "reads the license of every package (direct and
  transitive) from the restore graph". A graph that moves is therefore a licence problem and
  not only a build problem.
* **Cost proportional to what is being protected.** Three direct rows and one solution of two
  projects. A mechanism whose maintenance exceeds the graph it guards is the wrong mechanism
  at this size, and the size is the thing that will change.
* **A gate that can go quiet must have a test for the silence.** The root `CLAUDE.md` states
  it as a rule for adding a gate: "ask what would have to break for it to go quiet, then
  write that break down as a test". Anything adopted here is judged including that cost.

## Considered Options

* **A. Commit a lock file.** `RestorePackagesWithLockFile` set repository-wide, a
  `packages.lock.json` checked in per project, and a locked-mode restore in CI.
* **B. Decline the lock file, and make the premises it would have protected into binding
  rules.** Fixed versions only, and one package source, each enforced by the guardrail that
  already gates every commit.
* **C. A graph-diff gate instead.** Fail CI when the resolved package set differs from the
  hand-maintained enumeration in `docs/DEPENDENCY-LICENSES.md` section 3.1.

## Decision Outcome

Chosen option: **B**. The seven parameters below are binding. The seventh is the Confirmation
section, which MADR places after the consequences, so it appears last rather than in sequence.

The reasoning is one sentence long and the rest of this ADR is its evidence. NuGet
"tries to always produce the same full closure of package dependencies if the input
PackageReference list has not changed", and names exactly three cases where it cannot; two of
those three are absent from this repository today by circumstance, so this decision converts
them from circumstances into rules, and the third does not apply to nuget.org. What that
leaves uncovered is real, is not covered by the rules, and is recorded in parameter D as
accepted risk rather than argued away.

### A. No `packages.lock.json` is committed, and `RestorePackagesWithLockFile` is not set

Both halves, because there is no middle position and the reason is mechanical rather than
stylistic. **Committing the file is itself the opt-in.** First-party documentation states it
without qualification: "Once a project has `packages.lock.json` file in its root directory,
the lock file is always used with restore even if the property `RestorePackagesWithLockFile`
is not set. So another way to opt-in to this feature is to create a dummy blank
`packages.lock.json` file in the project's root directory."

So a lock file present "for reference" is a lock file in force, and the apparently cautious
combination of a committed file with the property left at `false` is not a half measure but
an error: `NU1005` reports "Invalid restore input where RestorePackagesWithLockFile property
is set to false but a packages lock file exists", and its own explanation is that "There are
2 opt-in methods the lock file functionality, by setting the RestorePackagesWithLockFile
property, or create a packages.lock.json next to the project file, and they are conflicting."

This parameter is therefore a rule about a **file's existence**, which is what makes it
reviewable with no instrument (see parameter G).

### B. No floating version, in any `*.props`, `*.targets` or `*.csproj`

Today's incidental state, made binding, because parameter A's whole argument rests on it. A
floating version is the first of the three cases in which NuGet cannot reproduce a closure,
and unlike the other two it is entirely within this repository's control: it is a character
somebody types.

**The scope is build files, not prose, and that boundary is deliberate rather than an
oversight.** A rule that read markdown would forbid its own explanation, because this ADR
quotes the documented example of a floating version, `Version="4.*"`, in order to say what is
being forbidden. The same is true of `Directory.Packages.props`, whose comment has to be able
to describe the form it does not use. Enforcement therefore reads the build files and nothing
else.

What this parameter does **not** do is make a version an exact pin. A plain `Version="X"`
restores as the constraint `>= X`, which ADR-0021 parameter A settled for OpenIddict as the
bracket form and deliberately did not impose on every row. This parameter forbids the
wildcard; it leaves the floor-versus-bracket question exactly where ADR-0021 left it, and a
file mixing the two forms remains correct.

### C. nuget.org is the only package source

The third of the three cases is a package version being removed from the repository, and the
documentation answers it by name: "Though nuget.org does not allow package deletions, not all
package repositories have this constraint." That sentence is why this parameter exists.
Adding a second source removes the ground under parameter A's argument, so it is a **revisit
trigger under parameter F rather than a configuration change**, and the person adding it owes
this decision a re-reading rather than a line in a config file.

**What is and is not established here, because the two are easy to conflate.** No
`NuGet.config` is tracked anywhere in the tree, measured 2026-08-03 with
`find . -iname 'nuget.config'`, which returned nothing. That establishes that **the
repository declares no source**, and therefore declares no second source. It does **not**
establish that a restore on any given machine reads only nuget.org: with no repository-level
configuration, the effective source list is the user-level and machine-level one, which this
repository does not pin. The parameter binds what the repository does, and the residual is
named in parameter D.

**First-party guidance recommends the opposite of an empty configuration, and pointing that
out is part of taking this parameter honestly.** The NuGet security-best-practices page says
"Add a `nuget.config` file in the root of your project repository. This is considered a best
practice as it promotes repeatability and ensures that different users have the same NuGet
configuration", with a `<clear />` element and nuget.org as the only source. That file would
*strengthen* this parameter rather than weaken it, by making the machine's configuration
irrelevant. It is not taken today because nothing in the tree needs it yet and because the
enforcement in parameter G reads a filename; adopting it is a decision available at any time
and is listed under parameter F as the thing whose arrival changes that enforcement.

### D. Residual risk, named rather than implied

Three items. They are what this decision costs, and none of them is mitigated elsewhere in
this ADR.

1. **No content-hash verification of restored package content.** This is the benefit the
   declined mechanism actually provides, in its own words: "Lock files store the hash of your
   package's content. If the content hash of a package you want to install matches with the
   lock file, it will ensure package repeatability." Nothing else in this repository provides
   it. An exact version bounds a *version string*; it says nothing about the bytes behind it,
   and parameters B and C do not change that.
2. **A transitive graph change is not a reviewable diff.** A direct version bump can pull in a
   new transitive package, or move an existing one, and no artifact in the repository records
   it, so nothing appears in a pull request for a reviewer to look at. The enumeration in
   `docs/DEPENDENCY-LICENSES.md` section 3.1 is a dated reading rather than a gate, and
   parameter E says whose problem this is.
3. **Unlisting behaviour is not verified, and it is left marked unverified rather than
   repeated.** `Directory.Packages.props` asserts that "a yanked or unlisted 5.6.0 would float
   this forward silently". The source read for this ADR covers a package version being
   **removed from the repository**, which nuget.org does not permit, and says nothing about a
   version that is *unlisted* there and still restorable by exact reference. So the claim in
   that comment is not established by anything this repository has read, and this ADR neither
   repeats it as fact nor contradicts it. The risk is accepted in that state: if unlisting does
   move a resolution, parameter B and this parameter do not stop it.

### E. The licence-graph half stays with ADR-0026

Residual risk 2 is not solved here, and this ADR must not read as though it is. ADR-0026
section C owns it: a CI licence-scan gate that "reads the license of every package (direct
and transitive) from the restore graph and fails the build if any license falls outside the
allow-list", still owed at M1 per `docs/BUILD-PLAN.md`. That gate reads the graph at the
moment it runs, which is the same graph this ADR declines to freeze, so the two decisions are
adjacent and not overlapping: this one bounds how the graph can move, and that one inspects
where it has moved to.

`docs/DEPENDENCY-LICENSES.md` gains nothing from this ADR, deliberately. Its section 3
enumeration is unaffected by a decision not to lock.

### F. Revisit triggers

Any one of these reopens this decision, and each is written so that the person who trips it
can recognise it without judgement:

* **A second package source**, by any route: a tracked `NuGet.config` that adds one, a
  command-line source, or a private feed. Parameter C's premise is gone at that point.
* **Any floating version**, in any build file, wanted for any reason.
* **A requirement for content-hash verification or a reproducible-build attestation.** The
  likeliest origin is the CI security-scan family in ADR-0092, whose stage 2 dependency scan
  is the one gate there that reads the restored graph. **ADR-0092 does not state such a
  requirement today**, searched on 2026-08-03 across that file for `attest`, `provenance`,
  `SBOM`, `content hash`, `reproducib`, and `supply chain` in **both** the spaced and the
  hyphenated spelling, case-insensitively: one hit, the hyphenated "supply-chain defect"
  describing workflow expressions. **The hyphen is why both spellings are listed rather than
  one.** Re-run on 2026-08-03, the spaced form alone returns zero in that file, so a search
  stated as `supply chain` would have reported a clean absence while the term was in the file.
  That is the failure `docs/CLAUDE.md` records against the unhyphenated spelling of
  Content Security Policy, reproduced here at the cost of one spelling. The nearest live commitments
  are elsewhere and neither asks for a lock file: ADR-0026 section C generates "An SBOM
  (CycloneDX) ... for license and supply-chain audit", and ADR-0030 owes "MinVer with the
  reproducible-build stack".
* **The dependency graph outgrowing hand-enumeration** in `docs/DEPENDENCY-LICENSES.md`
  section 3. Residual risk 2 is tolerable partly because a transitive change is still findable
  by a person reading a twenty-three-row list against a restore, and that stops being true at
  some size nobody can name in advance. The nearest thing to a marker already exists: that
  file's section 7 defers the completeness cross-check against `Directory.Packages.props` to
  when code lands at M1, on the ground that "two lists derived from prose can agree with each
  other while both omit the same thing". When that cross-check is written, this decision is due
  a re-reading in the same change.

### Consequences

* Good, because the two premises that were true by accident are now true by rule, so a
  future reader can tell an intentional property of this repository from a coincidence. That
  is the substance of what parameter A asked for: whole-graph determinism is now covered by a
  decision, which is what was owed.
* Good, because the cost is proportional and stays proportional. Nothing has to be
  regenerated on a bump, no second artifact can disagree with the manifest, and no gate
  exists that could go quiet.
* Good, because it avoids taking on the SDK-pin friction the mechanism carries. See More
  Information: first-party guidance asks for an exact SDK version with `rollForward` set to
  `disable` alongside lock files, and both of this repository's SDK inputs are deliberately
  looser than that. **This is a cost avoided, not a reason to decline**, and the distinction
  matters: it would have been payable, and if the decision is ever reversed it becomes
  payable again.
* Bad, and this is the real price: **nothing verifies the content hash of a restored
  package**, so a package whose bytes change under a fixed version string is not detected
  here. Accepted as parameter D.1 and routed to the Pre-GA checklist rather than left in an
  ADR nobody re-reads.
* Bad, because **a transitive graph change is invisible in review**. Accepted as parameter
  D.2, owned by ADR-0026 section C's gate at M1, and this is the residual most likely to be
  the reason for a reversal.
* Bad, because two of the three parameters are now rules a contributor can trip while doing
  something reasonable, in particular anyone who reaches for a floating version to test an
  upgrade. The remedy is a temporary local edit that CI refuses, which is intended, but it is
  friction that did not exist yesterday.
* Neutral, because nothing in the build changes. No property is set, no file is added to any
  project, and `dotnet restore` resolves exactly what it resolved before this ADR was accepted.

### G. Confirmation, and what it does not cover

* **Parameters B and C are enforced by two rules in `scripts/check-adrs.sh`, numbered
  Check 9, with planted-violation coverage in `scripts/test-check-adrs.sh`.** Rule 9a rejects
  a `Version` attribute whose value contains `*` in any tracked `*.props`, `*.targets` or
  `*.csproj`; rule 9b rejects a tracked file named `NuGet.config`, case-insensitively.
* **At the commit carrying this ADR those rules do not exist yet.** They land in this
  increment's second commit, on 2026-08-03. This is stated rather than glossed because a
  Confirmation section that describes a gate as running is a measurement, and this one would
  have been false by one commit. Parameter A of this ADR is what makes the sequence safe: the
  decision binds from acceptance whether or not an instrument is watching.
* **Both rules have a real subject today, which is what keeps them from being inert.** Three
  fixed version rows and zero configuration files. A check whose subject cannot produce a
  violation is green whether or not it is armed, which is the defect four of this
  repository's nine gates exist to catch.
* **Rule 9b reads a filename, so it cannot tell a configuration that adds a source from one
  that removes every source but nuget.org.** Those are opposite in effect and the second is
  the first-party recommendation quoted in parameter C. Stating the scope is part of the
  coverage claim: if the `<clear />` form is ever wanted, rule 9b is the thing that has to
  change, and the change is a narrowing of the rule rather than a retraction of parameter C.
* **A source added on the command line, in a user-level `NuGet.config`, or in a machine-level
  one is outside both rules entirely.** So is a package whose content changes under a fixed
  version, which is parameter D.1 and has no instrument at all here.
* **Parameter A needs no check, and that is an argument rather than an omission.** A committed
  lock file is a tracked file: it appears in a pull request as an added file, and it cannot be
  added silently. Should the two opt-in routes ever disagree, `NU1005` fails restore, so the
  incoherent state is not reachable in a green build. A check would restate what the diff and
  the restore already say.
* **Standing obligation, with a trigger rather than a date.** Re-read this decision at M1,
  when ADR-0026 section C's licence-scan gate lands, because parameter E defers residual risk
  2 to that gate existing. If M1 arrives without it, D.2 is an unowned gap rather than a
  division of labour, and the reasoning has to be re-taken. The Pre-GA checklist carries the
  ratification.

## Pros and Cons of the Options

### A. Commit a lock file with a locked-mode CI restore

* Good, and decisively on the merits, because it is the only option of the three that
  provides content-hash verification. It answers parameter D.1, which nothing else here does.
* Good, because it turns a transitive graph change into a reviewable diff, which is
  parameter D.2. That is the second thing the decline gives up and this option simply has it.
* Good, because locked mode is documented for exactly this purpose: "Enables locked mode for
  restore. This is useful in CI/CD scenarios where you want repeatable builds."
* Bad, because **the mechanism is inert in two states that look like success**, both measured
  and both in More Information. Locked mode with no lock file present exits 0 and validates
  nothing, and a warm no-op restore over a lock file edited out of agreement also exits 0.
  Adopting it would therefore have required a self-test of its own to prove it had not gone
  quiet, which is real cost on the adopt side of the ledger rather than a defect in NuGet.
* Bad, because it brings the SDK-pin friction in More Information with it, against a
  `global.json` and a CI SDK input that are both deliberately looser than the advice asks for.
* Bad, because for the projects that exist today the guidance itself is equivocal. Its rule is
  to check the file in for "an application, an executable, and the project in question is at
  the start of the dependency chain", and **not** to check it in for "a library project that
  you do not ship or a common code project on which other projects depend". `src/` currently
  holds one library and `tests/` one test project, so the repository has no case that is
  unambiguously on the check-it-in side of that line.

### B. Decline the lock file and make its premises binding (chosen)

* Good, because it closes what was actually owed. Parameter A owed a decision covering
  whole-graph determinism, and a recorded decline with its residual risk named is a decision.
* Good, because the two premises the argument depends on stop being circumstances and become
  rules with an instrument behind them, which is strictly more than the repository had
  yesterday.
* Good, because it adds nothing that can rot: no generated artifact, no second source of
  truth about versions, and no gate that is green when disarmed.
* Bad, because it accepts parameter D.1 and D.2 rather than solving them, and D.1 has no
  owner anywhere in the repository. Option A would have had one.
* Bad, because its correctness is contingent on facts about the graph's size and shape that
  will change. Parameter F is the whole mitigation, and a revisit trigger is a promise that a
  future reader keeps rather than a mechanism.

### C. A graph-diff gate against the enumerated package list

* Good, because it targets residual risk 2 directly and cheaply, and the enumeration it would
  compare against already exists in `docs/DEPENDENCY-LICENSES.md` section 3.1.
* Bad, because it is a lock file with none of a lock file's advantages: a second,
  hand-maintained statement of the resolved graph, kept in a markdown table, with no content
  hashes and no tooling that understands it. The failure mode is a stale table that nobody
  updates and a gate that gets disabled to unblock a bump.
* Bad, because that enumeration is a **dated measurement** of one project's graph, taken on
  2026-08-02, and section 7 of that file points the durable completeness check at
  `Directory.Packages.props` rather than at the table, deferred to M1. Turning a dated reading
  into a gate's expected value is how it gets edited to match today, which the root
  `CLAUDE.md` names as the thing that stops it being evidence.
* Bad, because it covers one project. The graph enumerated is the architecture-test project's,
  and the gate would say nothing about any project added later until somebody remembered it.

## More Information

### Sources read at source, 2026-08-03

Four reads. Every quotation in this ADR comes from one of them.

* **Microsoft Learn, `nuget/consume-packages/package-references-in-project-files`, section
  "Locking dependencies".** The premise and the three exceptions. NuGet "tries to always
  produce the same full closure of package dependencies if the input PackageReference list has
  not changed", and the page lists three cases where it is unable to. This is also the source
  for the always-used-if-present behaviour in parameter A, for the locked-mode purpose quoted
  under option A, and for the check-it-in-or-not guidance quoted under option A.
* **Microsoft Learn, `nuget/reference/errors-and-warnings/nu1005`.** The two-opt-in-routes
  conflict quoted in parameter A. **This source is not in the design spec for this increment,
  which asserted `NU1005` without a citation**; it was read to settle whether the assertion was
  true, and it is.
* **Microsoft Learn, `nuget/concepts/security-best-practices`.** The content-hash benefit
  quoted in parameter D.1, under its "Lock files" heading, and the `nuget.config` with
  `<clear />` recommendation quoted in parameter C. Both are on the same page, and quoting only
  the first would have been a selective read: one argues for the declined mechanism and the
  other argues for strengthening a parameter this ADR takes.
* **Microsoft Learn, `azure/devops/pipelines/artifacts/caching-nuget`.** The SDK-pin friction
  below.

The three cases the first source names, each against the state of this repository on
2026-08-03:

| Case, as the source states it | State here |
|---|---|
| Floating versions, its example being `<PackageReference Include="My.Sample.Lib" Version="4.*"/>` | None. Three rows in `Directory.Packages.props`, all fixed. Parameter B makes this a rule |
| A newer version matching the requirement is published: asked for 4.0.0 when only 4.1.0 and later existed, so 4.1.0 resolved as the nearest minimum; 4.0.0 is published later and now resolves instead | Requires a declared floor below every published version. Rows here are written from a version read off nuget.org, so the window is already closed when the row is written |
| "A given package version is removed from the repository. Though nuget.org does not allow package deletions, not all package repositories have this constraint" | nuget.org only, and no `NuGet.config` exists in the tree. Parameter C makes this a rule |

### The SDK-pin cost this decline avoids

Recorded as a cost avoided rather than as a reason to decline, because it is a consequence of
the mechanism and not an argument against determinism. The Azure Pipelines NuGet caching page
states: "If you use package lock files, consider specifying an exact .NET SDK version in a
`global.json` file and setting `rollForward` to `disable`. This can help prevent locked
restore failures when implicit dependencies vary across .NET SDK versions."

Neither of this repository's two SDK inputs is that shape, and both are deliberate.
`global.json` pins `"version": "10.0.100"` with `"rollForward": "latestFeature"`, which
ADR-0030 records as a parseable floor chosen over the corpus's inert wildcard form, and
`.github/workflows/ci.yml:72` installs `dotnet-version: "10.0.x"`, left as it was with a
comment saying why. Adopting lock files would have put both against first-party advice, or
required tightening them for a reason unrelated to why they were chosen. `NuGet/Home` issue
13344 tracks locked mode against `global.json` upstream.

### Measurements, 2026-08-03, .NET SDK 10.0.301 (`dotnet --version`), macOS

Every figure here is a measurement with a date. Re-run it rather than cite it forward. Each
was taken against this tree with the mechanism temporarily enabled, then reverted;
`git status` was clean afterwards. They are in this ADR because a decline supported by
measurement is evidence and a decline supported by preference is not.

| Setup | Command | Result |
|---|---|---|
| `RestorePackagesWithLockFile=true`, no lock file present | `dotnet restore` | exit 0; lock files generated, `Abstractions` 1 resolved entry, `ArchitectureTests` **23**, matching the twenty-three that `docs/DEPENDENCY-LICENSES.md` section 3.1 enumerates |
| lock files present and matching, `RestoreLockedMode=true` | restore | exit 0 |
| lock files **deleted**, `RestoreLockedMode=true` alone | restore | **exit 0, nothing created and nothing validated** |
| lock files deleted, both properties | restore | **exit 0, lock files silently regenerated** |
| a resolved version hand-edited from 5.6.0 to 5.5.0, warm `obj/` | restore | **exit 0, a no-op restore: the lock file is left edited and `project.assets.json` still says 5.6.0** |
| the same edit, `--force` | restore | exit 1, `NU1102`, worded "Unable to find package ... (>= 5.6.0)" while also reporting "Nearest version: 5.6.0" |
| `Directory.Packages.props` xunit.v3 moved 3.2.2 to 3.2.1, lock file untouched, `obj/` deleted | restore | exit 1, `NU1004` |
| the same drift, both properties passed as environment variables | `dotnet build` | exit 1, `NU1004` |
| the same drift | `dotnet format --verify-no-changes` | exit 1, `Unhandled exception: System.Exception: Restore operation failed.` with a stack trace and no `NU` code |

**Rows three and five are the two that argue about the mechanism's ergonomics rather than
about this decision's convenience, and they are why they are kept.** Locked mode with no lock
file exits 0 and validates nothing, so the strict flag is not self-enforcing: it is armed only
by the presence of the artifact it checks. And a warm no-op restore over a lock file that no
longer agrees with `project.assets.json` also exits 0, so the drift survives a restore that
reported success. Neither is a defect in NuGet, both are documented behaviour, and together
they mean that adopting this mechanism would have obliged this repository to write a self-test
proving its own gate had not gone quiet, in the shape four of its nine existing gates already
take. That cost belongs on the adopt side of the ledger and is counted there.

The last row is not about lock files and is kept for a different reason: it is the one place
where a restore failure surfaces as an unhandled exception with no diagnostic code, which is
worth knowing before somebody meets it during a bump.

### Related decisions, and what this ADR does not touch

* **No `stack-record: true` marker, and no row in ADR-0061, deliberately.** This ADR
  introduces no technology; it declines one mechanism of a package manager the project already
  uses. The precedent is immediate and adjacent: ADR-0093 and ADR-0094 are both mechanism ADRs
  from 2026-08-03 and neither carries the marker. This is said out loud because an absent
  marker otherwise reads as an oversight, and because adding one without a table row fails
  guardrail Check 4 in the other direction.
* Related decisions: ADR-0021 (parameter A, which owed this decision and whose bracket form
  this parameter B deliberately does not extend), ADR-0026 (sections B and C, the licence
  policy that reaches transitive packages and the gate that reads the resolved graph, which
  keeps residual risk 2), ADR-0030 (the SDK pin and the roll-forward form that lock-file
  guidance would have constrained), ADR-0092 (the CI security-scan family, named in parameter
  F as the likeliest origin of a future content-hash requirement and confirmed there not to
  state one today), and ADR-0093 and ADR-0094 (the two sibling mechanism decisions of the same
  date, and the precedent for carrying no stack-record marker).
* **The `docs/BUILD-PLAN.md` row that owed this decision is closed by this ADR**, under that
  file's own maintenance rule that "a row is deleted when its owner records the outcome" and is
  never marked done there. The row was line 51 of that file at this ADR's commit, reading
  "Whole-graph restore determinism (committed lock file, locked-mode CI restore)" with
  ADR-0021 as its owner and "Before M1" as its trigger. The deletion lands with this
  increment's second commit rather than with this one, because until the guardrail rules exist
  the outcome is decided and not yet fully recorded.
* **Deliberately out of scope, so that silence here is not read as a position.** Package source
  mapping and signed-package client trust policies are both named on the NuGet
  security-best-practices page read above, and neither is decided here. No change is made to
  `global.json` or to the CI SDK input: the lock-file-versus-SDK-pin friction is a cost
  recorded in this ADR, not a change it makes. And the gate count in the root `CLAUDE.md`
  stays at nine, because Check 9 is a rule inside an existing gate rather than a new gate.
* Authored fresh for this repository, not imported from the design corpus. The obligation came
  from ADR-0021 parameter A; the parameters, the four source reads, and every measurement above
  are this repository's.
