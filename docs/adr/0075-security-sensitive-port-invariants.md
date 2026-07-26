---
status: "accepted"
date: 2026-07-26
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OpenIddict's claim-destinations documentation (verified at documentation.openiddict.com on 2026-07-26); the four ports and their invariants as recorded in ADR-0008, ADR-0011, ADR-0033, ADR-0047 and the core-protocol detailed design; ADR-0024 (the ports/adapters rule the swap depends on), ADR-0044 (how a consumer-implemented port may evolve), ADR-0060 (the testing strategy this adds a category to), ADR-0043 and ADR-0052 (the existing construction-time and startup-time invariant checks this sits alongside)
informed: all contributors, via this repository
---

# Treat security-sensitive ports as carrying non-weakenable invariants, verified by a contract test the consumer runs

## Context and Problem Statement

Nami is built on ports and adapters (ADR-0024) and ships as NuGet packages, so a consumer can replace an adapter without touching Nami. That is a stated benefit: it is how the product stays cloud-agnostic and how an adopter fits it to an existing estate.

Most ports are neutral. A few are not. Four of them **hold a security property that is not visible in their signature**, so a replacement compiles, passes its own tests, and silently removes the property.

The clearest case is claim emission. OpenIddict does not copy a principal's claims into tokens on its own: its documentation states that "the OpenIddict server doesn't automatically copy the claims attached to a `ClaimsPrincipal` to access or identity tokens", `sub` excepted, and a claim is serialized only if `SetDestinations` declared one. Nami routes every issuance path through one choke point, `IClaimsProfileService.GetDestinations`, so this deny-by-default behaviour is enforced in one place rather than repeated. But **the engine's safe default protects Nami's own implementation, not a consumer's replacement of it.** An adapter that returns both destinations for every claim leaks personal data into a plain, readable access token (ADR-0005), and nothing detects it: not the compiler, because the signature is unchanged; not Nami's tests, because they exercise Nami's adapter; not the startup self-check (ADR-0043), because it runs inside Nami's deployment and cannot reason about a consumer's code.

The gap is narrower than "we should choose deny-by-default", which is not a decision Nami gets to make and which ADR-0052 already records as OpenIddict's posture. What has no owner is **whether a replacement is allowed to weaken the invariant, and how anyone would find out.**

The neighbouring decisions each cover an adjacent slice and none covers this one. ADR-0044 governs how a consumer-implemented port may **evolve**, treating an added interface member as a major change: it is about API shape, not behaviour. ADR-0060 establishes contract-regression tests, but those run in the other direction, asserting *OpenIddict's* behaviour on a pinned version rather than a *consumer's* behaviour against Nami's expectations. ADR-0043 checks hardening invariants at startup, and ADR-0052 makes an insecure client configuration impossible to construct; both are Nami checking Nami. Nothing addresses code Nami does not own.

## Decision Drivers

* An identity provider's security properties must not depend on an extension point being used carefully.
* The property is invisible in the type signature, so "document it and hope" is not a control.
* The check has to run where the adapter is, which is the consumer's build, not Nami's.
* Extensibility is a product goal; the answer cannot be to close the ports.
* A published invariant is also an adoption asset: it tells an adopter what they are guaranteed.

## Considered Options

* Leave the invariants in prose in the detailed designs, as today.
* Close the security-sensitive ports so no adapter can be substituted.
* Declare the invariants binding on any adapter and publish an executable contract test the consumer runs against their own adapter.
* Rely on the startup self-check (ADR-0043) to detect a weakened adapter at boot.

## Decision Outcome

Chosen: "declare the invariants binding and publish an executable contract test". Prose alone is not a control; closing the ports gives up an explicit product goal to solve a problem that testing solves; and a startup check cannot see the property, because these invariants are about what an adapter does with a request, not about how it was configured.

### A. The invariant register (binding)

Four ports are **security-sensitive**. Substituting an adapter for one of these is permitted; weakening its invariant is not. The invariant is part of the port's contract in the same way its signature is.

| Port | Invariant a replacement must preserve | Decision of record |
|---|---|---|
| `IClaimsProfileService` | **Claim destinations are deny-by-default.** A claim reaches a token only where a destination was explicitly declared for it. The adapter is the single choke point for every issuance path, so widening it widens every path at once | this ADR, with ADR-0005 for the minimal claim set the destinations carry |
| `IAuditSink` (and `ISecurityEventSink`) | **Tamper-evident and delivery-guaranteed.** Records are hash-chained and reach their destination at least once; the audit lane never degrades into the diagnostics lane, which has neither property | ADR-0008 |
| `ICheckAccess` | **Deny-by-default and consistency-carrying.** An unknown capability denies rather than allows, the decision is taken at the consistency level the call demands, and no access decision is cached behind the port | ADR-0047 |
| `ISigningKeyStore` | **Publish-before-sign, and scope-correct.** A key is present in the JWKS for validation before it ever signs, and `LoadAsync(scope, ct)` returns the records of exactly one key scope, never another tenant's | ADR-0011 for the rotation state machine, ADR-0033 for the scope argument |

Three properties of this register are deliberate. It is **closed**: adding a port to it is an amendment to this ADR, so the set cannot grow silently or, worse, shrink. Each row **cites the decision that owns the invariant** rather than restating it, so this ADR does not become a second source of truth for four subsystems. And `IClaimsProfileService` is the one row whose invariant this ADR itself owns, because no other decision states it: ADR-0005 owns which claims exist, which is a different question from which token each one rides in.

### B. Verification is a contract test the consumer runs (binding)

Nami publishes an **executable contract test per security-sensitive port**. A consumer supplying their own adapter runs it against that adapter and gets a pass or a failure, rather than a paragraph to read. The tests assert observable behaviour in the ADR-0060 style: given a claim with no declared destination, when a token is issued, then the claim is absent from it.

Two boundaries keep this honest.

* **The tests are not a security certification.** They check the stated invariant, not that an adapter is safe. A passing adapter can still be wrong in ways nobody enumerated, and the register above is the enumeration.
* **Running them is the consumer's act.** Nami cannot execute a test inside someone else's build, so this is a control that an adopter must choose to use. That is a real limit and is recorded as such rather than presented as enforcement.

Nami's own adapters are subject to the same tests, which is what keeps the invariants true of the defaults and stops the register drifting away from the code.

### C. What is deliberately left open

**How the tests ship is not decided here.** Whether they are one package or several, what it is called, and how it is versioned belong to packaging and to the public-API contract (ADR-0027, ADR-0044), and pinning them now would fix a mechanism before the first line of code exists. What is decided is that the tests exist, are published to consumers, and cover exactly the register in section A.

Equally, this ADR does not decide that every port gets a contract test. Only these four carry an invisible security property; the rest are ordinary seams where a compile error is a sufficient contract.

### D. Relationship to the checks that already exist

This is the third member of a set, and the three do not overlap.

* **Construction time (ADR-0052).** An insecure client configuration cannot be built. Nami's code, Nami's types.
* **Startup (ADR-0043).** A drifted deployment fails fast before serving. Nami's code, the operator's configuration.
* **Consumer build time (this ADR).** A replacement adapter that removes a security property fails a test. **Someone else's code**, which is the case the other two structurally cannot reach.

### Consequences

* Good, because the property that actually protects a token, an audit trail, an authorization decision, and a signing key stops depending on an extension point being used carefully.
* Good, because the invariants become an adoption asset: an adopter can read what they are guaranteed, and verify it, instead of trusting a claim.
* Good, because it closes the control behind the highest-rated information-disclosure row in the threat model, which until now named a rule that no decision owned.
* Good, because the register is closed and each row cites its owning decision, so the security surface of the extension points is enumerable rather than folklore.
* Bad, because the check runs in the consumer's build and Nami cannot compel it; mitigated by publishing the tests, by documenting the register, and by holding Nami's own adapters to it, but the residual is real and is not called enforcement.
* Bad, because it adds a maintenance obligation: the tests move with the ports, and a change to an invariant is now a change to a published contract under ADR-0044. That cost is the point, since an invariant nobody maintains is an invariant nobody has.
* Bad, because a passing test can read as a safety guarantee it does not give; mitigated by stating the boundary in section B and repeating it in the consumer documentation.

## Pros and Cons of the Options

### Prose in the detailed designs (today)

* Good, because it costs nothing and the properties are at least written down somewhere.
* Bad, because it is not a control: the invariant is invisible in the signature, so nothing fails when it is removed. This is exactly how the gap arose, with `IClaimsProfileService` stated only in a design document and cited to an ADR that owns a different rule.

### Close the ports

* Good, because the invariant cannot be weakened if the adapter cannot be replaced.
* Bad, because swappable infrastructure is an explicit goal of ADR-0024 and of the cloud-agnostic posture, and giving it up to solve a testing problem is disproportionate.

### Declare the invariants and publish contract tests (chosen)

* Good, because it puts the check where the adapter is, states the guarantee an adopter gets, and reuses the behaviour-first convention already adopted in ADR-0060.
* Bad, because Nami cannot force the test to run; accepted, and recorded as a residual rather than described as enforcement.

### Rely on the startup self-check

* Good, because it needs no new mechanism.
* Bad, because it cannot work: ADR-0043 inspects configuration and wiring at boot, and these invariants are behavioural, only observable by exercising the adapter with a request. A startup check would at best confirm that some implementation is registered.

## More Information

* **Why this ADR exists.** The deny-by-default claim-destination rule was found asserted in the architecture layer, the threat model, and the core-protocol design while no ADR contained it, during the ADR-candidate round on 2026-07-26. It was the first of eight such claims enumerated in the architecture layer's decisions index. Investigating it showed the rule itself needs no decision, since it is OpenIddict's documented behaviour and ADR-0052 already records that posture; what needed one was the replaceability of the port that enforces it. The other three ports were then found to have the same shape, which is why this is one decision with four rows rather than four decisions.
* **External verification, 2026-07-26.** OpenIddict's claim-destinations documentation states that the server does not automatically copy a principal's claims into access or identity tokens, that `sub` is the only mandatory claim, and that `principal.SetDestinations()` is what makes a claim serializable. The deny-by-default property is therefore the engine's, and Nami's contribution is the single choke point and this invariant.
* Related decisions: ADR-0024 (ports and adapters, and the rule that a port needs a real reason to exist), ADR-0005 (the minimal claim set, which is claim minimisation rather than destination), ADR-0008 (the audit invariants), ADR-0047 (the authorization invariants), ADR-0011 and ADR-0033 (the key-store invariants), ADR-0043 (the startup self-check, the second member of the set in section D), ADR-0052 (the fail-closed mapper, the first member, and the ADR that already records OpenIddict's deny-by-default posture), ADR-0060 (the behaviour-first testing convention these tests follow), ADR-0044 (how a consumer-implemented port evolves, which now governs changes to a published invariant), ADR-0027 (packaging, where the shipping question in section C lands), ADR-0026 (any test library used must be permissive OSS).
* Authored fresh for this repository. The design corpus identified the same four ports and the same non-weakenable framing in its productization document and proposed a shipped conformance-test kit; that framing is adopted here, while the specific package name and shape it sketched are left open per section C.
