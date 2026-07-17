---
title: Markdownlint gotchas in ADRs (MD036, MD033, MD040)
tags: [docs, markdownlint, ci, adr]
created: 2026-07-17
related: [[0009-secret-store-access-and-rollover]], [[0021-openiddict-version-adaptation]]
---

The `Docs lint` CI job runs `markdownlint-cli2` over `**/*.md`. Three rules
have bitten us while importing ADRs. Each one below shows the failing shape and
the fix.

## MD036: emphasis used instead of a heading

A line that is *entirely* bold (or italic) is read as a fake heading and fails.

- Fails: a line whose only content is `**B. Client-secret rollover (zero-downtime)**`.
- Passes: move any trailing text outside the bold, e.g.
  `**B. Client-secret rollover** (zero-downtime)`, or use a real heading.

A bold label followed by plain text on the same line does not trip the rule,
which is why `**A. Store access model** (aligned with ADR-0006)` passes.

## MD033: inline HTML

An angle-bracket token such as a `<version>` placeholder is parsed as HTML: it
fails the rule and the renderer eats it. Wrap the literal in backticks.

- Fails: a raw placeholder like `<version>` written directly in prose.
- Passes: the same placeholder in inline code, e.g. the marker
  `replace-when-native: OpenIddict <version>`.

## MD040: fenced code blocks need a language

Every fenced code block needs a language tag. Use `text` for plain diagrams and
directory trees, `bash` for shell, `yaml` for frontmatter.

## Verify locally before pushing

```bash
npx --yes markdownlint-cli2@0.23.0 "**/*.md"
```

Expect `Summary: 0 error(s)`. Running the pinned version matches CI exactly.
