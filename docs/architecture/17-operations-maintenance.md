---
status: reviewed
created: 2026-07-25
tags: [architecture, operations, runbooks, break-glass, upgrades]
---

# Operations and maintenance

> **Part of:** the [Software Architecture Document](README.md), quality and operational
> views.

How the system is run and kept healthy over time: runbooks, the background jobs an operator
has to reason about, key operations, the two break-glass paths, and the upgrade cadence. What
breaks and what it costs to recover is
[22-reliability-backup-dr](22-reliability-backup-dr.md); what is measured and alerted
is the observability view; where it all runs is [10-deployment-infrastructure](10-deployment-infrastructure.md).

## 1. Runbooks, with the linkage enforced rather than encouraged

**A page-severity alert with no runbook is a defect, and it is blocked in CI** (ADR-0041).
That inversion is the point: the usual failure is an alert that fires at 3am with nothing
attached, and the usual fix is documentation discipline, which decays. Making it a build
failure means the alert cannot ship without the procedure.

The runbook contents live with Ops; what this view fixes is the set and its triggers.

| Runbook | Trigger | Gist |
|---|---|---|
| Burn-rate response | Fast or mid-burn on latency or availability | Triage saturation against dependency failure; the release freeze applies automatically at the burn tier (ADR-0041) |
| JWKS unavailable | Burn on JWKS availability | JWKS down means **every** verification fails everywhere, which is why its target is held higher than the service's own; restore the publication path first |
| Keys not loaded | Readiness failing on the keys check | A keyring or signing-key load failure. Do **not** blind-restart pods: if the keyring is the problem, a restart can mint a fresh one and make the loss permanent (ADR-0012) |
| Key rotation overdue | Days-to-expiry running low | Trigger or verify rotation before expiry, rather than discovering it as a signing failure (ADR-0011) |
| Scheduler stale | A stale last-successful-run heartbeat | Prune or rotation missed a window. Clustering prevents a double run, not a **dead** run, so the heartbeat is the only signal that anything is wrong |
| Load shed sustained | A sustained 503 pattern | Distinguish genuine capacity from abuse, then scale or shed accordingly (ADR-0040, ADR-0042) |
| DR restore | A recovery event | Restore per store against its own objective, including the restore-both keyring and signing-key exercise (ADR-0012) |
| Recovery-point breach | Archiving lag, backup age, or replication lag | Inspect the backup and archiving pipeline. A stale backup is invisible to the DR drill, which is why this is a separate runbook (ADR-0074) |
| Key compromise | Unexplained keyring access, or a bad `kid` | Distrust the `kid` fail-closed and rotate it out of the JWKS inside the five-minute objective (ADR-0007) |
| Admin break-glass | The provider cannot issue tokens at all | Unseal the emergency path, audit before action, repair, rotate after use (ADR-0015) |
| Abuse response | Credential-stuffing, lockout denial-of-service, or an MFA-failure spike | Risk-triggered challenge scoped per source, not a global tightening (ADR-0042) |

## 2. Background jobs an operator has to reason about

The pattern each job uses is fixed by ADR-0031 and summarised in
[07-container-view](07-container-view.md). What matters operationally is narrower:

**Clustering prevents a double run. It does not prevent a dead run.** A clustered scheduler
guarantees that exactly one replica's trigger fires; it guarantees nothing about the trigger
firing at all. So every scheduled job emits a **last-successful-run heartbeat**, and the
alert is on the heartbeat going stale beyond roughly two intervals, with a
days-to-expiry backstop for rotation specifically, because rotation is the job whose silent
failure has a deadline attached (ADR-0011, ADR-0031).

Two further operational facts:

* **Pruning iterates tenants explicitly** (a Pool filter, or the dedicated connection per
  Silo), because tenant-partitioned data cannot be pruned in one global pass. It runs as its
  own invocation off the request path (ADR-0031).
* **The prune retention floor must exceed the longest refresh-token lifetime**, or entries
  still needed for reuse detection are removed early and a legitimate reuse stops being
  detectable (ADR-0004).

## 3. Key operations

* **Rotation costs no restart.** A new key is inserted, promoted to active with the previous
  one retired, and the options-monitor change token swaps the signing credential in process
  (ADR-0011).
* **The overlap window has to beat the client cache, and the two numbers are easy to
  confuse.** The propagation and retention windows are about **14 days** each, which exceeds
  the **client-side JWKS refresh default of 12 hours**, so in-flight tokens keep validating
  and a retired key is deleted only afterwards. The **24-hour** figure that also appears in
  the key design is the **server-side in-memory key-cache TTL** (dropping to one minute when
  a new key exists), which is a different thing entirely. Mixing them up produces a
  plausible-sounding but wrong conclusion about how long a retired key must stay published
  (ADR-0011, ADR-0007).
* **Break-glass is the same machinery run fast**, not a separate mechanism: mark revoked,
  push to the distrusted-key set, evict the JWKS caches, and shorten the resource server's
  refresh interval to its 5-minute floor (ADR-0007, ADR-0039).

## 4. Two break-glass paths that are not interchangeable

They are named similarly and solve unrelated problems, so conflating them is a real
operational risk.

**Key-compromise break-glass (ADR-0007).** A signing key is compromised. Eject the `kid`
from the JWKS within five minutes and distrust it fail-closed. Its **mass-revocation and
session-purge trigger requires dual control** with proposer and approver distinct, and that
is an **accepted** decision. Drill cadence: **quarterly and after every key-infrastructure
change**, also accepted.

**Admin break-glass (ADR-0015).** The provider itself cannot issue tokens (no signing key,
empty store, misconfiguration) and the admin app, being an OIDC client of the very provider
it manages, therefore cannot log in. This is a **separate cookie scheme** (`__Host-bg`,
path-scoped, with a 15-minute hard cap) protected by **Data Protection rather than the
signing key**, so it works with no key and no JWKS, which is exactly the situation needing
rescue. Gated by a feature flag defaulting off plus an IP allow-list that returns **404** to
hide the endpoint rather than 403 to advertise it. Two **sealed** accounts, not ordinary user
rows, with hashes verified from the secret store at the **hardened** iteration count rather
than the framework default, because these are the most privileged credentials in the
deployment and the one place that must not silently sit at a default (ADR-0028). **Audit before action**: the Severity-0 record is written *before*
sign-in and a sink failure is fail-closed. Rotate after every use; exempt from lockout.
Drill cadence: **every 90 days and after each staff change**, and the drill must confirm it
can be flipped on **while the provider is down**, since a default-off flag must not be
unavailable precisely when it is needed.

**One distinction inside ADR-0015 is worth stating carefully**, because reading it loosely
produces a contradiction that is not there. The ADR fixes **split custody**: the password
and the second factor are held by two different custodians, so no single person can use the
path. It leaves **approval** open: whether unsealing additionally requires a second approver
is a Security and data-protection-owner ratification item. Split custody of a credential and
an approval step are different controls, so this view describes the admin path as
split-custody and **not** as dual-control-approved, unlike the ADR-0007 trigger above.

**Never autonomous on irreversible or outward-facing actions.** Tenant deletion, mass
revocation, delegated-admin changes, data export and erasure all pass through the
server-side dual-control saga with proposer distinct from approver and an ETag re-check at
execution (ADR-0020, ADR-0010, runtime view 2).

## 5. Upgrade cadence

| What | Policy |
|---|---|
| .NET runtime | **LTS to LTS, skipping STS**: 10 to 12, skipping 11. This is seamless rather than a compromise, because .NET 10 is supported to roughly November 2028 while .NET 12 ships in November 2027, leaving no end-of-life gap. A security-critical identity service must never run an end-of-life runtime; the STS is built in an early-warning branch and never shipped (ADR-0030) |
| Protocol engine | Pinned to one version. **Every** bump runs the contract-regression suite before merge: the pipeline-snapshot test, a re-verification of every native-versus-build verdict, the seam catalogue, and the endpoint model (ADR-0021) |
| ORM, driver, tenancy library | Pinned, with the version-sensitive behaviours re-verified on each bump rather than assumed stable |
| Base image | Chiseled and digest-pinned; rebuilt, rescanned, and resigned on the base-image CVE cadence (ADR-0025, ADR-0051) |

The philosophy behind the contract-regression suite is the recurring lesson of this project:
**a version bump can silently reorder handlers or flip a native-versus-build verdict.** The
suite exists so that failure lands in CI rather than in production, where it would look like
an unrelated authorization bug (ADR-0021).

**An upgrade is also how features leave, not only how they arrive.** Several capabilities are
**build-interim**: Nami implements them because the engine does not, and each carries a
decommission marker so it retires when a native equivalent ships rather than accumulating as
permanent parallel code. That set is DPoP, back-channel logout, the token-exchange delegation
logic, and the custom telemetry meter. Reviewing those markers is part of a bump, which is
why an upgrade is scheduled work rather than a dependency bump (ADR-0021, ADR-0014, ADR-0019).

**Not everything waiting on the engine is a build-interim, and the difference changes what a
bump means.** Dynamic client registration is the counter-example: the standard endpoint waits
on the engine, but Nami did not build an interim of it. It chose a **different mechanism**,
self-service registration through the authenticated Admin API, as a decision rather than a
placeholder (ADR-0035). So a native registration endpoint arriving does not retire anything;
it would be a new option to evaluate. Treating the two shapes alike would put a decision on
the retirement list by mistake.

## 6. Deploy, configuration, and disposability

* **Migration is a Job, never a startup step in production.** The application runs `serve`
  and auto-seeds only key material, which is data rather than schema (ADR-0025, ADR-0017).
* **Configuration precedence** is environment, then secret store, then
  `appsettings.{Environment}`, then `appsettings`. No secret is in the image. Production logs
  to stdout and OTLP only (ADR-0031).
* **Graceful shutdown with a readiness flip** makes rolling deploys zero-downtime, and
  **liveness must never probe readiness** or the platform kills draining pods (ADR-0031).
* **Declarative configuration import and export** through the `export` mode supports GitOps
  and environment migration; import is an idempotent upsert carrying no secrets, and it does
  not overwrite an operator's live edit unless forced (ADR-0027, ADR-0052).

## 7. Incidents and on-call

Alerts deduplicate into one incident per `(rule, deployment, tenant scope)`, so many pods
raise one incident and a multi-tenant fault does not page once per tenant (ADR-0041, with
the key itself fixed in the observability design). Escalation runs primary, then secondary,
then team lead, with an acknowledgement timeout that re-pages. The roster and the timeouts
are an Ops ratification item.

Handling of a suspected leak, an unauthorised action, or a prompt-injection incident follows
the organisation's security policy: stop, notify security and the data-protection owner
within 24 hours, and preserve the inputs. For an AI-assisted change specifically, the
disclosure trailers on the commit are what make the input set reconstructable after the fact
(ADR-0067).

## 8. What is not settled here

Operations-facing items awaiting ratification before production, tracked in the
[Pre-GA Ratification Checklist](../PRE-GA-RATIFICATION-CHECKLIST.md):

* The concrete SLO numbers, the error-budget policy, and the on-call roster (ADR-0041).
* Recovery-time and recovery-point numbers per store, and the failover mechanism (ADR-0006,
  ADR-0074).
* Whether Redis durability is enabled, given the quantified replay-window trade-off, and the
  read-replica trigger threshold if that lever is ever adopted (ADR-0074).
* Whether unsealing admin break-glass requires a second approver, plus custody, rotation
  cadence, the alert recipients, and the network allow-list (ADR-0015).
* The authorised-personnel list for key-compromise break-glass and the multi-node
  cache-eviction automation (ADR-0007).
* The edge stack and the trusted-proxy ranges (ADR-0073).

## Sources

* ADR-0041 (the runbook-per-page-alert rule as a CI gate, burn-rate tiers and the automatic
  freeze, the JWKS target held higher than the service's own, and the unratified SLO table),
  ADR-0040 and ADR-0042 (load shedding versus abuse, and the scoped challenge response).
* ADR-0011 (no-restart rotation, the 90/14/14 windows, the server-side key-cache TTL that is
  **not** the client cache, and the heartbeat), ADR-0007 (the five-minute ejection, the
  accepted dual-control trigger, and the quarterly-plus-post-change drill), ADR-0012 (the
  keyring load failure and why a blind restart is dangerous), ADR-0039 (the distrusted-key
  set and the 5-minute refresh floor).
* ADR-0015 (the separate cookie scheme, the 404 gating, two sealed accounts with split
  custody, audit-before-action, rotate-after-use, the 90-day drill that must work while the
  provider is down, and the explicitly unratified approval question), ADR-0020 and ADR-0010
  (the dual-control saga for irreversible actions), ADR-0028 (the hardened hash iteration
  count the sealed credentials use).
* ADR-0030 (the LTS-to-LTS cadence with its dated no-gap argument), ADR-0021 (the
  contract-regression suite, what each bump re-verifies, and the decommission markers that
  make a build-interim feature retire rather than persist), ADR-0014, ADR-0019, and ADR-0035
  (the build-interim capabilities carrying those markers), ADR-0025 and ADR-0051 (the
  image cadence and resigning).
* ADR-0031 (migration as a Job, configuration precedence, stdout-only logging, graceful
  shutdown, and the liveness rule), ADR-0027 and ADR-0052 (the export mode and idempotent
  import), ADR-0017 (why key seeding is data rather than schema and migration stays a Job),
  ADR-0004 (the prune retention floor against the refresh lifetime), ADR-0067 (the
  disclosure trailers that make an AI-assisted change reconstructable).
* Unratified items in section 8 belong to their owners: ADR-0006 and ADR-0074 (recovery
  numbers, the failover mechanism, Redis durability, and the read-replica threshold) and
  ADR-0073 (the edge stack and trusted-proxy ranges).
* Reconciled against the design corpus's operations view on 2026-07-25. Taken from it: the
  runbook set and its enforced linkage, the heartbeat reasoning that clustering prevents a
  double run but not a dead one, the 12-hour-client-cache versus 24-hour-server-cache
  disambiguation, the two-break-glass-paths structure with its drill cadences, the
  upgrade-cadence table, and the incident deduplication key. Corrected rather than imported:
  the corpus describes "exactly one clustered Quartz runner" as a deployed process, which
  misdescribes clustered scheduling and is not adopted; its claim that readiness
  self-validates after the change token appears in neither the owning ADR nor the
  corresponding detailed design, so the sourced three-part readiness gate is used instead;
  and it states the admin break-glass unseal as dual-controlled without qualification, where
  ADR-0015 fixes split custody and leaves the approval question unratified, a distinction
  this view preserves.

---

[Prev: Observability and monitoring](16-observability-monitoring.md) · [Index](README.md) · Next: [Decisions index](18-decisions-index.md)
