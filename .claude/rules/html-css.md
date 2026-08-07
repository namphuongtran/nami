---
paths:
  - "**/*.css"
  - "**/*.html"
  - "**/*.js"
  - "**/wwwroot/**"
---

# Writing HTML, CSS, and client script here

This file loads when a `.css`, an `.html`, a `.js`, or anything under `wwwroot/` is in play. The
`wwwroot/` glob overlaps [`razor.md`](razor.md)'s `**/wwwroot/theme/**` on purpose, the same way
`razor.md` overlaps [`csharp.md`](csharp.md) on a `.cshtml.cs`. So this file holds nothing those
two already hold: the two-surface split, the antiforgery split, the cookie matrix, and the
open-redirect guard are `razor.md`'s.

What this file holds is the gap between generic HTML and CSS guidance and what Nami decided. Read
it as a list of traps, not as a style guide.

**Nami decides almost nothing about which colours to pick, and almost everything about how style
may reach the browser.** That asymmetry is the whole point of the file. A generic style guide
answers the first question and is silent on the second. Every row below is the second question, and
the "What is genuinely not decided" section records the first as an open gap with its searches
attached.

**No `.css`, `.html`, or `.js` file is tracked in this repository, measured 2026-08-07 at
`10df955`** (`git ls-files` filtered for `css`, `html`, `htm`, `scss`, `sass`, `less`, `js`, `mjs`,
and `cjs`, returning zero). So every row is derived from an accepted decision, and none was learned
by getting something wrong. That is the same footing `razor.md` records for itself. The difference
from `csharp.md` matters to a later reader: a decision-derived rule has never been tested by use.

## Which surface the style is for

Three surfaces, not two, and the third is the one a generic answer misses.

| Surface | Who writes the markup | Read at |
|---|---|---|
| Login, consent, logout, passkeys, account management, error | Nami, as Razor Pages | ADR-0072:41 |
| Admin console | Nami, as MVC Razor over the backend-for-frontend | ADR-0072:42 |
| The `response_mode=form_post` response | **The engine.** Nami does not render it and "has no way to place a nonce in it" | ADR-0091:180-181, design `22`:262 |

Two consequences follow. The `form_post` markup is assembled from string literals inside
`OpenIddict.Server.AspNetCore`, so it cannot be edited, themed, or linted here; it is registered as
a version-sensitive seam instead (design `22`:262). And the admin console "uses its **own** theme"
because "it is an operator console, not a tenant-facing surface", so per-tenant branding is
"*managed* here but not *applied* to the console itself" (design `16`:105-107).

## Where the generic HTML or CSS answer is wrong here

Each row was read at its source on 2026-08-07. The middle column quotes enough of the decision to
survive a line shift, so a drifted pointer reads as drift rather than as a different claim. Design
`11` is cited by **section** rather than by line, because `docs/design/CLAUDE.md` records that these
documents grow in the middle.

| A generic answer reaches for | Nami decided | Read at |
|---|---|---|
| An inline `<style>` block, or a nonce, to carry theme colours | "`style-src 'self'` on the UI profile, with no nonce and no hash"; the theme is "served as a **stylesheet response**" | ADR-0091:171-172, :130 |
| `element.style.cssText = ...`, to apply a token set in one statement | Blocked. Only `element.style.setProperty(...)` survives, because it "is not governed by the policy"; `element.setAttribute('style', ...)` is blocked too | ADR-0091:187-193, design `16`:117-120 |
| Tailwind, or an npm build step for CSS | "Bootstrap 5 is the default CSS framework, CSS-variable driven with no npm or Tailwind build step"; Tailwind is "an adopter's own choice, not a shipped default" | ADR-0072:43 |
| A Google Fonts link, or `@font-face` pointing at a CDN | `font-src 'self'`, and "Custom fonts and full per-tenant page templates are **not** in v1" | ADR-0091:130, design `11` section 5.5 |
| A CDN link for Bootstrap, or for a polyfill | The profile opens with `default-src 'none'` (ADR-0091:130), and script arrives "as external files that the policy permits by source", which means Nami's own origin (ADR-0072:45) | the two pointers inline |
| `url()` at a foreign host, for a background image or a font | Named as "an exfiltration and tracking channel" | design `11` section 5.5 |
| Locking `img-src` down to `'self'` | `'self' https:` on purpose, because "tenant and client logos are external"; accepting it is a Pre-GA checklist entry | ADR-0091:229, design `11` section 7.4 |
| An inline `onclick=`, or an inline `<script>` | "No client-side JavaScript framework"; scripting is "never as inline script" | ADR-0072:45, ADR-0091:130 |
| Raw tenant CSS, sandboxed to a `<style>` block | Not in the v1 `ThemeJson` schema, which "carries design tokens". The `<style>`-block condition was **foreclosed** on 2026-08-01: raw CSS would have to arrive as a served stylesheet, or reopen ADR-0091 | design `11` section 5.5 |
| A rich per-tenant palette, a gradient set, or a page template | v1 branding is "deliberately bounded: a logo, a primary and accent colour, a display name, and support, privacy, and terms links" | design `11` section 5.5 |
| Renaming or moving a file under `wwwroot/theme/`, as a tidy-up | "moving a shipped static-asset path breaks every consumer who replaced a file at it"; a move is MAJOR | ADR-0044:42 |
| Framing a page, or a per-tenant framing allowlist | `frame-ancestors 'none'` with `X-Frame-Options: DENY`. The allowlist is "rejected rather than deferred, so `TenantBranding` gains no such field" | ADR-0091:130, design `11` section 7.4 |
| A `<noscript>` fallback, to rescue a script the policy blocked | The HTML Standard ties `noscript` to scripting being **disabled for the document** rather than to a blocked script | ADR-0091:50, design `22`:235-236 |
| One small policy exception, so a theme can work | "a theme that would require `unsafe-inline` is rejected rather than accommodated", and the `csp-no-relaxation` invariant fails **startup** "in every environment including Development" | ADR-0072:43, ADR-0043:50 |
| A dark-mode or high-contrast variant, because it is standard practice | Nothing decides it. Twenty-one spellings returned zero, listed below | "What is genuinely not decided", in this file |

### Two of those rows fail silently, so they need more than a row

Both are cases where the generic answer does not merely differ. It breaks something, and the break
leaves a clean server log.

**1. `cssText` fails as an unstyled preview, never as an error.** The whole of this trap is design
`16` section 5.2, under "Theming". The line numbers below are that section's on 2026-08-07. It
names the shape before the detail, at `16`:114: "The preview has exactly one implementable form,
and the natural way to write it is the one that breaks." The admin branding screen renders a sample
login card from tokens being typed, so it "cannot be a served stylesheet, because the token values
are being typed and are not saved yet, and ADR-0091 parameter D admits no nonce anywhere" (design
`16`:115-116). What survives is `element.style.setProperty('--nami-primary', value)`. Assigning
`cssText` "is the obvious way to apply a whole token set in one statement, so it is what an
implementer reaches for first, and it fails as a silently unstyled preview rather than as an error"
(design `16`:120-122).

Two further facts make this worth more than a row. This is "the only surface in the product that
applies style from the client at all" (design `16`:122-123). And the distinction "is documented at
MDN rather than stated in the specification text, so it also carries a browser test"
(ADR-0091:193-195), so reading the CSP specification alone will not tell you.

**2. A policy-blocked script on the `form_post` page is a blank page.** Design `11` section 7.4
records the load-bearing case: `response_mode=form_post` "returns HTML the engine writes, ending in
an inline submit script and posting cross-origin to the client, so `script-src 'self'` and
`form-action 'self'` each stop authorization dead with a blank page and a clean server log". The
`<noscript>` submit button inside that markup does not rescue it, for the HTML Standard reason in
the table above. That is why the policy is three profiles chosen by response class rather than one
policy, and why the engine's submit script is the single hash in the whole set (ADR-0091:180-183).

## Four gates that read as covering a `.css` or an `.html` and do not

None is a defect to fix here. Each is a reason to read these files by eye where a `.cs` is read by
a tool.

1. **`.editorconfig` has no `css`, `html`, or `js` section, so these files fall to `[*]`.**
   Measured 2026-08-07, it carries exactly four section headers: `[*]` at line 3,
   `[*.{md,yml,yaml,json,csproj,props,targets}]` at 11, `[*.md]` at 14, and `[*.cs]` at 37. **The
   concrete consequence is the indent width.** `[*]` sets `indent_size = 4` (`.editorconfig:9`).
   The section that overrides it to 2 (`.editorconfig:12`) is scoped by the glob at line 11, and
   that glob lists neither `css` nor `js`. So a `.css` file inherits four spaces rather than the
   two that web tooling assumes. Nothing enforces it either way, because
   `dotnet format` reads this file for C# style and polices no `.css`. ADR-0092:147 states the wider
   limit: "The SDK analyzers see C#. They do not see Razor markup, SQL held outside C#,
   Dockerfiles, or GitHub Actions workflow definitions."
2. **No CSS, HTML, or accessibility linter is configured, so there is no formatter to defer to.**
   Searched 2026-08-07 over every tracked file, case-insensitive, and all nine returned **zero**:
   `stylelint`, `eslint`, `htmlhint`, `prettier`, `axe-core`, `pa11y`, `lighthouse`,
   `html validator`, and `W3C valid`. Write the style by hand and match what is already there.
3. **The name scrub and the docs guardrail are both scoped to markdown.** The local hook stages
   `-- '*.md'` (`scripts/hooks/pre-commit:25`), and Checks 1 and 5 read `git ls-files '*.md'`
   (`scripts/check-adrs.sh:28`). The root [`../../CLAUDE.md`](../../CLAUDE.md) rule against naming
   the direct commercial competitor applies to **every** committed file, and so does the
   no-em-dash rule. A CSS comment and a string inside a `.js` file are both plausible places for
   either. No gate sees them. `razor.md` records the same edge for `.cshtml`.
4. **The manifest that would catch an asset-path rename does not exist.** ADR-0044:136-138:
   "Parameters B through I are untouched by this and remain unenforced ... parameter I's manifest
   of configuration keys and asset paths has no file." So moving a file under `wwwroot/theme/` is
   MAJOR under a rule that nothing checks, and the ADR itself calls that manifest "the weakest link
   of the set, because it is the only one no analyzer maintains" (ADR-0044:49).

## What is genuinely not decided

Do not fill these from judgement. Each absence is a claim about a search, so each search is written
into it (`docs/CLAUDE.md`). All were run on 2026-08-07 at `10df955`, case-insensitive over every
tracked file.

- **No colour palette, no contrast target, and no accessibility standard for the end-user
  surface.** Twenty-one spellings returned **zero** hits each: `WCAG`, `a11y`, `wai-aria`,
  `aria-label`, `aria-`, `role=`, `screen reader`, `alt text`, `tabindex`, `skip link`,
  `focus ring`, `contrast ratio`, `colour contrast`, `color contrast`, `prefers-color-scheme`,
  `dark mode`, `high contrast`, `viewport`, `responsive`, `EN 301 549`, and `Section 508`.
  `accessib` returned six hits, and three of the six are `.editorconfig`'s
  `applicable_accessibilities` at lines 83, 91, and 100. Of the other three, ADR-0042:70 is a
  consequence of rejecting a CAPTCHA on every login. Design `16`:27 and `16`:126 are the **admin
  console**. So exactly one accessibility posture is decided anywhere in this repository, and it is
  admin-only: "Semantic HTML, labelled form controls, keyboard-navigable tables and dialogs"
  (design `16`:126). `contrast` returned three hits and none is about colour: ADR-0014:16 and
  ADR-0035:67 use "contrasting" and "contrasted" in the compared-with sense, and
  `architecture/18-decisions-index.md`:256 writes "The contrast with". **What this means for a
  generic style guide.** A 60-30-10 balance rule, a never-use list for background or text colours,
  and a named hex value for body text are each reasonable, and none of them is Nami policy.
  Adopting one is an ADR. It is never settled inside this file or inside a design
  (`docs/CLAUDE.md`, the authority order).
- **Bootstrap 5 is a stack-of-record entry with no licence row and no version pin.**
  ADR-0061:59 lists "Bootstrap 5, no build step" and cites ADR-0020 and ADR-0072. ADR-0072:103
  asserts "ADR-0026 (Bootstrap and any future CSS dependency must be permissively licensed)", but
  searching `docs/adr/0026-dependency-license-policy.md` for `bootstrap`, `css`, `frontend`,
  `front-end`, `npm`, and `javascript` returned **zero** hits on 2026-08-07, so ADR-0026 does not
  contain that rule and ADR-0072's line is an application of the general policy rather than a
  quotation of a specific one. `docs/DEPENDENCY-LICENSES.md` has no Bootstrap row either: its only
  `bootstrap` hit is line 114, inside the enumeration of the JMeter HTML report's bundled assets.
  **So do not read "Bootstrap 5" in the stack table as a verified licence or as a pinned version.**
  ADR-0026 section C owns what taking it would owe, which is a licence read at the distributed
  artifact rather than at a badge or a repository root.
- **No decision on where a `.js` file lives, or how it is named.** ADR-0072:45 fixes that script
  arrives "as external files that the policy permits by source", and ADR-0091:130 fixes
  `script-src 'self'`. Neither names a folder, a file-naming form, or a module format. ADR-0065 is
  the naming authority and its subject is .NET identifiers.

## Who owns which question

| Question | Authority |
|---|---|
| Which technology renders which surface, the CSS framework, and the no-build-step rule | ADR-0072, parameters C and E |
| The concrete directives, and the three response profiles | ADR-0091. Note parameter K makes the profile set total, so an absent attribute means the UI profile rather than no policy |
| Whether client code may apply style at all, and by which API | ADR-0091 parameter D, with design `16` section 5.2 for the one surface that does |
| The two branding tiers, the bounded token set, and the override seam | `docs/design/11-login-consent-ui.md` section 5.5, by section |
| UI customization as an extension point, and what each distribution channel offers | ADR-0027 parameter E. The container channel "offers configuration only" until design `21` records a route to a baked-in asset |
| Whether a `wwwroot/theme/` path may be renamed | ADR-0044 parameter I: it is MAJOR, and nothing enforces it yet |
| What fails startup if a consumer weakens the policy | ADR-0043, and the `csp-no-relaxation` invariant at `0043:50` |
| The `form_post` markup Nami does not author | `docs/design/22-openiddict-seam-catalogue.md`, seam S36 |
| Which packages, including a CSS framework, may be taken at all | ADR-0026, and its package-name deny-list |
| Markup inside a `.cshtml` | [`razor.md`](razor.md) |
| C# inside a `.cshtml.cs` | [`csharp.md`](csharp.md) |
