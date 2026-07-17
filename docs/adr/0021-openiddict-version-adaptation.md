---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; the public OpenIddict roadmap (verified 2026-07-04) and the source-verification V-files
informed: all contributors, via this repository
---

# Adapt to OpenIddict version upgrades with seam isolation, per-bump contract-regression tests, and a migration playbook

## Context and Problem Statement

Nami pins OpenIddict 7.5.0 and relies on three different stability tiers of it:

1. **Native, documented, versioned API** — low risk on a bump.
2. **Undocumented but maintainer-endorsed seams** — high risk, and they can break silently on a bump. Examples: the custom `IOptionsMonitor<OpenIddictServerOptions>` for no-restart rotation (issue #1434, ADR-0011); inserting event handlers by `SetOrder` around the built-ins for DPoP; the internal `ValidateProofOfPossession` throwing `ID2196`; and the Finbuckle-times-OpenIddict `OnModelCreating`/`SaveChanges` composition.
3. **Build-interim implementations** for features OpenIddict will ship natively later: back-channel logout (native in 8.0, ADR-0019 interim), DCR (native in 8.0, re-targeted from 7.6, ADR-0014 wait), and DPoP (built on both sides, with no committed native, so owned permanently).

When OpenIddict ships 7.6, 8.0, or a DPoP-native release, the upgrade must not become a mass rewrite, and an undocumented seam must not break silently in production. The "migrate to native when it ships" promises are currently scattered across ADR-0011, ADR-0014, and ADR-0019, so a single unified adaptation mechanism is needed.

## Decision Drivers

* A version bump must be a bounded, tested event, never a mass rewrite.
* Undocumented seams must fail CI before production, not silently in it.
* Interim builds must swap to native cleanly, as an adapter change rather than a caller change.
* The scattered "migrate when native" promises must be consolidated in one place.

## Considered Options

* Float the version and fix breakage as it appears
* Pin 7.5.0 forever and never upgrade
* Pin plus a disciplined adaptation mechanism (seam isolation, per-bump contract-regression tests, and a migration playbook)

## Decision Outcome

Chosen option: "Pin plus a disciplined adaptation mechanism", implemented as five parts.

* **A. Version pin plus controlled bump.** Pin OpenIddict exactly through Central Package Management (all sub-packages aligned). A bump is a deliberate, tested event, not floating, and follows the playbook in part D.
* **B. Seam catalogue plus isolation.** Maintain an OpenIddict seam catalogue (a deliverable design document) listing every dependency on OpenIddict (S1–S34), each tagged with a risk tier (native, endorsed-undocumented, internal-behavior, handler-order, build-interim, adjacent-stack) and pointing to a source-verify file, a contract test, an isolation port, and a decommission marker. Each build-interim is isolated behind Nami's own port so swapping to native changes an adapter, not a caller: DPoP behind a handler interface, back-channel logout behind a logout-fanout service (ADR-0019), rotation behind `ISigningKeyStore` plus the custom `IOptionsMonitor` (ADR-0011), and interim DCR (if built) behind admin provisioning. This matches ports/adapters (ADR-0006/0009).
* **C. Contract-regression test suite** (the core safety net for undocumented seams). A dedicated suite asserts each seam's behavior on the pinned version and runs on **every** bump (7.5 to 7.6 to 8.0). It extends what already exists: the options-monitor rotation contract (ADR-0011), the DPoP handler-order and `ID2196`-avoidance checks, the Finbuckle composition test, and the source-read assumptions captured in the V-files (for example that `AttachSecurityCredentials` uses `First()`, that `AttachSigningKeys` iterates without a `NotBefore` filter, that family-revoke calls `RevokeByAuthorizationIdAsync` inside `ValidateTokenEntry`, the introspection `ValidateAuthorizedParty` behavior, and the pass-through versus fully-handled endpoint set). A bump that breaks a contract fails the build, so it is known before production; the suite is wired into CI.
* **D. Per-release migration playbook.** For each OpenIddict release: read the release notes; run the contract-regression suite plus conformance; for a feature that has just become native (DCR and back-channel logout are both at 8.0), evaluate swapping interim-to-native behind the port (a small blast radius), keeping the interim until native is proven; update the pin; and decommission the interim. Note the 8.0 breaking changes in advance — an options type will no longer inherit the authentication-scheme options base (a high-risk seam), and all obsolete members are removed — so clear obsolete warnings on 7.5 now and run the rotation contract test against an 8.0 preview early.
* **E. Decommission-interim tracking.** Each build-interim carries a "replace-when-native: OpenIddict <version>" marker (back-channel logout to 8.0, DCR to 8.0 re-targeted from 7.6, DPoP with no committed native, and OTel with no milestone) so interims are migrated proactively rather than carried forever.

### Consequences

* Good, because a bump is a bounded, tested event rather than a mass rewrite; an undocumented seam breaking is caught by CI before production; an interim swaps to native cleanly through its port; and the scattered "migrate when native" promises are consolidated.
* Bad, because the seam catalogue and contract-regression suite must be maintained (a per-bump cost, small next to a production break), and keeping interims behind a port is a discipline. Undocumented seams still carry baseline risk, mitigated by the tests plus the fallbacks already recorded (the commercial-component fallback in ADR-0011 and the mTLS-only-defer-DPoP fallback in ADR-0014).

### Confirmation

* The maintainer-endorsed `IOptionsMonitor` seam (issue #1434, ADR-0011) is the archetypal undocumented seam; its contract test already embodies this idea, and this ADR generalizes it to every seam.
* The public OpenIddict roadmap, verified 2026-07-04: DCR (issue #2404) targets an 8.0 preview (re-targeted from 7.6, which shipped maintenance without DCR), back-channel logout (issue #2175) targets an 8.0 preview, token exchange (RFC 8693) shipped in 7.0, OTel (issue #1345) is open with no milestone, and DPoP has no committed native support.
* The operating principle is "a finding is not a finding until source-verified", so each bump re-verifies the source-read assumptions.
* Build-time: implement the contract-regression test project and its CI wiring per bump, and attach the decommission markers.

## Pros and Cons of the Options

### Float the version and fix breakage as it appears

* Good, because it needs no upfront machinery.
* Bad, because breakage surfaces in production and the blast radius is uncontrolled.

### Pin 7.5.0 forever and never upgrade

* Good, because it is perfectly stable.
* Bad, because it forgoes security patches and the native features that would let interims be retired, accumulating technical debt.

### Pin plus a disciplined adaptation mechanism (chosen)

* Good, because bumps are bounded and tested, seams are CI-guarded, and interims retire cleanly through ports.
* Bad, because it costs an ongoing seam catalogue and contract-regression suite and the discipline to keep interims behind a port.

## More Information

* Original decision: 2026-07-04. The seam catalogue is a drafted deliverable (S1–S34 with risk tiers, isolation ports, a contract-test map, the roadmap, and the per-bump playbook); the remaining build-time work is the contract-regression test project and its CI wiring.
* Sibling decision: ADR-0030 (.NET runtime/TFM upgrade) is in the same external-version-adaptation family — a .NET major bump usually drags an OpenIddict and EF bump, so both playbooks share one contract-regression suite.
* Related decisions: ADR-0006/0009 (ports/adapters), ADR-0011 (the rotation seam, the fragile-seam archetype), ADR-0014 (DPoP build and DCR wait), ADR-0018 (Finbuckle-times-OpenIddict composition), ADR-0019 (back-channel logout interim), ADR-0022 (OpenTelemetry, no native telemetry), ADR-0030 (the sibling .NET upgrade playbook).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial-component fallback reference was generalized (no vendor named); OpenIddict and its public issue numbers are retained.
