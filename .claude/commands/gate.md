---
description: Run the local gates and report what CI will see. Docs gates by default, all nine with the "all" argument
allowed-tools: Bash(git status:*), Bash(git ls-files:*), Bash(bash scripts/hooks/pre-commit), Bash(bash scripts/check-adrs.sh), Bash(npx --yes markdownlint-cli2@0.23.1:*), Bash(bash scripts/test-check-adrs.sh), Bash(bash scripts/test-editorconfig.sh), Bash(bash scripts/test-public-api-gate.sh), Bash(bash scripts/test-warnings-as-errors.sh), Bash(dotnet build:*), Bash(dotnet test:*), Bash(dotnet format:*), Bash(dotnet --version)
argument-hint: "[all]"
---

# Local gate

Run every check that gates a change, in the order that makes their output readable, and
report the result. **Do not fix anything, and do not stage anything in this command.** It exists
to tell the truth about the current tree, and a command that mutates the tree it is measuring
cannot do that.

## Scope, which the argument selects

**With no argument, run steps 1 to 4 only.** Those are gates 1, 2, and 3 of the nine: markdownlint,
`check-adrs.sh`, and the decisions index. They need no SDK and finish in seconds, which is why they
are the default for a docs change.

**With the argument `all`, also run steps 5 to 8**, which are gates 4 to 9. Six of those need the
.NET SDK, so they are slower and three of them **skip** without it. See step 8.

The nine-gate table, with the `ci.yml` line each gate runs at, is in
[`../skills/adding-a-ci-gate/SKILL.md`](../skills/adding-a-ci-gate/SKILL.md) and is not repeated
here. Read it when a gate's identity or wiring is the question. This command is about running them
and reading the output.

**Two different nines exist.** These nine are gates. `scripts/README.md` separately enumerates nine
**checks inside `check-adrs.sh`**, which are all one gate, number 2.

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

2. **Run the guardrail, the decisions index, and the name scrub together, exactly as the commit
   will.**

   ```bash
   bash scripts/hooks/pre-commit
   ```

   This runs three things in order: `scripts/check-adrs.sh` (`pre-commit:11`),
   `scripts/check-decisions-index.py` (`pre-commit:17`), and then, if
   `scripts/.local/name-denylist` exists, the local scrub over **staged** markdown
   (`pre-commit:24-48`). Three things to read carefully in its output:

   - A `coverage warning:` line means untracked markdown was **not read**. So the verdict below
     it, pass or fail, says nothing about those files. Report the filenames, and say plainly that
     they are unchecked. Do not run `git add` to make the warning go away.
   - A line reading `python3 not found, skipping the decisions-index check` means gate 3 did not
     run. The hook skips rather than fails on purpose (`pre-commit:13-15`), because CI is the
     authority. **Report it as a skip, never inside a pass.**
   - The name scrub only sees **staged** markdown. If nothing is staged, it ran over nothing. Say
     so rather than reporting it as a pass. It also reads a git-ignored denylist, so on a clone
     that never created one it does nothing at all.

3. **Run markdown lint on the same file set CI reads.**

   ```bash
   npx --yes markdownlint-cli2@0.23.1 "**/*.md"
   ```

   The pinned version is not a preference: it is the version bundled by the SHA-pinned
   action in `ci.yml` (ADR-0086). `.markdownlint-cli2.jsonc` sets `gitignore: true`, so this reads
   tracked files only, and the count should match CI's `Linting: N files` line. If the count is far
   higher than the number of tracked `.md` files, that config is not being picked up, and the extra
   files are git-ignored drafts.

4. **If the argument was not `all`, report now and stop.** Go to step 9, and say which six gates
   were not run.

   Otherwise continue. Steps 5 to 8 add gates 4 to 9, and they run in the order written below.
   That is **not** `ci.yml`'s order, which is parallel jobs. It is cheapest first, so a fast
   failure arrives before a slow one.

5. **Gate 4, the guardrail self-test.** The only one of the four self-tests that needs no SDK.

   ```bash
   bash scripts/test-check-adrs.sh
   ```

   **Its green covers Checks 8 and 9 only**, which `scripts/README.md` states in its own section
   for this script. Checks 1 to 7 of the guardrail have no self-test. So they are proven by the
   tree happening to contain violations, and a clean tree has nothing to match.

6. **Confirm an SDK is present before the six that need one.**

   ```bash
   dotnet --version
   ```

   If this fails, stop and say that gates 5 to 9 were not run. Do not report the earlier greens as
   a build verdict.

7. **Gates 5, 6, and 7, the three SDK self-tests.**

   ```bash
   bash scripts/test-editorconfig.sh
   bash scripts/test-public-api-gate.sh
   bash scripts/test-warnings-as-errors.sh
   ```

   **Each prints `SKIPPED ... This is a skip, not a pass` and exits 0 when `dotnet` is absent**
   (`scripts/test-editorconfig.sh:69-73`, `scripts/test-public-api-gate.sh:61-65`,
   `scripts/test-warnings-as-errors.sh:80-84`). An exit 0 is therefore two different outcomes.
   Read the text, not the code, and repeat the script's own wording when it skipped.

   **A red here means the gate stopped biting, not that the code is wrong.** CI runs each in its
   own job for that reason. Say which of the two happened.

8. **Gates 8 and 9, the build and the tests.**

   ```bash
   dotnet build Nami.Identity.slnx -t:Rebuild --nologo
   dotnet format Nami.Identity.slnx --verify-no-changes
   dotnet test Nami.Identity.slnx --nologo
   ```

   `-t:Rebuild` is deliberate, and it is **stricter than CI**, which runs a plain
   `dotnet build Nami.Identity.slnx --nologo` (`ci.yml:158`). CI can afford the plain form because
   a fresh runner has no `obj/` to reuse. A local tree does. A change that moves only an MSBuild
   property moves no compilation input, so an incremental build can skip the compiler and report
   the previous run's result, and that reused silence reads exactly like a pass.
   `scripts/test-warnings-as-errors.sh:51` states the same rule as guidance, and its `:348`
   records a 2026-08-03 measurement taken with the flag. The script itself does not build: part 6
   reads properties with `dotnet msbuild -getProperty:`, which evaluates instead.

   Two skip traps to read for, because both look like a green suite:

   - A test project that omits `TestingPlatformDotnetTestSupport` is **skipped** by `dotnet test`
     rather than failing it (`tests/CLAUDE.md:69-71`). A suite that quietly runs nothing looks like
     a suite that passes, so check the reported test count against the suites you expect.
   - Report the test count. Two test projects existed on 2026-08-08,
     `Nami.Identity.ArchitectureTests` and `Nami.Identity.UnitTests`, and the run prints one line
     per assembly with no combined total. A run reporting fewer assemblies than that is a
     finding, not a pass.

9. **Report, and be specific about coverage rather than only about pass or fail.**

   State the guardrail verdict. State whether a coverage warning fired, and for which files. State
   whether the decisions-index check ran or was skipped. State whether the name scrub had anything
   staged to look at. State the lint file count with its issue count.

   For an `all` run, add: which self-tests genuinely ran and which skipped, and the test count.
   List every gate that did not run.

   If everything passed, say what that does and does not cover. **Three limits are worth naming
   every time**, because none is visible in a green:

   - It does not cover whether a citation that resolves actually supports its claim. No gate here
     reads one. [`../skills/checking-a-citation/SKILL.md`](../skills/checking-a-citation/SKILL.md)
     is the procedure that stands in for the missing gate.
   - It does not cover a `file:line` pointer that has aged since it was written. Run
     `/refresh-citations` for that.
   - Guardrail Checks 1 to 7 have no self-test, so their green is a property of this tree rather
     than evidence that the checks still bite.
