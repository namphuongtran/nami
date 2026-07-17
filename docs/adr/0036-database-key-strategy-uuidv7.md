---
status: "accepted"
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

Fixed parameters of the decision:

* **Every entity's clustered primary key is UUIDv7**, represented as a `Guid`.
* **Generation** is either PostgreSQL 18's native `uuidv7()` at the database or .NET's `Guid.CreateVersion7()` in the application; Npgsql 10 translates `Guid.CreateVersion7()` to the PG18 native function when `SetPostgresVersion(18, 0)` is configured (verified, V15).
* **The OpenIddict key type is overridden from its default `string` to `Guid`** (via `UseOpenIddict<Guid>()` / `ReplaceDefaultEntities`), so the OpenIddict entity set shares the same key convention; OpenIddict's `Guid` key support was verified.
* **The optimistic-concurrency token is `xmin`** (the PostgreSQL system column), not a separate rowversion (rowversion is SQL-Server-only and the engine is PostgreSQL).
* **One deliberate exception — `ServerSideSessions.Id` is a `bigint` identity**: it is an internal surrogate that is never exposed (clients reference the random `sid`/`Key` string), is not tenant-scoped, and is never merged or moved across Silo, so UUIDv7's two benefits (being non-enumerable externally and globally unique for merge) do not apply; on this high-churn table (login/logout/expire/cleanup) an 8-byte `bigint` is cheaper than a 16-byte UUID. This is consistent with how identity servers commonly key the server-side session (an int/bigint identity). Every other entity uses UUIDv7.

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

* Decided 2026-07-03 (evidence V15). This ADR records a decision that until now lived only in the database-design documents, which other ADRs already assume: ADR-0025 references PostgreSQL 18's `uuidv7()` in dev/test to match production, and ADR-0030 lists `Guid.CreateVersion7()` as a forward-only .NET feature.
* Related decisions: ADR-0001 (Pool/Silo, where global uniqueness enables Silo and merge safety), ADR-0017 (the tenant Pool↔Silo move that relies on non-colliding keys), ADR-0018 (the pooled DbContext on the same PostgreSQL/EF stack), ADR-0025 (PostgreSQL 18 in dev/test matching production for `uuidv7()`), and ADR-0030 (UUIDv7 generation as a forward-only .NET-version feature). This is distinct from ADR-0033, which is about signing/encryption key-scope, not database primary keys.
* Authored in this repository in 2026-07 to record the settled database-design decision as an ADR; a competitor reference for the session-surrogate exception and a named fragmentation-analysis author were generalized, and PostgreSQL, Npgsql, EF Core, and OpenIddict are retained as the project's stack.
