---
status: "accepted"
date: 2026-07-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: EF Core advanced-performance guidance (.NET 10), the Finbuckle maintainer note on pooling (#375), spike A-4 and verification V08
informed: all contributors, via this repository
---

# Pool the Pool-mode OpenIddict DbContext with a per-request mutable TenantId, with a non-pooled fallback

## Context and Problem Statement

Two similarly named things must not be confused (a distinction that has caused confusion before):

* **Pool MODE** is a tenancy isolation model (a shared database plus a `TenantId` column plus a global query filter). It is ADR-0001's design and is **not** in scope here.
* **DbContext pooling** (`AddDbContextPool` / `AddPooledDbContextFactory`) is an EF Core performance feature that reuses context instances to cut allocation. **This ADR is only about DbContext pooling.**

The 10k concurrent-user target makes the token endpoint a hot path, and DbContext pooling cuts per-request allocation and initialization. But pooling plus multi-tenancy has a trap: when EF Core returns a context to the pool it resets only the state it owns (the `ChangeTracker`), **not** a custom field such as `TenantId`. If the tenant is not re-set on every request, a previous tenant leaks into the next request — a cross-tenant security bug. Finbuckle by default captures the tenant immutably at construction, which is safe but incompatible with pooling, and its maintainer warns to be careful when pooling. How should the tenant-scoped OpenIddict context be registered?

## Decision Drivers

* The 10k-CCU target makes the token endpoint hot, so cutting per-request allocation matters.
* Multi-tenant safety is non-negotiable: a stale pooled context must never leak one tenant into another request.
* The choice should be a DI-only concern, cheap to reverse in either direction.

## Considered Options

* **A. Pooled with a mutable `TenantId`**, via the official pattern: `AddPooledDbContextFactory<T>` (singleton) plus a scoped `IDbContextFactory<T>` that sets `context.TenantId = currentTenant` on every `CreateDbContext()` (the tenant coming from a scoped multi-tenant context accessor). Because the factory re-sets it each request, the value is always correct even though the pool does not reset it; a `ResetState()` on a resettable service can additionally clear the field.
* **B. Non-pooled `AddDbContext`** (scoped): the tenant is captured at construction and is immutable — the safest and simplest option, but it forgoes the pooling performance.
* **C. Pooled with no fallback**: commit to pooling at all costs, including hacks.

## Decision Outcome

Chosen: **Option A as the target, validated by spike A-4 (test T7), with Option B as the fallback if A proves fragile.** Option C is rejected as high-risk.

Fixed parameters of the decision:

* The pattern is `AddPooledDbContextFactory` plus a scoped factory that sets `TenantId` per request (the canonical pattern), applied to the tenant-scoped OpenIddict context in Pool mode.
* **Per-context matrix**: the global contexts (Identity, Data Protection, control plane) are pooled (a fixed connection, safe); **Silo** contexts are **not** pooled (their connection string varies, which is incompatible with `AddDbContextPool`); the **Pool-mode OpenIddict** context is pooled with a mutable `TenantId` (this decision).
* Spike A-4's test T7 is the decision gate: if pooled-plus-mutable isolates correctly under instance reuse and concurrency, ship A; if it is fragile, fall back to B.

How fragility is checked (so the implementation is fully specified): "fragile" means a stale or leaked tenant. Test T7 forces reuse of a pooled instance, interleaves tenant A then B plus concurrency, and asserts no cross-tenant read or stamp. Any of these is a fragility flag: (1) an instance now serving tenant B still carries A's `TenantId` (the per-request set or `ResetState()` did not take); (2) a named query filter reads the old tenant value (it must read the mutable property at query time, not capture a closure); (3) OpenIddict's internal `SaveChanges` (redeem/revoke/prune) stamps the wrong or missing tenant; (4) safety requires a hack outside the scoped-factory pattern. A negative control accompanies it: removing the per-request set must produce a visible leak, proving the guard is what protects.

The fallback from A to B changes only the DI registration at the composition root (the pooled factory plus scoped factory becomes a scoped `AddDbContext<T>` with the tenant captured at construction). It changes no schema, migration, model, query filter, wire protocol, API, or token format; pooling is purely a runtime/DI/performance concern, contained in `Program.cs` and cheap to reverse either way.

### Consequences

* Good, because it achieves pooling performance for the 10k-CCU target if T7 passes, and if it does not, the fallback is a trivial DI change that is also **safer** (a non-pooled, immutable tenant eliminates the whole leak class).
* Good, because login is unaffected either way: the tenant resolves per request (by host/path) and the store query filters by tenant. The only login risk is the pooled case with a stale tenant (cross-tenant), which T7 guards and the fallback removes, so the fallback makes login safer and never breaks it.
* Bad, because a non-pooled context costs extra allocation and GC; but the token endpoint is dominated by crypto and database I/O, so the real impact is modest.
* Worst case is shipping non-pooled and losing a modest performance optimization; no fallback scenario breaks correctness or login, so Option A has almost no downside.

### Confirmation

* EF Core advanced-performance guidance (.NET 10) verified: pooling resets only the `ChangeTracker`, not a custom field; the `AddPooledDbContextFactory`-plus-scoped-factory pattern sets `TenantId`; the microbenchmark is roughly 350us versus 701us; and network/database I/O usually dominates EF time (efcore issue #14625).
* The Finbuckle maintainer note (#375): pooling "could work" for a same-connection dynamic filter via a computed/mutable tenant, with a "be careful" on mutability.
* Verify-before-build: run spike A-4 / test T7 to validate the pooled-plus-mutable composition against OpenIddict's internal `SaveChanges` on the pinned OpenIddict 7.5.0, Finbuckle 10.1.1, and EF Core 10.

## Pros and Cons of the Options

### A. Pooled with a mutable `TenantId` (chosen target)

* Good, because it keeps the pooling performance on the hot path while staying correct through a per-request re-set.
* Bad, because correctness depends on the per-request set always running, which is why T7 gates it and B stands ready.

### B. Non-pooled scoped context (chosen fallback)

* Good, because an immutable per-construction tenant is the safest and simplest option and eliminates the leak class.
* Bad, because it forgoes the pooling allocation savings.

### C. Pooled with no fallback

* Good, because it would guarantee the performance path.
* Bad, because committing to pooling regardless of the T7 result invites hacks and a cross-tenant risk; rejected.

## More Information

* Original decision: 2026-07-01.
* The terminology distinction at the top is load-bearing: this ADR concerns only the EF Core DbContext-pooling performance feature and does not touch the Pool-mode isolation model of ADR-0001.
* Related decisions: ADR-0001 (multi-tenant isolation and Pool mode).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. Framework and library citations (EF Core, Finbuckle, the pooled-factory pattern) are retained as neutral technical precedent.
