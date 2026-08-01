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
  * **A delegator may only grant what they themselves hold. This grant-time ceiling is the PRIMARY anti-escalation control** (added 2026-08-01; before that, v1 deliberately shipped only the substitutes listed below, and the note in More Information records why that was reconsidered). Evaluated when a grant is created, batched over the requested capability set:

    ```text
    forall c in grant.capabilities : ICheckAccess(actor, c, routeTenant) == Allow
    ```

    It needs no new machinery: `ICheckAccess` is already the authorization port (ADR-0047). A request that fails it is refused **`403`, deliberately not `202`**: asking to grant authority you do not hold is refused structurally rather than placed in front of a human approver, because a reviewer cannot safely approve what the system already knows is escalation. An **expiry ceiling** applies with it: a grant may not outlive the grant it derives from.
  * **Inheritance narrows in effect, not by a DENY rule.** The v1 grant model is still purely **additive**: a grant grants, and there is no scoped DENY row, so v1 does **not** implement a parent-DENY-override, and a **request-time** ceiling in the AWS sense is also out of scope (see the limitation below). Both are ReBAC-era considerations. Alongside the grant-time ceiling above, the narrowing intent is carried by least-privilege capabilities on each grant, forbidden-cascade for dangerous capabilities, and the non-cascading `re_delegate` gate below.
  * **Known limitation, grant-time versus request-time.** The ceiling is evaluated when the grant is created. If the delegator's own grant is later narrowed, grants they already issued keep their capabilities until they expire or are revoked. Kubernetes has the same property, since a RoleBinding survives its creator losing rights; AWS is stronger, because a permissions boundary is evaluated on every request. Closing this needs a non-additive model, so it is a ReBAC-era target rather than a v1 gap to paper over.
* **Dangerous capabilities never cascade**: deleting a tenant, cross-tenant data export, IAM changes, and re-delegation each require a direct grant on that tenant (matching the deployment's dual-control policy and ADR-0009).
* **Dual-control on grant creation is keyed on reach, not on the word "dangerous"** (changed 2026-08-01). A grant creation returns `202` and opens a proposal when **either** the requested capability set contains a no-cascade capability **or** the root tenant has descendants in `TenantClosure`; otherwise `201` direct. The second clause is the one the old keying missed, and the two run opposite to intuition: `delete_tenant` carries the dangerous label yet reaches exactly **one** tenant, while `manage_users` is ordinary yet reaches **every descendant**. ADR-0009 chose dual-control for **blast radius**, and blast radius follows the tenant subtree rather than the label, so keying on the label under-protects the wider action and over-protects the narrower one.
  * **This does not become redundant once the ceiling exists**, which is why both stay. An actor who genuinely holds `manage_users` at a parent tenant **passes** the ceiling for a parent-rooted grant, so the ceiling cannot see that case at all; only clause 2 makes it reviewable. The ceiling stops authority being *created out of nothing*; dual-control stops authority the actor *legitimately holds* from being spread widely by one person. They are different failure modes.
  * **Known limitation:** clause 2 is evaluated at creation time, so a tenant that is a leaf today and gains descendants later turns an existing grant into a subtree grant that never passed two eyes. The `TenantClosure` maintainer is the place to flag that, and it is recorded rather than hidden.
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
* **Amendment, 2026-08-01: dual-control on grant *revoke* is removed; grant *create* keeps it.** As originally written this ADR required dual-control on both, and the design layer had already drifted away from it in one place: the admin-API design justifies single-actor membership removal "for the same reason grant revoke is not [dual-control]", a premise this ADR contradicted. Resolved in favour of the asymmetry, on the operational argument rather than on which document was older. Requiring two people to revoke means **no single responder can cut off a delegated admin whose account is compromised**, and an attacker holding a hijacked admin session is one of the two people whose approval is being waited on. Revoke is also the one direction that cannot escalate: it only reduces privilege. Create is the opposite on both counts, and stays dual-controlled. Revoke keeps **step-up**, and the distinction is the point: dual-control costs a second *person*, step-up costs the responder *seconds*; dropping step-up as well would let an attacker on a hijacked session revoke the responders coming to stop them, turning a safety control into an attack tool. This asymmetry is a security posture change and is listed for Security ratification alongside the capability taxonomy above. Azure RBAC supports different conditions for **add** versus **remove** on a role assignment, which is independent support for the asymmetry; that claim reached this ADR from the design corpus and was recorded here as unverified until it was read at Microsoft's own documentation later the same day (see the vendor evidence below).
* **Amendment, 2026-08-01: the grant-time ceiling is adopted, and the procedural half alone was never the industry shape.** This ADR previously declined the ceiling for v1 and substituted three mechanisms, one of which is the `re_delegate` gate. The reconsideration is not that the substitutes are wrong but that they are all **procedural or scoping** controls, and every comparable system uses a **structural** ceiling as its primary control. Verified at each vendor's own documentation on 2026-08-01, read directly rather than carried from the design corpus that raised the question:
  * **Kubernetes RBAC** ties the ceiling to what the actor already holds: "A user can only create/update a Role (or ClusterRole) if they already possess all the permissions contained in the Role (or ClusterRole), or if they have been explicitly granted permission to perform the `escalate` verb on roles or clusterroles resources", with `bind` as the matching escape hatch for RoleBindings. This is the shape adopted above, because it needs no object Nami does not already have.
  * **AWS IAM** ties it to a **separate** policy object, the permissions boundary, where "the effective permissions are the intersection of both policy types". AWS also openly **accepts** that a delegate may create principals more privileged than the delegate: its own worked example notes that "Zhang's policies allow him to create a user that can then access Amazon S3 resources that he can't access. By delegating these administrative actions, Maria effectively trusts Zhang with access to Amazon S3." So the AWS ceiling is external, not self-referential.
  * **Azure RBAC** ties it to an ABAC condition on the delegation itself, through the lower-privilege *Role Based Access Control Administrator* role rather than *Owner* or *User Access Administrator*. Microsoft's own statement of the problem is this finding almost verbatim: the unconstrained method means "Delegate can assign any role to any user within their scope, including themselves", and "Delegate can assign the Owner or User Access Administrator roles to another user, who can then assign roles to other users", which are the self-grant and re-delegation-chain routes exactly.
  * **None of the three uses two-person approval for this.** Nami's dual-control clauses therefore sit deliberately **above** industry baseline and must be described as supplementary, never as the gate.
* **The create / revoke asymmetry decided earlier today now has independent vendor support, upgrading it from the "not verified here" note recorded with it.** Azure RBAC lists, among its ways to constrain role assignments, "Specify different conditions for the add and remove role assignment actions", and its worked example does exactly what this ADR decided: "Dara can only assign the Backup Contributor or Backup Reader roles. Dara can remove any role assignments." Read at the same source on 2026-08-01.
* Research evidence: identity platforms are almost all flat tenants with explicit membership (Auth0 does not support sub-organizations; Okta treats orgs as hard boundaries with Org2Org/Aerial for time-bound delegation; WorkOS and FusionAuth are flat; ABP leaves hierarchy to the implementer, with the host as super-admin via `ICurrentTenant.Change`). Native-hierarchy exceptions include Frontegg (sub-accounts with opt-in role cascade) and Cerbos (scoped policy where a child only narrows). Microsoft's M&A guidance defaults to consolidating into one tenant, with delegated admin via Administrative Units plus PIM (AUs do not nest) or cross-tenant sync (linking, not nesting); GDAP replaces all-or-nothing with granular, scoped, time-bound admin. Cloud IAM: Azure and GCP inherit grants downward by default (additive, deny-override), whereas AWS does not (SCPs only restrict; cross-account access is an explicit assumable role), making AWS a stronger tenant-isolation reference. Authorization models: ReBAC (Zanzibar/OpenFGA `tuple_to_userset`, SpiceDB's `parent->admin` arrow) is the modern way to model hierarchy plus inheritance, while NIST hierarchical RBAC is role-to-role rather than a resource tree. OWASP Multi-Tenant guidance and CWE-441 warn about privilege escalation and the confused-deputy problem.
* Deferred to a post-v1 wave (proposed, no ADR yet): groups (distinct from roles) and attribute groups, built over the membership model, sized small-to-medium, with the trigger being a customer that models group-based access beyond roles. Carried over from the source project's v2 backlog and production-readiness register, where it is a post-v1 minor item rather than a gap. The coarse `roles` claim v1 puts in the token is compatible with that later move: RFC 9068 section 2.2.3.1 treats `groups`, `roles`, and `entitlements` (SCIM, RFC 7643) as the interchangeable authorization-claim family, and Microsoft's own guidance prefers application roles over group identifiers in tokens, so adding groups later is an additive claim, not a change to the ones already issued.
* Related decisions: ADR-0001 (flat tenants, explicit membership, `ParentTenantId`), ADR-0008 (audit provenance), ADR-0009 (dual-control), ADR-0047 (the authorization engine that evaluates these grants). The detailed enforcement lives in a separate delegated-admin enforcement design document, which is not ADR-0017.
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. The source's example acquisition scenario used specific company names; these are replaced here with generic parent/subsidiary placeholders.
