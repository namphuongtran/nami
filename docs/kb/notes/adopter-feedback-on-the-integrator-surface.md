---
title: What an integrator asked Nami for, and which of it is already a decision
tags: [adopter, integration, claims, api-surface, backlog, v2]
created: 2026-08-01
related: [[0044-public-api-stability-and-semver]], [[0087-http-surface-snapshot-gate]], [[0071-identity-change-event-publishing]], [[0084-membership-removal-semantics]], [[0005-encryption-credential-lifecycle]]
---

The design corpus contains an adopter-view document, written from the seat of a team
building a content-management product on Nami Identity, whose section 14 lists five things
that would make Nami easier to adopt. The document labels them plainly: *"None of these is a
v1 commitment"*, and calls them candidates for a v2 backlog. Read 2026-08-01 at
`INTEGRATION-REFERENCE-CMS.md` section 14 in the design corpus, which is not vendored into
this repository.

They are recorded here rather than in
[19-evolution-and-extensions](../../architecture/19-evolution-and-extensions.md), because
that view is a catalogue of **decisions**: every row in both of its tiers points at an ADR, and the view's
own rule is that a trigger firing earns "a full ADR and design" before any build. None of
these five has an ADR, so filing them there would give them a status they do not have and
would make the view's tier counts wrong. The repository's one-line rule applies instead:
decision to an ADR, knowledge to reference to the knowledge base.

**They are also not one kind of thing**, and flattening them into a list is what would make
them useless later. Two are decisions Nami has not made, one is a deliverable attached to a
decision already accepted, and two are documentation for a layer this repository has planned
and not built.

## The five, classified

| # | The ask | What it is here |
|---|---|---|
| 1 | A published integrator **claims contract** with a stability promise, versioned separately from the internal design | **Candidate decision.** The contract exists, as the canonical claims contract in design 09 section 5.2, but as an internal design document with no stability promise attached |
| 2 | A worked **consumer sample** for membership change events, including inbox deduplication | **Deliverable**, not backlog. [[0071-identity-change-event-publishing]] is accepted and design-complete |
| 3 | A cheap **live membership check** for a relying party closing the residual token window that [[0084-membership-removal-semantics]] reports as `residualTokenWindowSeconds`, 900 | **Candidate decision**, an API addition. Distinct from the self-service full-list endpoint below: that one answers the *user*, this one would answer a *relying party* |
| 4 | Guidance on **just-in-time provisioning**, including that the local actor row must be creatable without a login | Adopter documentation |
| 5 | Make the **`memberships` truncation flag loud** in adopter documentation | Adopter documentation, and see the finding below |

## Why three of them had nowhere to go

`docs/README.md` ends with "Getting-started guides, concept docs, configuration reference,
and the full docs site (DocFX) will be added as implementation progresses." That is the
adopter-facing layer, it is planned, and it does not exist. Items 4 and 5 are content for it,
and item 1 is a promise that would be published through it. So the reason this feedback had
no home is not that it was mislaid; it is feedback about a layer that has not been built, and
it should be re-read when that layer is started rather than treated as five loose tickets.

## The two worth separating from the rest

**Item 1 is the same shape as two decisions already taken, and is the gap between them.**
[[0044-public-api-stability-and-semver]] locks the .NET surface and
[[0087-http-surface-snapshot-gate]] locks the HTTP surface. What an integrator actually codes
against is neither: it is the set of claims in the tokens they receive. Nothing freezes those.
Whether they should be frozen is a decision this note deliberately does not make.

**Item 5 pointed at a real defect, which is now fixed.** The ask was to make the truncation
flag loud. Checking what the flag's documented remedy actually was found that the remedy had
no endpoint: [[0005-encryption-credential-lifecycle]] promises "a self-service endpoint for
the full list", [design 04](../../design/04-core-protocol.md) repeats it,
[design 09](../../design/09-federation-and-claims-profile.md) makes it the defined answer to
`memberships_truncated: true` and says the answer is "never a larger token", and
[design 11](../../design/11-login-consent-ui.md) has the tenant switcher call it.
[Design 08](../../design/08-user-management.md) owns the self-service surface and declared
no route. `GET /me/memberships` was added there on 2026-08-01. The lesson is worth keeping
separately from the feedback: **an outside reader asking for something to be documented is a
cheap way to discover it was never built**, because documenting a mechanism requires looking
at it.

## What this note is not

It is not a commitment to any of the five, and it is not a priority order. It is the record
that an integrator with a real product looked at this system and said these five things, on a
date, so a later "should we do X" conversation starts from that rather than from memory.
