---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security and DPO (the initial capability taxonomy and the ReBAC-adoption timing await their ratification); research survey of identity platforms, cloud IAM, and authorization models (see More Information)
informed: all contributors, via this repository
---

# Administer child tenants through explicit, scoped delegated-admin grants, not inherited seniority

## Context and Problem Statement

ADR-0001 settled that tenants are flat: a `Tenants` table with a `ParentTenantId` column, global identity, and explicit membership (user ↔ tenant ↔ roles). One question was left open. When one tenant is the parent of another — for example an acquiring company and the subsidiary it acquired — what may the parent's administrator do in the child tenant? Does authority inherit automatically, or must it be granted explicitly? This is a security decision touching privilege escalation, blast radius, and tenant isolation.

## Decision Drivers

* Security: avoid privilege escalation and any global super-admin; keep blast radius bounded.
* Auditability: every cross-tenant decision must record its provenance (direct membership vs delegated via a parent).
* Reflect real organizational structure — a parent genuinely administering a subsidiary — without implicit inheritance.
* Least privilege, revocability, and time-bounding.

## Considered Options

* Explicit membership only
* Explicit membership plus controlled delegated-admin
* Full ReBAC inherited admin

## Decision Outcome

Chosen option: "Explicit membership plus controlled delegated-admin", with a designed evolution path toward ReBAC, because it reflects real parent/subsidiary administration while avoiding a global super-admin and implicit inheritance.

Fixed parameters of the decision:

* **Tenants stay flat** (no native tenant nesting, consistent with mainstream identity platforms). The parent-child relationship is modeled by `Tenants.ParentTenantId` (from ADR-0001) at the authorization layer, not by nesting tenants.
* **Default is explicit per-tenant membership**: one human holds separate membership in each tenant they touch.
* **Cross-tenant admin is an explicit delegated-admin grant, never automatic seniority:**
  * **Scoped** to a subtree rooted at a parent tenant, applying only downward to descendants.
  * **Capability-typed** (for example manage-users, view-audit, billing) — least privilege, not "god over the child".
  * **Time-bound / just-in-time** where possible, **revocable**, and a **first-class grant object** (enumerable, auditable, revocable).
  * **Inheritance only narrows**: a child never exceeds its parent, and a parent DENY wins.
* **Dangerous capabilities never cascade**: deleting a tenant, cross-tenant data export, IAM changes, and re-delegation each require a direct grant on that tenant plus dual-control (matching the deployment's dual-control policy and ADR-0009).
* **Provenance in audit**: each authorization decision records whether it came from direct membership or was delegated via a named parent (ADR-0008).
* **Anti confused-deputy**: privileged handlers authorize the original principal, never a service identity (CWE-441).
* **Evolution**: start with the grant model in the membership/delegation tables; if relationships grow complex (many levels, many resource types), move to a ReBAC engine (for example OpenFGA or SpiceDB) with a `parent->admin` arrow. The membership/delegation schema is designed to map cleanly onto ReBAC later.

### Consequences

* Good, because there is no global super-admin, the audit trail carries provenance, it matches real organizational structure with control, grants are revocable, and blast radius stays bounded.
* Bad, because it requires defining a capability taxonomy and a delegation grant model, which is more complex than plain membership.
* It matches the deployment's dual-control requirement for dangerous cross-tenant operations.

### Confirmation

The enforcement design is detailed in a separate delegated-admin enforcement document (distinct from ADR-0017, which covers tenant provisioning). Its binding points:

* **Token vs decision-point split**: the 15-minute, single-tenant token carries only the `tenant` claim and coarse roles; the delegated-admin check runs **live at the Admin API** (revocable and time-bound, never baked into the token).
* **Authority is a server-side, deny-by-default grant check on the real initiator**; delegation is carried by the `act` claim (RFC 8693), not impersonation.
* **Forbidden-cascade** is enforced by an `IsInheritable` flag in the DB model (or the absence of a `from parent` arrow in ReBAC); tests confirm the forbidden capabilities never cascade from a parent grant.
* **Step-up (RFC 9470) plus dual-control** (proposer ≠ approver) gate dangerous and irreversible capabilities.
* `ICheckAccess` is DB-first (recursive CTE/closure) and moves to ReBAC later behind an unchanged contract.

## Pros and Cons of the Options

### Explicit membership only

A parent-admin who wants to manage a child must be explicitly added as a member of the child.

* Good, because it is the simplest and safest model with the clearest audit trail.
* Bad, because it is operationally heavy across many acquisitions and does not reflect that a parent company genuinely administers its subsidiary.

### Explicit membership plus controlled delegated-admin (chosen)

Explicit membership by default, plus delegated-admin grants that are subtree-scoped, capability-typed, time-bound, revocable, and audited; no global super-admin and no implicit inheritance.

* Good, because it captures real parent/subsidiary administration under least privilege, with revocation and bounded blast radius.
* Bad, because it requires a capability taxonomy and a delegation grant model.

### Full ReBAC inherited admin

Model `admin = direct_admin + parent->admin` in a ReBAC engine so a parent-admin automatically administers every descendant.

* Good, because it is powerful, matches organizational structure, and the engine can explain the access path.
* Bad, because such a broad automatic grant is dangerous, needs depth limits and capability scoping, and arrow-inheritance has a track record of escalation vulnerabilities.

## More Information

* Original decision: 2026-06-28 (Option 2). The initial capability taxonomy (proposed: `manage-users`, `manage-clients`, `manage-scopes`, `view-audit`, `view-config`; forbidden-cascade: `delete-tenant`, `data-export`, `iam-change`, `re-delegate`) and the timing of any ReBAC adoption await Security/DPO ratification.
* Research evidence: identity platforms are almost all flat tenants with explicit membership (Auth0 does not support sub-organizations; Okta treats orgs as hard boundaries with Org2Org/Aerial for time-bound delegation; WorkOS and FusionAuth are flat; ABP leaves hierarchy to the implementer, with the host as super-admin via `ICurrentTenant.Change`). Native-hierarchy exceptions include Frontegg (sub-accounts with opt-in role cascade) and Cerbos (scoped policy where a child only narrows). Microsoft's M&A guidance defaults to consolidating into one tenant, with delegated admin via Administrative Units plus PIM (AUs do not nest) or cross-tenant sync (linking, not nesting); GDAP replaces all-or-nothing with granular, scoped, time-bound admin. Cloud IAM: Azure and GCP inherit grants downward by default (additive, deny-override), whereas AWS does not (SCPs only restrict; cross-account access is an explicit assumable role), making AWS a stronger tenant-isolation reference. Authorization models: ReBAC (Zanzibar/OpenFGA `tuple_to_userset`, SpiceDB's `parent->admin` arrow) is the modern way to model hierarchy plus inheritance, while NIST hierarchical RBAC is role-to-role rather than a resource tree. OWASP Multi-Tenant guidance and CWE-441 warn about privilege escalation and the confused-deputy problem.
* Related decisions: ADR-0001 (flat tenants, explicit membership, `ParentTenantId`), ADR-0008 (audit provenance), ADR-0009 (dual-control). The detailed enforcement lives in a separate delegated-admin enforcement design document, which is not ADR-0017.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. The source's example acquisition scenario used specific company names; these are replaced here with generic parent/subsidiary placeholders.
