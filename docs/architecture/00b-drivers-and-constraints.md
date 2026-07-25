---
status: reviewed
created: 2026-07-25
tags: [architecture, sad, drivers, constraints, nfr, principles]
---

# Architecture drivers and constraints

> **Part of:** the [Software Architecture Document](README.md), structural views.

This file states the forces that shape the architecture: the goals, the significant
quality attributes with measurable targets, and the hard constraints. Everything in
the later views should trace back to a driver or a constraint stated here.

## 1. Goals

Nami is an open-source, Apache-2.0 authorization server intended to provide
full-featured OAuth 2.0 and OpenID Connect capability with no license dependency on
a commercial product. The load-bearing goals:

1. **Build on a permissive engine, and never fork it.** OpenIddict (Apache-2.0) owns
   the protocol engine; custom protocol behaviour is an inserted event handler at a
   named, order-anchored position, never a modification of the engine (ADR-0021,
   ADR-0024, ADR-0061).
2. **Multi-tenant from day one.** Serve many tenants with strong isolation, pooled by
   default and siloed when a tenant needs it (ADR-0001, ADR-0033).
3. **Cloud-agnostic.** Run the same core on any cloud or on-premises, with provider
   specifics isolated behind ports and optional adapters, and a default that needs no
   cloud at all (ADR-0006, ADR-0009, ADR-0023).
4. **Production-grade security and operability.** Meet the OAuth 2.0 Security Best
   Current Practice (RFC 9700), rotate keys with no downtime, support erasure and the
   data-subject-rights suite, and be observable by default (ADR-0043, ADR-0011,
   ADR-0016, ADR-0053, ADR-0022).
5. **Consumable as a product, not a codebase to copy.** Ship as a NuGet
   meta-package plus a reference host image and a template, so a consumer adopts it
   rather than forks it (ADR-0027, ADR-0044).

## 2. Architecture drivers (the significant forces)

| # | Driver | Why it is architecturally significant | Primary response |
|---|---|---|---|
| D1 | Strong tenant isolation | A cross-tenant leak is the worst failure mode an identity system has | Pool and Silo tiers, per-tenant issuer, forced row-level security as a backstop, tenant-aligned key scope (ADR-0001, ADR-0033, ADR-0037, ADR-0049); see [05-data](05-data.md) |
| D2 | No-downtime key rotation | Signing keys must rotate without a restart and without breaking in-flight tokens | An options-reload seam, an overlap and retention window, a database key store (ADR-0011, ADR-0012); see [06-runtime-views](06-runtime-views.md) |
| D3 | Scale to roughly 10k concurrent users | Hot paths must not serialize on the identity provider or its database | Stateless nodes, self-contained JWT by default, short token lifetimes, externalized state, pooled connections (ADR-0041, ADR-0018, ADR-0039) |
| D4 | High availability | Identity is a hard dependency of every downstream application, so it must hold a higher target than they do | Multi-instance and multi-zone, replication with failover, graceful shutdown (ADR-0006, ADR-0041) |
| D5 | Revocation and logout freshness | Tokens, sessions, and admin grants must be revocable within a bounded, stated window | A per-path freshness model with a backplane only where a path needs one, back-channel single logout (ADR-0039, ADR-0019, ADR-0003) |
| D6 | Erasure and audit are in tension | Right-to-erasure and a tamper-evident audit chain pull in opposite directions | Chain-over-commitments plus crypto-shred, with a keyed hash-chain audit lane kept separate from diagnostics (ADR-0016, ADR-0008, ADR-0022) |
| D7 | Cloud portability | The same core must run on any provider or none | Ports at real infrastructure seams only, optional per-provider adapters (ADR-0024, ADR-0006) |
| D8 | Safe evolution | Federation, client registration, and change events must be addable without touching v1 behaviour | Additive design with kill switches (ADR-0034, ADR-0035, ADR-0071) |

## 3. Quality-attribute targets

> **Standing caveat:** roughly 10k concurrent users is an **architecture target, not a
> vendor benchmark**. No published throughput figure exists for this stack, so every
> absolute latency and throughput number is established by the project's own load test
> on the target infrastructure, and the SLO is the official gate (ADR-0041).

| Attribute | Target | How measured | Owner |
|---|---|---|---|
| Throughput | Established by load test at roughly 10k concurrent users | k6 or NBomber | ADR-0041 |
| Token-endpoint latency | p95 under 200 ms, p99 under 500 ms | Load test, enforced in CI as a build-failing threshold | ADR-0041 |
| Local token validation | p99 under 50 ms | Load test | ADR-0041 |
| Availability (token and authorize) | **99.9% or 99.95%, not yet ratified** | SLO monitor | ADR-0041 |
| JWKS availability | Around 99.99%, held higher than the rest | SLO monitor | ADR-0041 |
| Error budget | Exactly `1 - SLO` over the trailing window, so 0.1% at 99.9% and 0.05% at 99.95% | Drives the release freeze | ADR-0041 |
| Recovery | RTO under 15 minutes and RPO under 5 minutes, **interim and not yet ratified**, bound **per store** (keyring, certificates, operational database, session store) rather than as one global figure | DR drill plus continuous RPO monitoring on archiving lag, backup age, and replication lag | ADR-0006 |
| Client and scope config propagation | 30 seconds or less across nodes | Synthetic canary | ADR-0039 |
| Compromised-key ejection | A distrusted key propagates in 60 seconds or less through a fail-closed distrusted-key set; a resource server's own JWKS refresh floor is 5 minutes | Canary plus break-glass drill | ADR-0039, ADR-0007 |
| Token revocation | A reference token is revoked immediately through introspection; a self-contained JWT dies at expiry, bounded by the 15-minute access-token lifetime | Synthetic canary | ADR-0004, ADR-0039, ADR-0048 |
| Scalability | Horizontal and stateless, with no sticky sessions | Scale-out load test | ADR-0041, ADR-0031 |
| Security baseline | OAuth 2.0 Security BCP (RFC 9700); OWASP ASVS 5.0 Level 2, with Level 3 on key, token, dual-control, and tenant-isolation controls | Startup hardening-invariant check, security suite, self-assessment | ADR-0043, ADR-0062 |

Two rows in that table are deliberately not presented as settled, and for the same
reason: both are tracked as open items in the
[pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

* **Availability** is left as two candidates because the choice is an explicit Product
  and Ops ratification. Since the error budget is `1 - SLO` and drives the
  release-freeze threshold, quoting a single availability figure while the choice is
  open would set that threshold wrong by a factor of two.
* **RTO and RPO** are the interim figures ADR-0006 binds per store; the formal targets,
  the DR runbook, and the per-adapter capability matrix await Ops and DPO ratification.
  ADR-0006 records that these numbers were originally stated as targets without being
  wired to a concrete mechanism, which is precisely why the mechanism and the number are
  tracked separately here.

Representative quality-attribute scenarios, as stimulus, response, and measure:

* **Performance:** at steady state near the concurrency target, a token request
  returns under 200 ms at p95 while the operational store sustains the per-issuance
  write path.
* **Availability:** on loss of one availability zone the service keeps serving from
  the remaining zones with no manual step; on primary database failure, failover
  promotes a standby within the operational-database RTO.
* **Security and isolation:** a token issued for tenant A is rejected by a resource
  server scoped to tenant B through issuer binding, and a query executing in tenant
  A's request context cannot read tenant B's rows because row-level security
  backstops the query filter.
* **Operability:** a signing key rotates with no process restart and no validation
  failure for tokens in flight during the overlap window.

## 4. Constraints

### 4.1 Technical constraints (fixed)

The **authority for the committed stack is the ADR-0061 stack-of-record table**, which
is machine-checked against the ADRs it indexes. The subset below is the part that
actively constrains the architecture; where this table and ADR-0061 disagree, ADR-0061
wins and this one is the bug.

| Constraint | Value | Owning ADR |
|---|---|---|
| Runtime | .NET 10 | ADR-0030 |
| Protocol engine | OpenIddict 7.5, version-pinned and seam-isolated | ADR-0021 |
| Database engine | PostgreSQL 18, the sole engine, with `FORCE ROW LEVEL SECURITY` | ADR-0037 |
| ORM and driver | EF Core 10 with Npgsql, pooled `DbContext` | ADR-0037, ADR-0018 |
| Primary keys | UUIDv7, with one deliberate `bigint` identity exception for strict ordering | ADR-0036 |
| Signing baseline | RS256, with ES256 selectable by configuration | ADR-0005 |
| Audit integrity | A **keyed** hash-chain, `HMAC_k(PrevHash \|\| canonical(fields))`, application-held key, prev-first operands, canonicalized to text | ADR-0008 |
| Logging and telemetry | `Microsoft.Extensions.Logging` plus OpenTelemetry over OTLP; Serilog deliberately dropped | ADR-0022 |
| Infrastructure as code | OpenTofu (MPL-2.0, under the Linux Foundation) | ADR-0023 |

Two of these are constraints in the strong sense, because they close off an
alternative rather than merely selecting one. PostgreSQL is the **sole** engine, so no
engine-native ledger or append-only table feature is available and the audit chain is
built in application code. And the engine is pinned, so every dependency on its
internals is a catalogued seam carrying a contract-regression test that is re-run on
each bump (ADR-0021).

### 4.2 Organizational and regulatory constraints

* **Permissive OSS dependencies only** (ADR-0026): no commercial, copyleft, or
  source-available packages, enforced by a CI license-scan gate. This constraint is
  what forces "build the surrounding parts" rather than "buy them", and it is the
  reason several capabilities are built that a commercial product would supply.
* **Cloud-agnostic** (ADR-0006, ADR-0009, ADR-0025): provider specifics live only
  behind ports, the default path runs offline on PostgreSQL with no cloud dependency,
  and cloud adapters are optional but must each meet a mandatory capability set
  (versioning, soft-delete with a recovery window, purge protection, encryption at
  rest, access auditing).
* **Data-protection posture** (ADR-0016, ADR-0053, ADR-0054): erasure, retention,
  residency, and audit are designed for, and jurisdiction-specific parameters are a
  ratified profile rather than hardcoded. No compliance verdict is asserted anywhere
  in this repository; those belong to Legal and the data-protection owner.
* **Edge assumption (ADR-0073).** The reference deployment
  **assumes an L7 edge in front** of the identity provider (a WAF, CDN, or reverse
  proxy) carrying TLS termination policy, IP-reputation and bot filtering, geographic
  and per-IP velocity rules, request and header size caps, and L7 denial-of-service
  absorption. These responsibilities **do not disappear** in a direct-to-internet
  deployment: they fall to Kestrel hardening (request body and header limits,
  connection limits, timeouts) plus the in-application rate limiting and lockout of
  ADR-0040 and ADR-0042, with a materially lower ceiling for volumetric attack. The
  two layers are complementary rather than alternatives: the edge handles volumetric
  traffic and the application handles per-user and per-client fairness. Anyone
  self-hosting the reference host must decide explicitly which posture they are in.
  ADR-0073 also pins the consequence that makes this load-bearing rather than advisory:
  behind a terminating proxy, forwarded headers must be processed and restricted to
  trusted proxies, because a lost scheme defeats the ADR-0043 cookie invariants, a lost
  client address collapses per-IP defenses into a single global bucket, and an
  unvalidated forwarded host becomes an input to host-based tenant resolution
  (ADR-0001). The concrete edge stack and the trusted-proxy ranges are an Ops
  ratification tracked in the
  [pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

### 4.3 Architecture-style constraint (ADR-0024)

One style applies to both deployables, differing in weight and not in rule.

* **Macro, mandatory:** the dependency rule is
  `Domain <- Application <- (Infrastructure | Presentation)`, with **ports and
  adapters only at real infrastructure seams** (persistence through EF Core, key,
  secret, and data-protection stores, the audit sink, the tenant store and resolver,
  and the `ICheckAccess` authorization port). A single-implementation interface
  created merely to satisfy layering is noise: a port must have **at least two real
  reasons** to exist, namely a swap, a test, or a genuine boundary.
* **The one acknowledged exception:** `Nami.Identity.Bff`, which composes YARP with
  `Duende.AccessTokenManagement.OpenIdConnect` (Apache-2.0), is a real infrastructure
  edge with **no port**. It is a composition boundary: the seam is configuration and
  the adapter is the proxy and token-management libraries themselves, so it meets
  neither the swap nor the in-process-fake reason (ADR-0029).
* **Micro, mandatory:** organize the Application layer by **feature slice**,
  `Features/<Area>/<UseCase>/`, each slice grouping its request, handler, validator,
  and response, and never by technical folder such as `Services/` or `DTOs/`.
* **Enforcement:** an architecture-test suite, `Nami.Identity.ArchitectureTests`,
  using TngTech.ArchUnitNET (Apache-2.0), asserting that Domain references no engine,
  ORM, or cloud SDK, that Application references neither a cloud SDK nor the engine,
  that one slice does not reference another except through Contracts or Domain, that
  cloud adapters live in Infrastructure, and that the BFF does not reference the admin
  assemblies.

This constraint is why the container and component views show ports at the
infrastructure edges but flat, pipeline-shaped internals for the protocol host.

### 4.4 Documentation constraints

* Decision references are written as `ADR-NNNN` and are machine-checked against the
  corpus, so a number that resolves nowhere fails the build.
* No em dash anywhere, enforced by the same guardrail.
* Every architecture file ends with a `Sources` section.
* No file may name the direct commercial competitor or a real client organization.
  Packages Nami actually depends on keep their real identifiers, because hiding a
  dependency's identifier would make the dependency record unusable by the
  license-scan gate (ADR-0026).

## 5. Architecture principles

The structure follows a small set of binding principles (ADR-0058, ADR-0024,
ADR-0066).

* **Hexagonal shell, vertical slices inside.** A dependency rule with ports and
  adapters only at the infrastructure edge; feature logic is organized as slices, not
  technical-layer folders.
* **One extension mechanism for the protocol.** Custom protocol behaviour is an
  inserted event handler at a named, order-anchored position, never a fork of the
  engine (ADR-0021, ADR-0024).
* **Managers, not stores.** Application code depends on the engine's manager facades,
  never on the underlying stores or the `DbContext` directly.
* **Deny by default on claims.** A single claims choke-point emits nothing into a
  token unless it is explicitly declared for a destination (ADR-0005).
* **Isolate tenants in two layers.** An EF Core global query filter is the primary
  control and forced row-level security under a de-privileged role is the database-level
  backstop, because in pooled mode the filter is a load-bearing security control and a
  single forgotten filter is a cross-tenant leak (ADR-0001, ADR-0037, ADR-0049).
* **Audit is a separate lane.** The tamper-evident hash-chain trail is kept strictly
  apart from operational logging and telemetry, and the two lanes are joined only by a
  correlation identifier (ADR-0008, ADR-0022).
* **Version adaptation is first-class.** Every dependency on the internals of
  OpenIddict, EF Core, Npgsql, or the multi-tenancy library is a catalogued seam with
  a contract-regression test, re-verified on every bump (ADR-0021).
* **Permissive dependencies only**, enforced by a license-scan gate (ADR-0026), with
  the committed stack recorded in ADR-0061.
* **Start simple, and let a pattern earn its place** (ADR-0066): no abstraction is
  introduced before a second real need for it exists.

## 6. Sources

* ADR-0061 (the stack of record and its selection rules, the authority for section
  4.1), ADR-0030 (runtime), ADR-0021 (engine pinning and the seam catalogue),
  ADR-0037 and ADR-0018 (database engine, row-level security, pooling), ADR-0036
  (primary keys and the ordering exception), ADR-0005 (signing baseline and
  deny-by-default claims), ADR-0008 (the keyed audit hash-chain), ADR-0022 (logging
  and observability, and the two-lane split), ADR-0023 (infrastructure as code).
* ADR-0041 (the NFR targets, the SLO release gate, and the error-budget formula, the
  authority for section 3), ADR-0006 (per-store RTO and RPO, cloud-agnostic key
  material, and continuous RPO monitoring), ADR-0039 (the per-path freshness model
  with the 30-second config and 60-second break-glass bounds), ADR-0007 (the
  five-minute compromised-key ejection), ADR-0011 and ADR-0012 (no-restart rotation and
  the key bootstrap and restore sequence, the mechanism behind driver D2), ADR-0004 and
  ADR-0048 (token lifetimes and
  the reference-token instant-revocation path), ADR-0043 and ADR-0062 (the security
  baselines).
* ADR-0001, ADR-0033, ADR-0049 (tenant isolation), ADR-0019 and ADR-0003 (logout and
  sessions), ADR-0016 and ADR-0053 and ADR-0054 (erasure, data-subject rights,
  residency), ADR-0034 and ADR-0035 and ADR-0071 (the additive evolution features),
  ADR-0026 (the dependency policy), ADR-0027 and ADR-0044 (packaging and the API
  seam), ADR-0040 and ADR-0042 (resiliency, overload protection, abuse defense),
  ADR-0024 and ADR-0029 (the architecture style and the BFF port exception), ADR-0058
  and ADR-0066 (the principles), ADR-0031 (the operational baseline), ADR-0009 and
  ADR-0025 (secret store and local development).
* Reconciled against the design corpus's architecture layer on 2026-07-25. Three
  corrections were made rather than transcribed: the corpus stated availability as
  "99.9% or better" in one view and a settled 99.95% with a 0.05% error budget in
  another while a third listed the choice as still open, so this file follows ADR-0041
  and states the choice as unratified with the budget as a formula; the corpus
  described the audit chain as a plain manual hash-chain, where ADR-0008 requires the
  keyed HMAC form; and the corpus's documentation-convention section recorded a
  name-placeholder rule and an organization-specific compliance label that do not
  apply to this repository.
* The section 4.2 edge assumption came from the design corpus's
  non-functional-requirements document (section 7.11bis, pre-implementation review dated
  2026-07-13). When this file was first written it was the one claim here with no ADR of
  record, and was stated as an open item on that basis. **ADR-0073 now owns it**, having
  been written the same day precisely because an infrastructure assumption that
  load-bearing should not sit in the architecture layer without a decision behind it. That
  ADR also independently verified the forwarded-header behaviour it depends on, and
  deliberately records one remaining gap: the application's own transport-security settings
  (HSTS parameters and the Kestrel TLS floor) are still not fixed by any ADR.
