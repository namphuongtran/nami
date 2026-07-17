---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Ops (who holds break-glass purge, the two-approver process, and the per-cloud adapter capability matrix await Security/Ops ratification)
informed: all contributors, via this repository
---

# Access the secret store with least-privilege workload identity and rotate client credentials via private_key_jwt

## Context and Problem Statement

Every key decision (ADR-0005, ADR-0006, ADR-0007) rests on one foundation that had not been stated: **how the application accesses the key/secret store** (DB-backed by default, or a cloud vault/KMS optionally) and **how client secrets rotate with zero downtime**. The earlier plan gave no access model (workload identity, least-privilege, break-glass) and had missed that OpenIddict natively supports zero-downtime secret rollover through `private_key_jwt` client assertions and multiple simultaneous asymmetric keys. How should Nami access the store and rotate client credentials?

## Decision Drivers

* Every key decision depends on a defined, auditable store-access model.
* Store access must use no static long-lived secret.
* Client-secret rotation must be zero-downtime.
* Purge and IAM changes are irreversible and require dual-control.
* Under ADR-0001 the store organizes key material by tier — a shared Pool-group set and each Silo tenant's own set — and the adapter must resolve the right key-set for the resolved tenant.

## Considered Options

* Direct cloud-SDK access with a static credential, and a shared symmetric client secret
* A provider-agnostic port/adapter with least-privilege workload identity, and `private_key_jwt` for service clients (symmetric secret only as a fallback)

## Decision Outcome

Chosen option: "A provider-agnostic port/adapter with least-privilege workload identity, and `private_key_jwt` for service clients", because static credentials are a standing risk and shared-secret rotation forces downtime.

Fixed parameters of the decision:

**A. Store access model** (aligned with ADR-0006: DB-backed default, cloud optional)

* The application accesses the store through an `ISecretResolver`/credential-source **port plus an adapter**, never a cloud SDK directly. The default adapter is DB-backed; cloud is optional.
* Access model per adapter:
  * **DB-backed (default)**: a least-privilege database user (read/write on the key/secret tables, no DROP or admin rights); keys encrypted at rest via Data Protection; "purge" means deleting a row (soft-delete via a status column first) under dual-control; the Data Protection keyring is protected by a certificate/DPAPI on-premises.
  * **Cloud (optional)**: per-platform **workload identity** (Azure Managed Identity, AWS IRSA, GCP Workload Identity, or Vault auth) — no static secret; least-privilege `get`/`unwrap`/`wrap` (and `sign` if store-managed); no `purge`/`delete`/`set` at runtime.
* **Purge/destroy** (DB and cloud alike) is a separate **break-glass** path with **two-approver dual-control** (aligned with ADR-0007).
* **Every store access is audited** into the audit sink (ADR-0008).
* The store has soft-delete/recovery-window plus purge-protection (or the equivalent) per ADR-0006: native in cloud; in DB mode a status-column soft-delete plus backup plus dual-control hard delete.

**B. Client-secret rollover** (zero-downtime)

* **Standard: `private_key_jwt` client assertions** for service/M2M clients — asymmetric-key authentication. OpenIddict allows registering multiple keys at once, enabling zero-downtime rotation (add the new key, let clients migrate, remove the old one). No shared secret; each client manages its own private key.
* **Fallback (symmetric secret)**: only when a client cannot manage its own keys. Support multiple parallel secrets (a side-table `ApplicationSecrets` with expiry) so they overlap during rollover; mask in the admin API; store only the hash.

### Consequences

* Good, because no static secret accesses the store, secret rollover is zero-downtime, and destructive operations are gated by dual-control.
* Bad, because `private_key_jwt` requires each client to manage its own private key (harder for simple clients), which is why the symmetric fallback exists.
* The multiple-secret side-table is extra build, because OpenIddict's application descriptor carries only a single `ClientSecret`.

### Confirmation

* The admin rollover flow (add a new key/secret, observe a grace period, retire the old one) masks secrets, audits every step, and enforces dual-control (two-person, proposer ≠ approver).
* Every store access appears in the audit sink.
* No static credential is used for store access: workload identity in cloud, a least-privilege database user in DB mode.

## Pros and Cons of the Options

### Direct cloud-SDK access with a static credential and a shared symmetric client secret

Bind the application to a cloud SDK using a static credential, and authenticate clients with a single shared secret.

* Good, because it is the simplest to stand up.
* Bad, because the static credential is a standing risk, shared-secret rotation forces downtime, and there is no dual-control on destructive operations.

### Provider-agnostic port/adapter with workload identity and `private_key_jwt` (chosen)

Access the store through a port plus per-cloud adapters using least-privilege workload identity, and authenticate service clients with rotating asymmetric keys.

* Good, because there are no static store secrets, rollover is zero-downtime, and destructive operations require dual-control.
* Bad, because clients must manage their own keys (mitigated by the symmetric fallback) and the multiple-secret side-table is additional work.

## More Information

* Original decision: 2026-06-28. The access model (cloud-agnostic port plus per-cloud adapter, workload identity, no static secrets) and service-client authentication (`private_key_jwt` standard, symmetric fallback) are accepted; who holds break-glass purge, the two-approver process, and the per-cloud adapter capability matrix await Security/Ops ratification.
* Tiered store organization per ADR-0001: a shared Pool-group key-set versus each Silo tenant's own set; the adapter resolves the key-set for the resolved tenant (by host/path); `private_key_jwt` is per-client (clients are tenant-scoped); the IdP's own workload identity is global.
* Related decisions: ADR-0001 (tiered key-set organization), ADR-0005/0006/0007 (the key decisions this access model underpins), ADR-0006 (store durability and the cloud-agnostic seam), ADR-0007 (break-glass dual-control), ADR-0008 (audit of store access and rollover).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized.
