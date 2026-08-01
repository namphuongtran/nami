---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the design corpus's admin-API wire specification (`api/admin-api.v1.yaml` and all forty-six path keys under `api/paths/`, read at source 2026-08-01, not through the `DD/` digest); ADR-0020 (the build-time follow-up this closes); ADR-0079 and ADR-0089 (the two custom surfaces and the route-family boundary); ADR-0044 and ADR-0087 (the versioning and locking mechanisms that turn out not to reach a URL); ADR-0080 (the probe routes this excludes); the cached `Asp.Versioning` package metadata (read from the `.nuspec` files 2026-08-01)
informed: Admin API and Admin App implementers, anyone generating a client from the published contract, the platform team wiring the chart and the manifests
---

# Serve Nami's own APIs under the base path `/api/v1`, and rule per route family which URLs carry a version

## Context and Problem Statement

The version prefix of Nami's HTTP surface had never been decided. Its only trace in this
repository was one clause in an accepted ADR's Confirmation section: "Build-time follow-ups:
an API versioning scheme (path `/v1` proposed)". That wording is now quoted at
[`0020-admin-architecture.md:67`](0020-admin-architecture.md), because the change that adds
this ADR closes the follow-up and keeps the original sentence rather than deleting it.
Counted on 2026-08-01 against the tree this ADR
was written on, the tracked markdown held **zero** occurrences of `/api/v1` and exactly
**one** of `/v1`, and that one was the clause above. So the scheme was proposed, unadopted,
and cited by nothing.

That would be a tolerable omission if something else covered it. Three things look as
though they do, and none does.

* **ADR-0079 defers to a discipline that does not reach a URL.** Its related-decisions list
  named ADR-0044 as "the public-surface versioning discipline this sits inside". Read in
  this repository on 2026-08-01, ADR-0044 contains **no occurrence of route, endpoint, HTTP,
  verb, or status code**, and its only URL-shaped string is the `<migration url>` placeholder
  at line 35. Its section F governs the DTO **assembly** and its `V1` namespace, which is a
  set of types. That parenthetical was therefore a citation that resolves and does not
  support; it is corrected in the same change as this ADR, and the original wording is
  preserved in the correction at
  [`0079-admin-api-http-conventions.md:260`](0079-admin-api-http-conventions.md).

* **ADR-0087 locks the surface and cannot see this string.** Its section B enumerates what
  the snapshot locks: "Path templates including parameter names, methods, required request
  headers, response status codes, and schema names and required-ness." In an OpenAPI
  document the version prefix lives in the `servers` entry, not in a path template. So the
  single most breaking change available to this surface, moving every operation's URL at
  once, is invisible to the gate built to make breaking changes appear in a diff.

* **The design layer looks unversioned and is in fact merely relative.** Design 15 declares
  `/tenants/{tenantId}/applications` and its siblings with no version segment, which reads
  like an omission and is not one: the corpus wire specification the surface was reconciled
  against declares every one of its forty-six path keys relative and puts the version once
  in the server entry, `- url: https://{adminHost}/api/v1`
  (`api/admin-api.v1.yaml:35`; the tenant-scoped form is spelled out at
  `25-design-admin-api.md:58` as `/api/v1/tenants/{tenantId}/...`). What is missing here is
  only the base, and nothing in this repository records it.

The cost of leaving it open is the cost ADR-0079 already argued for its own rules, raised.
ADR-0020 makes the API and the App separate deployables consuming a generated client, so a
path change after the first release is a breaking change across a project boundary. A base
path is in **every** URL a consumer writes, so it is the most expensive single string on the
surface to decide late.

## Decision Drivers

* A base path appears in every URL, so deciding it late costs more than any other route
  decision, and there is no code yet.
* Each rule should be decidable from a fact already recorded in this repository, which is
  ADR-0079's own drafting requirement and the reason its rules can be checked rather than
  debated.
* Several route families exist and they are not one surface. A rule stated for "the API"
  without saying which family is how a probe path acquires a version prefix nobody chose.
* **Exactly one mechanism may put a segment into `Request.PathBase`**, because the engine
  infers the per-request issuer from it (design [02](../design/02-data.md) at lines 1179 to
  1191). Any new prefix therefore has to state its relation to the tenant segment rather
  than leave it to be discovered by whoever writes the second mechanism.
* Some of these URLs are written down by someone other than us: a probe path lives in the
  chart and the manifests, and a webhook URL lives in an email provider's configuration.
  Versioning one of those turns an API release into an operational change we cannot schedule.

## Considered Options

* **A. `/api/v1` as a base path, with route templates staying relative**, matching the
  corpus wire specification.
* **B. A bare `/v1` base path**, which is what ADR-0020 proposed.
* **C. Version in a request header or a media type**, leaving URLs unversioned.
* **D. Leave it to build time**, which is the current state.

## Decision Outcome

Chosen: **Option A**. Five rules follow. They bind the design layer and the published
contract.

### 1. The base path is `/api/v1`, and route templates stay relative

The version appears **once**, in the base path, and never inside a route template. A
tenant-scoped admin collection is therefore
`https://{adminHost}/api/v1/tenants/{tenantId}/applications`, assembled from a base and a
relative template rather than declared as one string.

**No route declaration in design 15 changes because of this decision**, and that is the
check that the shape is right rather than a convenience: the corpus specification already
holds all forty-six of its path keys relative with the version in `servers`
(`api/admin-api.v1.yaml:35`), so this ADR supplies the base that was missing rather than
rewriting the surface that was present.

**Why `/api/v1` rather than the `/v1` ADR-0020 proposed.** Two reasons, of unequal weight,
and the weaker one is marked as such rather than dressed up.

* The `api` segment separates the custom surface from the protocol endpoints, which are
  configured at `connect/*` on the runtime host (design
  [04](../design/04-core-protocol.md) at lines 68 to 69) and are found by a consumer in the
  discovery document rather than by convention. On a host serving both, one segment makes
  the boundary legible in the URL itself.
* A weaker, legibility-only argument: **`v1` unqualified already means the release in this
  repository, not an API version.** The token `v1` or `v2` occurs 437 times across 78
  tracked markdown files under `docs/`, counted 2026-08-01, and the sense in the instances
  read is the release scope: `architecture/19-evolution-and-extensions.md:11` says "Where
  the architecture goes after v1" and line 35 labels a diagram subgraph "v1 core, the
  production target", and ADR-0034's title scopes a feature to v2. That is a token count
  with a sampled sense, not a proof about all 437, and it is offered as a tiebreaker rather
  than as a driver.

### 2. A route family carries the base path only if all three clauses hold

This is the rule that stops the question being reopened per endpoint, and it is stated as a
test rather than as a list so a family added later is decidable without amending this ADR.

**A route carries the `/api/v1` base if and only if (a) Nami serves it, (b) a consumer
writes code against it, and (c) Nami decides when it may break.** A family failing any
clause keeps its current path, at the host root.

| Route family | Declared at | Base path | Clause that decides it |
|---|---|---|---|
| Admin API | design [15](../design/15-admin-api.md) section 3 | **yes** | all three hold; this is the surface a generated client is built from (ADR-0079) |
| Self-service | design [08](../design/08-user-management.md) at lines 407 and 430 | **yes** | all three hold; ADR-0089 rule 5 already imports the paging and problem-details rules, which are the rules of a machine contract |
| OAuth and OIDC protocol endpoints | design [04](../design/04-core-protocol.md) at lines 68 to 69 and 129 to 130 | no | fails (c). Their shapes belong to their specifications and to OpenIddict, and a consumer resolves them through discovery, so we do not decide when they change |
| Health probes | ADR-0080, design [15](../design/15-admin-api.md) section 3.9 | no | fails (b) and (c). ADR-0080 fixes them at the host root on both hosts, and the path is a constant in the chart and the manifests rather than something a consumer compiles against |
| BFF endpoints | design [24](../design/24-bff.md) at lines 60 to 63 | no | fails (a). `/bff/*` and the `/api/*` proxy are routes in the **adopter's** application, which ADR-0029 ships a package for; Nami cannot version a URL it does not serve |
| Human-facing Razor Pages | design [11](../design/11-login-consent-ui.md) at lines 76 to 78, 312 and 341 | no | fails (b). `/Account/Login`, `/Account/Logout`, `/Account/StepUp`, `/Consent` and `/account/passkey/*` are navigated by a browser, and a version in a login URL would land in bookmarks and in the return-URL allow-list |
| Break-glass | ADR-0015, design [15](../design/15-admin-api.md) section 5.3 | no | fails (b). It is opened by a human during an incident, and its cookie is scoped by `Path=/breakglass`, so moving the route moves the cookie scope |
| Inbound provider webhooks | design [10](../design/10-email-notification.md) at line 376 | no | fails (c). `/webhooks/email/{provider}` is registered in each email provider's own configuration, so we cannot make a provider move, and an old provider still posting to a retired URL is a delivery outage rather than a client error |

Two things about that table are worth stating because they are the parts a reader is most
likely to get wrong.

**On the webhook, the answer is not "webhooks should be versioned so both can run".** They
can, and that is not the question. The question is whether it shares **this** clock. The
webhook payload contract changes when a provider changes its format, on the provider's
schedule; the admin API's changes on ours. Putting both behind one version number means one
of them majors for the other's reason. If a webhook contract ever has to break, it gets its
own route, not a shared bump.

**On the count, this table partitions on a different axis than ADR-0089 rule 6 and does not
contradict it.** That rule states a boundary for **its own** rules and names five families;
this one asks who owns the URL and lists eight. Both were produced by reading the design
layer, and the difference is the criterion rather than an error in either: grouping the
Razor Pages with the protocol endpoints is defensible, since the authorize controller's
`Challenge()` redirects into `/Account/Login` (design 11 at lines 66 to 67). The eight above
came from enumerating every route-shaped string in the tracked design layer on 2026-08-01,
and one candidate was deliberately excluded: `/signin-oidc/{tenant}/{alias}` exists only in
a git-ignored working note that marks it a v2 feature not to be built in v1.

### 3. The tenant segment composes outside the version prefix

**Two tenant-in-URL mechanisms coexist, and this paragraph exists so a later reader does not
tidy one into the other.** `/t/{tenant}` in rule 3 below and `/tenants/{tenantId}` in the
admin routes read like one idea spelled twice. They are different mechanisms on different
hosts, and the difference is why one sits outside the version prefix and the other inside it.

| | `/t/{tenant}`, runtime host | `/tenants/{tenantId}`, admin host |
|---|---|---|
| What the segment names | **who the request is for**: the tenant whose issuer serves it | **what the request is about**: the tenant resource being operated on |
| Why it is in the URL at all | resolution may not require a database query on claims at the token endpoint (ADR-0001 at line 24), so it has to come from the host or the path | the entity's table class decides the path (ADR-0079 rule 2, anchored to design [02](../design/02-data.md) at lines 114 to 122) |
| How many an actor has | exactly one, since "a token must belong to exactly one tenant" (ADR-0001 at line 25) | many, since a delegated-admin grant is scoped to a subtree and applies downward to every descendant (ADR-0010 at line 37) |
| What it sets | `PathBase`, and therefore `iss` | the tenant context for that operation's manager calls (design [15](../design/15-admin-api.md) section 5.1) |
| Its alternative | host-based tenancy, `acme.id.example.com`; the two are "alternatives, never a pair" (design [04](../design/04-core-protocol.md) at line 673) | none, because it is a resource path rather than a resolution strategy |

**So a resolution prefix composes outside the version and a resource segment lives inside
it.** A resolution prefix is not part of the contract, it says which deployment of the
contract you reached, and putting a version above it would version the deployment. A resource
segment **is** the contract. The admin host needs no resolution prefix at all: it is one host
for the whole deployment, and it reaches a tenant by naming it as a resource.

Where path-based tenancy is in use the order is **`/t/{tenant}/api/v1/...`**, tenant first.
This is mechanical rather than aesthetic, and the reverse order does not merely read badly,
it does not work.

The resolve middleware matches `path.StartsWith("/t/", StringComparison.Ordinal)`, then sets
`PathBase` to `/t/{tenant}` and `Path` to the remainder (design
[04](../design/04-core-protocol.md) at lines 716 to 727). So `/t/acme/api/v1/me` yields
`PathBase` of `/t/acme` and `Path` of `/api/v1/me`, and because the engine builds the issuer
as scheme plus host plus `PathBase`, deliberately without `Path` (design
[02](../design/02-data.md) at lines 1188 to 1191), the issuer is `https://host/t/acme` and
**the version segment is correctly absent from `iss`**. Written the other way,
`/api/v1/t/acme/...` never matches the prefix test, so the tenant is never resolved and the
request fails as `tenant_not_resolved` at 400 (design 04 at line 674).

**The Admin API is not affected by this rule, and saying so is the point.** It deploys on
its own host (ADR-0020) and derives the tenant from the `{tenantId}` **route parameter**,
which `TenantScopeHandler` reads (design [15](../design/15-admin-api.md) section 5.1), not
from a path prefix. An implementer who reads rule 3 without this sentence may add `/t/`
handling to a host where nothing needs it, and a second mechanism touching `PathBase` is
exactly what design 02 forbids.

**The `/t/` prefix is a design-layer constant and this ADR does not adopt it.** Before this
change it stood on **nine lines across four tracked files**, counted 2026-08-01: design 04 at
lines 666, 715, 721 and 726, design [02](../design/02-data.md) at lines 1076, 1170 and 1188,
design [20](../design/20-testing.md) at line 214, and one echo in a view,
[09-runtime-flow-views](../architecture/09-runtime-flow-views.md) at line 616. No ADR owns
it: ADR-0001 section C says only that tenant is "resolved from the subdomain/host or path"
and illustrates the host form alone. This rule composes with that constant and leaves its
ownership where it is.

### 4. The base path joins what the ADR-0087 snapshot locks

The committed snapshot's canonical form includes the **base path** of each server entry, so
a change to it fails CI the way a renamed path parameter does. The host itself is deployment
configuration and is normalised out; the path is contract.

Without this, the gate has a hole shaped exactly like the defect it was written after: a
rename that reads as cosmetic and moves every operation. Changing the base path is **MAJOR**
under ADR-0044 section B, being a rename of every operation's URL at once. ADR-0087's own
section E applies unchanged, so this addition counts as present only once a negative
self-test moves the base path and shows CI red.

**One consequence for the generated document, because it is where rule 2 and rule 4 meet.**
The corpus specification carries `paths/health.yaml` inside the same `/api/v1` server entry,
which renders the probes as `/api/v1/health/live`. Rule 2 rejects that. The generated
document must therefore either declare the probes outside the versioned base or omit them,
and the mechanism is a build-time choice deliberately not pinned here, in the same way
ADR-0087 section B does not pin its normaliser.

### 5. One version today, and `Asp.Versioning` is a named upgrade path rather than a dependency

There is one version. It is served as a route prefix declared by the application, and no
library is needed for that.

If Nami ever has to serve two versions at once, **v2 is a parallel base path, never a
mutation of v1**, which is the same shape ADR-0044 section F already chose for the wire
contracts, where "a breaking wire change is a parallel `V2`". The named mechanism for
concurrent versions is `Asp.Versioning`, and it is named here so the upgrade path is not
researched from scratch under pressure. Verified from the cached package metadata on
2026-08-01: `Asp.Versioning.Abstractions` 8.1.0 and `Asp.Versioning.Http`,
`Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` 8.1.1, each declaring
`<license type="expression">MIT</license>`, authored by ".NET Foundation and Contributors",
from `dotnet/aspnet-api-versioning`. MIT is permissive under ADR-0026.

**Three limits on that paragraph, so it is not read as an adoption.** Nami does not depend
on these packages today, so they carry no ADR-0061 row and this ADR carries no
`stack-record` marker; the ADR-0026 licence gate applies if and when one is actually
referenced. The cached versions ship `lib/net8.0` only, and **whether any release targets
net10 is not verified here**. And naming a library is not choosing one: if concurrent
versions are ever needed, that is its own decision with its own ADR.

### Consequences

* Good, because ADR-0020's oldest build-time follow-up closes, and it closes at zero cost to
  the surface: not one route declaration in design 15 moves, because the templates were
  already relative.
* Good, because the probe exclusion is written where an implementer meets it. A versioned
  probe path would have turned an API major into a chart change and a manifest change, and
  nothing in ADR-0080 or ADR-0079 would have caught it.
* Good, because rule 2 is a test rather than a list, so the next route family is decidable
  without reopening this ADR, and rule 3 states the composition order mechanically instead
  of leaving it to be discovered by a doubled `PathBase`.
* Good, because rule 4 closes a hole in the gate that exists to catch exactly this class of
  change.
* Bad, because every admin URL now has a base path that the design and architecture layers
  do not show, since their templates are relative. A reader has to know the base. Rule 1
  says the relativity is deliberate, which is the cheapest available mitigation.
* Bad, because it decides the base for the self-service surface while that surface has one
  declared route, which is the same cost ADR-0089 accepted for the same reason. It also
  meets a gap it does not close: **no document records which host serves the self-service
  routes.** The base path is host-relative so the rule holds either way, and the gap is
  design 08's to close rather than this ADR's.
* Neutral, because two senses of `v1` now coexist, the release and the API version. The
  `api` segment keeps them apart in a URL and nothing keeps them apart in prose.

### Confirmation

* **The corpus wire specification was read at source rather than through its digest**, which
  is what settled the relative-template shape: `api/admin-api.v1.yaml:35` for the server
  entry and every path key under `api/paths/` for the relative form, forty-six of them,
  enumerated 2026-08-01. `DD-25` was not used for either.
* **ADR-0044's silence on HTTP was verified in this repository rather than taken from
  ADR-0087's account of it**, on the rule that no single source is authoritative: zero
  occurrences of route, endpoint, HTTP, verb or status code, and `<migration url>` at line 35
  as the only URL-shaped string. ADR-0087's reading holds exactly, and `0079:256` is
  corrected on the strength of the re-read rather than of the report.
* **The route-family inventory was run, not inherited.** Every route-shaped string in the
  tracked design layer on 2026-08-01, grouped by who owns the URL: eight families, listed in
  rule 2 with a declaration site each. Where it differs from ADR-0089 rule 6's five, the
  criterion differs and both are recorded rather than one being called wrong.
* **One suspected drift was checked and is not one.** `/forgotPassword` and
  `/resendConfirmationEmail` still appear in tracked markdown after ADR-0089 renamed them,
  which looks like an incomplete rename. Every occurrence is narrative, quoting the old
  spelling while explaining the change (design 08 at line 436, and ADR-0038 and ADR-0089
  recording it). No route declaration carries the camelCase form. Recorded because a
  plausible finding that dissolves on reading is worth the same sentence as one that does
  not.
* **The tenancy prefix was verified as `/t/` in both directions.** Nine lines across four
  tracked files carried it before this change, enumerated in rule 3; no `/tenant/{tenant}`
  form exists anywhere, the apparent hits being
  slash-separated word lists in prose such as "filter by type/tenant/actor/from/to" at
  design 15 section 3.8. The most recent change in that area, "make the tenant PathBase a
  single-owner rule, before two mechanisms fight", changed which mechanism may set
  `PathBase` and recorded the `RebaseAspNetCorePathBase` default; it did not change the
  prefix.
* Tests at M1: every admin and self-service route is reachable only under the base path and
  a request to the unprefixed form is a 404; both probe routes are reachable at the host root
  and **not** under the base path, which is the assertion that makes rule 2's probe row
  enforced rather than merely stated; a request to `/t/{tenant}/api/v1/...` produces
  `iss` equal to `https://host/t/{tenant}` with no version segment in it; and the ADR-0087
  negative self-test is extended to move the base path and show CI red.
* **No pre-GA ratification entry.** Nothing here defers a policy, a threshold, or a human
  sign-off; the rules are settled in this document.

## Pros and Cons of the Options

### A. `/api/v1` base path with relative templates (chosen)

* Good, because it matches the artifact the surface was reconciled against, so it costs no
  change to the route declarations and no second description of the surface.
* Good, because the version is written once and cannot drift between route templates.
* Good, because the `api` segment keeps the custom surface legible next to the protocol
  endpoints on a host that serves both.
* Bad, because a reader of a relative template has to know the base path from elsewhere.

### B. A bare `/v1` base path

* Good, because it is shorter, and it is what ADR-0020 proposed, so choosing it would need
  no argument.
* Bad, because it collides in reading with the release sense of `v1` that this repository
  already uses heavily.
* Bad, because it puts no boundary between the custom surface and the protocol endpoints.

### C. Version in a header or a media type

* Good, because URLs stay clean and a version can be negotiated per request.
* Bad, because it cannot be exercised from a browser or a plain link, which is a real cost
  on an admin surface with a reference UI (design 15 section 6.1).
* Bad, because the version becomes invisible to a client generated from the contract, which
  is the consumer ADR-0079 explicitly shapes the surface for, and a count in a header was
  already rejected on that exact ground by ADR-0079 rule 3.

### D. Leave it to build time

* Good, because it defers a decision that could in principle be informed by code.
* Bad, because it has already been deferred once, in the ADR-0020 clause that opens this
  document, and a build-time follow-up with no owner did not close itself.
* Bad, because the string is in every URL, so it is the one path decision whose cost rises
  fastest with delay, and ADR-0079 made that argument for the cheaper decisions.

## More Information

* **The design corpus's admin-API wire specification**, `api/admin-api.v1.yaml` and
  `api/paths/`, read at source 2026-08-01. It is an external artifact and not part of this
  repository; ADR-0087 section F records the one-time reconciliation performed against it and
  is explicit that the reconciliation is not a control.
* `Asp.Versioning` package metadata read from the cached `.nuspec` files 2026-08-01. Project
  home `dotnet/aspnet-api-versioning`. Not a dependency, see rule 5.
* Mechanism: the base path and its wiring belong to design
  [15](../design/15-admin-api.md) for the admin surface and design
  [08](../design/08-user-management.md) for the self-service surface. This ADR fixes the
  scheme, those documents apply it.
* **Three accepted ADRs are amended in the same change**, each in one place and each
  recorded in both documents: ADR-0020's build-time follow-up is closed;
  ADR-0079's parenthetical at line 256 is corrected, because ADR-0044 does not contain the
  discipline it was cited for; and ADR-0087's locked set gains the server base path.
* Related decisions: ADR-0020 (the two deployables, the generated client, and the follow-up
  this closes), ADR-0079 and ADR-0089 (the two surfaces that carry the base, and the
  boundary statement rule 2 partitions differently), ADR-0044 (the SemVer classification
  rule 4 uses, and the section F parallel-`V2` shape rule 5 follows), ADR-0087 (the snapshot
  gate rule 4 extends), ADR-0080 (the probe routes, excluded by rule 2), ADR-0029 (the BFF
  package whose routes are the adopter's), ADR-0015 (break-glass, excluded), ADR-0026 (the
  licence policy rule 5 defers to), ADR-0061 (the stack of record this ADR deliberately does
  not enter), and ADR-0001 (tenant resolution by host or path, which does not fix the prefix
  rule 3 composes with).
