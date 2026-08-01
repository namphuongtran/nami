# Architecture Decision Records

Nami's architecture was designed decision-first: every significant choice is recorded as an ADR with its context, the options considered, and the rationale. Accepted ADRs are binding until superseded.

Format: [MADR 4.0.0](https://adr.github.io/madr/), full template (see [ADR-0000](0000-use-markdown-architectural-decision-records.md)). Files are named `NNNN-short-title-with-dashes.md`. ADRs `0001`-`0035` are being imported and translated from the original design corpus, keeping their original numbering one-to-one; new decisions continue from `0036`.

Several ADRs defer a policy, threshold, or human sign-off before general availability; those are consolidated as one release gate in the [Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

## Identifiers borrowed from the design corpus

Imported ADRs cite the corpus they came from, and a few of those citations are **numbers
that belong to that corpus and resolve to nothing here**. In a "More Information" or
"Confirmation" section, treat these as external provenance, never as a pointer into this
repository:

| Shape | Example | What it is |
|---|---|---|
| `doc NN §x` | "the productization design (doc 28 §9.1)" | a corpus root document. **The digits do not transfer**: the same number names a different document in each repository, so the topic named alongside it is the part that identifies it |
| `task N.NN` | "tasks 3.16a-d" | a corpus build task |
| `A-n`, `Vnn`, `Rnn`, `Tn` | "A-2/V19 with tests T3c and T3d" | a corpus spike, verification record, research record, or a test **inside** a spike harness |

**Test identifiers of the shape `NN.Txx`, `NN.Kxx` or `9.Nx` were removed on 2026-08-01 and
must not be reintroduced.** They read as pointers into a numbered test register that this
repository does not have. A test obligation is instead stated by **what it asserts** and
listed in the [testing design](../design/20-testing.md) against its owning document, which
is the same convention the rest of that document already uses.

The corpus numbers these labels by **owning document**, not in one series, so the `8.K`
family (its key management) and the `25.T` family (its admin API) exist alongside `9.T` and
`9.K`. Its own ADR-0011 and ADR-0012 each cite a `8.K` group and a `9.K` group in one
breath, on a single line, and the import that produced this repository took the `9.K` half
and left the other. That is how the other half arrives next time, which is why guardrail
Check 6 screens the whole `NN.T` / `NN.K` shape rather than a single family.

## Index

| ADR | Title | Status |
|---|---|---|
| [0000](0000-use-markdown-architectural-decision-records.md) | Use Markdown Architectural Decision Records (MADR) with the full template | accepted |
| [0001](0001-multi-tenant-isolation-model.md) | Tiered multi-tenant isolation: global identity, pooled tenant data by default, silo on demand | accepted |
| [0002](0002-federation-external-idp-integration.md) | Integrate external identity providers through ASP.NET Core Identity external login | accepted |
| [0003](0003-server-side-sessions-are-core.md) | Server-side session store is a core feature, not an option | accepted |
| [0004](0004-refresh-token-posture.md) | Keep OpenIddict's native refresh-token mechanics rather than rebuilding them | accepted |
| [0005](0005-encryption-credential-lifecycle.md) | Track the encryption credential's lifecycle separately from the signing credential | accepted |
| [0006](0006-disaster-recovery-key-material.md) | Make key-material storage and disaster recovery provider-agnostic | accepted |
| [0007](0007-key-compromise-break-glass-runbook.md) | Eject a compromised key from the JWKS within five minutes with a break-glass runbook | accepted |
| [0008](0008-audit-subsystem.md) | Make the audit subsystem first-class, tamper-evident, and delivery-guaranteed | accepted |
| [0009](0009-secret-store-access-and-rollover.md) | Access the secret store with least-privilege workload identity and rotate client credentials via private_key_jwt | accepted |
| [0010](0010-tenant-hierarchy-delegated-admin.md) | Administer child tenants through explicit, scoped delegated-admin grants, not inherited seniority | accepted |
| [0011](0011-no-restart-key-rotation.md) | Rotate signing and encryption keys without restarting, via the OpenIddict options change-token seam | accepted |
| [0012](0012-key-bootstrap-and-dr-sequence.md) | Bootstrap keys by auto-seeding at cold start, root the keyring in an X.509 certificate, and restore both key stores together | accepted |
| [0013](0013-mfa-assurance-and-step-up.md) | Make MFA the producer of acr/amr/auth_time and enforce step-up assurance | accepted |
| [0014](0014-advanced-protocol-scope.md) | Build both mTLS and DPoP sender-constrained tokens, and deliberately de-scope FAPI-specific protocols | accepted |
| [0015](0015-admin-break-glass-and-first-admin-bootstrap.md) | Provide an OIDC-independent admin break-glass path and a one-time first-admin bootstrap | accepted |
| [0016](0016-right-to-erasure.md) | Reconcile GDPR right-to-erasure with the immutable audit chain using chain-over-commitments | accepted |
| [0017](0017-tenant-provisioning-and-silo-migration.md) | Orchestrate the tenant lifecycle with build-artifact migrations, per-tenant version gating, and expand/contract | accepted |
| [0018](0018-dbcontext-pooling-for-pool-mode.md) | Register the Pool-mode OpenIddict DbContext non-pooled in v1, with pooled-plus-mutable deferred | accepted |
| [0019](0019-single-logout-strategy.md) | Achieve single logout with an interim back-channel logout on the session store, and drop front-channel | accepted |
| [0020](0020-admin-architecture.md) | Split admin into a REST API and an MVC Razor BFF app, enforce dual-control server-side, and reject app-only tokens | accepted |
| [0021](0021-openiddict-version-adaptation.md) | Adapt to OpenIddict version upgrades with seam isolation, per-bump contract-regression tests, and a migration playbook | accepted |
| [0022](0022-logging-and-observability-stack.md) | Use native ILogger plus OpenTelemetry (OTLP) for logging and observability, and drop Serilog | accepted |
| [0023](0023-iac-tool-opentofu.md) | Use OpenTofu as the default infrastructure-as-code tool instead of Terraform | accepted |
| [0024](0024-architecture-style.md) | Adopt a hexagonal shell (dependency rule plus ports/adapters) with vertical slices inside, for both IdP-core and Admin | accepted |
| [0025](0025-local-development-and-first-run.md) | Run locally with docker-compose dependencies, multi-stage Dockerfiles, Testcontainers integration tests, and a defined first-run order | accepted |
| [0026](0026-dependency-license-policy.md) | Restrict dependencies to permissive OSS licenses, enforced by a CI license-scan gate | accepted |
| [0027](0027-packaging-and-distribution.md) | Distribute Nami as a hybrid NuGet meta-package plus a reference host image and template, released under Apache-2.0 | accepted |
| [0028](0028-user-management.md) | Build user management on ASP.NET Core Identity with native passkeys and a lifecycle layer, packaged as Nami.Identity.Users | accepted |
| [0029](0029-bff.md) | Build a Nami.Identity.Bff package by composing OSS-permissive pieces rather than adopting a commercial BFF | accepted |
| [0030](0030-dotnet-version-upgrade.md) | Upgrade .NET on an LTS-to-LTS cadence, with multi-target packages and per-bump contract-regression | accepted |
| [0031](0031-twelve-factor-baseline.md) | Adopt the 12-factor (and 15-factor) methodology as the operational baseline, closing four soft spots as enforced invariants | accepted |
| [0032](0032-usage-visibility-and-licensing-posture.md) | Gain usage visibility through free registration and opt-in telemetry, with an open-core-ready seam, keeping the core Apache-2.0 | accepted |
| [0033](0033-key-scope-isolation-model.md) | Align key-scope isolation to the tenant tier with one keyset per deployment and a scope-aware key store | accepted |
| [0034](0034-dynamic-external-idp.md) | Open dynamic per-tenant external IdP federation as a v2 self-service, OIDC-only feature via a dynamic scheme provider | accepted |
| [0035](0035-self-service-client-registration.md) | Offer self-service client registration through the authenticated Admin API (DCR-inspired), not the standard DCR endpoint | accepted |
| [0036](0036-database-key-strategy-uuidv7.md) | Use UUIDv7 as the clustered primary key for every entity, with one deliberate bigint exception | accepted |
| [0037](0037-database-engine-postgresql.md) | Use PostgreSQL as the sole database engine | accepted |
| [0038](0038-email-notification-subsystem.md) | Build email delivery as a first-class, cloud-agnostic subsystem with a transactional outbox | accepted |
| [0039](0039-revocation-propagation-and-cache-coherence.md) | Achieve cross-node revocation freshness per-path, with no backplane for the per-request entity cache | accepted |
| [0040](0040-resiliency-and-overload-protection.md) | Standardize a resiliency and overload-protection posture (one outbound handler; rate-limiting vs load-shedding; Redis as accelerator) | accepted |
| [0041](0041-nfr-targets-and-slo-release-gate.md) | Adopt self-load-tested NFR targets and make the SLO a formal release gate, with burn-rate alerting and an external synthetic canary | accepted |
| [0042](0042-abuse-and-bot-defense.md) | Add a layered anti-automation and abuse-defense posture beyond IP rate-limiting and account lockout | accepted |
| [0043](0043-security-hardening-invariants-startup-check.md) | Enforce security hardening invariants with a fail-fast startup self-check | accepted |
| [0044](0044-public-api-stability-and-semver.md) | Treat the public API as a versioned seam governed by an analyzer-gated SemVer and deprecation policy | accepted |
| [0045](0045-security-disclosure-and-cve-policy.md) | Handle security vulnerabilities through private coordinated disclosure with CVE issuance | accepted |
| [0046](0046-governance-and-contribution-model.md) | Adopt an ADR-driven, DCO-based OSS governance and contribution model with dual-control releases | accepted |
| [0047](0047-authorization-decision-engine.md) | Compute authorization with a DB-first engine behind a consistency-carrying ICheckAccess port, swappable to ReBAC | accepted |
| [0048](0048-introspection-revocation-endpoint-isolation.md) | Isolate the introspection and revocation endpoints with client authentication and native audience confinement | accepted |
| [0049](0049-resource-server-per-tenant-validation.md) | Isolate tenants at the resource server by issuer and tenant binding, because a shared keyset means the signature is not an isolation boundary | accepted |
| [0050](0050-per-client-cors-policy.md) | Provide per-client CORS through a custom policy provider, not static global CORS | accepted |
| [0051](0051-release-supply-chain-integrity.md) | Sign and attest release artifacts with keyless provenance for a verifiable supply chain | accepted |
| [0052](0052-ergonomic-config-layer.md) | Build an ergonomic, fail-closed configuration layer for declaring clients and scopes | accepted |
| [0053](0053-data-subject-rights-suite.md) | Build the data-subject-rights suite, consent receipts, and breach hooks as reusable mechanisms | accepted |
| [0054](0054-cross-border-transfer-and-data-residency.md) | Make data residency and cross-border personal-data transfer first-class, jurisdiction-profiled controls | accepted |
| [0055](0055-saml-ws-federation-support.md) | Support SAML 2.0 and WS-Federation through a demand-driven federation extension | proposed |
| [0056](0056-fapi-2-conformance.md) | Support FAPI 2.0 high-assurance profiles through a demand-driven extension | proposed |
| [0057](0057-windows-negotiate-authentication.md) | Support Windows integrated authentication (Negotiate/Kerberos) through a demand-driven extension | proposed |
| [0058](0058-guiding-architectural-principles.md) | Adopt Separation of Concerns and pragmatic SOLID as binding architectural principles | accepted |
| [0059](0059-value-objects-and-aggregate-boundaries.md) | Model value objects as complex types and gate aggregates on a transactional invariant | accepted |
| [0060](0060-testing-strategy.md) | Consolidate the testing strategy and adopt behavior-first tests as living documentation | accepted |
| [0061](0061-technology-stack-of-record.md) | Record the committed technology stack and its cross-cutting selection rules | accepted |
| [0062](0062-owasp-asvs-security-baseline.md) | Adopt OWASP ASVS as the security-verification baseline | accepted |
| [0063](0063-observability-backend-and-dev-visualization.md) | Keep the observability backend operator-chosen and run a self-hosted Grafana stack for local development | accepted |
| [0064](0064-mcp-authorization-server-support.md) | Support Nami as the OAuth authorization server for MCP servers | proposed |
| [0065](0065-coding-and-naming-conventions.md) | Adopt the Microsoft naming and C# coding conventions as an enforced baseline, tailored to Nami | accepted |
| [0066](0066-design-patterns-vocabulary-and-pragmatic-use.md) | Adopt design patterns as a shared vocabulary applied pragmatically, not preemptively | accepted |
| [0067](0067-ai-assisted-development-governance.md) | Adopt an AI-assisted development policy: human-accountable, disclosed, license- and security-hygienic | accepted |
| [0068](0068-continuous-access-evaluation-shared-signals.md) | Support continuous access evaluation via the OpenID Shared Signals Framework (Nami as transmitter) | proposed |
| [0069](0069-verifiable-credentials-openid4vc.md) | Support issuing Verifiable Credentials via OpenID4VC (Nami as issuer) | proposed |
| [0070](0070-local-development-tls.md) | Serve HTTPS in local development with a locally-trusted cert behind a terminating reverse proxy | accepted |
| [0071](0071-identity-change-event-publishing.md) | Publish identity change events outward through a transactional outbox to a message broker, for backend consumers that are not OIDC relying parties | accepted |
| [0072](0072-ui-rendering-stack.md) | Render the human-facing UI as server-rendered Razor with no client runtime: Razor Pages for login and consent, MVC Razor for admin, Blazor deferred | accepted |
| [0073](0073-edge-posture-and-forwarded-headers.md) | Assume an L7 edge in front of the deployment, define the direct-to-internet fallback, and process forwarded headers only from trusted proxies | accepted |
| [0074](0074-database-ha-and-cache-durability.md) | Adopt a primary-plus-standby PostgreSQL topology with automatic failover, keep read replicas an optional non-v1 lever, and never depend on Redis durability | accepted |
| [0075](0075-security-sensitive-port-invariants.md) | Treat security-sensitive ports as carrying non-weakenable invariants, verified by a contract test the consumer runs | accepted |
| [0076](0076-application-transport-security.md) | Decide the application's own transport security: HSTS policy, the Kestrel TLS floor, and the transport-requirement guard | accepted |
| [0077](0077-metric-cardinality-and-telemetry-privacy.md) | Bound metric cardinality with an allow-listed tag set, and keep personal data out of the diagnostics lane | accepted |
| [0078](0078-load-test-tooling.md) | Adopt Apache JMeter as the load-test tool, replacing k6 (AGPL) and NBomber (commercial), and give the tool a row in the stack of record | accepted |
| [0079](0079-admin-api-http-conventions.md) | Decide the Admin API's HTTP surface by rule rather than per endpoint: revocation is DELETE, a tenant prefix follows the tenant discriminator, paging is a body envelope, If-Match splits by intent, and there is no generic proposal-creation route | accepted |
| [0080](0080-health-and-readiness-probe-contract.md) | Serve two anonymous probe routes, /health/live and /health/ready, on both hosts, with the Admin API's probes the single deliberate exemption from RequireActor | accepted |
| [0081](0081-dual-control-target-guard-taxonomy.md) | Classify a dual-control proposal's target as mutate, create, or query, so the time-of-check guard checks something for every action and a targetless proposal cannot be retried forever | accepted |
| [0082](0082-abuse-detection-lanes-and-grouping-keys.md) | Give every abuse rule a lane that can answer it, and add the SubjectRef, SourceIpHash and ClientId grouping keys the audit lane was missing | accepted |
| [0083](0083-abuse-detection-is-built-in.md) | Ship abuse detection as a built-in clustered job over the local audit store, bridging to the metric lane through a bounded rule-and-severity counter, and never blocking | accepted |
| [0084](0084-membership-removal-semantics.md) | Define what removing a person from a tenant guarantees: cascade the grants rooted here and the tenant-scoped tokens in one transaction, report the residuals the call cannot close, and refuse to leave a tenant with no administrator | accepted |
| [0085](0085-telemetry-instrument-naming.md) | Namespace every custom OpenTelemetry instrument nami.identity. and freeze the catalogue as public API, so a cardinality-cap selector cannot silently match nothing | accepted |
| [0086](0086-pin-ci-actions-by-commit-sha.md) | Pin every CI action by commit SHA rather than by a movable tag, extending ADR-0051's never-a-mutable-tag rule to the code that runs first and with the most access | accepted |
| [0087](0087-http-surface-snapshot-gate.md) | Lock the HTTP surface by committing a snapshot of the generated OpenAPI document and failing CI on a diff, because ADR-0044's eight mechanisms reach every public surface except the URL an adopter actually calls | accepted |
