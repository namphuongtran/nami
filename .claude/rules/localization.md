---
paths:
  - "**/*.resx"
  - "**/Resources/**"
---

# Localizing here

This file loads when a `.resx`, or anything under a `Resources/` folder, is in play. It holds the
gap between generic ASP.NET Core localization guidance and what Nami decided. Read it as a list of
traps, not as a style guide.

It holds nothing the files beside it already hold. [`razor.md`](razor.md) carries the row that an
English string in a `.cshtml` goes to `.resx` plus `IStringLocalizer<T>`, and it carries the
two-surface split between the end-user pages and the admin console. [`csharp.md`](csharp.md)
carries naming, target framework, and analyzer breadth. [`writing-style.md`](writing-style.md)
carries the English house style, which governs a source string and rules on nothing about a
translation of it.

**No `.resx` file and no localization code exists in this repository, measured 2026-08-07.** So
every row below is derived from an accepted decision, and none was learned by getting something
wrong. That is the same footing [`razor.md`](razor.md) stands on, and it matters to a later
reader: a decision-derived rule has never been tested by use.

Design documents are cited by **section** rather than by line, because `docs/design/CLAUDE.md:93`
says to prefer a section number inside that folder, since these documents grow in the middle.

## Two surfaces, one language

| Surface | Mechanism | Read at |
|---|---|---|
| Razor Pages end-user surface | `.resx` plus `IStringLocalizer<T>`, validation messages included | design 11 section 5.6 |
| Email | `IStringLocalizer` / `ResourceManager` fallback, and a template resolved by `(flow, tenant, culture)` | design 10 section 5.4 |
| Admin console | Shares the design 11 approach, and is a build-time item | `docs/design/16-admin-app.md:250` |

The first two are bound together, and design 11 section 5.6 states the binding: "The UI and email
render the same language within one flow." So a change to culture resolution on either side is a
change to both, and the two subsystems cannot be localized by separate decisions.

## Where the generic answer is wrong here

Each row was read at its source on 2026-08-07.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| The default `RequestLocalizationOptions` provider order | Three layers, with a tenant in the middle: `Accept-Language` through `RequestLocalizationMiddleware`, then a per-tenant default-culture override, then an explicit user culture cookie that wins over both | design 11 section 5.6 |
| A strict mode that throws on a missing key, at least in Development | "never throw, warn once on a missing key". Requested culture, then neutral culture, then the `en` floor | design 10 section 5.4, design 11 section 5.6, ADR-0038:45 |
| A hard-coded supported-culture list in the composition root | Configuration-driven per deployment. And a `Nami:` key is public contract, so "A rename or a move is MAJOR under B" | design 11 section 5.6, design 10 section 5.4, ADR-0044:42 (parameter I) |
| Razor for an email template, so the strings sit beside the markup | "never Razor for tenant-editable templates, which would execute C#". A sandboxed engine, Fluid or Scriban, implementation-open | design 10 section 5.4, ADR-0038:45 |
| One fallback chain | Two, and they are different. Strings fall culture-ward. Templates fall `(flow, tenant, culture)`, then tenant-override-any-culture, then global-template-that-culture, then global `en` | design 10 section 5.4 |
| Adding right-to-left support while you are in there | "RTL is out of scope for v1" | design 11 section 5.6 |
| Moving `UseRequestLocalization` earlier, so more of the pipeline sees the culture | Design 11 section 6.1 fixes request localization after security headers and CSP, and before routing. ADR-0091:123 fixes the **security-headers** middleware relative to request localization, which pins the pair from the other side | design 11 section 6.1, ADR-0091:123 |

### Two of those fail silently, so they need more than a row

Neither generic answer merely differs. Each breaks something, and the break leaves a clean server
log.

**1. Translating the error copy can undo anti-enumeration.** Design 11 section 5.8 makes E1
(lockout at login) and E2 (a disabled user) render the **same** uniform "invalid credentials", and
E1 adds "never 'account locked'". The vagueness is the feature. A translator handed a string table
and no context writes the helpful version, because that is what good translation looks like
everywhere else. The page renders, the flow completes, every test stays green, and the deployment
has gained an account-enumeration oracle. Two things that look like coverage are not. The mandatory
latency-invariance test (ADR-0038:42) measures timing, not wording. Design 11 section 9 asserts
"Error states: E1-E7 render the specified copy and behavior", which is written for the copy and
says nothing about a translation of it. Design 11 section 5.8 closes with "all copy passes through
localization", so the translated string is the one the user actually reads.

**2. The `en` floor makes a missing translation look like success.** Four sources scope the warning
to a missing **key**, and not one of them scopes it to a culture or to a deployment. Design 11
section 5.6 says "the `en` floor that always renders, warning once on a missing key", and its
section 9 acceptance line says "a missing key falls to the `en` floor and warns once". Design 10
section 5.4 says "never throw, warn once on a missing key", and its section 9 acceptance line says
"a missing key falls through to the `en` floor and warns once". So an absent culture with N keys
warns N times. Do not read "once" as one log line for the deployment.

So what makes this quiet is not the volume. It is that nothing fails: the page renders, no
exception is thrown, and the only signal is warn-level log volume that somebody has to be counting.
The acceptance invariant tests that the fallback happens, which is the opposite of noticing in
production that it happened. Whatever detects a half-translated deployment is not in this
repository. Searched 2026-08-07 with `git grep -niE` over every tracked file except this one:
`half-translat`, `translation coverage`, and `untranslated` returned **zero** hits each.

## Where gate coverage of a `.resx` stops

Neither row is a defect to fix here. Each is a reason to read a `.resx` by eye where a `.cs` is
read by a tool. The two rows differ in strength, and the second says so.

1. **Neither the name scrub nor the docs guardrail reaches a `.resx`.** Both are scoped to
   markdown. [`razor.md`](razor.md) carries that mechanism with its pointers, so it is not
   restated here. What is specific to a `.resx`: the root
   [`../../CLAUDE.md`](../../CLAUDE.md) rule against naming the direct commercial competitor
   applies to **every** committed file, and localized user-facing copy is a plausible place for a
   product comparison. No gate sees it.
2. **Whether an analyzer reads a `.resx` string value is settled by no source here.** This row is
   the weakest in this file, and it is kept because assuming the opposite is worse.
   ADR-0092:147-148 says "The SDK analyzers see C#", then names four things they do not see:
   "Razor markup, SQL held outside C#, Dockerfiles, or GitHub Actions workflow definitions".
   The quote begins on `:147` and its fourth item completes on `:148`. A `.resx` is **not** among
   the four. Searched 2026-08-07 with `git grep -niE` over every tracked file except this one:
   `resx` returned **4** hits, none of them about an analyzer, and `resource file` and `satellite`
   returned **zero** each. So this row extends ADR-0092's principle rather than
   quoting its decision, and it is labelled that way on purpose. Do not cite ADR-0092 for a claim
   about a `.resx`.

One question this raises is **open**, so do not fill it from judgement. The root `CLAUDE.md`
forbids an em dash in "prose you write for this project". No source says whether that reaches a
translation into a language whose typography uses one. Searched 2026-08-07 with `git grep -niE`
over every tracked file except this one: `typograph` returned **zero**; `translat` returned **60**,
and none is about the em-dash ban; `em dash` returned **11** and `em-dash` returned **4**, and
every one is about the guardrail check or the style rule. No line holds both a `translat` match and
an em-dash match. Raise the question rather than assume either answer.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is written
into it (`docs/CLAUDE.md`). All were run on 2026-08-07 with `git grep -niE` over every tracked
file **except this one**, in **substring** form. Excluding this file is what keeps them
re-derivable, because every term below now appears here. The substring form over-counts and never
under-counts, so a zero is reliable. Do not re-run these with `\b`: `docs/CLAUDE.md` records that
`git grep -E` ignores it here and returns zero for every term.

Nineteen spellings returned **zero** hits each: `ui_locales`, `claims_locales`, `CultureInfo`,
`CurrentUICulture`, `RequestCulture`, `SupportedCultures`, `DefaultRequestCulture`, `plural`,
`right-to-left`, `MessageFormat`, `satellite assembl`, `NeutralResourcesLanguage`, `resource key`,
`translation memory`, `crowdin`, `weblate`, `gettext`, `DisplayAttribute`, and
`ErrorMessageResourceName`. So the following are open.

- **No `.resx` naming or location convention**, and nothing on satellite assemblies. ADR-0065:37
  adopts the Microsoft .NET Framework Design Guidelines for naming by reference, and its list of
  adopted pages does include "resources". Whether that page reaches file layout as well as key
  naming was **not** checked, so treat this bullet as bounding the question rather than answering
  it.
- **No pluralization or message-format posture.** The near miss: `ICU` matches inside ordinary
  words, so search it as a standalone token. Done that way it returns one hit,
  `docs/design/02-data.md:826`, which is about PostgreSQL collations and not about ICU
  MessageFormat.
- **No answer on whether a resource key is contract.** ADR-0044:42 (parameter I) brings
  configuration keys and shipped static-asset paths under the SemVer rules. It does not reach
  resource keys. Do not extend it here: parameter I is itself an addition made on 2026-08-01, so
  the pattern for widening the surface is a new ADR, not a rules file.
- **No translation workflow**, and no answer on who supplies a translation or reviews one.
- **Nothing on `ui_locales`**, the OpenID Connect request parameter by which a client asks for a
  language. The near miss: `RTL` matches inside "shortly", so read the hits. Exactly one is the
  literal token, design 11 section 5.6, and it scopes right-to-left out of v1 while saying nothing
  about `ui_locales`.

A genuinely new decision here is raised as an ADR, never settled inside a design or inside this
file (`docs/CLAUDE.md`, the authority order).

## Who owns which question

| Question | Authority |
|---|---|
| Culture resolution, the string chain, supported cultures, right-to-left scope | design 11 section 5.6 |
| Error-state copy, and why it is deliberately uniform | design 11 section 5.8 |
| Middleware order around request localization | design 11 section 6.1, and ADR-0091 |
| Email templating, and the ban on Razor | ADR-0038 parameter G, and design 10 section 5.4 |
| The four-step template chain | design 10 section 5.4 alone. ADR-0038 parameter G holds the culture chain, not this one |
| Whether a configuration key may be renamed | ADR-0044 parameter I |
| The admin console surface | ADR-0020:39, with `docs/design/16-admin-app.md` |
| An English string inside a `.cshtml` | [`razor.md`](razor.md) |
| C# around a localizer | [`csharp.md`](csharp.md) |
| The English house style of a source string | [`writing-style.md`](writing-style.md) |
