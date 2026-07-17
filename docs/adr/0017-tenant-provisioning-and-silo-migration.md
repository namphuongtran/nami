---
status: "accepted"
date: 2026-06-29
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Ops and Security/DPO (the Pool-vs-Silo classification criteria and the deprovision key escrow retention/residency await their ratification); EF Core migration guidance and evolutionary-database-design practice
informed: all contributors, via this repository
---

# Orchestrate the tenant lifecycle with build-artifact migrations, per-tenant version gating, and expand/contract

## Context and Problem Statement

Under the Tiered multi-tenant model (ADR-0001), provisioning a new tenant is a control-plane saga plus, for a Silo tenant, a new database and key-set. Applying EF Core migrations across N tenant databases is an operational fan-out, not an EF feature: EF migrates one `DbContext` against one connection string, and Microsoft discourages a startup `Database.Migrate()` in production. A per-tenant `SchemaVersion` plus a traffic-gate had been decided in the database design, but without an ADR and without a full provisioning and rollout sequence. This needs a long-term design, not a workaround such as startup-migrate or "fail the whole fleet when one tenant errors". How should the tenant lifecycle (provision, migrate, deprovision, re-home) be orchestrated?

Note: this ADR is the tenant-lifecycle decision. It is **not** the delegated-admin enforcement design, which is a separate document referenced by ADR-0010.

## Decision Drivers

* Tenant lifecycle operations must be safe to re-run: idempotent and resumable.
* One tenant's failure must not take down the fleet.
* Roll-forward should be the default recovery, which requires backward-compatible (reversible) migrations.
* Reuse existing machinery: key bootstrap, dual-control, and the per-tenant `SchemaVersion`.
* Destructive operations (delete, re-home) require dual-control, must never run autonomously, and must never leave a half-live or half-erased state.

## Considered Options

* Startup `Database.Migrate()` per tenant
* An existing framework migrator (for example ABP's DB migrator or Finbuckle)
* A custom orchestrator: build-artifact fan-out plus a version-gate plus expand/contract

## Decision Outcome

Chosen option: "A custom orchestrator", because a startup migrate is discouraged and unsafe at fleet scale, and the existing framework migrators do not provide ordered rollout, a traffic-gate, or fleet rollback.

Fixed parameters of the decision:

* **Provision-tenant saga** (`ITenantProvisioningService`, idempotent and resumable, with a `ProvisioningRequest` checkpoint): register plus update the tenant closure (cycle-reject and serialize) → for a Silo, create and migrate the database → establish the key-set by reusing the ADR-0012 bootstrap auto-seed (a Silo gets its own key-set; a Pool tenant joins the group) → seed scopes/clients idempotently through the Manager, never raw SQL → flip `Enabled=true` only after a readiness gate (`SchemaVersion == AppExpectedVersion` and keys load). Dual-control applies (an iam_change/delete_tenant-class non-cascading capability, ADR-0009/0010). A partial failure sets `Enabled=false` and leaves the tenant for retry — never half-live.
* **Silo migration fan-out** (`IMigrationRunner`): a build artifact, not startup code — `dotnet ef migrations bundle` (`efbundle --connection`) or an `--idempotent` SQL script (both check `__EFMigrationsHistory`, so a re-run is a no-op and therefore resumable); the runtime application keeps a least-privilege connection with no DDL rights. Per-tenant `SchemaVersion` is the fleet view; per-database `__EFMigrationsHistory` is the truth. A per-tenant `503` traffic-gate engages when `SchemaVersion != AppExpectedVersion`, isolating the migrating tenant so the fleet does not fail and new code never runs on an old schema. Rollout is an ordered ring (ring-0 → canary → waves, bounded parallelism, halt-on-error). Expand/contract (parallel-change) provides real reversibility: backward-compatible migrations where old code and new schema coexist, and never a destructive change in the same release as the code that needs it. Per-tenant state is pending/in-progress/done/failed; a failure is 503-gated, logged, and retried or rolled forward.
* **Observability** is per-ring and per-tenant, with a schema-drift alarm.
* **Deprovision-tenant saga** (`ITenantDeprovisioningService`, idempotent and resumable), the ordered inverse of provisioning, dual-control, never autonomous: (1) flip `Enabled=false` with a `503` traffic-gate → (2) revoke all tokens (access, refresh, authorization) and kill all sessions (the ticket store and the reference-token store) → (3) erase or archive subject data through the erasure saga (ADR-0016, crypto-shred and Recital-66 reconciliation) → (4) escrow-then-destroy the key-set (short-term escrow per retention, then destroy — not immediate destroy; the DPO sets the retention window and residency) → (5) retire the keys from the JWKS (stop advertising them) → (6) drop or archive the Silo database, or purge Pool rows by tenant filter with forced row-level security → (7) remove the tenant from the registry and the closure table → (8) release secrets (connection-string and key references) from the secret store (ADR-0009/0010 dual-control) → (9) emit a delete_tenant-class, hash-chained audit event (ADR-0008). A partial failure halts at the checkpoint for a manual, dual-controlled resume or rollback — never half-erased.
* **Tenant move: re-parent and Pool↔Silo re-home** (`ITenantRehomeService`, dual-control, idempotent and resumable): (a) re-parenting (changing the parent in the closure table) recomputes and re-audits inherited delegated-admin grants — grants inherited from the old parent branch are revoked, the new branch is recomputed, every changed grant is audited, and cycles are rejected and serialized as in provisioning; (b) a Pool↔Silo re-home (moving data and key-set) reuses the ADR-0001 Pool→Silo data-move under dual-control, and after the move it verifies old-scope invisibility (data and keys in the old scope no longer resolve from the new scope or vice versa, so there is no key-scope blast-radius leak) plus a negative test (a cross-scope read, JWE-decrypt, or JWKS lookup must fail) before flipping `Enabled=true` in the new scope. Old-scope teardown runs the deprovision-saga steps (revoke, escrow/destroy the old key, retire the old JWKS).

### Consequences

* Good, because re-runs are safe, one tenant's failure does not drag the fleet down (503 isolation), roll-forward is the default recovery (expand/contract), and it reuses the bootstrap (ADR-0012), dual-control (ADR-0009/0010), and `SchemaVersion` machinery.
* Bad, because the orchestrator must be built (no framework provides it for free), the traffic-gate adds a schema-version-state lookup to each request (cacheable), and expand/contract is a coding discipline that must be enforced by a CI `has-pending-model-changes` check.
* This decision depends on ADR-0001 (Pool/Silo and the Pool→Silo data-move), ADR-0008 (hash-chained audit), ADR-0009/0010 (dual-control and non-cascading capabilities), ADR-0012 (key-set bootstrap), and ADR-0016 (the erasure saga reused during deprovisioning).

### Confirmation

* EF Core facts verified: there is no multi-tenant migrate; Microsoft discourages a startup Migrate (concurrency, elevated permissions, no rollback; "consider generating SQL scripts"); `dotnet ef migrations bundle` and `--idempotent` both check `__EFMigrationsHistory`; Finbuckle leaves migration to the developer; the ABP migrator has a per-tenant gap. Expand/contract is the established parallel-change / evolutionary-database-design pattern, and ring rollout follows established multi-tenant rollout practice.
* Verify-before-build: a CI acceptance test that a migration-version mismatch blocks via the traffic-gate; a ring-rollout and partial-failure runbook drill; a deprovision-saga partial-failure drill; a re-home old-scope-invisibility negative test (cross-scope read/JWE-decrypt/JWKS must fail); and a re-parent inherited-grant recompute audit test.

## Pros and Cons of the Options

### Startup `Database.Migrate()` per tenant

* Good, because it is the least code to write.
* Bad, because Microsoft discourages it for concurrency, elevated permissions, and the lack of rollback, and it cannot isolate a failing tenant from the fleet.

### An existing framework migrator (ABP migrator / Finbuckle)

* Good, because it exists and is a useful reference baseline.
* Bad, because Finbuckle is only connection plumbing (migration is left to the developer) and the ABP migrator has a per-tenant gap, and neither provides ordered rollout, a traffic-gate, or fleet rollback.

### A custom orchestrator (chosen)

* Good, because it delivers idempotent, resumable, per-tenant-isolated rollout with roll-forward recovery, reusing existing machinery.
* Bad, because it must be built and maintained, and expand/contract must be enforced as a discipline.

## More Information

* Original decision: 2026-06-29. This is the tenant-lifecycle ADR and is distinct from the delegated-admin enforcement design (a separate document referenced by ADR-0010).
* Open follow-ups: the Pool-vs-Silo classification criteria (DPA/residency) at onboarding, which resolves an open item in ADR-0001, pending Security/DPO ratification; and the deprovision key escrow retention window and residency, pending DPO ratification.
* Related decisions: ADR-0001 (Pool/Silo and Pool→Silo data-move), ADR-0008 (hash-chained audit), ADR-0009/0010 (dual-control and non-cascading capabilities), ADR-0012 (key-set bootstrap auto-seed), ADR-0016 (erasure saga reused in deprovisioning).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. Framework and library citations (EF Core, Finbuckle, the ABP migrator, and the parallel-change pattern) are retained as neutral technical precedent.
