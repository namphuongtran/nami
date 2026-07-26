# Nami documentation

| Section | Contents |
|---|---|
| [`architecture/`](architecture/) | Software Architecture Document (SAD): 24 files built from the ADRs, covering the arc42 template's twelve sections in order, with C4 context/container/component views, runtime and data views, quality and operational views, and the stakeholder-concern correspondence ISO/IEC/IEEE 42010 asks for |
| [`design/`](design/) | Detailed per-feature designs elaborating how each part is built; governed by the ADRs |
| [`adr/`](adr/) | Architecture Decision Records: every significant decision, with context and rationale |
| [`architecture/24-glossary.md`](architecture/24-glossary.md) | Domain, protocol, and project-convention vocabulary used across all three layers; definitions that point at the document of record rather than restating it. It sits inside the architecture layer because arc42 places the glossary there, not because the vocabulary is that layer's alone |
| [`PRE-GA-RATIFICATION-CHECKLIST.md`](PRE-GA-RATIFICATION-CHECKLIST.md) | The release gate: every DPO/Security/Ops/Legal/Product sign-off the ADRs defer before general availability |
| `DEPENDENCY-LICENSES.md` | Third-party dependency license inventory (created with the first code drop) |

Getting-started guides, concept docs, configuration reference, and the full docs site (DocFX) will be added as implementation progresses. The ADRs are the best place to understand the architecture today.
