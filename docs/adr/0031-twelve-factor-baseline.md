---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; the 12-factor methodology (12factor.net, Adam Wiggins at Heroku) and the 15-factor extension ("Beyond the Twelve-Factor App", Kevin Hoffman)
informed: all contributors, via this repository
---

# Adopt the 12-factor (and 15-factor) methodology as the operational baseline, closing four soft spots as enforced invariants

## Context and Problem Statement

Nami publishes for users to self-host (OSS, ADR-0027), so being cloud-native and deployable is a selling point, and versioning management in particular needs standardization. The industry standard is the 12-factor app methodology plus the "beyond the twelve-factor" 15-factor extension (adding API-first, telemetry, and auth). A current-state review found that the design already covers roughly twelve of the fifteen factors (I, II, IV, V, VII, X, XII strongly, plus XIII, XIV, and XV, with VI met through an externalized session and Data Protection keys). Four factors need tightening (III, VIII, IX, XI), of which two would be new invariants and two are already designed but not yet stated as 12-factor invariants nor enforced:

* **III Config (new):** there is a secret store (ADR-0009), but no invariant that no config or secret lives in the image and that per-deploy config comes from the environment, and no config-precedence chart.
* **VIII Concurrency (have, tighten):** stateless scale-out plus Quartz clustering for background jobs exist, but the invariant must be stated: the rotation timer (ADR-0011) must run through the clustered scheduler so it does not double-run, and the invariant has to cover **every** background job rather than only the scheduled ones, because the delivery relays are safe through a different mechanism and a bulk prune belongs off the request path entirely.
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
  * **VIII Concurrency:** horizontal scale-out through the process model, with **three sanctioned patterns for background work and no fourth**. Which pattern applies is a property of the job, not a preference, and stating only one of them would make the enforcement test in C reject the other two:
    * **Leader-guarded singleton**, for schedule-driven work where a second concurrent run is harmful. It registers through the **clustered Quartz scheduler** (a database job store with clustering on PostgreSQL, and a unique scheduler identity per instance) so that exactly one node's trigger fires across the cluster. Key rotation additionally takes a PostgreSQL advisory lock as an **independent** barrier, because two simultaneously active signing keys is a corruption rather than a hiccup and that guarantee must not rest on the scheduler's correctness alone (ADR-0011).
    * **Competing consumers**, for queue-drain work that is already safe at any instance count. The delivery relays claim rows with `FOR UPDATE SKIP LOCKED` and are idempotent per row, so N instances drain faster with no coordination and no leader. This is a **different** safety mechanism from the first, not a weaker one, and a job in this class is deliberately allowed to be a plain `BackgroundService` (ADR-0038, ADR-0019, ADR-0071).
    * **Separate invocation**, for bulk work heavy enough to compete with request latency. It is not co-hosted with request serving at all, but runs as its own process from the same image through a `prune` entrypoint mode alongside `serve`, `migrate`, and `export` (ADR-0027), on a schedule the platform owns. **In v1 the token and authorization prune is the only job in this class**, because it iterates every tenant and issues bulk deletes: co-hosting it would place a scheduled latency spike on exactly one replica while the load balancer keeps treating all replicas as equal, which is both an SLO risk (ADR-0041) and hard to attribute, since the same endpoint would be fast on every other replica.
    * What stays forbidden is an **unguarded** in-process `Timer` or `BackgroundService`, meaning one that belongs to the first class but skips the scheduler: it double-runs the moment a second instance starts. The prohibition is on the missing guard, not on `BackgroundService` as a mechanism.
  * **IX Disposability:** graceful shutdown is decided (SIGTERM, then drain, then a 30-second shutdown timeout) with a readiness gate; this adds the readiness-flip and an enforced test; startup is idempotent and fast (no migrate-on-startup, ADR-0025) and crash-only-friendly. The readiness key-check compares the active `kid` to the expected persisted `kid` rather than performing a bare Data-Protection round-trip: a round-trip **passes against a silently regenerated keyring**, so it would mask keyring loss instead of catching it, and ASP.NET Core's Data Protection logs that silent regeneration only at Debug level. The SIGTERM drain pairs with a Kubernetes `preStop` sleep so that `terminationGracePeriodSeconds > preStopSleep + shutdownTimeout`. The reference Helm chart carries the multi-AZ high-availability knobs that the claimed HA posture requires (a PodDisruptionBudget `minAvailable >= 1`, anti-affinity/topology-spread, controlled `rollingUpdate` timing, and resource requests/limits; final-review finding F53). As a deployment requirement every node, application and database alike, is time-synchronized (NTP/chrony), with a clock-drift alert when a node's offset exceeds roughly **30 seconds, half of the 60-second token `ClockSkewTolerance`**, so the alert fires *before* drift consumes the whole skew margin and `max_age`, `auth_time`, and the eight-hour refresh ceiling begin rejecting or accepting wrongly. The tolerance constant absorbs only small residual drift and is not a substitute for time synchronization.
  * **XI Logs:** the app writes its event stream to stdout/stderr (the OTLP exporter, ADR-0022) and never writes or rotates files inside the container; collection and routing are the environment's job (a collector or sidecar).
* **C. Enforce (against drift, the reason for an ADR rather than only a document):**
  * A health-probe test asserts that `/health/live` and `/health/ready` exist and that readiness fails without a key or database (from ADR-0025), plus a graceful-shutdown test that in-flight requests complete on SIGTERM.
  * An architecture/config test (TngTech.ArchUnitNET, per ADR-0024) asserts that no secret is read from a baked-in file, that there is no file-sink logging in the container profile, and that **every background job resolves to one of the three sanctioned patterns**: a schedule-driven job registers through clustered Quartz, a queue-drain service claims its rows with `SKIP LOCKED`, and the prune job is not registered as a hosted service in `serve` mode. A job matching none of the three fails the test, which is the point: the failure to catch is "unclassified", not "used the wrong base type".
  * A CI gate adds "12-factor checks" alongside the license-scan and contract-regression gates, and the reference image must declare `HEALTHCHECK`, run non-root, and read config from the environment (extending ADR-0025).
* **D. Versioning management (Factor V and XIII, the part emphasized):** a release is an immutable, version-identified artifact (build/release/run separated, ADR-0025); published packages use SemVer with the public-API analyzers (ADR-0027); dependencies are pinned lock-step via CPM (ADR-0026); and the target-framework/runtime version follows ADR-0030, giving consumers a clear version story.

### Consequences

* Good, because the cloud-native posture is explicit and enforced, so it does not drift during implementation, it is an OSS/self-host selling point, and it stays consistent with the org policy.
* Good, because the four soft spots move from implied to invariants with tests; VIII and IX in particular close real multi-instance holes (a double-run rotation and lost in-flight requests during a rolling deploy).
* Good, because naming three patterns rather than one keeps the enforcement test honest: a single-pattern rule would have flagged the `SKIP LOCKED` delivery relays as violations even though they are safe by construction, and a test that reports false violations gets suppressed rather than fixed.
* Bad, because it adds a few tests and CI checks (small) plus the discipline of graceful shutdown, of picking the right one of the three patterns per job, and of running the prune invocation from the platform rather than getting it for free in-process. Accepted.

### Confirmation

* The 12-factor methodology (12factor.net, Adam Wiggins at Heroku) and the 15-factor extension ("Beyond the Twelve-Factor App", Kevin Hoffman, adding API-first, telemetry, and auth) frame the baseline, and the detailed Covered/Partial gap map lives in the compliance document.
* The disposability hardening in B is not generic advice: the Helm HA knobs come from a final-review finding that the chart lacked them while the design claimed multi-AZ HA (F53), the `kid`-comparison readiness probe comes from the key-management design's fail-closed rule, and the time-synchronization requirement with its 30-second alert threshold comes from the resiliency design, which traces it to a severity-2 audit item and a pre-implementation review.
* The three patterns are not invented here; they are what mature identity products converge on, and they converge on **different** answers, which is why the rule is per job class rather than global. A leading OSS identity server runs scheduled work in-process on every node behind a cluster-wide "execute if not already executing on this or any other node" guard, which is pattern one. Another OSS authorization server ships its cleanup as a **separate command** from the same binary, documented for a scheduled platform job, on the stated grounds that it suits background work, is easier to run as a singleton, and does not cause request timeouts, which is pattern three and the same reasoning applied to prune here. A commercial .NET server instead tolerates concurrent cleanup and merely randomizes the first run within the cleanup interval to reduce collisions, which is the weakest of the three and has a standing double-run complaint, so it is deliberately **not** the model followed. The framework's own guidance is the decision rule adopted above: co-host lightweight input/output workers and split the memory or CPU heavy ones, since a worker process is the same background-service type with no HTTP server rather than a different capability. Verified against primary documentation on 2026-07-25.
* Many factors are already covered rather than new: dev/prod parity (X) via ADR-0025 (Testcontainers on PostgreSQL 18 equals production), the admin process (XII) via the entrypoint modes `serve`, `migrate`, `export`, and `prune` (ADR-0027), backing services (IV) via the ports (ADR-0024/0006/0009), and statelessness (VI) via the session and Data Protection keys held in the database/Redis (ADR-0003/0006).

## Pros and Cons of the Options

### A mapping document only

* Good, because it is the lightest and fastest to produce.
* Bad, because it has no gate against drift, so the four soft spots easily slip during coding.

### An ADR plus a mapping document plus enforcement (chosen)

* Good, because the baseline is committed and the four soft spots become invariants with tests and a CI gate.
* Bad, because it adds a few tests and CI checks and some background-job discipline.

## More Information

* Original decision: 2026-07-04. The Factor VIII invariant was widened on 2026-07-25 from one pattern to three, when writing the runtime and deployment views showed that the single-pattern wording covered only the scheduled jobs and would have condemned the `SKIP LOCKED` relays it was never meant to reach.
* Build-time follow-ups: implement invariant B (a graceful-shutdown handler, leader election for the scheduled singletons, the `prune` entrypoint mode, an stdout-only logging profile, and a config-precedence loader) and the test/CI gate C (the "12-factor checks").
* **Deliberately not decided here.** Whether the delivery relays are additionally given their own replica set for resource isolation is an **operator** choice, not an architectural one: they are safe co-hosted and safe standalone by the competing-consumer property, so the deployment view presents it as a knob rather than a requirement. Only the prune job is fixed off the request path, because there its placement changes a latency guarantee rather than a preference.
* Related decisions: ADR-0003 (the externalized session, Factor VI statelessness), ADR-0006/0009 (the secret/key store, Factor III/IV backing services), ADR-0011 (no-restart rotation and leader-election, Factor VIII/IX), ADR-0022 (OpenTelemetry, Factor XI/XIV), ADR-0023 (OpenTofu, build/release/run), ADR-0024 (ports, Factor IV swap), ADR-0025 (docker/first-run/health-probe/no-migrate-on-startup), ADR-0026 (lock-step CPM), ADR-0027 (release version and SemVer, Factor V), and ADR-0030 (the target-framework/runtime version).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. The authors of the two cited works are named again (restored 2026-07-25): the first import had stripped the attribution, which no content rule required and which left two cited works uncheckable. Tooling (Quartz, ArchUnitNET, OpenTelemetry, Redis) is retained as neutral technical reference, and there are no competitor references.
