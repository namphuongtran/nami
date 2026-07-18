---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: packaging precedents (OpenIddict's package split, .NET framework module and app-host stacks) and the bootstrap/config-import parity of mainstream OSS identity servers
informed: all contributors, via this repository
---

# Distribute Nami as a hybrid NuGet meta-package plus a reference host image and template, released under Apache-2.0

## Context and Problem Statement

The design is detailed to production level. The requirement is to release Nami as OSS for the community, as easy to adopt as a commercial identity server: a consumer adds a package with minimal config ("have a key and it runs") and it is monitorable out of the box. The question is whether to package it as a library or as a deployable product.

The positioning insight is that Nami builds **on** OpenIddict (the protocol library) and therefore does not re-implement the protocol from scratch. Nami's value is the opinionated, batteries-included layer on top — multi-tenancy, delegated-admin, no-restart rotation, admin, cloud-agnostic adapters, observability, and config developer-experience. So the positioning is a batteries-included distribution on top of OpenIddict, closer to an OpenIddict framework module or a .NET app-host stack than to a from-scratch server. The already-decided architecture (hexagonal ports in ADR-0024, cloud-agnostic ports in ADR-0006/0009, the auto-seed key in ADR-0012, and built-in OpenTelemetry in ADR-0022) already enables exactly this.

## Decision Drivers

* Easy adoption: add a package with minimal config, "have a key and it runs", monitorable out of the box.
* Serve both embedders (a library) and turnkey deployers (an image).
* Community OSS, free, with no paid gates.
* The value is the opinionated layer on OpenIddict, not a protocol re-implementation.

## Considered Options

* A pure library (NuGet plus `AddXxx()`, consumer self-hosts)
* A pure deployable (a container image plus Helm)
* A hybrid (a meta-package plus a fluent builder, plus a reference host image and a `dotnet new` template)

## Decision Outcome

Chosen option: "The hybrid", released under Apache-2.0, because the same core then serves both an embedder and a turnkey deployer, satisfying both "add a package" and "have a key and it runs, monitorable". A pure library fails "deploy and it runs", and a pure deployable fails "embed as a package".

* **A. Core: a NuGet meta-package plus a fluent builder (commercial-grade DX).** Ship a meta-package `Nami.Identity` plus granular sub-packages so a consumer takes only what it needs (mirroring how OpenIddict splits packages). A fluent builder, `services.AddNamiIdentity(cfg)...`, has safe defaults like `AddOpenIddict()`, with the minimal config being a connection string and an issuer. "Have a key and it runs" comes from auto-seed (ADR-0012, the Database provider), so no key is provisioned by hand. "Monitorable" comes from built-in OpenTelemetry (ADR-0022), so wiring up OTLP just works.
* **B. Reference host: an image plus Helm plus a template (deploy-and-it-runs DX).** A chiseled container image (ADR-0025) runs the IdP and Admin, configured via environment variables, in a dual-mode `serve`/`migrate`. A `dotnet new nami-identity` template scaffolds a customizable host. A Helm chart covers Kubernetes (production IaC is OpenTofu, ADR-0023). A bootstrap-admin path applies a `Bootstrap__Admin*` environment configuration once at first start (when no admin exists), with a forced password change, an audit entry, a tie to the ADR-0015 first-admin bootstrap and the ADR-0007 break-glass path, and a fail-fast in Production — parity with the bootstrap-admin environment variables of mainstream OSS identity servers such as Keycloak and Zitadel, giving a turnkey "run the container, then log in as admin". Declarative config import/export provides an idempotent, secret-free import (in `migrate`/init mode) covering clients, scopes, resources, Pool tenants and their memberships, claim-mappers, and CORS origins, upserting by concurrency token so it does not overwrite an operator's live edit unless forced (config precedence per ADR-0052), plus an `export` sub-command as a third entrypoint mode alongside `serve`/`migrate` that dumps the current config (never secrets or keys); this enables "pull and run my setup" plus GitOps and backup, at parity with mainstream OSS import/export (Keycloak's realm import/export and Zitadel's setup steps). The build/optimize step some servers have is not applicable to .NET, since publish-time already optimizes, so Nami keeps `serve`/`migrate`.
* **C. Release license: Apache-2.0.** It matches OpenIddict, is permissive, and carries a patent grant that matters for a security product, so the community can use it freely rather than paying. This is distinct from ADR-0026, which governs the licenses Nami *consumes*; this is the license Nami *releases*. There is no dual-license commercial tier, keeping Nami purely OSS in line with the "for the community" intent.
* **D. Public-API stability discipline (a library's survival condition).** The public API is treated as a versioned seam with strict SemVer, an analyzer-locked surface, and a staged deprecation policy. This is the ADR-0021 seam-catalogue thinking applied to Nami's own API, since a consumer is downstream of Nami exactly as Nami is downstream of OpenIddict. The full policy is recorded in ADR-0044 (public-API stability and SemVer) and is not restated here.
* **E. Extension model for consumers: the ports (ADR-0024).** The hexagonal ports (key store, secret resolver, tenant store, audit sink, email dispatcher, the ReBAC `ICheckAccess`, and so on) are documented extension points, so a consumer swaps an adapter (for example a key store to Vault) without forking the core.
* **F. OpenID certification path and target profiles.** Nami self-certifies a stable reference-host at specific OpenID Provider profiles rather than an abstract library (certification is per host, config profile, and version). The target set is OP Basic, OP Config, and OP FormPost, following the protocol scope of ADR-0014; Hybrid/Implicit is certified only if a client needs it, Dynamic (DCR) is deferred until OpenIddict ships it natively (8.0), FAPI 2.0 is not targeted (its message-signing profiles are de-scoped in ADR-0014), and back-channel-logout and DPoP/mTLS conformance follow once each is stable and proven. The OpenID Foundation conformance suite already runs in CI on the reference-host profile so it stays green before any submission, and re-certification is done on a major version or a protocol-affecting change. Certification is a pre-public-release step, not an MVP blocker. One precondition is open: certification needs a stable public reference-host, and whether to stand one up (owner, hosting, patch cadence, cost) or to certify local-Docker-only is pending Ops ratification.

### Consequences

* Good, because it satisfies both "add a package" and "deploy and it runs, has a key, and is monitorable"; it is both embeddable and turnkey; it is free for the community (Apache-2.0); and consumers extend it through ports rather than forks.
* Good, because the current architecture is already ready (ports, auto-seed, OpenTelemetry), so productization is packaging and polish rather than a rewrite.
* Bad, because maintaining a library is heavier than an internal app: public-API stability (SemVer plus the analyzer), docs and samples, versioning discipline, and community support, which need a governance/security policy and a CI publish pipeline.
* Bad, because there is no dual-license, and therefore no license revenue, which is traded for adoption and the OSS spirit.

### Confirmation

* OpenIddict and commercial identity servers both ship via NuGet package splits; a .NET framework module and an app-host stack are the hybrid precedents (packages plus a template/host); and the "put the IdP in a separate app" guidance supports a reference host. The public-API analyzers are the Microsoft-recommended way to hold API stability, and the Apache-2.0 patent grant is standard for security OSS.
* Approach settled 2026-07-07: the reference-host distribution (image/compose/Helm/template), the supply-chain measures (signing, SBOM, SLSA, scanning; detailed in ADR-0051), the bootstrap-admin, and the declarative config import/export are the decided approach; only build-time sub-details remain (final environment-variable names, the import format JSON versus YAML, and the template option set), which are not open decisions.

## Pros and Cons of the Options

### A pure library

* Good, because it is the simplest to embed.
* Bad, because "deploy and it runs" is not met; the consumer must build and operate the host.

### A pure deployable

* Good, because "deploy and it runs" is met out of the box.
* Bad, because it cannot be embedded as a package into a consumer's own host.

### A hybrid (chosen)

* Good, because one core serves both consumer types and satisfies every adoption goal.
* Bad, because it is the most to build and maintain (both a package line and a reference host).

## More Information

* Original decision 2026-07-04; the reference-host distribution, supply-chain measures, bootstrap-admin, and declarative config import/export were confirmed on 2026-07-07.
* Build-time follow-ups: the final package graph, the fluent-builder API surface, the template scaffold, the docs site, the OpenID certification timing and profile set (F), including whether to stand up a public reference-host or certify local-Docker-only (Ops ratify), the CI publish pipeline (NuGet plus a container registry plus an SBOM per ADR-0026), and the governance/security files.
* Related decisions: ADR-0006/0009 (cloud-agnostic ports as swappable adapters for consumers), ADR-0007 (break-glass, tied to bootstrap-admin), ADR-0012 (the auto-seed key behind "have a key and it runs"), ADR-0020 (admin packages), ADR-0021 (version-adaptation thinking applied to Nami's own API), ADR-0022 (built-in OpenTelemetry for monitorability), ADR-0024 (hexagonal ports as consumer extension points), ADR-0025 (the local-dev image and template), ADR-0026 (the consume-side license policy, distinct from this release license), and ADR-0044 (the full public-API stability and SemVer policy that D summarizes).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. References to a commercial identity server were generalized; the product-name placeholder was set to the repository's `Nami.Identity.*` convention (the meta-package `Nami.Identity`, the builder `AddNamiIdentity`, and the template `dotnet new nami-identity`); OSS peers (Keycloak, Zitadel), the .NET framework precedents, and tooling are retained as neutral technical references.
