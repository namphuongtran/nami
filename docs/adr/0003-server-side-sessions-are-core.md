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
* Sessions are keyed by `sid`; indexed columns: `sub`, `sid`, `last_activity_utc`, `absolute_expiry_utc`, `revoked`.
* Cookie re-validation interval of 1 to 2 minutes balances revocation immediacy against database load (exact value finalized during implementation).
* `sid` lifecycle: stable across passive refresh; **rotated on step-up or re-authentication**.
* **Strict timeouts** (sensitive-data posture): inactivity (sliding) 1 hour, absolute 8 hours; past the absolute limit, re-authentication is required.
* Authorization and refresh requests are denied when the session is revoked.
* **Concurrent-session cap**: a per-user `MaxConcurrentSessions` limit (default around 5, overridable per tenant), enforced on login by counting the user's live sessions by `SubjectId` and evicting the oldest when the cap is exceeded.

### Consequences

* Good, because it unlocks admin session kill, logout-everywhere, enforceable inactivity/absolute expiry, and provides the seam on which back-channel logout is built (ADR-0019).
* Bad, because every validation interval costs a database read, and the session store joins the HA/scaling and disaster-recovery surface (ADR-0006); it must be durable for multi-node operation.
* Token revocation and session revocation are distinct mechanisms, and both are required; conflating them is a design error this ADR explicitly forbids.

### Confirmation

* Integration tests: a revoked session is denied at the authorize and refresh endpoints within one validation interval; logout-everywhere revokes all of a user's sessions; absolute expiry forces re-authentication.
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
* Back-channel logout implementation notes carried with this decision: the IdP emits `logout_token` (`typ=logout+jwt`); fan-out is decoupled (background worker, retries, idempotent `jti`, dead-letter queue) and never blocks interactive logout; `backchannel_logout_uri` is validated against SSRF.
* Deferred to a post-v1 wave (proposed, no ADR yet): an end-user session/device management UI (view active logins, sign out everywhere) built over this session store; revisit when end-user self-service is prioritized.
* Related decisions: ADR-0001 (global identity), ADR-0006 (DR), ADR-0019 (single logout strategy), ADR-0021 (OpenIddict version adaptation).
* Open follow-up (does not block implementation): exact validation interval (1 or 2 minutes) and the kill-propagation SLO number.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized.
