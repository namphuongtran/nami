---
status: "proposed"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: the MCP authorization specification (revisions 2025-03-26, 2025-06-18, and 2025-11-25), verified 2026-07-18; the enabling decisions ADR-0014 (RFC 8707), ADR-0035 (client registration), ADR-0048/0049 (resource-server validation)
informed: all contributors, via this repository
---

# Support Nami as the OAuth authorization server for MCP servers

## Context and Problem Statement

The Model Context Protocol (MCP) is how AI agents connect to tools and data, and its authorization model has converged on OAuth. The MCP authorization spec evolved quickly through 2025 (revisions 2025-03-26, then 2025-06-18, then the current 2025-11-25) and settled on a clean split of roles: an MCP server is an **OAuth 2.1 resource server only**, and a **separate authorization server** issues the tokens it validates. That separation is exactly Nami's role: Nami is an authorization server, not an MCP server.

The question is whether Nami should explicitly support being the authorization server for MCP, and if so, what it must offer. This is worth recording now because the strategic fit is strong and most of the required capability already exists, but MCP is fast-moving and demand inside Nami's user base is unproven, so committing to build ahead of that demand would be premature. This ADR is `proposed`: it records the direction and the scope, to be accepted when demand is real.

## Decision Drivers

* Strategic fit: MCP needs an authorization server, and being one is Nami's core competence.
* Reuse over rebuild: most of what an MCP authorization server needs, Nami already has.
* Do not reinvent the protocol: the MCP requirements are OAuth RFCs that OpenIddict supports natively (ADR-0021/0061).
* Demand-driven: do not build an MCP surface ahead of evidence that Nami's users want it.
* Track a moving target: the spec changed three times in 2025, so any commitment must pin to a confirmed revision.

## Considered Options

* Do nothing explicit: MCP servers can already use a standard OAuth AS, so add no MCP-specific support.
* Support Nami as the MCP authorization server through a demand-driven extension that builds on existing OAuth capability.
* Build a full MCP stack, including an MCP server implementation and tool hosting.

## Decision Outcome (proposed)

Proposed: "support Nami as the MCP authorization server through a demand-driven extension", scoped strictly to the authorization-server role. Building a full MCP server is out of scope (it is a different product, and MCP explicitly separates the resource server from the authorization server). The capabilities, mapped to what Nami already has:

* **OAuth 2.1 with mandatory PKCE.** Nami already issues authorization-code tokens with PKCE through OpenIddict; the MCP requirement is met by existing capability.
* **Authorization-server metadata discovery.** MCP clients discover the AS through OAuth 2.0 AS metadata (RFC 8414) or OIDC discovery; Nami already publishes discovery metadata.
* **Resource Indicators (RFC 8707).** MCP clients send the target MCP server as the `resource` parameter so the issued token's audience is bound to that server. ADR-0014 records RFC 8707 resource indicators as native, so basic audience-binding is available today; the advanced per-tenant/per-API resource-isolation policy layer that ADR-0014 deferred is where MCP-specific audience scoping would land if demand warrants.
* **MCP client onboarding.** Onboarding uses Nami's authenticated Admin-API client registration (ADR-0035), which aligns with the 2025-11-25 spec's move toward enterprise-managed client registration. The standard RFC 7591 `/connect/register` endpoint (which some MCP clients expect for pure dynamic client registration) remains the documented compatibility path, gated on OpenIddict 8.0 and the ADR-0035 follow-up, to be built only if MCP demand requires it.
* **Token validation on the MCP-server side.** An MCP server validating Nami-issued tokens is a resource server, so Nami's per-tenant validation (ADR-0049) and introspection isolation (ADR-0048) apply, and confidential MCP clients can authenticate with `private_key_jwt` (ADR-0009). The RFC 9728 Protected Resource Metadata that the spec mandates is the MCP server's responsibility, not the authorization server's; Nami may document how to satisfy it but does not implement it.

Implementation stays open: no MCP library is pinned, and the specific spec revision is confirmed at build time because the target moves.

### Consequences

* Good, because it positions Nami for a fast-growing use case with mostly existing capability, and it does so without reinventing anything (the requirements are OAuth RFCs OpenIddict supports).
* Good, because scoping to the authorization-server role keeps Nami focused and matches how MCP itself separates the roles.
* Good, because leaving it `proposed` avoids building ahead of demand while still recording the direction and the enabling hooks.
* Neutral, because the enterprise-managed-client direction of the current spec happens to match ADR-0035, so the registration story needs no reversal.
* Bad, because MCP clients that require the standard RFC 7591 DCR endpoint are not served until that endpoint is built (OpenIddict 8.0 plus the ADR-0035 follow-up); accepted while this is `proposed`.
* Bad, because the spec is fast-moving, so any accepted build must re-verify the then-current revision; mitigated by the confirm-at-build note and the demand-driven trigger.

## Pros and Cons of the Options

### Do nothing explicit

* Good, because a standard OAuth AS already works for MCP in principle, so there is nothing to build.
* Bad, because it leaves the fit unstated and the resource-indicator, discovery, and registration story unverified against the MCP spec, so no one can tell whether Nami is actually MCP-ready.

### Demand-driven MCP authorization-server extension (chosen, proposed)

* Good, because it records the scope and the reuse of existing capability, and it triggers on real demand rather than speculation.
* Bad, because it defers the one genuinely missing piece (the standard DCR endpoint) and must track a moving spec; both accepted for a `proposed` record.

### Full MCP stack including a server implementation

* Good, because it would be an all-in-one MCP offering.
* Bad, because building an MCP server is a different product from an identity provider, contradicts the role separation MCP itself defines, and is far outside Nami's scope.

## More Information

* This is a sibling to the other demand-driven, `proposed` extension ADRs (ADR-0055 SAML/WS-Federation, ADR-0056 FAPI 2.0, ADR-0057 Windows/Negotiate): each records a direction to be accepted when demand is real, and pins no third-party library.
* Related decisions: ADR-0014 (native RFC 8707 resource indicators and the deferred resource-isolation policy layer), ADR-0035 (Admin-API client registration, and the standard RFC 7591 endpoint as a gated follow-up), ADR-0048 and ADR-0049 (resource-server introspection isolation and per-tenant validation for MCP servers), ADR-0009 (`private_key_jwt` for confidential clients), ADR-0021 and ADR-0061 (OpenIddict-native, do-not-reinvent).
* Spec verified 2026-07-18: MCP authorization revisions 2025-03-26, 2025-06-18, and the current 2025-11-25; the role split (MCP server as resource server, separate authorization server), mandatory PKCE, metadata discovery, RFC 8707 (client), and RFC 9728 (server) are named factually for identification.
* Revisit trigger: this stays `proposed` until Nami actively develops MCP support. At that point, re-read the then-current MCP authorization revision and check what has changed since 2025-11-25: any new required endpoint or metadata, changes to the client-registration model, tightened token-binding or audience rules, and any capability the spec now mandates that this scope does not yet cover. Update or accept this ADR against that revision before building.
* Authored fresh for this repository.
