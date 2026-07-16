# Architecture Decision Records

Nami's architecture was designed decision-first: every significant choice is recorded as an ADR with its context, the options considered, and the rationale. Accepted ADRs are binding until superseded.

Format: [MADR 4.0.0](https://adr.github.io/madr/), full template (see [ADR-0000](0000-use-markdown-architectural-decision-records.md)). Files are named `NNNN-short-title-with-dashes.md`. ADRs `0001`-`0035` are being imported and translated from the original design corpus, keeping their original numbering one-to-one; new decisions continue from `0036`.

## Index

| ADR | Title | Status |
|---|---|---|
| [0000](0000-use-markdown-architectural-decision-records.md) | Use Markdown Architectural Decision Records (MADR) with the full template | accepted |
| [0001](0001-multi-tenant-isolation-model.md) | Tiered multi-tenant isolation: global identity, pooled tenant data by default, silo on demand | accepted |
| [0002](0002-federation-external-idp-integration.md) | Integrate external identity providers through ASP.NET Core Identity external login | accepted |
| [0003](0003-server-side-sessions-are-core.md) | Server-side session store is a core feature, not an option | accepted |
| 0004-0035 | _importing from the design corpus..._ | |
