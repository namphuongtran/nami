---
status: reviewed
created: 2026-07-18
tags: [architecture, runtime, sequences]
---

# Runtime views (key sequences)

Ten end-to-end flows that show how the containers and components collaborate at
runtime, grouped as protocol and security flows (1 to 6) and operational flows
(7 to 10). Host names follow ADR-0065.

## 1. Authorization code with PKCE and tenant resolution

The spine flow: sign-in, tenant resolution, and minimal-claim token issuance.

```mermaid
sequenceDiagram
  autonumber
  actor U as End user
  participant RP as Relying party
  participant IDP as Nami.Identity
  participant T as Tenant resolver
  participant DB as PostgreSQL, RLS
  RP->>IDP: connect/authorize, PKCE challenge, scope
  IDP->>T: resolve tenant from host or path
  T->>DB: set app.current_tenant for RLS
  IDP->>U: redirect to login
  U->>IDP: credentials + MFA or passkey
  IDP->>DB: validate global user, load memberships
  IDP->>U: consent if required
  U->>IDP: grant
  IDP->>RP: redirect with authorization code
  RP->>IDP: connect/token, code + PKCE verifier
  Note over IDP: deny-by-default destinations, minimal access token
  IDP->>RP: access token JWT + refresh JWE + id_token
```

## 2. Admin dual-control with step-up

Propose, step-up, approve by a different person, TOCTOU re-check, execute, audit.

```mermaid
sequenceDiagram
  autonumber
  actor P as Proposer
  actor A as Approver
  participant APP as Admin.App BFF
  participant API as Admin.Api
  participant DB as PostgreSQL
  P->>APP: request destructive action
  APP->>API: POST proposal, user-delegated token
  Note over API: RequireActor rejects app-only token
  API->>DB: create proposal, capture TargetETag
  A->>APP: open Approval Inbox
  APP->>API: approve proposal
  API-->>APP: 401 insufficient_user_authentication, RFC 9470
  APP->>A: top-level OIDC re-auth, MFA
  A->>APP: re-authenticated, elevated acr
  APP->>API: approve, proposer not equal approver
  API->>DB: re-check TargetETag, TOCTOU guard
  API->>DB: execute atomically + append audit hash-chain
  API-->>APP: 200 executed
```

## 3. No-restart key rotation

Announce, publish-before-sign, promote, rebuild credentials with no restart.

```mermaid
sequenceDiagram
  autonumber
  participant Q as KeyRotation runner
  participant KS as ISigningKeyStore
  participant OM as custom IOptionsMonitor
  participant JWKS as JWKS / discovery
  participant N as All nodes
  Q->>KS: announce new key, publish before sign
  KS->>JWKS: key appears in JWKS, validation only
  Note over N: propagation window, about 14 days
  Q->>KS: promote to active signer
  KS->>OM: change token fires
  OM->>N: rebuild signing credentials, no restart
  Note over N: retire window, then delete old key
```

Break-glass compromise is the same machinery run fast: mark the key revoked, push
it to the distrusted-kid set, and evict JWKS caches so the compromised key is out
of rotation in under five minutes (ADR-0007).

## 4. Cross-node revocation (break-glass and force-logout)

A revocation on one node is enforced on every other node.

```mermaid
sequenceDiagram
  autonumber
  participant OpA as Node A
  participant R as Redis, distrusted-kid and session
  participant OpB as Node B
  participant C as Client
  OpA->>R: mark kid distrusted or delete session
  Note over R: kid propagation within 60s and session revoke instant
  C->>OpB: request with token
  OpB->>R: check distrusted-kid fail-closed, session valid
  R-->>OpB: distrusted or revoked
  OpB-->>C: 401 invalid_token
```

## 5. DPoP issuance and resource validation

Sender-constrained tokens for public SPA and mobile clients (ADR-0014).

```mermaid
sequenceDiagram
  autonumber
  actor S as SPA or mobile
  participant IDP as Nami.Identity
  participant API as Resource API
  participant R as Redis, jti replay
  S->>IDP: connect/token + DPoP proof
  Note over IDP: validate proof, compute thumbprint
  IDP->>S: access token with cnf.jkt
  S->>API: GET resource, Authorization DPoP token + proof
  Note over API: validate htm, htu, ath + thumbprint
  API->>R: check and insert jti, cross-node replay guard
  API-->>S: 200 or 401 use_dpop_nonce
```

## 6. BFF token custody for a first-party SPA

The token never reaches the browser, which is the real XSS mitigation (ADR-0029).

```mermaid
sequenceDiagram
  autonumber
  actor U as SPA
  participant BFF as Nami.Identity.Bff
  participant IDP as Nami.Identity
  participant API as Resource API
  U->>BFF: /bff/login
  BFF->>IDP: OIDC code + PKCE, confidential client
  IDP->>BFF: tokens stored server-side
  BFF->>U: session cookie only, no token in browser
  U->>BFF: /api/orders with cookie
  BFF->>API: proxy with bearer, server-side
  API-->>BFF: data
  BFF-->>U: data
  Note over BFF: silent-renew failure leads to 401 then top-level redirect
```

## 7. Tenant provisioning saga

Onboarding a tenant as a single orchestrated saga (ADR-0017).

```mermaid
sequenceDiagram
  autonumber
  actor Op as Operator
  participant API as Admin.Api
  participant PS as Provisioning service
  participant DB as PostgreSQL
  participant DNS as DNS / TLS
  Op->>API: create tenant, Pool or Silo
  API->>PS: start provisioning saga
  PS->>DB: register tenant + closure, Enabled false
  alt Silo tenant
    PS->>DB: create tenant database, run migrations
    PS->>DB: seed key set
  end
  PS->>DB: seed baseline clients and scopes
  PS->>DNS: provision subdomain + certificate
  PS->>DB: residency check, then set Enabled true
  PS-->>API: tenant provisioned, readiness passes
```

## 8. Transactional email outbox

Confirm and reset mail that is neither lost nor sent before commit (ADR-0038).

```mermaid
sequenceDiagram
  autonumber
  actor U as End user
  participant IDP as Nami.Identity
  participant DB as PostgreSQL
  participant Relay as Email relay
  participant P as Email provider
  U->>IDP: register or reset password
  Note over IDP,DB: one transaction
  IDP->>DB: create or update user
  IDP->>DB: mint token, enqueue OutboxEmail row
  IDP->>DB: commit
  IDP-->>U: constant-time response, no account disclosure
  Relay->>DB: claim pending row, SKIP LOCKED
  Relay->>P: send, at least once
  P-->>Relay: accepted, store provider id
  Relay->>DB: mark sent, redact token
```

## 9. Delegated cross-tenant admin action

An administrator acting on a child tenant under a delegated grant (ADR-0010).

```mermaid
sequenceDiagram
  autonumber
  actor Ad as Parent-tenant admin
  participant APP as Admin.App BFF
  participant API as Admin.Api
  participant AZ as ICheckAccess
  participant DB as PostgreSQL
  Ad->>APP: act on child tenant
  APP->>API: request with actor and act claim
  API->>AZ: check capability on target, strong consistency
  AZ->>DB: read delegated grant + tenant closure
  AZ-->>API: allowed within forbidden-cascade
  Note over API: anti-confused-deputy, initiator resolved
  API->>DB: execute + append audit with provenance
  API-->>APP: 200, actor recorded
```

## 10. GDPR erasure saga

Right-to-erasure reconciled with the tamper-evident audit chain (ADR-0016).

```mermaid
sequenceDiagram
  autonumber
  actor DPO as Operator or DPO
  participant API as Admin.Api
  participant ES as Erasure service
  participant DB as PostgreSQL
  DPO->>API: erasure request for subject
  API->>ES: start saga, dual-control approved
  ES->>DB: revoke live access first
  ES->>DB: delete operational data
  ES->>DB: delete identity data
  ES->>DB: destroy per-subject DEK, crypto-shred
  Note over DB: audit chain stays intact, payload unreadable
  ES-->>API: verified, tombstone written
```

---

[← Prev: Data](05-data.md) · [Index](README.md) · Next: [Cross-cutting →](07-cross-cutting.md)
