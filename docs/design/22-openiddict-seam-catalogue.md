---
status: draft
created: 2026-07-27
tags: [design, seams, version-adaptation, openiddict, contract-tests]
---

# The engine seam catalogue and version adaptation (detailed design)

> **Sits under:** [architecture: component view](../architecture/08-component-view.md) and
> [schema migration and evolution](../architecture/15-schema-migration-evolution.md).
> **Implementer source of record:** this document, for the seam registry, the risk tiers,
> the contract-regression mapping, and the per-bump playbook. The **mechanism** behind each
> seam belongs to its owning design, named in the registry; this document never restates a
> mechanism, it registers it.

ADR-0021 does not merely recommend a catalogue, it names one as a deliverable: *"Maintain
an OpenIddict seam catalogue (a deliverable design document) listing every dependency on
OpenIddict (S1-S34), each tagged with a risk tier ... pointing to a source-verify file, a
contract test, an isolation port, and a decommission marker."* This is that document.

The problem it solves is specific. Nami pins one engine version and depends on it in
thirty-four distinct places, and those places are not equally safe. Some are documented API
that breaks loudly with a release note. Some are internal behaviour that breaks **silently**,
where a bump changes a sort order or removes an internal throw and nothing fails until a
token is wrong in production. A catalogue that does not distinguish those two is decoration.

## 1. Decisions realized

| Decision | What this design applies |
|---|---|
| ADR-0021 | The whole document: pin rather than float, the seam registry with risk tiers, per-bump contract regression, isolation ports, decommission markers, and the migration playbook |
| ADR-0024 | The two extension axes this catalogue presumes, adapter-behind-port and handler-at-named-position, detailed in [01](01-foundations.md) |
| ADR-0030 | The sibling upgrade playbook: a .NET major bump drags the engine and ORM with it, so both share one contract-regression suite |
| ADR-0011 | The rotation seam family, the archetypal endorsed-undocumented dependency |
| ADR-0014 | The sender-constraint seam family, all of it built rather than native |
| ADR-0018 | The multi-tenant composition seams, the highest-risk adjacent-stack group |
| ADR-0019 | The back-channel-logout interim and its decommission marker |
| ADR-0022 | The telemetry seam, built because the engine emits none |
| ADR-0075 | Why a security-sensitive seam's invariant survives an adapter swap |

## 2. Purpose and scope

In scope: the risk-tier taxonomy, the registry of all thirty-four seams with owner and
contract test, the contract-regression mapping, the per-bump playbook, the decommission
rules, and the readiness work for the next major.

Out of scope, and this boundary is the document's discipline: **every seam mechanism**.
How the rotation monitor works is [12](12-key-management.md); how the sender-constraint
handlers work is [06](06-sender-constrained-tokens.md); the endpoint model is
[04](04-core-protocol.md); the multi-tenant composition is [02](02-data.md) and
[18](18-tenant-lifecycle.md); revocation and cache behaviour is
[13](13-revocation-propagation-and-caching.md); the extension axes and the port catalogue are
[01](01-foundations.md); the CI wiring is [21](21-cicd-and-deployment.md); the test
inventory is [20](20-testing.md). A registry that starts explaining mechanisms becomes a
second source of truth for ten designs, and then the two disagree.

**A naming warning that matters more than it looks.** The design corpus contains two
different tables both called the seam catalogue: one registers engine dependencies as
`S1`-`S34`, the other maps a commercial product's extensibility interfaces to equivalents
as `#1`-`#34`. They are unrelated: number fourteen is a cache-refresh interval in the first
and a proof-replay cache in the second. **ADR-0021 uses the `S`-numbering**, so this
document does too, and the extension-point material from the other table lives in
[01](01-foundations.md) restated as Nami's own surface.

## 3. Interfaces and contract

This design declares no new runtime interface. Its contract is a **registry schema**: what
a row must carry before a seam counts as registered.

```mermaid
classDiagram
  class Seam {
    <<registry row>>
    +string Id
    +RiskTier Tier
    +string OwnerDesign
    +string IsolationPort
    +string ContractTestId
    +string SourceRecord
    +string DecommissionMarker
  }
  class RiskTier {
    <<enumeration>>
    T1 native
    T2 endorsed undocumented
    T3 internal behaviour
    T4 handler order insertion
    T5 build interim
    T6 adjacent stack
  }
  class ContractTest {
    +Assert behaviour on the pinned version
    +Run on every bump
    +Fail the build on change
  }
  Seam --> RiskTier
  Seam --> ContractTest : exactly one
```

A row is incomplete, and the seam therefore unregistered, unless it names an **owner
design**, a **contract test**, and a **source record**. The isolation port is required only
for a build-interim, and the decommission marker only where a native replacement is
plausible. An unregistered dependency is the failure this document exists to prevent,
because it is the one nobody re-verifies on a bump.

## 4. Data and structure

No relational table. The structure is the registry itself, in section 5.2.

## 5. Behaviour

### 5.1 Risk tiers, and why the tier picks the strategy

The tier is not a severity label. It answers one question: **how will this break, loudly or
silently?** That determines what a bump has to do about it.

| Tier | What it depends on | How it breaks | Required strategy |
|---|---|---|---|
| **T1** native | documented, versioned API | loudly, with a release note | read the changelog, keep a light contract test |
| **T2** endorsed-undocumented | a maintainer-endorsed pattern that carries no version guarantee | **silently** | a contract test is **mandatory**, and a fallback must already be recorded |
| **T3** internal behaviour | a sort order, an internal throw, an unfiltered iteration | **silently** | a contract test asserting the specific behaviour, not the feature |
| **T4** handler-order insertion | sitting a handler beside a built-in one | medium, an order can shift within a minor | anchor by named descriptor, and snapshot the resolved order |
| **T5** build-interim | something the engine does not provide yet | not at all technically; it accrues debt | isolate behind a port, carry a decommission marker |
| **T6** adjacent stack | a co-versioned library, not the engine | medium, the composition breaks | pin in lock-step, test the composition |

The distinction that pays for the whole taxonomy is **T1 against T2 and T3**. A T1 break
arrives with documentation. A T2 or T3 break arrives as a wrong token.

### 5.2 The registry

Thirty-four seams. **Owner** is the design that holds the mechanism; **test** is the
contract test that must pass on every bump.

**Signing and key rotation** (owner [12](12-key-management.md), ADR-0011 and ADR-0012)

| # | Seam | Tier | Isolation | Test | Decommission |
|---|---|---|---|---|---|
| S1 | A custom options monitor drives no-restart rotation, an endorsed pattern with no version guarantee | **T2** | `ISigningKeyStore` plus the key cache and the monitor | rotation contract test | none, this is a permanent seam |
| S2 | The signer is whichever credential is **first** in the collection, with no selection logic in the engine | **T3** | follows S1 | rotation contract test (a) | none |
| S3 | The key-set endpoint iterates **every** credential with no validity-window filter | **T3** | follows S1 | rotation contract test (b) | none |
| S4 | The local validation stack snapshots keys at startup and refreshes on a change token, not per request | **T3** | a custom change-token source, and a test that trips it | rotation contract test (c) | none |
| S5 | Loading a signing certificate from a stream accepts PKCS#12 only, not PEM | T1 | the adapter converts | key-load test | none |

**Sender-constrained tokens** (owner [06](06-sender-constrained-tokens.md), ADR-0014)

| # | Seam | Tier | Isolation | Test | Decommission |
|---|---|---|---|---|---|
| S6 | Stamping the confirmation claim at issuance, which the engine does not do for this mechanism | **T5** | the issuance handler | spike A-1 plus the contract test | no committed native |
| S7 | The serializer must emit a **nested** confirmation object from a principal claim | **T3** | the issuance handler | the highest-risk contract test of this group | none |
| S8 | The built-in token extractor is hard-coded to one scheme, so a sibling extractor is inserted beside it | **T4** | the sibling extractor, one order before the built-in | the sender-constraint contract test | no committed native |
| S9 | The built-in proof handler recognises only the certificate thumbprint and **throws** on a key thumbprint | **T3** | the custom proof handler, ordered before it | the same test, plus the throw-avoidance assertion | no committed native |
| S10 | Surfacing the key thumbprint in the introspection response, where the engine covers only the certificate case | **T5** | the issuance handler and introspection | introspection contract test | no committed native |

**Validation, revocation, and caching** (owner [13](13-revocation-propagation-and-caching.md) and [05](05-resource-server-validation.md))

| # | Seam | Tier | Isolation | Test | Decommission |
|---|---|---|---|---|---|
| S11 | The per-request entity cache: scoped, bounded, no time-to-live, invalidated locally | **T3** | none needed, there is no backplane | cache-behaviour test | none |
| S12 | Token-entry and authorization-entry validation are enabled on the **validation** builder, not the server builder | **T3**, and a wrong-API case | none | conformance test | none |
| S13 | The refresh reuse leeway defaults to 30 seconds | T1 | set the value explicitly | refresh-concurrency test | none |
| S14 | Family revoke happens **inside** entry validation and is on by default, so re-implementing it double-revokes | **T3** | none, and deliberately no re-implementation | default-on contract test | none |
| S15 | The key-refresh intervals of the token-validation library, a 12-hour automatic refresh with a 5-minute floor | **T6** | the distrusted-key set, and a shortened interval | propagation test | none |

**The endpoint model** (owner [04](04-core-protocol.md)), the wrong-API class

| # | Seam | Tier | Isolation | Test | Decommission |
|---|---|---|---|---|---|
| S16 | Which endpoints are pass-through and which are fully handled | **T1**, and the wrong-API class | none, confirm before writing code | conformance suite | none |
| S17 | Endpoint URIs must be set explicitly; only discovery and the key set are auto-pathed | T1 | configuration | conformance suite | none |
| S18 | The end-session rename, of both the configuration method and the permission constant | **T1**, historical rename | the descriptor mapper | mapper and constant test | already happened |
| S19 | The permission and descriptor constant families | **T1** | the client-definition mapper | mapper test | none |
| S20 | Introspection and revocation validate the authorized party **natively**, and neither is pass-through | **T3**, and the wrong-API class | none | conformance suite | none |
| S21 | Pushed authorization requests are native and configured, not built | T1 | configuration | conformance suite | none |

**Multi-tenant composition** (owner [02](02-data.md) and [18](18-tenant-lifecycle.md), ADR-0018)

| # | Seam | Tier | Isolation | Test | Decommission |
|---|---|---|---|---|---|
| S22 | The tenancy library and the engine both override model building and save, and must compose | **T6, the highest risk here** | the multi-tenant marking plus enforcement on tracking | spike A-4, tests T1 to T7 | none |
| S23 | Bulk update and delete **bypass** save, so the tenancy stamp and its throw never run | **T3** | the row-level-security backstop | spike A-4 plus the prune test | none |
| S24 | A pooled context with a mutable tenant identifier, rather than one captured at construction | **T6** | a pooled factory with a scoped identifier | spike A-4 test T7, which is the gate | none |
| S25 | Pruning deletes only expired, redeemed, or revoked entries, which is filter-agnostic and therefore safe for a pooled tenant | **T3** | none | prune test | none |
| S26 | The version quartet must be pinned in lock-step | **T6** | central package management | build | none |
| S27 | Migration behaviours of the ORM and driver, including the advisory lock, out-of-order migrations, and identifier generation on the pinned database | **T6** | the migration runner | migration CI test | none |

**Build-interims with a native replacement in view** (T5 throughout)

| # | Seam | Owner | Isolation | Decommission marker |
|---|---|---|---|---|
| S28 | Back-channel logout fan-out | [13](13-revocation-propagation-and-caching.md), ADR-0019 | the logout fan-out service | targeted at the next major; **the marker trips only when native is proven on a released build**, not when an issue is assigned to a milestone |
| S29 | Dynamic client registration, if built as an interim | [15](15-admin-api.md) | the admin provisioning port | same condition, same major |
| S30 | The whole sender-constraint family, S6 to S10 | [06](06-sender-constrained-tokens.md) | the handler interfaces | **no committed native**; treat as owned indefinitely and monitor |
| S31 | Telemetry instruments, because the engine emits none | [19](19-observability-capacity-slo.md), ADR-0022 | Nami's own meter and activity source | open with no milestone; own it, and do not guess the native names |
| S32 | The actor claim and actor resolution, where the **grant itself is already native** | [14](14-advanced-flows.md) and [07](07-authorization.md) | the token-exchange handler | only the claim is interim |

**Pipeline stability** (owner this document and [01](01-foundations.md))

| # | Seam | Tier | Isolation | Test | Decommission |
|---|---|---|---|---|---|
| S33 | Every custom handler anchors by **named descriptor**, never a literal order number | **T4** | one file of order constants | the pipeline-order snapshot | none |
| S34 | Degraded mode is forbidden where real tokens are issued, enforced by a fail-fast startup guard | T1 | the startup guard, which also emits a security event | startup test | none |

### 5.3 What was read at source, and what was not

Four registry claims were re-read against the engine's own source at the pinned version on
2026-07-27, rather than carried on the corpus's word. This is the standard the rest of the
registry should reach before GA, not a claim that it already has.

| Seam | Read at | What it says |
|---|---|---|
| S2 | the validation protection handlers | the signing credential is assigned as `SigningCredentials.First()`, with no selection logic, so **Nami controls the signer purely by controlling order** |
| S3 | the key-set discovery handler | the loop over credentials filters on **algorithm only**; there is no validity-window filter, which is exactly what makes publish-before-sign work |
| S9 | the validation protection handlers | the confirmation check reads only the certificate thumbprint and otherwise throws a specific internal error, which is why the custom handler must run first |
| S13 | the server options | the reuse leeway is initialized to 30 seconds and documented as the default, confirming ADR-0004's wording rather than assuming it |

**Not verified here**, and marked so deliberately: whether the server-side signing path has
a handler identical to S2's, because the checked-in source excerpt covers the validation
side; and every roadmap statement in section 5.6, which is external and time-sensitive.

### 5.4 The lifecycle of a build-interim

A build-interim is the only seam class that is supposed to disappear, and the rule for when
it may is the part that gets rushed.

```mermaid
stateDiagram-v2
  [*] --> Built: the engine lacks it, so Nami builds it behind a port
  Built --> Marked: a decommission marker names the version to watch
  Marked --> Announced: the feature is assigned to a milestone
  Announced --> Marked: the milestone slips, which is the normal case
  Announced --> Shipped: it appears in a released build
  Shipped --> Proven: the contract test passes against the native path
  Proven --> Retired: the adapter swaps behind the port, the interim is deleted
  Retired --> [*]
  Marked --> Permanent: no committed native, so stop waiting
  Permanent --> [*]
```

**A milestone assignment is not a shipped feature.** The transition that matters is
`Shipped` to `Proven`, and it is a test run, not a reading of a roadmap. Two of the five
interims here are marked `Permanent` on purpose: nothing has been committed for them, and a
design that waits for a feature nobody promised waits forever while carrying the debt of
pretending otherwise.

### 5.5 The per-bump playbook

A version bump is a procedure, not a judgement call.

```mermaid
flowchart TD
  A[A new release of the engine or a co-versioned library] --> B[Read the release notes against this registry]
  B --> C[Run the contract-regression suite plus conformance on a branch]
  C --> D{Any seam failed}
  D -->|yes| E[Identify the tier, fix at the adapter or port]
  E --> F{Did the fix reach a caller}
  F -->|yes| G[The isolation was wrong; record that as a finding]
  F -->|no| C
  D -->|no| H{Did a feature become native}
  H -->|yes| I[Swap interim to native behind the port, keep the interim until proven]
  H -->|no| J[Update the pin]
  I --> J
  J --> K[Update the source records and this registry]
```

Step E carries the only diagnostic in the process worth stating out loud: **if fixing a
broken seam requires changing a caller, the isolation was wrong**, and that is a finding
about the design, not just about the bump.

### 5.6 Readiness for the next major

The engine's next major has announced breaking changes, and the work to absorb them is done
**now, on the pinned version**, so the bump is bounded.

| # | The change | Seams hit | Done now |
|---|---|---|---|
| R1 | The options pipeline is restructured, and validation moves to the options-validation interface with start-up validation | S1, S34 | isolate **all** options wiring into one module, so the blast radius is one file, with an architecture test keeping it there |
| R2 | An integration options type stops inheriting the authentication-scheme options base | S1 | forbid treating those options as scheme options, and record it as an invariant |
| R3 | Every obsolete member is removed | all | a zero-obsolete policy **now**: an obsolete warning from the engine's namespace is a build error, so nothing obsolete survives to the bump |
| R4 | Options validation moves to the validation interface with start-up validation | S34 and the invariant guards | pre-adopt on the pinned version, which already supports it, making the move a no-op |
| R5 | Client-secret hashing hardens | S19 and the secret store | do not hard-code hash parameters: store the algorithm and parameters **with** the hash and provide a re-hash-on-verify path |
| R6 | The precedent is that a major changes schema and API together | all | **run the suite and conformance against a preview in a spike branch now**, and find out what breaks instead of predicting it |

R6 and R3 are the priorities, for opposite reasons: R6 replaces speculation with a result,
and R3 is cheap and stops debt accumulating.

**One correction the corpus made against itself, kept here because it is the shape of the
error to avoid.** The scope of R1 and R2 is narrower than it first reads: the inheritance
change is announced for the **integration** options type, while the rotation seam hooks the
**core** options type. So the direct impact is **unconfirmed**, and the real risk is the
pipeline-wide restructure that the seam depends on. That must be settled by running the
rotation contract test against a preview, not by assuming either way.

**Roadmap provenance.** The statements above about which features are targeted at which
release come from the design corpus, verified there on 2026-07-04 and **not re-verified for
this document**. The corpus is careful about its own limit and that limit is carried
forward: what was verified is a **milestone assignment**, not a shipped feature, and the
released minor that some notes once credited with dynamic client registration does **not**
contain it. Re-verification is an open item in section 10.

## 6. Dependencies and wiring

This design adds **no dependency**. It registers dependencies that other designs already
take, which is why it has no library table of its own: a library appearing here for the
first time would mean a seam nobody owns.

What it does require in the build:

* one file of named pipeline-order constants, so every custom handler position is reviewable
  in one place (S33);
* the degraded-mode startup guard, expressed through options validation with start-up
  validation so it also satisfies R4 (S34);
* the contract-regression test project, wired to run on **every** dependency bump, with a
  failure blocking the build ([21](21-cicd-and-deployment.md));
* architecture tests keeping options wiring inside its one module (R1) and keeping the
  engine's types out of the domain ([01](01-foundations.md));
* the zero-obsolete policy as a compiler setting, not a review convention (R3).

> **Patterns applied** (ADR-0066). **Registry** for the catalogue itself, which is the whole
> artifact. **Adapter** for every build-interim, which is what makes an interim retirable at
> all: without a port, "swap to native" means editing call sites. **Template Method** for the
> per-bump playbook, in the weak sense that the steps are fixed and only the per-seam fix
> varies. No pattern is applied here for its own sake; the registry is a table because a
> table is what a reader needs at 2 a.m. during a bump.

## 7. Error handling, edge cases, invariants

* **A dependency without a registry row is the defect.** The row, not the code, is what a
  bump re-verifies.
* **A T2 or T3 seam without a behaviour-asserting contract test is unregistered in
  practice**, because those are precisely the seams that break without an error.
* **Order is anchored by named descriptor, never by a literal number.** The snapshot test
  blocks a silent reorder.
* **A broken contract fails the build.** The point of the suite is that the break is known
  before production, so a bump with a red suite is not a bump.
* **An interim retires only when native is proven on a released build.** Not on a milestone,
  not on a preview announcement, not on a maintainer's comment.
* **A fix that reaches a caller means the isolation failed**, and that is recorded as a
  finding rather than absorbed quietly.
* **Degraded mode is forbidden wherever real tokens are issued**, enforced fail-fast at
  start-up and audited when it is enabled anywhere else.
* **Confirm the endpoint model before writing code.** Hand-rolling something the engine does
  natively is the single most common error in this problem domain, and S16, S12, and S20 are
  its three usual shapes.

## 8. Security and multi-tenancy notes

Three seam groups are security seams rather than compatibility seams, and their tests are
not optional. The claim choke point is deny-by-default and a replacement adapter may not
weaken it (ADR-0075, mechanism in [09](09-federation-and-claims-profile.md) and
[04](04-core-protocol.md)). The audit lane never travels through the diagnostics pipeline,
which is a seam invariant as much as an audit one ([03](03-audit.md)). And the wrong-API
class is a security matter, not a style one: hand-rolling revocation, introspection party
validation, or a confirmation claim produces a **weaker** version of a control the engine
already implements correctly.

The multi-tenant composition group (S22 to S27) is the highest-risk group in the registry,
because it is the only one where two libraries both hook the same extension points, and
because a failure there is a cross-tenant leak rather than an outage. It is gated by spike
A-4 rather than by argument.

## 9. Testing

The contract-regression suite maps **one to one** onto section 5.2 and runs on every bump of
the engine or any co-versioned library.

| Group | Status |
|---|---|
| Rotation, S1 to S4 | specified |
| Sender constraint, S6 to S10 | specified, from spikes A-1 and A-3 |
| Multi-tenant composition, S22 to S24 | specified, from spike A-4 tests T1 to T7 |
| Endpoint model, S16, S17, S20, S21 | specified, through the conformance suite |
| Mapper and constants, S18, S19 | specified |
| Migration behaviour, S27 | specified |
| Pipeline order, S33 | specified, the snapshot test |
| Degraded mode, S34 | specified, the start-up test |
| **Cache, entry validation, leeway, family revoke, refresh interval, S11 to S15** | **to build** |
| **Prune scope S25, pin check S26, telemetry surface S31** | **to build** |

Nine of the thirty-four seams therefore have no contract test yet, and they are named rather
than glossed: a registry that reported itself complete would be worse than one that reports
its own gaps.

## 10. Open and build-time items

* **Build the contract-regression project** covering all thirty-four seams, extending the
  specified tests and adding the nine listed above.
* **Write the pipeline-order constants and the snapshot baseline** (S33), and attach the
  decommission markers in code as well as here (S28 to S32).
* **Run the readiness spike, R6**, against a preview of the next major, and report which
  seams break. This is the highest-value open item in this document.
* **Re-verify the roadmap quarterly**, on the same schedule as the runtime lifecycle watch
  (ADR-0030), since a runtime major drags the engine and ORM with it. The statements in 5.6
  carry the corpus's verification date, not this repository's.
* **Source-verify the two mappings the corpus flagged as unverified**: the engine handlers
  corresponding to a resource-validation seam and an authorize-interaction seam. The corpus
  marks both as cross-checked against a product reference rather than against engine source,
  and its own rule, which is also ours, is that a finding is not a finding until read at
  source. Neither may be coded against until that is done.
* **Reconcile the verification version gap**: [05](05-resource-server-validation.md) records
  an API verified at tenancy-library 10.1.2 while the pinned stack is 10.1.1. Harmless today,
  but verifying against a version the project does not pin is how a false confirmation gets
  in.

## 11. Sources

* **ADRs:** 0021 (the owning decision, and the source of the S1-S34 scope and the tier
  vocabulary), 0024 (the extension axes), 0030 (the sibling runtime playbook), 0011, 0014,
  0018, 0019, 0022 (the seam owners), 0075 (the security-sensitive port invariants), 0004
  (the leeway value), 0026 (the license gate that a new dependency would trip).
* **Architecture:** [component view](../architecture/08-component-view.md),
  [schema migration and evolution](../architecture/15-schema-migration-evolution.md).
* **Design:** [01](01-foundations.md) (the extension axes and the port catalogue),
  [04](04-core-protocol.md), [06](06-sender-constrained-tokens.md),
  [12](12-key-management.md), [13](13-revocation-propagation-and-caching.md), [02](02-data.md),
  [18](18-tenant-lifecycle.md), [19](19-observability-capacity-slo.md),
  [20](20-testing.md), [21](21-cicd-and-deployment.md).
* **External verification, 2026-07-27.** Seams S2, S3, S9, and S13 were read against the
  engine's upstream source at the pinned version, checked into the design corpus for exactly
  this purpose: the credential assignment and the confirmation-check throw in the validation
  protection handlers, the unfiltered credential loop in the key-set discovery handler, and
  the reuse-leeway initializer in the server options. The local package cache carries only
  the preceding patch line, so that checked-in tree is what made an offline read possible.
* Reconciled against the design corpus's seam catalogue and its extensibility design on
  2026-07-27, through the corpus's own five-part bundle: the root document, the mini-spec,
  the decision, the verification records, and the readiness register entry, which records
  this catalogue as complete with the readiness items outstanding. Divergences are stated
  where they occur: the `S`-numbering is used exclusively, the product-parity table is not
  imported and its substance is restated as Nami's own extension surface in
  [01](01-foundations.md), and the roadmap statements carry the corpus's verification date
  rather than this repository's.

[Prev: CI/CD and deployment](21-cicd-and-deployment.md) · [Index](README.md) · Next: [Configuration and client declaration](23-configuration-and-client-declaration.md)
