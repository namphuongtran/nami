# Nami

**An open-source, multi-tenant identity provider for .NET, built on [OpenIddict](https://github.com/openiddict/openiddict-core).**

Nami is a batteries-included OAuth 2.0 / OpenID Connect authorization server: a free, Apache-2.0 licensed alternative to commercial identity servers, designed for teams that want a production-grade IdP they can run, extend, and own.

> ⚠️ **Status: early development (pre-alpha).** Nami is being built in public. The architecture is fully designed and its risk spikes were validated with running code; the decision records are being imported into [`docs/adr/`](docs/adr/) and implementation is underway. Nothing here is production-ready yet. Watch or star the repo to follow progress.

## Why Nami?

| | |
|---|---|
| **Free and open** | Apache-2.0, no license keys, no production gates, no paid tiers on the core. Ever. |
| **Built on OpenIddict** | The protocol engine is OpenIddict, a mature, widely-deployed OSS foundation. Nami adds the opinionated product layer on top. |
| **Multi-tenant by design** | Tenant isolation (shared-database pool or dedicated silo, per tenant) is a first-class concept, not an afterthought. |
| **Zero-downtime key rotation** | Signing keys rotate automatically without restarting the server. |
| **Cloud-agnostic** | Runs anywhere .NET runs. Key stores, secrets, and email are ports with adapters (PostgreSQL, Azure, AWS, GCP, Vault, SMTP...). |
| **Admin included** | A REST admin API plus a server-rendered admin app, with RBAC, dual-control approvals, and hash-chained audit logs. |

## Planned feature set (v1)

- Authorization code + PKCE, client credentials, refresh tokens (rolling, reuse detection), device flow, token exchange, PAR, introspection, revocation
- ASP.NET Core Identity user management: MFA (TOTP + recovery codes), external identity providers, server-side sessions, back-channel logout
- Login / consent / logout UI (Razor, themeable)
- Multi-tenancy: pooled and siloed tenants, per-tenant issuer, PostgreSQL row-level security backstop
- Automatic signing-key management with no-restart rotation
- DPoP (RFC 9449) and mutual TLS sender-constrained tokens
- Admin API + admin app: applications, scopes, users, tenants, sessions, dual-control proposals, audit viewer
- First-class observability: OpenTelemetry (traces, metrics, logs) out of the box

## How Nami will ship

- **NuGet packages** for embedding in your own host: `Nami.Identity` (meta-package), `Nami.Identity.Core`, `Nami.Identity.Abstractions`, granular adapters
- **A reference host**: container image + Helm chart + `dotnet new` template, so `docker compose up` gives you a working IdP
- **Samples and docs** built and tested in CI

## Project principles

1. **Decisions are public.** Every architectural decision is recorded as an ADR with rationale and evidence, published in [`docs/adr/`](docs/adr/) (import in progress).
2. **Don't reinvent the protocol.** OpenIddict handles the OAuth/OIDC engine; Nami never hand-rolls what the engine does natively.
3. **Verified, not vibes.** Design claims trace to source-verified evidence; risky integrations were proven with runnable spikes before being committed to.
4. **Permissive dependencies only.** Every dependency is MIT/Apache-2.0/BSD-class OSS. No copyleft, no source-available, no commercial SDKs.

## Roadmap

| Milestone | Scope |
|---|---|
| M1 | Core protocol server issues tokens (auth code + PKCE, client credentials) with PostgreSQL persistence |
| M2 | Usable login: user management, MFA, login/consent UI, `docker compose up` quickstart |
| M3 | Production hardening: no-restart key rotation, observability, security review |
| M4 | Admin API + admin app, advanced flows (DPoP, mutual TLS, PAR, device, token exchange) |
| M5 | Conformance: OpenID certification, migration guide from commercial identity servers |

## Contributing

Nami is just getting started, and early contributors shape the project. See [CONTRIBUTING.md](CONTRIBUTING.md). Security issues: see [SECURITY.md](SECURITY.md), never open a public issue for vulnerabilities.

## License

[Apache-2.0](LICENSE). Copyright 2026 Nam Phuong Tran and Nami contributors.
