---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0010 (the delegated-admin grant model and inheritance, which is why an ancestor grant cannot be cancelled from here), ADR-0047 (`ICheckAccess`, the live read that makes the admin surface immediate), ADR-0004 (the 15-minute access-token lifetime that bounds the other surface), ADR-0015 (break-glass, the recovery this refuses to make necessary), ADR-0079 (the revocation conventions this route follows)
informed: Admin API implementers, the Admin App memberships editor, operators running an offboarding or an incident response
---

# Define what removing a person from a tenant guarantees, before writing the route

## Context and Problem Statement

`/tenants/{tenantId}/memberships` has `GET` and `PUT` and **no `DELETE`**. A person can
be added to a tenant and never removed, so nothing in the contract revokes a person's
access to one tenant.

The obvious fix is to add the route. Writing the obvious version first and then
checking it against the design is what makes this an ADR rather than an endpoint,
because **two independent paths survive a membership removal** and a route described as
"removes the user's membership, effective immediately" would be wrong about both.

1. **The authorization query never joins `Memberships`.** The decision query in the
   authorization design selects over `DelegatedAdmin`, `DelegatedAdminCapabilities`,
   `TenantClosure`, and `CapabilityCatalog`, and nothing else. A delegated-admin grant
   is therefore **independent of membership**: deleting the membership row leaves every
   administrative capability the person held fully alive. The endpoint would remove the
   coarse access and advertise itself as removing access.
2. **The access token carries the authorization as claims.** `tenant` is stamped to the
   access token, alongside the coarse per-tenant role that gateways and resource
   servers check. So the Admin API stops honouring the person immediately, because it
   re-reads live, while **a resource server keeps honouring the token it already
   holds**. The removal is immediate on one surface and delayed on the other, and
   nothing said so.

A third hazard appears the moment removal exists at all: nothing prevents removing the
**last** membership that administers a tenant, leaving it administrable only through
break-glass.

So the question is not which HTTP verb. It is **what the operation guarantees**, which
is the part an operator relies on during an offboarding or an incident.

## Decision Drivers

* An endpoint named "remove a user's membership" is called by someone who means **cut
  this person off from this tenant**. If it does less, it must say so **in the
  response**, not in a document the caller has not read.
* Under incident pressure nobody chains three calls in the right order. Whatever can be
  atomic should be.
* Some paths genuinely **cannot** be closed from here. An ancestor-rooted grant is a
  decision taken at the ancestor over many tenants, and cancelling it from a descendant
  would silently change the ancestor's model.
* A residual should be **a number the caller sees**, not an assumption they make.
* Locking every administrator out of a tenant must not be an ordinary side effect of an
  ordinary call.

## Considered Options

* **A. Remove only the membership row**, and document that grants and tokens are
  separate concerns.
* **B. Remove the membership, cascade what can be cascaded atomically, and report the
  rest.**
* **C. Refuse with `409` while the user still holds any grant rooted at this tenant**,
  forcing the caller to revoke grants first.

## Decision Outcome

Chosen: **Option B.**

**In one transaction:**

1. Remove the `Memberships` row.
2. **Revoke the user's delegated-admin grants whose root is exactly this tenant**,
   soft-deleted by setting the revocation timestamp, consistent with the existing grant
   revoke route. Without this the endpoint does not do what it is named after.
3. **Revoke that subject's tokens and authorizations scoped to this tenant.** Not
   subject-wide: a subject-wide revoke would cut the person out of **every** tenant,
   which is a different operation with a different blast radius.
4. **Audit every part under one correlation id**, as a membership-removed event plus
   the grant-revoke and token-revoke events, so the cascade is reconstructible instead
   of appearing later as three unrelated actions.

**Reported in the response body, because it cannot be done here:**

* **`residualAncestorGrants`.** A grant rooted at an **ancestor** still confers
  inheritable capabilities over this tenant (ADR-0010). It is left alone deliberately
  and named explicitly, so a caller who wanted a full cut-off knows exactly where to
  act next.
* **`residualTokenWindowSeconds`, 900.** Any access token that step 3 does not reach
  stays honourable for at most the access-token lifetime (ADR-0004). The window is
  small and it is **not zero**, and reporting 900 is worth more than reporting
  "effective immediately".

**Refused with `409` when the removal would leave the tenant with no administrator:**
no other active membership carrying an admin role, and no active grant conferring user
management rooted at this tenant or at an ancestor. Recovery would otherwise be
break-glass only (ADR-0015), so it is refused up front rather than audited afterwards.
This also covers the common case of an administrator removing their own last
membership.

**Timing, stated once so it is not re-derived:**

| Surface | When the removal takes effect | Why |
|---|---|---|
| Admin API | **immediately**, on the next request | The scope handler calls `ICheckAccess`, which is a live read |
| Resource servers and business APIs | **within 900 seconds**, and immediately for every token step 3 reaches | `tenant` and the coarse role are token **claims** |

**Conventions it inherits.** It is a **revocation**, so `DELETE` with **no** `If-Match`
(ADR-0079 rules 1 and 4), idempotent, and a membership in another tenant answers `404`
rather than `403`. It returns **`200` with a body**, not `204`, because the residuals
are the point.

**Step-up still applies, and it is not dual-control.** Step-up costs the responder
seconds; dual-control costs a second person. Declining dual-control here is not
declining step-up. It is not dual-control for the same reason grant revoke is not: it
**reduces** privilege and it is on the incident path, where a four-eyes requirement is a
hazard rather than a safeguard.

### Consequences

* Good, because the endpoint does what its name says, and where it cannot, the caller
  is told in the response rather than in a document they will not read during an
  incident.
* Good, because both survival paths are either closed or quantified instead of left as
  a reader's exercise. The failure this avoids is the one that reads as complete.
* Good, because the last-administrator guard turns an unrecoverable mistake into a
  `409`.
* Good, because `200`-with-a-body rather than `204` forces a client author to look at
  what actually happened. A `204` invites the assumption that nothing more needed
  saying, which is exactly the assumption that is wrong here.
* Bad, because step 3's mechanism is **not yet verified**. Tenant-scoped revocation
  presumes the store query honours the tenant filter when the ambient tenant is set.
  Spike A-4 proved the filter applies to store queries in general, but **not
  specifically for the subject-wide revoke API**, so this is a verify-at-build item
  rather than an assertion made here.
* Bad, because the operation is compound, so a partial failure has to roll back as a
  unit. It is one transaction on the control plane, which the dual-control saga already
  does for harder cases.
* Neutral, because the guard costs one reachability query per call. Membership removal
  is rare, and the query is the existing decision query with a different parameter.

### Confirmation

* **Both survival paths were verified against this repository, not assumed from the
  source.** The authorization decision query was read: it joins `DelegatedAdmin`,
  `DelegatedAdminCapabilities`, `TenantClosure`, and `CapabilityCatalog`, with no
  `Memberships` join anywhere in it. And `tenant` is stamped to the access token
  alongside the coarse per-tenant role.
* **This repository is in a better starting position than the source was**, and that is
  worth recording: the corpus this came from had already added a `DELETE` route
  described as "effective on the next authorization check", and the ADR was written to
  correct a route that already over-claimed. Here no route exists yet, so the operation
  is defined before it is built and no wrong description ever ships.
* Tests: removing a membership also revokes grants rooted at that tenant and leaves
  ancestor-rooted grants intact, with the count reported; a token issued before the
  removal is rejected by the Admin API immediately and by a resource server within the
  access-token lifetime; removing the last administering membership returns `409` and
  changes nothing; the operation is idempotent; a membership in another tenant returns
  `404`; and the whole cascade shares one correlation id in the audit chain.

## Pros and Cons of the Options

### A. Remove only the membership row

* Good, because it is the smallest change and each concern stays separable.
* Bad, because it is the version that is wrong about both survival paths while looking
  correct, and it puts the burden of knowing that on every caller.

### B. Remove, cascade what can be cascaded, report the rest (chosen)

* Good, because one call does what the operator meant, and the parts it cannot do are
  returned as data rather than left implicit.
* Bad, because it makes the operation compound and transactional, and it adds a
  reachability query.

### C. Refuse while any grant rooted here survives

* Good, because it never silently leaves privilege behind, and the caller is forced to
  see the grants.
* Bad, because it makes the incident path a multi-step negotiation exactly when speed
  matters, and it still would not address the token window, so the caller ends up with
  an ordering puzzle and no better guarantee.

## More Information

* Mechanism and the route: design [15](../design/15-admin-api.md). The authorization
  decision query is design [07](../design/07-authorization.md), and it is the artifact
  that makes problem 1 real rather than theoretical.
* Related decisions: ADR-0010 (grant inheritance, and why an ancestor-rooted grant is
  out of scope from a descendant), ADR-0047 (the live authorization read), ADR-0004
  (the access-token lifetime that sets the 900-second residual), ADR-0015 (break-glass,
  the recovery the `409` avoids needing), ADR-0079 (revocation is `DELETE` with no
  precondition), ADR-0008 (the shared correlation id across the cascade).
* Imported from the design corpus's membership-removal decision on 2026-08-01, with
  both survival paths re-verified against this repository's own authorization query and
  claim destinations rather than carried across.
