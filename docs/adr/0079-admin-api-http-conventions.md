---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Google AIP-136 (custom methods) and AIP-135 (delete), both fetched and quoted at source 2026-08-01; RFC 9457 (problem details); ADR-0001 (which entities carry the tenant discriminator); ADR-0020 (the two admin deployables); ADR-0080 (the probe paths, the one route family this ADR does not govern)
informed: Admin API and Admin App implementers, anyone generating a client from the published contract
---

# Decide the Admin API's HTTP surface by rule rather than per endpoint

## Context and Problem Statement

The admin surface is large and still being written. Its shape has been decided
endpoint by endpoint, which is how a surface becomes inconsistent without anyone
ever making a decision they would defend. Two questions in particular recur on
every new resource, and both have been answered from taste: **does this collection
sit under a tenant prefix**, and **is revoking something a `DELETE` or a
`POST /{id}/revoke`**.

That would be a tolerable style problem if the surface were internal. It is not.
ADR-0020 makes the API and the App **separate deployables**, and the App consumes a
client generated from the contract, so a verb or path change after the first release
is a breaking change across a project boundary. The conventions have to be settled
before the remaining endpoints are written.

Reviewing the current design against a convention set that did not yet exist found
five places where it had drifted, and they are listed in Confirmation rather than
here because the point of this ADR is the rule, not the cleanup.

## Decision Drivers

* A generated client breaks on any rename, so the cost of deciding late is paid by
  consumers rather than by us.
* Each rule should be **decidable from a fact already recorded in this repository**,
  not from judgement, or it will drift again the moment a different person writes
  the next endpoint.
* Where recognised industry guidance exists, follow it rather than invent, so an
  experienced integrator and a code generator both behave as expected.
* The incident path must not acquire new failure modes. A precondition that can
  return `428` in the middle of a compromise response is a hazard, not a safeguard.
* A route that lets a caller reach an action they are not authorized to invoke
  directly is an authorization bypass regardless of how convenient it is.

## Considered Options

* **A. Codify a small set of standards-based rules**, each anchored to a source or
  to a fact in the repo, and align the design to them.
* **B. Codify rules chosen for internal consistency alone**, ignoring outside
  guidance.
* **C. Keep reconciling per endpoint** as differences are noticed.

## Decision Outcome

Chosen: **Option A.** Six rules follow. They bind the admin API design and the
published contract.

### 1. Revocation is `DELETE /{resource}/{id}`; a state transition stays a custom method

Revoking a session, token, authorization, or delegated-admin grant is expressed as
`DELETE` on that resource. A **soft delete behind `DELETE`** is conformant and is
what Nami does where a row is retained for audit.

State transitions keep the custom-method form `POST /{resource}/{id}/{verb}`:
`disable`, `enable`, `lock`, `unlock`, `reset-password`, `force-logout`, `suspend`,
`resume`. Bulk operations over a collection also stay `POST`, because they are not
the removal of one identified resource.

**The two URL shapes coexist deliberately.** This sentence exists so a later reader
does not tidy one into the other and call it a cleanup.

**What the source actually says, because the distinction matters.** AIP-136,
fetched and quoted 2026-08-01, states: *"Custom methods **should** only be used for
functionality that can not be easily expressed via standard methods; prefer standard
methods if possible, due to their consistent semantics."* That sentence is what
carries this rule: revoking a session **is** removing it, so a standard method
expresses it easily and is therefore preferred.

AIP-136 separately states that *"the verb in the name should not contain any of the
standard method verbs"*, and enumerates exactly five: Get, List, Create, Update,
Delete. **"Revoke" is not one of them**, so that second rule does not literally
reach this case and is not cited as though it does. Note also that both are
**"should"**, so this is guidance adopted deliberately, not a mandate being obeyed.

### 2. A collection is tenant-scoped if and only if its entity carries a tenant column

`/tenants/{tenantId}/{collection}` is correct exactly when the underlying entity is
**class A or class B** in the taxonomy under "Three classes of control-plane table"
in the data design (`docs/design/02-data.md`). `Application`, `Authorization`, and
`Token` are class A. `Memberships` and `DelegatedAdmin` are class B, and ADR-0084
already publishes the first of them at `/tenants/{tenantId}/memberships`.
**`Scope` does not qualify**: ADR-0001 makes the scope catalog global and it carries
no tenant column at all, so `/scopes` is a root collection. **Class C does not
qualify either**, for a different reason: a provisioning request runs before its
tenant is usable, so there is no tenant to name in the path, which is why tenant
creation is `POST /tenants`.

Placing a table with no tenant column under a tenant path misstates ownership, and
the mistake survives review because the path *reads* plausible.

**The anchor is the table class, not `.IsMultiTenant()`, and the correction is not
cosmetic (2026-08-01).** This rule first read "exactly when the underlying entity is
`.IsMultiTenant()`", which is true of class A and **false of class B by design**:
class B carries a real `TenantId` but is deliberately kept outside Finbuckle's filter
so that authorization queries can read it, and the data design says in the same table
that it "must not be 'fixed' into class A". Read literally, the `.IsMultiTenant()`
anchor evicted `Memberships` and `DelegatedAdmin` from the tenant path and so
contradicted ADR-0084. The rule had been written as though entities were binary,
tenant-scoped or global, while the data design defines three classes. The class
column is still checkable against the data design, which is what the original anchor
was reaching for.

Root-level **item** routes (`/applications/{id}`, `/users/{id}`) stay root-level and
are guarded by the object-level authorization filter instead. The rule differs
between collection and item because a collection has no identifier from which an
owner could be derived.

**The path parameter is named `tenantId` wherever the tenant is the scope, and `id`
only where the tenant is itself the resource being addressed.** So `/tenants/{id}`
and `PUT /tenants/{id}` take `id`, while every `/tenants/{tenantId}/{collection}`
takes `tenantId`. This is stated because it is not decorative on either side. The
route is a wire contract frozen under ADR-0044, so the name is public and changing it
later is a breaking change. And the admin design keys its tenant-scope authorization
policy to `{tenantId}` by name, so a route declared with a different parameter name is
not the route that policy is written against. It is stated here rather than left to
the examples because leaving it to the examples is what produced the drift recorded in
Confirmation below.

### 3. Paging is `?page=&pageSize=` with a body envelope, and no count header

The response carries `PageMeta { page, pageSize, total }` in the body. There is no
`X-Total-Count` header.

Either shape would have been defensible; what is not defensible is specifying both,
which is what the design did. A count in a header is also invisible to a typed
client generated from the schema, which is the consumer this surface is shaped for.

### 4. `If-Match` is required on a state edit and deliberately absent on a revocation

| Kind | `If-Match` | Why |
|---|---|---|
| **State edit**: `PUT`, and a `DELETE` that raises a destructive proposal against an existing target (application, scope, tenant, secret) | **required**; absent gives `428`, mismatch `409` | An approver must be approving the object they looked at. This is the same guarantee the dual-control target guard makes at execution time (ADR-0081) |
| **Revocation**: `DELETE` on a session, token, authorization, or delegated-admin grant | **deliberately absent** | This is the incident path. A `428` while cutting off a compromised principal is a hazard, and revocation only ever reduces privilege, so a lost update here cannot escalate anything |

The split is by **intent, not by verb**, which is why it cannot be stated as "every
`DELETE`" or "every mutation". The blanket phrasing is the specific thing this rule
replaces: it was written as a universal, several endpoints correctly did not follow
it, and the universal statement is what was wrong rather than those endpoints.

### 5. There is no generic proposal-creation route

A dual-control proposal is created **only by the destructive endpoint it belongs
to**, which has already run that route's policy and capability check.

A generic `POST /proposals` accepting a caller-supplied action type and target would
let a caller raise a proposal for an action **whose own endpoint they are not
authorized to call**, which an approver would then execute in good faith. The
approval saga protects the *executor*; it does not re-run the *endpoint's*
authorization, and nothing downstream would notice the difference. Reading and
managing proposals (`GET`, approve, reject, cancel) is unaffected: those operate on
a proposal that some authorized endpoint already created.

### 6. Errors are RFC 9457 problem details with a machine-readable code

Unchanged from the current design, recorded here so the convention set is complete
rather than partially written down.

### Consequences

* Good, because the two recurring questions now have answers derivable from a
  recorded fact, so the next endpoint does not reopen them.
* Good, because rule 5 closes an authorization bypass, which is a security outcome
  rather than a consistency one.
* Good, because the surface matches published guidance, so a generated client and an
  experienced integrator behave as expected.
* Bad, because the admin API design needed changing in five places, and any contract
  already drafted against the old shapes has to move with it. That cost is lowest
  now and rises with every endpoint written.
* Neutral, because two URL shapes coexist by design. To a reader who does not know
  AIP-136 that reads as inconsistency, which is why rule 1 says so explicitly.

### Confirmation

* **AIP-136 was fetched and quoted at source on 2026-08-01**, and the quotation
  corrected an inherited over-claim: the design corpus this rule came from argued
  that "revoke is itself a synonym of a standard verb", but AIP-136's verb rule
  enumerates five verbs and revoke is not among them. The rule stands on the
  prefer-standard-methods sentence instead. Recorded because the wrong source for a
  right conclusion is the defect shape this repository has paid for most often.
* **Five drifts were found in the admin API design when it was read against these
  rules**, and all five are fixed in the same change as this ADR: revocation was
  `POST /{id}/revoke` on authorizations, tokens, and sessions; the scope collection
  sat under a tenant prefix while ADR-0001 makes the catalog global, so the design
  contradicted an accepted ADR rather than merely a convention; paging specified
  `?page=&size=` with `X-Total-Count`; `If-Match` was stated as required on **every**
  mutation, which would have put a `428` on the revocation path; and `POST /proposals`
  existed.
* **A sixth drift was found on 2026-08-01, and the sentence above is why it took a
  second pass.** "Five drifts were found, and all five are fixed" is a true record of
  what that pass caught and reads as a statement that the design was then clean. It
  was not: the tenant path parameter was spelled **three ways** in one document,
  `{t}` five times, `{id}` on four sub-collection routes, and `{tenantId}` in the two
  places that state the authorization rule (`docs/design/15-admin-api.md`). Nine route
  declarations therefore disagreed with rule 2 above, with ADR-0084's
  `/tenants/{tenantId}/memberships`, and with two sentences in their own file. The
  first pass read the design against the rules **as the rules were written**, and the
  parameter name was only ever shown in examples, never stated, so there was nothing
  to read it against. That gap is closed by the naming paragraph in rule 2. The count
  in the bullet above is left as written, because it is what that pass found; what is
  corrected is treating a count of findings as a measure of coverage.
* Tests: a contract check that every published path is registered and every reference
  resolves; an assertion that the `If-Match` split matches this table rather than a
  blanket rule; and a negative test that no route accepts a caller-supplied proposal
  action type.

## Pros and Cons of the Options

### A. Standards-based rules anchored to recorded facts (chosen)

* Good, because each rule cites either an external source or a fact in this
  repository, so it can be checked rather than debated.
* Good, because when the contested questions were decided on evidence rather than
  preference, the answers were consistent with published practice rather than
  against it.
* Bad, because it makes the prose design derived from the rules, so an implementer
  cannot settle a shape locally any more.

### B. Internally consistent rules only

* Good, because it needs no external research and cannot be invalidated by someone
  else's guidance changing.
* Bad, because it produces a surface that surprises every integrator who has used a
  comparable API, and a generated client cannot benefit from conventions the
  generator does not know.

### C. Reconcile per endpoint

* Good, because it needs no up-front decision and never blocks a new endpoint.
* Bad, because it is what produced the five drifts above, including one that
  contradicts an accepted ADR and one that is an authorization bypass. It also
  re-litigates the same two questions on every new resource.

## More Information

* **Google AIP-136, Custom methods**, fetched and quoted 2026-08-01. Related:
  AIP-135 (Delete), for soft-delete behind `DELETE`.
* Mechanism and the endpoint-by-endpoint surface: design
  [15](../design/15-admin-api.md). This ADR fixes the rules; that document applies
  them and remains the implementer source for the surface itself.
* Rule 2 is anchored to the three-class table taxonomy in design
  [02](../design/02-data.md), not to an ADR, because the class is a property of each
  table that the data design already enumerates. ADR-0001 is what puts `Scope` outside
  all three classes by making the catalog global, which is the one exclusion rule 2
  states by name.
* Related decisions: ADR-0020 (the two deployables, which is what makes a
  contract change expensive), ADR-0081 (the dual-control target guard that rule 4
  pairs with at execution time), ADR-0044 (the SemVer, deprecation and port-evolution
  discipline for the **assemblies and the wire DTO types**), ADR-0090 (the versioned base
  path the relative route templates in this ADR's design hang off, and which route families
  carry a version), ADR-0080 (probe routes, deliberately outside these rules).
* **Correction, 2026-08-01: the parenthetical on ADR-0044 above read "the public-surface
  versioning discipline this sits inside", and that is a citation which resolves without
  supporting the claim.** Read at source, ADR-0044 contains **no** occurrence of route,
  endpoint, HTTP, verb or status code, and its only URL-shaped string is the
  `<migration url>` placeholder in its section C example. Its section F versions the DTO
  **assembly** and its `V1` namespace, which is a set of types rather than a URL. So these
  conventions did not sit inside any recorded versioning discipline for the URL at all:
  that is the same hole ADR-0087 found on the locking side and ADR-0090 closes on the
  scheme side. Corrected here rather than rewritten silently, because a true fact with the
  wrong owner is the defect shape this repository has paid for most often.
* Imported from the design corpus's HTTP-conventions decision on 2026-08-01, with
  the AIP-136 citation re-read at source and narrowed, and rule 5 kept verbatim in
  substance because it is a security rule rather than a style one.
