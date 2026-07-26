---
status: draft
created: 2026-07-26
tags: [design, dpop, mtls, sender-constrained, tokens]
---

# Sender-constrained tokens: DPoP and mTLS (detailed design)

> **Sits under:** [architecture: security architecture](../architecture/13-security-architecture.md)
> (proof of possession) and [runtime flow views](../architecture/09-runtime-flow-views.md).
> **Implementer source of record:** this document, for both sender-constraint mechanisms.
> The issuance pipeline they hook into is [04](04-core-protocol.md); the per-tenant
> validation the `cnf` check composes on top of is [05](05-resource-server-validation.md);
> the cross-node cache the replay store uses is [13](13-revocation-caching.md).

A sender-constrained token is useless to a thief who does not also hold the
proof-of-possession key. Two mechanisms cover two client shapes, and the split is not a
preference: a browser cannot present a TLS client certificate, and a machine-to-machine
client has no reason to sign a proof per request when it already has a certificate.

## 1. Decisions realized

| Decision | What this design applies |
|---|---|
| ADR-0014 | mTLS as the native baseline for confidential and machine-to-machine clients, DPoP built for public clients; both in scope, neither optional |
| ADR-0005 | The access token is a plain signed JWT, which is why a resource server can read `cnf` from it without introspecting |
| ADR-0021 | The DPoP handlers are a version-pinned build-interim seam with a decommission marker: they retire if the engine ships native DPoP |
| ADR-0024 | The replay cache is a port at the application edge, not a direct Redis dependency |
| ADR-0049 | Sender-constraint is checked **after** per-tenant validation, never instead of it |
| ADR-0048 | The introspection response must carry `cnf.jkt` for a bound token, or be inactive |
| ADR-0029 | The backend-for-frontend, which is the actual mitigation for the browser threat DPoP does not solve |
| ADR-0073 | The proxy posture that forwards the client certificate, and the header-spoofing guard on it |

## 2. Purpose and scope

In scope: mTLS issuance and enforcement, DPoP issuance and validation, the proof contents
and their checks, replay protection, the validation modes and the nonce, the client-side
key contract, and the security boundary of each mechanism.

Out of scope: the token-issuance pipeline this hooks into and the introspection **server**
configuration (04); the per-tenant validation the `cnf` check runs after (05); the
cross-node cache infrastructure the replay store sits on (13); device, PAR, and
token-exchange wiring (14); and the backend-for-frontend host itself (16, ADR-0029).

## 3. Interfaces and contract

Four seams, three of them handlers Nami writes because the engine has nothing to configure.

```mermaid
classDiagram
  class StampConfirmationJkt {
    HandleAsync(ProcessSignInContext) ValueTask
  }
  class ExtractDPoPAccessToken {
    HandleAsync(ProcessAuthenticationContext) ValueTask
  }
  class ValidateDPoPProofOfPossession {
    HandleAsync(ValidateTokenContext) ValueTask
  }
  class IDPoPReplayCache {
    AddAsync(purpose, jti, expiry) ValueTask
    ExistsAsync(purpose, jti) ValueTask~bool~
  }
  ValidateDPoPProofOfPossession ..> IDPoPReplayCache : check then add
  StampConfirmationJkt ..> AccessTokenPrincipal : stamp after the principal is built
  note for ValidateDPoPProofOfPossession "runs before the built-in handler and consumes the jkt branch"
  note for IDPoPReplayCache "the one Redis path that fails closed"
```

| Seam | Pipeline and event | Why it exists |
|---|---|---|
| `StampConfirmationJkt` | Server, `ProcessSignInContext` | The engine stamps `cnf` only for a client certificate, so the `jkt` form has no code path |
| `ExtractDPoPAccessToken` | Validation, `ProcessAuthenticationContext` | The built-in extractor matches only the literal `Bearer` prefix, space included |
| `ValidateDPoPProofOfPossession` | Validation, `ValidateTokenContext` | The built-in proof handler understands only `x5t#S256` and **throws** on a `jkt` |
| `IDPoPReplayCache` | Port (ADR-0024) | Replay detection needs a cross-node store, and the store is an adapter concern |

The DPoP constants (`typ` value, claim names, error codes, header name) are also Nami's,
because the engine defines none of them. Section 11 records that as a source-verified fact
rather than an impression.

## 4. Data and structure

No relational table. The shape that matters is the `cnf` claim on the access token:

| Form | Value | Mechanism |
|---|---|---|
| `cnf.x5t#S256` | base64url SHA-256 of the client certificate's DER bytes | mTLS (RFC 8705) |
| `cnf.jkt` | base64url SHA-256 JWK thumbprint (RFC 7638) of the proof key | DPoP (RFC 9449) |

**`cnf` is a nested JSON object, and the claim must be set as one.** The engine's own
mTLS path builds a `JsonObject` and passes it to `SetClaim`, so the DPoP path does the
same and emits `"cnf":{"jkt":"..."}`. Setting a `string` instead produces a
double-serialized value that a resource server cannot read as an object, which is a real
reported failure mode and not a theoretical one.

Replay state is a key per proof identifier in the distributed cache, `DPoPReplay-jti-<jti>`,
with a lifetime of the proof validity window plus twice the applicable skew. The nonce,
when enabled, is a data-protected encrypted timestamp and is never persisted.

## 5. Behaviour

### mTLS: native, and the work is at the edge

For a confidential or machine-to-machine client with a certificate,
`UseClientCertificateBoundAccessTokens()` makes the token endpoint stamp `cnf.x5t#S256`,
and discovery advertises `tls_client_certificate_bound_access_tokens`. No handler is
written. The engine both stamps and validates the binding.

The work is therefore not in the application but at the edge and on the resource server.
The resource server must compare `cnf.x5t#S256` against the thumbprint of the certificate
on the actual connection; a mismatch is a rejection, and that comparison is the entire
value of the mechanism. Behind a TLS-terminating proxy, which is the default posture, the
certificate arrives as a forwarded header, so the trusted-proxy allow-list is
load-bearing: a client-certificate header accepted from an untrusted source **is**
client-certificate impersonation (04, ADR-0073). This path also requires internal
certificate authority infrastructure, which is an operational prerequisite rather than a
code one.

### DPoP: the engine has nothing, so both sides are built

The engine at the pinned version has no DPoP on either side. Not a partial
implementation, not a flag left off: no `jkt`, `ath`, `htm`, or `htu` constant, no
`dpop+jwt` type value, no nonce error code, and not even the string `DPoP` anywhere in
its constants, server options, or server handlers. The confirmation claim it builds
carries exactly one key, the certificate thumbprint. Section 11 gives the files and lines.

**What the spikes proved, stated narrowly.** A-1 and A-3 (record V18) established the
**shape**: which event and order the issuance handler must use, that the nested `cnf`
emits correctly, and that the built-in proof handler's throw can be neutralized by
inserting before it. They did **not** exercise the proof cryptography. A-1 stamped a
simulated thumbprint taken from a test header, and A-3 never ran signature, `htm`, `htu`,
`ath`, thumbprint, or replay checking. So the wiring is de-risked and the proof validator
is ordinary unproven code, which is exactly the distinction that matters when planning:
the risky part was the framework seam, and the remaining part is a test-covered
implementation.

#### Issuance

```mermaid
sequenceDiagram
  autonumber
  participant C as Public client
  participant T as Token endpoint
  participant P as PrepareAccessTokenPrincipal, built-in
  participant H as StampConfirmationJkt, ours
  C->>T: token request plus a DPoP proof header
  T->>T: validate the proof: typ, alg, public jwk, htm, htu, iat, jti
  Note over T: no ath at issuance, because no access token exists yet
  T->>P: build the access-token principal
  P->>H: ordered after the built-in handler
  H->>H: jkt equals base64url SHA-256 thumbprint of the jwk
  H->>H: stamp cnf as a nested object on the access-token principal
  T-->>C: bound access token, and a bound refresh token for a public client
```

Two orderings are load-bearing and both were the point of spike A-1. The handler runs
**after** the built-in principal-preparation handler and stamps on the **access-token
principal**, not on the incoming principal. Stamping earlier is silently lost, because the
access-token principal is constructed by that handler and the earlier object is not what
gets serialized. The engine's own mTLS stamp happens in the same place, which is the
strongest available evidence that the place is right.

Two issuance invariants follow, and both exist to prevent a **half-bound token**, meaning
a token that carries a binding the system will not enforce:

* **Introspection is enrich-or-inactive.** The engine's `cnf`-in-introspection covers only
  the certificate form, so the `jkt` form must be added. A bound token either carries
  `cnf.jkt` in the introspection response or the response is `active:false`. Never active
  and missing its binding, because a resource server would then honour it as a plain
  bearer token (05, ADR-0048).
* **Refresh requires thumbprint continuity.** A refresh grant for a bound token needs a
  **new** proof whose thumbprint equals the stored `cnf.jkt`; a mismatch or a missing
  proof is rejected, and the new access token is re-stamped with the same value
  (RFC 9449 section 5).

#### Validation

```mermaid
sequenceDiagram
  autonumber
  participant C as Public client
  participant X as ExtractDPoPAccessToken, ours
  participant V as ValidateDPoPProofOfPossession, ours
  participant RC as IDPoPReplayCache
  participant B as Built-in proof handler
  C->>X: Authorization DPoP token, plus a DPoP proof header
  X->>X: read the token from the DPoP scheme, before the built-in Bearer extractor
  X->>V: proceed
  V->>V: typ, alg, public jwk, signature
  V->>V: htm exact, htu normalized, iat in window, ath equals hash of the token
  V->>V: thumbprint of the jwk equals cnf.jkt
  V->>RC: does this jti exist
  alt replayed
    RC-->>V: found
    V-->>C: reject, invalid_dpop_proof
  else fresh
    V->>RC: add the jti, lifetime is validity plus twice the skew
    V->>V: consume the jkt branch so the built-in handler does not throw
    V->>B: built-in handler runs, sees no jkt
    B-->>C: authenticated
  end
```

Three mechanics are worth naming because each is a place the design could look right and
fail:

* **The extractor must run one order earlier than the built-in one**, which matches only the
  literal `Bearer` prefix, space included, and would otherwise leave the token unread.
* **The proof handler must consume the `jkt` branch**, that is strip the `cnf` claim,
  before the built-in proof handler runs. The built-in handler does not reject an
  unrecognised `cnf`; it **throws**, which surfaces as a 500 rather than a protocol error.
  That is the failure this ordering exists to prevent.
* **Anchor to the pipeline that actually runs.** The built-in proof handler is a server
  handler, so a co-hosted resource server reaches it, while a standalone validation-only
  resource server does not run the server pipeline at all and must anchor to the
  validation pipeline's own proof handler. The spike ran the co-hosted path; the
  standalone anchor is an integration item in section 10. Anchoring a handler on one
  pipeline to a descriptor from the other is the specific mistake this note exists to
  prevent.

A bound token presented over `Bearer` is rejected (RFC 9449 section 7.2). And because the
engine's `WWW-Authenticate` writer hardcodes the Bearer scheme, the DPoP challenge headers
are written directly and the built-in Bearer header is suppressed.

### Replay protection, and the one place Redis fails closed

The proof identifier check is a check-then-add against the distributed cache. The lifetime
is the proof validity window plus **twice the applicable skew**, where "applicable" means
one skew and not a sum: the `iat` mode uses the client clock skew, the nonce mode uses the
server clock skew.

**The add is fail-closed, and it is the only Redis path in the product that is.** If the
write is not confirmed, the proof is rejected. Everywhere else a cache outage degrades
performance (ADR-0040); here a fail-open add would open a replay window for exactly as
long as the cache is down, which converts a cache outage into a security hole. The residual
is stated rather than hidden: in the default mode, replay defence is the `iat` window plus
this cache, so a cache outage reduces the availability of DPoP-protected APIs during it.
That is the trade being made, and the backend-for-frontend is what makes it acceptable.

A local in-process cache tier must not be authoritative for this check. The distributed
store is the source of truth, because the whole point is catching a proof replayed against
a different node.

### Validation modes and the nonce

The proof-freshness mode is one of `Iat`, `Nonce`, `IatAndNonce`, or `IatOrNonce`. v1 ships
`Iat`, which is RFC-compliant and the simplest thing that works. There are **two distinct
skews** and conflating them is a bug: a client clock skew of a few minutes for the `iat`
window, and a server clock skew of zero for the nonce window.

A server-issued nonce is a later addition that defends against proofs generated in advance.
The resource server answers `401` with a `use_dpop_nonce` error and a nonce header; the
token endpoint answers `400` with the same error in the body plus the header; the client
retries with the nonce, and the server rotates it on success.

The validator is staged so each stage can be overridden and tested alone: header, then
signature, then payload, then freshness, then replay.

### The client contract, and what DPoP does not do

A browser client generates a **non-extractable** key pair through the platform crypto API
and keeps the handle in local storage; the public JWK travels in the proof header. A mobile
client uses a hardware-backed key. Every request carries the token under the DPoP scheme
plus a fresh proof with a fresh identifier of at least 96 bits of entropy, the access-token
hash, and the method and URL.

**The caveat is load-bearing and is stated plainly rather than buried.** A non-extractable
key prevents the key from being *exfiltrated*. It does not prevent cross-site scripting
from *using* it: script running in the page can call the signing API and mint valid proofs
on demand, which makes the key a signing oracle. Browser DPoP stops a stolen token from
being replayed elsewhere; it does not stop an attacker who is already executing in the
page. **The backend-for-frontend is the real mitigation** for a browser client, keeping the
token server-side behind an HTTP-only cookie (ADR-0029, 16).

Recording this is not pessimism. A design that presents DPoP as an answer to cross-site
scripting would lead an adopter to skip the control that actually works.

## 6. Dependencies and wiring

```csharp
// mTLS: native, nothing to write.
services.AddOpenIddict().AddServer(o => o.UseClientCertificateBoundAccessTokens());

// DPoP issuance: one server handler, ordered after the built-in principal preparation.
services.AddOpenIddict().AddServer(o => o.AddEventHandler(StampConfirmationJkt.Descriptor));

// DPoP validation: the extractor before the built-in Bearer one, the proof handler
// before the built-in proof handler.
services.AddOpenIddict().AddValidation(o =>
{
    o.AddEventHandler(ExtractDPoPAccessToken.Descriptor);
    o.AddEventHandler(ValidateDPoPProofOfPossession.Descriptor);
});

// The replay cache is a port; the Redis adapter is one implementation of it.
services.AddSingleton<IDPoPReplayCache, DistributedCacheDPoPReplayCache>();
```

Both handler orders are expressed relative to named built-in descriptors rather than as
numbers, and the resolved order is pinned by the pipeline-snapshot test (04, ADR-0021), so
a version bump that moves the built-in handlers fails the build rather than production.

### Configuration keys

**Set by this design**, following `Nami:Section:Key` with the `Nami__Section__Key`
environment form (ADR-0065):

| Key | Purpose |
|---|---|
| `Nami:DPoP:Enabled` | Whether the DPoP handlers are registered at all |
| `Nami:DPoP:ValidationMode` | `Iat`, `Nonce`, `IatAndNonce`, or `IatOrNonce`; v1 default `Iat` |
| `Nami:DPoP:ClientClockSkewSeconds` | The `iat` acceptance window |
| `Nami:DPoP:ProofValiditySeconds` | Shorter at a resource API than at the token endpoint |
| `Nami:DPoP:RequireNonce` | The later hardening step |
| `Nami:Mtls:Enabled` | Whether certificate-bound access tokens are issued |

### Key libraries and licenses

| Library | Purpose | License |
|---|---|---|
| `OpenIddict.Server` / `OpenIddict.Validation` | The pipelines both mechanisms hook into; mTLS binding is native here | Apache-2.0 |
| `Microsoft.AspNetCore.Authentication.Certificate` | The mTLS client-certificate scheme | MIT |
| `Microsoft.IdentityModel.Tokens` | Proof-JWT parsing, thumbprint computation, signature validation | MIT |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | The distributed-cache adapter behind the replay port | MIT |

No DPoP library is taken as a dependency: the engine has none to extend, and the proof
format is small enough that a dependency would add a supply-chain surface for less code
than it removes.

### Packaging

The server-side handlers ship in `Nami.Identity.DPoP` (`.AddDPoP(...)`, depending on the
core package); the resource-side validation ships in `Nami.Identity.Validation.DPoP`
(`.AddDPoPValidation(...)`, depending on `OpenIddict.Validation` and **not** on the core
package, so a resource API need not pull the server package to validate a bound token).
`IDPoPReplayCache` sits in `Nami.Identity.Abstractions` so both sides share one port. The
names are reserved in the foundations package graph (01); the exact boundaries land with the
first code (ADR-0027).

### Patterns applied

Named per ADR-0066:

* **Chain of Responsibility**, inherited: every seam here is an ordered handler.
* **Ports and adapters** for the replay cache, so the cross-node store is swappable.
* **Strategy** for the freshness mode, which is why the four modes are one option rather
  than four code paths.

## 7. Error handling, edge cases, invariants

* **No half-bound token.** A public client gets a fully bound token or no binding at all.
  If binding could not be emitted, the fallback is mTLS-only for confidential and
  machine-to-machine clients with DPoP deferred, contained to public clients. A partially
  bound token is never a state the system can be in.
* **Consume the `jkt` branch before the built-in proof handler**, or the request becomes a
  500. The built-in handler throws rather than rejecting.
* **Anchor to the pipeline that runs**, co-hosted server or standalone validation. The
  cross-pipeline anchor is a silent mis-order, not a compile error.
* **A bound token over `Bearer` is rejected** (RFC 9449 section 7.2).
* **The replay add is fail-closed**, uniquely among the product's cache paths.
* **Introspection is enrich-or-inactive** for a bound token.
* **Refresh requires thumbprint continuity**, and re-stamps the same value.
* **`cnf` is a nested object**, never a string; a string double-serializes.
* **The two skews are distinct.** Using the client skew for the nonce window widens the
  nonce's acceptance window to no purpose.
* **These handlers are a version-pinned seam** with a decommission marker: re-verify the
  orders and the built-in behaviour on every bump, and retire them if the engine ships
  native DPoP (ADR-0021).

## 8. Security and multi-tenancy notes

* **Sender-constraint composes after per-tenant validation, never instead of it.**
  Authenticate, bind the tenant (05), then check the proof. A proof match says the
  presenter holds the key; it says nothing about which tenant the token belongs to, so
  reversing the order would let a valid proof carry a cross-tenant token (ADR-0049, and
  the fourth test of spike A-7).
* **Cross-node replay is caught** because the identifier store is shared, which is the
  reason a local cache tier cannot be authoritative for it.
* **The browser caveat is the security honesty of this design**: DPoP is not a cross-site
  scripting defence, and the backend-for-frontend is.
* **The mTLS header path is a spoofing surface**, not a detail: accepting a
  client-certificate header from anything but the trusted proxy is impersonation
  (ADR-0073).
* **The fallback is contained.** A DPoP problem never reaches confidential or
  machine-to-machine clients, whose binding is the engine's own and untouched.

## 9. Testing

Established by spikes A-1 and A-3 (record V18), and kept as regression:

| Test | What it establishes |
|---|---|
| A-1 T1 | The token emits a nested `cnf` object, not a double-serialized string and not a flattened dotted key |
| A-1 T3 | No proof header yields a plain token with no `cnf`, which is the no-half-bound-token property |
| A-3 T9 | A `jkt`-carrying token fed to the unmodified pipeline does **not** authenticate, which is what proves the gap is real rather than assumed |
| A-3 H2 | With the handler inserted before the built-in one, the resource server authenticates and does not throw |

To build, and the list is longer than the spike set because the spikes covered the wiring
rather than the cryptography: a bound token over the DPoP scheme passes; the same token
over `Bearer` is rejected; a tampered method, URL, or token hash yields
`invalid_dpop_proof`; a replayed identifier is rejected **across two nodes sharing the
store**, which is the only way to test the property that matters; an `iat` outside the skew
is rejected; a thumbprint mismatch against `cnf.jkt` is rejected; the nonce round trip goes
`401` then retry then success; mTLS still works, proving no regression of the built-in
handler; introspection is enrich-or-inactive; and refresh continuity rejects a mismatched
thumbprint while a matching one rotates and re-stamps.

## 10. Open and build-time items

* **The standalone validation-pipeline anchor** is an integration verification. The spike
  ran the co-hosted path, and the order anchor differs for a validation-only resource
  server (section 5).
* **The nonce is phase two.** v1 is `iat`-only, which is RFC-compliant; the nonce is added
  if defence against pre-generated proofs is needed.
* **The certificate-authority infrastructure and the forwarding proxy** for mTLS are
  operational prerequisites, and the trusted-proxy address list is an Ops and Security
  ratification item (ADR-0073).
* **The fallback trigger** is recorded rather than left implicit: if a future version breaks
  the binding emission, the response is mTLS-only with DPoP deferred, not a partially bound
  token (ADR-0021).
* **The proof validator has no spike behind it.** Section 5 says so, and the integration
  test list in section 9 is what covers it. Anyone planning this work should read the
  spike boundary before assuming DPoP is de-risked end to end.

## 11. Sources

* Architecture: [security architecture](../architecture/13-security-architecture.md),
  [runtime flow views](../architecture/09-runtime-flow-views.md).
* Design: [04](04-core-protocol.md) for the issuance pipeline, the mTLS wiring, and the
  snapshot test; [05](05-resource-server-validation.md) for the per-tenant validation this
  composes after; [13](13-revocation-caching.md) for the cross-node cache;
  [14](14-advanced-flows.md) for device, PAR, and token exchange; [16](16-admin-app.md) and
  ADR-0029 for the backend-for-frontend.
* ADRs: 0014 (both mechanisms, and the build-versus-native split), 0005 (the readable
  token), 0021 (the pinned seam and its decommission marker), 0024 (the replay port),
  0049 (composition order), 0048 (introspection), 0029 (the backend-for-frontend), 0040
  (the fail-open cache policy this design carves out of), 0073 (the proxy posture), 0065,
  0066.
* Records: V18 for spikes A-1 and A-3, V27 for the fourth test of A-7 that showed
  sender-constraint composing after per-tenant validation, V14 for the finding that the
  engine's `cnf`-in-introspection covers only the certificate form.
* **External verification, 2026-07-26, OpenIddict at release tag 7.5.0**, the version
  ADR-0061 pins. The claim that the engine has no DPoP was checked rather than assumed:
  `OpenIddictConstants.cs`, `OpenIddictServerOptions.cs`, and `OpenIddictServerHandlers.cs`
  contain **no occurrence** of `jkt`, `ath`, `htm`, `htu`, `dpop+jwt`, or `use_dpop_nonce`,
  and none of the string `DPoP` at all. In `OpenIddictServerHandlers.cs` the private
  `CreateConfirmationClaim` builds a `JsonObject` whose **only** key is the certificate
  thumbprint parameter name, and it is passed to `SetClaim(Claims.Confirmation, ...)` in the
  handler that also assigns `context.AccessTokenPrincipal`, which is the source-level basis
  for both the "mTLS only" claim and the ordering rule in section 5. In
  `OpenIddictServerHandlers.Protection.cs` the built-in `ValidateProofOfPossession` is a
  handler on `ValidateTokenContext` and **throws** `InvalidOperationException` with resource
  string `ID2196`, which is why the custom handler must consume the branch rather than
  merely precede it. In `OpenIddictValidationAspNetCoreHandlers.cs` the built-in
  `ExtractAccessTokenFromAuthorizationHeader` is a handler on `ProcessAuthenticationContext`
  that tests the header with `StartsWith("Bearer ")` and slices by that literal's length, and
  `AttachWwwAuthenticateHeader` builds its value starting from the Bearer scheme constant.
  **One corpus statement was corrected against this:** the corpus says the confirmation claim
  is set as a `JsonElement`, and the engine's own code uses `JsonNode` with a `JsonObject`
  instance, so this design says nested JSON object and names the type the engine uses.
* Reconciled against the design corpus's sender-constrained-tokens design on 2026-07-26.
  Taken from it: the two-mechanism split by client shape, the issuance event and order with
  the reason stamping earlier is lost, the extractor and proof-handler insertion points, the
  enrich-or-inactive and refresh-continuity invariants, the replay key shape and the
  fail-closed carve-out with its two-skew rule, the four freshness modes and the nonce round
  trip, the client key contract, and the cross-site-scripting caveat. **Carried forward from
  this repository rather than from the corpus, and deliberately:** the corpus describes DPoP
  as spike-proven, while this repository had already recorded the sharper boundary that the
  spikes proved the framework **shape** and never ran the proof cryptography. That
  distinction is preserved here in sections 5, 9, and 10, because "spike-proven" would lead
  a planner to treat the validator as de-risked when only the seam is.

---

[Prev: Resource-server token validation](05-resource-server-validation.md) · [Index](README.md) · Next: [Authorization and delegated admin](07-authorization.md)
