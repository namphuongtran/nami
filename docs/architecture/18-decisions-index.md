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
| [0000](../adr/0000-use-markdown-architectural-decision-records.md) | Use Markdown Architectural Decision Records (MADR) with the f... | 11, 18 |
| [0001](../adr/0001-multi-tenant-isolation-model.md) | Tiered multi-tenant isolation: global identity, pooled tenant... | 01, 03, 04, 05, 06, 07, 08, 09, 11, 12, 13, 14, 15, 18, 20, 23, 24 |
| [0002](../adr/0002-federation-external-idp-integration.md) | Integrate external identity providers through ASP.NET Core Id... | 01, 04, 08, 14, 19 |
| [0003](../adr/0003-server-side-sessions-are-core.md) | Server-side session store is a core feature, not an option | 01, 03, 06, 08, 09, 12, 13, 20, 21, 23 |
| [0004](../adr/0004-refresh-token-posture.md) | Keep OpenIddict's native refresh-token mechanics rather than... | 01, 03, 07, 09, 13, 16, 17, 20, 21, 23 |
| [0005](../adr/0005-encryption-credential-lifecycle.md) | Track the encryption credential's lifecycle separately from t... | 03, 05, 07, 08, 09, 11, 13, 14, 21, 24 |
| [0006](../adr/0006-disaster-recovery-key-material.md) | Make key-material storage and disaster recovery provider-agno... | 01, 03, 04, 07, 08, 10, 11, 12, 13, 15, 17, 18, 20, 21, 22, 23 |
| [0007](../adr/0007-key-compromise-break-glass-runbook.md) | Eject a compromised key from the JWKS within five minutes wit... | 01, 03, 04, 09, 10, 13, 16, 17, 24 |
| [0008](../adr/0008-audit-subsystem.md) | Make the audit subsystem first-class, tamper-evident, and del... | 01, 02, 03, 04, 05, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16, 18, 22, 24 |
| [0009](../adr/0009-secret-store-access-and-rollover.md) | Access the secret store with least-privilege workload identit... | 01, 03, 04, 07, 08, 10, 13, 14 |
| [0010](../adr/0010-tenant-hierarchy-delegated-admin.md) | Administer child tenants through explicit, scoped delegated-a... | 01, 04, 06, 08, 09, 12, 13, 14, 17, 24 |
| [0011](../adr/0011-no-restart-key-rotation.md) | Rotate signing and encryption keys without restarting, via th... | 01, 03, 05, 07, 08, 09, 10, 11, 13, 14, 17, 18, 20, 21, 22, 24 |
| [0012](../adr/0012-key-bootstrap-and-dr-sequence.md) | Bootstrap keys by auto-seeding at cold start, root the keyrin... | 01, 03, 05, 08, 09, 10, 11, 12, 13, 16, 17, 18, 22, 24 |
| [0013](../adr/0013-mfa-assurance-and-step-up.md) | Make MFA the producer of acr/amr/auth_time and enforce step-u... | 01, 08, 09, 14, 24 |
| [0014](../adr/0014-advanced-protocol-scope.md) | Build both mTLS and DPoP sender-constrained tokens, and delib... | 01, 05, 07, 09, 10, 13, 14, 17, 18, 19, 21, 22, 23, 24 |
| [0015](../adr/0015-admin-break-glass-and-first-admin-bootstrap.md) | Provide an OIDC-independent admin break-glass path and a one-... | 01, 04, 10, 13, 14, 17, 24 |
| [0016](../adr/0016-right-to-erasure.md) | Reconcile GDPR right-to-erasure with the immutable audit chai... | 01, 02, 03, 05, 09, 11, 12, 13, 15, 16, 18, 20, 22, 23, 24 |
| [0017](../adr/0017-tenant-provisioning-and-silo-migration.md) | Orchestrate the tenant lifecycle with build-artifact migratio... | 01, 09, 10, 12, 15, 17, 20, 24 |
| [0018](../adr/0018-dbcontext-pooling-for-pool-mode.md) | Register the Pool-mode OpenIddict DbContext non-pooled in v1,... | 03, 07, 08, 09, 10, 11, 12, 21, 22, 23 |
| [0019](../adr/0019-single-logout-strategy.md) | Achieve single logout with an interim back-channel logout on... | 01, 03, 04, 07, 08, 09, 12, 17, 19, 23, 24 |
| [0020](../adr/0020-admin-architecture.md) | Split admin into a REST API and an MVC Razor BFF app, enforce... | 01, 05, 06, 07, 08, 09, 12, 13, 14, 17, 24 |
| [0021](../adr/0021-openiddict-version-adaptation.md) | Adapt to OpenIddict version upgrades with seam isolation, per... | 02, 03, 05, 08, 09, 11, 15, 16, 17, 20, 23, 24 |
| [0022](../adr/0022-logging-and-observability-stack.md) | Use native ILogger plus OpenTelemetry (OTLP) for logging and... | 01, 03, 04, 07, 08, 10, 11, 14, 16, 23, 24 |
| [0023](../adr/0023-iac-tool-opentofu.md) | Use OpenTofu as the default infrastructure-as-code tool inste... | 03, 10 |
| [0024](../adr/0024-architecture-style.md) | Adopt a hexagonal shell (dependency rule plus ports/adapters)... | 03, 05, 06, 07, 08, 10, 12 |
| [0025](../adr/0025-local-development-and-first-run.md) | Run locally with docker-compose dependencies, multi-stage Doc... | 03, 07, 10, 15, 17 |
| [0026](../adr/0026-dependency-license-policy.md) | Restrict dependencies to permissive OSS licenses, enforced by... | 03, 05, 11, 13, 16 |
| [0027](../adr/0027-packaging-and-distribution.md) | Distribute Nami as a hybrid NuGet meta-package plus a referen... | 01, 03, 07, 08, 10, 17, 18 |
| [0028](../adr/0028-user-management.md) | Build user management on ASP.NET Core Identity with native pa... | 01, 04, 07, 08, 12, 17 |
| [0029](../adr/0029-bff.md) | Build a Nami.Identity.Bff package by composing OSS-permissive... | 01, 03, 07, 08, 09, 13, 24 |
| [0030](../adr/0030-dotnet-version-upgrade.md) | Upgrade .NET on an LTS-to-LTS cadence, with multi-target pack... | 03, 11, 17 |
| [0031](../adr/0031-twelve-factor-baseline.md) | Adopt the 12-factor (and 15-factor) methodology as the operat... | 01, 03, 05, 07, 08, 09, 10, 14, 16, 17, 18, 20, 21, 22, 23 |
| [0032](../adr/0032-usage-visibility-and-licensing-posture.md) | Gain usage visibility through free registration and opt-in te... | 10, 16 |
| [0033](../adr/0033-key-scope-isolation-model.md) | Align key-scope isolation to the tenant tier with one keyset... | 01, 02, 03, 04, 05, 06, 09, 11, 12, 13, 14, 23, 24 |
| [0034](../adr/0034-dynamic-external-idp.md) | Open dynamic per-tenant external IdP federation as a v2 self-... | 01, 03, 04, 05, 08, 14, 19 |
| [0035](../adr/0035-self-service-client-registration.md) | Offer self-service client registration through the authentica... | 01, 03, 05, 17, 19, 23 |
| [0036](../adr/0036-database-key-strategy-uuidv7.md) | Use UUIDv7 as the clustered primary key for every entity, wit... | 03, 07, 08, 09, 12, 19, 21 |
| [0037](../adr/0037-database-engine-postgresql.md) | Use PostgreSQL as the sole database engine | 01, 03, 05, 07, 08, 09, 10, 11, 12, 13, 14, 15, 18, 21, 24 |
| [0038](../adr/0038-email-notification-subsystem.md) | Build email delivery as a first-class, cloud-agnostic subsyst... | 01, 04, 07, 08, 09, 11, 12, 13 |
| [0039](../adr/0039-revocation-propagation-and-cache-coherence.md) | Achieve cross-node revocation freshness per-path, with no bac... | 01, 03, 05, 07, 09, 10, 11, 12, 13, 15, 17, 18, 19, 20, 21, 22, 24 |
| [0040](../adr/0040-resiliency-and-overload-protection.md) | Standardize a resiliency and overload-protection posture: one... | 01, 03, 04, 07, 10, 11, 13, 14, 16, 17, 18, 21, 22, 23, 24 |
| [0041](../adr/0041-nfr-targets-and-slo-release-gate.md) | Adopt self-load-tested NFR targets and make the SLO a formal... | 01, 03, 05, 10, 11, 16, 17, 18, 20, 21, 22, 23, 24 |
| [0042](../adr/0042-abuse-and-bot-defense.md) | Add a layered anti-automation and abuse-defense posture beyon... | 03, 04, 10, 11, 13, 14, 16, 17 |
| [0043](../adr/0043-security-hardening-invariants-startup-check.md) | Enforce security hardening invariants with a fail-fast startu... | 03, 08, 11, 13, 14 |
| [0044](../adr/0044-public-api-stability-and-semver.md) | Treat the public API as a versioned seam governed by an analy... | 02, 03, 16, 18, 23 |
| [0045](../adr/0045-security-disclosure-and-cve-policy.md) | Handle security vulnerabilities through private coordinated d... | 11, 18 |
| [0046](../adr/0046-governance-and-contribution-model.md) | Adopt an ADR-driven, DCO-based OSS governance and contributio... | 10, 11 |
| [0047](../adr/0047-authorization-decision-engine.md) | Compute authorization with a DB-first engine behind a consist... | 08, 09, 14 |
| [0048](../adr/0048-introspection-revocation-endpoint-isolation.md) | Isolate the introspection and revocation endpoints with clien... | 03, 04, 07, 09, 11, 13, 14, 21, 24 |
| [0049](../adr/0049-resource-server-per-tenant-validation.md) | Isolate tenants at the resource server by issuer and tenant b... | 01, 02, 03, 04, 05, 07, 08, 09, 11, 13, 14, 18, 20, 23, 24 |
| [0050](../adr/0050-per-client-cors-policy.md) | Provide per-client CORS through a custom policy provider, not... | 11, 13 |
| [0051](../adr/0051-release-supply-chain-integrity.md) | Sign and attest release artifacts with keyless provenance for... | 10, 11, 13, 17 |
| [0052](../adr/0052-ergonomic-config-layer.md) | Build an ergonomic, fail-closed configuration layer for decla... | 01, 10, 17, 18 |
| [0053](../adr/0053-data-subject-rights-suite.md) | Build the data-subject-rights suite (access, portability, rec... | 01, 03, 09, 11, 13, 16, 20 |
| [0054](../adr/0054-cross-border-transfer-and-data-residency.md) | Make data residency and cross-border personal-data transfer f... | 01, 03, 09, 11, 12, 13, 15, 20 |
| [0055](../adr/0055-saml-ws-federation-support.md) | Support SAML 2.0 and WS-Federation through a demand-driven fe... | 01, 19 |
| [0056](../adr/0056-fapi-2-conformance.md) | Support FAPI 2.0 high-assurance profiles through a demand-dri... | 01, 19 |
| [0057](../adr/0057-windows-negotiate-authentication.md) | Support Windows integrated authentication (Negotiate/Kerberos... | 01, 19 |
| [0058](../adr/0058-guiding-architectural-principles.md) | Adopt Separation of Concerns and pragmatic SOLID as binding a... | 03, 05, 06 |
| [0059](../adr/0059-value-objects-and-aggregate-boundaries.md) | Model value objects as complex types and gate aggregates on a... | 05, 06 |
| [0060](../adr/0060-testing-strategy.md) | Consolidate the testing strategy and adopt behavior-first tes... | 07, 22, 23 |
| [0061](../adr/0061-technology-stack-of-record.md) | Record the committed technology stack and its cross-cutting s... | 01, 03, 07, 18, 19, 24 |
| [0062](../adr/0062-owasp-asvs-security-baseline.md) | Adopt OWASP ASVS as the security-verification baseline | 02, 03, 11, 13, 20 |
| [0063](../adr/0063-observability-backend-and-dev-visualization.md) | Keep the observability backend operator-chosen and run a self... | 11, 16 |
| [0064](../adr/0064-mcp-authorization-server-support.md) | Support Nami as the OAuth authorization server for MCP servers | 01, 19 |
| [0065](../adr/0065-coding-and-naming-conventions.md) | Adopt the Microsoft naming and C# coding conventions as an en... | 07, 09, 10, 12, 16, 18, 23, 24 |
| [0066](../adr/0066-design-patterns-vocabulary-and-pragmatic-use.md) | Adopt design patterns as a shared vocabulary applied pragmati... | 03 |
| [0067](../adr/0067-ai-assisted-development-governance.md) | Adopt an AI-assisted development policy: human-accountable, d... | 11, 17 |
| [0068](../adr/0068-continuous-access-evaluation-shared-signals.md) | Support continuous access evaluation via the OpenID Shared Si... | 01, 19 |
| [0069](../adr/0069-verifiable-credentials-openid4vc.md) | Support issuing Verifiable Credentials via OpenID4VC (Nami as... | 01, 19 |
| [0070](../adr/0070-local-development-tls.md) | Serve HTTPS in local development with a locally-trusted cert... | 10 |
| [0071](../adr/0071-identity-change-event-publishing.md) | Publish identity change events outward through a transactiona... | 01, 03, 04, 05, 07, 08, 09, 14, 15, 19, 24 |
| [0072](../adr/0072-ui-rendering-stack.md) | Render the human-facing UI as server-rendered Razor with no c... | 01, 07, 08, 11, 21, 22 |
| [0073](../adr/0073-edge-posture-and-forwarded-headers.md) | Assume an L7 edge in front of the deployment, define the dire... | 03, 04, 07, 09, 10, 11, 13, 17, 18, 21 |
| [0074](../adr/0074-database-ha-and-cache-durability.md) | Adopt a primary-plus-standby PostgreSQL topology with automat... | 05, 09, 10, 11, 12, 13, 14, 16, 17, 20, 21, 22 |
| [0075](../adr/0075-security-sensitive-port-invariants.md) | Treat security-sensitive ports as carrying non-weakenable inv... | 08, 09, 13, 14, 18, 23, 24 |
| [0076](../adr/0076-application-transport-security.md) | Decide the application's own transport security: HSTS policy, the... | 03, 13, 18, 23 |
| [0077](../adr/0077-metric-cardinality-and-telemetry-privacy.md) | Bound metric cardinality with an allow-listed tag set, and keep... | 14, 16, 18, 23 |
| [0078](../adr/0078-load-test-tooling.md) | Adopt Apache JMeter as the load-test tool, replacing k6 and N... | 03 |
| [0079](../adr/0079-admin-api-http-conventions.md) | Decide the Admin API's HTTP surface by rule rather than per e... | 18 |
| [0080](../adr/0080-health-and-readiness-probe-contract.md) | Serve two anonymous probe routes, `/health/live` and `/health... | 09, 10, 18, 24 |
| [0081](../adr/0081-dual-control-target-guard-taxonomy.md) | Classify a dual-control proposal's target, so the guard check... | 09, 12 |
| [0082](../adr/0082-abuse-detection-lanes-and-grouping-keys.md) | Give every abuse rule a lane that can answer it, and add the... | 13, 14 |
| [0083](../adr/0083-abuse-detection-is-built-in.md) | Ship abuse detection as a built-in component rather than a SI... | 13, 14 |
| [0084](../adr/0084-membership-removal-semantics.md) | Define what removing a person from a tenant guarantees, befor... | 06, 18 |
| [0085](../adr/0085-telemetry-instrument-naming.md) | Namespace every custom instrument `nami.identity.` and freeze... | 16 |
| [0086](../adr/0086-pin-ci-actions-by-commit-sha.md) | Pin every CI action by commit SHA, never by tag | 11 |
| [0087](../adr/0087-http-surface-snapshot-gate.md) | Lock the HTTP surface with a committed snapshot of the genera... | 18 |

## 3. What the shape of that table says

Two readings are worth stating, because both were computed rather than assumed.

**These figures exclude this page.** Section 2's table counts any mention, including the ones in
this analysis, so a decision named here would gain a view by being described as narrow. The
numbers below are therefore computed over the **other 23 views**, which is also the more honest
measure: it counts views that depend on a decision rather than views that mention it. Recompute
them with the same exclusion whenever section 2 is regenerated, and never regenerate one without
the other.

**The genuinely cross-cutting decisions are not the ones a reader would guess.** ADR-0008 leads
at 17 views, then ADR-0001 and ADR-0039 at 16, ADR-0006 and ADR-0011 at 15, and five at 14:
ADR-0016, ADR-0031, ADR-0037, ADR-0040, and ADR-0049. The audit chain, tenancy, revocation
freshness, key material and its recovery, the erasure mechanism, and the failure-posture
classification reach almost everywhere. Naming them here is safe under the exclusion above,
which is the reason for it. Notably **ADR-0039 is far more cross-cutting than its
title suggests**: "revocation propagation" reads as one subsystem, but its per-path freshness
model turns up in sixteen views, including context, data, runtime, security, performance,
reliability, schema, observability, and operations, because almost every other decision
eventually has to say how fast a change of mind takes effect.

**Three decisions are cited by no view, and all three zeros are correct. Two more were, and
were defects.** All five were judged by one test, the question this page exists to answer: if
the decision changed, would any view become wrong?

**ADR-0045** (coordinated vulnerability disclosure) and **ADR-0079** (the Admin API's HTTP
conventions) keep their zeros. A disclosure process is governance, not architecture, so it has
no structural or operational view to land in. ADR-0079's case is different and is **measured
rather than argued**: the architecture layer carries no HTTP verb and no admin path anywhere,
so a rule about tenant prefixes and about whether revoking is a `DELETE` invalidates nothing
here. Its sibling ADR-0044 is cited in four views because it is a product-shape driver; a
convention for one contract sits a layer below that. ADR-0045 is reachable through the
governance row of [11-cross-cutting-concepts](11-cross-cutting-concepts.md), which is where
decisions that are substance without a view belong; **ADR-0079 is not, and deliberately so**,
since an HTTP convention is not governance and filing it there to give it a home would be the
wrong-owner attribution this layer keeps having to correct. Its home is the contract itself,
and this row is the pointer.

**ADR-0087** (the HTTP-surface snapshot gate) joins ADR-0079 for the same measured reason and
was the harder call of the three. Reversing it would not make any view false, because no view
says the HTTP surface is locked; it says nothing about the HTTP surface at all. One candidate
was considered and rejected: concern **C10** in
[02-stakeholders-and-concerns](02-stakeholders-and-concerns.md), "protocol conformance and a
stable consumer contract", which points at ADR-0021 and ADR-0044. ADR-0087 does serve that
concern, and adding it there would have been defensible on its own. It was declined because
ADR-0079 serves C10 by the same argument and is not listed either, so adding one and not the
other would have made this page inconsistent with the verdict directly above it. If C10 is
ever expanded to name the HTTP contract, **both** belong in it, and the two rows move
together.

**ADR-0080** (the probe contract) and **ADR-0084** (what removing a person from a tenant
guarantees) were **not** correct zeros, and the ADR-0080 case is the instructive one. Two views
already stated parts of the probe contract without an owner, which is only untidy. The real
defect was the opposite shape: the runtime view's admin invariant and the glossary entry both
stated `RequireActor` as universal, while ADR-0080 creates exactly one exemption from it, the
anonymous probe routes. So the layer was not merely silent, it was teaching the reading that
makes a pod never reach Ready, which is the failure that ADR exists to prevent. ADR-0084 was a
plain coverage gap: the domain model defined Membership and never said that ending one leaves
the delegated-admin grant intact, which is a statement about aggregate independence and belongs
in the domain model rather than in an endpoint description.

Five sit at a single view. Three are genuinely narrow rather than under-covered: the record
format itself, the design-patterns vocabulary, and local-development TLS. The two that joined
them, the load-test tool and the telemetry instrument namespace, are narrow for the same
reason and were checked with the same test. Sixteen sit at two, and that tail is unremarkable: it is mostly the six demand-driven extensions, named where scope is set
([01-introduction-scope](01-introduction-scope.md)) and where their triggers are recorded
([19-evolution-and-extensions](19-evolution-and-extensions.md)), which is exactly the footprint
a recorded-but-uncommitted option should have.

## 4. Decisions that are not yet closed

Several ADRs defer a number, a policy, or a human sign-off. They are **not** undecided
architecture: the mechanism is built and the parameter is a named owner's call. They are
consolidated as one release gate in the
[Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md) rather than duplicated
here, so there is one place to read before a release rather than two that can disagree.

Separately, load-bearing claims surfaced while writing and auditing this layer that had **no
owning ADR**. Eight were found. Each was recorded in place in the view that carries it, marked
as a candidate rather than presented as settled, and they are being resolved one at a time.
They are enumerated rather than counted, because an earlier revision of this page asserted the
count as six and was wrong: two more were found later while writing the threat model, and a
number nobody can check is exactly the kind of claim this repository has learned not to write.

**None remain open**, and the tally is the finding. Eight claims produced **three** new ADRs.
Four were resolved by editing an existing decision directly, since no code exists yet and a
clean decision beats an amendment note. One was not a gap at all.

| Claim | Resolution |
|---|---|
| Deny-by-default claim destinations | **ADR-0075** (new), reframed as a port-invariant question |
| Application-side HSTS and TLS floor | **ADR-0076** (new) |
| Metric tag and cardinality rule | **ADR-0077** (new), reframed as a data-protection question |
| Lossy-not-blocking telemetry export | **ADR-0040** parameter E, a classification gap |
| Periodic restore-verify probe | **ADR-0006**, a new control between its monitoring and its drill |
| Crypto-path throughput CI gate | **ADR-0041**, considered and **rejected**, with the reasoning recorded |
| Exclusion of `may_act` | **ADR-0014**, added to its de-scope list |
| Meter inventory | **Not ownerless.** ADR-0044 section G and ADR-0065 already owned it |

**How each closed is the useful part.**

**ADR-0075**, the deny-by-default claim-destination rule, 2026-07-26. Investigating it showed
the rule itself needed no decision: it is OpenIddict's documented behaviour, and ADR-0052
already recorded that posture. What needed one was that the port enforcing it,
`IClaimsProfileService`, is **replaceable**, so the engine's safe default protects the shipped
adapter and not a consumer's. Three other ports turned out to have the same shape, which is why
one ADR covers four rows instead of four ADRs covering one each.

**ADR-0076**, the application-side transport security, 2026-07-26. This one was already named as
an omission inside ADR-0073, which recorded the gap rather than adopting settings silently, and
two of that ADR's own parameters depended on it. The obvious objection, that the framework
template already emits `UseHsts`, turned out to hold on only one of ADR-0027's two distribution
paths and to supply a general-purpose default rather than a policy on that one. It also picked
up a second gap found in passing: OpenIddict's `DisableTransportSecurityRequirement` was
forbidden only in a deployment design note and by no decision.

**ADR-0077 and ADR-0040 parameter E**, the three observability claims, 2026-07-26. Taken as a
cluster because they shared a home in the observability design, they turned out to be three
different things. The **meter inventory** was not ownerless at all: the names are a stable
contract under ADR-0044 section G and follow ADR-0065's scheme, leaving only a catalogue that
is not a decision. The **lossy-not-blocking export** was a **classification** gap: ADR-0040's
policy named caches and security checks, the telemetry path is neither, so it sat in no
category; ADR-0040 was edited directly to add it as a third posture. Only the **tag and
cardinality rule** was a decision, and it was mislabelled: filed as a capacity concern, it is
at least as much a data-protection one, because a metric backend is outside the reach of the
audit retention, crypto-shred, and erasure this project built.

**The last three, 2026-07-26.** The **restore-verify probe** turned out to name a sharper gap
than "add a probe": ADR-0006 already had continuous monitoring, which catches a backup that
stopped, and a quarterly drill, which catches one that cannot be restored, and a backup that
runs on schedule while producing an unrestorable artifact falls between them for up to a
quarter. The **crypto-path CI gate** was **rejected**, and the rejection is more informative
than an adoption would have been: the corpus rationale was contradicted by this repository's own
capacity finding, a different argument for the gate survived that correction, and it was still
rejected on flaky-micro-benchmark grounds with the surviving argument recorded. The **`may_act`
exclusion** was a real decision that had never been written down, and it belongs with the other
de-scopes in ADR-0014 with one distinction noted: it is a security decision, so demand would not
reopen it.

**The lesson, stated for whoever finds the next claim like these: ask what decision is actually
missing before assuming it is the claim as written.** Across eight, one needed no decision
because the engine already had the behaviour, one needed one the framework default could not
supply, one already had an owner nobody had checked for, one needed a classification rather than
a decision, one needed a rejection rather than an adoption, and one was mislabelled as a
capacity concern when it was a privacy one. **Eight claims, three new ADRs.** Writing eight
would have been the wrong answer, and so would writing none.

## 5. Decisions whose feature is not built

Three accepted ADRs describe features that are design-complete and deliberately not built in
v1, and six more are `proposed` demand-driven extensions. Both groups are
[19-evolution-and-extensions](19-evolution-and-extensions.md). A `proposed` ADR is not a stack entry and carries no
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

[Prev: Operations and maintenance](17-operations-maintenance.md) · [Index](README.md) · Next: [Evolution and extensions](19-evolution-and-extensions.md)
