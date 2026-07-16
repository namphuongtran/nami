---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Ops (the detailed runbook steps and the authorized-personnel list await Security/Ops ratification)
informed: all contributors, via this repository
---

# Eject a compromised key from the JWKS within five minutes with a break-glass runbook

## Context and Problem Statement

Nami's routine key rotation is graceful expiry only: a new certificate with a further-out `NotAfter` gradually supersedes the old one. When a signing or encryption key is **compromised**, graceful expiry is far too slow — an attacker can sign forged tokens until the old key expires on its own. Nami needs a **break-glass** path that pulls a dirty key out of the JWKS almost immediately. No mainstream product ships a turnkey break-glass procedure, so this is bespoke on any platform. Blast radius depends on the scope of the leaked key, so the runbook must first determine that scope before it purges or revokes anything.

## Decision Drivers

* Graceful expiry cannot bound the exploitation window of a leaked key; a fast ejection path is required.
* Blast radius follows the tiered model (ADR-0001): a leaked **Pool-group** key affects every Pool tenant (rotate/revoke the whole group); a leaked **Silo** key affects only that tenant; a leaked **global** Data Protection keyring is a system-wide incident. The runbook must establish scope (pool-group vs single Silo tenant) before acting.
* Mass-revocation is irreversible and therefore requires dual-control.

## Considered Options

* Graceful expiry only
* Break-glass runbook with automation, an SLO, and drills

## Decision Outcome

Chosen option: "Break-glass runbook with automation, an SLO, and drills", because graceful expiry cannot bound the exploitation window of a compromised key.

Fixed parameters of the decision:

* **SLO: a dirty key is out of the JWKS in under 5 minutes.**
* Runbook steps (automation plus checklist):
  1. Provision a clean certificate/key through the key-store port and promote it to signer (DB adapter: insert the new key and mark it signer; cloud adapter: create it in the vault/KMS).
  2. **Coordinated reload of every node**, reusing the rotation routine's reload mechanism (custom `IOptionsMonitor` plus a tripped change-token, no restart — ADR-0011).
  3. **Un-register the dirty certificate** from the credential set.
  4. **Force-evict** the discovery and JWKS cache on every node (override the count-based cache and set downstream `Cache-Control`).
  5. **Purge server-side state** for the blast radius (session store — ADR-0003).
* **Signing-key compromise**: tokens signed with the dirty key stop validating once it is pulled from the JWKS; revoke related sessions and authorizations if needed.
* **Encryption-key compromise**: treat **every outstanding refresh token, authorization code, and device code as burned** and revoke them all, because the attacker can decrypt them.
* Mass-revocation requires **dual-control** (ADR-0009), triggers an incident-response notification, and is audit-logged (ADR-0008).

### Consequences

* Good, because it caps the exploitation window of a leaked key at minutes rather than the token lifetime.
* Bad, because it needs a fast reload path and multi-node cache eviction (invested through the rotation and observability work) plus periodic drills.
* This decision depends on ADR-0003 (session purge), ADR-0005 (separate encryption-credential lifecycle), ADR-0006 (DB-backed key store and DR), ADR-0008 (audit), ADR-0009 (dual-control), and ADR-0011 (the no-restart reload mechanism it reuses).

### Confirmation

* The DR drill for this runbook runs quarterly and after every key-infrastructure change, synchronized with ADR-0006.
* Force-logout-subject and mass-revoke are available admin actions and are automatable.
* When a session is purged, clients are notified (interim back-channel logout).

## Pros and Cons of the Options

### Graceful expiry only

Let a compromised key age out as the new certificate's further `NotAfter` supersedes it.

* Good, because it needs no additional mechanism.
* Bad, because it cannot bound the exploitation window: an attacker keeps signing forged tokens until the leaked key expires on its own.

### Break-glass runbook with automation (chosen)

An automated, scoped procedure that ejects a dirty key from the JWKS under an SLO, backed by drills.

* Good, because exploitation is bounded to minutes and the procedure is rehearsed.
* Bad, because it requires a fast multi-node reload and cache-eviction capability plus an ongoing drill process.

## More Information

* Original decision: 2026-06-28. The SLO (out of the JWKS in under 5 minutes), the dual-control trigger for mass-revoke/purge (proposer ≠ approver), and the quarterly-plus-post-change drill cadence are accepted; the authorized-personnel list and the detailed multi-node reload/cache-evict automation await Security/Ops ratification.
* No mainstream product ships turnkey break-glass, so this runbook is bespoke regardless of platform.
* Blast-radius scoping under the tiered model (ADR-0001): a Pool-group key affects all Pool tenants, a Silo key affects only that tenant, and the global Data Protection keyring is a system-wide incident.
* Related decisions: ADR-0001 (tiered blast radius), ADR-0003 (session purge), ADR-0005 (encryption-credential lifecycle), ADR-0006 (key store and DR), ADR-0008 (audit), ADR-0009 (dual-control), ADR-0011 (no-restart reload mechanism).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. Corrected the source's attribution of the no-restart reload mechanism from ADR-06 to ADR-0011, which is that mechanism's actual home (ADR-0011 in turn lists this runbook as reusing it).
