---
status: reviewed
created: 2026-07-26
tags: [architecture, glossary, arc42]
---

# Glossary

> **Part of:** the [Software Architecture Document](README.md), arc42 section 12.

Terms used across the [architecture](README.md), the
[detailed designs](../design/README.md), and the [decision records](../adr/README.md). Where a term
is normative, the specification is named. Where a term is a convention of this project rather
than of the wider ecosystem, that is said explicitly, because those are the ones a newcomer
cannot look up.

Entries are definitions, not decisions: each points at the document of record rather than
restating it.

## Protocol and standards

* **Authorization server, client, resource server, resource owner** are the OAuth 2.0 and
  OpenID Connect roles. Nami is the **authorization server**; a **client** (also relying party)
  requests tokens; a **resource server** validates them; the **resource owner** is the end user.
* **Access token, ID token, refresh token.** Respectively: the credential a client presents to
  a resource server; the authentication assertion about the user; the credential used to obtain
  new access tokens. In Nami the access token is a plain signed JWT typed `at+jwt`, while
  refresh, authorization-code, and device-code artifacts stay encrypted (ADR-0005).
* **Pass-through versus fully-handled endpoint.** A distinction of the protocol engine, and
  **the most common source of error in this codebase**. A *pass-through* endpoint (authorize,
  token, userinfo, end-session, end-user verification) hands off to code you write that
  supplies the user and claims. A *fully-handled* endpoint (discovery, JWKS, introspection,
  revocation, device authorization) is completed by the engine with no controller. Membership
  is not a judgement call: the engine exposes a pass-through option for the first set and for
  nothing else, so the **device authorization** endpoint is fully handled while the **end-user
  verification** endpoint that completes the same flow is pass-through. Writing a controller for a fully-handled endpoint
  reimplements a correct mechanism and usually weakens it (ADR-0048).
* **Claim destination.** Which token a given claim rides in: access token, ID token, both, or
  neither. Nami routes every claim through a single choke-point that is **deny-by-default**, so
  a claim is emitted only where explicitly declared. Distinct from **claim minimisation**, which
  is about which claims exist at all (ADR-0005). The destination rule is elaborated in the
  [core-protocol design](../design/04-core-protocol.md) and decided by ADR-0075, which also
  makes it binding on a replacement adapter rather than only on the shipped one.
* **acr, amr, auth_time.** The assurance claims: the authentication context class, the methods
  used (RFC 8176), and when authentication happened. They are what step-up is evaluated against
  (ADR-0013).
* **Step-up.** Demanding stronger authentication for a sensitive action. The response is
  **`401` with `insufficient_user_authentication`** (RFC 9470), not `403`: the caller may be
  entitled, they simply have not proved enough yet.
* **PAR.** Pushed authorization request (RFC 9126): the client pushes parameters
  server-to-server and receives a `request_uri` to use in the browser redirect.
* **Device flow.** The device authorization grant (RFC 8628) for input-constrained devices.
* **Token exchange.** RFC 8693. The grant is native, but the delegation logic is Nami's own:
  subject and actor resolution, emitting the `act` chain, and rejecting the confused-deputy
  case. `may_act` is deliberately **not** issued, as a security decision rather than a scope
  one (ADR-0014, and the
  [authorization design](../design/07-authorization.md)).
* **Back-channel logout.** A server-to-server logout notification, a signed `logout_token`,
  delivered to each relying party in the session. Built as an interim until the engine ships a
  native equivalent (ADR-0019).
* **Tiered revocation.** The model where a self-contained JWT dies at expiry while a
  **reference token** is revocable immediately, at the cost of forcing its resource server onto
  introspection (ADR-0039).

## Sender-constrained tokens

* **DPoP.** Demonstrating proof-of-possession (RFC 9449): the client signs a per-request proof
  with a key whose thumbprint the token carries. Built on both sides, because the engine has
  neither issuance nor validation (ADR-0014).
* **mTLS-bound token.** A token bound to the client's TLS certificate (RFC 8705). Native.
* **cnf, jkt, x5t#S256.** The confirmation claim and its two binding forms: a JWK thumbprint
  for DPoP and a certificate thumbprint for mTLS.

## Multi-tenancy and isolation

* **Pool and Silo.** The two tenancy modes. **Pool** shares a schema with a tenant
  discriminator and row-level isolation; **Silo** gives a tenant its own database and keyset.
  Pool is the default (ADR-0001).
* **Per-tenant issuer.** Each tenant's tokens carry a tenant-specific `iss`, inferred from host
  or path rather than configured statically, and discovery must advertise the same value.
* **Row-level security, and `FORCE`.** The database-level second layer: a policy confining reads
  and bulk writes to the current tenant, under a **de-privileged** role. The role matters: a
  privileged connection bypasses the policy, which silently removes the layer (ADR-0037).
* **`SET LOCAL`.** How the current tenant is set, inside the request transaction, so it cannot
  leak across a pooled connection. The session-scoped form is forbidden for that reason.
* **Ambient tenant.** The resolved tenant for the current request. **Absence fails closed**, a
  throw or zero rows, never an unfiltered read.
* **Issuer and tenant binding.** The mechanism that actually isolates tenants at a resource
  server. **The signature does not isolate**: Pool tenants share a keyset, so a valid signature
  proves only that Nami issued the token, not for whom. Issuer, audience, and the `tenant`
  claim do (ADR-0033, ADR-0049).

## Keys, audit, and erasure

* **Signing-key state.** The lifecycle `announced`, then `active`, then `retired`, then
  `deleted` (ADR-0011). **Compromise is not one of these states**: it is a transition,
  represented by a revocation timestamp orthogonal to the state, so a key can be revoked
  from any state. That orthogonality is the key-management design's
  ([12](../design/12-key-management.md), with the column in
  [02](../design/02-data.md)), and the break-glass action that sets it is ADR-0007; ADR-0011
  fixes the four states and defers revocation rather than defining it.
* **Publish-before-sign.** A key appears in the JWKS for validation before it ever signs, so
  clients that cache the JWKS have already seen it. One deliberate exception: the very first
  key at genesis activates immediately, because there is nothing yet to protect (ADR-0012).
* **No-restart rotation.** Rotating the signing key with no process restart and no in-flight
  validation failure, through the framework options monitor driven by a custom configure-options
  (ADR-0011) and a non-static configuration manager
  reading the live key store (ADR-0011).
* **KEK and DEK.** Key-encryption key and data-encryption key. A **per-subject DEK** is what
  makes crypto-shred possible.
* **Crypto-shred.** Erasing a subject by destroying their data-encryption key, so ciphertext
  becomes unrecoverable **without deleting any row**. This is what lets erasure coexist with an
  append-only audit chain and with immutable backups (ADR-0016).
* **Hash chain.** The tamper-evidence mechanism: each audit record hashes the previous hash
  together with its canonical payload, **previous first**. It is **keyed** (an HMAC), and the
  key is the difference between proving ordering and proving authorship (ADR-0008).
* **Chain-over-commitments.** Keeping that chain verifiable after a subject is shredded,
  because the hash covers the ciphertext rather than the plaintext (ADR-0016).
* **Audit lane and diagnostics lane.** Two pipelines with opposite failure requirements, joined
  only by a correlation identifier. The audit lane is guaranteed and never dropped; the
  diagnostics lane is **lossy and must never block** a request (ADR-0008, ADR-0022).
* **Restore-both.** The disaster-recovery requirement that signing keys, the data-protection
  keyring, and the root certificate are restored **together** under the same application name.
  Restoring keys without the keyring leaves them present and undecryptable, which looks like a
  successful restore until the first token request (ADR-0012).

## Authorization and administration

* **Capability.** A named permission in the catalog, lowercase snake_case because it is a
  stored value rather than prose. Checked live at the Admin API, never baked into a token,
  because a grant is revocable and a claim is not (ADR-0010).
* **Delegated admin.** A tenant-scoped, time-bound, revocable administrative grant. **There is
  no super-admin.**
* **Forbidden cascade.** The control that makes "no super-admin" real: dangerous capabilities
  do not inherit down the tenant tree even from an ancestor grant, so they need a direct grant
  on the exact tenant. In v1 the grant model is **purely additive**, with no deny row and no
  parent ceiling (ADR-0010).
* **Dual control.** A destructive action needs a second, different principal: a proposal bound
  to a request hash, then approval by a distinct person, then execution with a re-check.
  Never autonomous (ADR-0020).
* **`RequireActor`.** The rule that the Admin API accepts only a user-delegated token, rejecting
  an app-only or client-credentials token, paired with an issuance-time invariant that no such
  client is ever granted the admin scope (ADR-0020).
* **Break-glass.** Two unrelated emergency paths that are easy to conflate. **Key-compromise
  break-glass** ejects a compromised key (ADR-0007). **Admin break-glass** is an
  OIDC-independent path for when Nami cannot issue tokens at all (ADR-0015).
* **BFF.** Backend-for-frontend: a server-side component that keeps tokens out of the browser
  for single-page applications, which is the real mitigation against cross-site scripting
  rather than a convenience (ADR-0029).

## Operations and quality

* **SLI, SLO, error budget.** A measured quantity, a target on it, and the remainder `1 - SLO`.
  The budget is deliberately expressed as a **formula rather than a figure**, because it drives
  the release freeze and the availability target is not yet ratified (ADR-0041).
* **Burn rate.** How fast the error budget is being consumed. Alerting is on burn rate rather
  than on instantaneous latency, and the burn tier drives an **automatic** freeze.
* **Rate limiting versus load shedding.** Different controls: rate limiting answers "this caller
  is asking too often" with `429`; load shedding answers "the service is past capacity" with
  `503` (ADR-0040).
* **Fail open, fail closed, and the carve-out.** Ordinary performance caches **fail open**;
  security checks **fail closed**. Both are the rule. There is exactly **one carve-out**, the
  per-recipient email throttle, which is an abuse control that would otherwise fail open and
  instead degrades to an in-process bucket (ADR-0040).
* **Expand and contract.** The reversibility model for schema change at fleet scale: ship
  migrations so old code and new schema coexist, and never ship a destructive change in the
  same release as the code needing it. Roll-forward is the default recovery (ADR-0017).
* **Traffic gate.** The per-tenant `503` returned while a tenant's schema version does not match
  the running code, isolating that tenant rather than the fleet. `503` rather than `404`
  deliberately, so relying parties do not purge cached metadata (ADR-0017).
* **Seam, and the seam catalogue.** A seam is a dependency on engine behaviour: a port, a named
  handler position, or an undocumented-but-endorsed extension point. The catalogue enumerates
  them (S1 to S34) with a risk tier, a contract test, and a decommission marker (ADR-0021).
* **Contract-regression suite.** The tests that pin resolved handler order and public contract
  and run on every engine or runtime bump, failing the build on drift. It exists because a bump
  can silently reorder handlers or flip a native-versus-build verdict (ADR-0021).
* **Build-interim.** A capability Nami implements because the engine does not, carrying a
  decommission marker so it retires when a native equivalent ships. Distinct from a
  deliberately chosen alternative, which retires nothing (ADR-0021).
* **Kill switch.** A future feature is absent unless its registration extension is called. The
  switch is composition, not configuration, so there is no flag to get wrong (ADR-0071).

## Conventions of this project

* **Native versus build.** Whether a capability comes from the protocol engine or must be
  written. Each verdict traces to a verification record, and each is re-checked on every bump.
* **Spike, and gate spike.** A runnable experiment that proves a risky mechanism before it is
  committed to. A **gate** spike blocks the decision until it passes. Several changed their
  design rather than confirming it, which is the point.
* **Verification record.** A dated evidence file behind a factual claim, cited as `V` plus a
  number. Finding identifiers such as `F-A9-1` name a specific discovery from a spike.
* **Stack of record.** The single table of committed technologies, one row per concern with its
  owning decisions. An ADR that fixes a technology carries a marker, and the guardrail enforces
  that the markers and the table agree (ADR-0061).
* **Pre-GA ratification.** A decision whose mechanism is built while a number, policy, or
  sign-off is a named owner's call. Consolidated as one release gate rather than scattered
  (see the [checklist](../PRE-GA-RATIFICATION-CHECKLIST.md)).
* **Naming.** Assemblies sit under `Nami.Identity.*`; configuration keys are `Nami:Section:Key`
  with `Nami__Section__Key` as the environment form and a short `NAMI_X` alias (ADR-0065).
* **The authority order.** Decision records bind, detailed designs are the authority for
  implementation detail, and the architecture layer synthesises both and **introduces
  nothing**. When it disagrees with either, the architecture is the bug.

## Sources

* Terms trace to the ADR or design named inline; where none is named the term is general
  ecosystem vocabulary defined here for consistency.
* Reconciled against the design corpus's glossary on 2026-07-26. Taken from it: the section
  structure, the protocol and sender-constrained vocabulary, the isolation and key terms, and
  the practice of defining project conventions rather than assuming them. Three groups were
  **not** carried. Its open-item bucket classes are that project's register taxonomy and have
  no counterpart here, where the equivalent is the pre-GA checklist. Its mandatory compliance
  label is an organisational artifact of that project, not a term of this one. And its
  hostname and contact conventions are not facts about this repository. Two entries were made
  more precise: the corpus defines a claim destination without separating it from claim
  minimisation, which are different controls with different failure modes; and it defines
  break-glass as one term, where the two paths are unrelated and conflating them is an
  operational risk.

---

[Prev: Risks and technical debt](23-risks-and-technical-debt.md) · [Index](README.md)
