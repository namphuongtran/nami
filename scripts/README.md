# scripts

## check-adrs.sh

Neutral ADR/docs hygiene checks, run in CI (`.github/workflows/ci.yml`) and locally:

- template placeholders (the curly-brace `Product` / `Company` / `domain` tokens) must not appear in tracked markdown;
- every `ADR-NNNN` reference in `docs/adr/` resolves to a `docs/adr/NNNN-*.md` file;
- the ADR index in `docs/adr/README.md` matches the files, and each ADR's frontmatter `status:` matches its index row;
- every ADR marked `stack-record: true` in its frontmatter appears in the ADR-0061 stack-of-record table, and every ADR cited in that table carries the marker (bidirectional);
- no em dash appears in tracked markdown (project style rule): use a comma, colon, or parentheses. The check builds the pattern from the codepoint, so this script stays pure ASCII and cannot fail against itself.

Run locally:

```bash
bash scripts/check-adrs.sh
```

## Pre-commit hook (opt-in, maintainers)

Enable once per clone:

```bash
git config core.hooksPath scripts/hooks
```

The hook runs `check-adrs.sh`. In addition, if you create a local, git-ignored
`scripts/.local/name-denylist` (one term per line; `#` comments and blank lines
ignored), the hook blocks a commit that introduces any of those terms in staged
markdown. That file lives under the git-ignored `scripts/.local/` directory and
is never committed, so nothing sensitive is published.

Terms are matched case-insensitively as whole words; use plain names and avoid
regular-expression metacharacters.

An optional companion file, `scripts/.local/name-allowlist`, exempts exact
identifiers that legitimately contain a denied term. The motivating case is an OSS
package the project actually depends on: a dependency record that hides the package
identifier is factually wrong and cannot drive the license-scan gate of
[ADR-0026](../docs/adr/0026-dependency-license-policy.md), while product comparison
and rejected commercial packages must still be generalized. For each denied term the
hook blanks every allowlisted identifier out of the matched lines and re-tests, so a
line that matched only because of an allowed identifier passes, and a line that also
carries a genuine mention still blocks. Allowlist entries are matched
**case-sensitively**: write package identifiers in their canonical casing, which is
also what the license scan needs.

## review/design-pointer-audit.py (review aid, not a gate)

The detailed-design layer cites sibling documents by bare number in prose ("DPoP
internals (06)", "owned by 13"). No link checker sees those, so a renumber leaves them
pointing at documents that still exist and are now the wrong ones. This script dumps
every such pointer next to the title it resolves to, for a human to read.

It is deliberately **not** part of `check-adrs.sh`. Whether `(06)` should have been
`(14)` depends on the topic, not the number, so a gate here would pass on exactly the
bug it was written for and turn an unchecked claim into a confident one. The one
mechanical thing it does report is a pointer to a number with no row in the design
index, which is always an error.

```bash
python3 scripts/review/design-pointer-audit.py            # whole layer
python3 scripts/review/design-pointer-audit.py docs/design/04-core-protocol.md
```
