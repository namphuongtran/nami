---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: ASP.NET Core Identity 10 email-sender contract; MailKit / SendGrid / AWS SES v2 / Azure Communication Services provider docs; evidence R12
informed: all contributors, via this repository
---

# Build email delivery as a first-class, cloud-agnostic subsystem with a transactional outbox

## Context and Problem Statement

Email sits on the critical path of the identity product: account confirmation, password reset, MFA and recovery delivery, security notifications, and the break-glass alert all depend on it. A cross-document audit found email to be the one genuine design hole: the plan carried only a one-line `IEmailSender` stub, and ASP.NET Core Identity's default when no sender is registered is a no-op that renders the confirmation link to the browser (test-only). With `RequireConfirmedAccount` enabled and a no-op sender, no one can complete registration or reset a password, so nobody can log in.

Email also concentrates several security surfaces that a naive sender gets wrong: an account-enumeration oracle on the reset and confirmation endpoints, an account-takeover surface on email change, bearer tokens (confirm/reset links) that must never leak into logs, and two distinct reliability failure modes (sending before the user transaction commits, and losing the message after it commits so the user exists but can never receive email).

How should Nami deliver email so that it is reliable, cloud-neutral, and does not open these security holes?

## Decision Drivers

* Delivery is on the login-critical path, so a missed or duplicated message is a login outage or a security event, not a cosmetic bug.
* Cloud-agnostic by the same port/adapter discipline as ADR-0006/ADR-0009/ADR-0024: it must run on-premises, locally, and against any cloud provider, chosen by configuration.
* Delivery must survive both transaction rollback and process crash (no send-before-commit, no lost-after-commit), following the outbox/delivery-guarantee discipline of ADR-0008.
* The reset/confirmation endpoints must not leak whether an account exists (uniform response and latency).
* Bearer tokens carried in email must never be logged; secrets are resolved through `ISecretResolver` (ADR-0009), never plaintext config.
* Email change is the top self-service account-takeover surface and must be hardened.

## Considered Options

* Keep the one-line no-op / single-method `IEmailSender` stub
* Rely on ASP.NET Core Identity's built-in auto-email callback for confirm/reset
* A first-class cloud-agnostic subsystem: an `IEmailDispatcher` port plus provider adapters, a transactional outbox with an at-least-once relay, anti-enumeration, and a suppression store

## Decision Outcome

Chosen option: "A first-class cloud-agnostic subsystem", because email is on the critical path and the two rejected options either do not send at all or send in the wrong transaction. The subsystem is fixed by the following parameters.

* **A. Integration point and layering.** Implement Identity's generic `IEmailSender<TUser>` (the 8.0+ interface, which carries `TUser` so branding and locale can be resolved), not the legacy single-method `IEmailSender`. The shim composes the message and enqueues it; it never sends inline. The cloud-agnostic `IEmailDispatcher` port lives in the Application layer; per-provider adapters live in Infrastructure and are selected by the `Nami:Email:Provider` configuration key (env `Nami__Email__Provider`): MailKit SMTP as the on-premises default, plus SendGrid, AWS SES v2, Azure Communication Services, and a File/dev adapter. No cloud SDK type leaks into the Application layer (ADR-0024); provider secrets resolve through `ISecretResolver` (ADR-0009).
* **B. Delivery guarantee via a transactional outbox.** The message is enqueued as an `OutboxEmail` row in the **same database transaction** as the user mutation, and an `EmailRelayBackgroundService` sends it afterward. This must **not** rely on Identity's `IEmailSender<TUser>` callback: `UserManager` calls `SaveChangesAsync` internally and the framework invokes the sender only after the method returns, which would place the outbox row in a later transaction and reintroduce the lost-after-commit mode. The critical flows therefore control the transaction boundary explicitly (open an ambient `BeginTransactionAsync`, create the user, mint the token, enqueue the outbox row, one `SaveChangesAsync`, commit). Because same-transaction enqueue only works within the context that owns the row, `OutboxEmail` has a home in **both** `IdentityDbContext` (confirm/reset) and `ControlPlaneDbContext` (break-glass alert, admin/proposal, invite), and one relay polls both. The relay is at-least-once with an idempotency-key unique index and a claim step (optimistic concurrency / `SKIP LOCKED`) so two relays never double-send, exponential backoff with jitter on transient failures (cap ~6), and a dead-letter state that emits a security event (ADR-0008) and pages. The break-glass alert uses a priority lane so it cannot queue behind a confirmation-email backlog.
* **C. Abuse and reputation controls (two limiters, different breach behavior).** A per-recipient anti-abuse limiter (rolling window, may drop-with-audit at the ceiling) and a global provider-quota limiter (token-bucket sized to about 80% of the adapter's quota, back-pressure, never drops). The per-recipient limiter is enforced **inside the relay after a constant-time response has already been returned**, never synchronously before enqueue, so it cannot become a timing oracle (see D). On a Redis outage both degrade to an in-process counter (a deliberate fail-closed carve-out in the resiliency and overload-protection posture, ADR-0040); the cap is never simply disabled. Break-glass bypasses both limiters.
* **D. Anti-enumeration.** `/forgotPassword` and `/resendConfirmationEmail` always return the same response with the same latency whether or not the account exists or is confirmed; the lookup runs and silently skips on failure with no HTTP or timing branch. Endpoint rate limiting keys on IP plus email-hash. A latency-invariance test is a mandatory acceptance invariant.
* **E. Suppression and bounce/complaint handling.** A canonical `SuppressionEntry` table in `ControlPlaneDbContext` (tenant-columned, storing only a `RecipientHash`, never the address) is checked immediately before dispatch. Provider bounce/complaint webhooks are verified by each provider's native signature scheme over the raw request body, through an `IWebhookSignatureVerifier` / `IWebhookEventParser` port with per-provider adapters. Permanent suppressions (hard bounce, complaint) persist until an audited admin action clears them; soft/transient ones carry a TTL.
* **F. Token and privacy hygiene.** Per-purpose token lifespans via subclassed `DataProtectorTokenProvider` (confirmation ~4h, password reset ~1h; the global 1-day default is not changed), with `Base64Url` encode/decode. Tokens, links, and bodies are never logged and are redacted from the outbox row once sent; the diagnostic lane is `ILogger` + OpenTelemetry with PII redaction (ADR-0022), and dead-letter/security events go to the separate `ISecurityEventSink` lane (ADR-0008).
* **G. Templating and i18n.** A sandboxed template engine (Fluid or Scriban), never Razor for tenant-editable templates, producing both HTML and plain-text parts; per-tenant branding from the tenant registry with a global fallback; a culture fallback chain down to an `en` floor that always renders.
* **H. Change-email hardening.** Notify the **old** address on request (a tripwire carrying a support contact, no action token); require step-up re-authentication (acr ≥ aal2, ADR-0013) before initiating; verify the **new** address before the switch takes effect; and rotate the security stamp on completion so existing sessions and refresh tokens are invalidated.

### Consequences

* Good, because email becomes reliable on the critical path: a rolled-back registration sends nothing, and a committed one is guaranteed to be delivered at least once even across a crash.
* Good, because the subsystem is cloud-neutral and swappable by configuration, matching the on-premises/local/any-cloud requirement, with no cloud SDK leaking above Infrastructure.
* Good, because the enumeration oracle, the email-change takeover surface, and token leakage are closed by design rather than left to each call site.
* Bad, because it is a substantial build (outbox in two contexts, a relay, two limiters, webhook signature verification, a template engine) far larger than the one-line stub it replaces.
* Bad, because the critical flows must own their transaction boundary explicitly rather than leaning on Identity's convenience callback, which is a discipline that is easy to get wrong.
* Neutral, because a non-critical notification may still enqueue after commit and accept a possible resend, so the strict same-transaction path is reserved for confirm/reset/break-glass.

### Confirmation

* An atomicity test proves a rolled-back user mutation leaves no outbox row (and no orphan user with no email), and that a committed one always leaves exactly one.
* An idempotency test proves two concurrent relays do not double-send.
* The latency-invariance test on the reset/confirmation endpoints is a permanent acceptance criterion.
* Dead-lettering a break-glass alert raises a security event and pages.
* Verify-before-build items are tracked under ADR-0021 and re-verified on each bump, because the `IEmailSender<TUser>` contract, `DataProtectionTokenProviderOptions`, and the provider request builders are version-sensitive.

## Pros and Cons of the Options

### Keep the one-line no-op / single-method `IEmailSender` stub

* Good, because there is nothing to build.
* Bad, because it does not actually send, so with confirmation required nobody can log in; it has no reliability, no anti-enumeration, and no suppression.

### Rely on Identity's built-in auto-email callback

* Good, because it needs the least code and uses the framework's own hook.
* Bad, because the callback fires after `UserManager` has already committed its own `SaveChanges`, so enqueuing there lands the message in a later transaction, which is exactly the lost-after-commit failure the design must eliminate; it also gives no control over anti-enumeration timing, throttling, or suppression.

### First-class cloud-agnostic subsystem (chosen)

* Good, because it delivers reliably and cloud-neutrally and closes the enumeration, takeover, and token-leak surfaces by construction.
* Bad, because it is the largest of the three to build and requires explicit transaction discipline in the critical flows.

## More Information

* This subsystem arose from the cross-document audit that identified email as the only genuine design hole (evidence R12). The `OutboxEmail` and `SuppressionEntry` DDL lives in the database design (doc 19 §5.6) and is realized under ADR-0037 (PostgreSQL). The change-email hardening (H) was added in the 2026-07-13 pre-implementation review; the throttle numbers (Product) and the suppression hash-vs-encrypt choice plus soft-bounce TTL (DPO, DP.01) are interim-accepted and await ratification.
* Related decisions: ADR-0006/ADR-0009 (the cloud-agnostic port/adapter and secret-resolution discipline this mirrors), ADR-0008 (the outbox/delivery-guarantee discipline and the security-event lane), ADR-0013 (step-up before email change), ADR-0015 (the break-glass alert this must never drop), ADR-0022 (the diagnostic logging lane with PII redaction), ADR-0024 (hexagonal ports keeping provider SDKs out of the Application layer), ADR-0028 (user management, whose confirm/reset/change-email flows call this), ADR-0037 (PostgreSQL, where the outbox and suppression tables live), and ADR-0040 (the resiliency and overload-protection posture, whose one deliberate fail-closed carve-out is this email anti-abuse throttle).
* Authored in this repository in 2026-07 to record the settled email-subsystem design as an ADR; neutral third-party providers and libraries (MailKit, SendGrid, AWS SES, Azure Communication Services, Fluid, Scriban) are named factually for identification only.
