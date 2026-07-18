---
status: "accepted"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the ~30 ADRs that each decided one part of the stack (see the table below)
informed: all contributors, via this repository
---

# Record the committed technology stack and its cross-cutting selection rules

## Context and Problem Statement

Nami's technology choices are all decided, but each lives in its own ADR, spread across roughly thirty records. There is no single place that answers "what is Nami built on" at a glance, and the selection rules that produced those choices (permissive licensing, cloud-agnostic ports, framework-native first, do not reinvent the protocol) were applied consistently but never written down as rules. A contributor or an evaluator cannot see the stack as a whole, and a future technology choice has no stated rubric to be judged against.

This ADR is the stack of record. It indexes the committed technologies in one table, each pointing to the ADR that owns it, and states the cross-cutting rules those choices follow. It decides no new technology; the owning ADR remains authoritative for each choice, and changing a choice means superseding that ADR, not editing this table.

## Decision Drivers

* Findability: one page that shows the whole stack and links to the decision behind each part.
* A stated rubric so a future technology choice is judged consistently, not re-argued from scratch.
* Onboarding and evaluation: a newcomer sees what Nami is built on without reading thirty ADRs.
* No re-deciding: this record must not duplicate or contradict the owning ADRs.

## Considered Options

* Leave the stack scattered across its owning ADRs.
* Maintain a stack table only in `README.md` or a docs page.
* Record a stack-of-record ADR that lists the stack and states the selection rules.

## Decision Outcome

Chosen: "a stack-of-record ADR", because the selection rules are themselves binding decisions that belong in the decision record, and because a decision record is the right home for a list that must not drift from the ADRs it cites. A friendly view may still be rendered in `README.md` or docs from this record.

### Cross-cutting selection rules (binding)

Every technology in the table below was chosen under these rules, and a future choice is judged against them:

* **Permissive OSS only.** Every dependency is MIT, Apache-2.0, or BSD-class; no copyleft, source-available, or commercial packages, enforced by a CI license-scan gate (ADR-0026).
* **Cloud-agnostic through ports.** Anything that varies by host (key store, secret store, data protection, email, observability backend) sits behind a port with adapters, and the default runs offline on PostgreSQL with no cloud (ADR-0006/0009/0024).
* **Framework-native first.** Prefer a .NET or ASP.NET Core native capability over a third-party library when it is adequate: OpenTelemetry and `Microsoft.Extensions.Logging` over Serilog (ADR-0022), ASP.NET Core Identity and native passkeys (ADR-0028), native rate limiting (ADR-0042). A third-party library must earn its place.
* **Do not reinvent the protocol.** OpenIddict owns the OAuth 2.0 / OpenID Connect engine; Nami never hand-rolls what the engine does natively, and custom protocol logic is an inserted handler, not a fork (ADR-0021/0024).
* **Pinned and regression-gated.** The engine and the runtime are version-pinned and bumped as bounded, tested events with a per-bump contract-regression suite (ADR-0021/0030), and the public API is a versioned seam under an analyzer-gated SemVer policy (ADR-0044).

### The stack of record

| Layer / concern | Committed choice | Owning ADR |
| --- | --- | --- |
| Runtime and language | .NET 10 (LTS-to-LTS cadence, multi-target) | 0030 |
| Protocol engine | OpenIddict 7.5 (pinned, seam-isolated) | 0021, 0014, 0048 |
| Database engine | PostgreSQL 18 (sole engine, forced RLS) | 0037 |
| ORM and driver | EF Core 10 and Npgsql, pooled DbContext | 0037, 0018 |
| Primary keys | UUIDv7 (one deliberate bigint exception) | 0036 |
| Multi-tenancy | Finbuckle.MultiTenant, RLS backstop, per-tenant issuer | 0001, 0049 |
| User management | ASP.NET Core Identity, native passkeys | 0028 |
| Distributed cache | Redis with FusionCache (accelerator, fail-open) | 0039, 0040, 0050 |
| Resiliency | Polly (one outbound handler; rate-limit versus load-shed) | 0040 |
| BFF and proxy | YARP with access-token management | 0029 |
| Admin and login UI | Server-rendered MVC Razor (BFF; token off the browser) | 0020 |
| Authorization engine | DB-first `ICheckAccess`, swappable to ReBAC | 0047 |
| Email | First-class subsystem with a transactional outbox | 0038 |
| Configuration | Ergonomic, fail-closed config layer | 0052 |
| Observability | OpenTelemetry / OTLP with `Microsoft.Extensions.Logging` (Serilog dropped); backend operator-chosen, Grafana stack for dev | 0022, 0041, 0063 |
| Architecture | Hexagonal shell plus vertical slices, ArchUnitNET | 0024, 0058, 0059, 0066 |
| Infrastructure as code | OpenTofu | 0023 |
| Local dev and test | docker-compose plus Testcontainers, Playwright, xUnit | 0025, 0060 |
| Code style and conventions | `.editorconfig` plus .NET analyzers and `dotnet format` | 0065 |
| Packaging and distribution | NuGet meta-package plus reference host image and template | 0027, 0044 |
| Supply chain | Keyless signing and provenance attestation | 0051 |
| Dependency policy | Permissive OSS only, CI license-scan | 0026 |
| Key management | No-restart rotation, provider-agnostic DR, per-scope keyset | 0005, 0006, 0011, 0012, 0033 |
| Security posture | Hardening-invariant startup check, abuse defense, CVE disclosure; OWASP ASVS baseline | 0043, 0042, 0045, 0062 |
| Governance | ADR-driven, DCO, dual-control releases | 0046 |

### Maintenance rule (binding)

When a new technology decision is accepted, add a row here in the same change that adds the ADR; when a choice is superseded, update the row to point at the superseding ADR. This table is an index, never the authority: if it disagrees with an owning ADR, the owning ADR wins and the table is the bug.

### Consequences

* Good, because the whole stack is visible in one place with a link to the decision behind each part, which helps onboarding, evaluation, and consistency.
* Good, because the selection rules are now stated, so a future technology choice has a rubric instead of a re-argument.
* Good, because it decides nothing new and cites owners, so it cannot contradict them as long as the maintenance rule holds.
* Bad, because an index can drift from its sources; mitigated by the maintenance rule, by the guardrail that already checks every `ADR-NNNN` reference resolves, and by treating any disagreement as a table bug rather than a competing decision.

## Pros and Cons of the Options

### Leave the stack scattered

* Good, because each choice already lives in its owning ADR and needs no new work.
* Bad, because there is no whole-stack view and no stated selection rubric, so evaluation is slow and future choices are re-argued.

### A stack table only in README or docs

* Good, because it is close to where a reader lands and can be friendly.
* Bad, because the selection rules are binding decisions that belong in the decision record, and a docs table has no status and is easy to let rot; the ADR can still feed a rendered docs view.

### A stack-of-record ADR (chosen)

* Good, because it puts the binding rules in the decision record, is `accepted` and supersede-able, and cites every owner.
* Bad, because it must be maintained in step with new stack ADRs; mitigated by the maintenance rule and the cross-reference guardrail.

## More Information

* This is a consolidating ADR, a sibling to ADR-0058 (principles) and ADR-0060 (testing strategy); together they make the "why", the "how tested", and the "built on what" findable in three records.
* `README.md` already lists a planned feature set and a why-Nami summary; it may render a derived, friendly view of this table, but this ADR is the authority for the stack.
* Authored fresh for this repository; it introduces no technology and pins no version beyond what the cited ADRs already fixed.
