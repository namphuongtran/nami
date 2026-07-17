---
status: "accepted"
date: 2026-07-05
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Product/Ops (the numeric SLO table and error-budget policy are theirs to ratify); the Google SRE multi-window burn-rate method; evidence in doc 11 / doc 22
informed: all contributors, via this repository
---

# Adopt self-load-tested NFR targets and make the SLO a formal release gate, with burn-rate alerting and an external synthetic canary

## Context and Problem Statement

Nami targets roughly 10k concurrent users, but there is no published vendor benchmark to lean on, and the observability stack decision (ADR-0022, `ILogger` plus OpenTelemetry) only fixes how signals are emitted, not what "healthy" means or when to stop shipping. Several load-bearing decisions about performance and availability lived only in the NFR document and its capacity deep-dive, with no ADR home: what the quantitative targets are and how they are established, whether the SLO is merely observed or is an enforced gate, how alerting is anchored, and how failures that only appear from outside the cluster are caught. An earlier note in the corpus said NFR/SLO should not be a separate ADR and should stay in a document banner; this audit revisits that, because the decisions are durable, have real alternatives, and are the kind a future maintainer must be able to find.

## Decision Drivers

* 10k CCU is an architectural goal to be proven, not a number to be quoted; there is no vendor benchmark to cite.
* The identity provider is on the critical path of every login, so it must hold a higher SLO than the services that depend on it (100% is the wrong target).
* Overload controls (rate-limiting and load-shedding, ADR-0040) protect the service within the SLO but are not the SLO; a measured target plus an enforcement gate is still needed.
* Alerts must be anchored to error-budget burn, not to noisy instantaneous latency or error spikes.
* Readiness probes miss failures that only manifest externally (certificate, DNS, JWKS publication, keyring), so an outside-in signal is required.

## Considered Options

* Trust published/theoretical throughput numbers, alert on instantaneous latency/errors, and rely on readiness probes
* Keep NFR/SLO as a document banner only, with no ADR
* Adopt self-load-tested targets, make the SLO a formal release gate, add error-budget-driven freeze and burn-rate alerting, and add an external synthetic canary

## Decision Outcome

Chosen option: "Adopt self-load-tested targets and make the SLO a formal release gate", recorded as an ADR (overriding the earlier banner-only lean) because it is a durable governance decision. The fixed parameters are:

* **A. Targets are self-load-tested, not vendor-quoted.** 10k CCU is an architectural goal proven by a load test (k6/NBomber), not a published benchmark. Measurements use percentiles (p95/p99), never averages, with enough tail samples per window. The starting targets are token-endpoint latency p95 < 200ms / p99 < 500ms, local validation p99 < 50ms, availability 99.9%+ (with JWKS held higher, around 99.99%), and an error budget of 1 − SLO (about 0.05%). The exact numbers are a business decision ratified with Product/Ops; this ADR records the model and the starting points, not frozen values, and there is one single source of truth for the numbers.
* **B. The SLO is a formal release/scale gate.** The load-test SLO is enforced in CI as a threshold that fails the build on breach (k6 `abortOnFail` on the p99 threshold; a non-zero exit fails the step). Widening a target requires re-ratifying at the single source of truth and propagating, never a local loosen in one file.
* **C. Error-budget-driven freeze.** The error budget is 1 − SLO over a trailing window; exhausting it freezes feature releases (except P0/security). The freeze is an automatic consequence of the budget/burn tier, not a manual decision.
* **D. Multi-window, multi-burn-rate alerting.** Alerting is on the rate of error-budget burn, not on instantaneous latency or error. The tiers (Google SRE method) are fast-burn ≥14.4× (1h window, 5m confirm) which pages and tightens the freeze to P0/security-only, mid-burn ≥6× (6h/30m) which pages, and slow-burn ≥1× (24h-3d/6h) which tickets and freezes feature releases. Each tier requires a short-window confirm to prevent alert flap, every page-severity alert must link a runbook (a page without a runbook is a defect blocked in CI), and burn rate is computed from existing counters with no new metric.
* **E. External synthetic canary.** A scheduled probe runs the full authorization-code + PKCE → token → userinfo → JWKS chain through the public/load-balancer path from outside the cluster, asserting each step, and alerts independently of pod readiness. It catches configuration/certificate/DNS/JWKS-publication/keyring failures that an internal readiness probe cannot see from the outside. Its end-to-end latency feeds the SLO gate. The canary complements the readiness probe; it does not replace it.

### Consequences

* Good, because the service's health is known from the user's perspective, and shipping stops automatically when it is unreliable, rather than by judgment call.
* Good, because burn-rate alerting anchored to the error budget is far less noisy than instantaneous-latency alerting and ties directly to the freeze policy.
* Good, because the external canary catches whole classes of outside-in failure (cert/DNS/JWKS/keyring) that green readiness probes hide.
* Bad, because it requires real load-test infrastructure and a maintained canary, which is ongoing effort rather than a one-time setup.
* Bad, because the numeric targets are interim starting points until Product/Ops ratify them, so the gate's thresholds are provisional even though the mechanism is fixed.
* Neutral, because it sits next to the observability-stack ADR (ADR-0022); this ADR is the targets and the gate, and ADR-0022 is the pipeline that emits the underlying signals.

### Confirmation

* A CI run that breaches the p95/p99 threshold fails the build, proving the gate is enforced rather than advisory.
* A game-day that burns the error budget fast fires the mapped page and tightens the freeze, and a slow burn opens a ticket and freezes feature releases.
* The canary fails and pages when JWKS publication, a certificate, or DNS breaks, even while all pods report ready.
* Every page-severity alert links a runbook, checked in CI.

## Pros and Cons of the Options

### Trust published numbers, alert on instantaneous signals, rely on readiness probes

* Good, because it needs no load-test rig, no burn-rate math, and no canary.
* Bad, because there is no vendor number to trust for this workload, instantaneous alerting is noisy and not budget-anchored, and readiness probes miss outside-in failures; the service could be failing users while every pod reports healthy.

### Keep NFR/SLO as a document banner only

* Good, because it changes nothing and keeps the ADR count down.
* Bad, because a release gate, an error-budget freeze policy, and a burn-rate alerting scheme are durable governance decisions with real alternatives; leaving them in a banner makes them easy to miss and hard to treat as binding.

### Self-load-tested targets, SLO release gate, burn-rate alerting, external canary (chosen)

* Good, because health is measured and enforced, alerting is budget-anchored, and outside-in failures are caught.
* Bad, because it is ongoing operational effort and its numbers are provisional until ratified.

## More Information

* This ADR records decisions from the NFR document (doc 11 §1, §9.0) and the capacity/SLO deep-dive (doc 22 §1, §3, §3.1-§3.2). It supersedes the earlier corpus note that NFR/SLO should remain a banner rather than an ADR. The numeric SLO table and the error-budget policy are Product/Ops ratify-pending; the abuse-alert ruleset (login/2FA-failure spikes, refresh replay, 429/503 bursts, key-access anomalies, clock-drift, RPO breach) shares this alerting infrastructure but belongs to the security and DR postures.
* Related decisions: ADR-0022 (the observability stack that emits the SLIs this ADR sets targets on), ADR-0040 (rate-limiting and load-shedding, which protect the service within the SLO but are not the SLO), ADR-0006 (RTO/RPO bound per store and the DR posture), ADR-0008 (the audit lane, kept separate from diagnostic telemetry), ADR-0018 (connection pooling, a capacity lever), and ADR-0037 (PostgreSQL, whose write path is the capacity bottleneck the targets are sized against).
* Authored in this repository in 2026-07 to record the settled NFR/SLO decisions as an ADR; neutral tools and methods (k6, NBomber, the Google SRE burn-rate method) are named factually for identification only, and no commercial competitor is named.
