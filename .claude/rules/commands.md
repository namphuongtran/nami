# Commands

The commands for this repository. [`scripts/README.md`](../../scripts/README.md) is the
authority on what each gate checks and why, and this list is not a summary of it.

```bash
bash scripts/hooks/pre-commit                          # guardrail + decisions index + name scrub
bash scripts/check-adrs.sh                             # docs guardrail; reads the git INDEX, so `git add` first
python3 scripts/check-decisions-index.py               # verifies what each index row says, not just that it exists
bash scripts/test-check-adrs.sh                        # self-test: the guardrail
bash scripts/test-editorconfig.sh                      # self-test: the C# style ruleset
bash scripts/test-public-api-gate.sh                   # self-test: the public-API lock and CPM
bash scripts/test-warnings-as-errors.sh                # self-test: the warning gate and the two analyzer axes
npx --yes markdownlint-cli2@0.23.1 "**/*.md"           # version-coupled to ci.yml, see .claude/rules/build-and-ci.md
dotnet build Nami.Identity.slnx --nologo
dotnet test Nami.Identity.slnx --nologo                # architecture rules (ADR-0024)
dotnet format Nami.Identity.slnx --verify-no-changes   # drop the flag and it fixes

git config core.hooksPath scripts/hooks                # enable the local hook, once per clone
```

This file carries no `paths` field, so it loads in every session. It holds commands only. The
root [`../../CLAUDE.md`](../../CLAUDE.md) holds the rules about these gates. It keeps them
because a rule must survive a `/compact`, and this list does not need to.
