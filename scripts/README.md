# scripts

## check-adrs.sh

Neutral ADR/docs hygiene checks, run in CI (`.github/workflows/ci.yml`) and locally:

- template placeholders (the curly-brace `Product` / `Company` / `domain` tokens) must not appear in tracked markdown;
- every `ADR-NNNN` reference in **any** tracked markdown resolves to a `docs/adr/NNNN-*.md` file, not only those written inside `docs/adr/`, because the architecture and design layers cite far more ADR numbers than the ADRs themselves do;
- the ADR index in `docs/adr/README.md` matches the files, and each ADR's frontmatter `status:` matches its index row;
- every ADR marked `stack-record: true` in its frontmatter appears in the ADR-0061 stack-of-record table, and every ADR cited in that table carries the marker (bidirectional);
- no em dash appears in tracked markdown (project style rule): use a comma, colon, or parentheses. The check builds the pattern from the codepoint, so this script stays pure ASCII and cannot fail against itself;
- no design-corpus test identifier appears in tracked markdown: the `9.T`, `8.K` and `25.T` families point into a numbered test register this repository does not have, so an obligation is stated by what it asserts and listed in `docs/design/20-testing.md` instead. The families are named by prefix here on purpose, because writing a whole identifier would trip this very check; `docs/adr/README.md` carries the full convention and the reason it is enforced;
- every ADR has a row in the architecture layer's reverse index, `docs/architecture/18-decisions-index.md`, and every row there resolves to a file (bidirectional). This is a second index, and the first one passing says nothing about it: nine ADRs had drifted out of this one while every other check was green. Membership only, never the "Views that cite it" column, which is regenerated from the views themselves.

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
