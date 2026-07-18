---
status: "proposed"
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OpenID4VC final specifications (OID4VP 2025-07-10, OID4VCI 2025-09-16, HAIP 2025-12-29), the SD-JWT VC and ISO mdoc/mDL formats, and eIDAS 2.0 / EUDI Wallet, verified 2026-07-18; that OpenIddict has no native OID4VC support (checked 2026-07-18); the OAuth base (ADR-0021/0014) and identity decisions (ADR-0028/0013)
informed: all contributors, via this repository
---

# Support issuing Verifiable Credentials via OpenID4VC (Nami as issuer)

## Context and Problem Statement

Classic OIDC federation is server-to-server: a relying party asks the identity provider each time, so the provider sees where a user signs in and the relying party must trust the provider to be online. The verifiable-credential (wallet) model inverts this into three roles, Issuer, Holder, and Verifier: the provider issues a credential the user keeps in a wallet, and the user later presents it to a verifier, offline and with selective disclosure (for example proving "over 18" without revealing a birthdate). This model is rising, and eIDAS 2.0 mandates EU Digital Identity (EUDI) Wallets, with the OpenID4VC specifications reaching final status in 2025 (OID4VP in July, OID4VCI in September, the high-assurance interoperability profile in December).

Nami is an OAuth/OIDC authorization server on OpenIddict. OID4VC issuance (OID4VCI) is an OAuth-protected API, so Nami's OpenIddict base can carry the OAuth layer, but the credential-specific parts (the credential endpoint, issuer metadata, credential formats such as SD-JWT VC and ISO mdoc, and holder key-binding proof) are additive and are not native to OpenIddict. The question is whether Nami should support being a credential issuer, and in what scope. This ADR is `proposed`: it records the direction, to be accepted when demand (primarily EU and eIDAS-driven) is real, because the fit is genuine but this is a model shift and a heavy build.

## Decision Drivers

* Strategic optionality: eIDAS 2.0 and the EUDI Wallet are a concrete regulatory driver, and being a credential issuer is adjacent to Nami's core.
* Reuse the OAuth base: OID4VCI is OAuth-protected, so OpenIddict issues the token that authorizes credential issuance rather than building a separate authorization layer.
* Privacy alignment: selective disclosure fits data-minimization and Nami's data-subject-rights posture.
* Scope honesty: this is a shift from federation to holder-presented credentials and a substantial build, not a configuration flip.
* Demand-driven: the demand is largely EU and government, and unproven inside Nami's user base.

## Considered Options

* Do nothing: Nami stays a classic OIDC provider and verifiable credentials are out of scope.
* Support Nami as a credential issuer via OID4VCI, demand-driven, reusing the OAuth base, SD-JWT VC first.
* Build the full wallet ecosystem (issuer, verifier, and a wallet) now.

## Decision Outcome (proposed)

Proposed: "support Nami as a credential issuer via OID4VCI, demand-driven", scoped first to the issuer role. Building the full ecosystem now is rejected (far ahead of demand and outside an IdP's scope); doing nothing forecloses the eIDAS-driven opportunity without a recorded rationale.

* **Role: issuer first.** After the user authenticates with the required assurance (ADR-0013), Nami issues a credential into the user's wallet via OID4VCI. The verifier and presentation side (OID4VP), and any Nami-hosted verifier, is a later phase, out of initial scope. Nami does not build a wallet.
* **Reuse the OAuth base.** OID4VCI's credential endpoint is OAuth-protected, so OpenIddict issues the access token that authorizes issuance and the OAuth/authorization layer is reused. The additive, non-native parts are the credential endpoint, issuer metadata, the credential formats, and the holder key-binding proof; OpenIddict has no native OID4VC support (verified), so this is a build on top of the OAuth base.
* **Formats: SD-JWT VC first.** SD-JWT VC is the first format because its selective disclosure is the privacy win; ISO mdoc/mDL is added only if a mobile-document use case appears. Credentials are signed with the existing key material and rotation machinery (ADR-0005/0006/0011), not a new key story.
* **Multi-tenant scoping.** A credential is tenant-scoped: it asserts membership or a role within one tenant, and its issuer identifier is that tenant's per-tenant issuer (ADR-0001/0049), so a credential from tenant A is not honored as tenant B's.
* **Privacy alignment.** Selective disclosure aligns with data-minimization and the data-subject-rights suite (ADR-0053) and the cross-border and residency controls (ADR-0054), which is a strong fit for the EU context that drives the demand.
* **Implementation open.** No library is pinned; a .NET verifiable-credential ecosystem exists but none is endorsed here, and the choice (build versus adopt, per the permissive-only policy ADR-0026) is confirmed at build time. This is an additional model, not a replacement for OIDC federation.

### Consequences

* Good, because it records a credible path into the eIDAS/EUDI opportunity, reusing the OAuth base and the existing signing-key machinery rather than starting over.
* Good, because selective disclosure strengthens the privacy story that Nami already invests in (ADR-0053/0054).
* Good, because scoping to the issuer role and SD-JWT VC first keeps a potentially huge surface bounded, and leaving it `proposed` avoids building far ahead of demand.
* Bad, because even the issuer role is a substantial, non-native build (credential endpoint, formats, holder key-binding) on top of OpenIddict; accepted as the reason it stays `proposed` until demand.
* Bad, because the wallet ecosystem and eIDAS conformance move quickly, so an accepted build must re-verify the then-current profiles; mitigated by the revisit trigger.

## Pros and Cons of the Options

### Do nothing

* Good, because Nami's classic OIDC already serves its current users and needs no new surface.
* Bad, because it forecloses the eIDAS-driven credential-issuer opportunity with no recorded rationale, and leaves the fit unassessed.

### Credential issuer via OID4VCI, demand-driven (chosen, proposed)

* Good, because it reuses the OAuth base and key machinery, aligns with the privacy posture, and is bounded to the issuer role and one format first.
* Bad, because it is still a heavy, non-native build and tracks a fast-moving ecosystem; both accepted for a `proposed` record.

### Full wallet ecosystem now

* Good, because it would be a complete offering.
* Bad, because building an issuer, a verifier, and a wallet is far outside an IdP's scope and far ahead of demand.

## More Information

* This is a sibling to the demand-driven `proposed` extension ADRs (ADR-0064 MCP, ADR-0068 Shared Signals, ADR-0055 SAML/WS-Federation, ADR-0056 FAPI 2.0, ADR-0057 Windows/Negotiate): a recorded direction, accepted when demand is real, pinning no library.
* Related decisions: ADR-0014 (the advanced-protocol scope this extends, deciding what Nami builds versus defers), ADR-0021 (the OpenIddict OAuth base OID4VCI's token layer reuses), ADR-0028 and ADR-0013 (the identity and assurance that gate issuance), ADR-0005/0006/0011 (the signing keys a credential is signed with), ADR-0001/0049 (per-tenant issuer scoping applied to credentials), ADR-0053 and ADR-0054 (the privacy and residency posture selective disclosure strengthens), and ADR-0026 (the permissive-only policy any VC library must pass).
* Standards verified 2026-07-18: OpenID4VC final specs (OID4VP 2025-07-10, OID4VCI 2025-09-16, HAIP 2025-12-29), SD-JWT VC and ISO mdoc/mDL formats, and eIDAS 2.0 / EUDI Wallet; OpenIddict has no native OID4VC support. All named factually.
* Revisit trigger: stays `proposed` until Nami actively develops credential issuance. At that point, confirm the then-current OID4VCI/OID4VP and HAIP profiles and the eIDAS/EUDI requirements, assess whether to build or adopt a permissive .NET VC library, and settle the format set (SD-JWT VC, and whether mdoc is needed) and the tenant-scoped issuer design, before accepting or building.
* Authored fresh for this repository.
