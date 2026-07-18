---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: the key rotation and retention design, and the tiered key-scope isolation model (ADR-0033)
informed: all contributors, via this repository
---

# Track the encryption credential's lifecycle separately from the signing credential

## Context and Problem Statement

OpenIddict enforces encryption by default: the access token is a JWE, and the authorization code, refresh token, and device code are JWEs that **cannot be turned off**. Nami therefore needs **both** a signing key and an encryption key. An early rotation plan applied one shared retention rule to "the cert": remove it once the tokens it signed have expired. That rule is correct for a *signing* key but wrong and dangerous for an *encryption* key: removing an encryption key while live refresh-token, authorization-code, or device-code JWEs still reference it destroys the ability to decrypt them, which kills those artifacts — users are logged out and redeems fail. Should the two credentials share one lifecycle, or be tracked separately?

## Decision Drivers

* The system must never lose the ability to decrypt an artifact that is still live.
* Rotation must be safe for both key roles, which have opposite retention semantics (signing retires by signed-token expiry; encryption must outlive the longest live JWE).
* Key-set scope follows the tier from ADR-0001 and ADR-0033: Pool tenants share the pool-group key-set; Silo tenants have their own signing and encryption key-sets, and retention applies per key-set.

## Considered Options

* Shared lifecycle for signing and encryption credentials
* Separate lifecycles, tracking the encryption credential with a retention floor based on the longest-lived live JWE artifact

## Decision Outcome

Chosen option: "Separate lifecycles", because a shared retention rule retires the encryption key on the signing key's schedule and would destroy live JWE artifacts. The encryption credential is treated as first-class with its own lifecycle.

Fixed parameters of the decision:

* **Encryption retention floor** = `max(refresh-token lifetime, device-code lifetime, other JWE token lifetimes)` plus a margin, enforced before un-registering an encryption key. With the 8-hour refresh-token ceiling from ADR-0004 as the upper bound and a short device-code TTL (roughly 10-15 minutes), the floor is approximately 8 hours plus margin.
* **Hard guard before removing an encryption `kid`**: verify no live JWE artifact still references it (or that it is safely past the retention floor).
* The **signing key keeps** the furthest-`NotAfter` selection rule with retention driven by the tokens it signed.
* **`DisableAccessTokenEncryption()` is enabled**: the access token is a plain signed JWT. Resource APIs validate it with `JwtBearer` + JWKS and `ValidTypes = ["at+jwt"]`. This mandates a **minimal access token** (only `sub`, `scopes`, and `tenant`) for GDPR claim minimization, because a plain JWT is readable by anyone holding it.
* **Refresh tokens, authorization codes, and device codes remain JWE** (this cannot be disabled), so the encryption credential is still required and is **never** retired on the access-token TTL.
* **Key-set scope by tier**: Pool tenants share the pool-group key-set; Silo tenants have their own signing and encryption key-sets; lifecycle and retention apply per key-set (ADR-0001, ADR-0033). The plain access-token JWT carries the `tenant` claim and a per-tenant `iss`.
* **Signing-algorithm baseline = RS256**, with ES256 selectable by configuration through the signing credential source; this choice is orthogonal to the signing/encryption lifecycle separation above.
* **Claim minimization extends to the id_token**: the `memberships` claim is size-capped (roughly 10 entries) with a `memberships_truncated` flag and a self-service endpoint for the full list, so a user in many tenants does not bloat the token; the assurance claims `acr`/`amr`/`auth_time` are produced by ADR-0013.

### Consequences

* Good, because Nami never loses the ability to decrypt a live artifact, and rotation is safe for both key roles.
* Bad, because the rotation service is more complex: two timelines and two sets of guards, and it must know the longest lifetime of every JWE artifact.
* This decision is coupled to ADR-0006 (disaster recovery: losing the encryption key means losing the artifacts it protects) and ADR-0007 (a compromised encryption key means every outstanding refresh token, authorization code, and device code is treated as burned).

### Confirmation

* The rotation service models each credential as `{kid, role: signing|encryption, notBefore, notAfter, retireAfter}`.
* Guard test: attempting to retire an encryption `kid` that a live JWE still references is blocked.
* The access token validates as `at+jwt` via `JwtBearer` + JWKS and carries only the minimal claim set.

## Pros and Cons of the Options

### Shared lifecycle for signing and encryption credentials

One retention rule ("retire the cert once its signed tokens expire") applied to both keys.

* Good, because it is the simplest model — one timeline, one guard.
* Bad, because it retires the encryption key on the signing key's schedule and destroys live JWE refresh tokens, authorization codes, and device codes, logging users out and failing redeems.

### Separate lifecycles (chosen)

Track the encryption credential independently with a retention floor set by the longest-lived live JWE artifact.

* Good, because rotation is safe for both roles and no live artifact is ever orphaned.
* Bad, because the rotation service must carry two timelines and two guard sets and must know every JWE artifact's maximum lifetime.

## More Information

* Original decision: 2026-06-28. `DisableAccessTokenEncryption()` was confirmed enabled (plain signed access-token JWT) with a minimal claim set; the 8-hour refresh-token ceiling (ADR-0004) is the upper bound that sets the encryption retention floor at roughly 8 hours plus margin.
* Rationale for the plain access token: it removes per-request JWE decryption CPU on first-party resource servers and eases debugging, while refresh tokens, authorization codes, and device codes stay JWE.
* Related decisions: ADR-0001 (tiered key-set scope), ADR-0004 (8-hour refresh-token ceiling, the retention-floor upper bound), ADR-0006 (disaster recovery for the key material), ADR-0007 (key-compromise runbook), ADR-0011 (no-restart key rotation), ADR-0033 (key-scope isolation model).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized.
