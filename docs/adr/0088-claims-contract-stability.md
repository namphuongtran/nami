---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0044 (the versioned-surface discipline this joins), ADR-0085 (the precedent for freezing a consumer-facing name set, whose shape this decision mirrors in reverse), ADR-0087 (the sibling surface decided the same day), ADR-0075 (the deny-by-default destination invariant and its published port contract test, read at source on 2026-08-01), ADR-0005 and ADR-0013 and ADR-0001 and ADR-0019 (the decisions that each own one row of the table), and the design corpus's adopter-view section 14, which asked for exactly this
informed: integrators and relying-party authors, resource-server implementers, Admin API implementers, release managers
---

# Freeze the claims contract as a consumer surface, and promise only the half Nami owns

## Context and Problem Statement

An integrator's code does not call Nami's .NET API and often does not call its Admin API.
It reads **claims out of a token**: it iterates `memberships[].tid`, it branches on
`acr == "urn:nami.identity:aal2"`, and a shared-host resource server isolates on `tenant`.
Those are the strings their code is written against.

Two decisions already cover the neighbouring surfaces. ADR-0044 section A locks the .NET
types, and ADR-0087 locks the HTTP surface. ADR-0044 section G goes further and declares a
**third** class of consumer-facing name to be contract, telemetry instrument names, on the
reasoning that "renaming them breaks dashboards". Claims are that same class and are absent
from the list.

**The shape here is the mirror image of ADR-0085 and is worth naming as such.** There, the
rule existed (section G said telemetry names were contract) and no document said what the
names were, so ADR-0085 supplied the list. Here the **list already exists and is complete**:
design [09](../design/09-federation-and-claims-profile.md) section 5.2 is "the single
definition of every bespoke Nami claim", seven rows with JSON shape, destination, producer
and consumer for each, plus an eighth name, `memberships_truncated`, defined inside the first
row. What is missing is the rule: nothing anywhere says that table will not change under
someone.

An integrator said so independently. The design corpus's adopter-view document asks for "a
published integrator claims contract with a stability promise, versioned separately from the
internal DD", because "today the shapes live in `DD-06`, an internal document. An adopter
needs to know which claims will not change under them" (read 2026-08-01, recorded in
`docs/kb/notes/adopter-feedback-on-the-integrator-surface.md`).

**One control exists and it points one way.** ADR-0075 makes claim destinations
deny-by-default through the single `IClaimsProfileService` choke point, and publishes an
executable port contract test whose assertion is "given a claim with no declared
destination, when a token is issued, then the claim is absent from it". That catches a claim
that **should not be there**. It cannot catch a claim that should be there and is not, a
destination that was narrowed, or a claim that was renamed, because after any of those the
new state is also fully declared and the test passes.

## Decision Drivers

* The claims are the surface an integrator's code actually branches on, and the only one of
  the three with no stability statement.
* **Nami does not own all of it.** Five of the seven names come from OIDC Core and RFC 8176.
  A contract that promises what its author does not control is worth less than a narrower
  one, because the first time the spec moves, the whole promise is suspect.
* Two precedents already exist for this exact problem. A third parallel discipline would be
  worse than extending one of them.
* The existing deny-by-default control covers one direction well. The decision should add
  the direction that is missing rather than restate the one that is not.

## Considered Options

* **A. Leave it in design 09** as an internal design document, as today.
* **B. Freeze the whole table uniformly**, all seven rows promised alike.
* **C. Freeze it with the promise split by who owns the name.**

## Decision Outcome

Chosen option: **C**, because it is the only one that produces a promise Nami can actually
keep, and because a uniform freeze would quietly commit this project to the stability of
documents it does not write.

* **A. The contract of record is design 09 section 5.2, and it is a published consumer
  surface from v1.0.** It joins the surfaces already under ADR-0044: the .NET types in
  section A, the telemetry names in section G with ADR-0085 supplying their list, and the
  HTTP surface under ADR-0087.

* **B. The promise splits by who owns the name, and the split is the substance of this
  decision.**

  | Claims | What Nami promises |
  |---|---|
  | `memberships`, `memberships_truncated`, `tenant` | **Everything.** The name, the JSON shape, and the destination set are Nami's, and they are frozen |
  | `acr`, `amr`, `auth_time`, `idp`, `sid` | **That it emits them, to which tokens, and the values it supplies where those values are Nami's.** Above all `urn:nami.identity:aal1`, `aal2` and `aal3`, which are Nami-owned identifiers and are frozen exactly as ADR-0085 freezes an instrument name. Nami does **not** promise the syntax or meaning of a standard claim, because OIDC Core and RFC 8176 own those |

  The second row is the honest half. `amr` carrying no `external` and no `passkey` value is
  a consequence of RFC 8176 defining neither, recorded in design 09; it is conformance, not
  a Nami promise, and if the RFC gains a value the design revisits it without this ADR
  having been broken.

* **C. Change classification uses ADR-0044 section B's existing scale**, with one case that
  the scale does not reach.
  * **MINOR**: adding a claim; adding an optional field to a Nami-owned claim's object.
  * **MAJOR**: removing a claim, renaming one, changing its JSON type, **removing a
    destination**, or changing an `acr` URN value.
  * **Widening a destination is not a SemVer break and still requires an ADR.** Putting an
    existing claim onto a token it was not on before breaks no consumer's code, so SemVer
    classifies it MINOR and waves it through. But ADR-0005 keeps the access token minimal
    and readable, so this is a data-protection decision wearing a versioning costume. It is
    called out because classifying it correctly by SemVer is exactly how it would ship
    unreviewed.

* **D. The lock is the missing direction of an assertion that already exists in one.**
  ADR-0075's port contract test proves that **no undeclared claim reaches a token**. This
  ADR adds the converse, asserted per claim: **every claim in the table reaches exactly its
  declared destinations and no others**. The pairing is deliberate and is the same structure
  ADR-0087 settled on for its own surface, one control for the thing that should not be
  there and one for the thing that should, because a single control that covers one
  direction reads as covering both.

* **E. Publication waits for the layer that will carry it, and the wait is named rather than
  left blank.** The ask was for a contract an adopter can read without reading internal
  design, and `docs/README.md` records that the adopter-facing layer is planned and not
  built. Until it exists, design 09 section 5.2 **is** the contract of record and is marked
  as such in the document itself. When that layer is built the table is published there and
  design 09 keeps the mechanism, so there is one list in two places rather than two lists.

### Consequences

* Good, because the surface an integrator actually reads is now the one with the clearest
  statement of what may change, rather than the only one with none.
* Good, because it costs no new list. The table exists, is complete, and is already the
  place other designs reference, so this decision adds a rule to a list rather than a
  second source of truth.
* Good, because the split promise survives contact with a specification change. A future
  RFC 8176 value is absorbed by design 09 without this contract having been broken.
* Bad, because a split promise is more to explain than "the table is frozen", and the
  second row will be misread by someone as a weaker guarantee than it is. It is not weaker;
  it is bounded to what Nami controls.
* Bad, because the converse assertion in section D cannot run before M1, like every other
  test-shaped control in this repository at this stage.
* Neutral, because the standard-claim half inherits the stability of documents Nami does not
  control, which is a property of using standards rather than a cost of this decision.

### Confirmation

* **The list was read at source on 2026-08-01**: seven rows in design 09 section 5.2, plus
  `memberships_truncated` defined inside the `memberships` row. Five of the seven names are
  specification-owned, which is what produced the split in section B rather than a uniform
  freeze.
* **The one-directional limit was read at source, not inferred.** ADR-0075 section on port
  contract tests states the assertion as "given a claim with no declared destination, when a
  token is issued, then the claim is absent from it". Nothing in that assertion constrains a
  declared claim, which is the gap section D closes.
* **A wrong-owner citation was found while writing this, and is fixed in the same change.**
  Design 09 said that adding a claim means editing the table and the `GetDestinations`
  switch together, "which the regression test in [20] enforces by asserting that an
  undeclared claim reaches no token". Design 20 contains no such row and **no mention of
  ADR-0075, port contract tests, or the choke point at all**. The assertion is ADR-0075's
  published port contract test, not design 20's catalogue. The citation resolved and did not
  support, which is the defect shape this repository has paid for most, and it is corrected
  here along with the missing catalogue row.
* Tests at M1: the converse assertion of section D, per claim; a negative branch that
  narrows one destination and is shown red; and the existing ADR-0075 assertion unchanged,
  since this decision adds to it rather than replaces it.
* **No pre-GA ratification entry.** That checklist consolidates policies and sign-offs owned
  by a human. This decision defers no policy: the split in section B is settled here, and
  the classification in section C defers to rules ADR-0044 already carries.

## Pros and Cons of the Options

### A. Leave it in design 09 as an internal design

* Good, because it is free and the table is already accurate and referenced.
* Bad, because accuracy is not stability. An integrator can read the table today and still
  has no answer to "will this change under me", which is the question that was actually
  asked.
* Bad, because it leaves claims as the only one of three consumer surfaces with no rule,
  which is an inconsistency that reads as an oversight and, until now, was one.

### B. Freeze the whole table uniformly

* Good, because it is one sentence and easy to communicate.
* Bad, because it promises the syntax of five claims that OIDC Core and RFC 8176 define.
  Nami cannot keep that promise and does not need to make it.
* Bad, because the first specification change would force either a false MAJOR or a quiet
  exception, and a contract with one quiet exception is not consulted again.

### C. Freeze with the promise split by ownership (chosen)

* Good, because every clause is one Nami can keep.
* Good, because it puts the Nami-owned `acr` URN values inside the frozen half, where
  ADR-0085's reasoning already places identifiers of exactly that kind.
* Bad, because it needs a table to explain rather than a sentence.

## More Information

* Related decisions: [ADR-0044](0044-public-api-stability-and-semver.md) (the versioned
  surface this joins, and the SemVer scale section C uses),
  [ADR-0085](0085-telemetry-instrument-naming.md) (the same problem in reverse, and the
  precedent for freezing Nami-owned identifiers),
  [ADR-0087](0087-http-surface-snapshot-gate.md) (the sibling surface, and the source of the
  two-control structure in section D),
  [ADR-0075](0075-security-sensitive-port-invariants.md) (the deny-by-default invariant and
  the existing half of the assertion),
  [ADR-0005](0005-encryption-credential-lifecycle.md) (which claims exist and how small they
  stay, and the reason a widened destination is a decision),
  [ADR-0013](0013-mfa-assurance-and-step-up.md) (the `acr` and `amr` producer),
  [ADR-0001](0001-multi-tenant-isolation-model.md) (exactly one `tenant` claim on an access
  token), and [ADR-0019](0019-single-logout-strategy.md) (`sid` on the id_token and the
  logout token).
* The list itself: design [09](../design/09-federation-and-claims-profile.md) section 5.2,
  which remains the contract of record and the place a claim is added or changed.
* The adopter request that prompted this, with its source and date:
  `docs/kb/notes/adopter-feedback-on-the-integrator-surface.md`.
