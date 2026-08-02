---
status: "accepted"
stack-record: true
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; the public OpenIddict roadmap (verified 2026-07-04) and the source-verification V-files
informed: all contributors, via this repository
---

# Adapt to OpenIddict version upgrades with seam isolation, per-bump contract-regression tests, and a migration playbook

## Context and Problem Statement

Nami pins OpenIddict 7.5.0 and relies on three different stability tiers of it:

1. **Native, documented, versioned API**: low risk on a bump.
2. **Undocumented but maintainer-endorsed seams**: high risk, and they can break silently on a bump. Examples: the #1434 seam for no-restart rotation, meaning the framework options monitor driven by a custom `IConfigureOptions<OpenIddictServerOptions>` and a custom `IOptionsChangeTokenSource` (issue #1434, ADR-0011, verification record V06); inserting event handlers by `SetOrder` around the built-ins for DPoP (spikes A-1 and A-3); the internal `ValidateProofOfPossession` throwing `ID2196` (spike A-3); and the Finbuckle-times-OpenIddict `OnModelCreating`/`SaveChanges` composition (spike A-4).
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

Chosen option: "Pin plus a disciplined adaptation mechanism", implemented as six parts (five originally; part F was added 2026-08-02).

* **A. Version pin plus controlled bump.** Pin OpenIddict exactly through Central Package Management (all sub-packages aligned). A bump is a deliberate, tested event, not floating, and follows the playbook in part D.
* **B. Seam catalogue plus isolation.** Maintain an OpenIddict seam catalogue (a deliverable design document) listing every dependency on OpenIddict (numbered `S1` onward), each tagged with a risk tier (native, endorsed-undocumented, internal-behavior, handler-order, build-interim, adjacent-stack) and pointing to a source-verify file, a contract test, an isolation port, and a decommission marker. Each build-interim is isolated behind Nami's own port so swapping to native changes an adapter, not a caller: DPoP behind a handler interface, back-channel logout behind a logout-fanout service (ADR-0019), rotation behind `ISigningKeyStore` plus the #1434 seam (ADR-0011), and interim DCR (if built) behind admin provisioning. This matches ports/adapters (ADR-0006/0009).
* **C. Contract-regression test suite** (the core safety net for undocumented seams). A dedicated suite asserts each seam's behavior on the pinned version and runs on **every** bump (7.5 to 7.6 to 8.0). It extends what already exists: the rotation contract for the #1434 seam (ADR-0011), the DPoP handler-order and `ID2196`-avoidance checks (spikes A-1 and A-3), the Finbuckle composition test (spike A-4), the "re-run on every bump" test from spike A-2 (T6), and the source-read assumptions captured in verification records V01, V05, V06, and V14 (for example that `AttachSecurityCredentials` uses `First()`, that `AttachSigningKeys` iterates without a `NotBefore` filter, that family-revoke calls `RevokeByAuthorizationIdAsync` inside `ValidateTokenEntry`, the introspection `ValidateAuthorizedParty` behavior, and the pass-through versus fully-handled endpoint set). A bump that breaks a contract fails the build, so it is known before production; the suite is wired into CI.
* **D. Per-release migration playbook.** For each OpenIddict release: read the release notes; run the contract-regression suite plus conformance; for a feature that has just become native (DCR and back-channel logout are both at 8.0), evaluate swapping interim-to-native behind the port (a small blast radius), keeping the interim until native is proven; update the pin; and decommission the interim. Note the 8.0 breaking changes in advance (an options type will no longer inherit the authentication-scheme options base (a high-risk seam), and all obsolete members are removed) so clear obsolete warnings on 7.5 now and run the rotation contract test against an 8.0 preview early.
* **E. Decommission-interim tracking.** Each build-interim carries a `replace-when-native: OpenIddict <version>` marker (back-channel logout to 8.0, DCR to 8.0 re-targeted from 7.6, DPoP with no committed native, and OTel with no milestone) so interims are migrated proactively rather than carried forever.
* **F. Handler insertion is order-anchored, and it is Nami's own mechanism rather than a consumer extension point** (added 2026-08-02, because the rule was binding, was stated only in the design layer, and was owned by no decision). Custom protocol behaviour is an OpenIddict event handler inserted into the engine pipeline, under three rules that exist to survive a bump, which is why the axis belongs in this ADR rather than beside the ports. It anchors to a **named built-in descriptor plus an offset**, never to a literal order number, so a built-in that moves carries the custom handler with it. Every custom position is declared as a **constant in one file**, so the whole set is reviewable in one place. And a **pipeline-order snapshot test** pins the resolved order and fails on a bump that changes it, which is what lets part D's playbook see a silent reorder rather than discover it in production. Part B already tags `handler-order` as a risk tier, and the positions are catalogued as seam S33 in design [22](../design/22-openiddict-seam-catalogue.md).

  **The axis is not offered to consumers, and that is a decision rather than an omission**, on the same reasoning by which ADR-0027 parameter G declines to ship a UI class library. An embedder can technically do it: `AddEventHandler` and the built-in descriptors are public types of OpenIddict, not of Nami, verified in the checked-in 7.5.0 source (`OpenIddictServerBuilder.cs:22` and `:44`, `OpenIddictServerHandlers.cs:3511`). What Nami declines is the **promise**, because the promise is not deliverable. ADR-0044 parameter E commits that when OpenIddict breaks, "Nami absorbs it behind its own surface" and the consumer reads only Nami's migration guide; there is no Nami surface between an embedder and an OpenIddict type they name directly. The failure is sharper than "hard to keep stable". ADR-0044 parameter B classifies changes to **Nami's** surface, making a rename MAJOR; an upstream rename is not a change to Nami's surface at all, so parameter B never classifies it, and it ships inside a MINOR that breaks consumer code while every rule Nami has says nothing happened. Nami therefore uses this axis itself, as DPoP does in design [06](../design/06-sender-constrained-tokens.md), and the ports remain the documented consumer axis (ADR-0027 parameter E).

  **Reversal has a named mechanism rather than a general willingness.** If the embedder path ever needs pipeline insertion, the way to make it supportable is to publish the constants file above as a **Nami-owned public surface** under ADR-0044, so a consumer anchors to a Nami constant and Nami absorbs the upstream rename behind it, which is the shape the ports already have. Blessing a direct anchor to an OpenIddict descriptor is the option that stays closed, because it is the one Nami cannot honour.

### Consequences

* Good, because a bump is a bounded, tested event rather than a mass rewrite; an undocumented seam breaking is caught by CI before production; an interim swaps to native cleanly through its port; and the scattered "migrate when native" promises are consolidated.
* Bad, because the seam catalogue and contract-regression suite must be maintained (a per-bump cost, small next to a production break), and keeping interims behind a port is a discipline. Undocumented seams still carry baseline risk, mitigated by the tests plus the fallbacks already recorded (the commercial-component fallback in ADR-0011 and the mTLS-only-defer-DPoP fallback in ADR-0014).

### Confirmation

* The maintainer-endorsed `IOptionsMonitor` seam (issue #1434, ADR-0011) is the archetypal undocumented seam; its contract test already embodies this idea, and this ADR generalizes it to every seam.
* The public OpenIddict roadmap, verified 2026-07-04: DCR (issue #2404) targets 8.0.0-preview.2 (re-targeted from 7.6, which shipped maintenance without DCR), back-channel logout (issue #2175) targets 8.0.0-preview.2, token exchange (RFC 8693) shipped in 7.0, OTel (issue #1345) is open with no milestone, and DPoP has no committed native support.
* The operating principle is "a finding is not a finding until source-verified", so each bump re-verifies the source-read assumptions.
* Build-time: implement the contract-regression test project and its CI wiring per bump, and attach the decommission markers. Part F adds two: the file of pipeline-position constants, and the pipeline-order snapshot baseline it is pinned against (seam S33).

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

* Original decision: 2026-07-04. The seam catalogue is a drafted deliverable (the numbered registry with risk tiers, isolation ports, a contract-test map, the roadmap, and the per-bump playbook); the remaining build-time work is the contract-regression test project and its CI wiring.
* Sibling decision: ADR-0030 (.NET runtime/TFM upgrade) is in the same external-version-adaptation family; a .NET major bump usually drags an OpenIddict and EF bump, so both playbooks share one contract-regression suite.
* Related decisions: ADR-0006/0009 (ports/adapters), ADR-0011 (the rotation seam, the fragile-seam archetype), ADR-0014 (DPoP build and DCR wait), ADR-0018 (Finbuckle-times-OpenIddict composition), ADR-0019 (back-channel logout interim), ADR-0022 (OpenTelemetry, no native telemetry), ADR-0030 (the sibling .NET upgrade playbook).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A commercial-component fallback reference was generalized (no vendor named); OpenIddict and its public issue numbers are retained.
* **Amended 2026-08-01: the seam range was un-pinned from `S1`-`S34` to "numbered `S1` onward".** Registering S35 (the private-claim carriage that the refresh-ceiling anchor and the session-liveness gate both ride on, design [22](../design/22-openiddict-seam-catalogue.md)) made the old text stale in two places here and five in the catalogue, which is the second time a seam count has had to be chased across documents. The decision this ADR makes is that **every** dependency is registered; how many there are is a measurement the catalogue takes, not a parameter this ADR sets. Pinning the upper bound in an accepted ADR quietly discouraged the thing the ADR exists to encourage, since adding a seam meant editing a binding document. The catalogue now owns the enumeration and this ADR owns the rule.
