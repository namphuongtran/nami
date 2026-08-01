---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: gap analysis against commercial identity servers' server-side session support and OpenIddict's session capabilities
informed: all contributors, via this repository
---

# Server-side session store is a core feature, not an option

## Context and Problem Statement

An early draft classified the server-side session store as "optional". Review showed it is a security keystone: it is the precondition for (a) an admin killing a session, (b) "log out everywhere", (c) inactivity timeout and absolute session lifetime, and (d) back-channel logout. OpenIddict has no server-side session concept (only the ASP.NET Core authentication cookie), while comparable commercial identity servers ship one out of the box. Without it, a compromised or abandoned session cannot be revoked immediately; the only remedy is waiting for tokens to expire. Should Nami treat server-side sessions as optional or as core?

## Decision Drivers

* Immediate, centralized session revocation is non-negotiable for an identity provider handling sensitive data.
* Inactivity and absolute session lifetimes must be enforceable server-side, not just in the cookie.
* Back-channel logout needs a server-side session registry to know which clients to notify.
* Production-grade expectation: this is a headline feature consumers expect from a production identity server.
* Multi-node deployments need a durable store, not in-memory state.

## Considered Options

* Cookie-only sessions (no server-side store)
* Server-side session store (`ITicketStore` over EF Core)

## Decision Outcome

Chosen option: "Server-side session store (`ITicketStore` over EF Core)", because cookie-only sessions cannot be revoked centrally and therefore fail the production bar for an IdP. The feature is promoted from optional to core.

Fixed parameters of the decision:

* **Backend: durable relational store (PostgreSQL via EF Core)**, implemented as an `ITicketStore` that persists the `AuthenticationTicket`; the cookie carries only a handle. A read-through cache (for example Redis) may be added later for high concurrency, but PostgreSQL remains the source of truth.
* **The store is global**: a session belongs to the human, not to a tenant, matching the global identity model of ADR-0001. For Silo tenants with hard isolation requirements, separate storage or access controls for their session/activity data is a consideration during tenant onboarding.
* **Storage shape** (`ServerSideSessions`, field-level detail in the data design): `Id` is a `bigint` identity, the one deliberate exception to the UUIDv7 primary-key rule (ADR-0036), because this table is an internal, high-churn surrogate; `Key` is the unique `sid` that clients reference; `Scheme`, `SubjectId`, `SessionId`, and an optional `DisplayName` identify the session; `Created`, `Renewed` (last activity, driving the inactivity window), and `Expires` (the absolute ceiling) carry its lifetime; and `Data` holds the serialized ticket. Indexes cover `Expires`, `SubjectId` (for concurrent-session evict-oldest), and `SessionId`. A child table `SessionParticipatingClients` is the registry that back-channel logout reads to know which relying parties to notify (ADR-0019). **Corrected 2026-08-01: it is keyed on the login chain, not on the session `Key`, and it identifies a relying party by `ApplicationId` rather than by `client_id`.** Both halves of the original shape were unusable. Keying on `Key` with a cascade delete meant that the `sid` rotation this ADR mandates on step-up (see the `sid` lifecycle below) deleted the participating-RP list, so a logout after any step-up notified **zero** relying parties, while updating the key in place would instead strand every RP still holding the old `sid`; the key itself was wrong, so there was no third option. And a `client_id` is unique only per tenant under Pool (ADR-0001 requires the composite `(TenantId, ClientId)`), so the relay could not resolve one `backchannel_logout_uri` from it. The row also carries `SidIssued`, the `sid` **that** relying party currently holds, because a `logout_token` must carry the value its recipient stored. Field-level detail is in the data design.
* **Revocation deletes the row.** There is deliberately **no `revoked` column**: force-logout, admin kill, and logout-everywhere all remove the `ITicketStore` row, and the cookie re-validation interval is the backstop that makes the removal take effect on any node. A soft-delete flag would create a second source of truth for "is this session live" and an easy way to mistake a dead session for a live one. An early sketch of this table did list a `revoked` column and omitted `Data`; it was replaced because without `Data` the `ITicketStore` contract cannot work at all, and the row-delete semantics followed from that.
* **Cookie re-validation** is driven by `SecurityStampValidatorOptions.ValidationInterval`, set to 1 to 2 minutes to balance revocation immediacy against database load (the exact value is finalized during implementation, together with the kill-propagation target below).
* **`sid` lifecycle**: stable across passive or silent refresh; **rotated on step-up** (MFA or elevation, ADR-0013). A separate session-fixation guard mints a fresh `sid` on the anonymous-to-authenticated transition; that guard is enforced in the core protocol pipeline rather than by this store.
* **Strict timeouts** (sensitive-data posture): inactivity (sliding) 1 hour, absolute 8 hours; past the absolute limit, re-authentication is required.
* Authorization and refresh requests are denied once the session row is gone. **Where the refresh half is executed, added 2026-08-01:** design [04](../design/04-core-protocol.md), in the same `HandleTokenRequest` block as the ADR-0004 ceiling. This clause was accepted with this ADR but no design carried it, which is why several documents described a "bounded logout" bound that nothing enforced (see ADR-0019). Two consequences of the row-delete semantics above follow directly: no new claim is needed, because `sid` already travels on the refresh token; and, because row-absence cannot distinguish revoked from expired, the effective refresh lifetime is `min(the ADR-0004 ceiling, the session still being alive)`, so the 1-hour inactivity window ends the refresh chain too. That is intended at the strictness this ADR chose, and it must not be softened by re-introducing the `revoked` column the row-delete bullet above rules out.
* **Concurrent-session cap**: a per-user `MaxConcurrentSessions` limit, overridable per tenant, enforced on login by counting the user's rows by `SubjectId` and evicting the oldest (ordered by `Created`) when the cap is exceeded. The shipped default is an illustrative 5 rather than a fixed policy number; the acceptance test for the cap is 9.T19.

### Consequences

* Good, because it unlocks admin session kill, logout-everywhere, enforceable inactivity/absolute expiry, and provides the seam on which back-channel logout is built (ADR-0019).
* Bad, because every validation interval costs a database read, and the session store joins the HA/scaling and disaster-recovery surface (ADR-0006); it must be durable for multi-node operation.
* Token revocation and session revocation are distinct mechanisms, and both are required; conflating them is a design error this ADR explicitly forbids.

### Confirmation

* Integration tests: deleting a session row denies the authorize and refresh endpoints within one validation interval; logout-everywhere removes all of a user's session rows; absolute expiry forces re-authentication; and test 9.T19 covers the concurrent-session cap evicting the oldest session.
* Kill-propagation across nodes is a stated NFR with a target below 2 minutes (finalized with the validation interval during implementation).
* Code review confirms the session store remains global and PostgreSQL-backed per this ADR.

## Pros and Cons of the Options

### Cookie-only sessions

The ASP.NET Core authentication cookie is the only session state; the server keeps nothing.

* Good, because it is zero additional infrastructure and zero added latency.
* Bad, because centralized revocation is impossible: no admin kill, no logout-everywhere, no server-enforced lifetimes; the only mitigation is short token TTLs.
* Bad, because back-channel logout has no session registry to work from.
* Bad, because it fails the production bar that commercial-grade identity servers set for sensitive data.

### Server-side session store (`ITicketStore` over EF Core) (chosen)

Tickets persisted in PostgreSQL keyed by `sid`; the cookie holds a handle; revocation and lifetimes enforced server-side.

* Good, because sessions become first-class revocable objects with enforceable lifetimes.
* Good, because it reuses the already-chosen persistence stack (EF Core + PostgreSQL) and stays cloud-agnostic.
* Neutral, because a caching layer can be added later without changing the source of truth.
* Bad, because of per-interval database reads and one more component in the HA and DR story.

## More Information

* Original decision: 2026-06-28, updated later to note that back-channel logout is built directly on this session store as an interim implementation rather than waiting for native support in a future OpenIddict version (see ADR-0019 and ADR-0021 for the seam and upgrade strategy).
* Interim posture until back-channel logout is fully built: "revoke all authorizations + clear session + short access-token TTL".
* Back-channel logout implementation notes carried with this decision: the IdP emits `logout_token` (`typ=logout+jwt`); fan-out is decoupled (background worker, retries, idempotent `jti`, dead-letter queue) and never blocks interactive logout, a rule ADR-0019's best-effort immediate dispatch is explicitly bounded by: it runs after the response and takes one attempt, so it cannot reintroduce the N-call latency; `backchannel_logout_uri` is validated against SSRF.
* Deferred to a post-v1 wave (proposed, no ADR yet): an end-user session/device management UI (view active logins, sign out everywhere) built over this session store; revisit when end-user self-service is prioritized.
* Related decisions: ADR-0001 (global identity), ADR-0006 (DR), ADR-0019 (single logout strategy), ADR-0021 (OpenIddict version adaptation).
* Open follow-up (does not block implementation): exact validation interval (1 or 2 minutes) and the kill-propagation SLO number.
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25. The reconcile corrected a real error: this ADR had listed a `revoked` column among the session store's indexed columns, which does not exist. Revocation is a row delete, confirmed by the corpus ADR, the corpus data-model DDL, the corpus session/logout design in two places, and this repository's own login/logout and data designs, all of which already said row removal. The column and index names were also corrected to the PascalCase identifier convention that the data design owns, and the storage shape, the `SessionParticipatingClients` child table, and the session-fixation guard were added.
