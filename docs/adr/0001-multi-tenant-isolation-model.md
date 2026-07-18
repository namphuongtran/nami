---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: evidence review of commercial identity servers, Azure Architecture Center, AWS SaaS guidance, Auth0 Organizations, ABP Framework (see More Information)
informed: all contributors, via this repository
---

# Tiered multi-tenant isolation: global identity, pooled tenant data by default, silo on demand

## Context and Problem Statement

Nami serves multiple tenants (customers or organizational units) from one identity service. OpenIddict has no tenant concept in its four entities (Application, Authorization, Scope, Token): without isolation designed in from the start, tenant-scoped data of different tenants would mix in one store. Isolation is a hard security decision that cannot be retrofitted cheaply, because it shapes the schema, every query, the key architecture, and migrations.

The prerequisite questions: is Nami multi-tenant at all, and if so, how strongly must tenant data be isolated, and how do users who belong to several tenants work?

## Decision Drivers

* No cross-tenant data leakage, ever: this is the top security invariant of an identity provider.
* One person should have one identity (one credential set, one MFA enrollment) even when they belong to many tenants, with fast tenant switching and no re-login.
* Some tenants have hard isolation requirements (data processing agreements, data residency, regulators); most do not and should not pay the cost of a dedicated database.
* Operational cost must stay sane: hundreds of pooled tenants must not mean hundreds of databases and migration fan-out.
* Tenant resolution must not require a database query on claims at the token endpoint (chicken-and-egg problem warned about by the OpenIddict maintainer in issue #1699).
* Downstream APIs need unambiguous tokens: a token must belong to exactly one tenant.

## Considered Options

* Single-tenant deployment (no multi-tenancy)
* Database-per-tenant for everyone (silo-only)
* Single shared database with a tenant discriminator column for everyone (pool-only)
* Tiered/bridge model: shared pooled database by default, dedicated silo database per tenant that needs it

## Decision Outcome

Chosen option: "Tiered/bridge model", because it gives the cheap shared-database path to the majority of tenants while still offering physical isolation to tenants that require it, without forcing either extreme on everyone; and it matches the documented practice of Azure, AWS, Auth0, and the .NET multi-tenant ecosystem. A tenant's mode is a per-tenant switch (`IsolationMode`), so moving a tenant from Pool to Silo is a data migration, not an architecture rewrite.

The decision fixes five sub-decisions:

### A. Data layering

* **Identity is global**: credentials, MFA, email, security stamps live in a global identity store; one user for all tenants, one login, one password change, one MFA enrollment.
* **Membership is global**: a mapping of user to tenant to roles is the single source of truth for "which tenants does this user belong to". Adding a user to a tenant creates a membership row, never a duplicate user.
* **Tenant registry (control plane) is global**: a `Tenants` table holds `{ TenantId, Parent, IsolationMode (Pool | Silo), ConnectionString, KeyScope }`. This is the switch that routes each tenant to its store.
* **Tenant-scoped data** (OpenIddict Applications, Authorizations, Tokens, plus detailed roles/claims) lives where `IsolationMode` says: Pool tenants share one database, discriminated by a mandatory `TenantId` column with a mandatory EF Core global query filter; Silo tenants get a dedicated database (no discriminator column needed).
* **The scope catalog is global, not per-tenant**: scopes are defined by the product's APIs and shared by all tenants. Scopes carry no `TenantId`, scope names are globally unique, and the catalog is seeded once. Per-tenant differences are expressed as scope allowlists on the client grant, never by forking the catalog. (Consistent with Auth0, ABP host-global scopes, mainstream commercial IdPs, and RFC 8707; validated empirically, see Confirmation.)
* This layering is realized as **four DbContexts**: `OpenIddictDbContext` (tenant-scoped: pool filter or silo connection), `IdentityDbContext` (global), `DataProtectionDbContext` (global), `ControlPlaneDbContext` (global: tenants, memberships, delegated admin, audit log, server-side sessions). This topology is fixed; changing it requires a superseding ADR.

### B. Tenant switching without re-login

* The session is the human's identity (a global SSO cookie); the active tenant belongs to the token, not the session.
* Switching tenant means a silent `/connect/authorize` round trip (`prompt=none`) for the target tenant: the IdP sees the valid cookie, checks membership, and issues a new token scoped to that tenant. No password prompt.
* **Access tokens are always single-tenant** (exactly one tenant claim, issuer per tenant), so downstream APIs are never ambiguous. The id_token carries the membership list so applications can render a tenant switcher.

### C. Tenant resolution by host or path, never by claim

Tenant is resolved from the subdomain/host or path (for example `acme.id.example.com`), not from a claim that would require a database query, avoiding the token-endpoint chicken-and-egg problem. Human authentication runs against the global identity store first, before any tenant database is touched.

### D. Per-tenant key sets

Silo tenants get their own signing/encryption key set (full isolation). Pool tenants share one pool-group key set. There is deliberately **no per-tenant key inside the Pool**: a pool tenant that needs its own keys must move to Silo (see ADR-0033 for the key-scope isolation model and its one-keyset-per-deployment invariant). Related: ADR-0005, ADR-0006, ADR-0007.

### E. Client registration

* Pool tenants: a client is a row in the shared database with a `TenantId` column; the same logical `client_id` may exist for several tenants. **Mandatory index override**: OpenIddict's EF Core defaults create a globally unique index on `ClientId`, and the multi-tenant library does not scope it automatically; the pool DbContext must replace it with a composite unique index on `(TenantId, ClientId)`. This was proven empirically: without the override, a second tenant reusing a `client_id` fails with a PostgreSQL unique violation; with it, registration succeeds and stays isolated per tenant.
* Silo tenants: clients live in the tenant's own database, no override needed.
* Provisioning a tenant seeds its clients automatically; scopes are never seeded per tenant (global catalog, see A).

### Consequences

* Good, because the majority of tenants ride the cheap pooled path, while sensitive tenants get hard isolation without forcing every tenant to pay for a dedicated database.
* Good, because one user identity spans tenants, giving single sign-on and instant tenant switching, which fits businesses that acquire or restructure organizations.
* Bad, because the code must support two routing modes (pool filter vs silo connection) and both must be tested everywhere.
* Bad, because pooled mode makes the global query filter a load-bearing security control: a single forgotten filter is a cross-tenant leak. Mitigated by mandatory negative tests, a tracking-time enforcement guard, and row-level security as a database-level backstop (the `FORCE` row-level-security policy under a de-privileged, non-`BYPASSRLS` database role is recorded in ADR-0037; a privileged connection would silently disable this second layer).

### Confirmation

* Cross-tenant negative tests are a permanent acceptance criterion: a pool tenant must never read another tenant's rows through the filter, and a silo tenant must always land on its own connection.
* The riskiest composition (multi-tenant library + OpenIddict + pooled DbContext isolation) was validated before adoption by a dedicated runnable spike (17/17 tests passing against real PostgreSQL via testcontainers), covering filter behavior under context pooling, internal writes, the composite `ClientId` index, and the global scope catalog. The spike harness is kept as regression tests.
* Compliance is checked in code review against this ADR; the DbContext topology in A may not be changed without a superseding ADR.

## Pros and Cons of the Options

### Single-tenant deployment

Simplest possible model; multi-tenancy handled by deploying one full stack per customer.

* Good, because there is no isolation code at all to get wrong.
* Bad, because it contradicts the product goal: Nami is multi-tenant SaaS-style infrastructure, and per-customer full deployments do not scale operationally or commercially.
* Bad, because users who belong to several organizations get separate accounts and logins.

### Database-per-tenant for everyone (silo-only)

Every tenant gets a dedicated database (connection string per tenant), keeping OpenIddict's own schema and constraints intact per database.

* Good, because isolation is physical and the class of forgotten-filter bugs does not exist.
* Good, because per-tenant key sets and per-tenant restore/erasure are natural.
* Bad, because operations multiply per tenant: provisioning, migrations, backups, connection pool exhaustion across many databases.
* Bad, because it is the wrong shape when many users belong to multiple related tenants (Azure Architecture Center explicitly recommends against it for that case).
* Bad, because the original v1 of this decision chose exactly this, and the evidence review overturned it: none of the surveyed platforms force silo-only.

### Single shared database with tenant discriminator for everyone (pool-only)

One database; every tenant-scoped row carries `TenantId`; EF global query filters plus manager-level guards, optionally row-level security.

* Good, because it is the cheapest to operate and migrate (one schema, one migration path).
* Good, because it is the common default in the .NET multi-tenant ecosystem.
* Bad, because one forgotten query filter is a cross-tenant data leak in an identity provider.
* Bad, because tenants with residency or regulator demands cannot be served at all.

### Tiered/bridge model (chosen)

Pool by default, silo on demand, selected per tenant via the control-plane registry; global identity and membership above both.

* Good, because each tenant pays only for the isolation level it needs, and the mode is a per-tenant switch rather than an architectural fork.
* Good, because it matches the surveyed industry consensus: AWS treats pool as default and silo for hard isolation; Azure recommends single identity plus membership; Auth0 Organizations implements exactly the one-user-many-orgs-single-tenant-token model; the leading commercial identity server (which has no native multi-tenancy) acknowledges both patterns as valid; ABP keeps OpenIddict stores host-global.
* Neutral, because it inherits the pooled mode's filter risk for pooled tenants, with the mitigations listed in Consequences.
* Bad, because both modes must be implemented, routed, and tested (two code paths in provisioning, migration, keys, and revocation).

## More Information

* Original decision: 2026-06-28 (v2, revised from a v1 database-per-tenant decision after the evidence review below). Imported into this repository and translated in 2026-07; content is preserved, internal references generalized.
* Evidence reviewed at decision time: a commercial identity server's multi-tenancy discussion ("a great many subtly different requirements", with vendor hooks for a `tenant` claim and re-auth validation); Azure Architecture Center guidance on identity vs data multi-tenancy decisions; AWS SaaS isolation guidance (pool default, silo for hard isolation, shared identity within silo definitions); Auth0 Organizations model; ABP Framework's host-global OpenIddict stores; OpenIddict maintainer warning on claim-based tenant resolution (openiddict-core #1699).
* Related decisions: ADR-0005 (encryption and credential lifecycle), ADR-0006 (DR and keyring), ADR-0007 (key compromise), ADR-0010 (tenant hierarchy: no automatic parent-child role inheritance; explicit membership plus scoped, time-bound delegated admin), ADR-0018 (DbContext pooling with mutable tenant), ADR-0033 (key-scope isolation model).
* Open follow-up (does not block implementation): the classification criteria for which tenants qualify for Silo (for example DPA or residency requirements) are ratified with security/data-protection stakeholders during tenant onboarding.
* Background jobs (for example token pruning) run outside a request and must iterate tenants and set the tenant context explicitly; this is a known constraint carried into the implementation docs.
