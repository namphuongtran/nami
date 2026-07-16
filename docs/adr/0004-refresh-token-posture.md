---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: verification of OpenIddict 7.5.0 source (rolling refresh, one-time-use, reuse/replay detection, and chain revocation confirmed default-on)
informed: all contributors, via this repository
---

# Keep OpenIddict's native refresh-token mechanics rather than rebuilding them

## Context and Problem Statement

Preliminary analysis implied Nami would have to hand-build rolling refresh tokens, reuse detection, and cascade revocation. Verifying the OpenIddict 7.5.0 source shows the opposite: **rolling refresh tokens, one-time-use, reuse/replay detection, and chain revocation by authorization are all default-on** in the engine. Re-implementing them would be redundant and would weaken the guarantees the engine already makes. Should Nami rebuild these mechanics or keep the native ones and add only what is genuinely missing?

## Decision Drivers

* Preserve the engine's security guarantees; the biggest risk here is a regression caused by accidentally disabling a default, not by missing functionality.
* Avoid over-engineering: duplicating native behavior (for example calling revoke a second time) is error-prone and self-defeating.
* Reliability: the reuse-detection leeway must not sit below realistic network timeouts, or legitimate clients get logged out spuriously.
* Refresh/authorization/token storage is tenant-scoped per the ADR-0001 tiered isolation model, and every added mechanism must respect that.

## Considered Options

* Build a custom cascade/reuse-detection handler
* Keep the native mechanics and add observation only (audit emission)

## Decision Outcome

Chosen option: "Keep the native mechanics and add observation only", because rolling refresh, one-time-use, reuse/replay detection, and family (chain) revocation are already default-on in OpenIddict 7.5.0; rebuilding them would duplicate the engine and risk weakening it.

Fixed parameters of the decision:

* **Do not disable the defaults**: never call `DisableRollingRefreshTokens()`, `DisableAuthorizationStorage()`, or `DisableTokenStorage()`.
* **Reuse leeway: 30 seconds** (the OpenIddict default, aligned with the ~30s industry sweet spot). An earlier draft set 15s to tighten security; that was corrected upward on 2026-07-01 because 15s sits *below* typical network timeouts (~30s): a client would time out, retry outside the leeway, trigger family-revoke, and log the user out spuriously. Multi-tab SPA, mobile reconnect, and lost-response retries hit the same edge. 30s covers network-timeout plus concurrency; the marginal security gain of 15s is not worth the reliability cost.
* **Family-revoke is native — we only add audit emission.** On reuse detection (outside the leeway), OpenIddict itself calls `RevokeByAuthorizationIdAsync` and revokes the **sibling tokens** of the authorization. Do **not** call `RevokeByAuthorizationIdAsync` ourselves — that double-revokes and is exactly the over-engineering this ADR forbids. OpenIddict deliberately does **not** revoke the `Authorization` object itself, so a legitimate client can start a fresh flow; our addition is limited to emitting an audit event on the (otherwise silent) engine revoke.
* **Absolute refresh lifetime ceiling**: rolling refresh gives sliding lifetime only, so a hard ceiling is stamped as an anchor on `Authorization.Properties` and enforced by rejecting the token request once the absolute expiry has passed.
* **Per-client refresh policy**: `IssueRefreshToken` is per-client; machine-to-machine / client-credentials clients get no refresh token.
* **Disabled-user handling**: deliberately de-scope Duende-style per-validation `IsActiveAsync` (which adds a database hit to the hot path and erodes JWT statelessness). Instead, gate at issuance (`CanSignInAsync`) and rely on **on-disable force-revoke** (authorizations plus sessions, per ADR-0003) with a 15-minute residual access-token TTL for JWTs and immediate effect for reference tokens issued to sensitive clients. This is an intentional narrowing tied to tiered revocation, not a hidden gap.
* **Prune job reconciliation**: the retention/prune job's `MinimumTokenLifespan` must exceed the longest refresh-token lifetime so redeemed entries still needed for reuse detection are not pruned early. Because the prune job runs outside a request, it must **iterate per tenant** and set the tenant context manually (Pool tenants: filter by `TenantId`; Silo tenants: per connection).

### Consequences

* Good, because the engine's security guarantees are preserved with minimal code, and token-theft protection comes from the built-in reuse detection.
* Bad, because the team must understand the defaults well enough not to disable one by accident; this is mitigated with a startup invariant check and a pipeline-snapshot test.
* A disabled user may keep using an already-issued JWT for up to 15 minutes unless force-revoked (which Nami does on disable). This trade-off is deliberate and documented, not an oversight.

### Confirmation

* Integration test: replaying a redeemed refresh token outside the leeway returns `invalid_grant` / `invalid_token` (OpenIddict error `ID2012`, "refresh token already redeemed") and the authorization's **sibling tokens** are revoked. The test must **not** assert that the `Authorization` itself is revoked — OpenIddict intentionally keeps it, and asserting otherwise would fail. Within the leeway, a concurrent retry still succeeds.
* Concurrency test (multi-tab / mobile / lost-response retry) is required.
* A startup invariant check confirms rolling refresh, reuse detection, and chain revocation remain enabled, guarding against a misconfiguration regression.

## Pros and Cons of the Options

### Build a custom cascade/reuse-detection handler

Hand-write rolling refresh, reuse detection, and cascade revocation instead of using the engine's.

* Bad, because it duplicates behavior OpenIddict already provides and risks weakening the engine's guarantees.
* Bad, because reuse detection and chain revocation are subtle; a bespoke implementation is more likely to be wrong.
* Neutral, because it offers no capability the native mechanics lack.

### Keep the native mechanics and add observation only (chosen)

Rely on OpenIddict's default-on rolling refresh, one-time-use, reuse detection, and family revocation; add only audit emission and a hard absolute-lifetime ceiling.

* Good, because it preserves engine guarantees with the least code.
* Good, because token-theft protection via reuse detection is battle-tested rather than bespoke.
* Bad, because it depends on the defaults staying enabled, which must be enforced by an invariant check.

## More Information

* Original decision: 2026-06-28. The reuse leeway was corrected from 15s to 30s on 2026-07-01 after finding 15s sat below typical network timeouts and caused spurious family-revoke and logout; first-party SPAs additionally run behind a BFF (ADR-0029), which serializes refresh server-side and removes multi-tab concurrency, leaving public mobile/native clients as the case the 30s leeway covers.
* Rejected alternative: Duende IdentityServer v7.0 switched its default to `ReUse` (rotation off) to reduce database pressure and lost-response re-logins. Nami keeps rolling rotation (per BCP RFC 9700, with stronger theft detection) and mitigates the reliability concern with the 30s leeway plus the BFF — the very reliability margin the 15s value had eroded.
* Fixed parameters: access-token lifetime 15 minutes; refresh-token absolute lifetime ceiling 8 hours (matching the ADR-0003 absolute session limit; rolling slides within it, but the hard 8-hour ceiling forces re-authentication); reuse leeway 30 seconds; machine-to-machine clients issue no refresh token.
* Related decisions: ADR-0001 (tenant-scoped token storage under the tiered isolation model), ADR-0003 (absolute session lifetime and on-disable session revocation), ADR-0019 (single logout strategy), ADR-0029 (BFF serializes first-party SPA refresh).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized.
