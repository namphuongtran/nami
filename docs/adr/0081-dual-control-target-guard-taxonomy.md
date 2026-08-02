---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0020 (the dual-control saga this guards), ADR-0047 (the `ICheckAccess` engine the proposer re-check calls), ADR-0079 (the `If-Match` split the mutate class pairs with), ADR-0010 (the delegated-admin grant, one of the two create-class actions)
informed: Admin API implementers, the Admin App approval inbox, anyone adding a proposal executor, DBA (one column and two check constraints)
---

# Classify a dual-control proposal's target, so the guard checks something for every action

## Context and Problem Statement

The dual-control saga separates **propose** from **execute** by up to 72 hours. The
guard against that window is a single column, `TargetETag text NOT NULL`, which the
executor re-checks before running: if the target changed, the proposal fails as
`target_changed`.

That column encodes an assumption nobody stated: **every proposal mutates exactly one
row that already exists**. Three of the actions in this repository's destructive
catalogue do not. `provision-tenant` and a dangerous `delegated-admin-grant` have no
target at propose time, because the thing they act on is the thing they create. A
bulk `audit-export`'s target is a **filter set**, not a row.

`approve-user-invite` looks like a fourth and is not: its target is the
pending-approval membership row that the invite created in the same request, so a
target does exist. What is different is that the **server**, not the client, knows
its ETag.

Two consequences make this more than a modelling nit.

First, a `NOT NULL` column that cannot be satisfied forces either a schema violation
or an invented value, and an invented value is the worse outcome because it looks
like a guard.

Second, and this is the live defect: the retry rule says a transient failure stays
retryable *while the `TargetETag` still matches*. For a proposal with no target that
condition is **vacuously true**. A `provision-tenant` that failed because its
identifier had been taken would be classified retryable and retried forever against
something that can never succeed.

This cannot be retrofitted cheaply. It is a column, two constraints, and the
terminal-versus-retryable semantics of a state machine that both operators and the
approval inbox read.

## Decision Drivers

* What the executor must establish is **"is the thing the approver approved still the
  thing that will happen"**, not "did two writers collide". The 72-hour window, not
  concurrency, is the exposure.
* `NOT NULL` is load-bearing for the actions that do have a target. Relaxing it
  globally would silently drop their guard, which is the tempting one-line fix and
  the wrong one.
* A guard that appears to exist but checks nothing is worse than an absent guard,
  because it stops anyone from looking.
* Adding a new executor must not require a schema change.

## Considered Options

* **A. Classify the proposal**: add `TargetClass`, make `TargetETag` nullable, and
  require it per class.
* **B. Keep `NOT NULL` and store a sentinel** for targetless proposals.
* **C. Give every proposal a target** by creating a pending row first, generalising
  what `approve-user-invite` already does.
* **D. Make `TargetETag` nullable with no class column.**

## Decision Outcome

Chosen: **Option A**, because it keeps the `NOT NULL` protection **exactly where it
carries meaning** and relaxes it only where it is unsatisfiable, and because keying
the constraint on a class rather than on a list of action names keeps the executor
registry out of the schema.

```sql
"TargetClass"  text NOT NULL,   -- 'mutate' | 'create' | 'query'
"TargetETag"   text NULL,       -- required when TargetClass = 'mutate'

CHECK ("TargetClass" IN ('mutate','create','query'))
CHECK ("TargetClass" <> 'mutate' OR "TargetETag" IS NOT NULL)
```

**What the executor re-checks, per class, across this repository's catalogue:**

| `TargetClass` | `TargetETag` | Guard re-run before executing | Actions |
|---|---|---|---|
| `mutate` | required | the ETag still matches | `delete-application`, `delete-scope`, `delete-tenant`, `suspend-tenant`, `resume-tenant`, `offboard-user`, `revoke-all-tokens`, `secret-revoke`, key purge, the Pool-to-Silo re-home, and **`approve-user-invite`** with a server-filled ETag |
| `create` | NULL | the create preconditions still hold: uniqueness, the existence of every referenced principal, and the endpoint's own admission rules | `provision-tenant`, a dangerous `delegated-admin-grant` |
| `query` | NULL | the filter frozen in the payload is authoritative and **may not be widened**, and its absolute upper time bound still holds, re-evaluated **at redemption rather than at approval** (see below) | bulk `audit-export` |

**For `query`, the guard runs at redemption, and that is a deliberate exception to the column
heading above (added 2026-08-02).** ADR-0008 makes a bulk audit export deliver through a
single-use, time-boxed grant that the proposer redeems, so approval mints the grant and the
rows move afterwards. "Re-run before executing" therefore has two candidate moments for this
class alone, and the useful one is the transfer: re-evaluating a frozen filter and its size at
approval time checks a moment when nothing is leaving. So `ExecutedAt` on a `query` proposal
records that the grant was minted, and all three checks (the filter is not widened, its
absolute upper time bound still holds, and the proposer still holds the capability) are
evaluated when the grant is redeemed. This is the only class where the two moments come
apart, because it is the only class whose effect is **data leaving** rather than state
changing, and reading `ExecutedAt` as "the data left" is exactly the misreading this note
exists to prevent. The egress gets its own audit event for the same reason (ADR-0008).

**The middle check said something else until later the same day, and what it named no longer
existed.** It read "the size or scope threshold that gated the approval is re-evaluated",
which is the wording imported from the corpus, where a threshold genuinely gated approval:
an export was dual-control only when it was full, unfiltered, spanned over ninety days, or
exceeded ten thousand rows, and a smaller one went direct. **ADR-0008 removed that direct
path on this same date**, making every export dual-control, so no threshold gates any
approval and the clause pointed at nothing. It survived because removing a path and
re-reading the guards that referred to it are two different acts, and only the first was
done. The replacement is the invariant the guard was always for: a filter that cannot be
widened is only checkable if it is closed, so ADR-0008 now requires the frozen filter to
carry an absolute upper time bound, and that bound is what the guard re-evaluates.

**A drift is a hard failure, terminal, and recovery is a new proposal.** This is not a new
rule. It is the refusal to make an exception, because `precondition_failed` is already
terminal and single-use for `create` and `query` alike, stated below in this ADR's own
executor semantics, and a warning-and-proceed would make `query` the one class whose guard
can fail while the action goes ahead, on the one action whose effect is a bulk personal-data
egress.

**The corpus asked this as an open question, and its own decision record had already answered
it two sections earlier.** Its ratify packet SEC-A4 puts three options to Security, hard
failure, warning-and-proceed, or a two-tier ceiling, with the decision line left blank. The
corpus decision record this taxonomy came from, the guard-taxonomy MADR whose number there is
**not** this one and does not transfer, already rules `precondition_failed` for create and
query "**terminal and single-use**", and asks whether query should instead warn forty-six lines
below that ruling, in the same file. So two of the three options were foreclosed by the
document offering them, and the ratification here is option (a) by consistency rather than by
severity. This is worth recording because the shape is one this repository has learned to look
for first: **a document disagreeing with itself is where the wrong claim is**, and the
disagreement travelled into a ratify packet, which is the artifact least likely to be read
against its own source.

*(The corpus's number for that document is deliberately not written here. It collides with a
live `ADR-NNNN` in this repository that decides something unrelated, so writing it would
produce a citation that passes guardrail Check 2 by resolving to the wrong decision. That is
the resolving-citation trap in its purest form, and it was hit while drafting this paragraph.)*

**What makes hard failure cheap rather than brittle is the bound above**: with the record set
closed at freeze, the residual movement is outbox lag and retention pruning rather than an open
window filling up, so a refusal signals something worth looking at instead of ordinary elapsed
time.

**`TargetId` is also `NOT NULL`, and the class is what says what it means.** The taxonomy
above would otherwise have left the same shape of hole one column over: a targetless
proposal has no more a target *identifier* than it has a target ETag. Rather than relax a
second column, `TargetClass` disambiguates this one, which is what a class column is for.

| `TargetClass` | `TargetId` holds |
|---|---|
| `mutate` | the identifier of the existing row, unchanged |
| `create` | the identifier of the thing **to be created**: the proposed tenant `Identifier`, or the grantee for a grant, where the root tenant is already in the row's own `TenantId` |
| `query` | a **digest of the frozen filter**, so the proposal names *which* export it authorises |

The digest is specified rather than left to an implementer, because two details decide
whether it works at all:

* **It is computed over the canonical TEXT rendering of the payload, not over the stored
  `jsonb`.** PostgreSQL `jsonb` does not preserve input byte order, so a digest of the
  stored column would depend on the database's internal representation and two identical
  filters could produce two digests. This is the same constraint the audit chain already
  solved, and the same canonicalisation is reused rather than a second one invented
  (design [03](../design/03-audit.md) section 5.2).
* **It is a plain SHA-256, deliberately not keyed.** The purpose here is
  **identification**, and integrity of the row is the audit chain's job. Using an HMAC as
  the chain does would imply a tamper-evidence property this column does not provide, and
  a security property that only looks present is the failure this whole ADR is about.

Two useful properties fall out, and one non-property is worth naming. A digest is
content-addressed, so two identical export proposals collide naturally, which gives dedup
and a comparison the approval inbox can show. It **complements rather than replaces** the
`Idempotency-Key` header on proposal creation: that header is client-supplied and
per-request, this is server-derived and content-derived. And it is **not** a guard: a
mismatch between the digest and the payload indicates a mishandled row, but the guard for
a `query` proposal is the re-evaluation of the frozen filter and its upper bound, not a
string comparison.

*(This rule was added on 2026-08-01, hours after the rest of this ADR, once the question
"was `TargetId` not already decided?" was checked at the source. It was not. The corpus
this taxonomy came from relaxed `TargetETag` and left `TargetId NOT NULL` untouched, while
its own stated premise, that three action types have no target at propose time, applies
identically to both. So it is internally inconsistent at exactly that point rather than
carrying a decision to inherit.)*

**`TargetETag` does not have to come from `If-Match`.** For a client-named target the
client supplies it through the header (ADR-0079 rule 4). For a **server-created**
target the server fills it, because a client cannot hold an ETag for a row that did
not exist when it called. `approve-user-invite` is the case that makes the
distinction necessary, and without stating it someone will conclude that a
server-created target has to be class `create`.

**Across every class the executor also re-checks that the proposer still holds the
capability**, through `ICheckAccess` on the fully-consistent path. Approval
authorises the action; it does not waive validation. Without this, a proposer whose
grant was revoked during the window still has their action executed by an approver
acting in good faith. A 72-hour gap is what turns a normally-acceptable
request-time-only check into a real exposure here.

**Retryability is decided by the failure reason, not by the ETag:**

* `target_changed` (mutate) and `precondition_failed` (create and query) are both
  **terminal and single-use**. Recovery is a **new** proposal with a fresh guard,
  linked by the prior-proposal lineage.
* Only a genuinely transient executor error is retryable, and for `mutate` it
  additionally requires the ETag to still match.
* A duplicate-key or precondition violation surfaces as `precondition_failed`, never
  as a transient error. This is the specific classification that closes the
  retry-forever path, and it is stated as a rule rather than left to whoever writes
  the catch block.

### Consequences

* Good, because the targeted actions keep a database-enforced guard while the three
  targetless ones gain a guard that checks something real.
* Good, because the retry-forever defect is closed **by construction**, through the
  class and the failure taxonomy, rather than by remembering to classify a failure
  correctly at each executor.
* Good, because `TargetClass` travels on the proposal DTO, so the approval inbox can
  show an approver **what is actually being guarded** instead of implying an ETag
  comparison that may not happen. An approver who believes a guard exists is the
  person this whole saga is protecting.
* Bad, because it is a migration: one column, two constraints, and backfill
  semantics. Cheap now while no data exists, expensive after go-live, which is the
  reason to decide it now.
* Bad, because the create and query guards are per-action code rather than one
  generic comparison, so every new executor must state its preconditions. Mitigated
  by making that an explicit item on the executor checklist rather than a convention.
* Neutral, because the proposer-capability re-check adds one authorization call per
  execution. Execution is rare and already inside a transaction.

### Confirmation

* **The defect is present in this repository, not merely possible.** The schema
  carries `"TargetETag" text NOT NULL`, and the admin design states that a transient
  failure "stays retry-able while the `TargetETag` matches". Three catalogue actions
  have no target row at propose time, and provisioning is one of them: the tenant
  design routes provision through a proposal, and the admin design marks
  `POST /tenants` as provision-to-proposal. So the unsatisfiable column and the
  vacuous retry condition both apply today.
* The two `CHECK` constraints are themselves a runtime confirmation: a mis-classified
  mutate proposal cannot be stored at all, which is a stronger guarantee than a test.
* Tests: a mutate ETag mismatch is terminal and not retried; `If-Match` is required
  on the mutate-class endpoints and absent gives `428`; a create whose uniqueness
  precondition broke fails `precondition_failed` **and is asserted not to be
  retried**, which is the regression for the defect above; a grant proposal whose
  proposer lost the capability mid-window is refused; a query whose frozen filter no
  longer holds at redemption fails `precondition_failed` **terminally** rather than
  executing against the old approval, and a companion asserts that a proposal whose
  filter carries no absolute upper time bound is refused at creation, since the
  guard cannot re-evaluate a bound that was never frozen; the `CHECK`
  rejects `mutate` with a null ETag; and `approve-user-invite` carries a
  server-filled ETag.

## Pros and Cons of the Options

### A. Classify the proposal (chosen)

* Good, because the constraint lands exactly where the guarantee exists and nowhere
  else.
* Good, because keying on a class rather than on action names means adding an
  executor is code, not DDL.
* Bad, because it introduces a concept an implementer must learn before adding an
  action.

### B. Sentinel value for targetless proposals

* Good, because it needs no schema change at all.
* Bad, because it is the invented-value outcome stated in the problem: the column
  reads as a guard, the comparison always succeeds, and nothing distinguishes a
  sentinel from a real ETag at a glance. It preserves the retry-forever defect
  exactly.

### C. Give every proposal a target row first

* Good, because one uniform guard would then cover everything.
* Bad, because it forces a pre-created row for actions that have no natural one, and
  a `provision-tenant` that must first insert a tenant-shaped placeholder acquires a
  second failure mode and a cleanup obligation for every abandoned proposal.

### D. Nullable with no class column

* Good, because it is the smallest change that stops the schema violation.
* Bad, because it removes the guarantee for the nine actions that do have a target
  without replacing it with anything, and nothing then records which proposals were
  supposed to be guarded. This is the tempting fix and it silently weakens the
  majority case to accommodate the minority.

## More Information

* Mechanism and the saga's state machine: design
  [15](../design/15-admin-api.md); the schema is design
  [02](../design/02-data.md), which is the authority for the column and constraints;
  the catalogue of destructive actions is design
  [07](../design/07-authorization.md); the provisioning and re-home sagas are design
  [18](../design/18-tenant-lifecycle.md).
* Related decisions: ADR-0020 (dual-control enforced in the application layer, the
  saga this guards), ADR-0079 (the `If-Match` split, which supplies the mutate
  class's ETag from the wire), ADR-0047 (`ICheckAccess`, the proposer re-check),
  ADR-0010 (the delegated-admin grant model, one create-class action), ADR-0015
  (break-glass, deliberately outside this saga), and ADR-0008 (the bulk audit export
  that is this taxonomy's only `query`-class action, whose grant-and-redeem delivery is
  what moves this class's guard to redemption).
* Imported from the design corpus's guard-taxonomy decision on 2026-08-01. The class
  assignments were re-derived against **this** repository's catalogue rather than
  copied: the corpus and this repository do not have identical action lists, and the
  Pool-to-Silo re-home is a mutate-class action here that its list does not carry.
