---
status: "accepted"
stack-record: true
date: 2026-07-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: EF Core advanced-performance guidance (.NET 10), the Finbuckle maintainer note on pooling (#375), spike A-4 and verification V08
informed: all contributors, via this repository
---

# Register the Pool-mode OpenIddict DbContext non-pooled in v1, with pooled-plus-mutable deferred

## Context and Problem Statement

Two similarly named things must not be confused (a distinction that has caused confusion before):

* **Pool MODE** is a tenancy isolation model (a shared database plus a `TenantId` column plus a global query filter). It is ADR-0001's design and is **not** in scope here.
* **DbContext pooling** (`AddDbContextPool` / `AddPooledDbContextFactory`) is an EF Core performance feature that reuses context instances to cut allocation. **This ADR is only about DbContext pooling.**

The 10k concurrent-user target makes the token endpoint a hot path, and DbContext pooling cuts per-request allocation and initialization. But pooling plus multi-tenancy has a trap: when EF Core returns a context to the pool it resets only the state it owns (the `ChangeTracker`), **not** a custom field such as `TenantId`. If the tenant is not re-set on every request, a previous tenant leaks into the next request, which is a cross-tenant security bug. Finbuckle by default captures the tenant immutably at construction, which is safe but incompatible with pooling, and its maintainer warns to be careful when pooling. How should the tenant-scoped OpenIddict context be registered?

## Decision Drivers

* The 10k-CCU target makes the token endpoint hot, so cutting per-request allocation matters.
* Multi-tenant safety is non-negotiable: a stale pooled context must never leak one tenant into another request.
* The choice should be a DI-only concern, cheap to reverse in either direction.

## Considered Options

* **A. Pooled with a mutable `TenantId`**, via the official pattern: `AddPooledDbContextFactory<T>` (singleton) plus a scoped `IDbContextFactory<T>` that sets `context.TenantId = currentTenant` on every `CreateDbContext()` (the tenant coming from Finbuckle's scoped `IMultiTenantContextAccessor`). Because the factory re-sets it each request, the value is always correct even though the pool does not reset it; implementing `IResettableService.ResetState()` on the context can additionally clear the field when it returns to the pool.
* **B. Non-pooled `AddDbContext`** (scoped): the tenant is captured at construction and is immutable, the safest and simplest option, but it forgoes the pooling performance.
* **C. Pooled with no fallback**: commit to pooling at all costs, including hacks.

## Decision Outcome

Chosen: **Option B, the non-pooled scoped `AddDbContext`, is the active registration for v1.**

Option A was the original target with B held as its fallback, and spike A-4's test T7 was the gate between them. **T7 has since run and Option A failed it** (see Confirmation), so the fallback rule fired and B is the decision. Option A survives as **A-4b**, a deferred post-v1 performance option that must clear a fresh spike before adoption. Option C is rejected as high-risk.

Fixed parameters of the decision:

* **The v1 registration** of the tenant-scoped OpenIddict context in Pool mode is a scoped `AddDbContext<T>`, with the tenant captured immutably at construction. The A-4b pattern held for later (`AddPooledDbContextFactory` plus a scoped factory that sets `TenantId` on every `CreateDbContext()`) is the canonical Microsoft pattern and stays documented here so a future re-spike does not have to rediscover it.
* **Per-context matrix**, and the axis that decides it is **whether the context carries tenant-scoped tables**, not whether its connection string is fixed. Pooled: `IdentityDbContext`, `DataProtectionDbContext`, and `ControlPlaneDbContext` **restricted to its global tables**. Not pooled: **Silo** contexts (their connection string varies, which is incompatible with `AddDbContextPool`, a separate and lesser reason); the **Pool-mode OpenIddict** context (this decision), because T7 showed a pooled instance carries a stale `TenantId` into the next request; and `ControlPlaneTenantDbContext`, which exists precisely so the five tenant-scoped control-plane tables are not on that topology.
* **Connection-pool sizing (the ADO.NET/Npgsql connection pool, distinct from the DbContext pooling above).** The Npgsql pool is keyed per connection string, so each Silo tenant gets its own pool; at the default `Maximum Pool Size` of 100, `pool_size x instances x tenants` would exceed PostgreSQL's connection ceiling for many Silo tenants. The rule is to keep that product under the ceiling by lowering the per-tenant `Maximum Pool Size` (about 5-10) with `Minimum Pool Size` 0, and to bound the connection-acquisition timeout so pool exhaustion fails fast to a load-shed 503 (ADR-0040) rather than hanging threads. Where PgBouncer transaction-mode multiplexes Silo connections it must itself be highly available (at least two instances with failover, since it is on the hot path), and the per-request tenant variable must be `SET LOCAL` inside the transaction so it cannot leak across a multiplexed connection (ADR-0037).
* Spike A-4's test T7 was the decision gate: ship A if pooled-plus-mutable isolates correctly under instance reuse and concurrency, otherwise fall back to B. **Outcome (A-4 run 2026-07-06, verification records V17 and V24): naive pooled reuse leaked the tenant**, so B is active for v1 and A is deferred as A-4b, untested. An earlier draft of this ADR labelled the pattern "A-4-validated"; that was a pre-spike prediction and is corrected here.

How fragility is checked (so the implementation is fully specified): "fragile" means a stale or leaked tenant. Test T7 forces reuse of a pooled instance, interleaves tenant A then B plus concurrency, and asserts no cross-tenant read or stamp. Any of these is a fragility flag: (1) an instance now serving tenant B still carries A's `TenantId` (the per-request set or `ResetState()` did not take); (2) a named query filter reads the old tenant value (it must read the mutable property at query time, not capture a closure); (3) OpenIddict's internal `SaveChanges` (redeem/revoke/prune) stamps the wrong or missing tenant; (4) safety requires a hack outside the scoped-factory pattern. A negative control accompanies it: removing the per-request set must produce a visible leak, proving the guard is what protects. **T7 fired flag (1)**: an instance now serving tenant B still carried tenant A's `TenantId`. This test matrix is retained because it is the acceptance gate any future A-4b attempt must clear.

Moving between A and B changes only the DI registration at the composition root (a pooled factory plus scoped factory versus a scoped `AddDbContext<T>` with the tenant captured at construction). It changes no schema, migration, model, query filter, wire protocol, API, or token format; pooling is purely a runtime, DI, and performance concern, contained in `Program.cs` and cheap to reverse in either direction. That is why taking the fallback cost nothing structurally, and why revisiting A-4b later stays cheap.

### Consequences

* Good, because the outcome that actually landed is the **safer** one: a non-pooled, immutable tenant eliminates the whole stale-tenant leak class, and getting there was a one-line DI change.
* Good, because login is unaffected either way: the tenant resolves per request (by host/path) and the store query filters by tenant. The only login risk is the pooled case with a stale tenant (cross-tenant), which T7 guards and the fallback removes, so the fallback makes login safer and never breaks it.
* Bad, because a non-pooled context costs extra allocation and GC; but the token endpoint is dominated by crypto and database I/O, so the real impact is modest.
* The realized cost is exactly the anticipated worst case: v1 ships non-pooled and forgoes a modest performance optimization. No fallback scenario breaks correctness or login, which is why gating on T7 instead of committing to pooling was the right structure.
* Token issuance is a hot **write** path (roughly one row per token), so a self-contained JWT removes per-validation reads but not per-issuance writes; at the 10k-CCU target the bottleneck is write IOPS and the transaction log, so the operational store is capacity-planned by write IOPS and may sit on a higher-write tier than the read-heavy config store (UUIDv7 keys, ADR-0036, reduce the B-tree fragmentation on this path).

### Confirmation

* EF Core advanced-performance guidance (.NET 10) verified: pooling resets only the `ChangeTracker`, not a custom field; the `AddPooledDbContextFactory`-plus-scoped-factory pattern sets `TenantId`; the microbenchmark on a single-row local query is roughly 350us and 4.6 KB allocated with pooling versus 701us and 50 KB without; and network and database I/O usually dominate EF time (efcore issue #14625).
* The Finbuckle maintainer note (#375): pooling "could work" for a same-connection dynamic filter via a computed/mutable tenant, with a "be careful" on mutability.
* **Spike A-4 test T7 has run** (2026-07-06, verification records V17 and V24) against the pinned OpenIddict 7.5.0, Finbuckle 10.1.1, and EF Core 10: naive pooled reuse leaked the tenant across requests, including through OpenIddict's internal `SaveChanges`. Option A therefore did not clear its gate, Option B is active for v1, and pooled-plus-mutable is deferred as A-4b, untested. The detailed data-tier design carries Option B as the current registration.

## Pros and Cons of the Options

### A. Pooled with a mutable `TenantId` (deferred as A-4b)

* Good, because it would keep the pooling performance on the hot path while staying correct through a per-request re-set.
* Bad, because correctness depends on the per-request set always running. T7 gated this option and T7 failed it: a reused instance kept the previous tenant, so it is not shippable without a further spike.

### B. Non-pooled scoped context (chosen for v1)

* Good, because an immutable per-construction tenant is the safest and simplest option and eliminates the leak class.
* Bad, because it forgoes the pooling allocation savings.

### C. Pooled with no fallback

* Good, because it would guarantee the performance path.
* Bad, because committing to pooling regardless of the T7 result invites hacks and a cross-tenant risk; rejected.

## More Information

* Original decision 2026-07-01 (Option A as target, Option B as fallback); revised 2026-07-06 to Option B after spike A-4 test T7 failed Option A. Pooled-plus-mutable is tracked as A-4b, a post-v1 performance option.
* **Extended 2026-08-01: the per-context matrix was right about the OpenIddict context and wrong about the control plane, and wrong for an instructive reason.** It listed the control plane as pooled and justified it with "a fixed connection, safe". That is the wrong axis. A fixed connection string is what makes pooling *possible*; it is not what makes pooling *safe*. What makes it unsafe is a pooled instance capturing the ambient tenant once at construction, which is independent of the connection string, and `ControlPlaneDbContext` hosted five `.IsMultiTenant()` and RLS-isolated tables (`LogoutDeliveryOutbox`, `OutboxEmail`, `SuppressionEntry`, `TenantBranding`, `ProcessingRestriction`) while being labelled Global. So this ADR's own T7 finding applied to it and the label hid that, leaving RLS as the only layer, which is the single-layer posture this ADR exists to avoid. Resolved by splitting rather than by making the whole context non-pooled, so `ServerSideSessions` and `AuditLog` keep the pool: see design [02](../design/02-data.md) section 1. **This is a rare case where the previous text was internally consistent and still wrong**, which is why the axis is now stated explicitly instead of the conclusion.
* The terminology distinction at the top is load-bearing: this ADR concerns only the EF Core DbContext-pooling performance feature and does not touch the Pool-mode isolation model of ADR-0001.
* Related decisions: ADR-0001 (multi-tenant isolation and Pool mode), ADR-0037 (PostgreSQL, where the `SET LOCAL` tenant variable and PgBouncer transaction-mode constraint live), ADR-0040 (the load-shed 503 that a bounded connection-acquisition timeout fails into).
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25: the decision outcome was corrected from Option A to Option B to match the A-4/T7 spike result, and the Microsoft benchmark figures were completed. Framework and library citations (EF Core, Finbuckle, the pooled-factory pattern) are named as neutral technical precedent.
