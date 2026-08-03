---
status: "accepted"
date: 2026-08-03
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0065 (which scopes itself to two style and naming diagnostics and therefore does not reach the CA quality rules), ADR-0092 (the security axis, the sibling that owns the third axis), ADR-0093 (the escalation this breadth feeds), ADR-0030 (the runtime version that determines which analyzer set ships)
informed: all contributors, and any adopter building this repository
---

# Run the SDK code-quality analyzers at Recommended, and reject All on the evidence of the one rule it adds today

## Context and Problem Statement

The .NET SDK's analyzers are configured along three axes, and until this ADR only two of them
had an owner.

**Style and naming belong to ADR-0065**, and that ADR is explicit about how far it reaches:
its agreed core "is exactly two diagnostics: `IDE1006` for naming and `IDE0055` for layout,
both auto-fixable by `dotnet format`" (`0065:41`). Two named `IDE` diagnostics is a scope, not
an omission, and the sentence says so.

**Security belongs to ADR-0092**, whose section 1 is titled "SAST: the .NET SDK's security
analysis axis, and no third-party tool" (`0092:69`) and which reads `AnalysisLevelSecurity` as
"a property separate from the ordinary `AnalysisLevel`" (`0092:72-73`).

**Code quality, the `CA` rules that are neither style nor naming nor security, belonged to
nobody.** That is a claim about a search, so here is the search. Every file under `docs/adr/`
was scanned with `grep -rIl` for each of `AnalysisMode`, `AnalysisLevel`,
`EnableNETAnalyzers`, `NetAnalyzers`, `code-quality`, `code quality`, `quality rules`, `CA1`,
`CA2` and `Recommended` on 2026-08-03. `AnalysisLevel` hits only `0061` and `0092`, and in both
it is the security spelling. `NetAnalyzers` hits only `0092`, as the analyzer assembly's name.
`code-quality` hits only `0026`, the dependency-licence policy, which is a different subject.
The remaining seven spellings, including `AnalysisMode` and every `CA1`/`CA2` rule number,
return no file at all.

ADR-0093 makes that gap load-bearing rather than tidy. It flips the default for an unnamed
warning from pass to fail, and it deliberately left this axis open: "The analyzer breadth that
decides how many diagnostics this gate reads is not decided here. This ADR fixes what happens
to a warning, not which rules produce one" (`0093:234-235`). The two questions are now the two
halves of one gate, and only one half has been answered.

So the question is not whether quality analyzers should run. It is which of the three settings
the breadth knob offers this repository takes, decided while `src/` is small enough that the
answer can be measured instead of estimated.

## Decision Drivers

* **An axis configured by nobody is not a neutral choice, it is an unrecorded one.** Whatever
  the SDK does by default is in force either way; the only question is whether the project can
  say why.
* **Breadth fails differently from severity, so it is decided differently.** ADR-0093 names the
  distinction: its own parameter "is measured at zero cost today and changes nothing until a
  warning appears, while a breadth change can turn existing, unchanged code red on the day it
  lands" (`0093:237-239`). A breadth decision therefore has to be taken against the code that
  exists, not against the code that is imagined.
* **Take the strictest setting the evidence supports, and no stricter.** Pre-alpha is when a
  ruleset is cheapest to adopt, which argues for reaching as far up as the measurement allows,
  and it is also when a single type can distort the answer, which argues against reading one
  finding as a mandate.
* **A rule that would be carved out on the day it is adopted has not been adopted.** A gate
  whose first act is to exempt itself teaches the exemption, which is the pressure ADR-0065
  predicted for M1 as "the pressure to weaken a rule rather than fix the code that trips it"
  (`0065:109`).
* **The implementer source of record outranks a rule's preference about a signature.** A member
  transcribed from a design is not free to change shape because an analyzer would prefer a
  different type.

## Considered Options

* **A. `<AnalysisMode>Recommended</AnalysisMode>`** in `Directory.Build.props`, repo-wide.
* **B. Leave `AnalysisMode` unset**, taking whatever the SDK applies.
* **C. `<AnalysisMode>All</AnalysisMode>`**, the widest setting the property accepts.

## Decision Outcome

Chosen option: **A**. The three parameters below are binding.

### A. `AnalysisMode` is `Recommended`, repo-wide, in `Directory.Build.props`

```xml
<AnalysisMode>Recommended</AnalysisMode>
```

It goes in `Directory.Build.props` so it is inherited by every project, and it applies to a
contributor's `dotnet build` exactly as it applies to CI. That is the same shape and the same
reason as ADR-0093 parameter A, which rejected the CI-only form on ADR-0065's measured finding
that a divergent local build leaves "the cost lands on contributors, whose local build goes
quiet while CI does not" (`0093:79-80`, quoting `0065:102`). Nothing about this axis makes that
reasoning different, so it is reused rather than re-argued.

**Tests are included**, for ADR-0093 parameter B's reason: a suppression by directory is one
nobody re-reads (`0093:85-89`).

**This property alone is written, and the neighbouring analyzer properties are not.**
`EnableNETAnalyzers` and `AnalysisLevel` are not set by this ADR. The `CA1050` probe below sets
nothing but `TargetFramework` and `AnalysisMode`, and `CA1050` fires in it, so the quality
analyzers already run without this repository writing a property to switch them on. Writing one
anyway would restate something the SDK is doing, which is the rule ADR-0093 states at `0093:232`
as ADR-0065's "write only what deviates" applied to a build file. The analyzer set's version
travels with the SDK, and the SDK version is ADR-0030's, which is the same attribution ADR-0092
uses at `0092:319-320`: "ADR-0030 (the runtime version that determines which SDK ships the
analyzers)".

### B. `All` is rejected, and the finding that rejects it is named

`All` costs exactly one warning against this repository today, and it is worth reading in full
rather than as a count:

```text
ScopeDefinition.cs(22,30): warning CA1819: Properties should not return arrays
```

That property is `public required string[] Resources { get; set; }`
(`src/Nami.Identity.Abstractions/ScopeDefinition.cs:22`), and its `string[]` is not this
repository's invention. The design declares the class at
`docs/design/23-configuration-and-client-declaration.md:89-93`, and its three members at
`90-92`, the third of which is `+string[] Resources` at `92`. That block sits inside the
design's section 3, "Interfaces and contract"
(`docs/design/23-configuration-and-client-declaration.md:66`). The member is transcribed from
the implementer source of record, and `src/CLAUDE.md:28-31` is where this repository records
that transcription as the reason the type could be landed at all.

**The line range above was read at source rather than inherited, and that mattered.** Read on
2026-08-03, `src/CLAUDE.md:29` gave the range as `23:87-91` and said it "gives all three of its
members", and the design read the same day did not bear that out: `87` was the tail of the
preceding class, and `92`, the `string[]` this whole rejection turns on, fell outside the
range. The correct range is the one cited above. Recorded here because a citation that resolves
to the wrong lines is the defect class this repository has had most often, and because a reader
who found the two disagreeing would otherwise have no way to tell which of them had been
checked.

So `All` offers exactly two ways forward on the first type in the repository, and both are
worse than not taking it. Changing `Resources` to a read-only collection would deviate from the
design that fixes its members, in the one place where `src/CLAUDE.md` says deviation is how "an
invented decision enters the codebase wearing the design's authority" (`src/CLAUDE.md:17-18`).
Suppressing `CA1819` instead would mean the repository's first `NoWarn` is written in the same
change that adopts the ruleset it exempts.

**That second option is precisely the pressure ADR-0065 predicted, arriving earlier than
predicted.** ADR-0065 dated the risk to M1: a ruleset proven against a fixture is "known to work
and **not** known to be liveable", and what moves to M1 is "the pressure to weaken a rule rather
than fix the code that trips it" (`0065:109`). One project of three properties was enough to
produce it. That is the finding, and it is the reason `All` is rejected on evidence rather than
on taste: the widest setting was tried, the cost was read, and the cost is a carve-out on the
only type there is.

**Reversal condition, with a trigger rather than a date.** Revisit `All` when `src/` holds
enough code to answer the question on more than one type. One finding on one property is not
evidence that `All` is unliveable, only that today it cannot be adopted clean, and the two are
different claims. When the catalogue of ports lands, re-run `AnalysisMode=All` against the
solution and read the whole list; if the findings are real defects, `All` becomes adoptable on
the same kind of evidence that rejected it here.

### C. The security axis is untouched, and stays ADR-0092's

`AnalysisLevelSecurity` is measured below alongside `AnalysisMode`, and it is measured only to
show that the two axes are independent and that neither is what makes the other cost nothing.
This ADR does not set it. ADR-0092 section 1 owns that axis (`0092:69`), and moving its property
here would put one question under two ADRs.

With this parameter, all three axes have an owner: style and naming with ADR-0065, security with
ADR-0092, code quality here.

### The measurements, 2026-08-03, .NET SDK 10.0.301

Every figure here is a measurement with a date. Re-run it rather than cite it forward. The SDK
version was read with `dotnet --version`, which reported `10.0.301`.

**Against the real solution, `Recommended` costs nothing.** Each row is a full rebuild,
`dotnet build Nami.Identity.slnx --nologo -t:Rebuild` plus the one property:

| Property | Warnings | Exit |
|---|---|---|
| `-p:AnalysisMode=Recommended` | 0 | 0 |
| `-p:AnalysisLevelSecurity=latest-all` | 0 | 0 |
| `-p:AnalysisMode=All` | 1, the `CA1819` quoted in parameter B | 0 |

`-t:Rebuild` is not decoration. Analyzers run with the compiler, so an up-to-date incremental
build reports no diagnostics and reads exactly like a clean result.

**What `Recommended` changes in the shipped configuration, and the caveat that goes with it.**
The SDK ships one globalconfig per analysis mode at
`Sdks/Microsoft.NET.Sdk/analyzers/build/config`. Counted there with `wc -l` and
`grep -c "severity = warning"`:

| File | Lines | `severity = warning` lines |
|---|---|---|
| `analysislevel_10_default.globalconfig` | 12 | 0 |
| `analysislevel_10_recommended.globalconfig` | 447 | 145 |

**That 145 is not "145 rules turn on".** The number counts the lines the `Recommended` file
sets to `warning`, and says nothing about what each of those rules would have done if nobody
set it. The `default` file is the evidence for the shape of the gap and also for its limit: read
in full it is 12 lines, of which exactly one is a severity, `dotnet_diagnostic.CA1516.severity =
none`. So the default configuration raises no rule to `warning`, and every rule other than
`CA1516` is left at whatever the analyzer itself ships. How many of the 145 were already
enabled that way is not measured here and is not claimed. What is measured is the next
paragraph: at least one of them is genuinely off until `Recommended` is chosen.

**`CA1050` is a rule that is genuinely off at the default.** Probed outside this repository, so
it inherits neither `Directory.Build.props` nor `.editorconfig`, with a `net10.0` project whose
only property is `TargetFramework` and one file declaring a type outside any namespace. `obj`
and `bin` were deleted between runs:

| Properties | `CA1050` | Exit |
|---|---|---|
| none | absent | 0 |
| `AnalysisMode=Recommended` | warning | 0 |
| `AnalysisMode=Recommended` plus `TreatWarningsAsErrors=true` | error | 1 |

The third row is this ADR meeting ADR-0093: breadth decides that the diagnostic is produced,
and ADR-0093's property decides that producing it stops the build. Neither row can be reached by
the other property alone.

**Method note, because the first attempt at that table produced a false pass.** The three runs
were first driven from a shell loop holding the flags in a variable. zsh does not word-split an
unquoted expansion, so the two-property row was handed to `dotnet` as a single argument,
`TreatWarningsAsErrors` never took effect, and the run reported `Build succeeded`, 0 warnings,
exit 0. That output is indistinguishable from the property doing nothing. Re-run with the two
flags as two arguments it produced `error CA1050` and exit 1, which is the row above. The
general shape is the one ADR-0093 already recorded for `MSBUILD : error MSB1006` (`0093:211-218`):
a property passed on a command line can fail to arrive, and the failure looks like a result.

### Consequences

* Good, because the axis has an owner, so the `CA` rules stop being in force by default with no
  document able to say why. Together with ADR-0065 and ADR-0092 section 1, the three analyzer
  axes now have three named owners.
* Good, because the added breadth costs zero warnings against the solution today, measured, so
  no contributor inherits a backlog on the day it lands. That is a property of the date rather
  than of the decision.
* Good, because `Recommended` plus ADR-0093 makes a real quality defect a build break at the
  moment it is written, which `CA1050` demonstrates end to end above.
* Bad, because `Recommended` will eventually flag code that its author considers correct, and
  under ADR-0093 that is a stop-work item rather than a warning. Accepted: ADR-0093 parameter D,
  a per-project `<NoWarn>` with a comment (`0093:124-129`), is the route, and it is deliberately
  narrow enough to be visible in a diff.
* Bad, because the set of rules this enables is the SDK's rather than this project's, so an SDK
  bump under ADR-0030 can widen the gate without any change in this repository. That is the
  price of not vendoring a ruleset, and the upgrade playbooks are where it surfaces.
* Bad, because rejecting `All` leaves rules unenforced that the project might on reflection
  want, and the reversal condition in parameter B is the only thing scheduled to re-ask. If
  nobody re-asks, `Recommended` becomes permanent by inattention rather than by decision.
* Neutral, because no existing diagnostic changes severity. `IDE1006` and `IDE0055` keep the
  error severity ADR-0065 gave them, `RS0016` and `RS0017` are untouched, and this ADR adds one
  property.

### Confirmation

* **Part 4 of `scripts/test-warnings-as-errors.sh` is the mechanism, and it is owed in this same
  increment.** It is not written, not run, and therefore not green at the time this ADR is
  accepted. What it has to assert is the `CA1050` table above as a gate rather than as a
  measurement: that with `AnalysisMode` removed from `Directory.Build.props` the fixture stops
  failing, since a gate never broken on purpose is not known to bite. `CA1050` is the right
  fixture for exactly the reason the middle row of that table gives, that it is absent at the
  default, so a rule already firing without `AnalysisMode` would give a green indistinguishable
  from the property being missing. A dated green belongs in the commit that runs it, not here:
  this repository has already shipped a commit message claiming a self-test was green before it
  had been run, which is why the root `CLAUDE.md` says a green hook is not a green build.
* **The zero measured against the solution is a fact about a repository with one project.** It
  is not a claim that `Recommended` is liveable, which is ADR-0065's distinction between a rule
  "known to work" and one known to be liveable (`0065:109`). The point to re-read this is when
  real code first produces a `CA` warning that ADR-0093 turns into a build break, and the
  question then is whether the code was fixed or the rule was suppressed.
* **The reversal condition in parameter B is a standing obligation, not a note.** Re-run
  `AnalysisMode=All` against the solution when `src/` holds more than one type, and record the
  new finding list with its date. If that run is never made, the rejection of `All` stays
  justified by a measurement against a single property, which is weaker evidence than it will
  look like by then.

## Pros and Cons of the Options

### A. `AnalysisMode=Recommended` (chosen)

* Good, because it is the widest setting that adopts clean against the code that exists,
  measured at 0 warnings on 2026-08-03.
* Good, because it is a curated set rather than every rule the analyzer ships, so the rules it
  turns into build breaks under ADR-0093 are ones the SDK vendor grouped as broadly applicable.
* Good, because it is a single inherited property with no per-project bookkeeping and no
  vendored ruleset to keep in sync.
* Bad, because the set is not this project's to choose, so its contents can change under an SDK
  bump without review here.
* Bad, because it is a middle setting, and a middle setting invites being left alone. Parameter
  B's reversal condition is the answer, and it depends on somebody honouring it.

### B. Leave `AnalysisMode` unset

* Good, because it changes nothing and cannot break a build that works today.
* Bad, and decisively, because the axis is configured either way: leaving it unset does not
  leave the quality rules unconfigured, it leaves them configured by a default no document here
  names. The `CA1050` row above is what that costs, one rule at a time.
* Bad, because it leaves ADR-0093's gate reading a rule set nobody chose, which is the half of
  that ADR's open question this one exists to close (`0093:234-235`).

### C. `AnalysisMode=All`

* Good, because it is the strictest option, it needs no judgement about which rules matter, and
  it cannot silently omit a rule the project would have wanted.
* Bad, and decisively, because adopting it today requires either deviating from the implementer
  source of record on `ScopeDefinition.Resources` or writing a suppression in the change that
  adopts the ruleset. Both are named in parameter B, and both are worse than waiting.
* Bad, because a one-finding sample is a weak basis for a permanent widest setting in either
  direction, so taking it now would be as under-evidenced as rejecting it forever. Parameter B
  records the trigger to re-ask instead.

## More Information

* **No `stack-record: true` marker, and no row in ADR-0061, deliberately.** This ADR introduces
  no technology: it sets one MSBuild property on the SDK ADR-0030 already chose, and `0061:68`
  already carries the "Code style and conventions" row naming `.editorconfig` plus .NET
  analyzers and `dotnet format`, owned by ADR-0065. This is said out loud because an absent
  marker would otherwise read as an oversight, and because adding the marker without adding a
  table row fails guardrail Check 4 in the other direction. `docs/adr/CLAUDE.md` records the
  wider trap: that check compares two lists derived from this repository's own markup, so it is
  blind to a shared omission and its green is not evidence of coverage.
* Related decisions: ADR-0093 (the escalation this breadth feeds, and the ADR that left this
  axis open), ADR-0065 (the style and naming axis, scoped to two diagnostics, and the predicted
  pressure this ADR saw arrive early), ADR-0092 (the security axis, which stays where it is),
  ADR-0030 (the runtime version that determines which SDK ships the analyzers), ADR-0044 (the
  public-API analyzers, whose severities this ADR does not touch), and ADR-0060 (the CI
  composition the gate runs inside).
* **The property lands in `Directory.Build.props` later in this same increment**, together with
  ADR-0093's, and the self-test named in the Confirmation lands with them. Until then this ADR
  is a decision with its measurements and not a configured build.
* Authored fresh for this repository, not imported from the design corpus. The three
  measurements, the `CA1819` finding, and the rejection reasoning are this repository's own.
