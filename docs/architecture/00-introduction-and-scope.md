---
status: reviewed
created: 2026-07-25
tags: [architecture, sad, scope, introduction]
---

# Introduction and scope

> **Part of:** the [Software Architecture Document](README.md), structural views.

## 1. Purpose

This is the architecture layer for **Nami**, an open-source, multi-tenant
OAuth 2.0 and OpenID Connect identity provider for .NET, built on OpenIddict 7.5
and .NET 10 and released under Apache-2.0 (ADR-0061, ADR-0027). It is a permissive
alternative to the commercial identity servers: the protocol engine is OpenIddict,
which Nami never hand-rolls, and Nami's value is the opinionated,
batteries-included layer above it.

Three things are first-class here by design:

* **Native multi-tenancy**, with tiered Pool and Silo isolation and a delegated
  administration model (ADR-0001, ADR-0010, ADR-0033).
* **No-restart key rotation**, so signing and encryption keys rotate without a
  process restart, with provider-agnostic disaster recovery (ADR-0011, ADR-0012,
  ADR-0006).
* **Cloud-agnostic ports** for the key store, secret store, data protection, email,
  and observability, whose default runs offline on PostgreSQL with no cloud
  dependency (ADR-0006, ADR-0009, ADR-0038, ADR-0022).

This layer exists to give one **coherent architectural picture** across a repository
whose detail is spread over 72 ADRs and 18 detailed designs. It answers, at the
architecture altitude:

* What is the system, who uses it, and what does it depend on?
  ([01-context](01-context.md), [02-domain](02-domain.md))
* What are its deployable parts, and how do they communicate?
  ([03-containers](03-containers.md), [04-components](04-components.md))
* How do the important flows run at runtime? ([06-runtime-views](06-runtime-views.md))
* How is data structured, stored, and isolated? ([05-data](05-data.md))
* What forces and hard constraints shape it?
  ([09-drivers-and-constraints](09-drivers-and-constraints.md))
* Which concerns cut across every container? ([07-cross-cutting](07-cross-cutting.md))
* Why were the load-bearing decisions made? ([the ADR corpus](../adr/README.md))

It is **not** an implementation task list and **not** a replacement for the detailed
designs. It points into them.

## 2. Audience

| Audience | Primary entry point |
|---|---|
| New contributors and reviewers | This file, then [01-context](01-context.md) and [03-containers](03-containers.md) |
| Architects | The full set, especially [03-containers](03-containers.md), [04-components](04-components.md), and [the ADR corpus](../adr/README.md) |
| Operations and SRE | [08-deployment](08-deployment.md), [07-cross-cutting](07-cross-cutting.md) |
| Security, DPO, and Legal | [07-cross-cutting](07-cross-cutting.md) and the [pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md); compliance verdicts remain theirs to make |
| Integrators building relying-party apps | [01-context](01-context.md), then [`docs/design/`](../design/README.md) |

## 3. Scope

### 3.1 In scope (v1, production target)

The v1 authorization server and its administration, multi-tenant from day one.

* **Protocol surface**, all on native OpenIddict wire grants (ADR-0014): authorization
  code with PKCE, client credentials (machine-to-machine via `private_key_jwt`,
  ADR-0009), refresh with rolling rotation and reuse detection (ADR-0004), device
  code, pushed authorization requests (RFC 9126, enabled per client), introspection,
  revocation, end-session, discovery, JWKS, and userinfo. Token exchange (RFC 8693)
  uses the native grant, while the actor and subject resolution is Nami's own code.
* **Multi-tenancy** (ADR-0001, ADR-0033): a tiered model with a pooled database by
  default and a dedicated silo database for tenants that need one, with row-level
  isolation as the backstop (ADR-0037) and per-tenant issuer binding at the resource
  server (ADR-0049).
* **User management and authentication** (ADR-0028): ASP.NET Core Identity, MFA with
  TOTP and recovery codes, passkeys, external login through the framework handler
  (ADR-0002), server-side sessions as a core feature rather than an option
  (ADR-0003), and assurance levels with step-up (ADR-0013).
* **User interface**: Razor Pages for login, consent, logout, tenant switch, and
  step-up.
* **Administration** (ADR-0020): an Admin API and an Admin App over a
  backend-for-frontend (ADR-0029), capability-scoped delegated administration with no
  super-admin (ADR-0010), dual-control on irreversible actions, break-glass access
  (ADR-0015), and a tamper-evident audit trail (ADR-0008).
* **Sender-constrained tokens** (ADR-0014): mTLS (RFC 8705), which is native, and
  DPoP (RFC 9449), which is built because the engine provides neither issuance nor
  validation for it.
* **Key management**: no-restart signing-key rotation (ADR-0011), a database key
  store with envelope encryption and cold-start auto-seed (ADR-0012), the ASP.NET
  Core Data Protection keyring, provider-agnostic disaster recovery (ADR-0006), and a
  compromised-key break-glass path that ejects a key from the JWKS within five
  minutes (ADR-0007).
* **Cross-cutting subsystems**: per-path revocation and cache coherence (ADR-0039),
  single logout by back-channel (ADR-0019), email and notification with a
  transactional outbox (ADR-0038), right-to-erasure reconciled with the audit chain
  (ADR-0016) and the wider data-subject-rights suite (ADR-0053), tenant lifecycle and
  provisioning (ADR-0017), observability (ADR-0022) with NFR targets and an SLO
  release gate (ADR-0041), resiliency and overload protection (ADR-0040), the
  12-factor operational baseline (ADR-0031) and the configuration layer (ADR-0052),
  and packaging and distribution (ADR-0027).

### 3.2 In scope as evolution outlook (design only)

Recorded at architecture level, each additive and kill-switched so it cannot alter
v1 behaviour:

* Dynamic per-tenant external identity providers, that is self-service federation
  (ADR-0034).
* Self-service client registration (ADR-0035).
* Identity change-event publishing over a transactional outbox to a broker
  (ADR-0071).

These are **design-complete but not built**. This layer describes their intended
shape; it does not claim they exist.

### 3.3 Out of scope

* **Code-level detail (C4 Level 4)**: class shapes, method signatures, and field
  types belong to the detailed designs in [`docs/design/`](../design/README.md).
* **Compliance verdicts**: whether a mechanism satisfies GDPR Article 17, a
  residency regime, or any other legal obligation is reserved for the Legal and
  data-protection owners of the deploying organization (ADR-0016, ADR-0053,
  ADR-0054).
* **Protocols deliberately de-scoped by decision** (ADR-0014): JARM, RAR (RFC 9396),
  EdDSA, JAR (RFC 9101), front-channel logout with `check_session_iframe` (dropped in
  ADR-0019 as third-party cookies are deprecated), and CIBA, which is skipped because
  the engine has neither support nor a roadmap item.
* **The standards dynamic client registration endpoint**, which waits for the native
  OpenIddict 8.0 implementation; v1 onboards clients through the authenticated Admin
  API (ADR-0035).
* **Capabilities recorded as `proposed` and demand-driven**, which are directions
  rather than commitments and pin no library: SAML 2.0 and WS-Federation (ADR-0055),
  FAPI 2.0 (ADR-0056), Windows integrated authentication (ADR-0057), acting as the
  authorization server for Model Context Protocol servers (ADR-0064), shared signals
  and continuous access evaluation (ADR-0068), and verifiable-credential issuance
  (ADR-0069).

## 4. Relationship to the detailed designs

This layer stops where a module's internal contract begins. Below it sits the
detailed-design set in [`docs/design/`](../design/README.md), currently 18 documents,
one per cohesive module: foundations, data and multi-tenancy, audit, core protocol,
authorization, user management, email and notification, login and consent UI, key
management, revocation and caching, advanced flows, admin API, admin app, erasure and
data-subject rights, tenant lifecycle, observability and capacity, testing, and
CI/CD and deployment.

The split is deliberate: module boundaries are fixed at this altitude so the detail
can be written against them, and a detailed design elaborates a view here rather than
restating it.

## 5. Document conventions

* **Language:** English throughout the repository.
* **Diagrams:** Mermaid. Structural views are styled flowcharts following C4
  semantics with one shared colour system (see [README section 2](README.md)); runtime
  and data views use `sequenceDiagram` and `erDiagram`. Node labels stay short, and the
  detail lives in the table beneath each diagram.
* **Decision references:** an ADR is cited as `ADR-NNNN`. Every such reference is
  machine-checked against the corpus by
  [`scripts/check-adrs.sh`](../../scripts/check-adrs.sh), so a number that resolves
  nowhere fails the build.
* **No em dash**, a project style rule enforced by the same guardrail. A comma,
  colon, or parentheses is used instead.
* **Traceability:** each file ends with a `Sources` section naming the exact
  documents it derives from.

## 6. Sources

* [`CLAUDE.md`](../../CLAUDE.md): repository conventions, the docs and KB boundary,
  and the content rules this layer observes.
* [`docs/adr/README.md`](../adr/README.md) and ADR-0001 through ADR-0071: the
  decisions of record. Specifically cited above: ADR-0001, ADR-0002, ADR-0003,
  ADR-0004, ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0010, ADR-0011, ADR-0012,
  ADR-0013, ADR-0014, ADR-0016, ADR-0017, ADR-0019, ADR-0020, ADR-0022, ADR-0026,
  ADR-0027, ADR-0028, ADR-0029, ADR-0031, ADR-0033, ADR-0034, ADR-0035, ADR-0037,
  ADR-0038, ADR-0039, ADR-0040, ADR-0041, ADR-0049, ADR-0052, ADR-0053, ADR-0054,
  ADR-0055, ADR-0056, ADR-0057, ADR-0061, ADR-0064, ADR-0068, ADR-0069, ADR-0071.
* [`docs/design/README.md`](../design/README.md): the detailed-design set this layer
  points into.
* [`docs/PRE-GA-RATIFICATION-CHECKLIST.md`](../PRE-GA-RATIFICATION-CHECKLIST.md): the
  deferred human sign-offs referenced from the scope-of-claims statement.
* Reconciled against the design corpus's architecture layer on 2026-07-25. The
  corpus stated the ADR and design-document counts, the language boundary, and a
  name-placeholder convention that do not hold in this repository; those were
  corrected rather than transcribed, and the corpus's out-of-scope list carried one
  item with no decision of record here, which was dropped rather than asserted.
