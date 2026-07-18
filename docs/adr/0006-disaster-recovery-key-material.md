---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: DPO and Ops (the formal RTO/RPO targets and DR runbook await their ratification during operation); comparison with commercial identity servers' signing-key store abstractions
informed: all contributors, via this repository
---

# Make key-material storage and disaster recovery provider-agnostic

## Context and Problem Statement

Nami depends on three layers of key material: (a) the signing certificate, (b) the encryption certificate (JWE, which cannot be disabled), and (c) the ASP.NET Core Data Protection keyring (which protects cookies and some artifacts). Losing or wrongly restoring any layer is severe and sometimes unrecoverable: losing the Data Protection keyring makes every cookie and handle unreadable — a self-inflicted mass logout — and losing the encryption certificate makes refresh-token, authorization-code, and device-code JWEs permanently undecryptable. An RTO under 15 minutes and RPO under 5 minutes were stated as targets but were not yet wired to concrete mechanisms, and no recovery drill existed. Under the tiered isolation model, disaster recovery must also cover three distinct key groups. How should key material be stored and recovered?

## Decision Drivers

* Losing any key layer is severe and often irreversible, so disaster recovery must be a designed, tested capability, not an assumption.
* The product must run multi-cloud **and** on-premises/local with no cloud at all, so key and secret management must be provider-agnostic.
* The Data Protection keyring must be restored verbatim (fixed `SetApplicationName`); renaming loses the old keys.
* Key provisioning and deletion are irreversible and require dual-control.
* Recovery must cover three key groups per ADR-0001: the **global** Data Protection keyring (identity plane), the **Pool-group** key-set shared by Pool tenants, and each **Silo** tenant's own key-set.

## Considered Options

* Rely on defaults (ephemeral/local Data Protection keyring, key vault without purge-protection)
* Actively designed, provider-agnostic disaster recovery (soft-delete plus purge-protection, portable keyring backup, periodic restore drills)

## Decision Outcome

Chosen option: "Actively designed, provider-agnostic disaster recovery", but **not locked to any cloud**, because the product must also run on-premises with no cloud dependency. Key and secret management sits behind an abstraction seam with per-deployment adapters.

Fixed parameters of the decision:

* **The application never calls a cloud SDK directly.** It defines ports and plugs an adapter per deployment: `ISigningCredentialSource` / `IEncryptionCredentialSource` (load certs/keys into OpenIddict's multi-certificate rotation), `ISecretResolver` (resolve external-IdP client secrets, connection strings), and an `IDataProtectionKeyStore` configuration (persist the keyring portably).
* **The default adapter is DB-backed**: a `SigningKeys` table encrypted at rest via Data Protection, following the common signing-key-store pattern (a keys table behind a store abstraction), independently designed here. This is the cloud-neutral baseline that runs on-premises, because not every deployment uses a cloud.
* **Cloud adapters are optional** (HashiCorp Vault, Azure Key Vault, AWS KMS + Secrets Manager, GCP KMS + Secret Manager) for those wanting HSM-backed or managed rotation. Every adapter must meet mandatory capabilities: versioning, soft-delete/recovery-window, purge-protection, encrypt-at-rest, and access auditing. The DB adapter meets these via a status-column soft-delete, Data Protection encryption, and the audit log.
* **Root of trust at rest**: keys in the database are encrypted by the Data Protection keyring; the keyring is protected by a certificate/DPAPI on-premises, or a cloud KEK when a cloud is present.
* **Signing default = envelope encryption, not sign-in-HSM.** By default the private key is wrapped at rest by a KEK (the Data Protection keyring on-premises, or a cloud KMS key), unwrapped into memory, and used to sign locally, because OpenIddict signs in-process and cannot delegate signing to an HSM natively. Sign-in-HSM (the key never leaves the HSM) is an optional adapter via a custom signature provider, accepting its latency/throughput cost for the smaller blast radius.
* **Multi-tenant storage**: the Pool key-set lives in the global `SigningKeys` table (control plane); each Silo tenant keeps its key-set in its own database (hard isolation).

Disaster-recovery requirements (provider-agnostic):

* Enable **soft-delete plus purge-protection** (or the equivalent) for all three — signing certificate, encryption certificate, and the keyring-wrapping key — at whatever provider is in use.
* **Persist the Data Protection keyring** to a durable, portable store, encrypted at rest, with a **fixed `SetApplicationName`** (it must restore verbatim; changing the name loses the old keys).
* Bind the RTO under 15 minutes and RPO under 5 minutes targets to **each store** (keyring, certificates, operational database, session store).
* Run a **DR restore drill quarterly and after every key-infrastructure change**, producing evidence that tokens and cookies issued before the restore still validate after it.
* **Monitor RPO continuously**, not only at the quarterly drill: alert on write-ahead-log archiving lag, last-successful-backup age, and replication lag against each store's bound RPO, so a backup that has silently stopped is caught before a disaster rather than during one.
* Document the **blast radius** by token format.
* Key provisioning and deletion are **irreversible and require dual-control** (ADR-0009) plus DPO/Security sign-off.

### Consequences

* Good, because key loss has a defined recovery path and a wrong operation cannot cause a self-inflicted mass logout.
* Good, because the provider-agnostic seam keeps Nami runnable on-premises and portable across clouds.
* Bad, because it adds infrastructure (vault policies, a backup pipeline) and a recurring drill process, with the attendant operational cost.
* This decision is coupled to ADR-0005 (encryption credential lifecycle), ADR-0007 (key compromise), and ADR-0009 (key vault access and dual-control).

### Confirmation

* The DR restore drill is a recurring test item, run quarterly and after each key-infrastructure change; the pass criterion is that tokens and cookies issued before the restore still validate after it.
* Multi-node deployments share one keyring and one fixed application name across every node.
* An adapter capability matrix (versioning, soft-delete, purge-protection, audit) is documented per adapter.

## Pros and Cons of the Options

### Rely on defaults

Leave the Data Protection keyring ephemeral/local and the key vault without purge-protection.

* Good, because it needs no additional infrastructure.
* Bad, because the risk of permanent key loss is high and there is no real recovery path; a keyring loss silently becomes a mass logout.

### Actively designed, provider-agnostic disaster recovery (chosen)

Ports plus per-cloud adapters, a DB-backed default, soft-delete/purge-protection everywhere, a portable keyring, and periodic restore drills.

* Good, because recovery is real and rehearsed, and the seam preserves on-premises and multi-cloud portability.
* Bad, because it costs added infrastructure and an ongoing drill process.

## More Information

* Original decision: 2026-06-28. The cloud-agnostic direction is accepted; the formal RTO/RPO targets, the DR runbook, and the per-adapter capability matrix (versioning, soft-delete, purge-protection, audit) await Ops/DPO ratification during operation.
* Disaster recovery must cover three key groups per the ADR-0001 tiered model: the global Data Protection keyring, the Pool-group key-set, and each Silo tenant's own key-set. The earlier v1 assumption of a per-tenant key for every tenant is dropped — only Silo tenants have their own key-set.
* Deferred to a post-v1 wave (proposed, no ADR yet): a FIPS 140-3-validated crypto mode (an OS/HSM-tier configuration over this key and crypto stack); revisit for a US-government or otherwise regulated deployment.
* Related decisions: ADR-0001 (tiered key-set scope), ADR-0005 (encryption credential lifecycle), ADR-0007 (key-compromise runbook), ADR-0009 (key vault access and dual-control).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized.
