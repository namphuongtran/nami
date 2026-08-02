---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the testing decisions already scattered across ADR-0025 (Testcontainers, end-to-end, CI), ADR-0024 (architecture tests), ADR-0021 and ADR-0030 (contract-regression per bump), ADR-0041 (the load-test SLO gate and canary), and the CONTRIBUTING test-first rule
informed: all contributors, via this repository
---

# Consolidate the testing strategy and adopt behavior-first tests as living documentation

## Context and Problem Statement

Nami's testing decisions are real but scattered. ADR-0025 settles Testcontainers integration tests, `WebApplicationFactory` end-to-end tests, and Playwright UI tests; ADR-0024 adds the ArchUnitNET architecture-test suite; ADR-0021 and ADR-0030 add per-bump contract-regression tests; ADR-0041 makes a load test an enforced SLO gate with an external synthetic canary; and CONTRIBUTING says protocol and security code is test-first. No single ADR states the testing strategy as a whole, and one thing is recorded nowhere: how a test should be written so it stays useful. Nothing says a test must describe observable behavior rather than implementation.

Without a consolidated record, a contributor cannot find "how does Nami test" in one place, and without a style convention, tests drift toward asserting internal structure, which makes them brittle and useless as documentation. This ADR consolidates the test taxonomy that other ADRs already decided (citing each rather than restating it) and adds the binding, net-new convention: tests are behavior-first and read as living documentation.

## Decision Drivers

* One findable place that states the whole testing strategy, so a new feature knows which suites it must satisfy.
* Tests that survive refactoring: asserting behavior, not implementation, so a passing suite means the requirement still holds.
* Tests that double as documentation of the requirement, readable by someone who does not know the code.
* Reuse, not reinvention: the test types are already decided; this ADR names them as one strategy, it does not re-decide them.

## Considered Options

* Leave the testing decisions scattered across the individual ADRs and CONTRIBUTING.
* Consolidate the test taxonomy into one ADR, but add no style convention.
* Consolidate the taxonomy and adopt the behavior-first / Given-When-Then convention as binding.

## Decision Outcome

Chosen option: "consolidate the taxonomy and adopt behavior-first tests", because the strategy needs a home and the style convention is the genuinely missing decision. All test libraries named below are already committed by the cited ADRs and are permissive-licensed (ADR-0026); this ADR pins nothing new.

### The test taxonomy (binding; each type's owner in parentheses)

* **Unit tests** are fast and need no container; they cover domain logic and handlers in isolation, with xUnit (ADR-0025).
* **Integration tests** run against Testcontainers PostgreSQL 18 through `WebApplicationFactory<Program>`, exercising the real pipeline (multi-tenant filter, row-level security, applied migrations), with Redis Testcontainers when a test touches the backplane or replay. SQLite is never substituted, because row-level security, `xmin` concurrency, and `uuidv7()` are PostgreSQL-specific (ADR-0025).
* **End-to-end tests** use xUnit plus `WebApplicationFactory` plus Testcontainers for the protocol path (issuance, validation, revocation, introspection, and a multi-tenant isolation negative test), and Playwright for the admin UI (ADR-0025).
* **Architecture tests** (`Nami.Identity.ArchitectureTests`, ArchUnitNET) enforce the dependency rule and slice decoupling in CI (ADR-0024).
* **Contract-regression tests** assert each OpenIddict seam's behavior on the pinned version and run on every OpenIddict and .NET bump, failing the build on a broken contract (ADR-0021, ADR-0030).
* **Load and soak tests** (Apache JMeter per ADR-0078, with any .NET-side gate hand-written rather than taken as a library) prove the NFR targets on percentiles (p95/p99), and the SLO is a formal CI gate that fails the build on breach, complemented by an external synthetic canary through the public path (ADR-0041).
* **Conformance** (OpenID certification) is run within the per-bump migration playbook (ADR-0021) and is a roadmap milestone.

### No third-party assertion library: xUnit's own assertions are the whole set (binding, added 2026-08-02)

Nami takes **no** fluent-assertion package. Tests assert with what `xunit.v3.assert` ships. This closes an item four documents carried as "an assertion library is picked at M1", and it is a decision not to add a dependency rather than a deferral of which one to add.

* **The two capability-shaped reasons are already in the pinned framework, read at source.** Structural object-graph comparison and reporting several failures from one test are what a fluent library is usually taken for, and the public symbols `Equivalent` and `Multiple` are both present in the shipped `xunit.v3.assert` **3.2.2** assembly in the local package cache, which is the version design [20](../design/20-testing.md) section 7 records reading. What remains is ergonomics: expressive chains and nicer failure text, which are real and are not worth a dependency on their own. (Method **names** were read, not signatures, and the extraction proves presence only: `Equal` did not appear in it and certainly exists, so nothing here is an absence claim.)
* **The decisive cost is downstream, not here.** The productization design's conformance test kit ships abstract contract tests for each port so a consumer can inherit them and run them against their own adapter. Assertions written in those base classes become a **transitive dependency of every adopter**, which makes an internal convenience into an imposition on other people's projects. ADR-0044 parameter D treats a consumer-implemented surface as the most dangerous one for exactly this reason, and a shipped test kit has that property on the test side.
* **The lineage has already cost this project once.** The obvious candidate is a community fork created *because* the original relicensed commercially at its version 8, and the original is on ADR-0026's package-name deny-list as a result. The fork's README promises the licence "will never change, not even to MIT", and [`DEPENDENCY-LICENSES.md`](../DEPENDENCY-LICENSES.md) section 5 already records the right weight for that: a stated intent is not an assurance.
* **This is not a permanent exclusion, and the evidence to reverse it is kept.** Two candidates stay verified at source with dates and read-locations in that same section, both inside ADR-0026 section A's permissive set. Adopting one later costs a stack-of-record row and a re-verify, not a research task. Reverse this if a concrete readability problem appears in the real test projects, and if it does, the constraint to carry is that a shipped test kit must not expose the choice to consumers.

### Test-first for protocol and security code (binding)

Protocol and security code is written test-first: the failing behavior test comes before the implementation. This elevates the CONTRIBUTING rule into the decision record. Security-relevant negative tests are first-class, not optional: a client-set client-certificate header must be rejected and never treated as mTLS-authenticated (ADR-0025), and a token issued for one tenant must fail validation when presented to another tenant's resource on the issuer and tenant binding (ADR-0049).

### Behavior-first tests as living documentation (binding, net-new)

* A test asserts **observable behavior**, never implementation detail: it exercises a public entry point and asserts an observable outcome, and does not assert private internals, call counts, or structure. A test that breaks on a refactor that preserved behavior is a defect in the test.
* Tests are named and structured as **scenarios**, in Given / When / Then form, so the suite reads as documentation of the requirements. Nami-real examples:
  * *Given* a proposal created by one admin, *when* a second admin approves it with step-up MFA, *then* the action executes and no token is exposed to the browser (ADR-0020, ADR-0025).
  * *Given* an access token issued to a client, *when* the client revokes it, *then* introspection reports it inactive on every node within the freshness bound (ADR-0039, ADR-0048).
  * *Given* a token issued for tenant A, *when* it is presented to tenant B's resource, *then* validation fails on the issuer and tenant binding (ADR-0049).
* This convention is a direct application of ADR-0058 (Separation of Concerns): a behavior test depends on the observable contract, not the internals, exactly as the dependency rule keeps callers off internals.

### CI composition and confirmation

CI runs unit, integration (Docker-in-Docker for Testcontainers), end-to-end, architecture, and contract-regression as build jobs, with the load-test SLO as a separate gating job (ADR-0025, ADR-0041). Build-time confirmation: when the test projects land (from M1), confirm this taxonomy against the real suites and adjust the naming/structure guidance to what the code shows, the same build-time-confirmation posture as ADR-0024.

### Consequences

* Good, because "how does Nami test" is answerable from one ADR, and every new feature knows which suites it must satisfy.
* Good, because behavior-first tests survive refactoring and document the requirement, so a green suite is meaningful and a new contributor can read the tests as scenarios.
* Good, because it consolidates without re-deciding: each test type still lives in its owning ADR, so there is no duplicated or conflicting policy.
* Bad, because "behavior, not implementation" is a judgment call that some genuinely white-box tests (a hash-chain link, a handler order) strain; mitigated by treating those as the deliberate exception and keeping the default behavior-first.
* Bad, because one more consolidating ADR must be kept aligned with its sources; mitigated by citing them rather than restating their content.

## Pros and Cons of the Options

### Leave testing decisions scattered

* Good, because it needs no work and each decision already lives in its own ADR.
* Bad, because there is no findable whole-strategy view and no recorded style convention, so tests drift toward brittle implementation assertions.

### Consolidate the taxonomy only, no style convention

* Good, because it gives the single findable view.
* Bad, because it leaves the actually-missing decision (how a test is written) unrecorded.

### Consolidate the taxonomy and adopt behavior-first tests (chosen)

* Good, because it both makes the strategy findable and records the net-new convention, and it grounds every claim in an existing decision or a Nami-real scenario.
* Bad, because the behavior-first rule needs judgment and one more document to keep aligned; both are mitigated as above.

## More Information

* Related decisions: ADR-0025 (Testcontainers, end-to-end, Playwright, and the CI composition), ADR-0024 (the ArchUnitNET architecture tests and the vertical slices under test), ADR-0021 and ADR-0030 (per-bump contract-regression), ADR-0041 (the load-test SLO gate and external canary), ADR-0049 (the tenant-isolation negative test), ADR-0039 and ADR-0048 (the revocation-freshness behavior), ADR-0020 (the dual-control scenario), ADR-0026 (all test libraries are permissive OSS), and ADR-0058 (behavior-first tests as an application of Separation of Concerns).
* Build-time follow-up: confirm the taxonomy and refine the behavior-first guidance against the real test projects at M1.
* **The assertion-library question was closed on 2026-08-02 by measuring the framework before shopping for a package.** Four documents had carried it as an open M1 pick framed as "which permissive library", and the licence work had already been done for two candidates, so the pick looked like the only remaining step. Reading `xunit.v3.assert` 3.2.2 in the cache showed the two capabilities the pick was for were already there, which turned a choice-of-package question into a do-we-need-one question with a different answer. Worth recording as a shape: a deferral phrased as "pick one of these" quietly assumes the thing is needed.
* Authored fresh for this repository (not imported from the design corpus). The Order-to-Shipment event-choreography illustration common to this material is deliberately not used, because event-driven choreography is forbidden as a backbone in Nami (ADR-0020); the Given/When/Then examples are Nami's own behaviors instead.
* **Corrected 2026-08-01: NBomber was removed from this ADR.** Its license is the NBomber License Agreement v3.0, which states that NBomber "is not free for organizational use" and requires "a valid Commercial Subscription" for any organizational use (verified in the `LICENSE` file shipped inside the NBomber 6.5.0 package). It therefore falls under the ADR-0026 section A ban on commercial or paid-tier packages, which also means the claim above that all test libraries are permissive OSS was false while NBomber was named. No taxonomy row and no testing decision changes; a .NET-side load gate is a hand-written xUnit concurrency test rather than a library. The load tool itself was then also replaced: k6 is AGPL-3.0 and equally banned by section A, so the tool is now Apache JMeter (ADR-0078), which is where the full licence evidence for all candidates is recorded.
