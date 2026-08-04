---
description: Run the full local docs gate (guardrail, name scrub, markdown lint) and report what CI will see
allowed-tools: Bash(git status:*), Bash(git ls-files:*), Bash(bash scripts/hooks/pre-commit), Bash(bash scripts/check-adrs.sh), Bash(npx --yes markdownlint-cli2@0.23.1:*)
---

# Local docs gate

Run every check that gates a docs change, in the order that makes their output readable, and
report the result. **Do not fix anything, and do not stage anything in this command.** It exists
to tell the truth about the current tree, and a command that mutates the tree it is measuring
cannot do that.

## Why this is not just `git commit`

The pre-commit hook (`scripts/hooks/pre-commit`) already runs the guardrail and the local name
scrub. But it runs them **after** a commit message is written, and it does **not** run
markdownlint, which is a CI gate. So a change can pass every local hook and still fail CI on lint.
This command closes that gap before the message is written.

The hook stays free of markdownlint on purpose. It is pure bash with no network and no package
download, and adding an `npx` step would put a multi-second fetch in front of every commit. The
lint belongs in a command you choose to run.

## Steps

1. **Show what is in play.**

   ```bash
   git status --short
   ```

2. **Run the guardrail and the name scrub together, exactly as the commit will.**

   ```bash
   bash scripts/hooks/pre-commit
   ```

   This runs `scripts/check-adrs.sh` and then, if `scripts/.local/name-denylist` exists, the local
   scrub over **staged** markdown. Two things to read carefully in its output:

   - A `coverage warning:` line means untracked markdown was **not read**. So the verdict below
     it, pass or fail, says nothing about those files. Report the filenames, and say plainly that
     they are unchecked. Do not run `git add` to make the warning go away.
   - The name scrub only sees **staged** markdown. If nothing is staged, it ran over nothing. Say
     so rather than reporting it as a pass.

3. **Run markdown lint on the same file set CI reads.**

   ```bash
   npx --yes markdownlint-cli2@0.23.1 "**/*.md"
   ```

   The pinned version is not a preference: it is the version bundled by the SHA-pinned
   action in `ci.yml` (ADR-0086). `.markdownlint-cli2.jsonc` sets `gitignore: true`, so this reads
   tracked files only, and the count should match CI's `Linting: N files` line. If the count is far
   higher than the number of tracked `.md` files, that config is not being picked up, and the extra
   files are git-ignored drafts.

4. **Report, and be specific about coverage rather than only about pass or fail.**

   State the guardrail verdict. State whether a coverage warning fired, and for which files. State
   whether the name scrub had anything staged to look at. State the lint file count with its issue
   count.

   If everything passed, say what that does and does not cover. It does not cover whether a
   citation that resolves actually supports its claim, which no tool here checks.
