---
status: "accepted"
date: 2026-07-09
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: the OpenIddict.Validation and JwtBearer TokenValidationParameters layer; Finbuckle per-tenant options; the A-7 resource-server-validation spike (V27); evidence R19
informed: all contributors, via this repository
---

# Isolate tenants at the resource server by issuer and tenant binding, because a shared keyset means the signature is not an isolation boundary

## Context and Problem Statement

The access-token issuer is already per-tenant (spike A-5), but ADR-0033 chose one shared signing keyset per pool-group, so Pool tenants sign with the same key. That has a sharp consequence at the resource server: a resource server that trusts the signature alone cannot tell one Pool tenant's token from another's, because both verify against the same key. Signature validity is therefore **not** a tenant-isolation boundary under the shared-keyset model. This is the resource-server-side mitigation that makes ADR-0033's accepted shared-key risk actually acceptable, and without it the shared key would permit cross-tenant token acceptance. The naive fix, one authentication scheme per tenant, does not scale and requires a restart whenever a tenant is provisioned, which conflicts with dynamic provisioning. How should a resource server validate tokens so tenant isolation holds even when tenants share a signing key?

## Decision Drivers

* Tenant isolation must hold at the resource server even when Pool tenants share a signing key (ADR-0033).
* It must scale and survive dynamic tenant provisioning without a per-tenant authentication scheme (which needs a restart).
* Isolation must be enforced at the layer both OpenIddict.Validation and JwtBearer rest on (TokenValidationParameters), so one design covers both.
* Sender-constraint (DPoP `cnf.jkt`) must compose with per-tenant validation.

## Considered Options

* One authentication scheme per tenant
* Trust the token signature as the tenant boundary
* Two resource-server shapes: per-tenant-host single-scheme issuer rejection, and shared-host multi-issuer validation with tenant-claim isolation

## Decision Outcome

Chosen option: "Two resource-server shapes", rejecting both a scheme-per-tenant (which cannot scale or absorb dynamic provisioning) and signature-as-boundary (which the spike proved insufficient under a shared keyset). The fixed design is:

* **A. Per-tenant-host endpoints (shape a).** For endpoints whose host or path already carries the tenant (the IdP's own userinfo/introspection/revocation, and per-tenant-host resource APIs), use a single validation scheme with Finbuckle `ConfigurePerTenant<OpenIddictValidationOptions, ...>` resolving the tenant from host/path (the same seam as spike A-5). The expected issuer is that host's tenant, and the token is **rejected if its `iss` does not match**. A Pool resource server sharing the database uses `UseLocalServer()` plus `EnableTokenEntryValidation` for instant revocation.
* **B. Shared-host product APIs (shape b).** For product APIs served on a shared host, resolve the issuer from the token, validate it with an `IssuerValidator` against the known set of tenant issuers, and isolate by the `tenant` claim mapped to row-level security (ADR-0037).
* **C. The load-bearing invariant.** Because Pool tenants share a keyset (ADR-0033), the signature does not isolate tenants; the resource server must validate signature **and** issuer **and** audience (and, for shape b, the `tenant` claim driving RLS). This is stated as an invariant so it cannot be quietly dropped, since dropping it re-opens cross-tenant token acceptance.
* **D. Enforced at the shared validation layer.** The rules live in `TokenValidationParameters`, the layer both OpenIddict.Validation and JwtBearer build on, so a single implementation covers reference-token and JWT paths, and DPoP `cnf.jkt` sender-constraint composes on top after per-tenant validation.

### Consequences

* Good, because tenant isolation holds at the resource server even under a shared Pool keyset, which is exactly what makes ADR-0033's accepted risk safe rather than a cross-tenant hole.
* Good, because it scales and absorbs dynamic tenant provisioning without per-tenant schemes or restarts, and the core invariant is proven at the layer both validators share.
* Good, because sender-constraint (DPoP) composes cleanly with per-tenant validation.
* Bad, because the shared-host shape must maintain the known-tenant-issuer set, the `IssuerValidator`, and the `tenant`-claim-to-RLS wiring, which is more moving parts than trusting the signature.
* Neutral, because the framework wiring (real OpenIddict.Validation plus Finbuckle `ConfigurePerTenant`, introspection for reference tokens, and Silo per-tenant keys) is a Phase-03 integration gate; the security invariant is de-risked at the core layer, the end-to-end wiring is not yet proven.

### Confirmation

* The A-7 spike (V27, run 2026-07-09, 4/4 passing) proves the core at the `TokenValidationParameters` layer: reject on issuer mismatch (T1); under the ADR-0033 shared key the signature does not isolate, only issuer binding does (T2); multi-issuer validation plus the `tenant` claim plus RLS isolates even under a shared signing key (T3); and DPoP `cnf.jkt` composes with per-tenant validation (T4).
* Phase-03 integration test 9.6j wires the real OpenIddict.Validation plus Finbuckle `ConfigurePerTenant` (confirming the API at Finbuckle 10.1.x or using a custom resolver), introspection for reference tokens, and Silo per-tenant keys end to end.

## Pros and Cons of the Options

### One authentication scheme per tenant

* Good, because each tenant's validation is fully separate and easy to reason about in isolation.
* Bad, because the number of schemes grows with tenants and a new tenant needs a restart to register its scheme, which is incompatible with dynamic provisioning and does not scale.

### Trust the token signature as the tenant boundary

* Good, because it is the simplest possible validation.
* Bad, because under the ADR-0033 shared Pool keyset two tenants' tokens verify against the same key, so the signature cannot distinguish them; the spike (T2) proved this is not isolation.

### Two resource-server shapes with issuer/tenant binding (chosen)

* Good, because isolation holds under a shared keyset, it scales, it absorbs dynamic provisioning, and it is proven at the shared validation layer.
* Bad, because the shared-host shape carries more wiring, and end-to-end framework integration remains a Phase-03 gate.

## More Information

* This is the resource-server-side companion to ADR-0033: ADR-0033 accepts that Pool tenants share a signing key, and this ADR is the mitigation at the token-validation layer that keeps that acceptable. Recorded from the endpoint/service-communication design (doc 12 §3) and evidence R19/V27; the spike lives in the repository's spike harness (A-7).
* Related decisions: ADR-0001 (per-tenant issuer, tenant resolution by host/path, single-tenant tokens), ADR-0033 (the shared Pool keyset whose accepted risk this mitigates at the resource server), ADR-0037 (PostgreSQL row-level security used to isolate by the `tenant` claim), ADR-0048 (introspection for reference tokens on these resource servers), ADR-0014 (DPoP sender-constraint that composes on top), and ADR-0021 (the version-sensitive Finbuckle `ConfigurePerTenant` seam re-verified per bump).
* Authored in this repository in 2026-07 to record the settled resource-server validation model as an ADR; the engine and library (OpenIddict, JwtBearer, Finbuckle) are named factually for identification only, and no commercial competitor is named.
