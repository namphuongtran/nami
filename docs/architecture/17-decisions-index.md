---
status: reviewed
created: 2026-07-26
tags: [architecture, decisions, index, navigation]
---

# Decisions index

> **Part of:** the [Software Architecture Document](README.md), decisions and evolution.

**This is the reverse index, and it deliberately does not restate any decision.** The forward
direction, view to decision, already exists: every view ends with a `Sources` section naming
the ADRs it rests on, maintained in the same change as the view. What no other document
answers is the reverse question, which is the one asked when a decision changes:

> I am about to change ADR-NNNN. **Which views must I re-read?**

The decision text itself lives in the ADR, and [`docs/adr/README.md`](../adr/README.md) is the
canonical one-line list. The `Decision` column below is the ADR's own title, quoted so a reader
can scan, not a paraphrase that could drift from it. **When a view and an ADR disagree, the ADR
wins and the view is the bug**, because an accepted decision binds until superseded (ADR-0000).

## 1. How this table stays true

It is **generated from the views' `Sources` sections**, not hand-maintained, so it cannot
silently drift from them. Regenerate and diff with:

```bash
python3 - <<'EOF'
import re,glob,os
v={os.path.basename(p):set(re.findall(r'ADR-(\d{4})',open(p).read()))
   for p in glob.glob('docs/architecture/*.md') if 'README' not in p}
for p in sorted(glob.glob('docs/adr/[0-9][0-9][0-9][0-9]-*.md')):
    n=os.path.basename(p)[:4]
    print(n, sorted(f.split('-')[0] for f in v if n in v[f]) or 'NONE')
EOF
```

A caveat worth stating rather than hiding: the generator counts **any** mention of an ADR
number in a view, including one inside that view's own `Sources` list or in a passing
cross-reference. So a listed view is one that **touches** the decision, not necessarily one
that depends on it. For the "what must I re-read" question that is the right side to err on.

## 2. ADR to view

| ADR | Decision | Views that cite it |
|---|---|---|
| [0000](../adr/0000-use-markdown-architectural-decision-records.md) | Use Markdown Architectural Decision Records (MADR) with the f... | 07 |
| [0001](../adr/0001-multi-tenant-isolation-model.md) | Tiered multi-tenant isolation: global identity, pooled tenant... | 00a, 00b, 01, 02, 03, 04, 05, 06, 07, 10, 11, 14 |
| [0002](../adr/0002-federation-external-idp-integration.md) | Integrate external identity providers through ASP.NET Core Id... | 00a, 01, 04 |
| [0003](../adr/0003-server-side-sessions-are-core.md) | Server-side session store is a core feature, not an option | 00a, 00b, 02, 04, 05, 06, 10, 11, 12 |
| [0004](../adr/0004-refresh-token-posture.md) | Keep OpenIddict's native refresh-token mechanics rather than... | 00a, 00b, 03, 06, 10, 11, 12, 15, 16 |
| [0005](../adr/0005-encryption-credential-lifecycle.md) | Track the encryption credential's lifecycle separately from t... | 00b, 03, 04, 06, 07, 11, 12 |
| [0006](../adr/0006-disaster-recovery-key-material.md) | Make key-material storage and disaster recovery provider-agno... | 00a, 00b, 01, 03, 04, 05, 07, 08, 10, 11, 12, 13, 14, 16 |
| [0007](../adr/0007-key-compromise-break-glass-runbook.md) | Eject a compromised key from the JWKS within five minutes wit... | 00a, 00b, 01, 06, 08, 11, 15, 16 |
| [0008](../adr/0008-audit-subsystem.md) | Make the audit subsystem first-class, tamper-evident, and del... | 00a, 00b, 01, 03, 04, 05, 06, 07, 08, 11, 13, 14, 15 |
| [0009](../adr/0009-secret-store-access-and-rollover.md) | Access the secret store with least-privilege workload identit... | 00a, 00b, 01, 03, 04, 08, 11 |
| [0010](../adr/0010-tenant-hierarchy-delegated-admin.md) | Administer child tenants through explicit, scoped delegated-a... | 00a, 01, 02, 04, 05, 06, 11, 16 |
| [0011](../adr/0011-no-restart-key-rotation.md) | Rotate signing and encryption keys without restarting, via a... | 00a, 00b, 03, 04, 06, 07, 08, 10, 11, 12, 13, 16 |
| [0012](../adr/0012-key-bootstrap-and-dr-sequence.md) | Bootstrap keys by auto-seeding at cold start, root the keyrin... | 00a, 00b, 04, 05, 06, 07, 08, 11, 13, 15, 16 |
| [0013](../adr/0013-mfa-assurance-and-step-up.md) | Make MFA the producer of acr/amr/auth_time and enforce step-u... | 00a, 04, 06 |
| [0014](../adr/0014-advanced-protocol-scope.md) | Build both mTLS and DPoP sender-constrained tokens, and delib... | 00a, 03, 06, 08, 11, 12, 13, 16 |
| [0015](../adr/0015-admin-break-glass-and-first-admin-bootstrap.md) | Provide an OIDC-independent admin break-glass path and a one-... | 00a, 01, 08, 11, 16 |
| [0016](../adr/0016-right-to-erasure.md) | Reconcile GDPR right-to-erasure with the immutable audit chai... | 00a, 00b, 05, 06, 07, 10, 11, 13, 14 |
| [0017](../adr/0017-tenant-provisioning-and-silo-migration.md) | Orchestrate the tenant lifecycle with build-artifact migratio... | 00a, 05, 06, 08, 10, 14, 16 |
| [0018](../adr/0018-dbcontext-pooling-for-pool-mode.md) | Register the Pool-mode OpenIddict DbContext non-pooled in v1,... | 00b, 03, 04, 05, 06, 07, 08, 12, 13 |
| [0019](../adr/0019-single-logout-strategy.md) | Achieve single logout with an interim back-channel logout on... | 00a, 00b, 01, 03, 04, 05, 06, 16 |
| [0020](../adr/0020-admin-architecture.md) | Split admin into a REST API and an MVC Razor BFF app, enforce... | 00a, 02, 03, 04, 05, 06, 11, 16 |
| [0021](../adr/0021-openiddict-version-adaptation.md) | Adapt to OpenIddict version upgrades with seam isolation, per... | 00b, 04, 06, 07, 10, 14, 15, 16 |
| [0022](../adr/0022-logging-and-observability-stack.md) | Use native ILogger plus OpenTelemetry (OTLP) for logging and... | 00a, 00b, 01, 03, 04, 07, 08, 15 |
| [0023](../adr/0023-iac-tool-opentofu.md) | Use OpenTofu as the default infrastructure-as-code tool inste... | 00b, 08 |
| [0024](../adr/0024-architecture-style.md) | Adopt a hexagonal shell (dependency rule plus ports/adapters)... | 00b, 02, 03, 04, 05, 08 |
| [0025](../adr/0025-local-development-and-first-run.md) | Run locally with docker-compose dependencies, multi-stage Doc... | 00b, 03, 08, 14, 16 |
| [0026](../adr/0026-dependency-license-policy.md) | Restrict dependencies to permissive OSS licenses, enforced by... | 00b, 07, 11, 15 |
| [0027](../adr/0027-packaging-and-distribution.md) | Distribute Nami as a hybrid NuGet meta-package plus a referen... | 00a, 00b, 03, 04, 08, 16 |
| [0028](../adr/0028-user-management.md) | Build user management on ASP.NET Core Identity with native pa... | 00a, 01, 03, 04, 05, 16 |
| [0029](../adr/0029-bff.md) | Build a Nami.Identity.Bff package by composing OSS-permissive... | 00a, 00b, 03, 04, 06, 11 |
| [0030](../adr/0030-dotnet-version-upgrade.md) | Upgrade .NET on an LTS-to-LTS cadence, with multi-target pack... | 00b, 07, 16 |
| [0031](../adr/0031-twelve-factor-baseline.md) | Adopt the 12-factor (and 15-factor) methodology as the operat... | 00a, 00b, 03, 04, 06, 08, 10, 12, 13, 15, 16 |
| [0032](../adr/0032-usage-visibility-and-licensing-posture.md) | Gain usage visibility through free registration and opt-in te... | 08 |
| [0033](../adr/0033-key-scope-isolation-model.md) | Align key-scope isolation to the tenant tier with one keyset... | 00a, 00b, 01, 02, 05, 06, 07, 11 |
| [0034](../adr/0034-dynamic-external-idp.md) | Open dynamic per-tenant external IdP federation as a v2 self-... | 00a, 00b, 01, 04 |
| [0035](../adr/0035-self-service-client-registration.md) | Offer self-service client registration through the authentica... | 00a, 00b, 16 |
| [0036](../adr/0036-database-key-strategy-uuidv7.md) | Use UUIDv7 as the clustered primary key for every entity, wit... | 00b, 03, 04, 05, 06, 12 |
| [0037](../adr/0037-database-engine-postgresql.md) | Use PostgreSQL as the sole database engine | 00a, 00b, 03, 04, 05, 06, 07, 08, 11, 12, 14 |
| [0038](../adr/0038-email-notification-subsystem.md) | Build email delivery as a first-class, cloud-agnostic subsyst... | 00a, 01, 03, 04, 05, 06, 07, 11 |
| [0039](../adr/0039-revocation-propagation-and-cache-coherence.md) | Achieve cross-node revocation freshness per-path, with no bac... | 00a, 00b, 03, 05, 06, 07, 08, 10, 11, 12, 13, 14, 16 |
| [0040](../adr/0040-resiliency-and-overload-protection.md) | Standardize a resiliency and overload-protection posture: one... | 00a, 00b, 01, 03, 07, 08, 11, 12, 13, 15, 16 |
| [0041](../adr/0041-nfr-targets-and-slo-release-gate.md) | Adopt self-load-tested NFR targets and make the SLO a formal... | 00a, 00b, 07, 08, 10, 12, 13, 15, 16 |
| [0042](../adr/0042-abuse-and-bot-defense.md) | Add a layered anti-automation and abuse-defense posture beyon... | 00b, 01, 07, 08, 11, 15, 16 |
| [0043](../adr/0043-security-hardening-invariants-startup-check.md) | Enforce security hardening invariants with a fail-fast startu... | 00b, 04, 07, 11 |
| [0044](../adr/0044-public-api-stability-and-semver.md) | Treat the public API as a versioned seam governed by an analy... | 00b |
| [0045](../adr/0045-security-disclosure-and-cve-policy.md) | Handle security vulnerabilities through private coordinated d... | **none** |
| [0046](../adr/0046-governance-and-contribution-model.md) | Adopt an ADR-driven, DCO-based OSS governance and contributio... | 07, 08 |
| [0047](../adr/0047-authorization-decision-engine.md) | Compute authorization with a DB-first engine behind a consist... | 04, 06 |
| [0048](../adr/0048-introspection-revocation-endpoint-isolation.md) | Isolate the introspection and revocation endpoints with clien... | 00b, 01, 03, 06, 07, 11, 12 |
| [0049](../adr/0049-resource-server-per-tenant-validation.md) | Isolate tenants at the resource server by issuer and tenant b... | 00a, 00b, 01, 03, 04, 06, 07, 10, 11 |
| [0050](../adr/0050-per-client-cors-policy.md) | Provide per-client CORS through a custom policy provider, not... | 07, 11 |
| [0051](../adr/0051-release-supply-chain-integrity.md) | Sign and attest release artifacts with keyless provenance for... | 07, 08, 11, 16 |
| [0052](../adr/0052-ergonomic-config-layer.md) | Build an ergonomic, fail-closed configuration layer for decla... | 00a, 08, 16 |
| [0053](../adr/0053-data-subject-rights-suite.md) | Build the data-subject-rights suite (access, portability, rec... | 00a, 00b, 06, 07, 10, 11 |
| [0054](../adr/0054-cross-border-transfer-and-data-residency.md) | Make data residency and cross-border personal-data transfer f... | 00a, 00b, 05, 06, 07, 10, 11, 14 |
| [0055](../adr/0055-saml-ws-federation-support.md) | Support SAML 2.0 and WS-Federation through a demand-driven fe... | 00a |
| [0056](../adr/0056-fapi-2-conformance.md) | Support FAPI 2.0 high-assurance profiles through a demand-dri... | 00a |
| [0057](../adr/0057-windows-negotiate-authentication.md) | Support Windows integrated authentication (Negotiate/Kerberos... | 00a |
| [0058](../adr/0058-guiding-architectural-principles.md) | Adopt Separation of Concerns and pragmatic SOLID as binding a... | 00b, 02 |
| [0059](../adr/0059-value-objects-and-aggregate-boundaries.md) | Model value objects as complex types and gate aggregates on a... | 02 |
| [0060](../adr/0060-testing-strategy.md) | Consolidate the testing strategy and adopt behavior-first tes... | 03, 13 |
| [0061](../adr/0061-technology-stack-of-record.md) | Record the committed technology stack and its cross-cutting s... | 00a, 00b, 03 |
| [0062](../adr/0062-owasp-asvs-security-baseline.md) | Adopt OWASP ASVS as the security-verification baseline | 00b, 07, 10, 11 |
| [0063](../adr/0063-observability-backend-and-dev-visualization.md) | Keep the observability backend operator-chosen and run a self... | 07, 15 |
| [0064](../adr/0064-mcp-authorization-server-support.md) | Support Nami as the OAuth authorization server for MCP servers | 00a |
| [0065](../adr/0065-coding-and-naming-conventions.md) | Adopt the Microsoft naming and C# coding conventions as an en... | 03, 05, 06, 08 |
| [0066](../adr/0066-design-patterns-vocabulary-and-pragmatic-use.md) | Adopt design patterns as a shared vocabulary applied pragmati... | 00b |
| [0067](../adr/0067-ai-assisted-development-governance.md) | Adopt an AI-assisted development policy: human-accountable, d... | 07, 16 |
| [0068](../adr/0068-continuous-access-evaluation-shared-signals.md) | Support continuous access evaluation via the OpenID Shared Si... | 00a |
| [0069](../adr/0069-verifiable-credentials-openid4vc.md) | Support issuing Verifiable Credentials via OpenID4VC (Nami as... | 00a |
| [0070](../adr/0070-local-development-tls.md) | Serve HTTPS in local development with a locally-trusted cert... | 08 |
| [0071](../adr/0071-identity-change-event-publishing.md) | Publish identity change events outward through a transactiona... | 00a, 00b, 01, 03, 04, 06, 14 |
| [0072](../adr/0072-ui-rendering-stack.md) | Render the human-facing UI as server-rendered Razor with no c... | 00a, 03, 04, 07, 12, 13 |
| [0073](../adr/0073-edge-posture-and-forwarded-headers.md) | Assume an L7 edge in front of the deployment, define the dire... | 00b, 01, 03, 06, 07, 08, 11, 12, 16 |
| [0074](../adr/0074-database-ha-and-cache-durability.md) | Adopt a primary-plus-standby PostgreSQL topology with automat... | 05, 06, 07, 08, 10, 11, 12, 13, 15, 16 |

## 3. What the shape of that table says

Two readings are worth stating, because both were computed rather than assumed.

**The genuinely cross-cutting decisions are not the ones a reader would guess.** By view count
they are ADR-0006 (14 views), ADR-0008 (13), ADR-0039 (13), ADR-0001 (12), ADR-0011 (12), and
then ADR-0012, ADR-0031, ADR-0037, and ADR-0040 at 11 each. Key material, the audit chain,
revocation freshness, and tenancy reach almost everywhere. Notably **ADR-0039 is far more
cross-cutting here than its title suggests**: "revocation propagation" reads as one subsystem,
but its per-path freshness model turns up in the context, container, data, runtime, security,
performance, reliability, schema, observability, and operations views, because almost every
other decision eventually has to say how fast a change of mind takes effect.

**One ADR is cited by no view at all: ADR-0045**, on coordinated vulnerability disclosure and
CVE issuance. That is a real signal and the honest reading is that it is **correct**: a
disclosure process is governance rather than architecture, so it has no structural or
operational view to land in. It is reachable through the governance row of
[07-cross-cutting](07-cross-cutting.md), which is where decisions that are substance without a
view belong. Recorded here explicitly so the zero is understood as a finding rather than an
oversight.

The other end of the distribution is equally unremarkable on inspection: the ADRs cited by a
single view are the deliberately narrow ones, the demand-driven extensions of
[18-v2-evolution](18-v2-evolution.md) named only where scope is set, plus licensing, public-API
versioning, local-development TLS, and the patterns vocabulary.

## 4. Decisions that are not yet closed

Several ADRs defer a number, a policy, or a human sign-off. They are **not** undecided
architecture: the mechanism is built and the parameter is a named owner's call. They are
consolidated as one release gate in the
[Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md) rather than duplicated
here, so there is one place to read before a release rather than two that can disagree.

Separately, **eight** load-bearing claims surfaced while writing and auditing this layer have
**no owning ADR**. Each is recorded in place in the view that carries it, marked as a candidate
rather than presented as settled. They are not listed above as decisions, because they are not
decisions yet. They are enumerated here rather than counted, because an earlier revision of
this page asserted the count as six and the count was wrong: two more were found later, while
writing the threat model, and a number nobody can check is exactly the kind of claim this
repository has learned not to write.

| # | Claim with no owning ADR | Recorded in |
|---|---|---|
| 1 | **Claim destinations are deny-by-default**, so a claim is emitted only where declared. The strongest candidate: it is the named control against claim leakage in a High-rated threat row | [04-components](04-components.md) section 1, [11-security-architecture](11-security-architecture.md) section 3, [20-threat-model](20-threat-model.md) row I4 |
| 2 | The **application-side strict-transport-security and TLS floor**, which ADR-0073 assumes at the edge rather than decides in the application | [11-security-architecture](11-security-architecture.md) section 7 |
| 3 | The **meter inventory**: which metrics exist at all | [15-observability-and-monitoring](15-observability-and-monitoring.md) |
| 4 | **Telemetry export is lossy, never blocking**, with the collector-outage proof that shows it | [15-observability-and-monitoring](15-observability-and-monitoring.md) |
| 5 | The **high-cardinality prohibition** and its enforcement: no tenant, subject, session, proof identifier, or address as a metric tag, with the exact-match view selector and the attachment test. It is a personal-data rule as much as a cardinality one | [15-observability-and-monitoring](15-observability-and-monitoring.md), [20-threat-model](20-threat-model.md) row I5 |
| 6 | The **periodic restore-verify probe**, which is what distinguishes a backup that exists from one that restores | [13-reliability-backup-and-dr](13-reliability-backup-and-dr.md) |
| 7 | A **crypto-path throughput gate in CI**, separate from the SLO gate ADR-0041 owns | [12-performance-and-scalability](12-performance-and-scalability.md) |
| 8 | The **exclusion of `may_act`**, the delegation claim: no ADR in this repository contains the term | [20-threat-model](20-threat-model.md) row E5 |

Items 3, 4, and 5 share an owner in the observability design and may resolve as one ADR rather
than three; items 6 and 7 are more likely amendments to ADR-0074 or ADR-0006 and to ADR-0041
than new decisions. That is a judgement to make when they are drafted, not here.

## 5. Decisions whose feature is not built

Three accepted ADRs describe features that are design-complete and deliberately not built in
v1, and six more are `proposed` demand-driven extensions. Both groups are
[18-v2-evolution](18-v2-evolution.md). A `proposed` ADR is not a stack entry and carries no
stack-of-record marker (ADR-0061).

## Sources

* [`docs/adr/README.md`](../adr/README.md) is the canonical decision list; this file adds only
  the reverse mapping and is generated from the views themselves.
* ADR-0000 (the record format and the rule that an accepted decision binds until superseded),
  ADR-0061 (the stack of record and why a proposed ADR carries no marker), ADR-0045 (the
  governance decision that correctly lands in no view).
* The measured cross-cutting set in section 3 is an observation about this layer rather than a
  claim from any one decision, and the decisions it names are ADR-0006 (key material and
  recovery), ADR-0008 (the audit chain), ADR-0039 (per-path revocation freshness), ADR-0001
  (tenancy), ADR-0011 (key rotation), ADR-0012 (key bootstrap and restore), ADR-0031
  (the operational baseline), ADR-0037 (the engine), and ADR-0040 (resiliency and overload).
* Reconciled against the design corpus's decisions index on 2026-07-26. **Its structure was
  deliberately not adopted.** That index restates each decision in a one-line table while its
  own preamble says the architecture "never restates a decision" and names the ADR list as
  canonical, so it duplicates the canonical list and can drift from it. This file keeps the
  reverse mapping, which is the part that is genuinely absent elsewhere, and quotes ADR titles
  rather than paraphrasing them. Its cross-cutting section was also **recomputed rather than
  copied**: the corpus names four themes by hand, and against this layer the measured answer
  differs, most visibly in ADR-0039 being one of the three most cross-cutting decisions here
  while the corpus list omits it and includes an architecture-style decision that is not in
  the measured top group.

---

[Prev: Operations and maintenance](16-operations-and-maintenance.md) · [Index](README.md) · Next: [v2 evolution](18-v2-evolution.md)
