---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0079 (the admin surface's conventions, whose scope statement is what leaves this surface ungoverned), ADR-0065 (the naming rules, which cover three identifier classes and not URL paths), ADR-0038 (the anti-enumeration requirement that names two of these endpoints), ADR-0044 and ADR-0087 (the versioning and lock discipline a route falls under once released), design 08 section 5.7 and design 10, read at source on 2026-08-01
informed: implementers of the user-facing endpoints, the login and account UI, security reviewers checking whether the rejected framework surface is mounted
---

# Give the self-service surface its own conventions, while it still has one endpoint

## Context and Problem Statement

Design [08](../design/08-user-management.md) section 5.7 names a surface: self-service over
**profile, email and phone, MFA and passkey and password, sessions, and membership**, served
by "custom endpoints, not `MapIdentityApi`", because `MapIdentityApi` "exposes `/register`,
`/login`, and similar as a parallel JSON attack surface that bypasses the UI flow,
anti-enumeration, and the challenge layer".

**Nothing governs its shape.** ADR-0079 fixes the HTTP conventions for the *admin* surface
and says so in its own first sentence, "the admin surface is large and still being written".
ADR-0065 fixes casing for three identifier classes, URNs, authorization capability
identifiers, and dual-control action types, and says nothing about URL path segments.

**The surface has one declared route.** An inventory of every route-shaped string in the
design layer on 2026-08-01 found five distinct route families: the OAuth and OIDC protocol
endpoints, the health probes, the BFF, the Admin API, and this one. This one contained
exactly **one** declared route, `GET /me/memberships`, added that same day after four
documents were found depending on an endpoint none of them declared. Two more route names
appear with real requirements attached and no declaration: `/forgotPassword` and
`/resendConfirmationEmail` carry anti-enumeration and latency-invariance obligations in
ADR-0038 section D and design 10, and are declared nowhere.

**And the naming has already drifted, toward the thing this surface exists to avoid.**
`/forgotPassword` and `/resendConfirmationEmail` are `MapIdentityApi`'s own route names, in
its camelCase. Design 08's hardening bullet manages to name both in the same sentence as the
claim that "no `MapIdentityApi` surface exists". Every path segment on the admin surface is
lowercase kebab-case, verified in both directions on 2026-08-01: `chain-status`,
`cors-origins`, `delegated-admin`, `revoke-all`, and no camelCase segment anywhere.

That last point is not a style complaint. A security reviewer asking the one question this
design cares about, *is the parallel JSON attack surface mounted*, would look at the route
list, see `MapIdentityApi`'s exact names, and reasonably conclude the wrong answer.

**The timing is the argument.** ADR-0079 was written after five drifts had accumulated in a
large surface, and its own Context calls deciding endpoint by endpoint "how a surface becomes
inconsistent without anyone ever making a decision they would defend". This surface has one
endpoint. The same decision costs nothing today and is a breaking change under ADR-0044 and
ADR-0087 once released.

## Decision Drivers

* A route is a wire contract from v1.0. Deciding the shape while there is one endpoint is the
  cheapest this will ever be, and the drift has already started.
* ADR-0079's rules were reasoned for a surface with tenant prefixes, id-routes behind an
  object-level filter, and a dual-control saga. **This surface has none of those**, so
  adopting it wholesale would import rules whose reasons do not reach here.
* Equally, a second full set of conventions for a five-area surface would be its own tax.
  What is wanted is adoption where the reason transfers and a stated divergence where it does
  not.
* The one rule that is load-bearing here is a **security** rule, not an aesthetic one, and it
  should be recorded as such.

## Considered Options

* **A. Extend ADR-0079 to cover both surfaces.**
* **B. Adopt ADR-0079 wholesale by reference**, with no divergences recorded.
* **C. A separate decision that adopts what transfers and states what diverges.**
* **D. Wait until the surface is large enough to have a shape worth deciding.**

## Decision Outcome

Chosen option: **C**, because the two surfaces genuinely differ on the points that matter
most, and because writing the divergences down while there is one endpoint is what stops
them being discovered as inconsistencies later.

* **1. Identity comes from the token. There is no identifier in a self-service path.**
  Every route is rooted at `/me` or is a verb with no subject. `GET /me/memberships`, not
  `GET /users/{id}/memberships`.

  **This is the security rule and it replaces ADR-0079 rule 2 outright.** The admin surface
  can safely publish `/users/{id}` because it has a deny-by-default object-level
  authorization filter that loads the object and derives its owner (design 15 section 5.1).
  This surface has no such filter. An identifier in a self-service path therefore means
  either duplicating that machinery or shipping the BOLA hole it exists to prevent, and the
  `/me` form makes the question unaskable rather than answered correctly.

* **2. Path segments are lowercase kebab-case**, as every admin segment already is. So
  **`/forgot-password`** and **`/resend-confirmation-email`**, not the camelCase names
  inherited from the framework feature this surface rejects. ADR-0038's anti-enumeration
  requirement is unchanged and its two endpoint references are re-spelled in the same change;
  ADR-0038 decides the *behaviour* of those endpoints and never decided their names, which is
  the gap this rule fills rather than a decision it overturns.

* **3. Anti-enumeration is a property of the surface, not of two endpoints.** Any route that
  accepts a caller-supplied identifier for an account, an email address or a phone number,
  returns a constant response and constant latency whether or not the account exists
  (ADR-0038 section D). Stating it here means the next endpoint inherits it instead of
  rediscovering it, and it is the reason this surface may not adopt the admin surface's
  freedom to distinguish a `404` from a `403`.

* **4. Step-up replaces dual control.** This surface has no proposals, so ADR-0079 rules 4
  and 5 do not reach it. Its analogue is the assurance gate: an action that changes a
  credential or a recovery path requires `acr >= aal2` and answers a failure with the RFC
  9470 `401` challenge, the same challenge shape the admin surface uses, so a client
  implements one behaviour for both.

* **5. What transfers from ADR-0079 unchanged**, because the reasoning is about HTTP rather
  than about admin: revocation is `DELETE /{resource}/{id}` and a state transition is a
  custom method (rule 1), which is how a user ends one of their own sessions; paging is
  `?page=&pageSize=` with a body envelope if any collection here ever needs it (rule 3); and
  errors are RFC 9457 problem details with a machine-readable code (rule 6).

* **6. The boundary is stated, because five route families exist and only one is this one.**
  These rules do not govern the OAuth and OIDC protocol endpoints, whose shapes belong to
  their specifications and to OpenIddict; nor the BFF endpoints (ADR-0029); nor the health
  probes (ADR-0080); nor the Admin API (ADR-0079). A route belongs to this surface when it is
  called by an authenticated end user acting on their own account.

### Consequences

* Good, because the rule that matters most here is written down as a security rule with its
  reason, rather than as a convention someone may later "tidy" into an id-route.
* Good, because the route names stop advertising a framework surface the design rejects,
  which removes a false signal from exactly the review that matters.
* Good, because it costs two renames today. After M1 the same change is a breaking change
  under ADR-0044 and would appear in the ADR-0087 snapshot diff as one.
* Bad, because it decides the shape of endpoints that do not exist yet, and a rule written
  before its cases can turn out to fit them badly. Bounded by keeping the rules to five,
  each anchored either to an existing ADR or to an existing property of this surface.
* Bad, because it touches an accepted ADR's text. ADR-0038's two endpoint references are
  re-spelled, which is a small edit to a document whose own decision is untouched, and the
  reason is recorded in both places.
* Neutral, because the surface may grow to need more than this. If it does, the pattern is
  ADR-0079's: state the rule, anchor it to a source, and record the drifts the rule found.

### Confirmation

* **The inventory was run, not estimated.** Every route-shaped string in `docs/design/` on
  2026-08-01, grouped: five route families, and one declared route on this surface.
* **The casing claim was verified in both directions**: no camelCase path segment exists in
  the admin design, and the kebab form is present in real routes (`chain-status`,
  `cors-origins`, `delegated-admin`, `revoke-all`). The one apparent counter-hit,
  `private-IP`, is prose rather than a route.
* **The naming drift was read at source, including the sentence that contradicts itself**:
  design 08 lists anti-enumeration "on `/forgotPassword` and resend" and closes the same
  bullet with "no `MapIdentityApi` surface exists", while both names are `MapIdentityApi`'s.
* **ADR-0065 was checked before claiming a gap**: it sets casing for URNs, capability
  identifiers, and dual-control action types, and has no rule for URL path segments.
* Tests at M1: an assertion that no self-service route template contains a route parameter
  naming a subject; the ADR-0038 latency-invariance test, unchanged but now surface-wide; and
  a negative test that a self-service route cannot be reached with another user's identifier,
  which under rule 1 should be unexpressible rather than merely denied.
* **No pre-GA ratification entry.** Nothing here defers a policy or a sign-off to a human
  owner; the rules are settled in this document.

## Pros and Cons of the Options

### A. Extend ADR-0079 to cover both surfaces

* Good, because there would be one document to read.
* Bad, because ADR-0079's central rule is about tenant prefixes and its other two structural
  rules are about the dual-control saga, none of which exists here. Extending it means
  qualifying most of its rules with "except on the self-service surface", which is harder to
  read than two documents.
* Bad, because it would rewrite an accepted ADR whose own scope statement is correct.

### B. Adopt ADR-0079 wholesale by reference

* Good, because it is one sentence.
* Bad, because it would import rule 2 and therefore permit `/users/{id}` shapes on a surface
  with no object-level filter, which is the one outcome this decision exists to prevent.

### C. Adopt what transfers, state what diverges (chosen)

* Good, because each rule carries the reason it applies here, so a later reader can tell a
  deliberate divergence from an oversight.
* Good, because it is the only option that records the `/me` rule as security rather than as
  style.
* Bad, because it is a second document, and two documents can drift. Bounded by rule 5 naming
  exactly what is adopted rather than restating it.

### D. Wait until the surface is larger

* Good, because rules written against real endpoints fit them better.
* Bad, because it is what ADR-0079 already paid for: that ADR was written after five drifts,
  and this surface has produced its first two before reaching two declared endpoints.
* Bad, because waiting past M1 turns a free rename into a breaking change.

## More Information

* Related decisions: [ADR-0079](0079-admin-api-http-conventions.md) (the sibling surface,
  the source of rules 5 and the reason 1 differs),
  [ADR-0065](0065-coding-and-naming-conventions.md) (the identifier casing rules this extends
  to URL paths), [ADR-0038](0038-email-notification-subsystem.md) (anti-enumeration, whose
  two endpoint names are re-spelled by rule 2),
  [ADR-0044](0044-public-api-stability-and-semver.md) and
  [ADR-0087](0087-http-surface-snapshot-gate.md) (what a route becomes once released),
  [ADR-0029](0029-bff.md) and [ADR-0080](0080-health-and-readiness-probe-contract.md)
  (two of the four surfaces rule 6 excludes).
* Mechanism and the endpoints themselves: design
  [08](../design/08-user-management.md) section 5.7, which owns this surface, with design
  [10](../design/10-email-notification.md) for the anti-enumeration behaviour.
