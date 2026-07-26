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

Design documents are numbered `01` to `21`, and that number is the **reading order** of
this layer. Several documents also cite a **`Phase NN`**, which is a different axis
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
| [01](01-foundations.md) | Foundations and solution structure | reviewed | 0024, 0027, 0052, 0065 |
| [02](02-data.md) | Data tier and multi-tenancy | reviewed | 0001, 0018, 0036, 0037, 0049 |
| [03](03-audit.md) | Audit subsystem | reviewed | 0008, 0022 |
| [04](04-core-protocol.md) | Core protocol server | reviewed | 0004, 0005, 0014, 0048, 0049 |
| 05 | Resource-server token validation | planned | 0049, 0005, 0004, 0001, 0033, 0009 |
| 06 | Sender-constrained tokens (DPoP and mTLS) | planned | 0014, 0005 |
| [07](07-authorization.md) | Authorization and delegated admin | reviewed | 0010, 0047, 0013 |
| [08](08-user-management.md) | User management and authentication | reviewed | 0028, 0013, 0003, 0002 |
| 09 | Federation and the claims profile | planned | 0002, 0005, 0001, 0009 |
| [10](10-email-notification.md) | Email and notification subsystem | draft | 0038 |
| [11](11-login-consent-ui.md) | Login, consent, and logout UI | draft | 0019, 0004, 0003, 0002, 0013 |
| [12](12-key-management.md) | Key management and rotation | draft | 0005, 0006, 0007, 0011, 0012, 0033 |
| [13](13-revocation-caching.md) | Revocation propagation and caching | draft | 0039, 0040 |
| [14](14-advanced-flows.md) | Advanced flows | draft | 0014 |
| [15](15-admin-api.md) | Admin API | draft | 0020, 0015 |
| [16](16-admin-app.md) | Admin App | draft | 0020, 0029 |
| [17](17-erasure-and-data-subject-rights.md) | Erasure and data-subject rights | draft | 0016, 0053 |
| [18](18-tenant-lifecycle.md) | Tenant lifecycle | draft | 0017, 0054 |
| [19](19-observability-capacity-slo.md) | Observability, capacity, and SLO | draft | 0022, 0041, 0063 |
| [20](20-testing.md) | Testing | draft | 0060, 0062 |
| [21](21-cicd-and-deployment.md) | CI/CD and deployment | draft | 0025, 0023, 0031, 0051 |

The three `planned` rows are reserved numbers rather than gaps: each module exists
in the design corpus and lands here when its turn comes. The ADR sets on those rows
are the ones the corpus records for them, and are confirmed against this repository
when the file is written. A `planned` row becomes a linked `draft`, then `reviewed`
once approved. The file number is the dependency order in which designs are
produced, so inserting a module renumbers the tail, which is a deliberate act.
