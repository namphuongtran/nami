---
name: driving-a-browser-safely
description: Use when an agent will drive a live browser against a running Nami surface, whether to record a test, to debug a flow, or to show a demo. Nami's forms are login and consent forms, so a typed credential, a page read back into the transcript, and a screenshot of an authenticated page all put secrets through a model, and ADR-0067 forbids that. A submit here is a grant that never expires rather than a form post. Read generating-a-playwright-test for the authoring procedure and for what adopting a browser-driving tool costs.
---

# Driving a live browser safely here

Read this before typing anything into a running Nami surface, and before taking a screenshot of
one. It exists for the reason
[`../writing-playwright-tests/SKILL.md`](../writing-playwright-tests/SKILL.md) gives at its lines 8
to 11: a `paths:` glob needs a file to trigger on, and driving a browser touches no file at all.

**The risk is not test authoring, it is acting on a running identity provider.** Someone debugging
a flow or showing a demo runs the same risk and reaches no step of the authoring procedure. That is
why this is separate.

**This skill holds only the safety delta.** Three skills own the rest, and none of it is restated
below.

- [`../generating-a-playwright-test/SKILL.md`](../generating-a-playwright-test/SKILL.md) owns the
  authoring procedure, what "MCP" means in this repository, and what adopting a browser-driving
  tool costs.
- [`../writing-playwright-tests/SKILL.md`](../writing-playwright-tests/SKILL.md) owns the package
  twin, the namespace, the admin-only scope, and the licence contradiction.
- [`../writing-tests/SKILL.md`](../writing-tests/SKILL.md) owns everything general to a test.

[`../../../tests/CLAUDE.md`](../../../tests/CLAUDE.md) holds the traps learned inside `tests/`, and
it is **not** re-injected after `/compact`.

ADR-0067 is the authority on whether an AI tool may be used and on what terms. Four of its lines
are quoted below, and they are the only part of it this skill turns on: `:38`, `:54`, `:58`, and
`:87`.

## What exists today, measured

Measured on 2026-08-07 at `10df955`. **Nothing can be driven yet, and no tool is configured to
drive it.**

- **One measurement of this skill's own.** `.claude/settings.json` enables 17 plugins on
  2026-08-07, and none of them is Playwright. So no tool in this session can drive a browser.
- **That there is no MCP server and nothing to drive is already measured**, at
  `../generating-a-playwright-test/SKILL.md:33` and `:37`, in the five-row table of why the
  published procedure cannot run. Read it there. Repeating a measurement gives it two homes, and
  the two then drift.

So every rule below is derived from an accepted decision, and none was learned by getting something
wrong here. A decision-derived rule has never been tested by use, and that difference matters to a
later reader.

## The rule the source document carries, and the reason it lacks

The document this skill was scoped from states two sentences and no reason: do not submit the form,
and ask for a review before submitting it. The reason is what makes them hold here.

1. **Never operate a control that changes state.** On this product that includes sign in, consent,
   deny, revoke, approve, and delete.
2. **Say what will be operated, and on which environment, before acting.**
3. **Wait for a person to approve it.**

The source gives rules 1 and 3. Rule 2 is what makes rule 3 answerable, because a reviewer cannot
approve a click they have not been shown.

**Why a back button does not undo it.** A consent approval with remember ticked "persists a
permanent authorization", consent "has **no expiry**", and the grants page is "the only removal
path" (`docs/design/11-login-consent-ui.md` section 5.2). The same submit "emits a hash-chained
**consent receipt** through the audit sink" (same section). So one stray approval writes a grant
that never expires and an audit record that is chained.

## Where the generic answer is wrong here

Each row was read at its source on 2026-08-07. The middle column quotes enough of the decision to
survive a line shift, so a drifted pointer reads as drift rather than as a different claim. Design
documents are cited by **section**, because `docs/design/CLAUDE.md` records that they grow in the
middle.

| A generic answer reaches for | Nami decided, or the artifact says | Read at |
|---|---|---|
| Typing a username and password to reach the page behind the login form | "Contributors must not paste secrets, credentials, key material, customer data, or embargoed security-fix details into AI tools" | ADR-0067:54 |
| Reading the page back into the transcript to find a selector | The page behind sign in carries tokens, codes, and claims, and a transcript is the tool that line names | ADR-0067:54 |
| A screenshot as the record of what was observed | The admin scenario asserts "**no access token in any browser response**". A screenshot of an authenticated page carries the thing that assertion proves absent | design `16` section 9, and ADR-0025:62 |
| Submitting the form, then reading the result | Approve "`SignIn`s under the OpenIddict server scheme" and, with remember ticked, "persists a permanent authorization". Consent "has **no expiry**" | design `11` section 5.2 |
| Signing in with the seeded admin account | There is no standing one, by decision. The bootstrap issues "a one-time setup token ... with a temporary random password that is **never logged**", forces a change, and enrolls MFA | ADR-0015:38 |
| Recording that temporary password so the run can be repeated | ADR-0015 chose that path because "no lasting seeded secret exists", and rejected a seed password as "a default-credential risk". Writing it into a transcript restores what the decision removed | ADR-0015:68-69 |
| "It is a test tool, so the AI policy does not reach it" | "AI tools are permitted" is a permission with conditions attached, and the no-secrets line is one of them | ADR-0067:38, then :54 |
| Asking a person once the form is filled | The source itself states the order, and it is review **before** submit | the source document |
| Reusing the source's shape, a hardcoded URL and a `~/Downloads` path | It hardcodes one person's home directory and one external form. Neither belongs in a tracked file here | the source document |

## What ADR-0067 does not settle

Two gaps. Both are flagged rather than filled, per the authority order in `docs/CLAUDE.md`.

**1. The scope line and the rule disagree about reach.** ADR-0067:58 says "This governs
contributions to the codebase, docs, and ADRs. It does not govern Nami's runtime (Nami is not an AI
product)". Driving a live browser is not obviously a contribution. ADR-0067:54 carries no such
condition. Take :54 as binding, because its sentence is unconditional. Whether ADR-0067 reaches
live-system operation is **unsettled**, and the clarification is an ADR change rather than
something to settle here.

**2. ADR-0067 points at a file that does not hold the rule it credits to it.** ADR-0067:87 says
"SECURITY.md carries the no-secrets rule", and ADR-0067:54 says "consistent with SECURITY.md". Read
in full on 2026-08-07, `SECURITY.md` is 28 lines under five headings: Security Policy, Reporting a
vulnerability, What to expect, Scope, and Supported versions. Searched case-insensitively for
`secret`, `credential`, `key material`, `private data`, and `paste`, it returned **zero** hits. So
quote ADR-0067:54 and never SECURITY.md. This is the failure mode
[`../checking-a-citation/SKILL.md`](../checking-a-citation/SKILL.md) exists for, and correcting it
is owed rather than done.

## What is genuinely not decided

Each absence is a claim about a search, so each search is written into it (`docs/CLAUDE.md`). All
were run on 2026-08-07 at `10df955`, case-insensitive over tracked files **excluding
`.claude/skills/`**, which is the convention
`../generating-a-playwright-test/SKILL.md:48` already uses.

- **Whether an agent may drive any environment at all.** No document says yes and none says no.
  What exists is an address form: local tenant addressing "uses the path-based form
  (`localhost/tenant`)" (ADR-0070:40), reached through the `make dev-up` wrapper of ADR-0025. An
  address is not a permission.
- **No account exists for an agent to use.** `test tenant`, `demo tenant`, `test user`,
  `test account`, `default password`, `first-run admin`, and `initial admin` returned **zero** hits
  each. Two terms returned hits and neither is a real one, which is why both are written down here
  rather than reported as a count. `service account` returned one line,
  `docs/adr/0038-email-notification-subsystem.md:27`, and the match spans a hyphenated compound:
  "self-**service account**-takeover surface". `throwaway` returned 14 files, all read on
  2026-08-07, and each hit is a build fixture, a `git worktree`, a backup restore target, or a
  grep screen. None is an environment.

Do not fill either from judgement. A genuinely new decision here is raised as an ADR, never settled
inside a skill or a test file (`docs/CLAUDE.md`, the authority order).

The browser matrix, the headed default, the version, the tool choice, and the licence row are
**not** repeated here. `../writing-playwright-tests/SKILL.md` records the first three with its own
searches, and `../generating-a-playwright-test/SKILL.md` records the last two.

## Who owns which question

| Question | Authority |
|---|---|
| The authoring procedure, what "MCP" means here, and what adopting a browser-driving tool costs | [`../generating-a-playwright-test/SKILL.md`](../generating-a-playwright-test/SKILL.md) |
| What a Playwright test here must look like, and every Playwright-specific trap | [`../writing-playwright-tests/SKILL.md`](../writing-playwright-tests/SKILL.md), then ADR-0025 parameter E |
| Whether an AI tool may be used, and on what terms | ADR-0067 |
| What a consent approval creates, and how it is removed | `docs/design/11-login-consent-ui.md` section 5.2 |
| The admin end-to-end scenario, and where it is already written | [`../generating-a-playwright-test/SKILL.md`](../generating-a-playwright-test/SKILL.md), which names its three sources |
| How the first admin is created, and why no standing credential exists | ADR-0015 |
| The local address form, and the dev stack it runs on | ADR-0070, and ADR-0025 |
| Reporting a vulnerability | `SECURITY.md`, which is that and nothing more |

**A tool is a source, never an authority.** The document this skill was scoped from is a personal
one-off prompt for an unrelated form, not guidance, and it does not override an accepted ADR. Where
the two disagree, stop and flag both with file and line.
