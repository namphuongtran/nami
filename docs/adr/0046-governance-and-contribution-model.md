---
status: "accepted"
date: 2026-07-07
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and maintainer
consulted: Contributor Covenant; the Developer Certificate of Origin (DCO); Conventional Commits and Keep a Changelog; .NET Foundation governance models
informed: all contributors, via this repository
---

# Adopt an ADR-driven, DCO-based OSS governance and contribution model with dual-control releases

## Context and Problem Statement

Nami is published as open source and invites outside contribution, so it needs a stated governance and contribution model: how architectural decisions are made and made visible, how contributors sign off their work, what conduct is expected, what a pull request must satisfy, and who has authority to publish a release. Without this written down, decisions look arbitrary, contributors do not know the ground rules, and there is no agreed guard on the irreversible act of publishing to the public registries. None of the existing ADRs cover project governance.

## Decision Drivers

* Architectural decisions must be transparent and explain their "why" to the community.
* Contribution must be low-friction for a .NET OSS project while keeping provenance clean.
* Publishing to a public registry is external and irreversible, so it must never be an unguarded or autonomous action.
* Conduct and change-tracking expectations must be explicit.

## Considered Options

* An informal/undocumented model (a benevolent-maintainer default with no written governance)
* A CLA-based corporate-style contribution model
* An ADR-driven, DCO-based model with a Contributor Covenant and dual-control releases

## Decision Outcome

Chosen option: "An ADR-driven, DCO-based model with dual-control releases", because it is transparent, standard for the .NET OSS ecosystem, and low-friction for contributors while keeping release authority guarded. The fixed parameters are:

* **A. Architecture is governed by ADRs.** Significant decisions are recorded as ADRs under `adr/` (the set this file belongs to), so the community can read why each choice was made; `GOVERNANCE.md` and `MAINTAINERS.md` point to the ADR set as the decision record.
* **B. Contribution flow.** `CONTRIBUTING.md` defines the developer setup (the ADR-0025 first-run), the coding standard, Conventional Commits, and a PR checklist that requires tests, an update to `PublicAPI.Unshipped.txt` when the public surface changes (ADR-0044), a changelog entry, and a passing license scan (ADR-0026). The changelog follows Keep a Changelog, generated from the conventional commits.
* **C. Sign-off = DCO.** Contributors certify origin with a Developer Certificate of Origin sign-off, chosen over a CLA because it is lightweight, standard for .NET OSS, and requires no copyright assignment. A CLA remains the alternative if legal later requires copyright assignment or a corporate-contribution regime; that reversal would be a superseding decision.
* **D. Conduct.** A Contributor Covenant `CODE_OF_CONDUCT.md` governs community behavior.
* **E. Release authority is dual-control.** Maintainers hold release authority, and publishing (NuGet, the container registry, a GitHub release) is an external, irreversible action that is never autonomous: it runs in a protected environment behind a required two-person approval. Pull-request CI (build and test) is automatic; the publish step is gated. This is both organizational policy and OSS supply-chain best practice, and it aligns with the dual-control discipline used elsewhere (ADR-0007, ADR-0020).

Two sub-decisions are deliberately left open: whether to remain self-governing or join a software foundation for governance credibility, and (should the need arise) the switch from DCO to a CLA. Neither blocks the model above.

### Consequences

* Good, because the "why" of every architectural choice is public and durable, contributors have clear rules, and no release can be pushed by one person or by an unattended job.
* Good, because DCO plus Conventional Commits keeps contribution light while producing clean provenance and an automatic changelog.
* Bad, because DCO sign-off enforcement, PR-checklist review, and a two-person publish approval add friction and require maintainer availability, which a small maintainer group must sustain.
* Neutral, because foundation membership and a possible future CLA are left open; the model works self-governed and with DCO in the meantime.

### Confirmation

* The repository carries `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `GOVERNANCE.md`/`MAINTAINERS.md`, and a `CHANGELOG.md`, and the PR template enforces the checklist.
* A PR without a DCO sign-off, or that changes the public surface without updating the API file, fails the checks.
* A publish to a public registry requires a second approver in a protected environment; an unattended publish cannot succeed.

## Pros and Cons of the Options

### Informal/undocumented model

* Good, because it needs no governance documents.
* Bad, because decisions look arbitrary, contributors lack ground rules, and there is no guard on irreversible releases; trust does not scale.

### CLA-based corporate-style model

* Good, because it gives the project explicit rights (relicensing latitude, copyright assignment) some organizations require.
* Bad, because a CLA is heavier friction that deters casual contributors and is unnecessary for an Apache-2.0 project that only needs origin certification, which DCO provides.

### ADR-driven, DCO-based, dual-control model (chosen)

* Good, because it is transparent, low-friction, ecosystem-standard, and keeps release authority guarded.
* Bad, because it depends on sustained maintainer availability for review and two-person publishes.

## More Information

* This model is recorded from the productization design (doc 28 §9.1/§9.3). The open sub-decisions (DCO versus CLA, and self-govern versus foundation membership) are the items flagged in doc 28 §9.4.
* Related decisions: ADR-0025 (the developer first-run referenced by `CONTRIBUTING.md`), ADR-0026 (the license scan gating a PR), ADR-0027 (packaging and the publish pipeline this dual-control guards), ADR-0044 (the public-API file a surface-changing PR must update), and ADR-0045 (the security disclosure policy, a sibling governance document). Dual-control here mirrors ADR-0007 and ADR-0020.
* Authored in this repository in 2026-07 to record the settled governance and contribution model as an ADR; neutral references (Contributor Covenant, the Developer Certificate of Origin, Conventional Commits, Keep a Changelog, the .NET Foundation) are named factually for identification only, and no commercial competitor is named.
