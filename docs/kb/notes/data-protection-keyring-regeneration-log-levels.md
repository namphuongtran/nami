---
title: What ASP.NET Data Protection actually logs when it regenerates a keyring
tags: [data-protection, keys, observability, disaster-recovery, dotnet]
created: 2026-08-01
related: [[0012-key-bootstrap-and-dr-sequence]], [[0006-disaster-recovery-key-material]], [[0031-twelve-factor-baseline]]
---

Three documents in this repository said that ASP.NET Core Data Protection logs a silent
keyring regeneration **only at Debug level**. That is wrong, and the way it is wrong
matters more than the level: the framework is loud about one of the two failure shapes and
completely silent about the other, and the silent one is the one the readiness gate exists
for. This note records the measurement so the claim is not re-derived from prose.

## What was measured

A console probe on **.NET SDK 10.0.301**, `Microsoft.AspNetCore.App` **10.0.9**, run
2026-08-01. It installs a capturing `ILoggerProvider` at `LogLevel.Trace` so that every
Data Protection line is recorded with its level, then runs three scenarios against a
file-system keyring protected by an X.509 certificate:

1. **empty ring, cold start** (the legitimate first boot)
2. **keyring rows present, protecting root replaced** (restore brought the ring but not the
   root that encrypts it)
3. **keyring deleted entirely** (restore brought the key store but not the ring)

## Result

```text
phase                           Trace  Debug   Info   Warn  Error
1-empty-ring-cold-start             1     12      2      0      0
2-keyring-present-root-lost        13     39      2      2     11
3-keyring-deleted-entirely          1     12      2      0      0
```

Lines at `Information` or above:

```text
---- 1-empty-ring-cold-start ----
   [Information] XmlKeyManager: Creating key {b2f434e1-...} with creation date ...
   [Information] FileSystemXmlRepository: Writing data to file '.../key-b2f434e1-....xml'.
---- 2-keyring-present-root-lost ----
   [Error      ] XmlKeyManager: An exception occurred while processing the key element
                 '<key id="b2f434e1-..." version="1" />'.          (x 11)
   [Warning    ] DefaultKeyResolver: Key {b2f434e1-...} is ineligible to be the default key
                 because its CreateEncryptor method failed after the maximum number of
                 retries.                                          (x 2)
   [Information] XmlKeyManager: Creating key {9163b3da-...} with creation date ...
   [Information] FileSystemXmlRepository: Writing data to file '.../key-9163b3da-....xml'.
---- 3-keyring-deleted-entirely ----
   [Information] XmlKeyManager: Creating key {2c47b44d-...} with creation date ...
   [Information] FileSystemXmlRepository: Writing data to file '.../key-2c47b44d-....xml'.
```

## The three things this settles

**Regeneration is announced at `Information`, never only at Debug.**
`XmlKeyManager: Creating key {kid} with creation date ..., activation date ..., and
expiration date ...` is `Information` in all three scenarios. An alert can key on it.

**Shape 2 is loud, not silent.** Eleven `Error` lines and two `Warning` lines precede the
regeneration. A separate observation from the same run: unprotecting an old payload
afterwards **throws** `CryptographicException`. The "an undecryptable key is skipped rather
than throwing" behaviour is real, but it belongs to the **key-ring load**, not to
per-payload unprotect. Conflating the two understated how much signal this shape gives.

**Shape 3 is indistinguishable from a legitimate cold start.** Identical level counts and
the same two `Information` lines. No log line anywhere says "a ring was supposed to be
here". This is the finding that justifies the design: a log alert cannot detect a lost
keyring, and neither can a protect-and-unprotect round trip, because both succeed against
the freshly generated key. Only comparing the active `kid` to an **expected persisted**
`kid` can, because only that carries the knowledge that a ring existed before
([[0012-key-bootstrap-and-dr-sequence]]).

## Scope, and what was not measured

The probe used `PersistKeysToFileSystem` with `ProtectKeysWithCertificate`. Nami persists to
`DataProtectionDbContext` via `PersistKeysToDbContext`. The levels above come from
`XmlKeyManager` and `DefaultKeyResolver`, which sit **above** the repository and are
therefore repository-independent; the one repository-specific line is
`FileSystemXmlRepository: Writing data to file`, which the EF Core repository replaces with
its own equivalent. So the `Creating key` line an alert keys on is portable, and the
"writing" line is not. Not measured: whether the EF Core repository adds or removes lines
of its own, and whether the retry count that produces eleven `Error` lines in shape 2 is
stable across versions. Alert on the presence of the pattern, not on the count.

## Reproducing it

A `Microsoft.NET.Sdk.Web` console project with `ImplicitUsings` on, a capturing
`ILoggerProvider` registered at `LogLevel.Trace`, and per scenario a fresh
`ServiceCollection` calling `AddDataProtection().SetApplicationName(...)
.PersistKeysToFileSystem(dir).ProtectKeysWithCertificate(cert)`. Generate two distinct
self-signed certificates with `CertificateRequest.CreateSelfSigned`, round-tripping each
through PKCS#12 so the private key is usable for unprotect. Scenario 2 is simply building
the second provider over the **same** directory with the **second** certificate.
