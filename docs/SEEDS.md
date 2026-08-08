# Seed issue tracker

Scheduled work, decomposed. [`../.claude/rules/seeds.md`](../.claude/rules/seeds.md) is the
authority on what a seed is, what fields it carries, and why. This file is the tracker and does not
restate that rule.

**This file may be cited.** That is the difference between it and
[`BUILD-PLAN.md`](BUILD-PLAN.md), which is temporary, will be deleted, and may never be pointed at.
An item moves from that queue into this file when it becomes actionable, and its queue row is
deleted in the same change.

**This file decides nothing.** Every seed points at the ADR, design, or source file that owns its
subject. Where a seed and its owner disagree, the seed is the bug.

## How to read the chain

A seed with `Blocked by: none` is actionable today. Start there. When a seed is done, read its
`Unblocks` line and mark those seeds `open`.

```mermaid
graph LR
  S001[S-001 classify] --> S002[S-002 bump pin]
  S001 --> S006[S-006 offline source]
  S002 --> S003[S-003 amend 0021]
  S002 --> S004[S-004 amend 0061]
  S001 --> S005[S-005 date the claims]
  S002 --> S005
  S002 --> S018[S-018 architecture layer]
  S002 --> S019[S-019 amend 0030]
  S002 --> S020[S-020 amend 0036]
  S003 --> S021[S-021 re-derive 0093 quote]
  S022[S-022 extend the 0061 rule] --> S023[S-023 wire the manifest check]
  S007[S-007 which identifier] --> S008[S-008 reference engine]
  S002 --> S008
  S007 --> S009[S-009 where the block splits]
  S008 --> S010[S-010 wire the engine]
  S009 --> S010
  S010 --> S011[S-011 contract suite]
  S010 --> S016[S-016 first slice]
```

## Status board

| ID | Title | Status | Blocked by |
|---|---|---|---|
| S-001 | Classify every `7.5.0` reference before changing any | done | none |
| S-002 | Bump the manifest to `[7.6.0]` and re-read the nine licences | done | S-001 done |
| S-003 | Amend ADR-0021 for the new pin and the half-run playbook | done | S-002 done |
| S-004 | Amend the ADR-0061 stack row to the new pin | done | S-002 done |
| S-005 | Date or re-point every source-read claim the bump invalidates | open | S-001, S-002 both done |
| S-006 | Decide what replaces the offline 7.5.0 source tree | open | S-001 done |
| S-007 | Resolve umbrella versus granular for `Core`'s engine reference | open | none |
| S-008 | Reference the engine from `Core` and enumerate the restore graph | blocked | S-007 |
| S-009 | Decide where the `AddOpenIddict` block splits at the persistence boundary | blocked | S-007 |
| S-010 | Wire the engine inside `AddNamiIdentity` | blocked | S-008, S-009 |
| S-011 | Stand up the contract-regression suite ADR-0021 part C requires | blocked | S-010 |
| S-012 | Reconcile design 01's context count against its own table | open | none |
| S-013 | Give the provider-selector key the decided form and an owner | open | none |
| S-014 | Place the three builder calls that exist in no document here | open | none |
| S-015 | Re-own design 04's boot-validation citation | open | none |
| S-016 | Define what the first slice is | blocked | S-010 |
| S-017 | Assign a configuration key to the nine options that have none | open | none |
| S-018 | Move the architecture layer's four engine-version statements to the new pin | done | S-002 done |
| S-019 | Amend ADR-0030's stack sentence to the new pin | done | S-002 done |
| S-020 | Amend ADR-0036's live-pin clause to the new pin | done | S-002 done |
| S-021 | Re-derive ADR-0093's verbatim quotation of ADR-0021 | done | S-003 done |
| S-022 | Extend ADR-0061's maintenance rule to cover a version moving inside a row | open | none |
| S-023 | Wire the ADR-0061-against-manifest check now its own trigger has fired | blocked | S-022 |
| S-024 | Correct view 03's inverted `DbContext` pooling row, and read the other eight | done | none |
| S-025 | Un-pin ADR-0030's seam range, as ADR-0021 already did to its own | open | none |
| S-026 | Correct the two ADRs that label ADR-0018 by the option it declined | done | none |
| S-027 | Give OpenTofu a licence-record row, its MPL-2.0 exception being unrecorded | open | none |

---

## S-001. Classify every `7.5.0` reference before changing any

**Status:** done · **Blocked by:** none · **Unblocks:** S-002, S-005, S-006

**Why this is a seed and not the first step of the bump.** Counted 2026-08-08, `7.5.0` appears on
**73 lines across 24 files**, excluding `BUILD-PLAN.md`. They are not the same kind of sentence, and
a bulk find-and-replace would corrupt two of the three kinds. Two of the hits are the name of a
rejected option in ADR-0021's own Considered Options list, and rewriting those would delete the
record of a decision. Many others are dated source reads, which `docs/CLAUDE.md` says must stay in
the past tense, because "a dated measurement edited to match today stops being evidence".

**End state.** A classification exists, in this seed, assigning every one of the
73 lines to exactly one of three buckets. It was written for a pull request body, and the
maintainer works directly on `main`, so it lands here instead, which is also what
`.claude/rules/seeds.md` asks for when prose has no other owner:

- **A, the live pin.** Sentences asserting what Nami pins today. These change.
- **B, the historical record.** Rejected options, amendment histories, and anything already written
  in the past tense with a date. These do not change.
- **C, the dated source read.** Claims of the form "read at the pinned version". These keep their
  date and their tense and gain a note that the pin has moved past them.

The count per bucket is stated, and the three counts sum to 73.

**Verification.** Two searches, not one, because one spelling is not the whole set. This
requirement is a correction the seed's own work produced, and finding 1 below is why.

```bash
git grep -n "7\.5\.0"      -- . ':!docs/BUILD-PLAN.md' ':!docs/SEEDS.md' ':!.claude/rules/seeds.md'
git grep -nP "7\.5(?!\.0)" -- . ':!docs/BUILD-PLAN.md' ':!docs/SEEDS.md' ':!.claude/rules/seeds.md'
```

Re-run both on the day of the work, because a total is a measurement and this one is dated
2026-08-08. Every line in the combined output appears in exactly one bucket, and the check runs in
both directions: no line in the output is unclassified, and no classified line is absent from the
output. The second search uses `-P` and not `-E`, because `docs/CLAUDE.md` records that `git grep
-E` does not honour `\b` in this clone.

The three exclusions each have a reason. `BUILD-PLAN.md` may never be cited. `SEEDS.md` and
`.claude/rules/seeds.md` hold this seed's own prose, so counting them counts the words describing
the work as if they were the work.

**Sources.** `docs/CLAUDE.md`, the section on a pointer at a file you are deleting from;
`docs/adr/0021-openiddict-version-adaptation.md:14`, `:32`, `:39`, `:71`;
`docs/adr/0061-technology-stack-of-record.md:49`.

**Out of scope.** Editing any of the 99 lines. This seed produces a classification, and the only
file it changes is this one.

### S-001 result, measured 2026-08-08 at commit `5c3e5ad`

**Read every line number below as that tree's, not as today's.** S-002, S-003, and S-021 landed
immediately after this seed and rewrote `Directory.Packages.props`,
`docs/DEPENDENCY-LICENSES.md`, `docs/adr/0021-openiddict-version-adaptation.md`, and
`docs/adr/0093-warnings-as-errors.md`, so the bucket-A pointers into those four files describe lines
that no longer say 7.5.0 and may no longer sit at those numbers. Re-deriving them would be wrong
rather than tidy, because the classification is a snapshot of the tree the bump was planned against.
`docs/CLAUDE.md` states the rule this follows: a sentence asserting what another file currently
contains is a measurement, so it is dated, written in the past tense, and names the commit it was
true at.

**The test that assigns the bucket.** The seed named three buckets and gave no test, so this is the
test used: **when the pin becomes 7.6.0, what must happen to this line?** Bucket A means the digits
change. Bucket B means nothing happens. Bucket C means the digits stay and the line gains a note
that the pin has moved past them. The boundary between B and C is whether the line is a closed
record or a fact the repository still rests on. A closed record is B, and a fact still relied on is
C.

| Spelling searched | Lines | Files | A | B | C |
|---|---|---|---|---|---|
| `7.5.0` | 73 | 24 | 14 | 4 | 55 |
| `7.5` with no patch number | 26 | 18 | 8 | 3 | 15 |
| **Combined** | **99** | **36** | **22** | **7** | **70** |

**The seed's own figure is confirmed.** Re-running the seed's command today returned 88 lines across
26 files, against the 73 across 24 it was written with. The whole difference is the tracker carrying
the seed: `SEEDS.md` held 14 and `.claude/rules/seeds.md` held 1, and both were written after the
measurement. Excluding those two files reproduces 73 across 24 exactly.

Seven findings follow. The first three change what the bump has to touch.

**1. The seed's own verification command was too narrow, and it hid eight bucket-A lines.** The
`7.5.0` search cannot see a line writing `OpenIddict 7.5` with no patch number, and 26 such lines
exist. Twelve of the 36 files carry only that shorter spelling, so the seed's command returned no
line at all from any of them. Eight of the 26 are bucket A. A bump run against the seed's command as
written would leave eight live-pin statements reading 7.5.

**2. Four bucket-A lines sit in `docs/architecture/`, and no seed owned them.**
`01-introduction-scope.md:14`, `03-drivers-and-constraints.md:114`, `04-system-context.md:21`, and
`README.md:10` each state the engine version. S-003 owns ADR-0021, S-004 owns ADR-0061, and S-005
owns bucket C, so all four fell outside every existing seed. S-018 now owns them.

**3. The bump amends four ADRs rather than two.** Besides ADR-0021 (S-003) and ADR-0061 (S-004),
`docs/adr/0030-dotnet-version-upgrade.md:14` names `OpenIddict 7.5` inside its sentence about the
stack .NET 10 underpins, and `docs/adr/0036-database-key-strategy-uuidv7.md:40` writes "the pin is
7.5.0 (ADR-0061)" in the present tense. S-019 and S-020 now own them, one seed each, because one ADR
per commit is the rule.

**4. ADR-0093 quotes a bucket-A line of ADR-0021 word for word.**
`docs/adr/0093-warnings-as-errors.md:150` writes that the playbook "already instructs the project to
`clear obsolete warnings on 7.5 now`" and cites `0021:44`. That string is `0021:44` itself, which is
bucket A. Editing the source falsifies the quotation, and a style pass may not simplify a quotation,
so the two edits have to land together as two commits. S-021 owns the second one.

**5. Bucket C is not uniform, and S-005 needs the split.** A bucket-C line naming 7.5.0 explicitly
inside a record that already carries a date needs no edit at all, only confirming, which is what
S-005 already says of `design/04-core-protocol.md:55`. Six such lines are ADR `consulted:` entries,
dated by their own frontmatter `date:` field. The lines that do need the note are the ones coupling
the read to the word **pinned**, because that coupling is what breaks.

**6. Two bucket-B lines are rejected options, and rewriting them would delete a decision.**
`0021:32` and `0021:71` are both the option "Pin 7.5.0 forever and never upgrade". Three more
bucket-B lines are illustrations of a bump sequence, `7.5` to `7.6` to `8.0`, at `0011:56`,
`0021:43`, and `design/12-key-management.md:64`. An illustration of a sequence stays true after the
pin moves along it.

**7. One false positive, named so a later searcher does not count it again.**
`docs/DEPENDENCY-LICENSES.md:132` matches a bare `7.5` search and has nothing to do with the engine:
it is `jcharts:jcharts:0.7.5` in the licence-bucket table.

**Per-file counts.** These sum to the combined row above, and they are what makes the classification
re-derivable without a 99-row table.

| File | A | B | C |
|---|---|---|---|
| `Directory.Packages.props` | 10 | 0 | 0 |
| `docs/CLAUDE.md` | 0 | 1 | 1 |
| `docs/DEPENDENCY-LICENSES.md` | 1 | 0 | 9 |
| `docs/adr/0004-refresh-token-posture.md` | 0 | 0 | 4 |
| `docs/adr/0011-no-restart-key-rotation.md` | 0 | 1 | 2 |
| `docs/adr/0014-advanced-protocol-scope.md` | 0 | 0 | 8 |
| `docs/adr/0018-dbcontext-pooling-for-pool-mode.md` | 0 | 0 | 1 |
| `docs/adr/0019-single-logout-strategy.md` | 0 | 0 | 1 |
| `docs/adr/0020-admin-architecture.md` | 0 | 1 | 0 |
| `docs/adr/0021-openiddict-version-adaptation.md` | 3 | 3 | 1 |
| `docs/adr/0030-dotnet-version-upgrade.md` | 1 | 0 | 0 |
| `docs/adr/0033-key-scope-isolation-model.md` | 0 | 0 | 5 |
| `docs/adr/0035-self-service-client-registration.md` | 0 | 0 | 7 |
| `docs/adr/0036-database-key-strategy-uuidv7.md` | 1 | 0 | 0 |
| `docs/adr/0039-revocation-propagation-and-cache-coherence.md` | 0 | 0 | 3 |
| `docs/adr/0043-security-hardening-invariants-startup-check.md` | 0 | 0 | 1 |
| `docs/adr/0048-introspection-revocation-endpoint-isolation.md` | 0 | 0 | 2 |
| `docs/adr/0061-technology-stack-of-record.md` | 1 | 0 | 0 |
| `docs/adr/0091-browser-facing-response-headers.md` | 0 | 0 | 2 |
| `docs/adr/0093-warnings-as-errors.md` | 1 | 0 | 0 |
| `docs/architecture/01-introduction-scope.md` | 1 | 0 | 0 |
| `docs/architecture/03-drivers-and-constraints.md` | 1 | 0 | 0 |
| `docs/architecture/04-system-context.md` | 1 | 0 | 0 |
| `docs/architecture/09-runtime-flow-views.md` | 0 | 0 | 1 |
| `docs/architecture/README.md` | 1 | 0 | 0 |
| `docs/design/02-data.md` | 0 | 0 | 3 |
| `docs/design/04-core-protocol.md` | 0 | 0 | 4 |
| `docs/design/05-resource-server-validation.md` | 0 | 0 | 3 |
| `docs/design/06-sender-constrained-tokens.md` | 0 | 0 | 2 |
| `docs/design/08-user-management.md` | 0 | 0 | 1 |
| `docs/design/09-federation-and-claims-profile.md` | 0 | 0 | 1 |
| `docs/design/12-key-management.md` | 0 | 1 | 2 |
| `docs/design/22-openiddict-seam-catalogue.md` | 0 | 0 | 3 |
| `docs/design/23-configuration-and-client-declaration.md` | 0 | 0 | 1 |
| `src/CLAUDE.md` | 0 | 0 | 1 |
| `src/Nami.Identity.Core/Nami.Identity.Core.csproj` | 0 | 0 | 1 |

**The 22 bucket-A lines in full**, because this is the list S-002 and the four new seeds act on and
it is short enough to write out. `Directory.Packages.props:122`, `:123`, and `:164` through `:171`;
`docs/DEPENDENCY-LICENSES.md:183`; `docs/adr/0021-openiddict-version-adaptation.md:14`, `:39`, and
`:44`; `docs/adr/0030-dotnet-version-upgrade.md:14`;
`docs/adr/0036-database-key-strategy-uuidv7.md:40`;
`docs/adr/0061-technology-stack-of-record.md:49`; `docs/adr/0093-warnings-as-errors.md:150`;
`docs/architecture/01-introduction-scope.md:14`;
`docs/architecture/03-drivers-and-constraints.md:114`; `docs/architecture/04-system-context.md:21`;
and `docs/architecture/README.md:10`.

**The 7 bucket-B lines in full**, because leaving a line alone is a decision that a later reader
will want to check. `docs/CLAUDE.md:182`; `docs/adr/0011-no-restart-key-rotation.md:56`;
`docs/adr/0020-admin-architecture.md:85`; `docs/adr/0021-openiddict-version-adaptation.md:32`,
`:43`, and `:71`; `docs/design/12-key-management.md:64`.

Bucket C is the remaining 70 lines, and S-005 owns them. It is not written out here because the two
searches plus the two lists above produce it by subtraction.

---

## S-002. Bump the manifest to `[7.6.0]` and re-read the nine licences

**Status:** done · **Blocked by:** S-001, which is done · **Unblocks:** S-003, S-004, S-005, S-008,
S-018, S-019, S-020

**What 7.6.0 actually contains**, read at the release page on 2026-08-08. It is a maintenance
release with three changes and no breaking change. The Entity Framework 6.x and EF Core stores
"have been updated to automatically restore the `EntityState` of token entities after failed
application deletion". `OpenIddict.Client.WebIntegration` "now supports Vercel and ID Austria". And
"all the .NET and third-party dependencies have been updated to their latest version". Published
2026-07-15.

**Only one of the three reaches Nami.** The first touches `OpenIddict.EntityFrameworkCore`, which
the persistence adapter will take. The second is in a package S-007 exists to keep out of the graph.
The third moves the transitive closure, which reaches the ADR-0026 licence scan rather than the pin.

**End state.**

- All eight `PackageVersion` rows in `Directory.Packages.props` read `[7.6.0]`, and no
  `PackageVersion` row reads `[7.5.0]`. **The scope on that second half is a correction this seed
  had to make to itself**, and the result section below says why the unscoped form it was written
  with cannot be satisfied.
- `DEPENDENCY-LICENSES.md` section 3.3 records nine licence reads at 7.6.0, each with the read
  location and the date, and carries the new upstream commit,
  `5ce649a5bbbf1340c9be9c4f264197af563ab473`. Read on 2026-08-08 for
  `OpenIddict.Server.AspNetCore` 7.6.0, which declares
  `<license type="expression">Apache-2.0</license>`; the other eight are re-read by this seed rather
  than carried forward from the 7.5.0 reading.
- The 7.5.0 reading is **past-tensed and kept**, not deleted, so the pin's history stays checkable.
- The manifest comment states that the playbook of ADR-0021 parameter D ran in part: the release
  notes were read, and the contract-regression suite it also requires does not exist. S-011 is named
  as the seed that closes that half.

**Verification.** `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, the four
self-tests, the guardrail, the decisions index, and `markdownlint-cli2` with its file count
cross-checked against `git ls-files '*.md'`. No `PackageReference` exists yet, so the restore graph
does not move and the build is expected to be unchanged; if it is not, that is a finding and this
seed stops.

**Sources.** `docs/adr/0021-openiddict-version-adaptation.md:39` for the bracket form and `:44` for
the playbook; `docs/adr/0026-dependency-license-policy.md` section A for the permissive list;
`docs/DEPENDENCY-LICENSES.md` section 7 for the read-at-source rule.

**Out of scope.** Amending ADR-0021 or ADR-0061, which are S-003 and S-004, because one ADR per
commit. Adding a `PackageReference`, which is S-008.

### S-002 result, measured 2026-08-08

The pin is `[7.6.0]` on all eight rows. Ten licences were read at their own `.nuspec`, not nine, and
the tenth is the finding. Four things this seed did not expect are recorded below.

**7.6.0 is the latest stable, and two 8.0 previews now exist.** Read at the flat container version
index for `OpenIddict.Core` on 2026-08-08: 85 published versions, ending `7.5.0`, `7.6.0`,
`8.0.0-preview.1.26302.68`, and `8.0.0-preview.2.26365.61`. That matters beyond this seed.
`docs/adr/0021-openiddict-version-adaptation.md:44` asks the project to "run the rotation contract
test against an 8.0 preview early", and until now no preview was named as existing. Two are. S-003
records the trigger and S-011 owns the test.

**1. Nine was an undercount, and the tenth package is named in no file here.**
`OpenIddict.EntityFrameworkCore.Models` arrives transitively through
`OpenIddict.EntityFrameworkCore`, on exactly the footing that put `OpenIddict.Abstractions` inside
the nine. Searched 2026-08-08 across every tracked file except `BUILD-PLAN.md`,
`EntityFrameworkCore.Models` returned zero hits. It was read at 7.6.0 and at 7.5.0, both on
2026-08-08, and both declare `<license type="expression">Apache-2.0</license>`. So this is an
omission in the record rather than a change in the graph, and section 3.3 now carries ten rows.

**2. The transitive closure moved in version and not in shape.** Every net10.0 dependency group was
diffed, 7.5.0 against 7.6.0. No dependency identifier was added and none was removed.
`Microsoft.Extensions.*` moved 10.0.7 to 10.0.10, `Microsoft.IdentityModel.*` 8.16.0 to 8.19.2,
`Microsoft.EntityFrameworkCore.Relational` 10.0.7 to 10.0.10, and
`Quartz.Extensions.DependencyInjection` 3.15.1 to 3.18.2. So the ADR-0026 section C scan set gains
no new identifier from this bump, which is a stronger statement than the seed's premise that "the
third moves the transitive closure" and it is the one that was measured.

**3. The offline reference tree no longer matches the pin, and the proof is a tenth nuspec.**
`docs/CLAUDE.md` records the checked-in corpus source at
`aa7fac0996cb1c86c4310a005bdc66077eb53ba8`. `OpenIddict.EntityFrameworkCore.Models` **7.5.0**
declares that same commit at its own `.nuspec`, read 2026-08-08, which is independent confirmation
that the tree matched the old pin. All ten packages at **7.6.0** declare
`5ce649a5bbbf1340c9be9c4f264197af563ab473` instead. So S-006 is not a tidy-up: from this commit
until S-006 lands, an engine claim cannot be verified offline at the pinned version. Both the
manifest comment and licence-record section 3.3 now say so out loud rather than leaving the old
sentence standing.

**4. This seed's own verification asked for something its out-of-scope clause forbids.** It required
that `git grep "\[7\.5\.0\]"` return nothing. Run after the bump, it returns four hits and every one
is correct: `docs/adr/0021-openiddict-version-adaptation.md:39` is S-003's line and this seed may not
touch it; `Directory.Packages.props:120` and `docs/DEPENDENCY-LICENSES.md:192` are this bump's own
history, which `docs/CLAUDE.md` requires be written in the past tense rather than deleted; and
`.claude/rules/seeds.md:28` uses the string as an illustration of a declarative end state. An
unscoped absence check cannot pass here, so the End state above is now scoped to `PackageVersion`
rows. Measured after the bump: 8 rows at `[7.6.0]`, 0 at `[7.5.0]`.

**Bucket C inside the licence record was handled here, not by S-005.** S-001 classified nine
`docs/DEPENDENCY-LICENSES.md` lines as bucket C, and this seed's own end state required the 7.5.0
reading be past-tensed and kept. It is, as a "superseded 7.5.0 reading" paragraph at the end of
section 3.3. S-005 does not need to revisit that file for those nine lines.

**Verification run 2026-08-08, all nine gates.** Guardrail green; decisions index green at 97 ADRs;
`markdownlint-cli2` 195 files 0 issues with `git ls-files '*.md'` also 195; `dotnet build` 0
Warning(s) 0 Error(s); `dotnet test` 5 and 33 passed; `dotnet format --verify-no-changes` exit 0;
and the four self-tests exit 0. The build is unchanged, which the seed predicted, because no
`PackageReference` exists and so no restore graph moved.

**One false green was produced and caught during this seed, and it is worth the line.** A first
format run was written as `dotnet format ... --nologo | tail && echo "format OK"`. `dotnet format`
does not accept `--nologo`, so it exited 1 and printed its help text, while the `&&` chained on
`tail` and printed the success message anyway. The command list in
[`../.claude/rules/commands.md`](../.claude/rules/commands.md) does not carry that flag, and adding
it was the mistake. Read an exit code directly, never through a pipe.

---

## S-003. Amend ADR-0021 for the new pin and the half-run playbook

**Status:** done · **Blocked by:** S-002, which is done · **Unblocks:** S-021

**The honest difficulty.** This ADR's parameter D requires, for each release, that the
contract-regression suite runs. Searched 2026-08-08, `tests/` holds only
`Nami.Identity.ArchitectureTests` and `Nami.Identity.UnitTests`, and `contract-regression` returns
zero hits across `tests/` and `src/`. So the first bump this repository performs runs half of its
own playbook. The amendment records that as an accepted gap with a named closing seed, rather than
letting a green build imply the playbook ran.

**End state.** ADR-0021 says the pin is 7.6.0. Line 14's "Nami pins OpenIddict 7.5.0" is updated
and the change is recorded in More Information in this ADR's existing amendment style. Parameter
A's bracket example reads `[7.6.0]`. The two Considered Options mentions of 7.5.0 are **unchanged**,
because they name a rejected option. A new More Information entry records the 2026-08-08 bump, what
the release contained, which half of parameter D ran, and that S-011 owes the other half.

**Three lines here, not two, and the third is quoted elsewhere.** S-001 classified `:14`, `:39`, and
`:44` as bucket A, and `:32`, `:43`, and `:71` as bucket B. When this seed was written, line `:44`
read "clear obsolete warnings on 7.5 now" and `docs/adr/0093-warnings-as-errors.md:150` quoted that
string word for word while citing `0021:44`. So editing `:44` falsified a quotation in another ADR.
S-021 repaired it, and the two commits landed together.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, and
`python3 scripts/check-decisions-index.py`. Check 3 requires the index row status to equal the
frontmatter status, so confirm neither moved.

**Sources.** `docs/adr/0021-openiddict-version-adaptation.md:14`, `:32`, `:39`, `:43`, `:44`, `:71`.

**Out of scope.** ADR-0061's row, which is S-004. Building the suite, which is S-011.

### S-003 result, measured 2026-08-08

Three lines changed, `:14`, `:39`, and `:44`, and one More Information amendment was added. The
amendment is longer than the edit because the bump turned four of this ADR's own forward-looking
sentences into checkable ones, and three of them checked out.

**The half-run playbook is recorded, as planned.** The release notes were read. The
contract-regression suite does not exist, and the amendment names S-011 rather than letting a green
build imply otherwise.

**1. The 7.6.0 release notes were verified at source rather than inherited from this seed.** Read at
the GitHub release body for tag `7.6.0` on 2026-08-08: published 2026-07-15T15:50:25Z,
`prerelease: false`, and exactly the three changes S-002 recorded. This seed's own premise is
therefore confirmed by a second read rather than quoted forward.

**2. Both 8.0 breaking changes parameter D predicted on 2026-07-04 are confirmed, and one is now
narrower.** Read at the `8.0.0-preview.1` release body: "All the members obsoleted in previous
versions of OpenIddict have been removed", and three named types, not one, "no longer inherit from
ASP.NET Core's `AuthenticationSchemeOptions` class in OpenIddict 8.x". The parameter said "an options
type" and did not name the base class. Both are now named. The parameter's own text was left alone
because it was right; the confirmation went in the amendment.

**3. The roadmap reading in the Confirmation has been overtaken, and this is the finding with the
longest reach.** That line records, verified 2026-07-04, that DCR (issue #2404) and back-channel
logout (issue #2175) both target `8.0.0-preview.2`. Preview.2 shipped 2026-07-15 without either.
Searching the release body is not enough to prove absence, so the issues were read the same day:
**both are open and both now carry milestone `8.0.0-preview.3`**. The target slipped one preview.
Issue #1345 (telemetry) is still open with no milestone, which confirms parameter E rather than
changing it. `ADR-0021:60` is the **only** line in the repository naming an 8.0 preview, searched
2026-08-08 for `8.0.0-preview`, `8.0 preview`, and `preview.[123]` across every tracked file except
the work queue, so the slip is contained to one line that already carries its date. Parameter E's
`replace-when-native: OpenIddict 8.0` markers are unaffected, naming 8.0 rather than a preview, so
ADR-0014 and ADR-0019 need no change.

**4. Four further 8.0 breaking changes do not reach Nami, and one measured line is the reason.**
Preview.1 drops ASP.NET Core 2.3 and EF Core 2.3, raises the .NET Framework floor to 4.8, removes the
.NET Standard 2.0/2.1 and UAP target frameworks, and moves to `Microsoft.Extensions.*` 10.x.
`Directory.Build.props:114` sets `NamiLibraryTargetFrameworks` to `net10.0` and nothing else. So none
of the first three applies today, and the S-002 dependency diff already showed the graph at 10.x. The
amendment states the condition under which this stops being true, which is that knob gaining a
`net48` entry.

**5. Parameter F's source read was left exactly as written, deliberately.** It verifies OpenIddict
types "in the checked-in 7.5.0 source", which names its own version, so by S-001's bucket-C test it
needs confirming rather than editing. What the amendment adds is that the checked-in tree no longer
matches the pin, pointing at S-006 and S-005.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, and
`python3 scripts/check-decisions-index.py`, both green. Check 3 compares the index row status against
the frontmatter status: both still read `accepted`, and neither was touched. `stack-record: true` is
unchanged, so Check 4 sees the same ADR-0061 row it saw before, and moving that row is S-004.
`markdownlint-cli2` green at 195 files. The two pointers S-021 depends on were re-read after the
edit: `0021:44` is still parameter D and `0093:150` still holds the stale quotation, so S-021's
sources did not move.

---

## S-004. Amend the ADR-0061 stack row to the new pin

**Status:** done · **Blocked by:** S-002, which is done · **Unblocks:** nothing yet. S-022 and S-023
are seeds this one **established** rather than unblocked: the gap each names existed before this seed
ran, and neither was waiting on it

**End state.** `docs/adr/0061-technology-stack-of-record.md:49` reads 7.6 rather than 7.5 in its
`Protocol engine` row, with the change recorded in that ADR's own maintenance style.

**Why it is a separate seed.** Two reasons, and the second is the real one. One ADR per commit is
the repository's rule. And ADR-0061 is guardrail-enforced from two directions by Check 4, so a
change to it is a different risk from a change to ADR-0021, which Check 4 does not read.

**Verification.** `bash scripts/check-adrs.sh` and `bash scripts/test-check-adrs.sh`. Check 4
compares two lists both derived from this repository's own markup, so a green result proves they
agree and proves nothing about a shared omission; read the row rather than the exit code.

**Sources.** `docs/adr/0061-technology-stack-of-record.md:49`, and `:84` for the limit of Check 4.

**Out of scope.** Every other row in that table.

### S-004 result, measured 2026-08-08

One cell changed, and the seed's own warning turned out to name the wrong blind spot.

**The edit.** `:49` reads "OpenIddict 7.6 (pinned, seam-isolated)". The patch number is deliberately
absent: every version in that table is written to major or minor only, and four other rows were read
to confirm it (".NET 10", "PostgreSQL 18", "EF Core 10", "Bootstrap 5"). ADR-0021 parameter A owns the
exact pin with its bracket, and an index repeating it would be a second place to be wrong.

**1. The maintenance rule does not authorize this edit, which is why S-022 now exists.** Read at
source, `0061:80` has two clauses: add a row when an ADR is accepted, and re-point a row when a choice
is superseded. A version moving **inside** an existing row is neither. The edit was made instead under
the sentence beside them, that the table "is an index, never the authority", so a stale version cell is
the table being the bug. Extending the rule to say so is a change to a binding rule, so it is its own
seed rather than a line smuggled into a transcription.

**2. The seed pointed at the wrong limit of Check 4, and the real one is narrower.** This seed's
Sources cite `0061:84` for what Check 4 cannot see, and that paragraph describes a **shared omission**:
a technology with no row and no marker, two empty entries agreeing perfectly. That is not this case.
This was a row that existed and was wrong. Read at `scripts/check-adrs.sh` on 2026-08-08, Check 4
extracts only the **last** cell of each row with `sed -E 's/.*\| ([0-9, ]+) \|$/\1/'` and set-compares
those ADR numbers against the `stack-record: true` markers. It never reads the "Committed choice" cell
at all. So the version could have stayed at 7.5 indefinitely with every gate green, and it did stay
wrong from the moment S-002 landed the pin until this commit.

**3. The trigger `0061:86` names has fired, which is why S-023 now exists.** That paragraph says to
wire the table-against-manifest check "when the manifest carries runtime packages", and that until then
the human step is the whole of it. When it was written the manifest held one build-time analyzer. It now
holds **eight** bracket-pinned OpenIddict rows, landed 2026-08-08 by S-002, and those are runtime
packages indexed by the very row this seed corrected. So the manifest side of the check can find
something rather than nothing, and the first thing it would find is the defect this seed fixed by hand.

**Verification.** `bash scripts/check-adrs.sh` and `bash scripts/test-check-adrs.sh`, both green, and
`python3 scripts/check-decisions-index.py` green at 97 ADRs. Per this seed's own instruction the row
was read rather than the exit code: `:49` names 7.6 and still cites `0021, 0014, 0048`, so Check 4's
last-cell extraction sees an unchanged set. The frontmatter was untouched, so `stack-record: true` and
`status: "accepted"` still satisfy Checks 3 and 4. `markdownlint-cli2` green at 195 files.

---

## S-005. Date or re-point every source-read claim the bump invalidates

**Status:** open · **Blocked by:** S-001 and S-002, both done · **Unblocks:** nothing yet

**The problem in one sentence.** At least a dozen documents state a fact about the engine and
attribute it to "the pinned version", and after S-002 the pinned version is not the one that was
read.

**End state.** Every bucket-C line from S-001 either names 7.5.0 explicitly with its original date,
or is rewritten to name the version actually read. No line says "the pinned version" while meaning
a version that is no longer pinned. `design/04-core-protocol.md:55`, which reads "Every API name in
this block was read at OpenIddict release tag 7.5.0", is already in the correct shape and is
confirmed rather than edited.

**The set is 70 lines, and S-001 says which.** Subtract S-001's 22 bucket-A lines and its 7 bucket-B
lines from the 99 its two searches returned. Two facts from that classification change this seed's
shape. A bucket-C line that already names 7.5.0 inside a record carrying its own date needs
confirming and no edit, and six of them are ADR `consulted:` entries dated by their frontmatter
`date:` field. The lines that do need the note are the ones coupling a read to the word **pinned**.

**Verification.** `git grep -n "at the pinned version"` returns only lines whose surrounding text
names 7.6.0, or names 7.5.0 with a date. **That pattern alone is too narrow to close this seed.**
Measured 2026-08-08, it matched 7 lines across 5 files outside `SEEDS.md`, while the wider phrase
`the pinned` matched 34 files. So run the wider phrase as well and read every hit that also names a
version. Re-run `/refresh-citations` afterwards, because this seed edits many files and will age
pointers into them.

**Sources.** `docs/design/04-core-protocol.md:55` and `:1054`;
`docs/design/22-openiddict-seam-catalogue.md:250` and `:606`;
`docs/design/23-configuration-and-client-declaration.md:486`;
`docs/adr/0091-browser-facing-response-headers.md:30` and `:438`;
`docs/design/05-resource-server-validation.md:597`;
`docs/design/06-sender-constrained-tokens.md:121`.

**Out of scope.** Re-verifying any of the claims against 7.6.0 source. This seed fixes what the
claims say about their own provenance. Whether each claim is still true at 7.6.0 is S-011's
subject, and S-006 is what makes checking it possible at all.

---

## S-006. Decide what replaces the offline 7.5.0 source tree

**Status:** open · **Blocked by:** S-001, which is done · **Unblocks:** nothing yet

**Why this is a decision and not a chore.** `docs/CLAUDE.md` records that the external design
corpus carries a checked-in OpenIddict source tree, that it sits at commit
`aa7fac0996cb1c86c4310a005bdc66077eb53ba8`, and that "the local NuGet cache carries only 7.4.0, so
that tree is the only offline way to verify a 7.5.0 default". 7.6.0 declares a different upstream
commit, `5ce649a5bbbf1340c9be9c4f264197af563ab473`, read 2026-08-08. So after S-002 there is no
offline way to read the pinned engine's source, and this repository's evidence rule leans on exactly
that ability.

**End state.** A decision exists, recorded as an ADR because it changes how every future engine
claim is verified. It picks one of: read the shipped assemblies from the restored package, which is
what ADR-0091 already did once; obtain the 7.6.0 source for offline reading; or accept that engine
claims are verified online and say so. Whatever is chosen, `docs/CLAUDE.md`'s paragraph about the
reference tree is corrected in the same change, because it currently describes a tree that matches
the pin and will not.

**Verification.** The ADR lands with rows in both indexes and passes Check 3 and Check 7.
`docs/CLAUDE.md` no longer claims the checked-in tree matches the pin.

**Sources.** `docs/CLAUDE.md`, the "Reading the external design corpus" section;
`docs/adr/0091-browser-facing-response-headers.md:5`, which read a shipped assembly rather than
source and is the precedent for one of the options.

**Out of scope.** Re-verifying any specific claim. This seed decides the method.

---

## S-007. Resolve umbrella versus granular for `Core`'s engine reference

**Status:** open · **Blocked by:** none · **Unblocks:** S-008, S-009

**The disagreement, quoted from both sides.**
`docs/design/01-foundations.md:430` lists the engine as `OpenIddict (AspNetCore,
EntityFrameworkCore, Quartz)` in its key-libraries table. `docs/design/04-core-protocol.md:827`
lists `OpenIddict.Server (.AspNetCore)` and `:828` lists `OpenIddict.Validation (.AspNetCore,
.ServerIntegration)`, and that document never names the umbrella package at all.

**Why it matters, measured 2026-08-08 at the nuget.org flat container.**
`OpenIddict.AspNetCore` declares **seven** net10.0 dependencies, being the whole client stack plus
the `OpenIddict` meta-package, and the meta-package reaches
`OpenIddict.Client.WebIntegration`, whose nupkg is **2 864 477 bytes**, for a server that is not an
OAuth client. `OpenIddict.Server.AspNetCore` declares **one**, `OpenIddict.Server`. Every extra node
is a licence read owed under ADR-0026.

**End state.** One of the two documents is corrected, and the correction says which was the bug and
why. Design 01 is the implementer source of record for the package graph, and design 04 for
everything inside the protocol host, so the resolution has to say which question each table was
answering before deciding which one is wrong.

**Verification.** `git grep -n "OpenIddict.AspNetCore"` returns no line that presents the umbrella
as what `Core` references, or design 04 is corrected instead and the same check passes in reverse.
`bash scripts/check-adrs.sh` and `markdownlint-cli2`.

**Sources.** `docs/design/01-foundations.md:98-99` and `:430`;
`docs/design/04-core-protocol.md:827-829` and `:1024-1029`.

**Out of scope.** Adding the reference, which is S-008.

---

## S-008. Reference the engine from `Core` and enumerate the restore graph

**Status:** blocked · **Blocked by:** S-007. S-002 is done · **Unblocks:** S-010

**End state.**

- `src/Nami.Identity.Core/Nami.Identity.Core.csproj` carries the `PackageReference` items S-007
  settled, with no `Version` attribute, because Central Package Management is on and a version there
  is `NU1008`.
- `DEPENDENCY-LICENSES.md` gains a restore-graph enumeration in the style of its section 3.1, read
  from `src/Nami.Identity.Core/obj/project.assets.json` after restore, with every node's licence
  read at its own nuspec and the date recorded.
- **The two inert architecture facts become live**, and the seed proves it rather than asserting it.
  Measured 2026-08-08, `Nami.Identity.Core.dll`'s reference table held only `System.*` and
  `Microsoft.Extensions.*`, so `CoreReferencesNoAdapterOrDatabaseProviderOrCloudSdk` and
  `CoreReferencesNoSiblingNamiPackageExceptAbstractions` were both asserting an empty set that was
  empty for a reason other than the rule. After this seed, at least one engine assembly appears in
  that table, and the class remarks recording the inert state are updated to say so.

**Verification.** All nine gates. Then plant a forbidden reference and watch
`CoreReferencesNoAdapterOrDatabaseProviderOrCloudSdk` fail, which is the check that could not be run
before this seed.

**Sources.** `src/CLAUDE.md`, the section on versions living in `Directory.Packages.props`;
`tests/Nami.Identity.ArchitectureTests/CoreDependencyRuleTests.cs`, the class remarks recording the
elision measurement; `docs/DEPENDENCY-LICENSES.md` section 3.1 for the enumeration shape.

**Out of scope.** Calling `AddOpenIddict`, which is S-010.

---

## S-009. Decide where the `AddOpenIddict` block splits at the persistence boundary

**Status:** blocked · **Blocked by:** S-007 · **Unblocks:** S-010

**The contradiction, quoted from both sides.**
`docs/design/01-foundations.md:98-99` says `Core` "depends only on `Abstractions` plus the protocol
engine" and "must not reference any adapter, database provider, or cloud SDK".
`docs/design/04-core-protocol.md:66-68` writes the wiring as
`.AddCore(o => o.UseEntityFrameworkCore().UseDbContext<OpenIddictDbContext>().UseQuartz())`.
`UseEntityFrameworkCore` is persistence and `UseQuartz` is scheduling, so the block cannot live
whole inside `Core`.

**What is already settled and must not be re-litigated.** ADR-0096 decision 4 of the 2026-08-08
session established that `Core` ships `AddNamiIdentity()`, which calls `AddOpenIddict()` inside
itself, and that the host calls only `AddNamiIdentity()`. So the question is not whether `Core`
calls the engine. It is which fluent segments belong to `Core` and which to the persistence adapter.

**End state.** A statement exists, in the layer entitled to make it, of which segments of the block
belong to which assembly. If the answer turns out to be a decision rather than a realization, it is
an ADR; if it is a realization of ADR-0024 and ADR-0027, it is a design correction. The seed says
which it concluded and why.

**Verification.** `bash scripts/check-adrs.sh`, and the claim is checkable by reading: no document
asks `Core` to call a persistence-configuring method.

**Sources.** `docs/design/01-foundations.md:98-99`; `docs/design/04-core-protocol.md:66-68`;
`docs/adr/0024-architecture-style.md:47`; `docs/adr/0027-packaging-and-distribution.md:35`.

**Out of scope.** Writing the wiring, which is S-010.

---

## S-010. Wire the engine inside `AddNamiIdentity`

**Status:** blocked · **Blocked by:** S-008, S-009 · **Unblocks:** S-011, S-016

**End state.** `AddNamiIdentity` calls `AddOpenIddict()` and configures the segments S-009 assigned
to `Core`. Every API name written is read at 7.6.0 rather than carried from a document that read
7.5.0, and the seed says where each was read. The options this repository already fixed are applied:
the endpoint URIs, the flows, and the token formats named in design 04 section 3.

**Verification.** All nine gates. Plus a unit fact per configured value that a later edit could
change silently, on the same reasoning that made the options defaults worth pinning: measured
2026-08-08, a changed default produced a green build, a green format run, and a byte-identical
public API file.

**Sources.** `docs/design/04-core-protocol.md` section 3, the implementer source of record for the
block; `docs/adr/0021-openiddict-version-adaptation.md:46` for the handler-order rules.

**Out of scope.** Any slice, which is S-016. The contract-regression suite, which is S-011.

---

## S-011. Stand up the contract-regression suite ADR-0021 part C requires

**Status:** blocked · **Blocked by:** S-010 · **Unblocks:** nothing yet

**Why it is blocked rather than first.** The suite asserts seam behaviour on the pinned engine, and
most seams need the engine wired before there is behaviour to assert. That is also why the 7.6.0
bump proceeds without it, and why S-003 records the gap instead of hiding it.

**End state.** A test project exists that asserts at least the seams reachable from what S-010
wired, it runs in CI as its own job, and ADR-0021's Confirmation is updated to say which part of its
build-time item is now closed and which is not. Each assertion is watched to fail against a planted
break before it is believed, per the rule this repository reached three times by three mechanisms.

**Verification.** The suite runs in CI. Every assertion has a recorded planted break and the run log
line showing it failed.

**Sources.** `docs/adr/0021-openiddict-version-adaptation.md:43` for what the suite must cover, and
`:62` for the build-time item; `docs/design/22-openiddict-seam-catalogue.md` for the seam registry;
`tests/CLAUDE.md`, the section on a rule you have not failed on purpose.

**Out of scope.** Conformance testing, which part D names separately and which needs a running host.

---

## S-012. Reconcile design 01's context count against its own table

**Status:** open · **Blocked by:** none · **Unblocks:** nothing yet

**The defect, counted 2026-08-08.** `docs/design/01-foundations.md` section 2 says "the **four**
database contexts" once. Six other places say "the **five** contexts": the package table in section
3.1, section 4's own opening sentence, the composition-root paragraph in section 5.1, the first-run
diagram in section 5.2, the patterns note in section 6, and the integration-test bullet in section
9. Section 4's table then lists **four** rows.

**The fifth is real, not a miscount.** `ControlPlaneTenantDbContext` appears in
`docs/adr/0001-multi-tenant-isolation-model.md`,
`docs/adr/0018-dbcontext-pooling-for-pool-mode.md`, `docs/design/02-data.md` five times,
`docs/design/10-email-notification.md`, and
`docs/design/17-erasure-and-data-subject-rights.md`.

**End state.** Section 4's table lists five contexts with their scope and pooling posture, matching
`design/02-data.md`, and section 2's "four" is corrected. The seed says which of the two numbers was
the transcription and which was derived, because a count and a list disagreeing means one of them was
copied.

**Verification.** `git grep -c "five contexts" docs/design/01-foundations.md` and a read of section
4's table row count agree. `markdownlint-cli2` and the guardrail.

**Sources.** As enumerated above.

**Out of scope.** The schema of the fifth context, which `design/02-data.md` owns.

---

## S-013. Give the provider-selector key the decided form and an owner

**Status:** open · **Blocked by:** none · **Unblocks:** nothing yet

**Two defects in one place.** `docs/adr/0065-coding-and-naming-conventions.md:78` fixes
configuration keys as `Nami:Section:Key` and makes them a public contract under ADR-0044 parameter
I. `Cloud:Provider` has no `Nami:` prefix. And
`docs/design/10-email-notification.md:168` calls it "the `Cloud:Provider` selector shape whose SSOT
is the foundations config" while `docs/design/12-key-management.md:683` names a shared
`CloudProviderSelector`, yet `docs/design/01-foundations.md` section 5.1 says only "A provider
selector reads one configuration value" and names neither the key nor the type. Two citations
resolve to a document that does not hold the claim.

**End state.** Design 01 section 5.1 names the key and the type, in the ADR-0065 form, or the two
citing documents are corrected to point at whatever does own them. Either way, no document attributes
a fact to design 01 that design 01 does not carry.

**Verification.** `git grep -n "Cloud:Provider"` returns only the decided spelling.
`bash scripts/check-adrs.sh` and `markdownlint-cli2`.

**Sources.** As quoted above.

**Out of scope.** Implementing the selector.

---

## S-014. Place the three builder calls that exist in no document here

**Status:** open · **Blocked by:** none · **Unblocks:** nothing yet

**The absence, with its search.** Counted 2026-08-08 over `docs/` excluding `BUILD-PLAN.md`, three
fluent calls the module set will need appear in **zero files**: an external-provider registration,
a scope-seeding call, and a client-seeding call. For the external-provider call five spellings were
tried and all five returned zero: `AddExternalProvider`, `AddExternalIdentityProvider`,
`AddFederation`, `AddExternalIdP`, `AddOidcProvider`. The **concept** does resolve, in
`docs/adr/0002-federation-external-idp-integration.md` and four other files, and naming that stops a
later reader re-finding it as coverage.

**End state.** Each of the three has a name and an owning design: the external-provider call in
`design/09-federation-and-claims-profile.md`, and the two seeding calls in
`design/23-configuration-and-client-declaration.md`. Design 01 section 3.4's sample is extended so
it stops showing four of the module calls and hiding the rest.

**Verification.** Each name resolves to exactly one owning design. `bash scripts/check-adrs.sh`.

**Sources.** `docs/design/01-foundations.md` section 3.4;
`docs/adr/0028-user-management.md:38` for the shape an existing module call takes;
`docs/adr/0096-fluent-builder-api-surface.md` parameter G for why they are extension methods.

**Out of scope.** Implementing any of them.

---

## S-015. Re-own design 04's boot-validation citation

**Status:** open · **Blocked by:** none · **Unblocks:** nothing yet

**The defect.** `docs/design/04-core-protocol.md:811` says the `Nami:Protocol:*` keys "are validated
at boot (ADR-0052)". ADR-0052's five parameters A through E are entirely about client and scope
declaration, and its subject is `ClientDefinition` and `ScopeDefinition`. It carries no clause about
protocol configuration keys.

**What is not the answer.** `docs/adr/0043-security-hardening-invariants-startup-check.md` is the
nearest live mechanism and is a different thing: it asserts a fixed list of named security
invariants, not the presence of a configuration value. Searched 2026-08-08 across `docs/adr/`, nine
spellings for an options-validation mechanism returned zero files each: `IValidateOptions`,
`ValidateOnStart`, `ValidateDataAnnotations`, `AddOptions`, `missing value`, `required value`,
`fail at start`, `fails at start`. A tenth, `OptionsBuilder`, returned one file, and the hit is
`DbContextOptionsBuilder` at `docs/adr/0036-database-key-strategy-uuidv7.md:40`, an EF Core type.

**End state.** The citation names an owner that carries the claim, or the claim is marked not
verified with its search. ADR-0096 parameter C decided the mechanism for `NamiIdentityOptions` only
and says so, so it is a precedent and not an answer for the protocol keys.

**Verification.** `bash scripts/check-adrs.sh`, and a read of the cited ADR confirming it holds the
claim.

**Sources.** `docs/design/04-core-protocol.md:811`; `docs/adr/0052-ergonomic-config-layer.md`
parameters A through E; `docs/adr/0096-fluent-builder-api-surface.md` parameter C.

**Out of scope.** Deciding the mechanism for the protocol keys, which is design 04's own change and
may need an ADR.

---

## S-016. Define what the first slice is

**Status:** blocked · **Blocked by:** S-010 · **Unblocks:** nothing yet

**The absence, with its search.** Counted 2026-08-08 with `git grep -i` over every tracked file
except `BUILD-PLAN.md`, `first slice` returned **zero**. `docs/adr/0024-architecture-style.md:47`
says the IdP-core "slice" is "the handler pipeline plus a few domain services (claims, consent,
keys)", which describes the layer rather than naming a first unit of work.

**The adjacent question, partly answered.** `docs/adr/0027-packaging-and-distribution.md:85` asks
which assembly carries the five pass-through controllers. `ADR-0024:47` places "the authorization
controller, endpoints, and `AddOpenIddict()` wiring in the server host", which answers one of the
five; `0024:51` names all five endpoints and no assembly.

**End state.** One slice is named, with its owning design, and it is small enough to be a seed of its
own. The naming says whether it was decided or transcribed.

**Verification.** The named slice resolves to a design that describes its request, handler, and
response, per `docs/adr/0024-architecture-style.md:44`.

**Sources.** As quoted above.

**Out of scope.** Writing the slice. That is the seed this one creates.

---

## S-017. Assign a configuration key to the nine options that have none

**Status:** open · **Blocked by:** none · **Unblocks:** nothing yet

**Not a defect, a consequence that was chosen.**
`docs/adr/0096-fluent-builder-api-surface.md` parameter F declined to mint eleven public
configuration contracts in one change, and left each key to the design owning its member's subject.
`docs/design/04-core-protocol.md` section 6 has assigned three. Nine members are therefore settable
in code only, and an operator cannot change them without recompiling.

**End state.** Each of the nine either has a key in the `Nami:Section:Key` form, assigned by the
design owning its subject, or is recorded as deliberately code-only with the reason. The seed does
not have to close all nine; it may split into one seed per owning design, and doing so is the
expected outcome rather than a failure to finish.

**Verification.** Every member of `NamiIdentityOptions` either appears in a configuration-key table
or is named in a list of code-only options.

**Sources.** `docs/adr/0096-fluent-builder-api-surface.md` parameter F;
`docs/adr/0065-coding-and-naming-conventions.md:78`;
`docs/adr/0044-public-api-stability-and-semver.md` parameter I.

**Out of scope.** Changing any default.

---

## S-018. Move the architecture layer's four engine-version statements to the new pin

**Status:** done · **Blocked by:** S-002, which is done · **Unblocks:** nothing yet

**Why this seed exists at all.** S-001 found four bucket-A lines that no seed owned. The reason they
were missed is the finding worth keeping: S-001's own search spelling was `7.5.0`, and all four write
`OpenIddict 7.5` with no patch number, so the search that was supposed to find every live-pin
statement returned none of them.

**End state.** All four lines name the pin S-002 landed:

| Line | What it says today |
|---|---|
| `docs/architecture/01-introduction-scope.md:14` | "built on OpenIddict 7.5" |
| `docs/architecture/03-drivers-and-constraints.md:114` | a table row, "OpenIddict 7.5, version-pinned and seam-isolated" |
| `docs/architecture/04-system-context.md:21` | a mermaid node label, "on OpenIddict 7.5 and .NET 10" |
| `docs/architecture/README.md:10` | "built on OpenIddict 7.5 and .NET 10" |

**This layer may not decide, so it may not disagree either.** `docs/architecture/README.md:32-33`
says this layer "points into them as the authoritative source, and where it disagrees with one of
them, this layer is the bug". A version statement here that outlives the pin is exactly that
disagreement, so the fix is a transcription from ADR-0061 and not a judgement.

**Verification.** `git grep -nP "OpenIddict 7\.5(?!\.0)" -- docs/architecture/` returns only
`09-runtime-flow-views.md:296`, which is bucket C and belongs to S-005. `bash scripts/check-adrs.sh`
after `git add`, `python3 scripts/check-decisions-index.py`, and `markdownlint-cli2` with its file
count cross-checked against `git ls-files '*.md'`. The mermaid label at `04:21` is inside a fenced
block, so read it rather than trusting a link checker, which is the trap `docs/design/CLAUDE.md`
records for pointers inside fences.

**Sources.** The four lines above, each read 2026-08-08; `docs/architecture/README.md:31-35` for why
this layer may not hold a version the ADR does not;
`docs/adr/0061-technology-stack-of-record.md:49` for the row that is authoritative once S-004 lands.

**Out of scope.** `09-runtime-flow-views.md:296`, which is a source-read claim rather than a pin
statement. Every other version in these four files, including .NET 10.

### S-018 result, measured 2026-08-08

All four lines read 7.6, and the seed's verification passes exactly as written:
`git grep -nP "OpenIddict 7\.5(?!\.0)" -- docs/architecture/` returns only
`09-runtime-flow-views.md:296`, which is bucket C and belongs to S-005.

**Two 8.0 forward references were read rather than assumed unaffected.**
`01-introduction-scope.md:137` waits for "the native OpenIddict 8.0 implementation" of dynamic client
registration, and `09-runtime-flow-views.md:716` carries a decommission marker for "OpenIddict 8.0's
native implementation" of back-channel logout. Both name 8.0 and neither names a preview, so the slip
S-003 found, both features moving from milestone `8.0.0-preview.2` to `8.0.0-preview.3`, does not reach
either line. This confirms in the architecture layer what S-003's amendment claimed about ADR-0014 and
ADR-0019.

**The finding is one row below the edit, and it is not a version.**
`03-drivers-and-constraints.md:116` reads "EF Core 10 with Npgsql, pooled `DbContext`". ADR-0018 is
titled "Register the Pool-mode OpenIddict DbContext **non-pooled** in v1". The row is inverted on a
tenant-isolation property, and it sits two lines under the row this seed corrected. It is **not** a
version statement, so it is outside this seed by its own scope line, and it is now **S-024**. Bundling a
correctness fix about tenant leakage into a version transcription would have hidden it in this commit's
diff.

**Verification.** `bash scripts/check-adrs.sh` after `git add`,
`python3 scripts/check-decisions-index.py`, and `markdownlint-cli2` with its file count cross-checked
against `git ls-files '*.md'`. The mermaid label at `04:21` sits inside a fenced block where no link
checker reaches, so it was read directly, which is the trap `docs/design/CLAUDE.md` records for pointers
inside fences.

---

## S-024. Correct view 03's inverted `DbContext` pooling row, and read the other eight

**Status:** done · **Blocked by:** none · **Unblocks:** nothing yet

**The contradiction, quoted from four sides.**

| Source | What it says |
|---|---|
| `docs/architecture/03-drivers-and-constraints.md:116` | "EF Core 10 with Npgsql, pooled `DbContext`" |
| `docs/adr/0018-dbcontext-pooling-for-pool-mode.md:10` | titled "Register the Pool-mode OpenIddict DbContext **non-pooled** in v1, with pooled-plus-mutable deferred" |
| `docs/adr/0061-technology-stack-of-record.md:51` | "DbContext pooling is **per context**, and the tenant-scoped hot path is deliberately **not** pooled in v1" |
| `docs/architecture/21-performance-scalability.md:81` | "For the Pool-mode operational context, **v1 registers the DbContext non-pooled**" |

**The architecture layer therefore disagrees with itself**, view 03 against view 21, which
`docs/design/CLAUDE.md` names as the cheapest signal that one of them was transcribed rather than read.
And `docs/architecture/README.md:32-33` settles which is the bug: this layer "points into them as the
authoritative source, and where it disagrees with one of them, this layer is the bug".

**This is a known defect that a previous pass missed, and both facts are already written down.**
`docs/architecture/07-container-view.md:288-290` records that "both this view and the ADR-0061 stack
table had been describing the stack as `pooled DbContext` when the ADR that owns the decision is titled
for the opposite", and that a DbContext pooling section was added to fix it. That pass, on 2026-07-25,
corrected view 07 and ADR-0061's table and left view 03's constraint table alone.
`docs/adr/0061-technology-stack-of-record.md:118` then predicted exactly this: "the remaining rows
deserve the same pass before GA".

**Why an inverted word matters more here than a wrong version.** ADR-0018 exists because spike A-4 test
T7, run 2026-07-06, found that "naive pooled reuse leaked the tenant across requests, including through
OpenIddict's internal `SaveChanges`" (`0018:62`). A constraint table telling an implementer the stack
uses a pooled `DbContext` is telling them to do the thing that leaked tenants.

**End state.**

- `03:116` agrees with ADR-0018 and with ADR-0061's row, and it distinguishes pooling per context from
  the tenant-scoped hot path rather than asserting one word for both.
- **The other eight rows of that table are each read against their owning ADR**, and the seed states
  which were confirmed and which were corrected. The table has nine data rows at lines 113 to 121,
  counted 2026-08-08: Runtime, Protocol engine, Database engine, ORM and driver, Primary keys, Signing
  baseline, Audit integrity, Logging and telemetry, and Infrastructure as code. This is the pass
  `0061:118` asked for, scoped to one table so it fits one sitting.
- A row that survives the read is recorded as confirmed rather than left silent, because a reader
  cannot otherwise tell a read row from an unread one.

**Verification.** `bash scripts/check-adrs.sh`, `python3 scripts/check-decisions-index.py`, and
`markdownlint-cli2`. Then the substantive check, which no gate performs: for each of the nine rows,
open the ADR its last cell cites and confirm the row's claim appears there. `git grep -niE "pooled.{0,25}dbcontext|dbcontext.{0,25}pooled" -- docs/architecture/`
returns no line asserting a pooled `DbContext` as the stack's posture.

**Sources.** The four rows quoted above, each read 2026-08-08;
`docs/architecture/07-container-view.md:288-290` for the pass that missed this table;
`docs/adr/0061-technology-stack-of-record.md:118` for the prediction;
`docs/adr/0018-dbcontext-pooling-for-pool-mode.md:62` for the T7 measurement;
`docs/architecture/README.md:32-33` for which layer is the bug.

**Out of scope.** ADR-0018 and ADR-0037, which are correct and are the sources here. Every other table
in view 03. The pooled-plus-mutable deferral, which ADR-0018 owns as A-4b. The same defect in
`docs/adr/0036-database-key-strategy-uuidv7.md:76` and
`docs/adr/0066-design-patterns-vocabulary-and-pragmatic-use.md:51`, which is **S-026**, because those
are ADRs and this repository authors one ADR change per commit.

**Do not repair this by writing "non-pooled", which would be wrong in the other direction.** Read at
`docs/design/02-data.md:55-59` on 2026-08-08, three global contexts are pooled,
`IdentityDbContext`, `DataProtectionDbContext`, and `ControlPlaneDbContext`, and the two tenant-scoped
ones are not. ADR-0061's row carries the accurate framing to follow.

### S-024 result, measured 2026-08-08

Row 116 now matches ADR-0061's own row, which section 4.1 already names as its authority. The
nine-row pass is done: **eight confirmed, one corrected**, and this was the sixth and last known
instance of the inversion.

**The eight confirmations carry their evidence, because a read row and an unread row look identical
otherwise.** The view's own section 6 records each one by `file:line`, so the pass is checkable rather
than claimed.

| Row | Owning ADR | Read at |
|---|---|---|
| Runtime | ADR-0030 | `0030:14` |
| Protocol engine | ADR-0021 | `0061:49`, `0021:14` |
| Database engine | ADR-0037 | its title, `:39` for `FORCE ROW LEVEL SECURITY`, `:41` for PostgreSQL 18 |
| ORM and driver | ADR-0037, ADR-0018 | **corrected** |
| Primary keys | ADR-0036 | `0036:42` for the one `bigint` exception, `0036:34` for the `seq bigint` column |
| Signing baseline | ADR-0005 | `0005:39`, which states the row almost word for word |
| Audit integrity | ADR-0008 | `0008:35` for the keyed canonicalized chain, `0008:86` for `HMAC_k`, the application-held key, and prev-first operands |
| Logging and telemetry | ADR-0022 | its title |
| Infrastructure as code | ADR-0023 | `0023:6` and `:26`, both writing "OpenTofu (MPL-2.0, Linux Foundation)" |

**The pass found one thing the row-read could not fix, now S-027.** OpenTofu is MPL-2.0, and
`0026:36` routes MPL-2.0 through "Architect and Legal approval recorded as an exception". Searched
2026-08-08, OpenTofu has **no row anywhere** in `docs/DEPENDENCY-LICENSES.md`, while five comparable
external tools do: Apache JMeter, cosign, Trivy, gitleaks, and OWASP ZAP. Row 121 itself is correct,
ADR-0023 supporting it as written, so this is a completeness gap in the licence record rather than a
defect in this table.

**The inversion count is closed at six.** `0061:145` and `architecture/07-container-view.md:288-290`
are the first two, repaired 2026-07-25. ADR-0066, ADR-0036, and ADR-0033 are the third, fourth, and
fifth, repaired 2026-08-08 by S-026. This row is the sixth. `0061:118` predicted the set when it said
the remaining rows deserved the same pass.

**Verification.** `bash scripts/check-adrs.sh` after `git add`,
`python3 scripts/check-decisions-index.py`, and `markdownlint-cli2`, all green. The substantive check
was the nine reads above, which no gate performs. `git grep -niP "(ADR-)?0018" -- docs/ | grep -iP "pool"`
returns no line labelling ADR-0018 by the pooled option, only the correct usages and the correction
records.

---

## S-025. Un-pin ADR-0030's seam range, as ADR-0021 already did to its own

**Status:** open · **Blocked by:** none · **Unblocks:** nothing yet

**The defect is a copy of one already fixed, in the ADR that shares the suite.**
`docs/adr/0030-dotnet-version-upgrade.md:40` writes that each .NET bump runs "the same
contract-regression suite (seams S1 through S34)". `docs/adr/0021-openiddict-version-adaptation.md:159`
un-pinned exactly that range on 2026-08-01, and states the reason in its own words: "Pinning the upper
bound in an accepted ADR quietly discouraged the thing the ADR exists to encourage, since adding a seam
meant editing a binding document. The catalogue now owns the enumeration and this ADR owns the rule."

**Why it survived.** That amendment says the stale text was fixed "in two places here and five in the
catalogue". ADR-0030 was neither, so it kept the pinned range while the ADR it calls its sibling gave
it up. Measured 2026-08-08, the catalogue's highest registered row is **S36**, so ADR-0030 is two
seams behind and cites a ceiling that a new seam would have to edit a binding document to raise.

**What is already correct, named so nobody re-fixes it.**
`docs/architecture/24-glossary.md:197` records the same repair for itself in the past tense: it "said
`S1 to S34` until 2026-08-01, one seam behind the registry", and now reads "Thirty-eight rows, numbered
S1 to S36". That entry also states the trap worth carrying: reading the highest number as the total
undercounts it, because two rows are sub-lettered.

**What is not stale, and must not be edited.**
`docs/design/22-openiddict-seam-catalogue.md:60` says the **external design corpus** contains a table
registering `S1`-`S34`. That is a fact about another tree, not about this repository's registry, so it
stays exactly as written. A search for `S34` returns it, so a later agent will meet it and should be
able to tell from this seed that it is not the defect.

**End state.** `0030:40` no longer names an upper bound. It refers to the seam catalogue as the owner
of the enumeration, in ADR-0021's own wording or a stated equivalent, and the change is recorded in
ADR-0030's own More Information style. After the change, `git grep -nE "S1[^0-9].{0,12}S34|S34\b"` over
`docs/` excluding the work queue returns only three lines: ADR-0021's amendment, the glossary's
past-tensed record, and design 22's statement about the corpus. All three are correct records rather
than live claims.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, `bash scripts/test-check-adrs.sh`, and
`python3 scripts/check-decisions-index.py`. Check 3 compares the index row status against the
frontmatter status, so confirm neither moved, and ADR-0030 carries `stack-record: true`, so confirm its
ADR-0061 row is untouched for Check 4. Then run the grep above and read all three survivors.

**Sources.** `docs/adr/0030-dotnet-version-upgrade.md:40`;
`docs/adr/0021-openiddict-version-adaptation.md:159` for the un-pinning and its reason;
`docs/architecture/24-glossary.md:197` for the count and the sub-lettering trap;
`docs/design/22-openiddict-seam-catalogue.md:60` for the corpus statement that is not the defect. Every
one read 2026-08-08.

**Out of scope.** Counting the seams, which the catalogue owns and which the glossary already dates.
Every other clause of parameter D, including the suite itself, which is S-011.

---

## S-026. Correct the two ADRs that label ADR-0018 by the option it declined

**Status:** done · **Blocked by:** none · **Unblocks:** nothing yet

**Two defects, one class, and the class already has a repair on record.** ADR-0018 is titled "Register
the Pool-mode OpenIddict DbContext **non-pooled** in v1, with pooled-plus-mutable deferred", and its
Option A, the pooled one, is the deferred A-4b. Two ADRs name it by that declined option.

| Line | What it says | Why it is wrong |
|---|---|---|
| `docs/adr/0036-database-key-strategy-uuidv7.md:76` | Related decisions: "ADR-0018 (the pooled DbContext on the same PostgreSQL/EF stack)" | labels the ADR by the option it declined |
| `docs/adr/0066-design-patterns-vocabulary-and-pragmatic-use.md:51` | "**Factory** for the pooled `DbContext` in Pool mode (ADR-0018)", under the heading "patterns already in deliberate use" | the Factory comes from ADR-0018 Option A, `AddPooledDbContextFactory<T>` plus a scoped `IDbContextFactory<T>`, which is deferred. v1 registers a scoped `AddDbContext`, which needs no factory |

The second is the worse one, because it lists a pattern as in deliberate use when the option that would
use it was not taken.

**"Non-pooled" is not the correction either, and this is the trap.** Pooling **is** used, per context.
Read at `docs/design/02-data.md:55-59` on 2026-08-08, three global contexts are pooled,
`IdentityDbContext`, `DataProtectionDbContext`, and `ControlPlaneDbContext`, while the two
tenant-scoped ones are not. ADR-0061's row states the accurate framing: "DbContext pooling is **per
context**, and the tenant-scoped hot path is deliberately **not** pooled in v1". A blanket "non-pooled"
would be a second wrong claim in the other direction.

**Why this class matters more than a wrong version.** ADR-0018 exists because spike A-4 test T7, run
2026-07-06, found that "naive pooled reuse leaked the tenant across requests, including through
OpenIddict's internal `SaveChanges`" (`0018:62`).

**This is the third and fourth instance of one defect, and the first two are already recorded.**
`docs/adr/0061-technology-stack-of-record.md:145` records the correction of its own table on 2026-07-25,
and `docs/architecture/07-container-view.md:288-290` records the same for that view: "both this view and
the ADR-0061 stack table had been describing the stack as `pooled DbContext` when the ADR that owns the
decision is titled for the opposite". `0061:118` then predicted the remaining rows deserve the same
pass. **Seed S-024 owns the fifth instance**, `docs/architecture/03-drivers-and-constraints.md:116`,
which is in the architecture layer rather than in an ADR.

**End state.** Both lines describe ADR-0018 by what it decided. `0036:76` names it without asserting
pooling as the posture. `0066:51` either drops the Factory entry, or re-attributes it to something that
genuinely uses a factory today, or states that it is the deferred A-4b shape rather than a pattern in
use. The seed says which of the three it concluded and why. Each change is recorded in its own ADR's
maintenance style.

**It lands as two commits, one per ADR**, because this repository authors one ADR change per commit.
That is not two seeds: it is one repair with one reasoning, and splitting it would put the same
paragraph in two places.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, `bash scripts/test-check-adrs.sh`, and
`python3 scripts/check-decisions-index.py`. Both ADRs carry `stack-record: true`, so confirm their
ADR-0061 rows are untouched for Check 4, and neither frontmatter `status` moved for Check 3. Then
`git grep -niE "pooled[^.]{0,30}dbcontext|dbcontext[^.]{0,30}pooled" -- docs/adr/` returns only
ADR-0018's own text, ADR-0018's index row, ADR-0061's past-tensed correction record, and whatever
wording this seed chose.

**Sources.** The two lines above; `docs/adr/0018-dbcontext-pooling-for-pool-mode.md:10` for the title,
`:29-30` for the two options, `:35` for which was chosen, and `:62` for the T7 measurement;
`docs/design/02-data.md:55-59` for the per-context matrix;
`docs/adr/0061-technology-stack-of-record.md:51` for the accurate framing, `:145` for the first
correction, and `:118` for the prediction; `docs/architecture/07-container-view.md:288-290` for the
second. Every one read 2026-08-08.

**Out of scope.** `docs/architecture/03-drivers-and-constraints.md:116`, which is S-024. Every correct
use of the word pooled, and there are many: ADR-0018 itself, views 21 and 23, design 01, design 02,
design 10, and seam S24 in design 22 all distinguish the two cases properly and must not be swept.

### S-026 result, measured 2026-08-08

Two commits, one per ADR, as planned. The two repairs came out different, and the reason is worth
carrying.

**`0066:51` was removed, not re-labelled, and this ADR's own rule decided that.** Its pragmatic-use
rule says "a pattern applied without a problem to solve is a defect, not good design", and the heading
above the list claims the patterns are "already in deliberate use". An entry with no use contradicts
both, so a re-label would have left the contradiction in place. Nothing is lost: `0018:41` records the
factory shape as "the A-4b pattern held for later", and ADR-0066's own "Where the guidance lives"
section says each pattern is owned by the ADR that applies it and that this one "does not override
them".

**The absence was searched properly, and the method is in the amendment.** Using `git grep -P`,
`\bFactor(y|ies)\b` returned **exactly one** line across `docs/` excluding the work queue and this
tracker, which was the entry itself, and **zero** across `src/` and `tests/`.
`AddPooledDbContextFactory` occurs only at `0018:17`, defining the term, and `0018:29`, Option A. The
three genuinely pooled contexts use `AddDbContextPool` (`docs/design/02-data.md:1164-1166`), which
takes no factory. So there was nothing to re-attribute the entry to.

**A false zero nearly hid it, and it is recorded.** The same search written `git grep -cE
"\bFactory\b"` returns 0 against a file that contains the word. That is the word-boundary trap
`docs/CLAUDE.md` records for this clone, and an absence written that way reports zero for every term
whether present or not. The method was proven on the target file before being trusted: `-E` gave 0 and
`-P` gave 1.

**`0036:76` was re-worded rather than removed**, because a Related-decisions bullet has to say what the
relation is. It now names per-context pooling with the tenant-scoped context non-pooled, matching
ADR-0061's framing.

**Neither frontmatter was edited, and ADR-0066's is now knowingly loose.** Its `consulted:` line groups
ADR-0018 among "the pattern-applying ADRs", which over-includes it after the removal. ADR-0018 genuinely
was consulted on 2026-07-18, so the record of that reading stays. The amendment says so out loud, so the
looseness is a recorded decision rather than a silent inconsistency for a later agent to "fix".

**The seed's own verification found a third ADR, so it landed as three commits rather than two.** The
verification asked for the `docs/adr/` sweep to come back clean.
`docs/adr/0033-key-scope-isolation-model.md:77` wrote "ADR-0018 (pooled DbContext, where a Silo's own
connection gives one keyset per instance)", the same defect in the same shape. It was not found by
reading. **A verification that only confirms what the author already believed would have missed it**,
and that is the reusable lesson from this seed rather than any of the three wordings.

**The count, stated because this seed kept mis-stating it.** **Six** instances are known:

| # | Where | Repaired |
|---|---|---|
| 1 | ADR-0061's stack table, recorded at `0061:145` | 2026-07-25 |
| 2 | `architecture/07-container-view.md`, recorded at `:288-290` | 2026-07-25 |
| 3 | `ADR-0066:51`, the `Factory` entry | 2026-08-08, this seed |
| 4 | `ADR-0036:76`, a Related-decisions bullet | 2026-08-08, this seed |
| 5 | `ADR-0033:77`, a Related-decisions bullet | 2026-08-08, this seed |
| 6 | `architecture/03-drivers-and-constraints.md:116` | **open, S-024 owns it** |

A draft of the ADR-0036 amendment said five instances and was corrected before committing.
`0061:118` predicted the whole set, saying the remaining rows deserved the same pass.

**What is not a defect is enumerated once, in ADR-0036's amendment, and not copied.** Searched
2026-08-08, about thirty lines across `docs/` name ADR-0018 near the word pool and only rows 5 and 6
above are wrong. The rest are right in three distinct ways: "pooling" used as the name of the ADR's
subject, an explicit "non-pooled" or "pooled-plus-mutable ... post-v1", or the PgBouncer connection
pooler, which is a different subject. A second copy of that list would be a second place to be wrong,
so ADR-0033's amendment points at it rather than repeating it.

**Verification.** For each of the three commits: `bash scripts/check-adrs.sh` after `git add`,
`bash scripts/test-check-adrs.sh`, `python3 scripts/check-decisions-index.py`, and `markdownlint-cli2`,
all green. All three ADRs carry `stack-record: true` and no frontmatter moved, so Checks 3 and 4 see the
unchanged "Architecture", "Primary keys", and "Key management" rows in ADR-0061. ADR-0066's in-use list
was read back and holds seven entries. After all three commits,
`git grep -niP "(ADR-)?0018" -- docs/ | grep -iP "pool"` returns no line labelling ADR-0018 by the
pooled option except `architecture/03-drivers-and-constraints.md:116`, which is S-024's.

## S-019. Amend ADR-0030's stack sentence to the new pin

**Status:** done · **Blocked by:** S-002, which is done · **Unblocks:** nothing yet

**End state.** `docs/adr/0030-dotnet-version-upgrade.md:14` names the engine version S-002 landed.
The line reads "Nami pins .NET 10 (LTS), the runtime foundation of the entire stack (ASP.NET Core, EF
Core 10, OpenIddict 7.5, Npgsql, ASP.NET Core Identity, Finbuckle)", so the edit is one item inside a
list. The change is recorded in this ADR's own maintenance style.

**Why it is its own seed.** One ADR per commit. And this ADR's subject is the .NET pin, not the
engine pin, so a reader looking for engine-version work would not open it. That is precisely why it
was missed until S-001 searched the shorter spelling.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, and
`python3 scripts/check-decisions-index.py`. Check 3 compares the index row status against the
frontmatter status, so confirm neither moved. Read the line rather than the exit code, because no
gate here reads a version.

**Sources.** `docs/adr/0030-dotnet-version-upgrade.md:14`, read 2026-08-08.

**Out of scope.** Every other technology in that sentence. The .NET pin itself, which this ADR
decides and this seed does not touch.

### S-019 result, measured 2026-08-08

`:14` reads "OpenIddict 7.6". The amendment records three things the bump made checkable, and one
defect it is deliberately leaving to S-025.

**Why this line went stale unnoticed, which is the reusable part.** This ADR names the engine version
without owning it: the sentence lists what .NET 10 underpins, so the version is context rather than a
decision. A reader hunting engine-version statements does not open an ADR about the .NET pin, and
S-001's search used the spelling `7.5.0` while this line writes `OpenIddict 7.5`. Both filters missed
it independently.

**1. The bump ran the opposite way from parameter F, and that is complementary rather than a gap.**
Parameter F describes an LTS-led bump, which moves the target framework and the CPM versions
lock-step "in the same beat as ADR-0021". This was an OpenIddict-led bump with no runtime move, which
ADR-0021 parameter D owns. Parameter C's lock-step was checked rather than assumed: the engine's
transitive `Microsoft.Extensions.*` edge moved 10.0.7 to 10.0.10, which stays inside the 10.x band
that parameter aligns to, so no runtime-major alignment was disturbed.

**2. Parameter E's mirror now has something to mirror.** It describes the early-warning branch as
"mirroring the OpenIddict 8.0-preview spike", and until 2026-08-08 no 8.0 preview was recorded as
existing anywhere here. Two are now. The OpenIddict half of that mirror is live; the .NET half still
waits for a .NET 11 preview.

**3. `ADR-0030:79`'s two-knob claim was verified, and it holds.** That entry says the target-framework
knob landed as two properties, `NamiLibraryTargetFrameworks` and `NamiApplicationTargetFramework`,
"both reading `net10.0` today". Both were read at `Directory.Build.props:114` and `:115` on 2026-08-08
and both read `net10.0`. This matters because S-018's conclusion that four 8.0 breaking changes do not
reach Nami rested on the target framework, and it now rests on both knobs rather than one.

**The finding left out, now S-025.** Parameter D writes "seams S1 through S34". ADR-0021 un-pinned
exactly that range on 2026-08-01, and the catalogue registers S36 today. It is a different subject
from this pin, and one ADR change per commit, so it is its own seed and its own commit.

---

## S-020. Amend ADR-0036's live-pin clause to the new pin

**Status:** done · **Blocked by:** S-002, which is done · **Unblocks:** nothing yet

**One line carrying two claims, and only one of them moves.**
`docs/adr/0036-database-key-strategy-uuidv7.md:40` says the key-type mapping was "**Read at
`OpenIddict.EntityFrameworkCore` 7.4.0**, the only version in the local package cache; the pin is
7.5.0 (ADR-0061), so re-confirm on the pinned package at M1". The dated read at 7.4.0 is a
measurement and stays. The clause "the pin is 7.5.0" is present tense about the pin and moves.

**End state.** The clause names the pin S-002 landed. The 7.4.0 read keeps its version, its wording,
and its "re-confirm at M1" instruction, because that instruction is now more owed rather than less:
the pin has moved twice past the version actually read.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, and
`python3 scripts/check-decisions-index.py`. Then read the line and confirm that the sentence still
distinguishes the version read from the version pinned. A single edit collapsing the two into one
version is the failure mode here, and it would delete a measurement.

**Sources.** `docs/adr/0036-database-key-strategy-uuidv7.md:40`, read 2026-08-08;
`docs/CLAUDE.md`, the section on a pointer at a file you are deleting from, for why the 7.4.0 read
stays in the past tense.

**Out of scope.** Re-confirming the key-type mapping against the new pin. That is the M1 item the
line already names, and it needs a restored package rather than a document edit.

### S-020 result, measured 2026-08-08

Two things changed on that line, not one, and the second was not in the seed.

**1. The version, as asked.** The clause reads "the pin is `[7.6.0]`".

**2. The citation owner, which the seed did not ask for and which was wrong.** The old clause
attributed a three-part version to ADR-0061. Read 2026-08-08, ADR-0061's stack row says "OpenIddict
7.6" and has never carried a patch number: every version in that table is major or minor only. So the
pointer resolved to a real file that did not hold the claim, which is the citation shape this
repository calls the dangerous one. It now cites ADR-0021 parameter A, which owns the exact pin
including its bracket form. Fixing the version without fixing the owner would have left a wrong
citation looking freshly checked.

**3. The 7.4.0 read is confirmed, not edited, and the gap it names has widened.** The sentence says
the mapping was read at `OpenIddict.EntityFrameworkCore` 7.4.0, "the only version in the local package
cache". Checked 2026-08-08, `~/.nuget/packages/openiddict.entityframeworkcore/` still holds only
`7.4.0`, so the measurement stays true and keeps its wording. The distance changed: the read is now
two minor versions behind the pin rather than one. The "re-confirm at M1" instruction is more owed,
and the moment it becomes possible is when the first `PackageReference` restores the engine, which is
S-008. S-006 is the related decision, the offline tree that could have answered it without a restore
no longer matching the pin.

**The finding left out, now S-026.** This ADR's Related-decisions bullet calls ADR-0018 "the pooled
DbContext". ADR-0018 is titled for the opposite. It is a different subject from the pin, so it is its
own seed and its own commit.

---

## S-021. Re-derive ADR-0093's verbatim quotation of ADR-0021

**Status:** done · **Blocked by:** S-003, which is done · **Unblocks:** nothing yet

**Blocked by S-003 and not by S-002, and the distinction is the whole seed.** The quotation only
breaks if S-003 actually edits the line it quotes. So this seed cannot be written until S-003 has
landed, and it must read the final text rather than predict it.

**The coupling, quoted from both sides.**
`docs/adr/0093-warnings-as-errors.md:150` writes that ADR-0021's playbook "already instructs the
project to `clear obsolete warnings on 7.5 now`" and cites `0021:44`.
`docs/adr/0021-openiddict-version-adaptation.md:44` is the source of that string and is bucket A in
S-001's classification.

**End state.** ADR-0093's quotation matches ADR-0021:44 word for word after S-003 landed, and the
`0021:44` pointer is re-derived against the final tree rather than assumed. If S-003 moved lines
above `:44`, the pointer changes even when the quoted words do not.

**Which rule owns this, stated exactly, because the near-miss answer is wrong.**
`.claude/rules/writing-style.md` rule 2 of its "Nami only" section says "Quoted **outside** text
stays word for word". ADR-0021 is not outside text, so that rule is a precedent here and not the
owner. What owns it is the root `CLAUDE.md` evidence rule: "Quote before you assert. Citing
`ADR-NNNN` means the fact is *in* that ADR". A quotation that no longer matches its source is that
rule's failure, not a style defect. So the only correct repair is to re-read the source and copy it,
never to paraphrase the quotation into agreement.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, and
`python3 scripts/check-decisions-index.py`. Then open `0021:44` and compare the quoted substring
character by character. `/refresh-citations` covers the pointer, and the `checking-a-citation` skill
owns what the pointer must satisfy.

**Sources.** `docs/adr/0093-warnings-as-errors.md:150` and
`docs/adr/0021-openiddict-version-adaptation.md:44`, both read 2026-08-08; the root
[`../CLAUDE.md`](../CLAUDE.md) evidence rule, its "Quote before you assert" bullet;
`.claude/rules/writing-style.md`, the "Nami only" section, rule 2, as the precedent it is.

**Out of scope.** Every other citation in ADR-0093, and ADR-0021 itself, which S-003 owns.

---

## S-022. Extend ADR-0061's maintenance rule to cover a version moving inside a row

**Status:** open · **Blocked by:** none · **Unblocks:** S-023

**The gap, quoted from the rule itself.** `docs/adr/0061-technology-stack-of-record.md:80` binds two
things and only two: "When a new technology decision is accepted, add a row here in the same change
that adds the ADR; when a choice is superseded, update the row to point at the superseding ADR." S-004
had to move `OpenIddict 7.5` to `7.6` inside an existing row whose owning ADR did not change. That is
neither clause, and S-004 made the edit under the next sentence instead, that the table "is an index,
never the authority".

**Why it is a decision and not a wording fix.** The rule is marked binding, and a third clause tells
every future bump that it owes an edit here. That is a new obligation on a class of change, which is
what an ADR amendment is for. It also has to say who notices: the pin lives in ADR-0021 parameter A
and in `Directory.Packages.props`, and nothing today connects either to this table.

**End state.** `0061:80`'s rule carries a clause covering a version or a descriptive detail changing
inside a row while its owning ADR stays the same, in this ADR's own amendment style. The clause names
where the authoritative value lives, which is the owning ADR rather than this table. The amendment says
whether the obligation is on the seed that moves the pin or on a periodic pass, because a rule with no
named moment is a rule nobody runs.

**Verification.** `bash scripts/check-adrs.sh` after `git add`, `bash scripts/test-check-adrs.sh`, and
`python3 scripts/check-decisions-index.py`. Then read `:80` and confirm a reader who has just bumped a
pin can tell from the rule alone that this table owes an edit. Check 3 compares the index row status
against the frontmatter status, so confirm neither moved.

**Sources.** `docs/adr/0061-technology-stack-of-record.md:80` for the two clauses, `:84` for the
shared-omission blind spot this is **not**, and `:86` for the deferred manifest check;
`scripts/check-adrs.sh` Check 4, read 2026-08-08, which extracts only each row's last cell with
`sed -E 's/.*\| ([0-9, ]+) \|$/\1/'` and never reads the choice cell.

**Out of scope.** Building any check, which is S-023. Auditing the other rows for stale details, which
this ADR's own 2026-07-25 entry already says the remaining rows deserve before GA.

---

## S-023. Wire the ADR-0061-against-manifest check now its own trigger has fired

**Status:** blocked · **Blocked by:** S-022 · **Unblocks:** nothing yet

**The trigger, quoted, and the measurement that fired it.**
`docs/adr/0061-technology-stack-of-record.md:86` says to "Wire the check when the manifest carries
runtime packages, and until then the human step above is still the whole of it". It also explains why
it was not worth doing then: "a manifest of build-time analyzers is not a stack". Measured 2026-08-08,
`Directory.Packages.props` carries eleven `PackageVersion` rows, of which **eight** are the
bracket-pinned OpenIddict packages landed by S-002. Those are runtime packages, and they are indexed by
the "Protocol engine" row. So the condition in that sentence is met.

**What the check has to catch, and it is not the one already recorded.** `0061:84` describes a shared
omission, a technology with no row and no marker, and states plainly that the guardrail is blind to it.
S-004 found a second and narrower shape: a row that exists with a stale value. Read at source
2026-08-08, Check 4 extracts only each row's last cell and set-compares ADR numbers, so it reads no
version anywhere. The first defect this check should find is therefore the one S-004 fixed by hand.

**End state.** A check exists that compares the version in ADR-0061's stack table against
`Directory.Packages.props` for every technology present in both, and it fails the build on a
disagreement. It runs where the other eight gates run. **The direction is stated rather than assumed**,
because `0061:86` warns that reconciling the table *to* the manifest finds nothing useful while the
manifest is small: this check runs manifest to table for versions, and the completeness direction stays
the human step until a later seed changes that.

**The check must be failed on purpose before it is believed.** `.claude/rules/build-and-ci.md` and this
repository's four self-tests exist because a control read as enforced while enforcing nothing. So the
end state includes a planted break, at minimum a table cell moved one minor version away from the
manifest, with the run log line showing the check red. The table writes versions to major or minor while
the manifest pins to patch inside brackets, so the comparison is not string equality and the seed says
what it is instead.

**Verification.** The new check fails on a planted mismatch and passes on the real tree, both logged.
All nine existing gates stay green. If the check is added to `scripts/check-adrs.sh`, then
`bash scripts/test-check-adrs.sh` covers it and gains a planted case; if it is a new script, it arrives
with its own self-test, and `.claude/rules/commands.md` gains its command.

**Sources.** `docs/adr/0061-technology-stack-of-record.md:84` and `:86`; `scripts/check-adrs.sh`
Check 4, read 2026-08-08; `Directory.Packages.props`, eleven `PackageVersion` rows of which eight are
OpenIddict, counted 2026-08-08; the `adding-a-ci-gate` skill for the procedure.

**Out of scope.** Checking licences, which ADR-0026 section C owns. Checking completeness, meaning a
technology present in the manifest with no table row, which `0061:86` says finds nothing useful yet.

### S-021 result, measured 2026-08-08

**The repair is not the one this seed asked for, and the difference is the point.** The seed's end
state said the quotation should match `0021:44` word for word. Doing only that means writing 7.6 where
7.5 stood, which leaves the citation coupled to the pin and guarantees this seed recurs at every bump.
`docs/CLAUDE.md` already rules on that shape: "Prefer an anchor that survives an edit."

**What landed instead satisfies the end state and removes the recurrence.** Parameter E now quotes two
fragments, "clear obsolete warnings" and "all obsolete members are removed". Both were verified
2026-08-08 to be verbatim substrings of `0021:44`, so the quotation does match word for word, and
neither carries a version, so the next bump cannot falsify it. The pin was dropped from the sentence
because ADR-0021 parameter A owns it and repeating it here bought nothing: the timing in that
paragraph was always carried by "ahead of the 8.0 bump" rather than by the version.

**Dropping the version cost the paragraph no meaning**, which is why this was a judgement call rather
than a decision needing its own seed. Parameter E's argument is that a deprecation becomes a build
break, and that this is intended because ADR-0021 already tells the project to clear obsolete warnings
before the bump that deletes the members. The version was never load-bearing in that argument.

**The old wording is kept, in an amendment, not deleted.** ADR-0093's More Information now records
what the quotation used to say, which seeds moved it, and why new digits were the wrong repair. That
matters more than the fix: a citation that resolves while no longer holding the claim is the defect
class this repository keeps paying for, and this is one instance caught with its cause written down.

**One phrase was chosen to avoid polluting another seed's search.** The replacement says "at the
current pin" rather than "at the pinned version", because S-005 verifies its own work by grepping
`at the pinned version` and the wider `the pinned`. Measured after the edit, ADR-0093 returns zero
hits for both, so this change adds no false positive to that seed.

**Verification.** `bash scripts/check-adrs.sh` and `python3 scripts/check-decisions-index.py`, both
green; `markdownlint-cli2` 195 files 0 issues. The frontmatter was untouched, so Check 3 sees the same
`accepted` status in both index rows. Both quoted fragments were confirmed present in `0021:44` by
substring test, and `0021:44` was confirmed still to be parameter D. Parameter E's edit replaced two
lines with two lines and the amendment was appended inside More Information, so **every other pointer
into ADR-0093 was re-read rather than reasoned about**: `0093:88-89`, `:94-98`, `:133-138`,
`:255-256`, and `:258-260` each still hold what their citing documents claim, across
`.claude/rules/build-and-ci.md`, `tests/CLAUDE.md`,
`tests/Nami.Identity.UnitTests/Nami.Identity.UnitTests.csproj`, and ADR-0094. `0093:150` still holds
the quotation line.

**The one remaining `7.5` in ADR-0093 is deliberate.** The amendment quotes the old wording to record
it. By S-001's bucket test that is bucket B, a closed record with a date, so it stays.

---

## S-027. Give OpenTofu a licence-record row, its MPL-2.0 exception being unrecorded

**Status:** open · **Blocked by:** none · **Unblocks:** nothing yet

**The gap, with the searches that establish it.** `docs/adr/0026-dependency-license-policy.md:36`
reads "Case-by-case, needing Architect and Legal approval recorded as an exception: MPL-2.0 and LGPL
(file/dynamic-link scope)". OpenTofu is MPL-2.0: `docs/adr/0023-iac-tool-opentofu.md:6` and `:26` both write
"OpenTofu (MPL-2.0, Linux Foundation)", and `docs/adr/0061-technology-stack-of-record.md:65` carries it
as the "Infrastructure as code" stack row. Searched 2026-08-08 over
`docs/DEPENDENCY-LICENSES.md` for both `OpenTofu` and `tofu`, case-insensitively, **zero hits**. So the
exception ADR-0026 requires be recorded is not recorded anywhere.

**Why this is not merely a missing row.** Five comparable external tools do have rows: Apache JMeter
and cosign in section 2, and Trivy, gitleaks, and OWASP ZAP in section 6, read 2026-08-08. So the
absence is not a policy that external tools are out of scope. It is one tool missed, and it happens to
be the one whose licence is the only non-permissive one in the stack table.

**How it was found, which is the argument for the wider pass.** Not by auditing the licence record.
S-024 read view 03's nine constraint rows against their owning ADRs, and the Infrastructure-as-code row
named the licence in passing. `0061:84` already records why this class survives: the guardrail
"compares two lists that are both derived from this repository's own markup ... It therefore catches a
disagreement between them and is blind to a shared omission."

**End state.**

- OpenTofu has a row in `docs/DEPENDENCY-LICENSES.md`, in the section its nature fits, with the licence
  read at the distributed artifact rather than from ADR-0023 or from a badge, and with the read date.
  Section 7's rule is explicit that a licence is "never recorded from ... another document in this
  repository", so ADR-0023 is the pointer to what to read, not the evidence.
- The row states whether the ADR-0026 MPL-2.0 exception has actually been approved by Architect and
  Legal, or records plainly that it has not. **The second is an acceptable outcome and the more likely
  one**, because ADR-0023 was decided 2026-07 and no approval appears in the record. Writing "not yet
  approved" is the deliverable; inventing an approval is the failure.
- Section 7's composition rule is considered: OpenTofu is distributed as a release archive, so what it
  bundles is read before its licence is trusted, in the shape section 2.1 already worked for JMeter.

**Verification.** `git grep -nic "opentofu" -- docs/DEPENDENCY-LICENSES.md` returns a non-zero count.
`bash scripts/check-adrs.sh` and `markdownlint-cli2`. The licence is quoted from the artifact with its
read location and date, and a reader can tell from the row whether the exception is approved or open.

**Sources.** `docs/adr/0026-dependency-license-policy.md:36` for the MPL-2.0 route;
`docs/adr/0023-iac-tool-opentofu.md:6` and `:26` for the licence claim;
`docs/adr/0061-technology-stack-of-record.md:65` for the stack row; `docs/DEPENDENCY-LICENSES.md`
section 2 for the external-tool shape, section 2.1 for a worked composition read, and section 7 for the
read-at-source and composition rules; `0061:84` for the shared-omission blind spot. Every one read
2026-08-08.

**Out of scope.** Re-deciding OpenTofu, which ADR-0023 owns. Auditing every other stack row for a
missing licence row, which is the wider pass this finding argues for and which deserves its own seed
with its own enumeration.

---

## Maintenance

- A seed is added here **in the same change** that establishes the work, with its end state, its
  verification, its chain edges, and its sources.
- A seed is marked `done` when its verification has been run and its pull request has landed. The
  seed stays in the file, because the chain has to stay readable after the fact.
- Seed IDs are never reused and never renumbered. A seed that turns out to be wrong is marked and
  explained, not deleted.
- When a seed splits, the original stays and names the seeds it split into. Splitting is the normal
  outcome of finding that a seed was not single-agent-sized.
- An item arriving from [`BUILD-PLAN.md`](BUILD-PLAN.md) has its queue row deleted in the same
  change that creates its seed.
