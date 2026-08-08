---
status: "accepted"
date: 2026-08-08
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Microsoft Learn on the options pattern, the `required` modifier, and `ValidateOnStart` (read 2026-08-08 at the .NET 10 view); `dotnet/runtime` at tag `v10.0.0` for `OptionsFactory<TOptions>`; ADR-0027 (which owed this surface), ADR-0044 (which versions it), ADR-0065 (the configuration-key form), ADR-0031 (the externalization posture), and design 01 and design 04 (the two documents that name these members)
informed: all contributors, via this repository
---

# Fix the fluent-builder API surface, binding `NamiIdentityOptions` from configuration before the delegate runs, keeping `INamiIdentityBuilder` to a service-collection carrier, and failing at start-up rather than marking the two required options `required`

## Context and Problem Statement

`Nami.Identity.Core` cannot be written. Its two entry types are named everywhere and defined nowhere.

`design/01-foundations.md:110` lists `AddNamiIdentity()`, `INamiIdentityBuilder`, and
`NamiIdentityOptions` as the package's key public surface. `design/01-foundations.md:305` gives the
signature `AddNamiIdentity(Action<NamiIdentityOptions>)`. Section 3.4 of the same document then
gives a table of ten option names, their defaults, and their owning ADRs.

That table fixes what each option **means**. It fixes no C# type, no nullability, and no accessor.
A document can leave those out and still read as complete. C# cannot: writing the type forces a
decision on every one of them. So the gap is not a detail to fill in while coding. It is a
decision, and `ADR-0027:83` already says so by listing "the fluent-builder API surface" among its
own build-time follow-ups.

`INamiIdentityBuilder` is worse. Counted 2026-08-08 with `git grep -c "IServiceCollection"` over
`docs/` excluding the work queue: **zero**. No source states any member of it.

Three further problems surfaced while reading, and each has to be answered before a type can be
written.

**The same three settings have two public surfaces.** `design/04-core-protocol.md:810-812` states
that keys follow `Nami:Section:Key` and that "**These names are set by this design**, so this
section is their origin". It then names `Nami:Protocol:AccessTokenLifetime`,
`Nami:Protocol:RefreshTokenLifetime`, and `Nami:Protocol:SigningAlgorithm`. Those are three of the
members `design/01` section 3.4 lists on `NamiIdentityOptions`. ADR-0044 parameter I makes a
configuration key a stable public contract, and ADR-0065:78 fixes its form, so both surfaces are
versioned and neither can be treated as informal. No source says how they relate.

**No decision says how a missing required value fails.** `design/01-foundations.md:438-439` states
the requirement: "Required values bind with validation on start, so a missing value crashes the
host at boot rather than surfacing lazily on the first request that needs it." It names no
mechanism. `design/04-core-protocol.md:811` attributes boot validation of the protocol keys to ADR-0052, and
ADR-0052 does not carry that claim: its five parameters A through E are entirely about client and
scope declaration, and its subject is `ClientDefinition` and `ScopeDefinition`. ADR-0043 is the
nearest live mechanism and is a different thing: it asserts a fixed list of named **security
invariants** and does not check whether a required configuration value is present.

**An eleventh option exists outside the table.** `ADR-0032:37` puts `RegistrationKey` on this type,
configured as `.AddNamiIdentity(o => o.RegistrationKey = ...)`. `design/01` section 3.4 has no row
for it.

## Decision Drivers

* A type cannot be written until every member has a type, a nullability, and an accessor.
* Both surfaces, the C# member and the configuration key, are versioned under ADR-0044, so silence
  about their relationship is expensive rather than neutral.
* ADR-0031 puts per-deploy values in the environment, so the answer must not quietly make an
  operator override impossible without saying so.
* The repository already has precedent for these choices, in `src/CLAUDE.md`, and a second answer
  to a settled question is drift.
* An absence must be recorded as an absence, not filled from judgement.

## Considered Options

* Transcribe the design 01 table and choose each type while writing the code
* Fix the surface here, and assign a configuration key to every member
* Fix the surface here, and leave key assignment to the design that owns each member's subject

## Decision Outcome

Chosen option: "Fix the surface here, and leave key assignment to the design that owns each
member's subject". Transcription is not available, because the table states no types. Assigning a
key to all twelve properties would mint eleven new public contracts in one change, each of them
MAJOR-locked by ADR-0044 parameter I, for members whose subjects belong to other designs.

* **A. `NamiIdentityOptions` is a mutable POCO with a public parameterless constructor.** Every
  member uses `set` rather than `init`, following `ScopeDefinition` and `ClientDefinition` rather
  than the audit DTOs, because a configuration binder writes this type (`src/CLAUDE.md`, the
  five choices recorded for `ClientDefinition`).

* **B. The two required options are nullable, and neither carries the `required` modifier.** This
  reverses what the word "required" in the design table invites, and the reason is mechanical.
  Microsoft Learn states that `required` "indicates that the field or property it applies to must
  be initialized by an object initializer" and that "any expression that initializes a new instance
  of the type must initialize all required members" (read 2026-08-08). The options system does not
  write such an expression. Read at source on 2026-08-08 in `dotnet/runtime` at tag `v10.0.0`,
  `src/libraries/Microsoft.Extensions.Options/src/OptionsFactory.cs` constrains its type parameter
  as `where TOptions : class` at line 16, with **no** `new()` constraint, and creates the instance
  at line 102 with `Activator.CreateInstance<TOptions>()`. So `required` would bind call sites
  inside a consumer's own code and place no obligation on the path that actually populates this
  type.

  **This parameter deliberately does not answer the open question at `src/CLAUDE.md:36-38`**,
  which asks whether the options binder populates `required` members. It routes around it. The
  answer still needs the configuration packages, and nothing here establishes it.

* **C. A missing required value fails at start-up, through `IValidateOptions<NamiIdentityOptions>`
  registered by `AddNamiIdentity` and enforced by `ValidateOnStart()`.** This is what makes
  parameter B safe: the guarantee `design/01:438-439` asks for moves from a compile-time modifier
  to a start-up check, which is where that document already puts it. Microsoft Learn documents
  `ValidateOnStart` as running options validation when the application starts rather than on first
  access (read 2026-08-08 at the ASP.NET Core 10 view). The validator rejects a null or
  whitespace-only `ConnectionString` or `Issuer`.

* **D. A member and a configuration key that name the same setting are one setting with two
  spellings.** They are not two settings, and a consumer may use either.

* **E. Configuration binds first, and the delegate runs after it and wins.** `AddNamiIdentity`
  binds the owned configuration section onto the options instance, then invokes the
  `Action<NamiIdentityOptions>` the caller supplied.

  **The consequence is stated rather than left to be discovered: a value hard-coded in the delegate
  cannot be overridden by an environment variable.** That is a way to defeat ADR-0031's
  externalization posture, and Nami cannot detect it. It is accepted because the alternative is
  worse: if configuration won, the sample at `design/01:352-356`, which passes
  `cfg.GetConnectionString("Identity")` into the delegate, would be writing a value that something
  later overwrites. That sample is the documented way to start, so the order has to make it
  correct.

* **F. The configuration key for a member is assigned by the design that owns that member's
  subject, and this ADR assigns none.** Design 04 has assigned three, and they stand. A member with
  no key assigned today is settable in code only, until a design assigns one. This keeps eleven
  new public contracts from being minted here, and it puts each future key next to the decision it
  serves.

* **G. `INamiIdentityBuilder` exposes the service collection and nothing else.** Every `.Add…` and
  `.Use…` call in the catalogue is shipped by a package that depends on `Core`
  (`design/01-foundations.md:98`), so none of them can be a member of an interface `Core` declares.
  They are extension methods on this interface, declared in their owning packages. The interface
  therefore needs only what an extension method needs, which is somewhere to register into.

  Keeping it to one member is the point rather than a minimum: adding a module later then adds no
  member to Nami's own surface, so ADR-0044 parameter B never has to classify it.

* **H. The member table.** Twelve properties. The `Source` column says where the default was read,
  and an empty owning ADR is recorded rather than invented.

  | Member | Type | Default | Default read at |
  |---|---|---|---|
  | `ConnectionString` | `string?` | none | `design/01:316`, required |
  | `Issuer` | `string?` | none | `design/01:317`, required |
  | `SigningAlgorithm` | `SigningAlgorithm` | `RS256` | `ADR-0005:39` |
  | `AccessTokenLifetime` | `TimeSpan` | 15 minutes | `ADR-0004:76` |
  | `RefreshTokenLifetime` | `TimeSpan` | 8 hours | `ADR-0004:76` |
  | `SessionInactivity` | `TimeSpan` | 1 hour | `ADR-0003:40` |
  | `SessionAbsolute` | `TimeSpan` | 8 hours | `ADR-0003:40` |
  | `AccessTokenEncryption` | `bool` | `false` | `ADR-0005:36` |
  | `RequireHttps` | `bool` | `true` | `design/01:323` only |
  | `AutoSeedFirstKey` | `bool` | `true` | `design/01:324` |
  | `MigrateOnStartup` | `bool` | `false` | `design/01:325` |
  | `RegistrationKey` | `string?` | `null` | `ADR-0032:37` |

  **`TimeSpan` is read rather than chosen.** `design/04-core-protocol.md:88-89` writes
  `o.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))` and
  `o.SetRefreshTokenLifetime(TimeSpan.FromHours(8))`, so the values these members carry reach the
  engine as `TimeSpan`. The two session members follow it for consistency, and that half is a
  choice.

* **I. `SigningAlgorithm` is an enum, not a string.** This repository has already recorded the
  string form of a closed domain as a defect: `AccessTokenType` is a two-value domain typed as
  `string` with no invariant checking it, and an unrecognized value reads as the weaker option.
  The same shape here would let an unrecognized algorithm string fall back silently. The enum
  carries **explicit ordinals**, and its default is expressed as an initializer rather than by
  sitting at ordinal 0, which is the correction `ClientAuthMethod` already needed
  (`src/CLAUDE.md`). Members are named `RS256` and `ES256`, matching the wire values, under the
  Microsoft convention that a two-letter acronym keeps both letters uppercase.

### Consequences

* Good, because `Nami.Identity.Core` becomes writable, which is what blocked it.
* Good, because a missing connection string or issuer stops the host at boot, which is what
  `design/01:438-439` requires, through a mechanism that is now named.
* Good, because the two public surfaces are reconciled without minting eleven contracts, and each
  future key lands beside the decision it serves.
* Bad, because a consumer who hard-codes a per-deploy value in the delegate defeats ADR-0031, and
  no gate here can see it. Parameter E states this rather than hiding it.
* Bad, because parameter F leaves nine members with no configuration key, so an operator cannot set
  them without a code change until their owning designs assign keys.
* Neutral, because `required` is not used, so the `PublicAPI` blind spot recorded in
  `src/CLAUDE.md` (the analyzer asks for `Name.set -> void` either way) does not apply to this type.

### Confirmation

* Build-time: `NamiIdentityOptions`, `INamiIdentityBuilder`, the validator, and `AddNamiIdentity`
  land in `Nami.Identity.Core`, with one unit fact per default, because the unit suite is the only
  gate that sees a changed default (`tests/CLAUDE.md`, measured 2026-08-08).
* **Two defaults cannot carry an initializer**, and this is measured behaviour rather than a
  preference. `AccessTokenEncryption` and `MigrateOnStartup` both default to `false`, and writing
  `= false` trips `CA1805`, which `ADR-0093` makes an error. So the initializer is absent and the
  default comes from the language. The unit fact still sees the value, and deleting an initializer
  that is not there is not a possible break, so no hole is opened. `RequireHttps` and
  `AutoSeedFirstKey` do carry `= true`.
* **Not verified**: whether an `IApplicationBuilder` counterpart exists. `ADR-0076:50` and
  `ADR-0091:115` both say middleware "is registered by the fluent builder", and
  `design/01-foundations.md:390` describes an ordered middleware pipeline. `AddNamiIdentity`
  registers services and cannot register middleware. This is an absence claim, so it carries its
  search: counted 2026-08-08 over `docs/` excluding the work queue, `UseNamiIdentity` returned
  zero files, and no source names any method that would place this pipeline. Resolve it with the
  middleware, not here.
* **Not verified**: whether `Activator.CreateInstance<TOptions>()` and the configuration binder
  populate a member the binder has no key for. Parameter F leaves nine such members, and nothing
  here establishes what the binder does with them, because the configuration packages are not
  referenced yet.

## Pros and Cons of the Options

### Transcribe the design 01 table and choose each type while writing the code

* Good, because it needs no decision record and lands sooner.
* Bad, because the table states no types, so every choice would be invented at the keyboard and
  would then wear the design's authority. `src/CLAUDE.md` records that exact failure mode as the
  reason `ISecretResolver` could not be written at all.
* Bad, because the two-surface question and the validation question would stay unanswered while
  code depended on some answer to both.

### Fix the surface here, and assign a configuration key to every member

* Good, because every option becomes settable by an operator, which suits ADR-0031.
* Good, because one `Bind` call would cover the whole type.
* Bad, because it mints eleven public contracts in one change, each MAJOR-locked under ADR-0044
  parameter I, for subjects this ADR does not own.
* Bad, because it would compete with `design/04-core-protocol.md:810-812`, which claims origin for
  three of the names, and an ADR overruling a design's own naming needs a reason better than
  convenience.

### Fix the surface here, and leave key assignment to the design that owns each member's subject (chosen)

* Good, because it answers what blocks the code and nothing more.
* Good, because it respects the origin design 04 claims and gives later keys a rule to follow.
* Bad, because nine members are code-only until their designs act, which is stated as a
  consequence rather than left to be found.

## More Information

* This closes the "fluent-builder API surface" item that `ADR-0027:83` lists among its build-time
  follow-ups. It does not close the other items in that list.
* Related decisions: ADR-0027 (the meta-package and the builder this surface belongs to), ADR-0044
  (the public surface and the configuration key as versioned contracts), ADR-0065 (the
  configuration-key form and the naming conventions), ADR-0031 (per-deploy values from the
  environment), ADR-0003, ADR-0004, ADR-0005, ADR-0012, ADR-0032 and ADR-0049 (the individual
  defaults), ADR-0043 (the start-up self-check, which asserts security invariants and is not this),
  ADR-0052 (the client and scope declaration layer, which design 04 cites for boot validation and
  which does not carry it), ADR-0093 (the warning gate that makes `CA1805` an error), and ADR-0024
  (the dependency rule that makes every module registration an extension method).
* **This ADR picks no technology and carries no `stack-record` marker.** `Microsoft.Extensions.Options`
  arrives with the framework rather than as a chosen dependency, and ADR-0061's table records
  choices between technologies.
* **One citation is flagged and not repaired here.** `design/04-core-protocol.md:811` says the
  `Nami:Protocol:*` keys "are validated at boot (ADR-0052)". ADR-0052's subject is client and scope
  declaration and it carries no such clause. Correcting a design is that design's own change, and
  the finding is recorded in the work queue instead. Parameter C decides the mechanism for
  `NamiIdentityOptions` only, and says nothing about the protocol keys design 04 owns.
* **The external design corpus was read for this decision and did not close it.** Its root
  productization document carries the same ten-row table with the same defaults and states no C#
  type or nullability either, so the gap this ADR fills was inherited rather than introduced. Its
  identifiers are external provenance and are deliberately not cited here.
