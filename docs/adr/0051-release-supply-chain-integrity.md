---
status: "accepted"
date: 2026-07-07
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: sigstore/cosign keyless signing; SLSA build provenance; CycloneDX; container vulnerability scanning (Trivy/Grype)
informed: all contributors, via this repository
---

# Sign and attest release artifacts with keyless provenance for a verifiable supply chain

## Context and Problem Statement

Nami is security infrastructure shipped as NuGet packages plus a reference container image. Consumers, and the coordinated-disclosure process of ADR-0045, need to verify that a given artifact is the genuine, untampered one from Nami and to trace how it was built. The packaging decision (ADR-0027) named "signing, SBOM, SLSA, scanning" as the approach but not the mechanism, ADR-0026 owns the SBOM and the dependency/license scan, and ADR-0045 relies on "verifiable signed releases" without defining them. So release integrity is referenced in three places and specified in none: how artifacts are signed, how build provenance is attested, and how base-image vulnerabilities are caught between releases.

## Decision Drivers

* A consumer must be able to verify the publisher and integrity of every artifact (package, image, SBOM).
* No static, long-lived signing key should sit in CI (the standing-secret risk ADR-0009 avoids).
* Build provenance must be attestable (SLSA) to resist supply-chain tampering.
* Base-image vulnerabilities that appear between releases must be caught, not only at release time.
* Signing and publishing are external and irreversible, so they are gated by dual-control (ADR-0046).

## Considered Options

* Unsigned artifacts, trusting the registry
* Signing with a static private key held in CI
* Keyless signing (cosign via OIDC) plus SLSA provenance and a scheduled re-sign cadence

## Decision Outcome

Chosen option: "Keyless signing plus SLSA provenance and a re-sign cadence". The fixed parameters are:

* **A. NuGet package signing** with a publisher (Authenticode) certificate, so a consumer can verify the publisher of every package.
* **B. Keyless image, SBOM, and provenance signing** with cosign through GitHub Actions OIDC (sigstore), so no static private key is held anywhere (this is ADR-0009's no-static-secret principle applied to the release pipeline). SLSA build-provenance is attested the same way.
* **C. SBOM per release.** A CycloneDX SBOM (owned by ADR-0026) is generated for each release, attached to the GitHub release, and cosign-attested on the image.
* **D. Base-image integrity between releases.** The base image is digest-pinned (never a mutable tag), passes a container vulnerability-scan gate (Trivy/Grype) at release, and is on a scheduled rebuild, re-scan, and re-sign cadence (a dependency bot bumps the digest), so a base-image CVE that appears after a release is caught rather than shipped silently until the next feature release.
* **E. Signing happens only in the gated release pipeline.** Signing runs on a version tag, after all quality gates pass, under dual-control (a protected environment with required reviewers, ADR-0046); pull-request CI never signs or publishes.

### Consequences

* Good, because every artifact is verifiable end to end (publisher, integrity, and build provenance), which is exactly what makes ADR-0045's "verifiable fixed release" promise concrete.
* Good, because keyless OIDC signing means there is no long-lived signing key to leak or rotate, consistent with ADR-0009.
* Good, because the scheduled re-scan/re-sign cadence catches base-image CVEs between releases instead of leaving a known-vulnerable image published.
* Bad, because keyless signing roots trust in the CI identity provider and sigstore, so that infrastructure must be available and trusted, and a publisher certificate must still be obtained and managed for NuGet.
* Bad, because the rebuild/re-scan/re-sign cadence is ongoing operational work rather than a one-time setup.
* Neutral, because the SBOM contents and the dependency/license scan remain owned by ADR-0026; this ADR is the signing, provenance, and cadence, not the SBOM itself.

### Confirmation

* A consumer can verify the NuGet package signature and can cosign-verify the image, its SBOM attestation, and its SLSA provenance.
* A release with a failing vulnerability scan, a missing signature, or a missing attestation is aborted by the gate.
* The scheduled cadence rebuilds, re-scans, and re-signs the image when a base-image CVE appears between releases.
* Signing and publishing require a second approver in a protected environment; no unattended job can sign or publish.

## Pros and Cons of the Options

### Unsigned artifacts, trusting the registry

* Good, because there is nothing to set up.
* Bad, because a consumer cannot distinguish a genuine artifact from a tampered one, there is no provenance, and the CVE process has nothing verifiable to point at.

### Static-key signing

* Good, because it produces signed artifacts and is conceptually simple.
* Bad, because a long-lived private key in CI is exactly the standing secret ADR-0009 rejects: it can be exfiltrated, must be rotated, and its compromise forges trusted releases.

### Keyless signing plus SLSA provenance and a re-sign cadence (chosen)

* Good, because artifacts are verifiable with no static key, provenance resists tampering, and base-image CVEs are caught between releases.
* Bad, because trust roots in the CI OIDC provider and sigstore, and the cadence is ongoing work.

## More Information

* Recorded from the productization design (doc 28 §10.5-§10.7, gate #7, and the F43 re-sign cadence of 2026-07-05); the supply-chain approach was confirmed on 2026-07-07. The publisher certificate and enabling cosign keyless are build-time follow-ups.
* Related decisions: ADR-0009 (no static secret, which keyless OIDC signing embodies), ADR-0026 (the CycloneDX SBOM and the dependency/license scan this attaches and attests), ADR-0027 (packaging and distribution, which named these supply-chain measures and defers their detail here), ADR-0044 (the versioned releases being signed), ADR-0045 (the coordinated-disclosure process that relies on a verifiable fixed release), and ADR-0046 (dual-control on the irreversible sign-and-publish step).
* Authored in this repository in 2026-07 to record the settled release-integrity decision as an ADR; neutral tools and standards (sigstore/cosign, SLSA, CycloneDX, Trivy, Grype, GitHub Actions OIDC, Authenticode) are named factually for identification only, and no commercial competitor is named.
