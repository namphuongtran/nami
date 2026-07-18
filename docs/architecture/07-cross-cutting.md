---
status: reviewed
created: 2026-07-18
tags: [architecture, cross-cutting]
---

# Cross-cutting concerns

Concerns that span every container rather than living in one of them.

## Multi-tenancy and isolation

Identity (users and roles) is global; the OpenIddict application, authorization,
and token entities are tenant-scoped; the scope catalog is global. Pool tenants
share a database with a `TenantId` column, an EF query filter, and FORCE RLS; Silo
tenants get a dedicated database and key set. Because Pool tenants can share a
pool-group key set, a signature alone is not an isolation boundary: issuer and
tenant binding are what isolate resource validation (ADR-0001, ADR-0033,
ADR-0049).

## Security posture

OWASP ASVS is the verification baseline, L2 as the floor and L3 for key, token,
dual-control, and tenant-isolation paths (ADR-0062). A fail-fast startup
self-check enforces the hardening invariants: HTTPS issuer, `Secure` cookies,
PKCE, encryption, and no degraded mode in a token-issuing environment (ADR-0043).
Abuse defense layers beyond rate limiting and lockout (ADR-0042).

## Key management

RS256 baseline with an asymmetric-only invariant, encryption credential lifecycle
tracked separately from signing, per-tier key scope, and provider-agnostic DR that
restores the signing keys, the data protection keyring, and the root certificate
together (ADR-0005, ADR-0011, ADR-0012, ADR-0033).

## Audit and observability: two lanes

Two lanes that never cross (ADR-0022, ADR-0008):

* **Audit lane**: `ISecurityEventSink`, append-only, hash-chained, delivery-guaranteed,
  forwarded to a WORM/SIEM destination through an outbox, with a periodic integrity
  job. Covers the negative paths (failures, denials, errors).
* **Diagnostics lane**: native `ILogger` plus OpenTelemetry (OTLP) for logs,
  metrics, and traces, with PII redaction. The backend is operator-chosen; a
  self-hosted Grafana stack serves local development (ADR-0063).

The two are joined only by a correlation/trace id.

## Endpoint isolation and CORS

Introspection and revocation are client-authenticated and audience-confined, and
are handled natively rather than through a custom controller (ADR-0048). CORS is
per-client through a custom policy provider, not a static global policy (ADR-0050).

## Resiliency and overload

One outbound resiliency handler (Polly), rate limiting distinct from load
shedding, and Redis as a fail-open accelerator with deliberate fail-closed
carve-outs (the email anti-abuse throttle and the distrusted-kid check)
(ADR-0040). Capacity is modelled and load-tested to an SLO that is a release gate
(ADR-0041).

## Privacy and compliance

Right-to-erasure reconciles with the immutable audit chain through
chain-over-commitments and per-subject crypto-shred (ADR-0016). The
data-subject-rights suite (access and portability), consent receipts, and breach
hooks are reusable mechanisms (ADR-0053), and data residency and cross-border
personal-data transfer are jurisdiction-profiled controls (ADR-0054). Several of
these carry DPO/Legal sign-off items in the Pre-GA checklist.

## Quality attributes

Performance and availability targets are self-load-tested and the SLO is a formal
release gate, with burn-rate alerting and an external synthetic canary (ADR-0041).
Availability rests on stateless scale-out, no-restart key rotation (ADR-0011), and
a per-store RTO/RPO with the data protection keyring the strictest (ADR-0006). The
concrete SLO numbers and the error-budget policy are an Ops ratification item
before GA.

## Version adaptation

Every OpenIddict, EF Core, Npgsql, and Finbuckle touchpoint is a catalogued seam
with a contract-regression test and a decommission marker. Build-interim features
(DPoP, back-channel logout, DCR) retire when the engine ships a native equivalent
(ADR-0021).

## Governance and supply chain

ADR-driven decisions, DCO sign-off, and dual-control releases (ADR-0046);
keyless signing and provenance attestation for release artifacts (ADR-0051);
permissive-OSS-only dependencies enforced by a license-scan gate (ADR-0026); and
an AI-assisted development policy (ADR-0067).

---

[← Prev: Runtime views](06-runtime-views.md) · [Index](README.md) · Next: [Deployment →](08-deployment.md)
