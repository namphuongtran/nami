---
status: "accepted"
stack-record: true
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; the Terraform BSL relicensing and the OpenTofu (MPL-2.0, Linux Foundation) fork
informed: all contributors, via this repository
---

# Use OpenTofu as the default infrastructure-as-code tool instead of Terraform

## Context and Problem Statement

The design documents listed "Terraform/Helm/Bicep" for infrastructure as code. HashiCorp relicensed Terraform from MPL-2.0 to BSL v1.1 in August 2023, restricting "competitive" use and reverting to MPL only four years after each release. Nami follows a license-freedom, OSS line — it chose an Apache-2.0 protocol engine over a commercial alternative and chose OSS PostgreSQL — and the IaC tool should be consistent with that. Which IaC tool should be the default?

## Decision Drivers

* License freedom and OSS consistency: avoid a BSL competitive-use restriction.
* Cloud-agnostic operation, matching ADR-0006/0009.
* Security: the IdP's IaC state can hold secrets and connection strings, so state encryption matters.
* Drop-in migration: do not rewrite existing modules.

## Considered Options

* Terraform (BSL)
* OpenTofu (MPL-2.0 OSS, under the Linux Foundation)

## Decision Outcome

Chosen option: "OpenTofu", as the default IaC tool, because it is drop-in compatible with Terraform, keeps an OSI-style OSS license, and adds native state encryption that matters for an identity provider.

Fixed parameters of the decision:

* **Drop-in Terraform-compatible**: HCL, module structure, state format, and the CLI (`init`/`plan`/`apply`/`destroy`/`state`) all match, so existing Terraform modules run almost unchanged and migration is roughly trivial.
* **MPL-2.0 OSS** (under the Linux Foundation), which avoids the BSL competitive-use restriction and matches the license-freedom line.
* **Security bonus (important for an IdP)**: OpenTofu's native state encryption (v1.7 and later) encrypts the state file, including remote state, without an external KMS workflow. Because the IdP's IaC state can contain secrets and connection strings, encrypting state is a real value, not only a licensing point.
* **Helm** for Kubernetes; a per-cloud provider (Azure, AWS, GCP, or Vault) through an adapter, matching the cloud-agnostic direction of ADR-0006. **Bicep** is used only if an Azure-specific need arises, and is not the default.

### Consequences

* Good, because it gives license freedom (MPL), native state encryption (a security bonus for an IdP), a drop-in migration, and features that are ahead of the Terraform OSS CLI (state encryption, provider `for_each`, and `-exclude`).
* Bad, because its market share and ecosystem are smaller than Terraform's; this is mitigated by the drop-in compatibility and by large production adopters, and the missing vendor-cloud-specific features do not apply because Nami is cloud-agnostic.
* This is coupled to ADR-0006: state-encryption key management uses the cloud-agnostic secret/key ports.

### Confirmation

* Terraform was relicensed to BSL v1.1 in August 2023; OpenTofu is an MPL-2.0 fork of Terraform 1.5 under the Linux Foundation (a CNCF sandbox project with an exception to retain MPL). OpenTofu features ahead of the Terraform OSS CLI include state encryption (v1.7), provider `for_each` (v1.9), and `-exclude` (v1.9). As of 2026 the drop-in claim is credible and migration is trivial because HCL, modules, and state are compatible, with large adopters running it in production.
* Follow-ups (Ops): the state backend (object storage or Postgres) plus state-encryption key management tied to the ADR-0006 cloud-agnostic key ports; provider version pinning; and CI wiring where plan/apply is gated by dual-control.

## Pros and Cons of the Options

### Terraform (BSL)

* Good, because it has the largest market share and the widest ecosystem.
* Bad, because the BSL license restricts competitive use and ties the project to a single vendor's terms, which conflicts with the license-freedom line.

### OpenTofu (MPL-2.0 OSS) (chosen)

* Good, because it is OSS under a neutral foundation, drop-in compatible, and adds native state encryption; and it is ahead of the Terraform OSS CLI on several features.
* Bad, because its ecosystem is smaller, mitigated by compatibility and production adopters.

## More Information

* Original decision: 2026-07-04.
* This continues the project's license-freedom line — an Apache-2.0 protocol engine and OSS PostgreSQL, and now an MPL-2.0 IaC tool.
* Related decisions: ADR-0006/0009 (cloud-agnostic ports and adapters; state-encryption key management uses these).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. A reference to having chosen an Apache-2.0 engine over a commercial alternative was generalized (no competitor named); Terraform, OpenTofu, and the tool and foundation names are retained as the factual subject of the decision.
