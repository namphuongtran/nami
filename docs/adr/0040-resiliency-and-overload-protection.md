---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ASP.NET Core 10 resilience / rate-limiting middleware and Polly `AddStandardResilienceHandler` docs; evidence in doc 11
informed: all contributors, via this repository
---

# Standardize a resiliency and overload-protection posture: one outbound resilience handler, rate-limiting and load-shedding kept distinct, Redis as an accelerator only

## Context and Problem Statement

Nami targets roughly 10k concurrent users across stateless nodes and depends on external calls (the database, email providers, external identity providers) and on Redis as a shared accelerator. Without a stated posture, resilience decisions get made ad hoc per call site: hand-composed retry/timeout/circuit-breaker pipelines that stack unpredictably, retries applied to non-idempotent operations, a single overload control conflated to serve two different purposes, and Redis quietly treated as a source of truth so that a Redis outage takes authentication down.

What resilience and overload-protection posture should the whole service adopt, so that outbound failures are contained, inbound overload is shed safely, and a Redis outage degrades performance without breaking authentication?

## Decision Drivers

* Outbound failures (DB/email/external IdP) must be contained without stacking conflicting resilience policies or retrying non-idempotent operations.
* Two different overload situations (per-caller quota/abuse versus instantaneous capacity exhaustion) must be handled by two different mechanisms, because conflating them either drops legitimate traffic or fails to protect the instance.
* Redis is an accelerator, not a root of trust: an outage must degrade latency, not availability of authentication.
* A security-critical throttle must not silently disable itself when its backing store is unavailable.
* The behavior under failure must be measurable, not assumed, so fail-open and load-shed behavior is exercised under load test.

## Considered Options

* Hand-composed resilience pipelines per call site
* A single inbound overload control serving both quota and capacity protection
* A standardized posture: one outbound resilience handler, distinct inbound rate-limiting and load-shedding, and Redis as an accelerator with an explicit degraded mode

## Decision Outcome

Chosen option: "A standardized posture". The fixed parameters are:

* **A. Outbound resilience = one standard handler.** Every outbound HTTP dependency uses Polly's `AddStandardResilienceHandler` (rate-limiter, total timeout ~30s, retry with exponential backoff and jitter, circuit breaker, per-attempt timeout ~10s) as a single handler. Pipelines are not hand-composed or stacked. Retries are disabled for non-idempotent verbs (POST/PUT/PATCH/DELETE) to avoid duplicated side effects. Database access uses EF Core `EnableRetryOnFailure()`, and any explicit transaction is wrapped in `CreateExecutionStrategy()`. Every external call (DB, email, external IdP) has a timeout.
* **B. Two distinct inbound overload controls.** Inbound rate limiting (`AddRateLimiter`/`UseRateLimiter`, fixed/sliding/token-bucket, partitioned by user/IP/client) returns **429** and exists for quota, fairness, and abuse; partitioning on raw unauthenticated input is itself treated as a DoS vector and avoided. Separately, the token endpoint has a **concurrency limiter that sheds load** with **503 plus `Retry-After`** when concurrent in-flight requests exceed a threshold, to protect the instance's immediate processing capacity. These are different in kind (quota versus capacity) and both exist simultaneously; a bulkhead (Polly concurrency limiter outbound, concurrency rate limiter inbound) isolates pools.
* **C. Redis is an accelerator with an explicit degraded mode.** Redis is a caching/acceleration layer, never the sole source of truth. On a Redis failure, the distributed cache **fails open**: a miss reads through to the durable store at higher latency rather than returning a 5xx. The server-side session store is durable PostgreSQL (ADR-0003/ADR-0037) and the Data Protection keyring is stored independently of Redis (ADR-0006), so a Redis outage does not break authentication, only degrades performance. This fail-open behavior is measured under load test.
* **D. One deliberate fail-closed carve-out.** The per-recipient email anti-abuse throttle (ADR-0038) is a security control and is **never disabled** when Redis is down: it degrades to a per-instance in-process bucket plus an outbox-row counter keyed by recipient hash, accepting per-instance approximation rather than turning the cap off. This is the single, intentional exception to the fail-open rule in C.

This posture is the source of the fail-open-versus-fail-closed cache policy that ADR-0039 (revocation propagation) and ADR-0038 (email throttle) refer to: ordinary performance caches fail open; security checks (the email throttle here, the distrusted-kid set in ADR-0039) fail closed.

### Consequences

* Good, because outbound failures are contained by one consistent, well-understood handler rather than a patchwork of per-site pipelines, and non-idempotent operations are never silently duplicated by a retry.
* Good, because separating rate-limiting (429) from load-shedding (503) means the service can protect its capacity without penalizing well-behaved callers, and can enforce fairness without pretending that is overload protection.
* Good, because a Redis outage degrades latency instead of breaking authentication, and the one security-relevant throttle stays enforced even then.
* Bad, because two overload controls plus a bulkhead and per-dependency timeouts are more configuration surface to tune and test than a single knob.
* Bad, because fail-open behavior must actually be load-tested to be trusted, which is real test effort, not a paper guarantee.
* Neutral, because the standard handler's defaults (timeouts, retry counts) are starting points that are tuned per dependency against the capacity model.

### Confirmation

* Load tests exercise the Redis-down path and confirm the distributed cache fails open (no 5xx, higher latency) while authentication continues.
* A test confirms the token-endpoint concurrency limiter returns 503 with `Retry-After` under overload, distinct from the 429 returned by the rate limiter.
* A test confirms retries do not fire on non-idempotent verbs.
* A test confirms the email anti-abuse throttle stays enforced (per-instance) when Redis is unavailable.

## Pros and Cons of the Options

### Hand-composed resilience pipelines per call site

* Good, because each site can be tuned in isolation.
* Bad, because policies stack unpredictably, drift apart across sites, and make it easy to retry a non-idempotent call; there is no single posture to reason about or test.

### A single inbound overload control for both quota and capacity

* Good, because it is one mechanism to configure.
* Bad, because quota/abuse limiting and instantaneous capacity protection are different problems: a quota limiter tuned for fairness will not shed a capacity spike in time, and a capacity limiter tuned to protect the instance will wrongly reject legitimate steady traffic. One control cannot serve both without failing one of them.

### Standardized posture (chosen)

* Good, because outbound uses one consistent handler, inbound uses the right control for each situation, and Redis degrades gracefully with a clear security carve-out.
* Bad, because it is more surface to tune and must be load-tested to be trusted.

## More Information

* This posture is recorded from the NFR/resiliency design (doc 11 §6, with the connection-pool and graceful-shutdown specifics folded into ADR-0018 and ADR-0031 respectively). The numeric thresholds (timeouts, concurrency limits, rate-limit windows) are tuned against the capacity model and are ratified with product/ops stakeholders.
* Related decisions: ADR-0003 (durable session store independent of Redis), ADR-0006 (Data Protection keyring stored independently of Redis), ADR-0018 (connection-pool sizing and fail-fast-to-503 on pool exhaustion), ADR-0031 (graceful shutdown and readiness), ADR-0037 (PostgreSQL as the durable store), ADR-0038 (the email anti-abuse throttle whose fail-closed degradation is the carve-out in D), and ADR-0039 (the per-path revocation model that follows the same fail-open/fail-closed split).
* Authored in this repository in 2026-07 to record the settled resiliency posture as an ADR; neutral libraries and platforms (Polly, ASP.NET Core rate limiting, Redis, PostgreSQL) are named factually for identification only, and no commercial competitor is named.
