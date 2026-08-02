---
status: "accepted"
stack-record: true
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the GitHub Actions Node 20 deprecation notice (2025-09-19); `actions/checkout` and `DavidAnson/markdownlint-cli2-action` release metadata read at their repositories on 2026-08-01
informed: all contributors, via this repository
---

# Pin every CI action by a full version tag, never by a floating major or a SHA

> **Parameter A was reversed on 2026-08-02, one day after this ADR was accepted, and
> everything below is written as it now stands rather than as it first read.** The original
> parameter A required a 40-character commit SHA with the version as a trailing comment. It
> now requires a full version tag, `@v7.0.1`. **The reversal is not a retreat to the status
> quo this ADR was written against**: the rejected status quo was a *floating* major tag,
> `@v4` and `@v24`, and that stays rejected and is now machine-enforced, which it never was
> under the SHA rule. What changed hands is the narrower guarantee described in Consequences.
> The reason was reviewability, raised by the maintainer, and it is recorded in More
> Information rather than paraphrased here. Parameter B was never about SHAs and is
> unchanged.

## Context and Problem Statement

The CI workflow runs third-party code. Each `uses:` entry fetches a GitHub Action and
executes it in the runner with access to the checked-out repository, and until now every
one of them was referenced by a **mutable tag**: `actions/checkout@v4` and
`DavidAnson/markdownlint-cli2-action@v24`. A git tag is a movable pointer, so the code that
runs tomorrow under an unchanged workflow file is not necessarily the code that ran today.

This is not hypothetical here, and the evidence is what prompted the decision. Between the
workflow being written and 2026-08-01, `@v24` moved from `v24.0.0` to `v24.1.0`, which
changed the bundled linter from `markdownlint-cli2` 0.23.0 / `markdownlint` 0.41.0 to
0.23.1 / 0.41.1 (read in the action's `package.json` at each tag). `CLAUDE.md` told
contributors to run 0.23.0 locally and described it as "same pinned version CI runs". That
sentence had quietly become false, and nothing failed, because both versions happened to
agree on this corpus. A pinning discipline that holds only while two versions agree is not
a discipline.

ADR-0051 section D already settled the underlying principle for one artifact class: the
base image is "digest-pinned (never a mutable tag)". Nothing extended it to the actions
that build and gate the repository, even though those execute earlier and with more access
than the image does.

The Node 20 runtime deprecation forced the question: `actions/checkout@v4` targets Node 20,
which the runner now force-runs on Node 24, so the reference had to change regardless. The
decision is what to change it *to*.

## Decision Drivers

* An unchanged workflow file should produce an unchanged build. A tag cannot promise that.
* The local command a contributor is told to run and the version CI actually runs must be
  the same version, and must be **kept** the same by something other than memory.
* Consistency with ADR-0051 section D rather than a second, weaker rule for a riskier
  artifact class.
* A pin must not be so rigid that security patches never land.

## Considered Options

* Floating major tag (`@v4`, `@v24`), the status quo
* Exact version tag (`@v7.0.1`, `@v24.1.0`)
* Commit SHA with the version as a trailing comment

## Decision Outcome

Chosen option: **exact version tag**, kept at the latest stable release of each action.

The two grounds are different in kind and both are needed, because the first alone would
have chosen the SHA. **Against the floating major**, an exact tag is the minimum that makes
an unchanged workflow file mean an unchanged build under normal operation, which is the
driver this ADR was written for. **Against the SHA**, a reviewer has to be able to read the
diff: a reference nobody can evaluate by eye is checked by nobody, and a review that is
performed but not really performed is a worse control than one that is honest about its
scope. That second ground is a judgement about how this project actually reviews changes,
not a claim that a tag is as immutable as a SHA. It is not, and Consequences says so.

* **A. Every `uses:` is a full version tag, `@vX.Y.Z`** (revised 2026-08-02; this parameter
  previously required a 40-character commit SHA). Three forms are rejected and the rejection
  is machine-enforced by `scripts/check-adrs.sh` Check 8c, which is what makes this
  parameter a gate rather than a habit:
  * a **floating major** (`@v7`), which is the form that actually caused the drift described
    above and differs from the sanctioned form by four characters in a diff;
  * a **branch or any other moving ref** (`@main`);
  * a **commit SHA**, rejected not because it is weak but because it is the form this
    project has decided not to read. Allowing it would leave two sanctioned styles with no
    way to tell a deliberate one from a leftover, and half a workflow that a reviewer skims
    is the outcome this parameter exists to prevent.

  Local (`./...`) and container (`docker://...`) references are out of scope; image digests
  are ADR-0051 section D.

* **A2. The pinned version is the latest stable release at the time of the change**, so an
  exact pin is not an excuse to sit on an old one. A prerelease is not a stable release and
  is not pinned here. Verified at the point of the reversal: `actions/checkout` v7.0.1,
  `actions/setup-dotnet` v6.0.0, and `DavidAnson/markdownlint-cli2-action` v24.1.0 were each
  the latest release on 2026-08-02, read through the GitHub releases API, so the reversal
  moved **no** version and changed only the form of the reference.
* **B. The linter version is coupled, not merely coincident.** The markdownlint action's
  bundled `markdownlint-cli2` version and the version `CLAUDE.md` tells a contributor to run
  are one decision, bumped in one change. The workflow comment states the bundled version so
  the coupling is visible at the point of edit rather than only in this ADR.
* **C. Pinned is not frozen.** A bump is a normal change: move the tag to the new latest
  stable and, for the markdownlint action, the documented local version in the same commit.
  Dependabot already watches `github-actions` weekly in `.github/dependabot.yml`, so a bump
  arrives as a pull request a human merges rather than as a manual edit; that was true under
  the SHA rule and is unchanged by the reversal.
* **D. Scope.** This governs the actions in `.github/workflows/`. It does not govern NuGet
  packages, whose policy is ADR-0026, nor container base images, which are already covered
  by ADR-0051 section D.

### Consequences

* Good, because the workflow file is readable, so the pin is something a reviewer actually
  checks rather than scrolls past. This is the ground the 2026-08-02 reversal turns on.
* Good, because it closes the specific defect that motivated the ADR: the documented local
  linter version and the version CI runs can no longer drift apart unobserved.
* Good, because the rejection is now **enforced** rather than remembered. Check 8c fails a
  build on `@v7`, on `@main`, on `@v7.0`, and on a SHA, and it was proven against all four
  before being wired, in `scripts/test-check-adrs.sh`. Under the original SHA rule nothing
  mechanical stopped a floating tag from being added back.
* **Bad, and this is the guarantee the reversal gave up, stated plainly rather than
  softened.** A release tag is a pointer that whoever owns the action repository can move,
  so an exact tag constrains **accident** but not **intent**. Under the SHA rule the same
  workflow file resolved to the same code forever; under this one it resolves to whatever
  `v7.0.1` points at today. The realistic attack against a CI pipeline is precisely a
  repointed tag on a third-party action, and this repository is now exposed to it. Three
  things bound the exposure and none of them removes it: the workflow uses three actions,
  two of them published by GitHub itself; the runner token is `permissions: contents: read`;
  and there are no secrets in these jobs. **All three change at M1**, when the release
  pipeline puts `id-token: write` on a job and adds signing credentials, so this consequence
  is the reason to re-open parameter A at that point rather than to let it stand by default.
* Bad, because updates need a deliberate step instead of arriving free. That cost is the
  point, but it means a neglected pin ages, which is what Dependabot is for.

### Confirmation

* The runtime claim is read at source: `action.yml` at each `actions/checkout` major tag
  gives `using: node20` for `v4` and `using: node24` for `v5`, `v6` and `v7` (read
  2026-08-01). `v7` is chosen: its breaking change blocks checking out a fork pull request
  under `pull_request_target` and `workflow_run`, and this workflow triggers only on `push`
  and `pull_request`, so the change is a hardening that does not touch it.
* The drift claim is read at source: the action's `package.json` pins `markdownlint-cli2`
  0.23.0 at `v24.0.0` and 0.23.1 at `v24.1.0`. Both were run against the 154 tracked
  markdown files on 2026-08-01 and both reported zero errors, so the drift was harmless on
  this corpus. **That is a fact about this corpus on this date, not a reason the drift was
  acceptable**, and recording it that way is deliberate.
* **The rule is enforced, and the enforcement was proven against the bug before being
  wired** (2026-08-02), which is this repository's standing rule for a checker. Check 8c was
  run against a planted workflow carrying `@v7`, `@main`, a 40-character SHA, and `@v7.0`,
  and reported exactly those four while leaving four look-alikes alone: a valid `@v7.0.1`,
  a valid tag with a trailing comment, a local `./` action, and a `docker://` reference.
  Planting `@v7` in the real `ci.yml` fails the guardrail with exit 1.
* **Writing that check exposed a second-order defect in the self-test that guards it.**
  `scripts/test-check-adrs.sh` copies the working-tree guardrail into its throwaway
  worktree, because a worktree at `HEAD` otherwise tests the committed script. It did not
  copy the working-tree **workflows**, so a check introduced in the same commit as the fix
  it demands ran against `HEAD`'s unfixed file and failed for a reason unrelated to what it
  was asserting. A self-test's subject is the script and its input, and that is now recorded
  in the script.
* **A2 was verified rather than assumed** at the reversal: the three actions' latest
  releases on 2026-08-02, read through the GitHub releases API, were v7.0.1 (published
  2026-07-20), v6.0.0 (2026-07-16) and v24.1.0 (2026-07-18), which are the three already
  pinned. The reversal therefore moved no version.

## Pros and Cons of the Options

### Floating major tag (status quo)

* Good, because patches and security fixes arrive with no action.
* Bad, because the reference is mutable, so the workflow file stops describing the build.
* Bad, because a minor bump can change tool behaviour silently, which is exactly what
  happened to the linter version here.

### Exact version tag (chosen 2026-08-02)

* Good, because it is readable and stops accidental minor drift.
* Good, because "is this the right pin" is answerable from the diff alone, so the review
  step is real rather than nominal.
* Bad, because a tag is still movable by whoever owns the repository, so it constrains
  accident but not intent, and CI is where intent matters most.

### Commit SHA plus a version comment (chosen 2026-08-01, reversed 2026-08-02)

* Good, because the reference is immutable and the comment keeps it nominally readable.
* Bad, because the comment is the only readable part and it is **not** what resolves, so
  the one thing a reviewer can check is the one thing that has no effect. That asymmetry is
  what the reversal turned on: it was recorded as a small hazard when this option was chosen
  and treated as the deciding cost a day later.
* Bad, because bumps are manual until a dependency bot is wired up, and the comment can go
  stale independently of the pin.

## More Information

* **Why parameter A was reversed on 2026-08-02, recorded because the reason is not
  reconstructable from the text.** The maintainer, reviewing the pull request that added the
  solution shell, said the SHAs made the workflow hard to review and asked for version
  numbers instead. That is a maintainability judgement about this project, and it is the
  maintainer's to make; it was raised as a decision, not discovered as a defect. The
  counter-argument was put once before the change, in the terms Consequences now records,
  and the decision was reaffirmed. The instruction also included dropping this ADR outright,
  which was **not** done and the reason is worth keeping: parameter B has nothing to do with
  SHAs and is the clause that caught the only real drift this repository has had, so
  deleting the record to reverse one of its two parameters would have taken the other with
  it. Eleven files cite ADR-0086, and `adr/README.md` holds that a decision is superseded
  rather than removed, so amending in place with the reversal dated is also the form this
  repository's own governance asks for.
* **This ADR no longer extends [ADR-0051](0051-release-supply-chain-integrity.md) section
  D, and that is a real loosening rather than a rewording.** It was originally chosen partly
  for consistency with that section's "digest-pinned (never a mutable tag)" rule for base
  images, and the two now differ: an image is pinned by digest here, an action by a mutable
  tag. Nothing about ADR-0051 changes, and no signing or provenance decision there is
  restated or touched. What is gone is the claim that one rule covers both artifact classes.
* Related decisions: ADR-0051 (release supply chain), ADR-0026 (dependency policy, which
  governs NuGet packages rather than actions), ADR-0025 and ADR-0060 (the CI gates the
  workflow runs), and ADR-0021 (the habit of re-asserting a contract on every bump, which
  is the same instinct applied to versions rather than to seams).
* The CI/CD design is [21](../design/21-cicd-and-deployment.md).
* Authored in this repository on 2026-08-01 while closing the Node 20 deprecation warning.
  GitHub Actions, `actions/checkout`, `markdownlint-cli2` and the community action that
  wraps it are named factually as the project's own build dependencies.
