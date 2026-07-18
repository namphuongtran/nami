---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0022 (OTLP emission scope), ADR-0025 (the dev inner loop), ADR-0026 (permissive-only dependency policy), ADR-0061 (framework-native-first); observability-tool licenses verified 2026-07-18
informed: all contributors, via this repository
---

# Keep the observability backend operator-chosen and run a self-hosted Grafana stack for local development

## Context and Problem Statement

ADR-0022 fixed how Nami emits signals: logs, metrics, and traces travel over one OTLP pipeline, vendor-neutral and cloud-agnostic. It deliberately stopped at emission and did not choose a backend (the store and visualization that OTLP data lands in). That leaves two things unspecified: what a developer sees on the local inner loop (ADR-0025 runs docker-compose plus `dotnet run`, but nothing renders the traces and metrics), and what the reference deployment (ADR-0027/0031) assumes for production observability.

Naming a single production backend would break the cloud-agnostic posture, and naming nothing leaves the dev loop blind. There is also a licensing subtlety to get right, not wrong: the most capable self-host stack (Grafana, Loki, Tempo) is AGPLv3, and Nami's dependency policy (ADR-0026) is permissive-only. The question is whether that policy even applies to a local dev backend, and where the real line is. This ADR decides the backend posture for production, for the dev loop, and for Nami's shipped artifacts.

## Decision Drivers

* Cloud-agnostic: do not mandate a production backend; the operator owns that choice (ADR-0006/0009/0022).
* Keep Nami's shipped artifacts permissive: no copyleft baked into the NuGet packages, reference host image, or Helm chart (ADR-0026).
* A capable local dev experience: dashboards, log search, and trace views on the inner loop, matching what developers expect (ADR-0025).
* Apply the license policy correctly: distinguish a shipped dependency from a dev-time tool.

## Considered Options

* Bundle a backend into Nami's shipped artifacts (reference host image / Helm chart) as the default.
* Keep production operator-chosen, run a self-hosted Grafana stack for local dev from upstream images, and keep shipped artifacts permissive.
* Use the .NET Aspire dashboard (MIT) as the local default instead of a full stack.
* Bless a single hosted vendor as the default backend.

## Decision Outcome

Chosen: "operator-chosen production, a self-hosted Grafana stack for local dev, permissive shipped artifacts." It preserves cloud-agnosticism, gives the dev loop a full-featured view, and keeps what Nami distributes copyleft-free.

### Production: Nami is backend-neutral (binding)

Nami emits OTLP (ADR-0022) and mandates no production backend. The operator points the OTLP pipeline at any OTLP-compatible backend they run or buy (self-hosted or hosted). Nami bundles no backend in the reference host or Helm chart by default; it ships the OTLP export configuration and the documentation for connecting one, and it documents the collector topology (agent versus gateway), the open item ADR-0022 flagged. Observability is cloud-agnostic exactly as key, secret, and email delivery are (ADR-0006/0009).

### Local development: a self-hosted Grafana stack (binding)

The documented dev loop runs a full observability stack as a docker-compose profile alongside the ADR-0025 dependencies: the OpenTelemetry Collector (Apache-2.0) receives OTLP and fans out to Prometheus (Apache-2.0) for metrics, Loki for logs, and Tempo for traces, with Grafana as the dashboard, log-search, and trace-view UI. A developer gets metrics, logs, and traces locally with one command. It is a compose profile so pure unit-test runs need not start it.

### Why the AGPL components are acceptable here (binding rationale)

Grafana, Loki, and Tempo are AGPLv3 (relicensed from Apache-2.0 in 2021). ADR-0026's permissive-only rule governs Nami's **dependencies**: code compiled or linked into Nami's packages and distributed in its artifacts. The dev stack is not that. It is unmodified upstream container images, pulled from their own registries, run as separate services that Nami talks to over OTLP. Nami does not link, modify, or redistribute them, so they are dev tooling, not a dependency, and AGPL's copyleft (including its network clause, which triggers on modifying and then conveying or serving the work) does not reach Nami's code. The line ADR-0026 protects is therefore intact.

### Shipped artifacts stay permissive (binding)

The line that is held: Nami's shipped artifacts (NuGet packages, the reference host image, the Helm chart) bundle no AGPL component as a default, because shipping them would make Nami a distributor of copyleft. Production observability stays operator-chosen; the Grafana stack lives in the dev-time compose profile, not in what Nami distributes.

### Confirmation

Build-time: verify the compose profile wires OTLP through the Collector into Prometheus, Loki, and Tempo with Grafana datasources and a starter dashboard, and settle the collector agent-versus-gateway topology for the reference host (the ADR-0022 open item). The dev-stack images are upstream and replaceable and carry no production commitment.

### Consequences

* Good, because production observability stays cloud-agnostic and Nami forces no backend on operators.
* Good, because the dev loop gets a full metrics, logs, and traces experience with one command, matching what developers expect.
* Good, because the license question is answered precisely rather than avoided: AGPL is fine for a dev-time upstream tool, and Nami's distributed artifacts stay copyleft-free.
* Bad, because the dev stack is heavier than a single dashboard container, costing local resources and start time; mitigated by making it an opt-in compose profile.
* Bad, because Nami ships no batteries-included production dashboards, so an operator must connect a backend; mitigated by clear docs and the ubiquity of OTLP backends.
* Bad, because the local Grafana view will differ from whatever an operator runs in production; accepted, because the inner loop optimizes for developer insight, not production parity.

## Pros and Cons of the Options

### Bundle a backend into shipped artifacts

* Good, because it is the most turnkey experience out of the box.
* Bad, because it presumes a production backend (breaking cloud-agnosticism) and, for the Grafana stack, ships copyleft in Nami's artifacts against ADR-0026.

### Operator-chosen production plus a self-hosted Grafana dev stack (chosen)

* Good, because production stays neutral and permissive, the dev loop is full-featured, and the license line is correctly drawn between dependency and dev tool.
* Bad, because the dev stack is heavier and differs from production; both accepted and mitigated as above.

### The .NET Aspire dashboard as the local default

* Good, because it is MIT, framework-native (ADR-0061), and a single zero-config container.
* Bad, because it gives a thinner experience (no durable log/metric store, no rich dashboards) than the Grafana stack; set aside in favor of local fidelity, and it remains available to any developer who prefers a lighter view.

### Bless a single hosted vendor

* Good, because it would give one polished, documented path.
* Bad, because it breaks cloud-agnosticism and privileges one vendor in an OSS project.

## More Information

* Related decisions: ADR-0022 (OTLP emission, the scope this ADR extends), ADR-0025 (the docker-compose dev loop the observability profile joins), ADR-0027 and ADR-0031 (the reference host and 12-factor deployment that ship OTLP config, not a bundled backend), ADR-0026 (the permissive-only dependency policy, whose scope this ADR clarifies as dependencies rather than dev tooling), ADR-0006/0009 (the cloud-agnostic port posture observability now matches), and ADR-0061 (framework-native-first, weighed for the Aspire-dashboard alternative).
* Licenses verified 2026-07-18: Grafana, Loki, and Tempo are AGPLv3 (since 2021); Prometheus, the OpenTelemetry Collector, and Jaeger are Apache-2.0; the .NET Aspire dashboard is MIT.
* Authored fresh for this repository.
