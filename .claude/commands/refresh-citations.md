---
description: Find every file:line pointer this increment aged, and report which were re-derived against the final tree
allowed-tools: Bash(git diff:*), Bash(git log:*), Bash(git status:*), Bash(git grep:*), Bash(git merge-base:*), Read
argument-hint: "[base-ref]"
---

# Refresh the citations this increment aged

Produce the list of pointers this increment may have aged, and report what opening them found.
**Do not edit anything in this command.** Finding the drift and repairing it are two jobs, and
running them together hides how much drifted.

The base ref is the argument, or `origin/main` when there is no argument.

This command is mechanism only. The judgement belongs to the `checking-a-citation` skill, which
owns why no gate catches this, the four defect classes, and the three habits that decide the order
of work. None of that is repeated here. Read it before acting on anything this command prints.

## What this command screens, measured

Counted on 2026-08-07 at `10df955`, with the pattern in step 2 over tracked markdown: **244**
pointers across **183** files. **174** of them sit in five instruction files written in the days
before that count, so the exposure is concentrated and recent rather than spread evenly.

That is one count on one day, not a live total. The pattern is written into step 2 so a later
count can be re-run rather than quoted forward.

## Steps

1. **List the files this increment touched.**

   ```bash
   git diff --name-only $(git merge-base HEAD origin/main)...HEAD
   ```

   Use the argument in place of `origin/main` when one was given. This is the set that can have
   aged a pointer. A file edited by any commit in the range counts, including one touched only to
   fix something else.

2. **List every pointer in tracked markdown.**

   ```bash
   git grep -nE '([A-Za-z0-9_.\-]+\.(md|cs|csproj|sh|py|props|json|jsonc|yml|slnx|txt)|ADR-[0-9]{4}|\.editorconfig):[0-9]+([-,][0-9]+)*' -- '*.md'
   ```

   The pattern covers both forms the repository uses: a filename with a line suffix, and a
   four-digit ADR reference with one. Neither form matches the other, so the two counts add.

3. **Intersect, and open the result.**

   Keep the pointer lines that name a file step 1 listed. Two sets fall out, and the second is the
   one that gets missed. The first is the pointers this increment **wrote**. The second is the
   pointers **anywhere in the repository** that aim into a file this increment touched.

   Open every pointer in the intersection. Screening by which ones look suspect is what the
   `checking-a-citation` skill rules out, and it is the habit this command exists to replace.

4. **Report three counts, not one total.**

   | Kind | What it means | What the report gives |
   |---|---|---|
   | Drifted | The line moved, the target still exists | The old pointer, the new one, and a quoted phrase from the target |
   | Gone | The pointer aims into something this increment deleted | Prose, not a new number |
   | Re-confirmed | Opened, and correct | The count, because a report listing only failures does not say how much was read |

   Then state the coverage. Name the pointers that were **not** opened, and why. Say plainly that
   a pointer landing on the right line is not evidence that the line supports the claim. Nothing
   here checks that, and neither does any gate.

$ARGUMENTS
