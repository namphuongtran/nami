---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; the existing spike-harness (Testcontainers on PostgreSQL 18) and Microsoft container/testing guidance
informed: all contributors, via this repository
---

# Run locally with docker-compose dependencies, multi-stage Dockerfiles, Testcontainers integration tests, and a defined first-run order

## Context and Problem Statement

The repository already has a spike-harness (Testcontainers on PostgreSQL 18) as its first running code, but the dev/test/first-run story is scattered across several design documents, with no single place settling how to run the whole system locally, how first-run setup proceeds, and how end-to-end testing works. The requirement is to run locally with no cloud (consistent with the Database-provider default of ADR-0006/0009), reproducibly, with a clear end-to-end test path. Production IaC (OpenTofu, ADR-0023) is a separate concern; this ADR is only about dev and CI on the local inner loop.

## Decision Drivers

* Run 100% locally and offline, with no cloud dependency, matching the Database-provider default.
* Be reproducible, with the dev environment, the test environment, and the production engine all on the same PostgreSQL major.
* Provide a clear end-to-end test path.
* Keep a fast inner loop for developers.

## Considered Options

* Manually install PostgreSQL/Redis on the dev machine
* docker-compose for dependencies plus Testcontainers for integration tests plus `dotnet run` for the app
* Full local Kubernetes (kind/minikube)

## Decision Outcome

Chosen option: "docker-compose for dependencies plus Testcontainers plus `dotnet run`", because it is reproducible, runs locally and offline, and matches the existing spike-harness. Manual installation is not reproducible, and full local Kubernetes is overkill for the inner loop (reserved for staging).

**A. docker-compose (dependency-only for the inner loop).** `deploy/docker-compose.yml` starts the dependencies; the app runs via `dotnet run`/IDE for a fast inner loop, or via a compose profile:

* `postgres`: image `postgres:18` (matching production: native `uuidv7()`, forced RLS), on the same major as Testcontainers and production so behavior does not drift; a persistent volume; `POSTGRES_*` from `.env` (no committed secrets).
* `pgadmin` (a DB admin UI; a lighter alternative is adminer) — dev only, never production.
* `redis` (distributed cache, DPoP replay, cache backplane) — optional for the inner loop, degrading fail-open.
* `otel-collector` (with optional trace/log viewers) for local logs, metrics, and traces (ADR-0022) — optional.
* No local KMS/key vault, so the Database-provider default applies (ADR-0006/0009): the signing key, the Data Protection keyring, and secrets live in PostgreSQL, with the DP-root certificate mounted from a file in dev. It runs 100% offline.

**B. Dockerfile (multi-stage, per deployable).** Each runnable project (`Nami.Identity`, `Nami.Identity.Admin.Api`, `Nami.Identity.Admin.App`) has a multi-stage Dockerfile:

* A `build` stage on the .NET 10 SDK image runs `restore` (via Central Package Management) and `publish -c Release`.
* A `runtime` stage on the .NET 10 ASP.NET image (with chiseled/distroless under consideration for a small attack surface) runs non-root (`USER app`) with a `HEALTHCHECK` on `/health/live`.
* The runtime base image is digest-pinned (not merely tag-pinned) for reproducible builds and against tag mutation, with a scheduled rebuild plus container-scan plus re-sign cadence (to pick up base-image CVE patches, scan the image, and re-sign the artifact) as a named container-scan gate in the pipeline, aligned with SBOM/signing/SLSA supply-chain practice.
* No secrets or certificates are baked into the image; they load at runtime via environment/secret store (ADR-0009). `.dockerignore` excludes bin/obj and secrets.
* mTLS behind a TLS-terminating proxy (a production-deploy note recorded here for Dockerfile/host consistency): explicitly settle pass-through versus terminate-and-forward. If the proxy terminates TLS and forwards the client certificate (for example an `X-Forwarded-Client-Cert` header), ASP.NET Core certificate forwarding must be enabled with tightly locked known-proxies/known-networks (trusting only the internal proxy IPs), and the client-cert header must be stripped at the edge (the proxy deletes any inbound client-cert header and sets its own) to defeat a spoofed-header mTLS bypass. Local dev is usually pass-through (Kestrel receives the certificate directly). A negative test asserts that a client-set client-cert header is rejected and never treated as mTLS-authenticated.

**C. First-run setup (an explicit order that avoids chicken-and-egg),** on an empty database, matching the ADR-0012 bootstrap:

1. `docker compose up -d postgres redis`, then wait for `postgres` to be healthy (`pg_isready`).
2. Migrate the database (dev) with a dedicated one-shot migrator (a `migrator` compose service, or `dotnet ef database update` per context) — never migrate-on-startup in production (ADR-0017); dev may enable migrate-on-startup in `Development` for convenience. This applies to the four contexts (OpenIddict, Identity, Data Protection, control plane) in order, each with its own history table.
3. Auto-seed the first key (ADR-0012): app startup blocks until ready, seeds the signing and encryption key with immediate activation and DP-wrapping, and `/health/ready` fails until a key exists.
4. Seed clients/scopes (dev) with idempotent seeders from `appsettings.Development` (for example `web`, `worker`, and `admin-app` clients, an `admin-api` scope, and an example tenant).
5. Bootstrap the first admin (ADR-0015) through the separate, audited break-glass path.
6. `dotnet run` (or the full compose profile), then `/health/ready` passes and the system is usable.

A Makefile/`justfile`/`dotnet` tool wraps these into one `make dev-up`. A production-deploy note that gates the provision saga: when a tenant uses an issuer subdomain (`tenant.id.<domain>`), the provision saga must include a DNS-plus-TLS-cert step before `Enabled=true` — defaulting to a wildcard `*.id.<domain>` certificate (simplest) in Helm/IaC, or per-subdomain ACME/cert-manager (more isolated) — gating `Enabled=true` on cert-ready, with a path-based fallback (`id.<domain>/tenant`) for environments that cannot provision a subdomain (local dev uses path-based, needing no wildcard cert).

**D. Testcontainers (integration tests, matching the spike-harness).** Integration tests spin Testcontainers PostgreSQL 18 (not SQLite, because RLS, `xmin`, and `uuidv7()` are PostgreSQL-specific), reusing a container across a test class for speed. `WebApplicationFactory<Program>` boots the app in-memory against Testcontainers PostgreSQL to exercise the full pipeline (the multi-tenant filter, RLS, applied migrations), with Redis Testcontainers when testing replay/backplane. Pure handler unit tests need no container.

**E. End-to-end tests.** API end-to-end tests use xUnit plus `WebApplicationFactory` plus Testcontainers (token issuance/validation/revocation/introspection, plus a multi-tenant isolation negative test). UI end-to-end tests for the admin app use Playwright (login, propose, a second user approving with step-up, then executed, asserting no token in the browser), run on the compose stack or `WebApplicationFactory` with a real browser. CI (GitHub Actions or an equivalent) runs unit plus integration (which needs Docker in CI for Testcontainers) plus end-to-end, with the SLO/load-test gate as a separate job.

### Consequences

* Good, because it is reproducible and runs 100% locally and offline (the Database provider), the dev environment equals the test environment equals the production engine (PostgreSQL 18), onboarding is one command, and the end-to-end path is clear.
* Good, because Testcontainers matches the existing spike-harness, so there is no drift.
* Bad, because it needs Docker on the dev machine and in CI (Docker-in-Docker for Testcontainers), and there is image build time (mitigated by layer caching and a chiseled runtime).
* Bad, because docker-compose is dev/CI only; production deployment is OpenTofu plus Helm (ADR-0023), and compose is never used for production (stated to avoid confusion).

### Confirmation

* Testcontainers for .NET (MIT) is already used in the spike-harness (PostgreSQL 18). Microsoft guidance covers multi-stage .NET Dockerfiles, chiseled images, `WebApplicationFactory` integration testing, and EF Core migrations applied at deployment (a script/bundle, not startup-migrate in production). Playwright (Apache-2.0) covers .NET UI end-to-end testing. The Database-provider default (ADR-0006/0009) runs without a cloud, and PostgreSQL 18 matches the `uuidv7`/RLS requirements.
* This is distinct from ADR-0023 (OpenTofu as production IaC); this ADR is the dev/CI inner loop only.

## Pros and Cons of the Options

### Manually install PostgreSQL/Redis on the dev machine

* Good, because it needs no container tooling.
* Bad, because it is not reproducible and causes environment drift between developers and CI.

### docker-compose plus Testcontainers plus `dotnet run` (chosen)

* Good, because it is reproducible, offline, one-command, and identical to the spike-harness and the production engine.
* Bad, because it requires Docker locally and in CI and incurs image build time.

### Full local Kubernetes (kind/minikube)

* Good, because it is closest to a production topology.
* Bad, because it is overkill for the inner loop; it is reserved for staging.

## More Information

* Original decision 2026-07-04, accepted with defaults: the DB admin tool is pgAdmin; dev migration uses a dedicated one-shot `migrator` compose service (closest to production, which stays on script/bundle with no startup-migrate); the runtime base is chiseled/distroless.
* Build-time follow-ups: write `deploy/docker-compose.yml`, `.env.example`, a per-deployable chiseled Dockerfile, `make dev-up`, and the CI workflow.
* Related decisions: ADR-0006/0009 (the Database-provider default, so it runs without a cloud), ADR-0012 (the bootstrap auto-seed order), ADR-0015 (the first-admin break-glass path), ADR-0017 (no migrate-on-startup in production and the migration model), ADR-0018 (pooling), ADR-0022 (local OpenTelemetry), ADR-0023 (OpenTofu as production IaC, distinct from this), ADR-0024 (Testcontainers at the infrastructure edge), ADR-0030 (the .NET SDK/runtime image pin), ADR-0031 (12-factor: no baked secrets, HEALTHCHECK, non-root, and no migrate-on-startup), and ADR-0060 (the consolidated testing strategy that this ADR's test types feed into).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. The product-name placeholder was set to the repository's `Nami.Identity.*` convention and the issuer-domain placeholder made generic; tool names (PostgreSQL, Redis, Testcontainers, Playwright, Docker, Helm, and the others) are retained as neutral technical references.
