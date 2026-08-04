---
description: Check the architecture decisions index against the views, and report or apply the drift
allowed-tools: Bash(python3 scripts/check-decisions-index.py:*), Bash(git diff:*), Read, Edit
argument-hint: "[fix]"
---

# Decisions index check

`docs/architecture/18-decisions-index.md` answers "which views must I re-read when this decision
changes". Its two columns are derived from other files and can drift from them. Guardrail Check 7
checks only that every ADR has a **row**, never what the row says.

## Steps

1. **Compare.**

   ```bash
   python3 scripts/check-decisions-index.py
   ```

   Read the exit code:

   - Exit 0 means the views column matches the numbered views, and every `Decision` cell is its
     ADR's H1 title or a truncation of it.
   - Exit 1 lists each drifted ADR.
   - Exit 2 means the index could not be parsed. That means its row format changed, and it is a
     real problem rather than a tooling glitch.

2. **If it exits 0, say what that does and does not prove.** It proves the column is current. It
   does not prove a view *depends* on a decision it mentions. A mention is any occurrence of the
   number, including one in the view's own `Sources` list. That caveat is the table's stated
   design, not a defect.

3. **If it exits 1, do not hand-edit cells from the failure list.** Regenerate:

   ```bash
   python3 scripts/check-decisions-index.py --print-table
   ```

   This writes nothing. It emits the correct rows for section 2.

4. **Only if the argument was `fix`**, apply those rows to section 2 of the index with Edit, then
   re-run step 1 and show `git diff --stat`.

   Otherwise stop after step 3 and report what would change. The drift itself is often the finding.
   A view that started or stopped citing a decision is a change someone made, and it may be the
   citation that is wrong rather than the table.

$ARGUMENTS
