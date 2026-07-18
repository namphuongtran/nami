---
status: "accepted"
stack-record: true
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: OpenFGA / SpiceDB (Zanzibar) consistency models; RFC 8693 (actor claim); the source-verification files
informed: all contributors, via this repository
---

# Compute authorization with a DB-first engine behind a consistency-carrying ICheckAccess port, swappable to ReBAC

## Context and Problem Statement

ADR-0010 fixed the delegated-admin *policy model*: explicit membership, scoped and capability-typed and time-bound and revocable grants, forbidden-cascade for dangerous capabilities, no global super-admin, and an evolution path toward a relationship-based access-control (ReBAC) engine. What it did not record is *how* an access decision is computed, where that computation lives, and how the design stays swappable to a dedicated ReBAC engine (OpenFGA, SpiceDB) without silently breaking correctness. Authorization is on the hot path of every protected operation and every admin action, so the engine, its consistency guarantees, and its failure behavior are load-bearing decisions in their own right.

The specific trap: if the authorization port omits consistency semantics, swapping from a strongly-consistent database to an eventually-consistent ReBAC engine would silently reintroduce the "new enemy" problem, where a check immediately after a revoke could still be served from stale relationship data and wrongly allow access.

## Decision Drivers

* Ship on the database without new infrastructure, while keeping a clean path to a dedicated ReBAC engine that does not touch call sites.
* The port contract must carry consistency, so a future DB-to-ReBAC swap cannot silently break read-after-write on a revoke.
* Authorization is on the hot path, so it needs a latency budget and a fail-closed timeout.
* There must be one canonical authorization seam, not two competing abstractions.

## Considered Options

* Adopt a ReBAC engine (OpenFGA/SpiceDB) from day one
* DB-only authorization with no engine-swap path
* A DB-first engine behind a single `ICheckAccess` port that carries consistency semantics and is swappable to ReBAC

## Decision Outcome

Chosen option: "A DB-first engine behind a consistency-carrying `ICheckAccess` port". The fixed parameters are:

* **A. One canonical port.** `ICheckAccess` is the project's single authorization seam (a hexagonal port, ADR-0024); it is the same seam other design notes referred to conceptually as a relationship checker, unified under this one name rather than built twice. The `DbCheckAccess` adapter is used now; `OpenFgaCheckAccess`/`SpiceDbCheckAccess` can replace it later without changing any call site.
* **B. DB-first computation.** Access is computed in the database with a recursive CTE or a closure table for tenant-ancestor lookup. The closure table trades a write on tenant reshape for fast index-only ancestor reads, which fits a tenant tree that changes rarely and is read often; the alternative recursive CTE is available where a closure table is not wanted. This all lives in the global control-plane context.
* **C. Consistency is in the contract (the LSP-leak fix).** The port carries a `ConsistencyRequirement`: `MinimizeLatency` (default, steady-state, some staleness tolerable), `AtLeastAsFresh` (at least as fresh as a supplied freshness token), and `FullyConsistent` (bypass any cache, mandatory on the check immediately after a revoke or grant write). The adapters map it: `DbCheckAccess` reads live from PostgreSQL (strong for all three, ADR-0037); a ReBAC adapter maps to that engine's minimize-latency / at-least-as-fresh (with a freshness token) / fully-consistent modes. The rule is that any check right after a revoke or grant write is `FullyConsistent`, which closes the new-enemy problem on any future engine swap.
* **D. Fail-closed with a latency budget.** An `AuthzCheckTimeout` (default 250ms, tunable via `IOptionsMonitor`) returns `AccessDecision.Deny` on timeout, forced by deny-by-default (ADR-0005). The v1 `DbCheckAccess` SLO is p95 < 30ms / p99 < 80ms (interim, ratify-pending), with a future ReBAC tier at p95 < 50ms / p99 < 150ms; a histogram of check duration, a timeout counter, and a CI gate on the DB-tier percentiles plus a timeout rate below 0.001 enforce it.
* **E. Dynamic policy provider that never caches a decision.** A single singleton `IAuthorizationPolicyProvider` parses a `Capability:` policy-name prefix into a `CapabilityRequirement` and validates it against the capability catalogue (an unknown capability yields a 403, closing an injection hole); the default provider remains as the backup for the fixed role/acr/actor policies. The deny-by-default, `FullyConsistent` decision stays in a scoped handler that calls `ICheckAccess`, so the framework's per-name policy cache never caches an access decision. The handler is registered scoped (it depends on the scoped `ICheckAccess`/tenant context), not singleton.
* **F. Revoke immediacy uses a live DB read, not a cache-bust channel.** A grant revoke is DB-direct (the authorization entry validation of ADR-0039, no backplane), and PostgreSQL's strong reads mean `DbCheckAccess` satisfies `FullyConsistent` directly. If a cross-request decision cache is ever added, its invalidation needs its own dedicated channel (or a short TTL as a ceiling), and must not reuse the grant-propagation path of ADR-0039.

### Consequences

* Good, because it ships on the database with no new infrastructure while keeping a clean, call-site-stable path to a ReBAC engine when scale or feature needs justify it.
* Good, because consistency is part of the contract, so the DB-to-ReBAC swap cannot silently reintroduce stale-after-revoke authorization.
* Good, because the fail-closed timeout and the SLO keep authorization bounded and safe on the hot path, and there is exactly one authorization seam.
* Bad, because the `ConsistencyRequirement` must be threaded correctly at every call site (every post-write check must pass `FullyConsistent`), which is enforced by a negative test rather than by the type system.
* Bad, because the closure table must be maintained on tenant reshape, and the ReBAC condition/caveat and consistency APIs are version-dependent seams to re-verify per engine upgrade (ADR-0021).
* Neutral, because the v1 authorization SLO numbers are interim and await Ops/Security ratification.

### Confirmation

* Negative tests (a high bar, since there is no reference implementation to lean on): a cross-tenant check denies; forbidden-cascade holds (a parent admin cannot delete a child tenant); an expired or revoked grant denies immediately; a check immediately after a revoke, run `FullyConsistent`, denies with no stale cache hit; and a timed-out check denies (fail-closed).
* A CI gate asserts the DB-tier p95/p99 and a timeout rate below 0.001.
* The single `ICheckAccess` seam is exercised by both the token-issuance and admin paths, proving there is not a second abstraction.

## Pros and Cons of the Options

### Adopt a ReBAC engine from day one

* Good, because it is the eventual target and avoids a later migration.
* Bad, because it adds a distributed dependency and operational surface before the scale that justifies it, and the relationship model can be expressed in the database first at far lower cost.

### DB-only authorization with no swap path

* Good, because it is the simplest to build.
* Bad, because it bakes the database into every call site, so moving to ReBAC later becomes a rewrite, and it invites a consistency-free contract that would break on that move.

### DB-first behind a consistency-carrying `ICheckAccess` port (chosen)

* Good, because it ships simply, swaps cleanly, and cannot silently lose read-after-write consistency across the swap.
* Bad, because the consistency parameter is a discipline enforced by tests, and closure maintenance plus version-dependent ReBAC seams are ongoing costs.

## More Information

* This engine and its `ICheckAccess` port are the canonical authorization seam of the project (doc 17 §0.6, §4, §5a, item 18). The v1 `DbCheckAccess` SLO and the check timeout are interim-accepted and await Ops/Security ratification.
* Related decisions: ADR-0010 (the delegated-admin policy model this engine enforces; ADR-0010 owns *what* is allowed, this ADR owns *how* it is computed), ADR-0005 (deny-by-default, which forces the fail-closed outcome), ADR-0021 (the version-dependent ReBAC condition/caveat and consistency seams re-verified per upgrade), ADR-0024 (the hexagonal port this is), ADR-0037 (PostgreSQL, whose strong reads let `DbCheckAccess` satisfy `FullyConsistent`), and ADR-0039 (grant revoke is DB-direct, and its cache-bust channel is kept separate from any future authorization cache).
* Authored in this repository in 2026-07 to record the settled authorization-engine decision as an ADR; neutral engines and standards (OpenFGA, SpiceDB, the Zanzibar consistency model, RFC 8693, OWASP guidance) are named factually for identification only, and no commercial competitor is named.
