---
status: reviewed
created: 2026-07-18
tags: [design, index]
---

# Detailed feature designs

Per-feature design docs that elaborate *how* each part of Nami is built. They sit
one level below the [architecture overview](../architecture/README.md) (the
high-level C4 views) and are governed by the [ADR corpus](../adr/README.md), which
remains the authority: a design doc realizes decisions, it does not make them.

> **Decision rule.** If a detailed design surfaces a genuinely new decision that
> no ADR covers, it is raised as an ADR (or a Pre-GA checklist entry), never
> settled silently inside a design doc. Each doc's "Open / build-time items"
> section is where those are flagged.

Each doc follows one template: purpose and scope, the decisions it realizes,
component and interface design (including key libraries and their licenses), data
model, runtime flows, edge cases and failure modes, security considerations,
testing strategy, open and build-time items, and references. Design docs describe
the durable shape and rationale; the mechanical build recipe (exact scaffolding
commands, `.gitignore`, package version pins, CI file contents) lives in the
implementation plan, not here.

## Index

| # | Design | Status | Realizes (primary ADRs) |
|---|---|---|---|
| [01](01-foundations.md) | Foundations and solution structure | reviewed | 0024, 0027, 0052, 0065 |
| [02](02-data.md) | Data tier and multi-tenancy | reviewed | 0001, 0018, 0036, 0037, 0049 |
| 03 | Audit subsystem | planned | 0008, 0022 |
| 04 | Core protocol server | planned | 0004, 0005, 0014, 0048, 0049 |
| 05 | Authorization and delegated admin | planned | 0010, 0047, 0013 |
| 06 | User management and authentication | planned | 0028, 0013, 0003, 0002 |
| 07 | Email and notification subsystem | planned | 0038 |
| 08 | Login, consent, and logout UI | planned | 0019 |
| 09 | Key management and rotation | planned | 0005, 0006, 0007, 0011, 0012, 0033 |
| 10 | Revocation propagation and caching | planned | 0039, 0040 |
| 11 | Advanced flows | planned | 0014 |
| 12 | Admin API and Admin App | planned | 0020, 0015 |
| 13 | GDPR erasure and tenant provisioning | planned | 0016, 0017, 0053, 0054 |
| 14 | Observability, capacity, and SLO | planned | 0022, 0041, 0063 |
| 15 | Testing, CI/CD, and deployment | planned | 0060, 0025, 0023, 0031, 0051 |

Docs are produced in dependency order, one at a time. A `planned` row becomes a
linked `draft`, then `reviewed` once approved.
