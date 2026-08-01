---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0044 (the public-surface discipline this extends, read section by section on 2026-08-01), ADR-0020 (the code-first OpenAPI posture this deliberately does not change), ADR-0079 (the rules the surface has to satisfy), ADR-0081 (the dual-control catalogue whose reachability is the other half of this), the design corpus's `api/` OpenAPI 3.1 specification (reconciled against design 15 on 2026-08-01, 63 operations against 61)
informed: Admin API implementers, adopters coding against the Admin API, CI owners, release managers
---

# Lock the HTTP surface with a committed snapshot of the generated OpenAPI document

## Context and Problem Statement

ADR-0044 treats Nami's public API as a versioned seam and builds eight mechanisms for it:
**A** analyzers over `PublicAPI.Shipped.txt`, **B** SemVer rules, **C** a deprecation policy,
**D** stricter rules for consumer-implemented ports, **E** isolation from OpenIddict's own
breaks, **F** independently versioned wire contracts, **G** telemetry names as contract, and
**H** the process and CI gate that ties them together.

**Not one of the eight reaches an HTTP route.** Read on 2026-08-01, ADR-0044 contains no
occurrence of the words route, endpoint, HTTP method, verb, or status code, and its single
match for a URL-shaped string is the `<migration url>` placeholder inside section C's
`[Obsolete(...)]` example. Section F is the closest and it governs the DTO **assembly**,
`Nami.Identity.Admin.Contracts` with its `V1` namespace, which is a set of types. Types are
already covered by section A. The URL an adopter actually calls is covered by nothing.

That is a real hole rather than a theoretical one, and this repository produced two proofs of
it on a single day. Both were present in the tree at commit `efa2e1b`, where the docs
guardrail's seven checks passed and CI reported success with **zero annotations**.

1. **The surface drifted from its own rules and nothing noticed.** The tenant path parameter
   was spelled three ways in design 15, `{t}` five times and `{id}` on four tenant-scoped
   routes, against the `{tenantId}` that ADR-0079, ADR-0084, and two sentences of design 15
   itself use. Nine route declarations disagreed with an accepted ADR.
2. **Two dual-control actions had no endpoint at all.** `secret-revoke` and `audit-export`
   each carry a target-guard row in ADR-0081, a place in the destructive-action catalogue,
   and supporting machinery down to a SHA-256 digest of a frozen filter, and neither could be
   raised from anywhere, because ADR-0079 rule 5 forbids a generic proposal route.

Both were found by a person reading two documents side by side, once, because someone thought
to look. The second was found only by diffing against an external specification. Neither
method is repeatable, and a control that depends on somebody deciding to run it is not a
control.

ADR-0079 already anticipates a test, "a contract check that every published path is
registered and every reference resolves". That checks registration and reference resolution.
It would have passed on both defects above: `{t}` registers and resolves perfectly well, and
an endpoint that does not exist has no reference to fail.

One thing is **not** in question here. ADR-0020 settles that the OpenAPI document is the
built-in .NET output, generated from the code, and design 15 section 6.1 says the same. This
decision does not reopen that. The question is what locks the surface, not who authors it.

## Decision Drivers

* The HTTP surface is the thing an adopter writes code against, and ADR-0044 freezes the
  public surface from v1.0. A frozen contract with no lock is a promise, not a contract.
* **A generated document reports the surface; it cannot object to a change.** Regenerating
  after a rename yields a document that faithfully describes the new, broken contract.
  Reporting is not locking, and the difference is the whole decision.
* Whatever locks it should work the way the .NET lock already works, so contributors learn
  one discipline instead of two.
* No second authority for the surface. Design 15 stays the implementer source; nothing
  adopted here may become a rival description of the same endpoints.
* There is no code before M1, so any mechanism has a start date, and the interval before it
  needs an answer rather than an omission.

## Considered Options

* **A. Contract-first.** A hand-authored OpenAPI document is the source and the code is
  written to satisfy it, which is the design corpus's shape.
* **B. Committed snapshot of the generated document, diffed in CI.**
* **C. Review discipline.** Reviewers are expected to notice route changes.
* **D. Check the corpus specification into this repository** as a reference copy to compare
  against.

## Decision Outcome

Chosen option: **B, a committed snapshot of the generated document, diffed in CI**, because it
is the only option that makes a route change appear in a diff without introducing a second
author for the surface, and because it is ADR-0044 section A's own pattern applied to a
different artifact rather than a new discipline to learn.

* **A. The generated document is written to a tracked file and CI fails when the generated
  form differs from the committed one.** This is `PublicAPI.Shipped.txt` for URLs. A pull
  request cannot change a route without the change appearing in its own diff, which converts
  a silent edit into a reviewed one. The failure message must say which operations moved.

* **B. What the snapshot locks is the wire shape, not the prose.** Path templates including
  parameter names, methods, required request headers, response status codes, and schema
  names and required-ness. Summaries, descriptions, tag ordering, and example values are
  documentation and must be excluded, because a gate that fires on a reworded sentence gets
  disabled. The snapshot is therefore compared in a **canonical, normalised form**; the exact
  normaliser is a build-time choice and is deliberately not pinned here.

  **Extended on 2026-08-01 by [ADR-0090](0090-versioned-api-base-path.md): the locked set
  also includes the base path of each `servers` entry**, the host itself being deployment
  configuration and normalised out. The enumeration above omitted it, and the omission was
  not cosmetic: in an OpenAPI document the version prefix lives in `servers` rather than in a
  path template, so the single most breaking change available to this surface, moving every
  operation's URL at once, was invisible to the gate written to make breaking changes appear
  in a diff. Section E applies to the addition unchanged, so it counts as present only once a
  negative self-test moves the base path and shows CI red.

* **C. A route change is classified by ADR-0044 section B, not by a new scale.** Adding an
  operation or an optional field is MINOR. Removing or renaming an operation, renaming a path
  parameter, adding a required parameter or header, or changing a success status code is
  MAJOR. Renaming a path parameter is called out explicitly because it reads as cosmetic and
  is the exact defect this ADR was written after.

* **D. Absence is a different failure and needs a different control.** A snapshot diff
  compares what exists today against what existed yesterday, so it detects **drift** and is
  blind to **omission**: it would have caught defect 1 above and would **not** have caught
  defect 2, because an endpoint that never existed never disappears. The control for omission
  is the reachability assertion recorded in design 20, that every `ActionType` in the
  ADR-0081 catalogue has an endpoint that raises it. Two failure modes, two controls, and
  saying so here is what stops the snapshot from being read as full coverage.

* **E. The gate must be proven to fail.** Before the gate counts as present, a negative
  self-test renames one path parameter and shows CI red. This repository has already recorded
  the reason: a checker that stays green on the bug it was written for is worse than none,
  because it converts an unchecked claim into a confident one.

* **F. Before M1 there is no generated document, and this is stated rather than left blank.**
  Until code exists the surface lives only in design 15 section 3, held by nothing mechanical.
  The reconciliation performed on 2026-08-01 against the design corpus's `api/` specification,
  63 operations against 61, is a **one-time act and not a control**, and it is recorded in
  Confirmation below so a later reader can see both its result and its date. The first task
  of this ADR at M1 is to generate the document, diff it against design 15 section 3, and
  resolve every difference before the snapshot is committed as the baseline.

* **G. The corpus specification is evidence, not an artifact, so option D is rejected on
  purpose.** It is not checked in. Copying it would create exactly the second authority driver
  four forbids, and it is already stale against this repository in at least one direction:
  it has no per-client CORS route, which ADR-0050 requires here. Where it is cited, it is
  cited the way every other corpus reference is, by name and date.

### Consequences

* Good, because a route rename stops being invisible: it appears in a diff, is classified by
  rules that already exist, and reaches a reviewer as a change rather than as an incident.
* Good, because the discipline is the one already in place for the .NET surface, so section H
  of ADR-0044 absorbs it without a parallel process.
* Good, because ADR-0020 is untouched. The document stays generated, and this only decides
  that its output is retained and compared.
* Bad, because a snapshot of a generated document is churn if the normalisation is wrong, and
  a noisy gate is a disabled gate. Driver B above exists to bound that, and the normaliser is
  the part most likely to need a second pass after first contact.
* Bad, because nothing here runs before M1. The interval is named in section F rather than
  hidden, but it is still an interval in which prose is the only guard.
* Neutral, because the gate inherits the generator's determinism. If the generator emits a
  non-deterministic ordering, the normaliser must sort it, and that is a build-time detail
  rather than a reason to choose differently.

### Confirmation

* **The two defects this ADR was written after, both fixed on 2026-08-01.** The tenant path
  parameter drift (nine route declarations, three spellings, against ADR-0079 and ADR-0084)
  and the two unreachable dual-control actions (`secret-revoke` and `audit-export`, each with
  an ADR-0081 target-guard row and no route). Both were present at `efa2e1b`, where the
  guardrail's seven checks and CI both reported clean, which is the measured statement that
  no existing mechanism covers this surface.
* **The reconciliation that found the second one**, run 2026-08-01 against the design
  corpus's `api/` OpenAPI 3.1 specification: 63 operations there against 61 here, and **eight
  row-level differences that resolve to six** once the same operation written two ways is
  counted once. Four are deliberate: a secret-rollover shape that follows ADR-0079 rule 1
  where the corpus posts to a noun, a memberships verb that follows ADR-0084, a subject-wide
  token revoke that ADR-0084 reasons is a different operation with a different blast radius
  and this repository therefore does not offer, and a per-client CORS route this repository
  has under ADR-0050 and the corpus lacks. The remaining two were the missing endpoints.
* At M1: CI fails a pull request that alters a route without updating the committed snapshot;
  the negative self-test of section E is shown red; and the reachability assertion of
  section D is shown to fail when an `ActionType` has no endpoint.
* **No pre-GA ratification entry is created, and that is deliberate.** The pre-GA checklist
  consolidates policies, thresholds, and sign-offs owned by a human (DPO, Legal, Security,
  Ops, Product). Everything deferred here is a build-time engineering choice, the normaliser
  and the generator, and the release-time judgements it feeds are already ADR-0044's.

## Pros and Cons of the Options

### A. Contract-first, the hand-authored document is the source

* Good, because the contract exists before the code and can be reviewed on its own.
* Good, because it is what the design corpus does, so the artifact already exists.
* Bad, because it contradicts ADR-0020, which settles the document as generated output, and
  reversing that is a much larger decision than the problem requires.
* Bad, because it creates a second authority for the surface alongside design 15, and a
  hand-authored document drifts from the code exactly as prose does.

### B. Committed snapshot of the generated document, diffed in CI (chosen)

* Good, because it applies a pattern this repository has already accepted for the .NET
  surface, so there is one discipline rather than two.
* Good, because it leaves authorship where ADR-0020 put it and adds only retention and
  comparison.
* Bad, because it cannot see an endpoint that was never written, which is why section D
  pairs it with a reachability assertion.
* Bad, because it needs a normaliser to avoid firing on documentation churn.

### C. Review discipline

* Good, because it costs nothing to adopt.
* Bad, because it is what was in place while nine route declarations drifted and two
  endpoints went missing, through more than one review pass. It is the option with a measured
  failure rate in this repository.

### D. Check the corpus specification in as a reference copy

* Good, because the comparison that found the missing endpoints becomes repeatable.
* Bad, because it is the second-authority problem in its clearest form: a copied
  specification with no owner, which ages against both repositories and is trusted precisely
  because nobody maintains it.
* Bad, because it is already wrong in this repository's favour in at least one place
  (ADR-0050's per-client CORS route), so it would be checked in as an authority that is
  known to be stale on arrival.

## More Information

* Related decisions: [ADR-0044](0044-public-api-stability-and-semver.md) (the seam discipline
  this extends, and the SemVer rules section C defers to),
  [ADR-0020](0020-admin-architecture.md) (the code-first OpenAPI posture, unchanged),
  [ADR-0079](0079-admin-api-http-conventions.md) (the rules the surface must satisfy, and
  rule 5, which is why a missing endpoint cannot be worked around),
  [ADR-0081](0081-dual-control-target-guard-taxonomy.md) (the catalogue whose reachability is
  the omission-side control), [ADR-0065](0065-coding-and-naming-conventions.md) (proposal
  action types as wire contract), and
  [ADR-0050](0050-per-client-cors-policy.md) (the route the corpus specification lacks).
* Surface and mechanism: design [15](../design/15-admin-api.md) section 3 remains the
  implementer source for the endpoints themselves; design [20](../design/20-testing.md)
  section 5.7 carries the reachability row.
* The design corpus's `api/` specification is cited as evidence with the date it was read and
  is not vendored here, consistent with how every other corpus artifact is treated in this
  repository.
