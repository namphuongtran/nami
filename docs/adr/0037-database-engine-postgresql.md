---
status: "accepted"
date: 2026-07-03
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: PostgreSQL 18 / Npgsql 10 / EF Core 10 capabilities and benchmarks (evidence V15)
informed: all contributors, via this repository
---

# Use PostgreSQL as the sole database engine

## Context and Problem Statement

Every persistence decision in Nami rests on one choice that was never recorded as an ADR: which relational database engine the product targets. The engine is not a swappable detail. It fixes the row-level-security syntax that backs tenant isolation, the optimistic-concurrency mechanism, the primary-key generation path, the bootstrap-locking primitive, the tamper-evidence strategy for the audit log, and the connection-pooling topology. Several accepted ADRs already assume an engine without naming one: ADR-0001 relies on row-level security as an isolation backstop, ADR-0008 relies on an append-only tamper-evident audit table, ADR-0011/ADR-0012 rely on a distributed advisory lock for first-key seeding, ADR-0018 relies on a specific pooling model, and ADR-0036 mandates UUIDv7 keys generated natively by the database.

That last point is an anomaly worth stating plainly: the *consequence* (UUIDv7 keys) earned its own ADR (ADR-0036) while the foundational engine choice it depends on had none. Which engine does Nami support, and is it one engine or a portable multi-engine abstraction?

## Decision Drivers

* The engine must provide the isolation, concurrency, and integrity primitives the accepted ADRs already assume, rather than forcing those ADRs to be rewritten.
* Native, time-ordered UUID generation is required by ADR-0036.
* A tenant-isolation backstop below the application (row-level security) must be available and enforceable even against bulk and raw-SQL write paths.
* The engine must run everywhere the product must run: on-premises, local developer machines, and any managed cloud, with no proprietary-cloud lock-in (ADR-0006).
* Licensing must align with the project's OSS license-freedom rationale (no per-core commercial database licensing imposed on adopters).
* One authoritative engine keeps the schema, migrations, and tests single-flavored, rather than paying to test two SQL dialects everywhere.

## Considered Options

* PostgreSQL as the single supported engine
* Microsoft SQL Server as the single supported engine
* A portable, engine-agnostic data layer supporting both

## Decision Outcome

Chosen option: "PostgreSQL as the single supported engine", decided 2026-07-03 on the evidence in V15. PostgreSQL provides every primitive the accepted ADRs assume, generates UUIDv7 natively, is fully OSS and cloud-neutral, and is available as a managed service on every major cloud as well as on-premises.

PostgreSQL is the authoritative SQL flavor from this point on. The fixed consequences of the choice are:

* **Tenant-isolation backstop = row-level security**, using `FORCE ROW LEVEL SECURITY` plus a per-request `SET LOCAL` tenant variable (`set_config('app.current_tenant', <tid>, true)`) read by the policy. This is the database-level second layer beneath the EF Core global query filter (ADR-0001). The application's database role must be de-privileged (`NOSUPERUSER`, no `BYPASSRLS`): a superuser bypasses row-level security, so a privileged connection would silently disable the backstop (verified, V23/V25).
* **Optimistic-concurrency token = the `xmin` system column** (via Npgsql), used for admin ETags and dual-control TOCTOU checks (ADR-0009/ADR-0020). A separate rowversion column is not used because rowversion is SQL-Server-specific.
* **Primary-key generation = PostgreSQL 18 native `uuidv7()`** or .NET `Guid.CreateVersion7()` translated by Npgsql 10 when `SetPostgresVersion(18, 0)` is configured (ADR-0036).
* **Bootstrap first-key locking = `pg_advisory_lock`**, the distributed lock that ADR-0012's single-first-key seeding relies on.
* **Audit tamper-evidence = the application-side hash chain** (ADR-0008). A database-native immutable-ledger table is off the table: the mainstream commercial engine's ledger feature is proprietary and platform-locking, so PostgreSQL's `REVOKE UPDATE/DELETE/TRUNCATE` plus the application hash chain is the portable, engine-neutral mechanism. Because PostgreSQL `jsonb` does not preserve input byte order, the audit hash is computed over a separately canonicalized TEXT form of the payload, not over the stored `jsonb`.
* **JSON columns = `jsonb`**, used only as an extension bag; authorization data is normalized into indexable columns rather than left only in JSON.
* **Encryption at rest** is provided by full-volume/managed-disk encryption plus per-column `IDataProtection` for sensitive payloads (for example reference-token payloads), rather than an engine-native transparent-data-encryption feature, which PostgreSQL does not ship natively (ADR-0005/ADR-0006).
* **Connection pooling** follows the PgBouncer transaction-mode path where Silo scale requires it, with per-tenant pool sizing bounded so the connection count stays under the server ceiling (ADR-0018). Under transaction-mode pooling the tenant variable must be `SET LOCAL` inside the request transaction so it cannot leak across pooled connections.
* **Migrations** are applied through EF Core migration bundles (`efbundle`); the row-level-security objects (policies, `FORCE RLS`, the de-privileged role and grants) are not part of the EF model and are added as an explicit raw-SQL migration step after table creation (ADR-0017, verified V25).
* **Version floor = PostgreSQL 18** (released 2025-09-25) with `Npgsql.EntityFrameworkCore.PostgreSQL` 10 on EF Core 10, treated as a forward-only stack requirement (ADR-0030), because native `uuidv7()` is a PostgreSQL 18 feature.

### Consequences

* Good, because every primitive the accepted ADRs assume (row-level security, advisory-lock seeding, native UUIDv7, `xmin` concurrency) is provided natively, so no accepted decision has to be reworked around a weaker engine.
* Good, because PostgreSQL is fully OSS and available managed on every major cloud and on-premises, matching the cloud-neutral (ADR-0006) and license-freedom goals, and imposing no database licensing cost on adopters.
* Good, because a single authoritative flavor keeps schema, migrations, and tests single-dialect, removing the cost of proving two SQL dialects everywhere.
* Bad, because it forecloses the commercial engine's stronger turnkey features (a database-native cryptographically verifiable ledger; column-level Always-Encrypted-style protection), which must instead be reproduced at the application layer (the audit hash chain) or the infrastructure layer (volume encryption).
* Bad, because the "de-privileged role else the row-level-security backstop is silently off" rule is a security footgun that must be enforced by deployment convention and checked, not assumed.
* Neutral, because some illustrative DDL in the design corpus still carries the earlier SQL-Server flavor (`varchar`/`bit`/`varbinary`/`rowversion`); it is re-flavored to PostgreSQL (`text`/`boolean`/`bytea`/`xmin`) at implementation with no change in semantics.

### Confirmation

* Evidence V15 (2026-07-03) records the current, verified facts: `Npgsql.EntityFrameworkCore.PostgreSQL` 10 on EF Core 10; PostgreSQL 18 released 2025-09-25 with native `uuidv7()` (RFC 9562) and a 50-million-row insert benchmark of roughly 1.8 minutes for v7 versus roughly 20 minutes for v4 with an index about 25% smaller.
* The pool-isolation spike (ADR-0001 / ADR-0018) runs against real PostgreSQL 18 via Testcontainers, and V23/V25 confirmed that `FORCE ROW LEVEL SECURITY` under a de-privileged role confines both reads and bulk `DELETE` at the database level, independent of the EF filter, with a no-tenant context yielding zero rows.
* A deployment check asserts the application's database role is `NOSUPERUSER` and lacks `BYPASSRLS`, so the isolation backstop cannot be silently disabled.

## Pros and Cons of the Options

### PostgreSQL as the single supported engine (chosen)

* Good, because it natively provides row-level security, `pg_advisory_lock`, native `uuidv7()`, `xmin` concurrency, and `jsonb`; it is OSS, cloud-neutral, and runs on-premises and locally.
* Bad, because it has no engine-native verifiable-ledger or transparent-data-encryption feature, so those are reproduced above the engine.

### Microsoft SQL Server as the single supported engine

* Good, because it offers a database-native append-only ledger with cryptographic verification, `SESSION_CONTEXT`-based row-level security, and Always-Encrypted column protection out of the box.
* Bad, because those strongest features are proprietary and platform-locking, it carries commercial per-core licensing that conflicts with the project's license-freedom goal, and it does not offer native time-ordered UUID generation, weakening ADR-0036.

### A portable, engine-agnostic data layer supporting both

* Good, because it would let adopters bring their existing engine.
* Bad, because row-level security, concurrency tokens, advisory locks, UUID generation, and tamper-evidence differ enough between the two engines that "portable" would mean either the lowest common denominator (losing the primitives the ADRs depend on) or maintaining and testing two full SQL dialects everywhere, at a cost out of proportion to the benefit.

## More Information

* Decided 2026-07-03 (evidence V15). This ADR records an engine choice that until now lived only in the database-design documents, even though ADR-0001, ADR-0008, ADR-0011, ADR-0012, ADR-0018, ADR-0033, and ADR-0036 already assume it. ("SQLite" appears in the corpus only as a migration-locking example, never as a candidate engine; the rejected alternative was the commercial SQL Server engine.)
* Related decisions: ADR-0001 (row-level security as the tenant-isolation backstop), ADR-0005/ADR-0006 (encryption at rest and the provider-agnostic key/store seam), ADR-0008 (application-side audit hash chain, chosen over a database-native ledger as a consequence of this engine), ADR-0011/ADR-0012 (advisory-lock first-key seeding), ADR-0017 (migration bundles plus the raw-SQL row-level-security step), ADR-0018 (connection pooling and PgBouncer), ADR-0030 (PostgreSQL 18 as a forward-only stack floor), and ADR-0036 (UUIDv7 primary keys, which depend on native `uuidv7()`).
* Authored in this repository in 2026-07 to record the settled engine decision as an ADR; neutral third-party engines (PostgreSQL, Npgsql, EF Core, and Microsoft SQL Server as the rejected alternative) are named factually for identification and comparison only.
