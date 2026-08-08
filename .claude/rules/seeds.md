# Seeds, and how to plan work here

This file carries no `paths` field, so it loads in every session. It holds the standing rule for
how work is planned and how it is explained. [`../../docs/SEEDS.md`](../../docs/SEEDS.md) is the
tracker itself, and this file is not a summary of its contents.

Adopted 2026-08-08, on the maintainer's instruction, after an increment landed as two commits
touching twenty files at once. Nothing was wrong with the result. What was wrong is that no other
agent could have picked the work up halfway, because the plan lived in one conversation.

## The rule, in one paragraph

**Work is planned as seeds.** A seed is one issue, scoped to what a single agent can finish in one
sitting, carrying enough context to be worked without reading the conversation that produced it.
Seeds are **forward chained**: each one names what blocks it and what it unblocks, so a reader can
look at the set and see which seeds are actionable right now without reasoning about the rest.
Never write one large issue where three small ones would do, and never open a pull request that
closes more than one seed unless the seeds are the same change.

## What each of the four words means, because they are doing real work

**Detailed** means a second agent does not have to re-derive what you already read. Name the file
and the line for every claim the seed rests on. If the seed depends on a measurement, put the
number and the date in the seed, not a pointer to a conversation.

**Declarative** means the seed states the **end state** and how to **check** it, not a list of
keystrokes. Write "`Directory.Packages.props` carries `[7.6.0]` on all eight OpenIddict rows and no
`[7.5.0]` remains" rather than "edit the manifest". The reason is that a procedure goes stale the
moment the tree moves, while an end state stays checkable. A seed whose verification cannot be run
is not finished being written.

**Forward chained** means the dependency edges are written down in both directions. `Blocked by`
lets an agent skip a seed it cannot start. `Unblocks` lets the agent who finishes one know what
became available, which is the half that is usually left out and the half that keeps the set moving.
A seed with no `Blocked by` entry is claiming to be actionable today, so that claim has to be true.

**Single-agent-sized** is the hardest one to hold and the easiest to check. The test is: can one
agent reach the seed's end state, run its verification, and commit, without a second decision from
the maintainer? If the honest answer is no, the seed contains a decision, and the decision is its
own seed that blocks the rest.

## The fields a seed carries

| Field | What it holds |
|---|---|
| ID | `S-NNN`, never reused, never renumbered |
| Title | One line, imperative, naming the end state |
| Status | `open`, `blocked`, `in progress`, or `done` |
| Blocked by | Seed IDs, or `none` |
| Unblocks | Seed IDs, or `nothing yet` |
| End state | What is true when the seed is done, in checkable sentences |
| Verification | The commands to run, and what their output must say |
| Sources | Every `file:line` the seed rests on |
| Out of scope | What a reader might reasonably expect and will not get |

The last field earns its place. A seed that does not say what it excludes gets read as covering
more than it does, which is the same defect as an unsourced claim reaching a later reader.

## Explain the idea in prose before and while you work

**Also adopted 2026-08-08.** Alongside the seed, write the reasoning in plain prose, aimed at the
next agent rather than at a reviewer who already agrees with you. This is not a summary of the
diff, which git already holds. It is the part that does not survive in code: why this shape rather
than the obvious one, which sources disagreed and how that resolved, and what you looked at and
decided not to do.

Where that prose lives depends on what it is about, and the choice is not free:

- A decision goes in an ADR.
- A trap learned by getting something wrong in a folder goes in that folder's `CLAUDE.md`.
- A choice made with no source goes in `src/CLAUDE.md`, under its existing heading for exactly that.
- Everything else goes in the seed and in the commit message.

Do not put the prose only in a chat reply. A chat reply is the one place no future agent reads.

## Where seeds live, and what still belongs to the work queue

Two trackers exist and the boundary between them is stated rather than felt.

| | [`../../docs/SEEDS.md`](../../docs/SEEDS.md) | [`../../docs/BUILD-PLAN.md`](../../docs/BUILD-PLAN.md) |
|---|---|---|
| Holds | scheduled work, decomposed into seeds | items owed with an owner and a trigger, and claims not verified |
| May be cited | yes, by anything | **no**, by nothing, ever |
| Lifetime | until the work is done | temporary, and it will be deleted |

An item moves from the work queue into `SEEDS.md` **when it becomes actionable**, and the queue row
is deleted in the same change. It never lives in both, because the queue's own maintenance rule
already states why: "a second place recording completion is a second place to be wrong".

`BUILD-PLAN.md` section 1, the ordered "Next" list, is **replaced** by `SEEDS.md` and does not
return. Sections 2 through 4 stay where they are.

**The two links to the queue in this file are the exception the queue's own rule allows, and they
have an expiry.** The rule forbids citing that file as the source of a fact. Naming it to state a
rule about it is what the root `CLAUDE.md` already does in its own work-queue section. When the
queue is finally deleted, the table row above and those links go with it, and this paragraph is the
reminder to remove them rather than leave two dangling pointers behind.

## Where the generic answer is wrong here

| A generic answer reaches for | This repository decided |
|---|---|
| One issue per feature | One seed per single-agent unit of work, and a feature is usually several |
| A checklist of steps | An end state plus a verification, because a procedure goes stale and a state does not |
| "Blocked by" only | Both directions, so finishing a seed tells you what opened up |
| Putting the plan in the pull request description | The seed is the durable artifact; the pull request closes it |
| Tracking work in `BUILD-PLAN.md` | That file is temporary and may not be cited; scheduled work goes in `SEEDS.md` |
| Explaining the change in the chat reply | Prose goes in the ADR, the folder `CLAUDE.md`, the seed, or the commit message |
| One pull request that finishes an increment | One pull request per seed, unless two seeds are genuinely the same change |
