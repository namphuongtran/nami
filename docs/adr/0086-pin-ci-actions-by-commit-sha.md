---
status: "accepted"
stack-record: true
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the GitHub Actions Node 20 deprecation notice (2025-09-19); `actions/checkout` and `DavidAnson/markdownlint-cli2-action` release metadata read at their repositories on 2026-08-01
informed: all contributors, via this repository
---

# Pin every CI action by commit SHA, never by tag

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

Chosen option: **commit SHA with the version as a trailing comment**, because it is the only
one of the three where the reference is immutable, and because it applies ADR-0051 section
D's existing "never a mutable tag" rule to the class of artifact that runs first and with
the most access.

* **A. Every `uses:` is a full 40-character commit SHA**, with the human-readable version as
  a trailing comment. The comment is documentation and is **not** what resolves; a reader
  who edits only the comment has changed nothing.
* **B. The linter version is coupled, not merely coincident.** The markdownlint action's
  bundled `markdownlint-cli2` version and the version `CLAUDE.md` tells a contributor to run
  are one decision, bumped in one change. The workflow comment states the bundled version so
  the coupling is visible at the point of edit rather than only in this ADR.
* **C. Pinned is not frozen.** A bump is a normal change: resolve the new tag to its SHA,
  update both the SHA and the comment, and, for the markdownlint action, the documented
  local version in the same commit. Automating this with a dependency bot is expected and
  is not a weakening, since the bot proposes a SHA and a human merges it.
* **D. Scope.** This governs the actions in `.github/workflows/`. It does not govern NuGet
  packages, whose policy is ADR-0026, nor container base images, which are already covered
  by ADR-0051 section D.

### Consequences

* Good, because the workflow becomes reproducible: the same file resolves to the same code.
* Good, because a repointed tag on a third-party action, the realistic supply-chain attack
  against a CI pipeline, no longer reaches this repository silently.
* Good, because it closes the specific defect that motivated it: the documented local
  linter version and the version CI runs can no longer drift apart unobserved.
* Bad, because a SHA is unreadable, which is why the trailing version comment is mandatory
  rather than optional, and why a stale comment is its own small hazard.
* Bad, because updates need a deliberate step instead of arriving free. That cost is the
  point, but it means a neglected pin ages, so the bump belongs to whatever dependency
  automation the repository adopts.

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
* The pins are verifiable: each SHA in the workflow resolves through
  `git ls-remote` or the tag-to-SHA API to the tag named in its comment.

## Pros and Cons of the Options

### Floating major tag (status quo)

* Good, because patches and security fixes arrive with no action.
* Bad, because the reference is mutable, so the workflow file stops describing the build.
* Bad, because a minor bump can change tool behaviour silently, which is exactly what
  happened to the linter version here.

### Exact version tag

* Good, because it is readable and stops accidental minor drift.
* Bad, because a tag is still movable by whoever owns the repository, so it constrains
  accident but not intent, and CI is where intent matters most.

### Commit SHA plus a version comment (chosen)

* Good, because the reference is immutable and the comment keeps it readable.
* Bad, because bumps are manual until a dependency bot is wired up, and the comment can go
  stale independently of the pin.

## More Information

* This decision extends [ADR-0051](0051-release-supply-chain-integrity.md) section D's
  never-a-mutable-tag rule from base images to CI actions; it does not restate the signing
  and provenance decisions there.
* Related decisions: ADR-0051 (release supply chain), ADR-0026 (dependency policy, which
  governs NuGet packages rather than actions), ADR-0025 and ADR-0060 (the CI gates the
  workflow runs), and ADR-0021 (the habit of re-asserting a contract on every bump, which
  is the same instinct applied to versions rather than to seams).
* The CI/CD design is [21](../design/21-cicd-and-deployment.md).
* Authored in this repository on 2026-08-01 while closing the Node 20 deprecation warning.
  GitHub Actions, `actions/checkout`, `markdownlint-cli2` and the community action that
  wraps it are named factually as the project's own build dependencies.
