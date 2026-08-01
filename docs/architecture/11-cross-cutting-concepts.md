---
status: reviewed
created: 2026-07-18
tags: [architecture, cross-cutting, navigation]
---

# Cross-cutting concerns

> **Part of:** the [Software Architecture Document](README.md), quality and operational
> views.

**This page navigates rather than explains.** A cross-cutting concern is one that spans every
container, so it has no single owning view, and the natural failure is to summarise it here as
well as in the view that owns it. Two summaries of one thing drift, and the shorter one wins
by being read first. So each concern below names **where the substance is** and **which
decision owns it**, and nothing else.

Where a concern has no architecture view, this page points at the ADRs directly. That is the
honest answer rather than a gap: not every cross-cutting concern is a structural or
operational view.

## Concern index

| Concern | Where the substance is | Owning decisions |
|---|---|---|
| **Multi-tenancy and isolation** | [12-data-architecture](12-data-architecture.md) section 5 for the model, [13-security-architecture](13-security-architecture.md) section 2 for the three layers and the signature caveat, [09-runtime-flow-views](09-runtime-flow-views.md#13-per-request-tenant-resolution-and-isolation) view 13 for the per-request flow | ADR-0001, ADR-0033, ADR-0037, ADR-0049 |
| **Security posture** | [13-security-architecture](13-security-architecture.md), the whole view | ADR-0043, ADR-0062, ADR-0042 |
| **Key management** | [08-component-view](08-component-view.md) for the rotation state machine, [17-operations-maintenance](17-operations-maintenance.md) section 3 for the operations, [13-security-architecture](13-security-architecture.md) section 4 for the protections | ADR-0005, ADR-0011, ADR-0012, ADR-0033, ADR-0006 |
| **Audit and diagnostics, the two lanes** | [16-observability-monitoring](16-observability-monitoring.md) section 1 for the split, [13-security-architecture](13-security-architecture.md) section 6 for the chain's properties | ADR-0008, ADR-0022, ADR-0063 |
| **Endpoint isolation and CORS** | [13-security-architecture](13-security-architecture.md) sections 3 and 7 | ADR-0048, ADR-0050 |
| **Resiliency and overload** | [21-performance-scalability](21-performance-scalability.md) section 5 for the controls, [22-reliability-backup-dr](22-reliability-backup-dr.md) section 2 for behaviour under dependency failure | ADR-0040, ADR-0018 |
| **Data protection and privacy mechanisms** | [13-security-architecture](13-security-architecture.md) section 6, [09-runtime-flow-views](09-runtime-flow-views.md#10-gdpr-erasure-saga) view 10 for the erasure ordering | ADR-0016, ADR-0053, ADR-0054 |
| **Quality attributes and the SLO** | [20-nfr-catalogue](20-nfr-catalogue.md) | ADR-0041, ADR-0006 |
| **Version adaptation** | [17-operations-maintenance](17-operations-maintenance.md) section 5 for the cadence, [15-schema-migration-evolution](15-schema-migration-evolution.md) section 4 for the version-pinned hazards | ADR-0021, ADR-0030 |
| **Governance and supply chain** | **No architecture view; the decisions are the substance.** ADR-driven decisions with DCO sign-off and dual-controlled releases (ADR-0046), keyless signing and provenance attestation (ADR-0051) with every CI action pinned by commit SHA rather than by a movable tag (ADR-0086, which extends ADR-0051's never-a-mutable-tag rule from the base image to the actions that run earlier and with more access), permissive-OSS-only dependencies enforced by a license scan (ADR-0026), private coordinated vulnerability disclosure and CVE issuance (ADR-0045), and an AI-assisted-development policy requiring human accountability and disclosure (ADR-0067) | ADR-0046, ADR-0051, ADR-0086, ADR-0026, ADR-0045, ADR-0067 |

## Three invariants that reappear in almost every view

These are listed here not as a summary but because they are the cross-cutting statements most
often dropped when a view is written in isolation, and each one is stated fully in its owning
view.

1. **A valid signature does not prove the tenant.** Pool tenants share a pool-group signing
   key, so isolation rests on issuer and audience and the `tenant` claim, never on the
   signature. Dropping it re-opens cross-tenant token acceptance
   ([13-security-architecture](13-security-architecture.md) section 2, ADR-0033, ADR-0049).
   The `tenant` claim this rests on is frozen by name and destination from v1.0 (ADR-0088),
   which is why an invariant may name it.
2. **Every subsystem is classified into one of three failure postures, and there is exactly
   one carve-out.** Performance caches fail open, the diagnostic telemetry path fails open for
   a different reason, and security checks fail closed. The carve-out is the email anti-abuse
   throttle (ADR-0038). The distrusted-key set (ADR-0039) and the proof-replay set are **not**
   exceptions; they follow the rule. What the classification exists to prevent is a subsystem
   sitting in **none** of the three, which is where the telemetry export path was until
   ADR-0040 parameter E was added
   ([22-reliability-backup-dr](22-reliability-backup-dr.md) section 2, ADR-0040).
3. **This layer never introduces a decision.** Where a view found a load-bearing claim with no
   owner, the resolution was a new ADR rather than an assertion here, which is how ADR-0072,
   ADR-0073, and ADR-0074 came to exist. Where a view found a useful control that no decision
   covers, it is recorded in place as a candidate rather than adopted (ADR-0000, and the
   authority order in the [index](README.md)).

---

[Prev: Deployment and infrastructure](10-deployment-infrastructure.md) · [Index](README.md) · Next: [Data architecture](12-data-architecture.md)
