---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; the 12-factor methodology (12factor.net) and the 15-factor extension ("Beyond the Twelve-Factor App")
informed: all contributors, via this repository
---

# Adopt the 12-factor (and 15-factor) methodology as the operational baseline, closing four soft spots as enforced invariants

## Context and Problem Statement

Nami publishes for users to self-host (OSS, ADR-0027), so being cloud-native and deployable is a selling point, and versioning management in particular needs standardization. The industry standard is the 12-factor app methodology plus the "beyond the twelve-factor" 15-factor extension (adding API-first, telemetry, and auth). A current-state review found that the design already covers roughly twelve of the fifteen factors (I, II, IV, V, VII, X, XII strongly, plus XIII, XIV, and XV, with VI met through an externalized session and Data Protection keys). Four factors need tightening (III, VIII, IX, XI), of which two would be new invariants and two are already designed but not yet stated as 12-factor invariants nor enforced:

* **III Config (new):** there is a secret store (ADR-0009), but no invariant that no config or secret lives in the image and that per-deploy config comes from the environment, and no config-precedence chart.
* **VIII Concurrency (have, tighten):** stateless scale-out plus Quartz clustering for background jobs exist, but the invariant must be stated and the rotation timer (ADR-0011) must run through the clustered scheduler so it does not double-run.
* **IX Disposability (have, tighten):** graceful shutdown with a shutdown timeout, a keys-loaded readiness gate, and no-migrate-on-startup are decided; this only needs raising to an enforced test.
* **XI Logs (new):** OpenTelemetry/`ILogger` exist (ADR-0022), but no invariant that the app logs to the stdout stream and does not write files inside the container.

No factor lacks a foundational design; the risk is drift at implementation time (losing in-flight requests, a double-run rotation, a secret in the image, or file-logging in a container), plus losing cloud-native credibility with consumers if the posture is neither stated nor enforced.

## Decision Drivers

* Cloud-native and self-host readiness is an OSS selling point, so the posture must be explicit and enforced.
* Prevent implementation drift on the four soft spots.
* Give self-hosting consumers a clear versioning story.
* Stay consistent with the org policy that secrets never leave the store.

## Considered Options

* A mapping document only (reference), with no ADR and no enforcement
* An ADR fixing the baseline, plus a mapping document, plus enforcement via tests and CI

## Decision Outcome

Chosen option: "An ADR plus a mapping document plus enforcement", adopting 12-factor and 15-factor as the operational contract of the service (and of the reference host the consumer receives), because a mapping document alone has no gate against drift and the four soft spots would slip during implementation.

* **A. Mapping and gap = the compliance document (single source of truth).** It maps each factor from I to XV to the existing decision/document with a Covered/Partial status and closes the four soft spots. The document is the lookup; this ADR is the commitment.
* **B. Close the four soft spots as invariants:**
  * **III Config:** every per-deploy value (connection string, issuer, KMS endpoint, OTLP endpoint, secret) comes from the environment or the secret store (ADR-0009) and is never baked into the image (ADR-0025 already forbids baking secrets; this raises it to a general invariant with a config-precedence chart of environment over secret-store over `appsettings.{Env}` over `appsettings`), matching the org policy that secrets never leave the store.
  * **VIII Concurrency:** horizontal scale-out through the process model; a stateful background job registers through clustered Quartz for a single run across nodes, especially the rotation timer (ADR-0011); no unguarded in-process `Timer` or `BackgroundService`.
  * **IX Disposability:** graceful shutdown is decided (SIGTERM, then drain, then a 30-second shutdown timeout) with a readiness gate; this adds the readiness-flip and an enforced test; startup is idempotent and fast (no migrate-on-startup, ADR-0025) and crash-only-friendly. The readiness key-check compares the active `kid` to the expected persisted `kid` rather than a bare Data-Protection round-trip (which would mask a lost keyring), and the SIGTERM drain pairs with a Kubernetes `preStop` sleep so `terminationGracePeriodSeconds > preStopSleep + shutdownTimeout`. The reference Helm chart carries multi-AZ high-availability knobs (a PodDisruptionBudget `minAvailable >= 1`, anti-affinity/topology-spread, controlled `rollingUpdate` timing, and resource requests/limits), and all nodes are time-synchronized (NTP/chrony) with a clock-drift alert at roughly half the token `ClockSkewTolerance`, as a deployment requirement.
  * **XI Logs:** the app writes its event stream to stdout/stderr (the OTLP exporter, ADR-0022) and never writes or rotates files inside the container; collection and routing are the environment's job (a collector or sidecar).
* **C. Enforce (against drift, the reason for an ADR rather than only a document):**
  * A health-probe test asserts that `/health/live` and `/health/ready` exist and that readiness fails without a key or database (from ADR-0025), plus a graceful-shutdown test that in-flight requests complete on SIGTERM.
  * An architecture/config test (ArchUnitNET) asserts that no secret is read from a baked-in file, that there is no file-sink logging in the container profile, and that a stateful background job registers through clustered Quartz rather than an unguarded `BackgroundService`.
  * A CI gate adds "12-factor checks" alongside the license-scan and contract-regression gates, and the reference image must declare `HEALTHCHECK`, run non-root, and read config from the environment (extending ADR-0025).
* **D. Versioning management (Factor V and XIII, the part emphasized):** a release is an immutable, version-identified artifact (build/release/run separated, ADR-0025); published packages use SemVer with the public-API analyzers (ADR-0027); dependencies are pinned lock-step via CPM (ADR-0026); and the target-framework/runtime version follows ADR-0030 — giving consumers a clear version story.

### Consequences

* Good, because the cloud-native posture is explicit and enforced, so it does not drift during implementation, it is an OSS/self-host selling point, and it stays consistent with the org policy.
* Good, because the four soft spots move from implied to invariants with tests; VIII and IX in particular close real multi-instance holes (a double-run rotation and lost in-flight requests during a rolling deploy).
* Bad, because it adds a few tests and CI checks (small) plus the discipline of graceful-shutdown and leader-election when coding a worker, which is accepted.

### Confirmation

* The 12-factor methodology (12factor.net) and the 15-factor extension ("Beyond the Twelve-Factor App", adding API-first, telemetry, and auth) frame the baseline, and the detailed Covered/Partial gap map lives in the compliance document.
* Many factors are already covered rather than new: dev/prod parity (X) via ADR-0025 (Testcontainers on PostgreSQL 18 equals production), the admin process (XII) via the migrator's `serve`/`migrate` modes, backing services (IV) via the ports (ADR-0024/0006/0009), and statelessness (VI) via the session and Data Protection keys held in the database/Redis (ADR-0003/0006).

## Pros and Cons of the Options

### A mapping document only

* Good, because it is the lightest and fastest to produce.
* Bad, because it has no gate against drift, so the four soft spots easily slip during coding.

### An ADR plus a mapping document plus enforcement (chosen)

* Good, because the baseline is committed and the four soft spots become invariants with tests and a CI gate.
* Bad, because it adds a few tests and CI checks and some worker-coding discipline.

## More Information

* Original decision: 2026-07-04.
* Build-time follow-ups: implement invariant B (a graceful-shutdown handler, leader-election for the worker, an stdout-only logging profile, and a config-precedence loader) and the test/CI gate C (the "12-factor checks").
* Related decisions: ADR-0003 (the externalized session, Factor VI statelessness), ADR-0006/0009 (the secret/key store, Factor III/IV backing services), ADR-0011 (no-restart rotation and leader-election, Factor VIII/IX), ADR-0022 (OpenTelemetry, Factor XI/XIV), ADR-0023 (OpenTofu, build/release/run), ADR-0024 (ports, Factor IV swap), ADR-0025 (docker/first-run/health-probe/no-migrate-on-startup), ADR-0026 (lock-step CPM), ADR-0027 (release version and SemVer, Factor V), and ADR-0030 (the target-framework/runtime version).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. The 12-factor and 15-factor methodology and their canonical works are cited without personal-name attribution; tooling (Quartz, ArchUnitNET, OpenTelemetry, Redis) is retained as neutral technical reference, and there are no competitor references.
