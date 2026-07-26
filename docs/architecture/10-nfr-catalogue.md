---
status: reviewed
created: 2026-07-25
tags: [architecture, nfr, quality-attributes, slo]
---

# Quality-attribute catalogue

> **Part of:** the [Software Architecture Document](README.md), quality and operational
> views.

This is the **requirements** view: what must be true, with a measurable target and a way to
measure it. It deliberately does not say *how*. The how lives in
[12-performance-and-scalability](12-performance-and-scalability.md),
[13-reliability-backup-and-dr](13-reliability-backup-and-dr.md), the security and
observability views, and [16-operations-and-maintenance](16-operations-and-maintenance.md).
The drivers behind these attributes are in
[00b-drivers-and-constraints](00b-drivers-and-constraints.md).

## 1. The standing caveat, and why it is first

**The 10k-concurrent-user goal is an architecture target proven by our own load test, not a
vendor benchmark.** No published throughput figure exists for this stack, so every absolute
number here is either derived from the capacity model or bound by a load test on the target
infrastructure. What is enforced is not a throughput number but the **SLO**, which is a
formal release gate in CI (ADR-0041).

Numbers marked interim are exactly that: the architecture is built to meet whatever is
ratified, and the gate uses the ratified figures rather than these.

## 2. Quality-attribute targets

| # | Attribute | Target | How measured | How it is met |
|---|---|---|---|---|
| N1 | Token-endpoint latency | p95 < 200 ms, p99 < 500 ms (interim) | Load-test threshold, enforced in CI | [12](12-performance-and-scalability.md) |
| N2 | Local validation latency | p99 < 50 ms, no network hop (interim) | Instrumentation | [12](12-performance-and-scalability.md) |
| N3 | Throughput | Whatever the load test establishes at the 10k-user workload mix; not a fixed figure | Load test | [12](12-performance-and-scalability.md) |
| N4 | Availability, token and authorize | **99.9% or 99.95%, and the choice between them is unratified** (see below) | SLO monitor over a trailing window | [13](13-reliability-backup-and-dr.md) |
| N5 | JWKS availability | Held **higher** than the service's own, around 99.99% | SLO monitor | [13](13-reliability-backup-and-dr.md) |
| N6 | Scalability | Horizontal and stateless, with no session affinity | Scale-out load test | [12](12-performance-and-scalability.md) |
| N7 | Recovery | Bound **per store**, never one global figure, with the keyring strictest | DR drill plus continuous monitoring | [13](13-reliability-backup-and-dr.md), ADR-0006, ADR-0074 |
| N8 | Configuration propagation | The engine's entity cache is per-request, so it needs no backplane; any added configuration cache is within 30 s cross-node | Synthetic canary | [12](12-performance-and-scalability.md), ADR-0039 |
| N9 | Revocation freshness | Per path, not one number: reference token immediate, a self-contained JWT until expiry at the 15-minute access TTL (ADR-0004), force-logout within the 1-to-2-minute validation interval (ADR-0003), key break-glass within 60 s, configuration within 30 s | Synthetic canary and tests | ADR-0039, runtime view 4 |
| N10 | Protocol security | Conforms to the OAuth 2.0 security best current practice, and passes a conformance suite | Audit plus conformance suite | Security view, ADR-0062 |
| N11 | Tenant isolation | No cross-tenant read, write, or token acceptance | Cross-tenant negative tests as a permanent acceptance criterion, plus spikes A-4 and A-7 | [05-data](05-data.md), ADR-0001, ADR-0049 |
| N12 | Operability | Key rotation with no restart (ADR-0011), graceful shutdown, and readiness gated on keys loaded (ADR-0031) | Contract-regression and chaos tests | [16](16-operations-and-maintenance.md) |
| N13 | Evolvability | Additive schema evolution through expand-and-contract; a pinned-version contract regression on every engine bump | CI, including a pending-model-changes check and pipeline snapshots | ADR-0017, ADR-0021 |
| N14 | Data-protection posture | Erasure, retention, and audit are **designed for**, and **no compliance verdict is asserted** | Data-protection-owner and Legal ratification | ADR-0016, ADR-0053, ADR-0054 |

Two rows carry more than a number and are worth reading twice. **N9 is per path**, and
collapsing it into a single "revocation is fast" would be wrong by an order of magnitude
depending on which path is meant. **N14 asserts a design posture, not compliance**: the
mechanisms exist, and whether they satisfy a given regulation is a decision reserved to
Legal and the data-protection owner, not something this architecture can claim.

## 3. Service-level objectives and the error budget

An SLI is a measured quantity, an SLO is a target on it, and an SLA is an SLO plus
consequences. Nami needs a **higher** SLO than the applications that depend on it, because
it sits on the critical path of every login. 100% is the wrong target: the residual
**error budget** is what governs the release freeze.

| SLI | Interim target | Window |
|---|---|---|
| Token-endpoint latency | p95 < 200 ms, p99 < 500 ms | Trailing, 28 days proposed |
| Availability, token and authorize | 99.9% or 99.95%, unratified | Trailing |
| Local validation latency | p99 < 50 ms | Trailing |
| JWKS availability | Around 99.99% | Trailing |
| Error budget | **`1 - SLO`**, stated as a formula | Drives the freeze |

**The error budget is deliberately written as a formula rather than a figure, and that is a
decision rather than an omission.** It is 0.1% at 99.9% and 0.05% at 99.95%. Because the
budget drives the release-freeze policy, quoting one number while the availability target is
still open would set the freeze threshold **wrong by a factor of two** (ADR-0041). This view
therefore refuses to pick one, and so should any dashboard built from it.

Burn is alerted with multi-window multi-burn-rate rules, and the burn tier wires directly to
a freeze level rather than to a human decision. The mechanics are in the observability view.

## 4. Ratification status

The interim numbers become binding only when their named owner ratifies them. Tracked in the
[Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md).

| Item | Owner | State |
|---|---|---|
| The SLO table, the availability choice, and the error-budget policy | Product and Ops | Interim |
| The workload mix (logins per day, peak factor, machine-to-machine ceiling) | Product | Interim, accepted as a working basis |
| Recovery objectives per store, and the failover mechanism | Ops and Security | Interim |
| Erasure service levels, retention, and residency | Data-protection owner and Legal | Reserved to them; this view asserts no verdict |
| The capability taxonomy, assurance tiers, and dual-control roles | Security | Interim |

Two of those are **not** merely unratified numbers but reserved judgements: the
data-protection items are reserved to their owner by policy, and no amount of architectural
work converts them into an engineering decision.

## Sources

* ADR-0041 owns almost everything here: the self-load-tested posture rather than vendor
  quotes, the p95 and p99 latency targets, the 99.9%-or-99.95% availability choice being an
  explicit ratification rather than a settled value, the higher JWKS target, the error budget
  as a formula and the named reason for keeping it one, the burn tiers, and the SLO as a
  formal CI release gate.
* ADR-0039 (the per-path revocation and configuration-propagation bounds behind N8 and N9),
  ADR-0004 and ADR-0003 (the 15-minute access TTL and the validation interval those bounds
  rest on).
* ADR-0001 and ADR-0049 (the isolation property N11 states, and the negative tests that are
  its permanent acceptance criterion), ADR-0062 (the verification baseline behind N10),
  ADR-0017 and ADR-0021 (the evolvability mechanisms behind N13), ADR-0016, ADR-0053, and
  ADR-0054 (the data-protection mechanisms behind N14, and the reservation of the verdict).
* ADR-0011 and ADR-0031 (the operability properties behind N12), ADR-0006 and ADR-0074 (the
  per-store recovery framing behind N7).
* Reconciled against the design corpus's non-functional-requirement catalogue on 2026-07-25.
  Taken from it: the requirements-versus-mechanisms separation that gives this view its
  purpose, the attribute table shape, the SLI-SLO-SLA distinction, and the ratification-state
  table. **Corrected rather than imported, and this is the sharp one:** the corpus states
  availability as a firm 99.95% and the error budget as "0.05%, about 21 minutes a month".
  ADR-0041 leaves the availability choice explicitly unratified and states the budget as a
  formula **for the stated reason** that quoting a figure while the target is open sets the
  freeze threshold wrong by a factor of two. The corpus therefore commits precisely the
  mistake our own decision record names in advance, so both rows are stated as the ADR does.
  Also corrected: the corpus gives a single revocation-freshness line, where ADR-0039 makes
  it per path with four different bounds.

---

[Prev: Stakeholders and concerns](09-stakeholders-and-concerns.md) · [Index](README.md) · Next: [Security architecture](11-security-architecture.md)
