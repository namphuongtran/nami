---
status: reviewed
created: 2026-07-18
tags: [architecture, sad, c4, arc42, overview]
---

# Nami Software Architecture Document (SAD)

**System:** Nami, an open-source multi-tenant OAuth 2.0 / OpenID Connect identity
provider for .NET, built on OpenIddict 7.5 and .NET 10, released under Apache-2.0.

**Notation:** the [C4 model](https://c4model.com) at Levels 1 to 3, plus supporting
sequence, ER, and deployment diagrams, all rendered as Mermaid.

**Structure:** the chapter sequence follows the **arc42** template's twelve sections,
one topic per file. Where arc42 keeps a single chapter this layer splits it, most
visibly its crosscutting-concepts chapter, which here is seven files because data,
security, threats, schema evolution, observability, and operations each need room.
One chapter, evolution and extensions, has no arc42 counterpart and is placed after
the decisions index. From ISO/IEC/IEEE 42010 this layer takes the
stakeholder-concern-correspondence structure, which is
[02-stakeholders-and-concerns](02-stakeholders-and-concerns.md); no normative claim is
made beyond that structure.

> arc42 is by Dr. Gernot Starke and Dr. Peter Hruschka, [arc42.org](https://arc42.org),
> licensed CC BY-SA 4.0. Only the section sequence is used here: no arc42 text,
> explanation, or diagram is reproduced, and the chapter set is adapted as described
> above. Verified at source on 2026-07-26.

This folder is the **architecture layer** of the repository. It gives the decisions
in [`docs/adr/`](../adr/README.md) and the detail in [`docs/design/`](../design/README.md)
a single coherent picture. It does not replace either: it points into them as the
authoritative source, and where it disagrees with one of them, this layer is the bug.

## 1. How to read this SAD

Read top to bottom for the full picture, or jump to the view that answers your
question. Each topic is one file.

**The file number is the reading order.** Files run `01` to `24` with no gaps.

### The problem (arc42 sections 1 to 3)

| File | Topic | Diagram |
|---|---|---|
| [01-introduction-scope](01-introduction-scope.md) | Introduction, scope, and what is deliberately out of it | - |
| [02-stakeholders-and-concerns](02-stakeholders-and-concerns.md) | Who has a stake, what they care about, and where it is answered | Correspondence table |
| [03-drivers-and-constraints](03-drivers-and-constraints.md) | Architecture drivers, quality targets, and hard constraints | - |
| [04-system-context](04-system-context.md) | System context: actors and external systems | C4 L1 |

### The answer in short (arc42 section 4)

| File | Topic | Diagram |
|---|---|---|
| [05-solution-strategy](05-solution-strategy.md) | The seven decisions that determined everything downstream, what each one buys, and what was deliberately not chosen | - |

### The structure (arc42 sections 5 to 7)

| File | Topic | Diagram |
|---|---|---|
| [06-domain-model](06-domain-model.md) | Bounded contexts and the ubiquitous language | - |
| [07-container-view](07-container-view.md) | Host processes, package graph, and datastores | C4 L2 |
| [08-component-view](08-component-view.md) | The IdP core internals and its subsystems | C4 L3 |
| [09-runtime-flow-views](09-runtime-flow-views.md) | Sixteen key end-to-end sequences, each with the invariants it must preserve | Sequence |
| [10-deployment-infrastructure](10-deployment-infrastructure.md) | Topology, HA, and the edge | Deployment |

### Cross-cutting concepts (arc42 section 8, split into seven)

| File | Topic | Covers |
|---|---|---|
| [11-cross-cutting-concepts](11-cross-cutting-concepts.md) | The index into the six that follow, plus the invariants that reappear everywhere | Tenancy, security, observability, configuration |
| [12-data-architecture](12-data-architecture.md) | Logical data model and database topology | ER diagram, the five contexts, row-level security |
| [13-security-architecture](13-security-architecture.md) | The primary quality attribute, given its own view | Trust boundaries, the three isolation layers, token and key protection, administration controls, audit, abuse resistance |
| [14-threat-model](14-threat-model.md) | The threats those controls answer, and the residual | STRIDE across five boundaries, two attack trees, and where the residual actually lives |
| [15-schema-migration-evolution](15-schema-migration-evolution.md) | How the schema and the tenant fleet change safely | Migration as a build artifact, the traffic gate, expand-and-contract, the tenant lifecycle |
| [16-observability-monitoring](16-observability-monitoring.md) | How it is seen, and how objectives become alerts | The two lanes, metrics and cardinality control, burn-rate alerting, the canary |
| [17-operations-maintenance](17-operations-maintenance.md) | Running it and keeping it healthy | Runbooks, background jobs, key operations, the two break-glass paths, upgrade cadence |

### Decisions and where they go next (arc42 section 9, plus one chapter arc42 has no slot for)

| File | Topic | Covers |
|---|---|---|
| [`../adr/README.md`](../adr/README.md) | The ADR corpus | Every decision of record, with context and rationale |
| [18-decisions-index](18-decisions-index.md) | The **reverse** index: which views cite each decision | Generated ADR-to-view map, the measured cross-cutting set, the one decision no view cites, and the eight load-bearing claims found with no owning ADR, all now resolved, with what each resolution turned out to be |
| [19-evolution-and-extensions](19-evolution-and-extensions.md) | What attaches after v1, and what v1 pays for it | Three accepted-but-unbuilt features, six proposed demand-driven extensions, kill switch by composition |

### Quality and risk (arc42 sections 10 and 11)

| File | Topic | Covers |
|---|---|---|
| [20-nfr-catalogue](20-nfr-catalogue.md) | What must be true, with a way to measure it | Quality-attribute targets, the SLO and error budget, ratification status |
| [21-performance-scalability](21-performance-scalability.md) | How the throughput and latency attributes are met | Capacity model, bottleneck ordering, sizing, overload controls, the load-test gate |
| [22-reliability-backup-dr](22-reliability-backup-dr.md) | Behaviour under failure, and the cost of getting back | High availability, resiliency, per-store recovery objectives, backup, DR drills |
| [23-risks-and-technical-debt](23-risks-and-technical-debt.md) | Known risks and deliberate debt, kept apart from things that are neither | Risk register with designed responses, three debt items, and what only looks like debt |

### Vocabulary (arc42 section 12)

| File | Topic | Covers |
|---|---|---|
| [24-glossary](24-glossary.md) | Domain, protocol, and project-convention terms | Definitions that point at the document of record rather than restating it |

> **The glossary lives here but is not this layer's property.** Its vocabulary is
> measurably used across all three layers, and for several terms more heavily outside
> this one: `seam catalogue` and `verification record` each appear in seven ADRs. It
> sits here because arc42 places the glossary in the architecture document, and because
> an entry **names the document of record rather than owning the term**, so a definition
> of `stack of record` here leaves ADR-0061 the authority. Measured on 2026-07-26.

Every view listed above exists. The release gate the deferred ratifications roll up to
is the [Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

## 2. C4 legend

The SAD uses four abstraction levels of the C4 model and stops at Level 3:

- **Level 1, System Context** ([04-system-context](04-system-context.md)): Nami as one box, with
  the people and external systems it interacts with. Audience: everyone.
- **Level 2, Container** ([07-container-view](07-container-view.md)): the separately
  deployable or runnable units (the IdP host, Admin API, Admin App and BFF,
  databases, cache, message broker) and how they communicate. Audience: architects
  and operators.
- **Level 3, Component** ([08-component-view](08-component-view.md)): the major building
  blocks inside the important containers (protocol pipeline, tenant resolution,
  key-rotation service, admin security module, outbox relay). Audience: developers.
- **Level 4, Code** is deliberately **out of scope** for this layer. Class-level and
  field-level detail belongs to the detailed designs in
  [`docs/design/`](../design/README.md).

Structural views are drawn as **styled Mermaid flowcharts that follow C4 levels and
semantics**, rather than the C4-specific Mermaid diagram type, which renders cramped
and cannot be coloured consistently. Every diagram shares one colour system, the
classic C4 blue palette, so a reader never has to ask what a box is.

**Visual conventions, consistent across every diagram here:**

- A solid arrow is a synchronous call. A dashed arrow is an asynchronous or event
  flow, or an optional or v2 relationship. The arrow label states the protocol or
  the intent.
- Shape and colour carry fixed meaning:

```mermaid
graph LR
  P([Person or actor]):::person
  H[Our system, container, or host]:::host
  C[Component]:::comp
  D[(Datastore)]:::store
  X[External system]:::ext
  O[Optional, not required for v1]:::optional
  V[v2 evolution]:::v2

  classDef person fill:#08427b,stroke:#052e56,color:#ffffff
  classDef host fill:#1168bd,stroke:#0b4884,color:#ffffff
  classDef comp fill:#85bbf0,stroke:#5d82a8,color:#000000
  classDef store fill:#438dd5,stroke:#2e6295,color:#ffffff
  classDef ext fill:#999999,stroke:#6b6b6b,color:#ffffff
  classDef v2 fill:#7b4fa0,stroke:#54356f,color:#ffffff,stroke-dasharray:5 4
  classDef optional fill:#cfd8dc,stroke:#90a4ae,color:#1a2b34,stroke-dasharray:5 4
```

| Class | Colour and shape | Meaning |
|---|---|---|
| `person` | Dark blue stadium | Person, actor, or role |
| `host` | Medium blue rectangle | Our system, container, or host |
| `comp` | Light blue rectangle | A component inside a container (C4 L3) |
| `store` | Blue cylinder | Datastore (database or cache) |
| `ext` | Grey rectangle | External system (not ours) |
| `v2` | Purple dashed | v2 evolution, kill-switched off in v1 |
| `optional` | Slate dashed | Optional, not required for v1 |

Subgraphs mark a system boundary or the container being decomposed. Node labels stay
short so nothing overlaps; the detail lives in the table beneath each diagram.

## 3. Where this layer sits (authority order)

```text
docs/adr/                            decisions and why      (authority for a decision)
        |
docs/design/                         per-feature detail     (authority for how it is built)
        |
docs/architecture/  <- THIS LAYER    the coherent picture across all views
        |
docs/kb/                             evidence and lessons   (how we know)
docs/PRE-GA-RATIFICATION-CHECKLIST.md  the deferred human sign-offs
```

Authority runs upward, not downward:

- For a **decision**, an accepted ADR is the authority and is binding until
  superseded. If this layer contradicts one, this layer is corrected.
- For **implementation detail**, the detailed design is the authority. If this layer
  contradicts one, this layer is corrected.
- This layer **never introduces a decision** that is not already recorded in an ADR.
  Where a topic it must cover is genuinely undecided, it is labelled an open item
  and, if a human owner must settle it, mirrored into the
  [pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

## 4. Verification discipline

Every file here follows the repository conventions in
[`CLAUDE.md`](../../CLAUDE.md), in particular:

1. **Read the whole relevant set first.** For each topic, the ADR of record, the
   detailed design, and the supporting evidence are enumerated and read in full, not
   sampled.
2. **Verify before asserting, and cite or research.** A claim is reconciled against
   the ADR of record and, where it is externally checkable (an RFC, a package
   version, a database behaviour), against the primary source. No claim is written
   here without a source, and where two sources disagree the disagreement is
   resolved at the primary source rather than by preferring one document.
3. **Flag what is not settled.** An optional lever, an unratified number, or a
   compliance question is labelled as such rather than presented as decided. A
   read-replica read/write split, for example, is an optional scale lever and not a
   v1 requirement.
4. **Every file ends with a `Sources` section** naming the exact documents it
   derives from. ADR cross-references are machine-checked by
   [`scripts/check-adrs.sh`](../../scripts/check-adrs.sh), so an `ADR-NNNN` written
   here must resolve to a real ADR.

## 5. Scope of claims

This is a **technical architecture description**. It asserts no legal or regulatory
compliance: no statement here is a verdict on GDPR, on a data-residency regime, or
on any other legal obligation. Those verdicts belong to the Legal and
data-protection owners of whoever deploys Nami, and the sign-offs this project
defers to a human owner are consolidated in the
[pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

Nami is an independent open-source project. Names of third-party projects,
standards bodies, and packages appear only for factual identification.

---

Next: [Introduction and scope](01-introduction-scope.md)
