# Architecture Decision Records

Nami's architecture was designed decision-first: every significant choice is recorded as an ADR with its context, the options considered, and the rationale. Accepted ADRs are binding until superseded.

Format: [MADR 4.0.0](https://adr.github.io/madr/), full template (see [ADR-0000](0000-use-markdown-architectural-decision-records.md)). Files are named `NNNN-short-title-with-dashes.md`. ADRs `0001`-`0035` are being imported and translated from the original design corpus, keeping their original numbering one-to-one; new decisions continue from `0036`.

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
| 0025-0035 | _importing from the design corpus..._ | |
