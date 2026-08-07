---
paths:
  - "**/*.cshtml"
  - "**/*.cshtml.cs"
  - "**/wwwroot/theme/**"
---

# Writing Razor here

This file loads when a `.cshtml`, a `.cshtml.cs`, or a theme asset is in play. A `.cshtml.cs`
also matches `**/*.cs`, so [`csharp.md`](csharp.md) loads beside it. This file therefore holds
nothing that file already holds: naming, target framework, analyzer breadth, and the tool table
are there.

What this file holds is the gap between generic Razor guidance and what Nami decided. Read it as
a list of traps, not as a style guide.

**No `.cshtml` file exists in this repository yet, measured 2026-08-07.** So every row below is
derived from an accepted decision, and none was learned by getting something wrong. That is the
opposite of how [`csharp.md`](csharp.md) was built, and the difference matters to a later reader:
a decision-derived rule has never been tested by use. `docs/BUILD-PLAN.md` section 1 has PR-5 and
PR-6 next, and neither one is the host.

## Which surface are you in

The product carries **two** server-rendered Razor technologies, and ADR-0072:42 records that as
a deliberate cost rather than an accident.

| Surface | Technology | Project | Decided at |
|---|---|---|---|
| Login, consent, logout, passkeys, account management, error | Razor Pages | `Nami.Identity.Host` | ADR-0072:41, ADR-0027:46 |
| Admin console | MVC Razor over the backend-for-frontend | `Nami.Identity.Admin.App` | ADR-0072:42, ADR-0020:39, ADR-0065:73 |

Generic Razor Pages guidance describes the first surface only. The `**/*.cshtml` glob that loads
this file cannot tell the two apart, so establish which one you are in before applying anything
below. The detailed designs differ too: the end-user surface is
`docs/design/11-login-consent-ui.md`, and the admin app is `docs/design/16-admin-app.md` with
`docs/design/24-bff.md`.

## Where the generic Razor answer is wrong here

Each row was read at its source on 2026-08-07. Design 11 is cited by **section** rather than by
line, because `docs/design/CLAUDE.md` records that these documents grow in the middle.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| MediatR, once a page grows handlers and dependencies | Forbidden by name; if a mediator is ever genuinely needed, Nami writes one | ADR-0026:37, :43, deny-list :56 |
| A fluent assertion package in page tests | "Nami takes **no** fluent-assertion package"; `FluentAssertions` is on the deny-list | ADR-0060:47, ADR-0026:56 |
| Asserting how many times an injected service was called | "does not assert private internals, call counts, or structure" | ADR-0060:60 |
| `@Html.AntiForgeryToken()` plus a `fetch` header, for AJAX | "No client-side JavaScript framework"; script arrives "as external files", "never as inline script" | ADR-0072:45 (parameter E) |
| A nonce, or an inline `<style>` block, to carry theme colours | No nonce anywhere; the theme arrives as a served stylesheet under `style-src 'self'` | ADR-0091:130, design 11 section 7.4 |
| `@Html.Raw`, an inline `<script>`, or an inline event handler | `script-src 'self'` with no `unsafe-inline`; a theme that would require it "is rejected rather than accommodated" | ADR-0091:130, ADR-0072:43 (parameter C) |
| "UI pages validate antiforgery, OAuth endpoints ignore it" | Finer. `POST /connect/authorize` carries **both** policies at once | design 11 section 7.1 |
| A `SecurityHeadersAttribute` that *applies* the headers | The middleware is the only writer, and the attribute only *selects* | design 11 section 7.4, ADR-0091 parameter K |
| One `SameSite` value for every cookie | Two rows: SSO and session take `__Host-` plus `Lax`; the external-login correlation cookie takes `SameSite=None` and no `__Host-` | design 11 section 7.2, ADR-0043:43 |
| `Url.IsLocalUrl` on the login page | Applied on Login, Logout, Consent, ExternalLogin, Redirect, StepUp, and the tenant switcher, with the helpers in `Extensions.cs` | design 11 section 7.3 |
| Azure Key Vault for secrets, `appsettings.json` for the rest | "not locked to any cloud"; keys are `Nami:Section:Key` and `Nami__Section__Key`, and they are a stable public contract | ADR-0006:31, ADR-0065:78, ADR-0044:42 |
| A `DbContext` injected into a page model | The pages are decoupled from the engine by a thin interaction service; the auth backend is design 08 and the protocol is design 04 | ADR-0072:41, design 11 sections 3.1 and 3.2 |
| An English string written into a `.cshtml` | `.resx` plus `IStringLocalizer<T>`, validation messages included, falling to an `en` floor that always renders | design 11 section 5.6 |
| A version segment on a page route | The human-facing pages are deliberately unversioned, because a version in a login URL lands in bookmarks | ADR-0090:134 |
| A Razor Class Library, so a consumer view wins by view-engine precedence | No UI package ships, so precedence is not a mechanism here. A third override point was withdrawn on 2026-08-01 for exactly this reason | ADR-0027:46 (parameter G), design 11 section 5.5 |
| Blazor, or one Blazor component on an otherwise static page | Deferred, not rejected in principle, and not adopted for the end-user surface | ADR-0072:44 (parameter D) |

### Two of those rows fail silently, so they need more than a row

Both are cases where the generic answer does not merely differ. It breaks something, and the
break leaves a clean server log.

**1. The antiforgery split is finer than "UI pages on, OAuth endpoints off".** Design 11 section
7.1 reached that by reading the engine's own sample at source rather than paraphrasing it.
`POST /connect/authorize` accepts a machine arrival and a consent-form submit on the **same
route**, and the two are discriminated by a form-value selector rather than by route. Two
consequences follow, and the design names both as easy to get wrong. A blanket
`AutoValidateAntiforgeryToken` over an area containing the authorize controller breaks the machine
entry. A blanket ignore on that route strips protection from the consent submit, which is the one
form on that route a hostile page would most want to forge. The terms appear in two designs and
nowhere else in this repository, checked 2026-08-07: `docs/design/11-login-consent-ui.md` lines
533 to 545, and `docs/design/24-bff.md:146` for the admin app's server-rendered form profile.

**2. The security-headers attribute is a selector, not the security boundary.** ADR-0091
parameter K makes the middleware the only writer of a response profile, and a response whose
endpoint carries no such attribute gets the **UI** profile rather than no headers at all. So
forgetting the attribute on a new page is a wrong-profile bug and not a no-profile one, and the
two are found by different means. Design 11 section 7.4 records the load-bearing case:
`response_mode=form_post` under the UI profile produces **no navigation**, which is a blank page
that no server-side header assertion can see.

## Two gates that read as covering a `.cshtml` and do not

Both are the kind of coverage a generic answer assumes. Neither is a defect to fix here. Each is
a reason to read a `.cshtml` by eye where a `.cs` is read by a tool.

1. **The C# analyzers and `.editorconfig` do not see Razor markup.** ADR-0092:147 states it:
   "The SDK analyzers see C#. They do not see Razor markup, SQL held outside C#, Dockerfiles, or
   GitHub Actions workflow definitions." Measured 2026-08-07, `.editorconfig` carries no `cshtml`
   section and no `razor` section, grepped case-insensitively for both with zero hits. So
   `dotnet format --verify-no-changes` polices the `.cshtml.cs` and leaves the `.cshtml`.
2. **The name scrub and the docs guardrail are both scoped to markdown.** The local hook stages
   `-- '*.md'` (`scripts/hooks/pre-commit:25`), and guardrail Checks 1 and 5 read
   `git ls-files '*.md'` (`scripts/check-adrs.sh:28`). The root [`../../CLAUDE.md`](../../CLAUDE.md)
   rule against naming the direct commercial competitor applies to **every** committed file, and
   user-facing page copy is a plausible place for a comparison. No gate sees it. The same holds
   for the no-em-dash rule inside page copy.

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is
written into it (`docs/CLAUDE.md`). All were run on 2026-08-07 with `git grep -niE` over every
tracked file.

- **No handler, binding, or Post-Redirect-Get convention.** Fourteen spellings returned **zero**
  hits each: `PageModel`, `OnGet`, `OnPost`, `RedirectToPage`, `Post-Redirect-Get`,
  `redirect after post`, `BindProperty`, `IActionResult`, `IPageFilter`, `TempData`, `ISession`,
  `_ViewStart`, `_ViewImports`, and `Pages/Shared`.
- **No model-binding or validation posture.** Seven spellings returned **zero** hits each:
  `ModelState`, `TryUpdateModel`, `IValidatableObject`, `[Required]`, `asp-for`, `asp-validation`,
  and `asp-page`. Separately, [`csharp.md`](csharp.md) records that no validation library is
  chosen, measured 2026-08-05.
- **No test-double library.** `Moq`, `NSubstitute`, `FakeItEasy`, `fake`, `stub`, and `spy`
  returned zero. `mock` returned one hit, `.gitignore:364`, which is a comment in the standard
  ignore template naming Telerik's JustMock configuration file. `test double` returned one hit,
  `docs/design/03-audit.md:353`, which is about adapters rather than about a library. Every
  `substitut` hit is the "SQLite is never substituted" sense or ADR-0026:43. ADR-0060:35 fixes
  the test **types**, and ADR-0060:60 forbids asserting call counts, which together bound the
  question without answering it.
- **No tag-helper or view-model convention.** Five spellings returned **zero** hits each:
  `tag helper`, `TagHelper`, `view model`, `ViewModel`, and `input model`.

A genuinely new decision here is raised as an ADR, never settled inside a design or inside this
file (`docs/CLAUDE.md`, the authority order).

## Who owns which question

| Question | Authority |
|---|---|
| Which technology renders which surface, and why not Blazor | ADR-0072 |
| Browser-facing response headers, and the three profiles | ADR-0091, and note parameter K makes the profile set total |
| Page catalog, interaction service, antiforgery split, cookie matrix, open-redirect guard, middleware order, theming, localization | `docs/design/11-login-consent-ui.md`, by section |
| Where the pages live, and what ships in a package | ADR-0027 parameters B, E, and G |
| Whether a configuration key or a `wwwroot/theme/` path may be renamed | ADR-0044 parameter I: it is MAJOR |
| The admin surface | ADR-0020, with `docs/design/16-admin-app.md` and `docs/design/24-bff.md` |
| Whether a page route carries a version | ADR-0090 |
| Cookie attributes asserted at startup | ADR-0043, and the fail-fast `core-cookie-attributes` self-check |
| Test taxonomy, and what a test may assert | ADR-0060, and `docs/design/20-testing.md` |
| Which packages may be taken at all | ADR-0026, and its package-name deny-list |
| C# inside a `.cshtml.cs` | [`csharp.md`](csharp.md), which loads on the same file |
