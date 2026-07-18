---
status: "accepted"
stack-record: true
date: 2026-07-18
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and maintainer
consulted: OSS generative-AI contribution practice (Apache `Generated-By`, OpenInfra `Assisted-By`/`Generated-By`, the Linux kernel accountability stance), verified 2026-07-18; the governance and hygiene ADRs (0046, 0026, 0062, 0060, 0045) and SECURITY.md
informed: all contributors, via this repository
---

# Adopt an AI-assisted development policy: human-accountable, disclosed, license- and security-hygienic

## Context and Problem Statement

Nami is built in public, invites outside contribution, and is itself developed with AI assistance (its own commits carry an AI co-author trailer). Contributors will use AI coding tools whether or not the project says anything. Yet no ADR states whether AI-assisted contributions are allowed, on what terms, how they are disclosed, or how they stay compatible with the three things the project already guards: the DCO and clean provenance (ADR-0046), the permissive-only license line (ADR-0026), and the security and test bar for a security product (ADR-0062/0060).

For a security-critical identity provider with a "verified, not vibes" ethos, an unstated AI policy is a real governance gap: provenance can blur, a copyleft or vendor-copyright-retained snippet can slip in, or a plausible-but-wrong AI change can pass a relaxed review. The OSS field has largely settled on allow-with-accountability plus disclosure rather than banning. This ADR adopts that stance for Nami and wires it into the existing governance and hygiene gates. It governs how the project is built; it does not govern Nami's runtime.

## Decision Drivers

* Keep human accountability and provenance clean: the DCO assumes a responsible human, and that must not erode.
* Protect the permissive-only line from AI-introduced copyleft or vendor-retained-copyright code (ADR-0026).
* Hold the security and quality bar for an IdP; AI must not lower or replace review (ADR-0062/0060).
* Transparency the community now expects, at low friction, without banning a tool contributors already use.
* Honesty: the project uses AI itself, so its policy should be explicit, not tacit.

## Considered Options

* Ban AI-generated contributions outright.
* Allow AI assistance with human accountability, mandatory disclosure, and license and security hygiene.
* Allow AI assistance silently, with no stated policy.

## Decision Outcome

Chosen: "allow with accountability, disclosure, and hygiene." Banning is rejected (unenforceable and out of step with practice); silent allowance is rejected (it is the status quo gap this ADR exists to close).

### Human accountability (binding)

AI tools are permitted. The DCO sign-off (ADR-0046) is unchanged and applies regardless of tool: signing off certifies the contributor has the right to submit the work under Apache-2.0 and takes responsibility for it. A contributor must understand and be able to explain and debug any AI-assisted code they submit; "the AI wrote it" is never a defense in review. AI is never a contributor of record for accountability, a human always is.

### Disclosure (binding)

Adopt the two-tier commit-trailer convention, aligning to the Apache and OpenInfra standard: `Assisted-By: <tool>` for assistive use (completion, small suggestions, refactoring help), and `Generated-By: <tool>` when a substantial portion of the change is AI-generated. Existing `Co-Authored-By:` trailers in the history are retained (no history rewrite); going forward the AI-disclosure signal is the `Assisted-By`/`Generated-By` trailer, and a commit may carry both a co-author line and the disclosure trailer.

### License hygiene (binding)

AI output must pass the permissive-only policy (ADR-0026): no copyleft or otherwise non-permissive code reproduced from training data, and no code whose copyright the tool vendor retains in a way incompatible with Apache-2.0. The CI license-scan gate backstops dependencies; for AI-authored source, the contributor performs the due diligence and the reviewer checks it, and suspected verbatim reproduction of third-party code is rejected.

### Security and quality bar (binding)

AI-assisted code meets the same bar as any code: the testing strategy and behavior-first tests (ADR-0060), the OWASP ASVS baseline (ADR-0062), the architecture tests (ADR-0024), and the public-API rules (ADR-0044). Because Nami is a security product, security-relevant AI-generated code receives more review scrutiny, not less; AI accelerates authoring, it does not substitute for review.

### No secrets or private data into tools (binding)

Contributors must not paste secrets, credentials, key material, customer data, or embargoed security-fix details into AI tools, consistent with SECURITY.md. Embargoed vulnerability work under coordinated disclosure (ADR-0045) is handled without third-party AI tools.

### Scope and ratification

This governs contributions to the codebase, docs, and ADRs. It does not govern Nami's runtime (Nami is not an AI product), and the MCP authorization work (ADR-0064) is a separate, product-facing matter. The IP and copyright dimension of AI-generated contributions is unsettled across the industry, so the standing pre-GA IP-lawyer review confirms the DCO-plus-disclosure approach for the project's distribution model before public launch; the policy mechanism is adopted now.

### Consequences

* Good, because contributors get a clear, low-friction rule that keeps provenance clean, and the project is honest about its own AI use.
* Good, because it reuses the existing gates (DCO, license-scan, ASVS, tests, arch tests) rather than inventing new machinery; AI code simply meets the bar that already exists.
* Good, because disclosure trailers make AI involvement visible in history and align to the emerging OSS standard.
* Bad, because disclosure and due-diligence rely on contributor honesty and reviewer attention rather than a hard gate; mitigated by the DCO's legal weight, review, and the license-scan backstop.
* Bad, because the "substantial portion" threshold for `Generated-By` is a judgment call; accepted, and resolved by erring toward disclosure.

## Pros and Cons of the Options

### Ban AI-generated contributions

* Good, because it appears to sidestep the provenance and copyright questions.
* Bad, because it is unenforceable, out of step with current practice, and would reject useful contributions while the project itself uses AI.

### Allow with accountability, disclosure, and hygiene (chosen)

* Good, because it keeps a human accountable, discloses AI involvement, and routes AI code through the existing license and security gates.
* Bad, because it leans on honesty and judgment at the margins; mitigated by the DCO, review, and the license-scan backstop.

### Allow silently

* Good, because it needs no work.
* Bad, because it leaves provenance, licensing, and disclosure unaddressed, which is precisely the gap this ADR closes.

## More Information

* Related decisions: ADR-0046 (the DCO-based governance this extends), ADR-0026 (the permissive-only license policy AI output must pass), ADR-0062 and ADR-0060 (the security baseline and testing bar AI code must meet), ADR-0024 and ADR-0044 (architecture tests and public-API rules), ADR-0045 (coordinated disclosure and the embargoed-work exclusion), and ADR-0064 (the separate, product-facing MCP matter). SECURITY.md carries the no-secrets rule.
* Best-practice references, named factually: Apache's `Generated-By` label (2023), OpenInfra's mandatory `Assisted-By`/`Generated-By` two-tier labels, and the Linux kernel's contributor-accountability stance.
* This ADR is deliberately honest that Nami itself is developed with AI assistance; recording the policy is an application of the project's provenance-first principle, not an afterthought.
* Authored fresh for this repository.
