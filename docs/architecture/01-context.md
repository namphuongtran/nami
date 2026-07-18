---
status: reviewed
created: 2026-07-18
tags: [architecture, c4, context]
---

# Context view (C4 Level 1)

Who and what sits around Nami. Nami is one system; everything else is an actor
who uses it or an external system it talks to across the boundary.

```mermaid
graph TB
  enduser([End user]):::person
  admin([Tenant / delegated admin]):::person
  breakglass([Break-glass operator]):::person
  ops([Operator / DevOps]):::person

  nami[Nami Identity Provider<br/>multi-tenant OAuth2 / OIDC server]:::host

  rp[Relying-party apps<br/>web, SPA, mobile, device, M2M]:::ext
  api[Resource APIs]:::ext
  extidp[External IdPs<br/>Entra ID, Google, OIDC]:::ext
  email[Email provider]:::ext
  kms[KMS / secret store]:::ext
  obs[Observability and SIEM]:::ext
  hibp[Breach-check service]:::ext

  enduser -->|authenticates, consents| nami
  admin -->|administers via BFF| nami
  breakglass -->|emergency unseal| nami
  ops -->|provisions, deploys| nami
  rp -->|OAuth2 / OIDC + back-channel logout| nami
  api -->|JWKS / introspection| nami
  nami -->|delegated sign-in| extidp
  nami -->|confirm / reset / notify| email
  nami -->|wrap keys, resolve secrets| kms
  nami -->|telemetry + audit forward| obs
  nami -->|password-exposure check| hibp

  classDef person fill:#08427b,stroke:#052e56,color:#ffffff
  classDef host fill:#1168bd,stroke:#0b4884,color:#ffffff
  classDef store fill:#438dd5,stroke:#2e6295,color:#ffffff
  classDef ext fill:#999999,stroke:#6b6b6b,color:#ffffff
```

## Actors

| Actor | Role at the boundary |
|---|---|
| End user | Browser-based sign-in, consent, passkey/MFA, tenant switch |
| Tenant / delegated admin | Manages tenant resources through the admin BFF, under RBAC and delegated-admin grants (ADR-0010) |
| Break-glass operator | Emergency, dual-control access that works even when the IdP cannot issue tokens (ADR-0007, ADR-0015) |
| Operator / DevOps | Tenant onboarding, deployment under dual control, DR drills |

## External systems

| System | Relationship |
|---|---|
| Relying-party apps | OAuth2/OIDC clients (web, SPA, mobile, device, M2M); receive tokens and back-channel `logout_token` |
| Resource APIs | Validate tokens locally by JWKS or by introspection, per-tenant issuer (ADR-0048, ADR-0049) |
| External IdPs | Federated sign-in; static and global in v1, per-tenant dynamic in v2 (ADR-0002, ADR-0034) |
| Email provider | Confirmation, reset, and notification mail through a cloud-agnostic port (ADR-0038) |
| KMS / secret store | Optional envelope encryption and secret resolution; database-backed default when absent (ADR-0006, ADR-0009) |
| Observability and SIEM | OTLP telemetry sink and write-once audit anchoring (ADR-0022, ADR-0008) |
| Breach-check service | Password-exposure check with k-anonymity, fail-open (ADR-0028) |

---

[← Index](README.md) · Next: [Domain →](02-domain.md)
