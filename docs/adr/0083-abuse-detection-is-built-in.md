---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ADR-0082 (the lanes and grouping keys this completes), ADR-0008 (the always-local audit table the detector reads), ADR-0031 (the clustered runner and its stale-run heartbeat), ADR-0042 (the synchronous controls this must not become a second copy of), ADR-0077 (the bounded tag set the detector's own output has to satisfy)
informed: implementers of the audit subsystem and the observability kit, consumers of the OSS package, Security (thresholds) and Ops (routing)
---

# Ship abuse detection as a built-in component rather than a SIEM responsibility

## Context and Problem Statement

ADR-0082 resolved a real contradiction and added the grouping keys the audit lane was
missing. That was necessary and it is not sufficient, in three ways that are worth
naming rather than inheriting.

1. **The audit lane has data and no evaluator.** Shipping query templates for someone
   else's SIEM is not an evaluator: nothing runs them on a schedule, nothing owns
   their thresholds, nothing pages. The rules were moved to a lane that *could* answer
   them and then left with nobody asking.
2. **That lane's existence would be conditional.** The concrete write-once and SIEM
   destination is deliberately left to ratification (ADR-0008), so it is legitimately
   optional. That would make brute-force detection, lockout-DoS detection, and
   per-tenant abuse detection **optional features of an identity provider**, which is
   not a posture this product can ship.
3. **Per-IP detection would be conditional on a legal answer.** Making `SourceIpHash`
   nullable so a refusal "degrades gracefully" is only graceful if what degrades is
   not the detection itself. As inherited, a refusal removed the capability.

A fourth gap is structural and is the interesting one. ADR-0082 says the metric lane
sees deployment-wide aggregates while the audit lane knows who, and stops there.
**Nothing bridges them.** So even a correctly-laned design has no way for a
per-principal finding to reach an on-call pager without a forbidden tag.

## Decision Drivers

* An identity provider must detect credential stuffing and account attacks **out of
  the box**. A capability that appears only when a customer happens to wire an
  external product is not a capability we ship.
* The alerting path has to reach a pager. A detection that lands only in a table
  nobody queries is the same failure in a different place.
* No new operational concept. Every part must already exist in the decided stack, or
  the detector becomes its own reliability problem.
* Detection must not turn into a second, asynchronous, coarser rate limiter.
* The query cost must be **measured, not assumed**.

## Considered Options

* **A. A built-in detector job over the local audit store**, emitting a security event
  plus a bounded metric.
* **B. Make a SIEM destination mandatory for production.**
* **C. Real-time inline detection with distributed counters in the shared cache.**
* **D. Leave it as ADR-0082 has it**: ship query templates and document the
  limitation.

## Decision Outcome

Chosen: **Option A.**

### The mechanism, built entirely from parts already decided

The audit lane already writes to a **local** audit table and forwards to write-once
storage and any SIEM through an outbox (ADR-0008), so **the data is local regardless
of whether a SIEM exists**. The evaluator is therefore internal:

* A **clustered scheduled job**, on the runner already in the stack and already
  covered by the stale-run heartbeat alert, evaluates a fixed rule set over the audit
  table, grouping on `SubjectRef`, `TargetTenantId`, and `ClientId` (ADR-0082).
* Each firing writes a security event of type **`abuse.detected`** carrying the rule,
  the window, the count, and the grouping key, so the finding is itself hash-chained
  and forwarded like any other security event.
* Each firing also increments a counter tagged `{rule, severity}`.

### The bridge, which is the load-bearing idea

**The detector's output is bounded even though its input is not.**

`{rule, severity}` is roughly ten rule values by three severities. So **the metric
lane can legitimately page**, with no forbidden tag anywhere, while attribution stays
in the audit lane where it belongs. This is precisely what ADR-0082 was missing: it
correctly separated "how many" from "who" and then had no path from "who" to a phone.
The detector consumes the unbounded dimension inside the audit lane and emits a
bounded signal to the metric lane.

### Hard scope boundary: the detector detects and alerts, it never blocks

Blocking stays where it already is and where it must be, on the synchronous path:
account lockout and the rate limiter (ADR-0042). Without this line the detector grows
into a second rate limiter that is asynchronous, coarser, and up to one job interval
late. Detection latency is the job interval, which is **acceptable for alerting and
unacceptable for enforcement**, and that is exactly why the two must not merge.

### Detection and forensics are split for the source address

| | What it needs | What is stored |
|---|---|---|
| **Threshold detection** per address | the value **transiently**, inside a bounded in-memory sliding window | **nothing** |
| **Forensics and cross-node aggregation** | a stable value that groups across nodes and days | `SourceIpHash`, persisted, ADR-0082 |

The address is already present in the request. Using it inside a bounded in-memory
window and discarding it is a **smaller** privacy footprint than persisting a keyed
hash. So **per-IP detection is unconditional**, and only durable per-IP *attribution*
depends on the ratification answer.

**"Bounded" is a measured property, not an intention.** An unbounded dictionary keyed by
source address would make the detector the memory-exhaustion vector it exists to detect,
so the corpus's spike A-11 asserted it directly (V30, case T5): two million distinct
addresses, and the window held its ten-thousand cap while a repeat offender inside the
retained set was still counted.

The cost is then stated rather than hidden: an in-memory window is **per-node**, so a
distributed attack is undercounted by roughly the node count. Thresholds are therefore
expressed per-node, and the cross-node view comes from the persisted path where it is
permitted. A per-node window is also a second reason this cannot be the enforcement
mechanism.

### The SIEM's role is redefined, not removed

It is for correlation with the rest of an estate, long-term forensics, and rules that
span systems we do not own. It is **not** our detection engine. A deployment with no
SIEM therefore loses **correlation**, not **detection**.

### Rule set for v1, with thresholds left to ratify

Credential stuffing by distinct-`SubjectRef` count against a shared failure pattern;
brute force on one account; lockout-DoS on one account, which is the alert ADR-0042
promised and nothing could produce; second-factor failure spike per subject;
token-issuance spike per client or per tenant; `invalid_client` spike per client;
per-address velocity in the transient window; keyring access from an unknown source.
Each rule declares its window, threshold, severity, and grouping key. The thresholds
themselves are a pre-GA ratification item for Security, not an architecture constant.

### Consequences

* Good, because abuse detection stops being an optional feature of an identity
  provider.
* Good, because the finding reaches a pager through a **bounded** metric, so the
  cardinality rule and the alerting requirement stop being in tension at all rather
  than being reconciled by documentation.
* Good, because per-IP detection no longer waits on a legal decision, and the privacy
  footprint of detection itself drops to zero stored bytes.
* Good, because every part already exists: the audit table, the clustered runner,
  the security-event sink, the meter. The runner already has a stale-heartbeat alert,
  so **a dead detector is itself detectable**, which is the failure mode a
  cron-shaped control usually hides.
* Good, because `abuse.detected` is hash-chained and forwarded like any other security
  event, so a detection has the same tamper-evidence as the events it was derived
  from.
* Bad, because **we now own a detection engine** and must tune thresholds and absorb
  false positives. Accepted deliberately: the alternative is an identity provider
  whose brute-force detection works only if the customer wires an external product.
* Bad, because it puts read queries on a hot append-only table. **Measured rather than
  assumed, and it held**: the design corpus ran the gate spike (A-11, verification
  record V30, 5/5, 2026-07-29) and found the direct queries adequate with no rollup
  table needed, and no detectable cost on the append path. The measurement also
  **removed two of the four indexes the design had assumed it would need**, which is
  the more valuable half of the result, because this table is on the audit write path
  where an unjustified index is a permanent write cost. What remains is a build-time
  re-measurement on target hardware, not an open design question.
* Bad, because per-node windows undercount distributed attacks. Bounded by design and
  stated in the threshold definition; it is a detection aid and never an enforcement
  control.
* Neutral, because thresholds become a ratification item rather than a constant in
  this document.

### Confirmation

* The detector's own liveness is covered by the existing stale-run heartbeat on the
  clustered runner, which is what makes "the detector silently stopped" a detectable
  state rather than an assumption.
* Tests: a simulated credential-stuffing pattern produces exactly one
  `abuse.detected` event and one counter increment with `{rule, severity}` and no
  other tag; the emitted counter is asserted against the ADR-0077 allow-list so the
  bridge cannot regress into a forbidden tag; a per-address burst inside one node's
  window fires while nothing is persisted, asserted by inspecting the audit row for an
  absent `SourceIpHash` when emission is disabled; and the detector is asserted **not**
  to reject a request, so it cannot quietly acquire enforcement behaviour.
* **The read-query cost is settled and the thresholds are not, and the two were
  deliberately separated.** The corpus's spike A-11 closed the cost question (V30,
  above). The rule thresholds remain a Security ratification item, because a threshold
  invented here would be an architecture constant pretending to be a security
  decision. Recorded so the closure of one is not read as the closure of both.

## Pros and Cons of the Options

### A. Built-in detector over the local audit store (chosen)

* Good, because detection ships with the product and needs nothing external.
* Good, because it is assembled from components already decided, so it adds no new
  operational surface.
* Bad, because we own the engine, its thresholds, and its false positives.

### B. Make a SIEM mandatory in production

* Good, because a real SIEM is better at correlation than anything we would build.
* Bad, because it makes a core security capability contingent on a component ADR-0008
  deliberately leaves optional, and it pushes a licensing and operational cost onto
  every adopter of an open-source identity provider.

### C. Real-time inline detection with distributed counters

* Good, because it would be immediate and cross-node accurate.
* Bad, because it puts a shared-cache round trip on the authentication hot path and
  makes the cache a correctness dependency, which the caching decision deliberately
  refuses. It also collapses the detect-versus-block boundary this ADR exists to hold.

### D. Ship query templates and document the limitation

* Good, because it is zero work.
* Bad, because it is the state this ADR was written to leave. A documented limitation
  is still a missing capability, and writing it down does not make an identity
  provider without brute-force detection acceptable.

## More Information

* Mechanism: the detector job and its rule set belong to design
  [03](../design/03-audit.md) for the event side and design
  [19](../design/19-observability-capacity-slo.md) for the metric and alerting side.
* **Completes ADR-0082 and does not reverse it.** That ADR's lane assignment and its
  three fields stand unchanged. What changes is that lane A gains an evaluator, its
  existence stops being conditional on an optional SIEM, and per-IP detection stops
  being conditional on a ratification answer.
* Related decisions: ADR-0042 (the synchronous prevention controls, which keep
  blocking), ADR-0008 (the audit sinks and the always-local table), ADR-0031 (the
  clustered runner and the heartbeat that makes a dead detector visible), ADR-0077
  (the tag rule the bounded output satisfies), ADR-0085 (the instrument naming).
* Imported from the design corpus's built-in-detector decision on 2026-08-01. The
  corpus pairs it with its lane decision on the same day and says to read the two
  together; that pairing is preserved here deliberately, because the record of what
  the first decision left unfinished is the most reusable part of it.
* **This record shipped stating an open spike that had already closed, and the gap is
  worth naming rather than quietly overwriting.** The corpus ran A-11 on 2026-07-29;
  this ADR was written on 2026-08-01 from the corpus decision text, which still
  described the spike as pending, and the verification record that closed it was in a
  different folder. Nothing here was wrong when the corpus wrote it. The reusable
  lesson is that importing a decision means importing whatever ran against it since,
  because a decision and its evidence age at different rates and only the decision is
  where an importer looks.
