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
| **Multi-tenancy and isolation** | [05-data](05-data.md) section 5 for the model, [11-security-architecture](11-security-architecture.md) section 2 for the three layers and the signature caveat, [06-runtime-views](06-runtime-views.md#13-per-request-tenant-resolution-and-isolation) view 13 for the per-request flow | ADR-0001, ADR-0033, ADR-0037, ADR-0049 |
| **Security posture** | [11-security-architecture](11-security-architecture.md), the whole view | ADR-0043, ADR-0062, ADR-0042 |
| **Key management** | [04-components](04-components.md) for the rotation state machine, [16-operations-and-maintenance](16-operations-and-maintenance.md) section 3 for the operations, [11-security-architecture](11-security-architecture.md) section 4 for the protections | ADR-0005, ADR-0011, ADR-0012, ADR-0033, ADR-0006 |
| **Audit and diagnostics, the two lanes** | [15-observability-and-monitoring](15-observability-and-monitoring.md) section 1 for the split, [11-security-architecture](11-security-architecture.md) section 6 for the chain's properties | ADR-0008, ADR-0022, ADR-0063 |
| **Endpoint isolation and CORS** | [11-security-architecture](11-security-architecture.md) sections 3 and 7 | ADR-0048, ADR-0050 |
| **Resiliency and overload** | [12-performance-and-scalability](12-performance-and-scalability.md) section 5 for the controls, [13-reliability-backup-and-dr](13-reliability-backup-and-dr.md) section 2 for behaviour under dependency failure | ADR-0040, ADR-0018 |
| **Data protection and privacy mechanisms** | [11-security-architecture](11-security-architecture.md) section 6, [06-runtime-views](06-runtime-views.md#10-gdpr-erasure-saga) view 10 for the erasure ordering | ADR-0016, ADR-0053, ADR-0054 |
| **Quality attributes and the SLO** | [10-nfr-catalogue](10-nfr-catalogue.md) | ADR-0041, ADR-0006 |
| **Version adaptation** | [16-operations-and-maintenance](16-operations-and-maintenance.md) section 5 for the cadence, [14-schema-migration-and-evolution](14-schema-migration-and-evolution.md) section 4 for the version-pinned hazards | ADR-0021, ADR-0030 |
| **Governance and supply chain** | **No architecture view; the decisions are the substance.** ADR-driven decisions with DCO sign-off and dual-controlled releases (ADR-0046), keyless signing and provenance attestation (ADR-0051), permissive-OSS-only dependencies enforced by a license scan (ADR-0026), and an AI-assisted-development policy requiring human accountability and disclosure (ADR-0067) | ADR-0046, ADR-0051, ADR-0026, ADR-0067 |

## Three invariants that reappear in almost every view

These are listed here not as a summary but because they are the cross-cutting statements most
often dropped when a view is written in isolation, and each one is stated fully in its owning
view.

1. **A valid signature does not prove the tenant.** Pool tenants share a pool-group signing
   key, so isolation rests on issuer and audience and the `tenant` claim, never on the
   signature. Dropping it re-opens cross-tenant token acceptance
   ([11-security-architecture](11-security-architecture.md) section 2, ADR-0033, ADR-0049).
2. **Fail-open is the rule for performance caches, fail-closed is the rule for security
   checks, and there is exactly one carve-out.** The carve-out is the email anti-abuse
   throttle (ADR-0038). The distrusted-key set (ADR-0039) and the proof-replay set are **not**
   exceptions; they follow the rule
   ([13-reliability-backup-and-dr](13-reliability-backup-and-dr.md) section 2, ADR-0040).
3. **This layer never introduces a decision.** Where a view found a load-bearing claim with no
   owner, the resolution was a new ADR rather than an assertion here, which is how ADR-0072,
   ADR-0073, and ADR-0074 came to exist. Where a view found a useful control that no decision
   covers, it is recorded in place as a candidate rather than adopted (ADR-0000, and the
   authority order in the [index](README.md)).

---

[Prev: Runtime views](06-runtime-views.md) · [Index](README.md) · Next: [Deployment](08-deployment.md)
