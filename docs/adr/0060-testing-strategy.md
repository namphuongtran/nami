---
status: "accepted"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the testing decisions already scattered across ADR-0025 (Testcontainers, end-to-end, CI), ADR-0024 (architecture tests), ADR-0021 and ADR-0030 (contract-regression per bump), ADR-0041 (the load-test SLO gate and canary), and the CONTRIBUTING test-first rule
informed: all contributors, via this repository
---

# Consolidate the testing strategy and adopt behavior-first tests as living documentation

## Context and Problem Statement

Nami's testing decisions are real but scattered. ADR-0025 settles Testcontainers integration tests, `WebApplicationFactory` end-to-end tests, and Playwright UI tests; ADR-0024 adds the ArchUnitNET architecture-test suite; ADR-0021 and ADR-0030 add per-bump contract-regression tests; ADR-0041 makes a k6/NBomber load test an enforced SLO gate with an external synthetic canary; and CONTRIBUTING says protocol and security code is test-first. No single ADR states the testing strategy as a whole, and one thing is recorded nowhere: how a test should be written so it stays useful. Nothing says a test must describe observable behavior rather than implementation.

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
* **Load and soak tests** (k6 or NBomber) prove the NFR targets on percentiles (p95/p99), and the SLO is a formal CI gate that fails the build on breach, complemented by an external synthetic canary through the public path (ADR-0041).
* **Conformance** (OpenID certification) is run within the per-bump migration playbook (ADR-0021) and is a roadmap milestone.

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
* Authored fresh for this repository (not imported from the design corpus). The Order-to-Shipment event-choreography illustration common to this material is deliberately not used, because event-driven choreography is forbidden as a backbone in Nami (ADR-0020); the Given/When/Then examples are Nami's own behaviors instead.
