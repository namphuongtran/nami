# Architecture Decision Records

Nami's architecture was designed decision-first: every significant choice is recorded as an ADR with its context, the options considered, and the rationale. Accepted ADRs are binding until superseded.

Format: [MADR 4.0.0](https://adr.github.io/madr/), full template (see [ADR-0000](0000-use-markdown-architectural-decision-records.md)). Files are named `NNNN-short-title-with-dashes.md`. ADRs `0001`-`0035` are being imported and translated from the original design corpus, keeping their original numbering one-to-one; new decisions continue from `0036`.

Several ADRs defer a policy, threshold, or human sign-off before general availability; those are consolidated as one release gate in the [Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

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
| [0011](0011-no-restart-key-rotation.md) | Rotate signing and encryption keys without restarting, via a custom OpenIddict options monitor | accepted |
| [0012](0012-key-bootstrap-and-dr-sequence.md) | Bootstrap keys by auto-seeding at cold start, root the keyring in an X.509 certificate, and restore both key stores together | accepted |
| [0013](0013-mfa-assurance-and-step-up.md) | Make MFA the producer of acr/amr/auth_time and enforce step-up assurance | accepted |
| [0014](0014-advanced-protocol-scope.md) | Build both mTLS and DPoP sender-constrained tokens, and deliberately de-scope FAPI-specific protocols | accepted |
| [0015](0015-admin-break-glass-and-first-admin-bootstrap.md) | Provide an OIDC-independent admin break-glass path and a one-time first-admin bootstrap | accepted |
| [0016](0016-right-to-erasure.md) | Reconcile GDPR right-to-erasure with the immutable audit chain using chain-over-commitments | accepted |
| [0017](0017-tenant-provisioning-and-silo-migration.md) | Orchestrate the tenant lifecycle with build-artifact migrations, per-tenant version gating, and expand/contract | accepted |
| [0018](0018-dbcontext-pooling-for-pool-mode.md) | Pool the Pool-mode OpenIddict DbContext with a per-request mutable TenantId, with a non-pooled fallback | accepted |
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
