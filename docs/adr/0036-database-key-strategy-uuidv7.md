---
status: "accepted"
stack-record: true
date: 2026-07-03
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: PostgreSQL 18 / .NET 10 / Npgsql 10 / EF Core 10 capabilities and benchmarks (evidence V15)
informed: all contributors, via this repository
---

# Use UUIDv7 as the clustered primary key for every entity, with one deliberate bigint exception

## Context and Problem Statement

Every entity needs a primary-key type, and the choice is a foundational, cross-cutting data-layer decision. The classic options each have a sharp trade-off: a random v4 GUID is globally unique but causes severe clustered-index fragmentation and page splits on insert; a database identity/sequence `bigint` is compact and sequential but is not globally unique and is enumerable. Nami is multi-tenant with Pool and Silo tiers (ADR-0001), where a tenant can move between Pool and Silo (ADR-0017), so keys must not collide across databases; and it runs on PostgreSQL 18 (which added a native `uuidv7()`) and .NET 10 (which has `Guid.CreateVersion7()` since .NET 9). What primary-key strategy should every table use? This ADR records a decision made on 2026-07-03 that until now lived only in the database-design documents, even though other ADRs already assume it.

## Decision Drivers

* Index locality and insert performance: avoid the v4-GUID clustered-index fragmentation and page-split cost.
* Global uniqueness: keys must be safe for Silo isolation, tenant merge, and a Pool↔Silo move (ADR-0001, ADR-0017) with no cross-database collision.
* Non-enumerable identifiers: do not leak row counts or the next id.
* One consistent key convention across the whole schema, including the OpenIddict entity set.
* Fit the pinned stack natively: PostgreSQL 18, .NET 10, Npgsql 10, EF Core 10.

## Considered Options

* A random v4 GUID
* A database identity/sequence `bigint`
* UUIDv7 (a time-ordered UUID)

## Decision Outcome

Chosen option: "UUIDv7 for the clustered primary key of every entity", represented as a .NET `Guid`, because it is globally unique like a v4 GUID but time-ordered, so it preserves index locality and avoids v4's fragmentation, and it is generated natively by both PostgreSQL 18 and .NET.

**The time-ordering is coarse, and must never be used as a sort key where exact order matters.** `Guid.CreateVersion7()` in .NET is **not monotonic within a single millisecond**: the sub-millisecond bits are random rather than a counter, so values created in the same millisecond sort in effectively arbitrary order under `ORDER BY id`. This was found empirically, not assumed: a v2 spike's per-subject ordering test failed on exactly this at high enqueue rate, and the fix was to add a separate monotonic `seq bigint GENERATED ALWAYS AS IDENTITY` column and order by that (matching AWS's guidance to use a timestamp **and** a sequence number). The property this ADR relies on is **index locality**, which only needs coarse time-clustering, and that is unaffected. So the rule is: UUIDv7 stays the primary key and the deduplication/idempotency key, and anything requiring a strict order carries its own sequence column (ADR-0071's outbox is the first such case).

Fixed parameters of the decision:

* **Every entity's clustered primary key is UUIDv7**, represented as a `Guid`.
* **Generation** is either PostgreSQL 18's native `uuidv7()` at the database or .NET's `Guid.CreateVersion7()` in the application; Npgsql 10 translates `Guid.CreateVersion7()` to the PG18 native function when `SetPostgresVersion(18, 0)` is configured (verified, V15).
* **The OpenIddict key type is overridden from its default `string` to `Guid`**, so the OpenIddict entity set shares the same key convention; OpenIddict's `Guid` key support was verified. **Use the custom-entity overload, `UseOpenIddict<TenantApplication, TenantAuthorization, TenantScope, TenantToken, Guid>()`, together with `ReplaceDefaultEntities`.** This bullet previously wrote it as `UseOpenIddict<Guid>()`, and that form is dangerous precisely because it **compiles and runs**: the single-type-argument overload exists and its own documentation says it registers the entity sets "using the **default** OpenIddict models and the specified key type". The default models have no `TenantId` column at all, so the entire Pool tenancy model would be absent with no compile error and no exception, just every tenant sharing one client set. The mandatory part is therefore the custom entities plus `ReplaceDefaultEntities`; the *position* is **not** a library constraint, since the five-type-argument overload is published on both `ModelBuilder` and `DbContextOptionsBuilder`. Getting this wrong in the direction the old text invited would be the worst outcome: an implementer "correcting" the position while keeping the `<Guid>` overload still loses tenancy silently. **Read at `OpenIddict.EntityFrameworkCore` 7.4.0**, the only version in the local package cache; the pin is `[7.6.0]` (ADR-0021 parameter A), so re-confirm on the pinned package at M1. The data design puts it in `OnModelCreating` because that is where `.IsMultiTenant()`, the composite unique index and the soft-delete filter also live.
* **The optimistic-concurrency token is `xmin`** (the PostgreSQL system column), not a separate rowversion (rowversion is SQL-Server-only and the engine is PostgreSQL).
* **One deliberate exception: `ServerSideSessions.Id` is a `bigint` identity**: it is an internal surrogate that is never exposed (clients reference the random `sid`/`Key` string), is not tenant-scoped, and is never merged or moved across Silo, so UUIDv7's two benefits (being non-enumerable externally and globally unique for merge) do not apply; on this high-churn table (login/logout/expire/cleanup) an 8-byte `bigint` is cheaper than a 16-byte UUID. This is consistent with how identity servers commonly key the server-side session (an int/bigint identity). Every other entity uses UUIDv7.

### Consequences

* Good, because it near-eliminates the v4-GUID clustered-index fragmentation (the cited benchmark, evidence V15, is a 50-million-row insert of roughly 1.8 minutes for v7 versus roughly 20 minutes for v4, with an index about 25% smaller), keeps keys globally unique so Silo isolation, tenant merge, and a Pool↔Silo move are safe (ADR-0001, ADR-0017), keeps identifiers non-enumerable, and gives one consistent key convention across the whole schema including OpenIddict.
* Bad, because a UUID is 16 bytes versus 8 for a `bigint` (accepted for the simplicity and consistency), and a UUIDv7 embeds its creation timestamp, so anyone holding an id can read the row's creation time (judged harmless here, since nothing sensitive is derivable from it).
* This depends on PostgreSQL 18 (native `uuidv7()`) and .NET 9+/Npgsql 10 (`Guid.CreateVersion7()`), which are the pinned stack; dropping below those would lose native generation, and it is treated as a forward-only feature per ADR-0030.

### Confirmation

* Verified (V15) that Npgsql 10 translates `Guid.CreateVersion7()` to the PG18 native function when the Postgres version is set, and that OpenIddict supports a `Guid` key type.
* A schema/convention test asserts that every entity's key is a `Guid` (UUIDv7) except the documented `ServerSideSessions.Id` `bigint`, which is asserted as the single intentional exception.
* The fragmentation and throughput claims are backed by well-documented analysis of clustered-GUID fragmentation and the cited PG18 insert benchmark (V15).

## Pros and Cons of the Options

### A random v4 GUID

* Good, because it is globally unique and can be generated by the application without a round-trip.
* Bad, because a random value as a clustered key causes severe index fragmentation and page splits on insert and a larger, more-churned index.

### A database identity/sequence `bigint`

* Good, because it is compact (8 bytes), sequential, and gives fast inserts.
* Bad, because it is not globally unique (it collides across Silo databases and is unsafe for tenant merge/move) and it is enumerable (it leaks counts and the next id). It is kept only for the internal session surrogate, where those downsides do not apply.

### UUIDv7 (chosen)

* Good, because it is globally unique and time-ordered (so it keeps index locality) and does not leak counts, and it is generated natively by both PostgreSQL 18 and .NET.
* Bad, because it is 16 bytes and embeds a creation timestamp.

## More Information

* **Updated 2026-08-08: the key-type bullet names the current pin, and it now cites the ADR that owns
  the pin.** Seed S-002 moved the engine to `[7.6.0]`, so the clause reading "the pin is 7.5.0" was
  stale. Two things changed rather than one, and the second was not in the seed that asked for the
  first.
  * **The citation moved from ADR-0061 to ADR-0021 parameter A.** The old clause attributed a
    three-part version to ADR-0061, and that ADR's stack table writes versions to major or minor only:
    read 2026-08-08, its row says "OpenIddict 7.6" and has never carried a patch number. So the
    pointer resolved to a real file that did not hold the claim, which is the citation shape this
    repository treats as the dangerous one. ADR-0021 parameter A owns the exact pin including its
    bracket form.
  * **The 7.4.0 read is confirmed rather than edited, and the gap it names has widened.** The sentence
    says the mapping was read at `OpenIddict.EntityFrameworkCore` 7.4.0, "the only version in the local
    package cache". Checked 2026-08-08, `~/.nuget/packages/openiddict.entityframeworkcore/` still holds
    only `7.4.0`, so that measurement is still true and keeps its wording. What changed is the distance:
    the read is now two minor versions behind the pin rather than one. The "re-confirm on the pinned
    package at M1" instruction is therefore more owed, not less, and the moment it becomes possible is
    when the first `PackageReference` restores the engine, which is seed S-008. Seed S-006 is the
    related decision, because the offline source tree that could have answered this without a restore
    no longer matches the pin.
  * **One defect in this ADR was deliberately left to the next commit**, and it is the entry below
    this one.
* **Corrected 2026-08-08: the Related-decisions bullet no longer calls ADR-0018 "the pooled
  DbContext".** [ADR-0018](0018-dbcontext-pooling-for-pool-mode.md) is titled "Register the Pool-mode
  OpenIddict DbContext **non-pooled** in v1, with pooled-plus-mutable deferred", and `0018:35` chooses
  the non-pooled scoped `AddDbContext` for v1. So this ADR labelled its sibling by the option that
  sibling declined. The bullet now names per-context pooling with the tenant-scoped context non-pooled,
  which is the framing [ADR-0061](0061-technology-stack-of-record.md) uses.
  * **A blanket "non-pooled" would have been wrong in the other direction.** Pooling is used, per
    context. Read 2026-08-08, `docs/design/02-data.md:55-59` pools `IdentityDbContext`,
    `DataProtectionDbContext`, and `ControlPlaneDbContext` and leaves the two tenant-scoped contexts
    unpooled, and `docs/design/02-data.md:1174` says `AddDbContextPool` "is deliberately not used for
    `OpenIddictDbContext` or for `ControlPlaneTenantDbContext`".
  * **Why an inverted word costs more here than a wrong version.** ADR-0018 exists because spike A-4
    test T7, run 2026-07-06, found that "naive pooled reuse leaked the tenant across requests, including
    through OpenIddict's internal `SaveChanges`" (`0018:62`). A sibling ADR calling it the pooled
    context points an implementer at the registration that leaked tenants.
  * **Six instances of one defect are now known, and this is the fourth to be repaired.** `0061:145`
    and `architecture/07-container-view.md:288-290` record the first two, both 2026-07-25, and
    `0061:118` predicted the rest. ADR-0066's `Factory` entry was the third, removed in the commit
    before this one. This bullet is the fourth. Two remain:
    [ADR-0033](0033-key-scope-isolation-model.md)'s Related-decisions bullet, found by running seed
    S-026's own verification rather than by reading, and
    `architecture/03-drivers-and-constraints.md:116`, which seed S-024 owns because it is in the
    architecture layer rather than in an ADR. **The count is stated because it kept being wrong**: a
    draft of this bullet said five.
  * **What is not a defect, enumerated so nobody sweeps the word.** Searched 2026-08-08, about thirty
    lines across `docs/` name ADR-0018 near the word pool and only the two above are wrong. The rest
    are accurate in three distinct ways: they use "pooling" as the **name of the ADR's subject**
    (`architecture/07-container-view.md:252`, `08-component-view.md:393`,
    `24-glossary.md:93`, `docs/design/01-foundations.md:572`, `docs/design/02-data.md:1371`); they say
    **non-pooled** or "pooled-plus-mutable ... post-v1" (`architecture/21-performance-scalability.md:152`,
    `23-risks-and-technical-debt.md:81`, `docs/design/01-foundations.md:377`,
    `docs/design/02-data.md:28`); or they mean the **PgBouncer connection pooler**, a different subject
    entirely (`0074:44`, `architecture/10-deployment-infrastructure.md:91` and `:248`,
    `22-reliability-backup-dr.md:31`).
* Decided 2026-07-03 (evidence V15). This ADR records a decision that until now lived only in the database-design documents, which other ADRs already assume: ADR-0025 references PostgreSQL 18's `uuidv7()` in dev/test to match production, and ADR-0030 lists `Guid.CreateVersion7()` as a forward-only .NET feature.
* Related decisions: ADR-0001 (Pool/Silo, where global uniqueness enables Silo and merge safety), ADR-0017 (the tenant Pool↔Silo move that relies on non-colliding keys), ADR-0018 (per-context `DbContext` pooling on the same PostgreSQL/EF stack, with the tenant-scoped context non-pooled in v1), ADR-0025 (PostgreSQL 18 in dev/test matching production for `uuidv7()`), and ADR-0030 (UUIDv7 generation as a forward-only .NET-version feature). This is distinct from ADR-0033, which is about signing/encryption key-scope, not database primary keys. See ADR-0059 for the entity-versus-value-object distinction that builds on this identity model.
* Authored in this repository in 2026-07 to record the settled database-design decision as an ADR; a competitor reference for the session-surrogate exception and a named fragmentation-analysis author were generalized, and PostgreSQL, Npgsql, EF Core, and OpenIddict are retained as the project's stack.
