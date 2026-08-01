---
status: "accepted"
date: 2026-06-28
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: Security and DPO (the initial capability taxonomy and the ReBAC-adoption timing await their ratification); research survey of identity platforms, cloud IAM, and authorization models (see More Information)
informed: all contributors, via this repository
---

# Administer child tenants through explicit, scoped delegated-admin grants, not inherited seniority

## Context and Problem Statement

ADR-0001 settled that tenants are flat: a `Tenants` table with a `ParentTenantId` column, global identity, and explicit membership (user ↔ tenant ↔ roles). One question was left open. When one tenant is the parent of another (for example an acquiring company and the subsidiary it acquired) what may the parent's administrator do in the child tenant? Does authority inherit automatically, or must it be granted explicitly? This is a security decision touching privilege escalation, blast radius, and tenant isolation.

## Decision Drivers

* Security: avoid privilege escalation and any global super-admin; keep blast radius bounded.
* Auditability: every cross-tenant decision must record its provenance (direct membership vs delegated via a parent).
* Reflect real organizational structure (a parent genuinely administering a subsidiary) without implicit inheritance.
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
  * **Capability-typed** (for example `manage_users`, `view_audit`, `view_config`), least privilege, not "god over the child".
  * **Time-bound / just-in-time** where possible, **revocable**, and a **first-class grant object** (enumerable, auditable, revocable).
  * **Inheritance narrows in effect, not by a DENY rule.** The v1 grant model is purely **additive**: a grant grants, and there is no scoped DENY row, so v1 deliberately does **not** implement a parent-DENY-override or an explicit "a child cannot exceed its parent" ceiling. The narrowing intent is met by three other mechanisms instead: least-privilege capabilities on each grant, forbidden-cascade for dangerous capabilities, and the non-cascading `re_delegate` gate below. A scoped deny-override is a ReBAC-era consideration, not a v1 property, and nothing in v1 may be designed as if the ceiling were enforced.
* **Dangerous capabilities never cascade**: deleting a tenant, cross-tenant data export, IAM changes, and re-delegation each require a direct grant on that tenant plus dual-control (matching the deployment's dual-control policy and ADR-0009).
* **Provenance in audit**: each authorization decision records whether it came from direct membership or was delegated via a named parent (ADR-0008).
* **Anti confused-deputy**: privileged handlers authorize the original principal, never a service identity (CWE-441).
* **Evolution**: start with the grant model in the membership/delegation tables, concretely `Memberships`, `DelegatedAdmin` (the subtree-rooted, time-bound, revocable grant carrying its own provenance), `DelegatedAdminCapabilities`, `CapabilityCatalog(Capability, IsInheritable)`, and `TenantClosure` for ancestor lookup. If relationships grow complex (many levels, many resource types), move to a ReBAC engine (for example OpenFGA or SpiceDB) with a `parent->admin` arrow. The schema is designed to map cleanly onto ReBAC later, and the `IsInheritable` flag is what carries the forbidden-cascade rule in the meantime.

### Consequences

* Good, because there is no global super-admin, the audit trail carries provenance, it matches real organizational structure with control, grants are revocable, and blast radius stays bounded.
* Bad, because it requires defining a capability taxonomy and a delegation grant model, which is more complex than plain membership.
* It matches the deployment's dual-control requirement for dangerous cross-tenant operations.

### Confirmation

The enforcement design is detailed in a separate delegated-admin enforcement document (distinct from ADR-0017, which covers tenant provisioning), backed by research into RFC 8693, RFC 9068, RFC 9470, Azure RBAC/PIM/GDAP, OpenFGA and SpiceDB, OWASP, and CWE-441. Its binding points:

* **Token vs decision-point split**: the 15-minute, single-tenant token carries only the `tenant` claim and coarse roles; the delegated-admin check runs **live at the Admin API** (revocable and time-bound, never baked into the token).
* **Authority is a server-side, deny-by-default grant check on the real initiator**; delegation is carried by the `act` claim (RFC 8693), not impersonation.
* **Forbidden-cascade** is enforced by an `IsInheritable` flag in the DB model (or the absence of a `from parent` arrow in ReBAC); tests confirm the forbidden capabilities never cascade from a parent grant.
* **Grant management is itself gated**, which is what makes `re_delegate` meaningful: both creating and revoking a delegated-admin grant require the actor to hold `re_delegate` **directly** on that tenant (it never cascades), so a delegated admin cannot mint sub-grants and build an escalation chain. **Create and revoke are then deliberately asymmetric on dual-control: creating a grant needs two people, revoking one needs one** (revoke keeps step-up). See the amendment in More Information; the short form is that two eyes exist to stop authority being *manufactured*, while revoke only ever *destroys* authority, and putting a second person in front of it means nobody can cut off a compromised delegated admin alone.
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

* Original decision: 2026-06-28 (Option 2). The initial capability taxonomy (proposed inheritable: `manage_users`, `manage_clients`, `manage_scopes`, `view_audit`, `view_config`; forbidden-cascade: `delete_tenant`, `data_export`, `iam_change`, `re_delegate`) and the timing of any ReBAC adoption await Security/DPO ratification. The identifiers are lowercase snake_case because they are stored values in `CapabilityCatalog` and appear in policy names and the capability attribute, not prose (ADR-0065).
* **Amendment, 2026-08-01: dual-control on grant *revoke* is removed; grant *create* keeps it.** As originally written this ADR required dual-control on both, and the design layer had already drifted away from it in one place: the admin-API design justifies single-actor membership removal "for the same reason grant revoke is not [dual-control]", a premise this ADR contradicted. Resolved in favour of the asymmetry, on the operational argument rather than on which document was older. Requiring two people to revoke means **no single responder can cut off a delegated admin whose account is compromised**, and an attacker holding a hijacked admin session is one of the two people whose approval is being waited on. Revoke is also the one direction that cannot escalate: it only reduces privilege. Create is the opposite on both counts, and stays dual-controlled. Revoke keeps **step-up**, and the distinction is the point: dual-control costs a second *person*, step-up costs the responder *seconds*; dropping step-up as well would let an attacker on a hijacked session revoke the responders coming to stop them, turning a safety control into an attack tool. This asymmetry is a security posture change and is listed for Security ratification alongside the capability taxonomy above. The design corpus reports that Azure RBAC supports different conditions for **add** versus **remove** on a role assignment, which would be independent support for the asymmetry; that claim is **not verified here** and is deliberately not relied on, because the operational argument above does not need it.
* **Not changed by that amendment, and still an open gap:** the child-cannot-exceed-parent ceiling this ADR declines to implement in v1 (see the inheritance bullet in the decision). The corpus this repository tracks has since implemented one; whether Nami adopts it is a separate decision, not folded in here, because it changes what this ADR decided rather than correcting what it said.
* Research evidence: identity platforms are almost all flat tenants with explicit membership (Auth0 does not support sub-organizations; Okta treats orgs as hard boundaries with Org2Org/Aerial for time-bound delegation; WorkOS and FusionAuth are flat; ABP leaves hierarchy to the implementer, with the host as super-admin via `ICurrentTenant.Change`). Native-hierarchy exceptions include Frontegg (sub-accounts with opt-in role cascade) and Cerbos (scoped policy where a child only narrows). Microsoft's M&A guidance defaults to consolidating into one tenant, with delegated admin via Administrative Units plus PIM (AUs do not nest) or cross-tenant sync (linking, not nesting); GDAP replaces all-or-nothing with granular, scoped, time-bound admin. Cloud IAM: Azure and GCP inherit grants downward by default (additive, deny-override), whereas AWS does not (SCPs only restrict; cross-account access is an explicit assumable role), making AWS a stronger tenant-isolation reference. Authorization models: ReBAC (Zanzibar/OpenFGA `tuple_to_userset`, SpiceDB's `parent->admin` arrow) is the modern way to model hierarchy plus inheritance, while NIST hierarchical RBAC is role-to-role rather than a resource tree. OWASP Multi-Tenant guidance and CWE-441 warn about privilege escalation and the confused-deputy problem.
* Deferred to a post-v1 wave (proposed, no ADR yet): groups (distinct from roles) and attribute groups, built over the membership model, sized small-to-medium, with the trigger being a customer that models group-based access beyond roles. Carried over from the source project's v2 backlog and production-readiness register, where it is a post-v1 minor item rather than a gap. The coarse `roles` claim v1 puts in the token is compatible with that later move: RFC 9068 section 2.2.3.1 treats `groups`, `roles`, and `entitlements` (SCIM, RFC 7643) as the interchangeable authorization-claim family, and Microsoft's own guidance prefers application roles over group identifiers in tokens, so adding groups later is an additive claim, not a change to the ones already issued.
* Related decisions: ADR-0001 (flat tenants, explicit membership, `ParentTenantId`), ADR-0008 (audit provenance), ADR-0009 (dual-control), ADR-0047 (the authorization engine that evaluates these grants). The detailed enforcement lives in a separate delegated-admin enforcement design document, which is not ADR-0017.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. The source's example acquisition scenario used specific company names; these are replaced here with generic parent/subsidiary placeholders.
