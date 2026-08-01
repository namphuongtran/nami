---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Kubernetes API-server health-check documentation and ASP.NET Core health-check documentation (both fetched and quoted at source 2026-08-01); `draft-inadarei-api-health-check`; ADR-0020 (the `RequireActor` policy this carves the only exemption from); ADR-0012 (the keys-loaded readiness gate whose content this ADR deliberately does not restate); ADR-0031 (the readiness flip in the shutdown sequence)
informed: Nami.Identity.Host and Admin API implementers, the platform team wiring the probes
---

# Serve two anonymous probe routes, `/health/live` and `/health/ready`, on both hosts

## Context and Problem Statement

Two hosts need probes and they deploy separately: the IdP runtime and the Admin API
(ADR-0020). Three spellings were circulating in this repository (`/health`,
`/health/live`, `/health/ready`) and a fourth, `/healthz`, is widespread in the wild,
so an implementer had no way to choose without guessing.

The path string is the least important part of this decision and the part that
attracts the most argument. What actually matters, and what was written nowhere, is
that the Admin API's probes must be **anonymous**. That makes them the **only**
exemption from `RequireActor` on a host whose entire posture is "a user-delegated
token or nothing" (ADR-0020, which rejects any token with no `sub`). An undocumented
auth exemption gets built wrong in one of two directions: someone applies the policy
uniformly and the pod never reaches Ready, or someone opens the route and serves the
full dependency report to anyone who asks.

## Decision Drivers

* A kubelet carries **no bearer token**. Probe routes therefore cannot require
  authentication. This is a constraint, not a preference, and it is why the exemption
  cannot be avoided by tightening something.
* A liveness probe that fails for a dependency reason **kills a pod that only needed
  to wait**, so liveness and readiness must be genuinely different checks rather than
  two names for one.
* A health report names internal dependencies and their state, which has
  reconnaissance value on a public path.
* Renaming a probe path is pure churn: it touches several designs plus the chart and
  the manifests, for no functional gain.

## Considered Options

* **A. `/healthz`**, a single endpoint, the most widely recognised spelling.
* **B. `/livez` and `/readyz`**, the current Kubernetes API-server spelling.
* **C. `/health/live` and `/health/ready`**, which most of this repository already
  used.

## Decision Outcome

Chosen: **Option C**, because **there is no standard to conform to**, so the only
rational tiebreakers are internal consistency and zero churn.

**The evidence, recorded so this is not researched a third time:**

* **There is no IETF standard for the health-check path.** The only relevant IETF
  work, `draft-inadarei-api-health-check`, defines a **response media type**
  (`application/health+json`) and never a path, and it expired without becoming an
  RFC.
* **`/healthz` is deprecated in the system it comes from.** The Kubernetes
  documentation, fetched and quoted 2026-08-01, states: *"The `healthz` endpoint is
  deprecated (since Kubernetes v1.16), and you should use the more specific `livez`
  and `readyz` endpoints instead."* The trailing `z` is Google internal-tooling
  heritage rather than a specification, so copying `/healthz` today copies a
  deprecated form.
* **That same page is about the Kubernetes API server's own endpoints, not a
  recommendation to applications running on the cluster**, which is the reason
  Option B is not simply "the current standard". `/livez` and `/readyz` are the
  platform's convention for itself.
* **The ASP.NET Core documentation recommends no path.** It uses `/healthz` in
  current samples and `/health` in older ones with no stated preference. It does
  firmly recommend the **split**, and its own example is `/healthz/ready` plus
  `/healthz/live`, which is structurally identical to Option C with the vestigial
  `z` dropped.

**The rules that bind:**

1. **Two endpoints, never one.** `GET /health/live` for liveness and
   `GET /health/ready` for readiness, separated by health-check tag.
2. **Liveness never touches a readiness dependency.** It must not query the database,
   the key store, or anything else external. Otherwise a slow dependency restarts a
   healthy pod, and a pod that is deliberately draining gets killed mid-drain.
3. **Both routes are anonymous, on both hosts.** On the Admin API this is an
   **explicit, deliberate, and singular** exemption from `RequireActor`. It is
   recorded at ADR level precisely because the exemption is invisible in the code
   that grants it: nothing about a `MapHealthChecks` call announces that it is the
   one route on the host not carrying the policy.
4. **Status code only on the public route, with no detail body.** The
   dependency-level report is not exposed. The framework offers both an
   authorization requirement and a host or port restriction for this; the choice here
   is **no detail** rather than auth, because auth is exactly what the kubelet cannot
   satisfy. If a detailed report is ever wanted it goes on a **separate management
   port**, never behind a token on the same route.
5. **The Admin API gets its own probes.** It deploys separately from the IdP runtime
   and must not be judged ready or live by the runtime's state.
6. **What the readiness check contains is owned elsewhere and is not restated here.**
   The keys-loaded gate, including its requirement that the Data Protection check
   asserts a **`kid` match rather than a bare protect-and-unprotect round trip**,
   belongs to ADR-0012 and the observability design. This ADR fixes the contract, not
   the check list, and duplicating the list here would create a second source of
   truth for it.

### Consequences

* Good, because the naming question now has an answer with sources attached and
  cannot consume time again.
* Good, because the anonymous exemption is written where someone reviewing the Admin
  API's security posture will actually meet it, rather than being discovered by
  whoever debugs a pod that never becomes Ready.
* Good, because almost nothing changes: this ratifies what most of the repository
  already specified, and only one stale route list needed fixing.
* Bad, because `/health/live` is not the `/livez` a Kubernetes-native operator might
  reach for. Accepted, because these are documented routes in a chart we ship rather
  than endpoints discovered by convention.
* Neutral, because rule 4 gives up a convenient debugging view on the public path.
  The management-port option stays available if Ops later wants one.

### Confirmation

* Both external sources were fetched and quoted at source on 2026-08-01 rather than
  carried from a summary, and doing so produced the Option B refinement above: the
  Kubernetes page is the API server describing its own endpoints, which weakens the
  "conform to the platform" argument that would otherwise have favoured `/livez`.
* **One drift was found and fixed in the same change:** the admin API design listed
  its meta routes as `GET /health` and `GET /health/ready`, so it both used the bare
  spelling and omitted liveness entirely, while eight other sites across the design
  layer already said `/health/live` and `/health/ready`.
* **The anonymous exemption was absent everywhere**, while `RequireActor` is stated
  in ADR-0020 and in the admin design as rejecting any token without a `sub`. Nothing
  recorded that the probes are exempt, which is the "someone applies the policy
  uniformly and readiness never passes" failure this ADR exists to prevent.
* Tests: liveness must not touch a readiness dependency; readiness fails when signing
  or encryption keys are missing or the active Data Protection `kid` does not match
  the expected persisted `kid`; and a route inventory asserts the Admin API exposes
  **exactly two** anonymous routes, so a third one is a finding rather than a
  convenience.

## Pros and Cons of the Options

### A. `/healthz`

* Good, because it is the most widely recognised spelling.
* Bad, because it is a **single** endpoint, which conflicts with the liveness-versus-readiness
  driver and with the framework's own recommendation to split.
* Bad, because Kubernetes deprecated it in v1.16, so the familiarity argument points
  at an obsolete form.

### B. `/livez` and `/readyz`

* Good, because it is correctly split and is what a Kubernetes operator recognises.
* Bad, because it is the **platform's convention for its own API server**, not
  guidance to applications, so adopting it borrows authority the source does not
  offer.
* Bad, because it would rename routes across several designs plus the chart and
  manifests for no functional gain.

### C. `/health/live` and `/health/ready` (chosen)

* Good, because it is correctly split, matches the framework's documented example
  shape, and is already what most of this repository specifies.
* Good, because the hierarchical form leaves room for a future sibling without
  inventing another top-level path.
* Bad, because it is not `/livez`, so an operator used to that spelling has to read
  the chart.

## More Information

* **Kubernetes, API server health endpoints**
  (`https://kubernetes.io/docs/reference/using-api/health-checks/`), fetched and
  quoted 2026-08-01.
* **ASP.NET Core health checks**, the readiness-versus-liveness split guidance and
  the access-control options, consulted 2026-08-01. No path is recommended there.
* `draft-inadarei-api-health-check` defines `application/health+json` as a response
  format only and expired without becoming an RFC.
* Mechanism: the probe wiring and the readiness check list live in design
  [19](../design/19-observability-capacity-slo.md) and
  [12](../design/12-key-management.md); the admin host's routes are in design
  [15](../design/15-admin-api.md).
* Related decisions: ADR-0020 (`RequireActor`, the policy this exempts), ADR-0012
  (the keys-loaded gate and the `kid`-match requirement), ADR-0031 (the readiness
  flip during graceful shutdown, which depends on liveness not failing while a pod
  drains), ADR-0079 (the admin API's other HTTP conventions, which deliberately do
  not govern these routes).
* Imported from the design corpus's probe-contract decision on 2026-08-01, with both
  external citations re-fetched at source.
