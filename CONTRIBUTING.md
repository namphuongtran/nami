# Contributing to Nami

Thanks for your interest! Nami is early-stage and being built in public, which means early contributors have outsized influence. All contributions are welcome: code, docs, samples, issue triage, and design review of the [ADRs](docs/adr/).

## Ground rules

- Be respectful: we follow the [Code of Conduct](CODE_OF_CONDUCT.md).
- Security vulnerabilities go through [SECURITY.md](SECURITY.md), never public issues.
- Significant design changes start with a discussion or an ADR proposal, not a surprise PR. The ADRs in `docs/adr/` (import in progress) are the project's decision record; changing a decided behavior means revisiting its ADR.

## Knowledge base

The [knowledge base](docs/kb/) records lessons, how-things-work notes, and
research that are worth keeping but are not decisions. Write a decision as an
[ADR](docs/adr/); write durable knowledge as a KB note. See
[docs/kb/README.md](docs/kb/README.md) for the boundary and the note format.

## Developer Certificate of Origin (DCO)

We use the [DCO](https://developercertificate.org/) instead of a CLA. Sign off every commit:

```bash
git commit -s -m "feat: add device flow backoff"
```

This adds a `Signed-off-by:` line certifying you have the right to submit the contribution under Apache-2.0.

## Commit style

We use [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `build:`, `ci:`, `chore:`. The changelog is generated from these.

## Pull request checklist

- [ ] Commits are DCO signed-off and follow conventional commits
- [ ] Tests cover the change (this project treats protocol/security code as test-first)
- [ ] Public API changes update the `PublicAPI.Unshipped.txt` of the affected package (once packages exist)
- [ ] New dependencies are permissive OSS (MIT/Apache-2.0/BSD-class) only; no copyleft, source-available, or commercial packages
- [ ] Docs updated where behavior changed

## Development setup

Requires the .NET 10 SDK and Docker (for PostgreSQL testcontainers). Full instructions will land with the first code drop; until then, the repo is docs and scaffolding.

## Questions?

Open a [discussion](https://github.com/namphuongtran/nami/discussions) or an issue with the question template.
