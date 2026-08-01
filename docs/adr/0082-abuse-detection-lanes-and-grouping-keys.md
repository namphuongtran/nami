---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0077 (the allow-listed metric tag set this reconciles with), ADR-0042 (the abuse-defense posture whose alerts had no lane), ADR-0008 (the audit sinks and the outbox to write-once storage), ADR-0016 (the `SubjectRef` surrogate and the per-subject key vault this reuses rather than invents), ADR-0073 (the edge that owns per-IP rules where one is deployed); ASP.NET Core built-in metrics documentation, fetched 2026-08-01, for the rate-limiting meter's tag set
informed: implementers of the audit subsystem and the shipped observability kit, consumers of the OSS package, the data-protection officer for the one open item
---

# Give every abuse rule a lane that can answer it, and add the three grouping keys the audit lane was missing

## Context and Problem Statement

Two binding rules in this repository were in direct contradiction, and the
contradiction had already been shipped as a threat-model mitigation.

ADR-0077 allow-lists metric dimensions and names `sub`, `tenant_id`, `client_id`,
`session.id`, `jti`, any raw token, and **any IP address** as never permitted. ADR-0042
then promises "a distinct alert for many lockouts on one account, separate from the
brute-force-spike alert", and the threat model carries that alert as the mitigation
for lockout weaponisation.

**An alert about one account cannot be a metric under ADR-0077.** That much is
resolvable by moving the rule to the audit lane. The part that is not resolvable that
way, and is the reason this needs a decision rather than a note, is that **the audit
lane cannot answer it either**:

* **`ActorSub` is per-subject ciphertext.** The audit design makes crypto-shred the
  runtime default, so the subject identifier on the event is ciphertext. Grouping by
  user is impossible unless that encryption is deterministic, and making it
  deterministic to enable grouping would weaken the crypto-shred. The design names
  PII-outside-the-chain with an opaque `SubjectRef` as the **design target** and
  crypto-shred as the **runtime default**, so the groupable surrogate exists as an
  aspiration and is not on the event.
* **There is no source-address field at all.** Every per-IP rule has no data source in
  either lane: forbidden as a metric tag, absent from the event.
* **There is no client identifier on the event**, so per-client rules have no source
  either.

So four named rule families have no lane capable of answering them, and one of them
is written into the threat model as a control. A mitigation that no mechanism produces
is the worst kind of gap, because it stops anyone from looking for the real one.

## Decision Drivers

* An alert that cannot name the offender is close to useless for an abuse response,
  and an alert specified against data that does not exist is worse than absent,
  because the runbook implies someone will act on it.
* Personal data must not enter the diagnostics lane. That lane has no hash chain, no
  write-once copy, wider read access, and a different retention window than the audit
  store, and the post-erasure scan covers metric tags explicitly.
* Reuse the surrogate that already exists. A second subject pseudonym would mean two
  mappings to delete on erasure, and erasure correctness depends on there being
  exactly one.
* The mechanism must not wait on a legal question, and the legal question must not be
  answered here.

## Considered Options

* **A. Assign every rule a lane by what that lane can answer, and add the fields the
  audit lane needs to answer its share.**
* **B. Relax the cardinality rule for abuse metrics**, allowing a per-subject or
  per-IP tag on that family only.
* **C. Drop the per-principal rules** and keep only deployment-wide aggregates.

## Decision Outcome

Chosen: **Option A.**

### Lane assignment

| Lane | What it can answer | Rules |
|---|---|---|
| **M**, metric, bounded tags only | **deployment-wide aggregates**, never which principal | login-failure rate, token-error and `invalid_grant` rate, refresh-replay count, 429 and 503 bursts, clock drift, recovery-point breach, error-budget burn |
| **A**, audit, grouping on `SubjectRef` / `TargetTenantId` / `ClientId` | **per-user, per-tenant, per-client** | credential stuffing by distinct-subject count, brute force and lockout-DoS on one account, second-factor failure per user, issuance spike per client or tenant, keyring access from an unknown source |
| **E**, edge, where one is deployed (ADR-0073) | **per-IP**, velocity, geography, volumetric | per-IP rules, where an edge exists |

Two consequences are stated rather than left to be discovered. **The metric lane
cannot answer a per-tenant question**, because `tenant_id` is a forbidden tag, so
per-tenant rules are lane A and not lane M. And a deployment with **no** edge has no
lane E, which is why per-IP detection is handled in ADR-0083 rather than left as a
hole here.

### The three fields added to the audit event and the audit table

| Field | Serves | Why this shape rather than the obvious one |
|---|---|---|
| `SubjectRef` uuid NULL | per-user rules | **Deterministic**, so it can be grouped on. Not `ActorSub`, which is per-subject ciphertext. It is the **same** surrogate that the processing-restriction table and the per-subject key vault already use (ADR-0016), so erasure still has exactly one mapping to destroy, after which the value is an opaque orphan: history still groups, nobody resolves it to a person |
| `SourceIpHash` bytea NULL | per-IP rules where no edge is in front | Keyed HMAC-SHA256, and **not truncated**. Truncation buys no meaningful privacy against an input space this small and creates collisions, and a collision in an abuse rule is **false attribution**, which is worse than no attribution |
| `ClientId` text NULL | per-client rules | A registered application identifier, so not personal data, so stored plainly. It stays **off** the metric lane regardless, because it is unbounded there |

**`SourceIpHash` is a pseudonym, not anonymisation.** An HMAC over the IPv4 space is
brute-forceable by anyone holding the key, so the protection is key custody and access
control on the audit store, not the hash function. This is stated plainly so that
nobody builds a compliance argument on the word "hashed". Whether it is acceptable
under the applicable data-protection basis is **not decided here** and is a
pre-GA ratification item for the data-protection officer. The field is nullable and
its emission configurable, so a refusal is a configuration answer rather than a
redesign, and ADR-0083 makes sure a refusal does not remove the detection capability.

### Enforcement using an identifier is not observation using it

The rate limiter partitions by user, address, or client to enforce quotas, and that is
untouched by any of this. Verified at source on 2026-08-01: every instrument on the
`Microsoft.AspNetCore.RateLimiting` meter carries only `aspnetcore.rate_limiting.policy`
and, on two of them, `aspnetcore.rate_limiting.result`. **No partition key appears on
any of them**, so `aspnetcore.rate_limiting.requests` is bounded by construction and is
a legitimate lane-M signal. The same check surfaced that .NET 10 ships a
`Microsoft.AspNetCore.Authentication` meter whose instruments carry only scheme,
result, and error type, so several lane-M authentication aggregates are available
without instrumenting anything.

### Consequences

* Good, because every rule now has a lane that can actually answer it, which was true
  of no per-principal rule before.
* Good, because the pressure to break the cardinality rule is removed at its source.
  ADR-0077's rule was being undone by another requirement rather than by an
  implementer's mistake, and no amount of implementer discipline fixes that shape of
  problem.
* Good, because the shipped observability kit stops carrying per-principal rules as
  metric rules that would need forbidden tags.
* Bad, because these are three columns on a **hash-chained** table, so they must be in
  the canonical field set **from genesis**. Adding one later is a chain schema version,
  not an ordinary migration. Cheap now, expensive after go-live, which is the argument
  for deciding it now rather than when the first rule is written.
* Bad, because `SourceIpHash` opens a data-protection question somebody else has to
  answer. Mitigated by nullability and by ADR-0083 keeping detection independent of the
  answer.
* Neutral, because per-user attribution now costs one resolution of `SubjectRef`
  through the mapping table, which is a deliberate access-controlled hop rather than an
  inconvenience.

### Confirmation

* **The contradiction is present here, not hypothetical.** ADR-0042 promises the
  per-account lockout alert, the threat model carries it as a mitigation, ADR-0077
  forbids `sub` and any address as a metric tag, and the security event carries only
  event type, outcome, actor ciphertext, target tenant, and correlation id. No lane
  could produce that alert.
* The rate-limiting meter's tag set was **fetched and read at source** rather than
  assumed, because the whole argument that enforcement-by-identifier is compatible
  with the tag rule depends on the partition key genuinely not being exported.
* Tests: no shipped metric rule references a forbidden dimension, parsed from the rule
  files rather than eyeballed; the three fields exist and are inside the canonical
  hashed field set; **per-user grouping works on `SubjectRef` and does not work on
  `ActorSub`**, asserted by two events for one subject sharing `SubjectRef` while being
  allowed to differ in `ActorSub`, which is the entire point; and `SourceIpHash` is
  deterministic within a deployment and untruncated.
* The existing post-erasure scan already fails if a subject's personal data appears in
  metric-tag output, which is why this could not have been resolved by simply capping
  cardinality.

## Pros and Cons of the Options

### A. Lane assignment plus the three grouping keys (chosen)

* Good, because it fixes the cause rather than choosing a side between two rules that
  are each individually right.
* Good, because it reuses the existing subject surrogate, so erasure keeps exactly one
  mapping to destroy.
* Bad, because it changes the canonical field set of a hash-chained table, which is
  the one part of this that is genuinely expensive later.

### B. Relax the cardinality rule for abuse metrics only

* Good, because it needs no schema change and the rules could be written immediately.
* Bad, because it puts subject identifiers and addresses into the lane with no hash
  chain, wider read access, and a retention window that erasure does not reach. The
  post-erasure scan would fail, correctly.
* Bad, because "only for this family" is not a boundary anything enforces.

### C. Drop the per-principal rules

* Good, because it is instantly consistent.
* Bad, because credential stuffing and account attacks are detectable only per
  principal, so this deletes the capability rather than relocating it, and it would
  silently remove a control the threat model relies on.

## More Information

* Mechanism: the audit event shape and the sinks are design
  [03](../design/03-audit.md); the columns are design [02](../design/02-data.md); the
  metric lane and the shipped rules are design
  [19](../design/19-observability-capacity-slo.md).
* The evaluator for lane A is **ADR-0083**, which completes this decision. Read the
  two together: this one assigns lanes and adds keys, and on its own it would leave
  the audit lane with data and nobody asking questions of it.
* Related decisions: ADR-0077 (the allow-listed tag set), ADR-0042 (the prevention
  posture whose alerts this makes answerable), ADR-0008 (the audit sinks), ADR-0016
  (the `SubjectRef` surrogate and the erasure mapping), ADR-0073 (the edge that owns
  lane E), ADR-0085 (the instrument naming these lane-M signals follow).
* Open item for pre-GA ratification: whether `SourceIpHash` is acceptable under the
  applicable data-protection basis. Architecturally decided here, legally open.
* Imported from the design corpus's lane-assignment decision on 2026-08-01. The
  rate-limiting meter claim was re-verified at source; the field shapes were checked
  against **this** repository's existing `SubjectRef` surrogate and per-subject key
  vault rather than assumed to match.
