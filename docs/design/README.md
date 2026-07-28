---
status: reviewed
created: 2026-07-18
tags: [design, index, ieee-1016]
---

# Detailed feature designs

Per-feature design docs that elaborate *how* each part of Nami is built. They sit
one level below the [architecture overview](../architecture/README.md) and are
governed by the [ADR corpus](../adr/README.md), which remains the authority: a
design doc realizes decisions, it does not make them.

**Altitude.** The architecture layer covers C4 levels 1 to 3 (context, container,
component) plus the quality views, and stops where a module's internal contract
begins. This layer is **C4 level 4 (code) plus that internal contract**: interfaces
and signatures, fields and types and DDL, algorithms and state machines, wiring and
configuration keys, invariants. The working test is **enough for an engineer to
implement the module without guessing**. A design doc never repeats the C4 L1 to L3
pictures; it points up to the architecture view and goes down from there.

**Structure.** The section sequence follows the **IEEE 1016** software-design-description
viewpoints (interface, information, interaction, state, algorithm, dependency), merged
with this repository's evidence-first conventions. Only the viewpoint vocabulary and the
section ordering are used: no standard text is reproduced.

> **Decision rule.** If a detailed design surfaces a genuinely new decision that
> no ADR covers, it is raised as an ADR (or a Pre-GA checklist entry), never
> settled silently inside a design doc. Section 10 is where those are flagged.

## Two numbering axes, and how to tell them apart

Design documents are numbered `01` to `23`. That number is the **reading order** for `01`
to `21`. The tail, `22` and `23`, was **appended rather than inserted**, because inserting a
chapter renumbers everything after it and invalidates every cross-reference, including the
prose ones. Those two are read when their subject comes up rather than in sequence: `22`
when a dependency is bumped, `23` when a client is declared. Several documents also cite a **`Phase NN`**, which is a different axis
entirely: the **build order** of the design corpus this layer was reconciled from, which
has nine phases.

| Phase | Scope | Phase | Scope |
|---|---|---|---|
| 01 | Foundations | 06 | Admin, and the net-new security module |
| 02 | Database | 07 | Advanced flows |
| 03 | Core protocol | 08 | Keys and rotation |
| 04 | Users and MFA | 09 | Testing and deployment, running from day one |
| 05 | UI and consent | | |

**The two axes coincide for 01 and 02 by accident and diverge from 03 onward.** The
core-protocol design is document `04` and Phase `03`; the document numbered `03` is the
audit subsystem. So notation is the only reliable signal: a bare number, as in `(04)` or
"detailed in 04", is a **document in this layer**, while `Phase 04` is a **corpus build
phase**. They are never interchangeable, and a sentence that mixes them ("it is Phase 03
and rests on the data tier (02)") is using both deliberately.

Nothing in this repository schedules those phases, and the corpus roadmap is not part of
the published documentation, so a `Phase NN` reference is build-order context and
provenance rather than a plan this project commits to. Nami's own delivery marker is
**`M1`**, the point at which application source lands under `src/`; a finer milestone
breakdown would be a decision and would get an ADR rather than appearing here.

## The eleven sections

| # | Section | What belongs in it |
|---|---|---|
| 1 | Header and decisions realized | The frontmatter, the architecture view this sits under, and the implementer source of record open the file **above** section 1, the way a title does; section 1 itself is the **decisions-realized table** mapping this module to the ADRs it applies |
| 2 | Purpose and scope | What the module is and its boundary, including what is out of scope and which design owns it instead |
| 3 | Interfaces and contract | Public interfaces, method signatures, DTOs, ports. `classDiagram` lives here |
| 4 | Data and structure | Entities, fields, types, keys, indexes, DDL. `erDiagram` lives here |
| 5 | Behaviour | Algorithms, internal sequences, state machines. `sequenceDiagram`, `stateDiagram-v2`, `flowchart`, and `graph` live here |
| 6 | Dependencies and wiring | Dependency injection and composition, **configuration keys**, the **key-libraries-and-licenses table** (the ADR-0026 gate needs exact package identifiers), and the **patterns-applied callout** (ADR-0066) |
| 7 | Error handling, edge cases, invariants | Fail-closed rules, guards, and the failure modes a reader would otherwise discover in production |
| 8 | Security and multi-tenancy notes | Module-specific, on top of the layer-wide controls |
| 9 | Testing | Unit, integration, and negative tests, with spike and verification references |
| 10 | Open and build-time items | Anything deferred to a human sign-off or to implementation |
| 11 | Sources | The ADRs, architecture views, and records this design traces to |

A small or data-heavy module may fold sections; a DDL-heavy design carries most of
its content in section 4. Keep the numbering meaningful rather than forcing an empty
section. Diagram types belong to the sections named above, and a diagram is drawn
only where it removes ambiguity. The mechanical build recipe (scaffolding commands,
`.gitignore`, package version pins, CI file contents) lives in the implementation
plan, not here.

## Index

| # | Design | Status | Realizes (primary ADRs) |
|---|---|---|---|
| [01](01-foundations.md) | Foundations and solution structure | draft | 0024, 0027, 0065, 0075, 0044, 0026, 0030, 0031, 0006, 0009, 0025, 0021 |
| [02](02-data.md) | Data tier and multi-tenancy | reviewed | 0001, 0018, 0036, 0037, 0049 |
| [03](03-audit.md) | Audit subsystem | draft | 0008, 0022, 0016, 0006, 0009, 0001, 0053, 0041 |
| [04](04-core-protocol.md) | Core protocol server | reviewed | 0004, 0005, 0014, 0048, 0049 |
| [05](05-resource-server-validation.md) | Resource-server token validation | draft | 0049, 0033, 0005, 0004, 0048, 0001, 0037, 0009 |
| [06](06-sender-constrained-tokens.md) | Sender-constrained tokens (DPoP and mTLS) | draft | 0014, 0005, 0021, 0024, 0049 |
| [07](07-authorization.md) | Authorization and delegated admin | draft | 0010, 0047, 0013, 0005, 0008, 0024, 0001, 0021, 0075 |
| [08](08-user-management.md) | User management and authentication | draft | 0028, 0013, 0003, 0002, 0075, 0005, 0001, 0008, 0016, 0009, 0042 |
| [09](09-federation-and-claims-profile.md) | Federation and the claims profile | draft | 0002, 0075, 0005, 0001, 0013, 0019, 0009, 0034 |
| [10](10-email-notification.md) | Email and notification subsystem | reviewed | 0038 |
| [11](11-login-consent-ui.md) | Login, consent, and logout UI | draft | 0019, 0004, 0003, 0002, 0013 |
| [12](12-key-management.md) | Key management and rotation | draft | 0005, 0006, 0007, 0011, 0012, 0033 |
| [13](13-revocation-propagation-and-caching.md) | Revocation propagation and caching | reviewed | 0039, 0040 |
| [14](14-advanced-flows.md) | Advanced flows | draft | 0014 |
| [15](15-admin-api.md) | Admin API | draft | 0020, 0015 |
| [16](16-admin-app.md) | Admin App | draft | 0020, 0029 |
| [17](17-erasure-and-data-subject-rights.md) | Erasure and data-subject rights | draft | 0016, 0053 |
| [18](18-tenant-lifecycle.md) | Tenant lifecycle | draft | 0017, 0054 |
| [19](19-observability-capacity-slo.md) | Observability, capacity, and SLO | draft | 0022, 0041, 0063 |
| [20](20-testing.md) | Testing | draft | 0060, 0062 |
| [21](21-cicd-and-deployment.md) | CI/CD and deployment | draft | 0025, 0023, 0031, 0051 |
| [22](22-openiddict-seam-catalogue.md) | Engine seam catalogue and version adaptation | draft | 0021, 0024, 0030, 0011, 0014, 0018, 0019, 0022, 0075 |
| [23](23-configuration-and-client-declaration.md) | Configuration and client declaration | draft | 0052, 0043, 0009, 0039, 0050, 0001, 0031, 0065, 0044, 0021 |

Every number is now a written document; the last reserved `planned` row was filled by
`09`. A row's ADR set starts from what the design corpus records for that module and is
**confirmed against this repository when the file is written**, which has in practice
meant it grows: `09` arrived with four ADRs recorded against it and needed eight, because
the corpus attributed the claim choke-point to the wrong decision and had no equivalent of
ADR-0075. A row moves `draft` to `reviewed` once approved. The file number is the reading
order defined above, and designs are also
authored in that order because a later document leans on an earlier one; it is **not** the
order in which the code gets built, which is the separate axis described above. Inserting
a module renumbers the tail, which is a deliberate act.
