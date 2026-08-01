---
status: "accepted"
stack-record: true
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Ops (the rootCert provisioning/rotation ceremony and the per-environment cloud protector adapter await Ops ratification); verification against ASP.NET Core Data Protection (.NET 10) behavior
informed: all contributors, via this repository
---

# Bootstrap keys by auto-seeding at cold start, root the keyring in an X.509 certificate, and restore both key stores together

## Context and Problem Statement

The Data Protection keyring wraps the signing key's `Data` blob at rest. On an empty database (a fresh deploy or a DR restore) there is no signing key yet, and the keyring must be usable **before** any signing key can be read or written, a chicken-and-egg ordering. Getting the order wrong kills the deploy or the restore, which makes this a SEV-1 gap: cold start is the point where a new deployment or a DR restore most easily dies. ADR-0006 fixed the DR philosophy but gave no concrete cold-start sequence and did not pin a default keyring protector for on-premises. What is the exact bootstrap and restore sequence?

## Decision Drivers

* Cold start (fresh deploy and DR restore) must be deterministic and self-healing, with no manual ops step for the first key.
* It must be multi-node safe without requiring a distributed lock.
* The default must be cloud-neutral so it runs on-premises.
* A missing keyring must never cause silent key regeneration, which would invalidate every existing token and session.

## Considered Options

* First-key seeding: (a) auto-seed with an initialization window and no distributed lock, tolerating convergence; (b) explicit provisioning via a CLI/init-job with dual-control but no self-healing DR; (c) a hybrid.
* Keyring root protector (on-prem/DB default): (i) `ProtectKeysWithCertificate` (X.509), multi-node-safe and cloud-neutral; (ii) passphrase-derived; (iii) DPAPI, single-host Windows only.

## Decision Outcome

Chosen options: first-key seeding by **auto-seed (a)**, and keyring root protector **`ProtectKeysWithCertificate(X509)` (i)**. DPAPI and a cloud key vault remain optional OS/cloud adapters, not the default.

The ordered bootstrap sequence, run on every cold start:

1. **Data Protection keyring**: `AddDataProtection().PersistKeysToDbContext<DataProtectionDbContext>().ProtectKeysWithCertificate(rootCert).SetApplicationName("Nami.Identity")`. Data Protection lazily bootstraps its master key (immediate activation when the ring is empty).
   * **If this line fails to compile, fix the type argument, not the context.** The constraint is `where TContext : DbContext, IDataProtectionKeyContext` (read from the shipped `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.8 assembly metadata, 2026-08-01), and only `DataProtectionDbContext` implements that interface (design [02](../design/02-data.md) section 1). The tempting way to make a wrong type argument compile is to add a `DbSet<DataProtectionKey>` to the other context. That compiles, runs, and **silently moves the keyring into a store that does not own it**, which breaks the per-context `__EFMigrationsHistory` split, the ownership stated in design 02, and the DR requirement that `DataProtectionKeys` is restored as its own store (ADR-0006). The instruction is the deliverable here, more than the one-word type name.
2. **Seed the first key** (`KeyRotationHostedService.StartAsync`, blocking startup): inside a transaction plus a DB advisory-lock / unique constraint (`Use`, `State=active`) so only one instance seeds even across many nodes; generate the first signing and encryption key with **immediate activation** (key #1 does not wait the 14-day propagation), wrap them with Data Protection, and persist with `DataProtected=true`.
3. **Materialize** the options through the #1434 seam (ADR-0011) for dynamic signing and JWKS.
4. **Readiness gate** `/health/ready`: passes only when there is at least one active signing key, at least one encryption key, and a keyring probe that asserts the **active `kid` matches the expected persisted `kid`**. Fail-closed. A bare protect-and-unprotect round trip is **not** sufficient and must not be used: Data Protection silently regenerates a keyring it cannot load, so the round trip passes on the freshly regenerated key, the pod goes Ready, takes traffic, and every previously issued cookie plus every DP-wrapped signing key is already undecryptable. Design [12](../design/12-key-management.md) section 5.8 states this probe and the companion DR-restore rule (`DisableAutomaticKeyGeneration()`, scoped to the DR path only so it does not block a legitimate empty-ring cold start).
5. Open traffic.

Multi-node: **no distributed lock**; the unique constraint / advisory lock exists only to prevent two active signers at cold start.

### Consequences

* Good, because a fresh deploy or a DR restore is self-healing and needs no manual ops step and no restart for the first key.
* Bad, because the application identity generates keys; this is mitigated by a bootstrap audit event recording who, when, and which `kid`, and dual-control still applies to revoke/purge/rotate-out (it does not block bootstrap).
* **DR is restore-both (mandatory)**: back up and restore `SigningKeys`, `DataProtectionKeys`, and the `rootCert` protector **together**, keeping an identical `SetApplicationName`. Losing the keyring or rootCert while keeping `SigningKeys` means the keys cannot be decrypted ("Error unprotecting key with kid...") and auto-management would generate new keys, invalidating old tokens and sessions. The quarterly DR drill verifies that tokens and cookies issued before the restore still validate after it (ADR-0006).
* This decision depends on ADR-0006 (DR philosophy and cloud-agnostic adapters) and ADR-0011 (materialization via the #1434 seam).

### Confirmation

* Verify-before-build tests: **test 9.K4** asserts auto-seed produces exactly one key across multiple nodes and that readiness moves from fail to pass; **test 9.K5** asserts DR restore-both works, and carries the negative case proving that a missing keyring is detected rather than silently regenerated.
* ASP.NET Core Data Protection (.NET 10) behavior verified: an empty ring at cold start activates a key immediately; `PersistKeysTo...` turns off at-rest encryption, so `ProtectKeysWith...` is mandatory; `ProtectKeysWithCertificate(X509)` is the multi-node-safe, framework-recommended choice; `SetApplicationName` must match on every node; a deployment-slot swap that does not share the ring causes a mass logout; deleting a key is unrecoverable.
* OpenIddict has no built-in self-seed, so Nami builds it.

## Pros and Cons of the Options

### First-key seeding

* **Auto-seed (chosen)**: good, because deploy and DR self-heal with no manual step; bad, because the app identity mints the first key (mitigated by a bootstrap audit event).
* **Explicit provisioning (CLI/init-job)**: good, because a human with dual-control mints the first key; bad, because DR is no longer self-healing and needs a manual step exactly when an operator is under pressure.
* **Hybrid**: good, because it could combine both; bad, because it adds branching complexity for little gain over auto-seed plus a bootstrap audit event.

### Keyring root protector

* **`ProtectKeysWithCertificate` (X.509) (chosen)**: good, because it is multi-node-safe and cloud-neutral (runs on-prem); bad, because the rootCert must be provisioned and rotated out-of-band.
* **Passphrase-derived**: good, because it is simple; bad, because passphrase management and rotation are weaker and error-prone across nodes.
* **DPAPI**: good, because it is zero-config on Windows; bad, because it is single-host Windows only and not multi-node-safe, so it cannot be the default.

## More Information

* Original decision: 2026-06-28, extending ADR-0006. The rootCert (the Data Protection protector) is provisioned out-of-band; its bytes never enter the repository or any unsanctioned destination.
* The auto-seed shape (lazy first-key creation at startup, a short initialization window plus a small synchronization delay instead of a distributed lock, and wrapping the key material) follows a common pattern also seen in mature commercial identity servers' automatic key management.
* Related decisions: ADR-0006 (DR and cloud-agnostic adapters), ADR-0011 (dynamic materialization via the #1434 seam).
* **Corrected 2026-08-01, two defects in the bootstrap sequence, both of which an implementer would have hit by following this ADR rather than the design.**
  1. *Step 1 named the wrong context.* It read `PersistKeysToDbContext<ControlPlaneDbContext>()`. That does not compile: the constraint is `where TContext : DbContext, IDataProtectionKeyContext`, read from the shipped `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.8 assembly metadata on 2026-08-01, and design 02 assigns the interface to `DataProtectionDbContext` alone. The severity is not the compile error, which is loud, but the obvious workaround: adding the `DbSet` to the named context compiles and silently relocates the keyring. That warning is now written into the step.
  2. *Step 4 specified the refuted probe.* It asked only for "a successful keyring `Unprotect` probe", while design 12 section 5.8 requires the active `kid` to match the expected persisted `kid` and states why a bare round trip cannot work: Data Protection regenerates a keyring it cannot load, so the round trip succeeds on the new key while every previously issued cookie and every DP-wrapped signing key is already undecryptable. The design layer was correct and this ADR contradicted it, which is the more dangerous direction, since an implementer opens the ADR for the bootstrap sequence.
  Both were found by reading this ADR against design 12 rather than by any mechanical check; nothing in the guardrail can see either.
* Imported into this repository and translated in 2026-07, then reconciled against the design corpus on 2026-07-25 to restore the test labels 9.K4 (multi-node auto-seed and the readiness transition) and 9.K5 (DR restore-both with its missing-keyring negative case). References to a specific commercial identity server's key management stay generalized, including its initialization-window and synchronization-delay values; ASP.NET Core Data Protection framework references are named.
