---
status: reviewed
created: 2026-07-26
tags: [architecture, stakeholders, concerns, iso-42010]
---

# Stakeholders and concerns

> **Part of:** the [Software Architecture Document](README.md), the bridge from the structural
> views to the quality views.

This view makes the ISO/IEC/IEEE 42010 core explicit: **who** has a stake, **what** each cares
about, and **where** that concern is answered. It complements
[03-drivers-and-constraints](03-drivers-and-constraints.md), which states the forces, by
naming the people those forces come from.

It sits here on purpose. The views before it say **what the system is**; the views after it say
**how it behaves and how well**. This is the hinge: it names the concerns, and
[20-nfr-catalogue](20-nfr-catalogue.md) is those same concerns made measurable.

## 1. Stakeholders

| # | Stakeholder | What they are accountable for, and what they therefore care about |
|---|---|---|
| S1 | **End users**, the resource owners | Signing in and consenting. They care that sign-in is reliable, that a session or token can actually be revoked, and that their personal data is handled and erasable |
| S2 | **Tenant administrators** | Operating one tenant. They care about isolation, self-service inside their tenant, and being structurally unable to reach another tenant or escalate |
| S3 | **Relying-party and client developers** | The apps and APIs consuming tokens. They care about protocol conformance, a stable public contract, and being able to validate a token **correctly, including per tenant** |
| S4 | **Platform and operations** | Running it in production. Availability, recovery, key operations, capacity, and whether a failure is visible |
| S5 | **Security** | The threat model. Isolation, key custody, authorization correctness, and the break-glass paths |
| S6 | **Data-protection owner and Legal** | The data-protection posture. Erasure, retention, audit content, residency, consent. **Holds the only authority to assert a compliance verdict** |
| S7 | **Product** | Packaging and adoption: the release, the licensing and usage posture, and the documentation an adopter actually needs |
| S8 | **Maintainers** | Building and evolving it. The architecture rules, the extension seams, and safe adaptation to new engine and runtime versions |
| S9 | **Auditors and reviewers** | Reviewing after the fact. A tamper-evident trail and provenance they did not have to trust the operator for |

Two of these are easy to under-weight. **S3 is not a passive consumer**: a resource server that
validates only the signature re-opens cross-tenant acceptance, so part of this architecture's
correctness lives in someone else's code and has to be documented as an obligation rather than
assumed (ADR-0049). And **S9 reviews without trusting the operator**, which is why the audit
chain is keyed and append-only at the database level rather than merely well-behaved
(ADR-0008).

## 2. Concerns, each traced to a driver

| # | Concern | Driver |
|---|---|---|
| C1 | Strong tenant isolation: no cross-tenant read, write, or token acceptance | D1 |
| C2 | Key rotation without downtime | D2 |
| C3 | Scale to the target concurrency without serialising a hot path | D3 |
| C4 | High availability with bounded recovery | D4 |
| C5 | Revocation and logout freshness inside a stated window | D5 |
| C6 | Erasure and tamper-evident audit, which pull against each other | D6 |
| C7 | Cloud portability, including running with no cloud at all | D7 |
| C8 | Safe evolution: adding a feature without touching v1 | D8 |
| C9 | Authorization correctness: least privilege, no confused deputy, dual control on dangerous actions | D1, D5 |
| C10 | Protocol conformance and a stable consumer contract | D8, via ADR-0021 and ADR-0044 |
| C11 | Data-protection posture: retention, residency, consent | D6 |
| C12 | Operability: observability, runbooks, a capacity model | D3, D4 |

**C6 is the only concern that is a stated tension rather than a goal**, and that is deliberate.
Erasure wants data gone; a tamper-evident chain wants nothing altered. Naming it as a tension
is what forced a mechanism that satisfies both rather than a compromise that satisfies neither
(ADR-0016).

## 3. Correspondence: stakeholder to concern to view

This is the correspondence table ISO/IEC/IEEE 42010 asks for. It is what makes the architecture
description checkable: a concern with no view is a gap, and a view answering no concern is
weight without purpose.

| Stakeholder | Concerns | Answered in |
|---|---|---|
| S1 End users | C1, C5, C6, C11 | [09-runtime-flow-views](09-runtime-flow-views.md) views 1, 12, 14 and 10; [13-security-architecture](13-security-architecture.md) sections 2 and 6 |
| S2 Tenant administrators | C1, C9 | [13-security-architecture](13-security-architecture.md) sections 2 and 5; [09-runtime-flow-views](09-runtime-flow-views.md) views 2 and 9 |
| S3 Client developers | C10, C1, C5 | [04-system-context](04-system-context.md) (the obligation on a resource server); [09-runtime-flow-views](09-runtime-flow-views.md) views 1, 5, 11; [07-container-view](07-container-view.md) (the package graph they consume) |
| S4 Operations | C2, C3, C4, C12 | [21-performance-scalability](21-performance-scalability.md), [22-reliability-backup-dr](22-reliability-backup-dr.md), [16-observability-monitoring](16-observability-monitoring.md), [17-operations-maintenance](17-operations-maintenance.md) |
| S5 Security | C1, C2, C6, C9 | [13-security-architecture](13-security-architecture.md) throughout; [14-threat-model](14-threat-model.md) for the threats those controls answer |
| S6 Data protection and Legal | C6, C11 | [13-security-architecture](13-security-architecture.md) section 6; [09-runtime-flow-views](09-runtime-flow-views.md) view 10. **Verdicts reserved to S6** |
| S7 Product | C10, C8 | [19-evolution-and-extensions](19-evolution-and-extensions.md); [10-deployment-infrastructure](10-deployment-infrastructure.md) section 4 (the three onboarding paths) |
| S8 Maintainers | C8, C10 | [08-component-view](08-component-view.md) (the seams); [17-operations-maintenance](17-operations-maintenance.md) section 5 (the upgrade cadence); [15-schema-migration-evolution](15-schema-migration-evolution.md) |
| S9 Auditors | C6 | [13-security-architecture](13-security-architecture.md) section 6; [12-data-architecture](12-data-architecture.md) (the chain's storage) |

## 4. Who signs off before production, and on what

Four concerns are gated by a **human ratification that is not a code artifact**. They gate
**production, not the build**: the mechanism is designed and testable now, and the parameter or
verdict belongs to a named owner. All are consolidated in the
[Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md) rather than duplicated
here.

* **Security (S5).** The capability taxonomy and its non-inheriting set, the assurance level
  required per dangerous capability, dual-control approver roles, the revocation objective,
  and, most consequentially, **accepting the Pool shared-keyset risk** before general
  availability (ADR-0033).
* **Data protection and Legal (S6).** Whether crypto-shred satisfies the erasure right, the
  audit retention basis, residency classification, consent policy, and the telemetry and
  registration data categories.
* **Operations with Security (S4, S5).** Recovery objectives per store, cryptoperiods,
  key custody and break-glass custody, drill cadence, and the on-call roster.
* **Product (S7).** The release gate, the adoption plan, and the licensing terms.

**Two adjacent things are deliberately not the same gate.** The verification **baseline** is
self-assessed against OWASP ASVS, with L2 as the floor and L3 on the key, token, dual-control,
and isolation paths; buying that assurance instead, as a paid assessment or a certification, is
deferred as premature, and self-assessment's weakness is a recorded trade-off. An **independent
penetration test is a gate**, owned by Security (ADR-0062), scoped to the protocol endpoints,
the admin surface, tenant isolation including the Pool shared-keyset case, and the break-glass
paths. What is gated is that the test was run and its findings ratified or accepted, not that a
report was published, which is a separate Product question.

**No statement anywhere in this architecture asserts a compliance verdict.** That is reserved
to S6, and saying so is itself part of the architecture rather than a disclaimer.

## Sources

* ADR-0049 and ADR-0008 (the two under-weighted stakeholder obligations: correct validation at
  the resource server, and an audit trail an auditor can trust without trusting the operator),
  ADR-0016 (the erasure-versus-audit tension named as a tension), ADR-0033 (the accepted risk
  Security ratifies), ADR-0062 (the self-assessed baseline and the deliberately deferred
  external audit), ADR-0021 and ADR-0044 (the conformance and stable-contract concern).
* [03-drivers-and-constraints](03-drivers-and-constraints.md) for drivers D1 to D8, which
  every concern above traces to; the
  [Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md) for the sign-off items.
* ISO/IEC/IEEE 42010 supplies the structure (stakeholders, concerns, correspondence). No
  normative claim is made beyond that structure.
* Reconciled against the design corpus's stakeholders view on 2026-07-26. Taken from it: the
  nine-stakeholder set, the twelve concerns traced to drivers, and the correspondence table,
  which is the part that makes an architecture description checkable rather than decorative.
  **One item was taken from the corpus after this view first got it wrong, which is worth
  recording rather than silently fixing.** The corpus lists a "pen-test gate" under Security
  ratification and a "published security audit" under Product. This view originally rejected
  both, reading ADR-0062's deferral of a paid assessment as also declining a penetration test.
  It does not: ADR-0062 was answering what standard the product is held to, and its wording
  bundled two different questions. The corpus is the more correct side here and specifies the
  gate in detail elsewhere, with an explicit scope and rules of engagement. ADR-0062 was
  corrected on 2026-07-26 and the checklist row added. **The Product half was not taken**:
  publishing a report is a launch-communications choice, not a security gate, and nothing
  decides it. Its open-item bucket taxonomy is that project's register convention and maps
  here onto the pre-GA checklist.

---

[Prev: Introduction and scope](01-introduction-scope.md) · [Index](README.md) · Next: [Drivers and constraints](03-drivers-and-constraints.md)
