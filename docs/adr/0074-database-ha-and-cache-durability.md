---
status: "accepted"
stack-record: true
date: 2026-07-25
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops (the concrete failover mechanism, whether Redis durability is enabled, and the read-replica trigger threshold are theirs to ratify); ADR-0006 (per-store recovery objectives and continuous monitoring), ADR-0037 (the engine), ADR-0018 (connection pooling), ADR-0039 and ADR-0014 (the two fail-closed caches), ADR-0041 (the availability target), ADR-0031 (disposability)
informed: all contributors, via this repository
---

# Adopt a primary-plus-standby PostgreSQL topology with automatic failover, keep read replicas an optional non-v1 lever, and never depend on Redis durability

## Context and Problem Statement

Identity is a hard dependency of every application that trusts it, so ADR-0041 holds Nami to a higher availability target than the services it serves. ADR-0037 fixes the engine and ADR-0006 binds recovery objectives **per store** and requires continuous monitoring, but **no ADR records the database topology itself**: whether there is a standby, how failover happens, whether the application may read from a replica, or what happens to the Redis-backed caches when Redis restarts.

That gap was concrete rather than theoretical. The topology was already being asserted in the architecture layer (a primary with streaming replication and automatic failover) with no decision behind it, which is exactly the "silent gap" this project's conventions reject. Two further questions were unanswered and both are easy to get wrong:

* **May the application read from the standby?** Treating a failover standby as a read-scaling replica is a common and damaging conflation: it changes the consistency contract without anyone deciding to.
* **Does anything depend on Redis surviving a restart?** ADR-0039 and ADR-0014 each define a **fail-closed** cache, and the detailed designs note that one of them is rebuildable from the database and the other has no durable source at all. Whether Redis is configured with persistence therefore has a different consequence for each, and the difference had never been written down as a decision.

## Decision Drivers

* The availability target of ADR-0041 cannot be met by a single database instance, so a standby and a failover path are required rather than optional.
* Consistency must not degrade by accident: a read served from a lagging replica is a correctness question, not a performance one.
* Nami is cloud-agnostic (ADR-0006), so the topology must be expressible on managed services and on self-managed infrastructure without mandating a product.
* Recovery objectives already exist per store (ADR-0006); this decision must complement them, not restate or contradict them.
* A cache whose loss changes a **security** property must be distinguished from one whose loss changes only latency.
* An optional scale lever should be recorded as optional, so that adopting it later is a decision rather than a drift.

## Considered Options

* A single primary with backups only, and no standby
* A primary with a streaming-replication standby and automatic failover, with read replicas as a separate optional lever
* A primary with a standby that the application also reads from, for read scaling
* A multi-primary or active-active database topology

## Decision Outcome

Chosen option: **a primary with a streaming-replication standby and automatic failover, with read replicas kept as a separate optional lever**. The fixed parameters are:

* **A. Topology.** A **primary** serving reads and writes, a **streaming-replication standby**, **automatic failover** that promotes the standby, and **point-in-time recovery** through write-ahead-log archiving. The failover mechanism is deliberately **not** pinned to a product: a self-managed cluster manager or a managed PostgreSQL high-availability offering both satisfy this, and which one is an Ops choice. Nodes are spread so that losing one availability zone does not lose the service, and the application tier stays stateless with no session affinity (ADR-0031, ADR-0072).
* **B. The standby is for failover, not for read scaling.** The application talks to the **primary**. This is stated as an invariant because the failure mode of violating it is silent: reads served from a lagging standby return stale data with no error, and an administrative change made through the Admin API would appear not to have taken effect.
* **C. Read replicas are an optional lever, explicitly not v1.** Routing read-heavy configuration and discovery reads to a read-only replica is a **scale lever applied only when a measured read-throughput bottleneck exists**, never as a default. Adopting it requires accepting a replication-lag caveat on configuration reads and deciding how it interacts with the ADR-0039 30-second configuration-propagation bound, which is a decision to be made then, not assumed now.
* **D. The connection pooler, where used, is itself highly available.** ADR-0018 makes transaction-mode pooling conditional on Silo scale. Where it is used it sits **on the hot path**, so a single instance is a single point of failure: at least two instances with failover, and its failover is exercised in the recovery drill rather than assumed.
* **E. Redis durability is an operator option that the application never depends on.** This is the parameter with the most consequence, and it is stated per cache rather than globally, because "enable Redis persistence" is not one decision:

  | Cache | Durable source to rebuild from | Consequence of losing it on a Redis restart |
  |---|---|---|
  | Output cache (discovery, JWKS) | Yes, recomputed | A cache miss. No correctness impact |
  | Configuration cache (clients, scopes) | Yes, PostgreSQL | A cache miss and one propagation interval of extra latency. No correctness impact |
  | Distrusted-key set (break-glass) | **Yes**, the revocation timestamps on the key store | Rebuildable, therefore no correctness impact **provided the rebuild actually happens**: see the invariant below |
  | DPoP proof-replay set | **No.** It is the authoritative store and there is nothing to read through to | **A replay window** bounded by the proof's cache lifetime, during which a captured proof can be replayed once |

  Two consequences follow, and both are decisions rather than observations:

  * **Invariant: an empty distrusted-key set is never evidence that nothing is revoked.** The set is rebuilt from the key store on startup and on a miss, and a lookup that finds the set absent or empty must fall back to the store rather than concluding "not revoked". Without this, a Redis restart silently re-trusts a key that break-glass had ejected, which would defeat ADR-0007's five-minute ejection guarantee at exactly the wrong moment. ADR-0039 already specifies fail-closed behaviour when Redis is **unreachable**; this invariant covers the different case where Redis is reachable but has forgotten.
  * **Accepted risk, stated with its bound: without Redis durability, a Redis restart opens a proof-replay window** equal to the remaining lifetime of proofs already seen (the proof validity plus twice the applicable clock skew, per the advanced-flows design). Enabling Redis durability closes that window, at the cost of a synchronous write-durability step on the proof-validation path for DPoP-bound tokens only. **Whether to enable it is an Ops decision**, made with that trade-off explicit rather than by default. The core token path is unaffected either way, because it takes no mandatory Redis hit (ADR-0039).

* **F. Monitoring and drills come from ADR-0006, with two additions.** ADR-0006 already requires continuous monitoring of archiving lag, last-successful-backup age, and replication lag against each store's bound objective. This ADR adds that **a disconnected standby is itself an alert condition** (a failover would then lose data), and that the recovery drill includes a **failover drill**, covering both the database promotion and the connection pooler where one is used.

### Consequences

* Good, because the availability target now has a topology behind it, and the topology has a decision record rather than living only in a diagram.
* Good, because the standby-is-not-a-replica invariant closes a silent-staleness failure mode before any code exists to commit it.
* Good, because the read-replica lever stays available without being a default, so adopting it later is a deliberate decision with a named caveat.
* Good, because Redis durability is now a per-cache question with one security consequence quantified, rather than a single yes-or-no whose blast radius nobody had mapped.
* Good, because the distrusted-key rebuild invariant removes a real re-trust window that neither ADR-0007 nor ADR-0039 had closed, since each addressed a different Redis failure mode.
* Bad, because a standby doubles the database footprint for capacity that is idle in normal operation; accepted, as the alternative is not meeting the availability target.
* Bad, because reads all land on the primary, so a read-heavy deployment may hit a bottleneck that the optional lever exists to relieve; accepted, and measured rather than pre-empted.
* Neutral, because no failover product is mandated, which keeps the decision cloud-agnostic at the cost of the topology being verifiable only per deployment.

### Confirmation

* A failover drill promotes the standby within the operational database's recovery-time objective, and the service resumes without manual application changes.
* A test asserts the application's connection configuration targets the primary, so a replica endpoint cannot be introduced for ordinary reads without the test failing.
* A drill flushes Redis while break-glass has a key ejected, and the ejected key is **still rejected**, proving the rebuild path rather than the persistence setting.
* Alerts fire on archiving lag, backup age, replication lag, and a disconnected standby.
* Where a connection pooler is deployed, its failover is drilled and the service survives losing one instance.

## Pros and Cons of the Options

### Single primary with backups only

* Good, because it is the simplest and cheapest topology, and point-in-time recovery still bounds data loss.
* Bad, because recovery means restoring rather than promoting, so the recovery time is far outside the availability target for a service on the critical path of every login.

### Primary plus failover standby, read replicas optional (chosen)

* Good, because it meets the availability target, keeps one consistency contract, and leaves a scale lever available without turning it on.
* Bad, because the standby is idle capacity and all reads land on the primary.

### Primary plus standby that also serves reads

* Good, because it would relieve read pressure at no extra infrastructure cost.
* Bad, because it silently changes the consistency contract: a configuration change would read back stale, and the failure is invisible rather than an error. Conflating a failover target with a read replica is the specific mistake parameter B exists to prevent.

### Multi-primary or active-active

* Good, because it would offer the highest write availability.
* Bad, because it introduces write-conflict resolution into an identity store where uniqueness and ordering are load-bearing, for a write volume that does not require it. Far more failure modes than it removes.

## More Information

* **Ratification (Ops, before production).** The concrete failover mechanism, whether Redis durability is enabled given the quantified replay-window trade-off, the read-replica trigger threshold if that lever is ever adopted, and the drill cadence for failover. Tracked in the [Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).
* **Why this ADR exists.** The topology was found asserted in the architecture layer with no owning decision while reconciling that layer on 2026-07-25, the third such ownerless claim found that way after the edge posture (ADR-0073) and the user-interface rendering stack (ADR-0072). The design corpus recorded the topology as a one-line entry in its non-functional-requirements table and elaborated it in its reliability view, including the phrase "Redis with persistence"; that phrase is the reason parameter E is written per cache, because this repository's own detailed designs state that the proof-replay set is authoritative with no durable source while the distrusted-key set is rebuildable, which makes a single global persistence answer wrong in one direction or the other.
* **Deliberately not decided here.** Per-store recovery objectives and their numeric bounds stay with ADR-0006, which also owns the continuous-monitoring requirement and its own ratification. Backup mechanics, the restore-both requirement for keys and keyring, and the crypto-shred interaction with backups belong to ADR-0006, ADR-0012, and ADR-0016. Application-tier availability measures (readiness gating, graceful shutdown, zone spread) belong to ADR-0031 and the deployment view.
* **Related decisions:** ADR-0037 (the engine and its row-level security, which the topology must preserve across a failover), ADR-0006 (per-store recovery objectives, the cloud-agnostic posture, and continuous monitoring), ADR-0018 (connection pooling, per-tenant pool sizing, and the conditional use of a transaction-mode pooler), ADR-0039 (the per-path freshness model, the configuration-propagation bound, the distrusted-key set, and the no-mandatory-Redis property of the core token path), ADR-0014 (the DPoP proof-replay cache), ADR-0007 (the five-minute compromised-key ejection this ADR's rebuild invariant protects), ADR-0041 (the availability target that motivates the topology), ADR-0031 (stateless disposability), ADR-0040 (Redis as an accelerator that fails open, and the load-shed path when the connection pool saturates), ADR-0072 (the no-session-affinity invariant the stateless tier depends on), and ADR-0017 (per-tenant migration, which fans out across whatever topology this fixes).
* Authored 2026-07-25 for this repository. Failover mechanisms and cloud offerings are described generically; no product is mandated or endorsed.
