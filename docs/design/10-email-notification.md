---
status: reviewed
created: 2026-07-23
tags: [design, email, notification, outbox, deliverability, suppression]
---

# Email and notification subsystem (detailed design)

## 1. Decisions realized

| Decision | What this design applies |
|---|---|
| ADR-0038 | The owning decision: `IEmailSender<TUser>` shim to a cloud-agnostic `IEmailDispatcher`; transactional outbox with an at-least-once relay; two-tier throttle; anti-enumeration; suppression; per-purpose token lifespans |
| ADR-0006 / ADR-0009 | Cloud-agnostic port with a DB-default adapter; provider secrets resolve through the existing `ISecretResolver`, never plaintext config |
| ADR-0008 | Dead-letter and other email security events go on the `ISecurityEventSink` audit lane (append-only, hash-chained, delivery-guaranteed) |
| ADR-0022 | Operational detail goes on the `ILogger` + OpenTelemetry diagnostic lane with PII redaction; the two lanes are never mixed |
| ADR-0024 | Ports in `Nami.Identity.Abstractions`, relay/compose logic in core, provider SDKs confined to adapter packages; ArchUnitNET-enforced |
| ADR-0013 | Change-email requires step-up (`acr` >= aal2) before initiate; enforced at the 08/endpoint layer, the composer is invoked only after the gate |
| ADR-0040 | Polly `AddStandardResilienceHandler` on every provider call; the email anti-abuse throttle is the one deliberate fail-closed carve-out |
| ADR-0015 | The break-glass alert email is the most-critical at-least-once flow and uses a priority lane that bypasses both limiters |
| ADR-0042 / ADR-0038 | Throttle numbers are interim (owner: Product), tracked on the Pre-GA checklist |
| ADR-0021 | The version-sensitive Identity/provider contracts are re-verified at each .NET/OpenIddict bump via the seam catalogue |
| ADR-0028 / ADR-0037 | Consumed through `.AddUsers(...)` as a swappable port; the outbox/suppression DDL is realized on PostgreSQL |

## 2. Purpose and scope

Transactional mail is the one delivery path the whole product depends on and the
cross-doc audit (A04) flagged as the single real design hole: account confirmation,
password reset, MFA-related mail, change-email tripwires, and the break-glass alert
all flow through it. Because sign-in sets `SignIn.RequireConfirmedAccount = true`
(the SSOT is 08 / the hardening baseline), the framework default (a no-op sender that
renders the link to the browser) means **nobody can log in** until a real sender
exists. This design replaces the one-line placeholder task in the user-management
plan ("SMTP/SendGrid abstraction") with the full subsystem: a cloud-agnostic port and
adapters, a transactional outbox that is the reliability chassis other subsystems
reuse, anti-abuse throttling, anti-enumeration, templating/i18n, and bounce/complaint
suppression.

In scope: the `IEmailSender<TUser>` shim, the `IEmailDispatcher` port and its provider
adapters, the transactional outbox and relay (the shared at-least-once chassis),
two-tier throttling, anti-enumeration on the reset/resend endpoints, templating and
i18n, bounce/complaint suppression and webhook ingestion, per-purpose token lifespans,
and the delivery slice of change-email.

Out of scope: the schema (02 is the SSOT for `OutboxEmail` and `SuppressionEntry`; this
doc references it and owns behavior only), the change-email *flow and policy* (08), the
step-up *enforcement* that gates change-email (07), the audit sink internals (03), and
the reset/confirm UI pages (11). The self-service **endpoints** that trigger mail are
08's; what this design guarantees is that the email side of them cannot leak whether an
account exists.

The outbox chassis defined here is reused by the back-channel `logout_token` delivery over
`LogoutDeliveryOutbox` (a table defined in 02, storing delivery *intent* rather than a
payload), whose fan-out behaviour belongs to 11. This design owns the chassis, not that
fan-out. **One difference from this design's own tables matters to the relay:**
`LogoutDeliveryOutbox` is **class B, global** (02 section 1), not tenant-scoped. A session is
keyed by a global `sid` that can span a tenant switch, so the table carries no ambient-tenant
filter and no row-level security, and its `TenantId` is data rather than a discriminator. A
drain loop written for this design's tenant-scoped tables must therefore **not** be reused
verbatim for it: iterating per tenant would drop the deliveries for a session's other tenants.

## 3. Interfaces and contract

### 3.1 Framework facts this design is built on (verified .NET 10)

| Fact | Consequence |
|---|---|
| Two different interfaces exist: the legacy `IEmailSender` (one method, called only by scaffolded Razor) and `IEmailSender<TUser>` (8.0+, three methods, called by Identity infrastructure for confirm/reset) | Implement `IEmailSender<TUser>` as the integration point (it carries `TUser`, so branding/i18n can key off tenant and locale); implementing only the legacy one means confirm/reset never send |
| `IEmailSender<TUser>` exposes `SendConfirmationLinkAsync`, `SendPasswordResetCodeAsync`, `SendPasswordResetLinkAsync`; we do not map `MapIdentityApi` on the authorization-server host (decision recorded in 08 / user-management) | Reset uses the **link-style** path with a self-minted token; the `MapIdentityApi` reset-code path does not exist on this host |
| The default (no sender registered) is a no-op that renders the link to the browser (test only) | A real adapter is mandatory; `DisplayConfirmAccountLink = false` in production |
| Tokens come from `DataProtectorTokenProvider`; `DataProtectionTokenProviderOptions.TokenLifespan` defaults to one day | One day is too long for a security flow; subclass per purpose (5.6), never change the global default |
| Data-protection tokens contain `+`, `/`, `=` that corrupt in a URL | `WebEncoders.Base64UrlEncode` on mint and `Base64UrlDecode` on consume; this is the most common "invalid token" bug |
| Tokens are not intrinsically single-use; they are invalidated when the `SecurityStamp` changes | Keep the default security-stamp behavior; a successful reset rotates the stamp, killing outstanding tokens |
| `System.Net.Mail.SmtpClient` is not recommended by Microsoft ("use MailKit or other libraries instead") | The SMTP adapter uses MailKit |

### 3.2 Two-layer port and adapter

The Application/abstractions layer owns a cloud-agnostic port; the shim that Identity
calls composes a message and enqueues it, and never sends inline.

```csharp
// Nami.Identity.Abstractions (no provider SDK)
public sealed record EmailMessage(
    string ToAddress, string? ToDisplayName, string Subject,
    string HtmlBody, string PlainTextBody, string TemplateId,
    IReadOnlyDictionary<string, string> Metadata, // tenant, flow, correlationId
    string IdempotencyKey);

public interface IEmailDispatcher
{
    // Called by the relay to actually send through the configured adapter.
    Task<EmailSendResult> SendAsync(EmailMessage m, CancellationToken ct);

    // Direct enqueue for callers that own a control-plane transaction and cannot go
    // through the Identity shim (break-glass alert, proposal notification, invite).
    Task EnqueueAsync(EmailMessage m, CancellationToken ct);
}

// Infrastructure: the single IEmailSender<TUser> - composes + enqueues, never sends inline
public sealed class IdentityEmailSender<TUser>(IEmailComposer composer, IEmailOutbox outbox)
    : IEmailSender<TUser> where TUser : class
{
    public Task SendConfirmationLinkAsync(TUser u, string email, string link)
        => Enqueue(EmailFlow.Confirmation, u, email, link);
    public Task SendPasswordResetCodeAsync(TUser u, string email, string code)
        => Enqueue(EmailFlow.PasswordResetCode, u, email, code);
    public Task SendPasswordResetLinkAsync(TUser u, string email, string link)
        => Enqueue(EmailFlow.PasswordResetLink, u, email, link);

    private async Task Enqueue(EmailFlow f, TUser u, string email, string token)
    {
        var msg = await composer.ComposeAsync(f, u, email, token);
        await outbox.EnqueueAsync(msg); // same DbContext transaction as the user mutation
    }
}
```

The port carries **both** methods deliberately. `SendAsync` is the relay's call into an
adapter; `EnqueueAsync` is the path for a caller that already owns a control-plane
transaction and therefore cannot reach the outbox through the Identity shim (5.1). A
port with only `SendAsync` would leave that caller no atomic option, which is the
failure this design exists to remove.

`IEmailDispatcher` was already declared for this phase in the foundations ports catalog
(01). Its siblings introduced here are `IEmailComposer` (template resolution and
render), `IEmailOutbox` (transactional enqueue), and the webhook ports
`IWebhookSignatureVerifier` and `IWebhookEventParser`; all live in
`Nami.Identity.Abstractions`. Secret material (SMTP credentials, provider API keys,
webhook-signing secrets) resolves through the **existing** `ISecretResolver` (01,
ADR-0009), never plaintext config; this design declares no new secret port.

```mermaid
flowchart TB
  classDef port fill:#dae8fc,stroke:#6c8ebf,color:#000
  classDef adapter fill:#d5e8d4,stroke:#82b366,color:#000
  classDef infra fill:#ffe6cc,stroke:#d79b00,color:#000

  subgraph app["Application / Abstractions (no SDK)"]
    disp["IEmailDispatcher (port)"]:::port
    comp["IEmailComposer (port)"]:::port
    ob["IEmailOutbox (port)"]:::port
  end

  shim["IdentityEmailSender of TUser<br/>(the only IEmailSender of TUser)"]:::infra
  relay["EmailRelayBackgroundService"]:::infra

  subgraph adapters["Infrastructure adapters (provider SDK confined here)"]
    smtp["MailKit SMTP (default)"]:::adapter
    sg["SendGrid"]:::adapter
    ses["AWS SES v2"]:::adapter
    acs["Azure ACS"]:::adapter
    file["File / dev"]:::adapter
  end

  shim --> comp
  shim --> ob
  relay --> disp
  disp --> smtp
  disp --> sg
  disp --> ses
  disp --> acs
  disp --> file
```

### 3.3 Provider adapters and configuration

The adapter is chosen by `Nami:Email:Provider` (env `Nami__Email__Provider`),
mirroring the `Cloud:Provider` selector shape whose SSOT is the foundations config
layer (01 §1.14, `Database` default). Configuration binds a static section; production
values that change at runtime are managed through admin surfaces, not redeploys.

```jsonc
"Nami": { "Email": {
  "Provider": "Smtp",
  "FromAddress": "no-reply@auth.example",
  "FromDisplayName": "Nami Identity",
  "Smtp": { "Host": "...", "Port": 587, "UseStartTls": true },        // creds via ISecretResolver
  "SendGrid": { "ApiKeySecretRef": "kv://email/sendgrid-key" },
  "Ses": { "Region": "eu-north-1" },
  "Acs": { "ConnectionStringSecretRef": "kv://email/acs-conn" }
}}
```

| Provider | Package | Call shape | Note |
|---|---|---|---|
| SMTP (MailKit) | `MailKit` | `ConnectAsync(host, port, StartTls)` then `AuthenticateAsync` then `SendAsync(MimeMessage)` | Default / on-premises |
| SendGrid | `SendGrid` | `new SendGridClient(apiKey)` then `SendEmailAsync`; `SetClickTracking(false, false)` | Click-tracking off for security links |
| AWS SES v2 | `AWSSDK.SimpleEmailV2` | `SendEmailAsync(SendEmailRequest)` | Use v2; credentials via IAM role |
| Azure ACS | `Azure.Communication.Email` | `SendAsync(WaitUntil.Started, ...)` then poll `UpdateStatusAsync` | Non-blocking plus outbox poll |
| Dev / File | none | write `.eml` to a pickup dir / log | CI and local; no real cloud |

Adapters live in the phase-later packages named in the foundations package graph
(`Nami.Identity.Email.Smtp` / `.SendGrid` / `.Ses` / `.Acs`, under the
`Nami.Identity.*` root). No cloud SDK type leaks into the Application layer (the SOLID
layering rule); this is ArchUnitNET-enforced (ADR-0024). Licences are recorded in 6.2.

## 4. Data and structure

This design references two tables defined at design fidelity in the data-tier doc (02,
the schema SSOT) and realized on PostgreSQL (ADR-0037); it does not redefine columns or
types.

- **`OutboxEmail`** has **two homes**: one in `IdentityDbContext` (global, no `TenantId`,
  for confirm/reset, because identity is global) and one in `ControlPlaneDbContext`
  (which adds a `TenantId` column under FORCE RLS, for break-glass alert, admin/proposal,
  and invite mail). Key columns: `IdempotencyKey` (unique, prevents double-send), `Status`
  (`Pending`/`InFlight`/`Sent`/`DeadLettered`), `Attempts`, `NextAttemptAt`, `Payload`,
  `ProviderMessageId`. The index `(Status, NextAttemptAt)` backs the relay claim via
  `SKIP LOCKED`.
- **`SuppressionEntry`** (control-plane, tenant-columned) stores `RecipientHash`
  (`bytea`, hash only, never the address, DP.01), `Reason`, `ExpiresAt`, indexed
  `(TenantId, RecipientHash)`.

Both tenant-columned tables carry the discriminator as `varchar(64)` holding
`Tenants.Identifier`, so their RLS predicate is the plain text form
`TenantId = current_setting('app.current_tenant', true)` (02). An unset GUC then simply
fails to match and returns zero rows, which is fail-closed by construction with no cast to
get wrong. The relay's per-tenant iteration must still set the ambient tenant and the GUC
before touching the control-plane copies, or it sees nothing rather than everything. Both
tables live in `ControlPlaneTenantDbContext`, which is **non-pooled** for the T7 reason in
02 section 1, so the relay must not assume a pooled context here.

The `Payload` holds a live bearer token until the row is `Sent`, which is why it is
redacted at that point (section 5.1) and never logged (section 8).

## 5. Behaviour

### 5.1 The transactional outbox and relay

Two failure modes must be designed out: **send-before-commit** (the confirmation goes
out, then registration rolls back) and **lost-after-commit** (the user commits, then
the process crashes before the mail is sent, so the account can never confirm). Both
are eliminated by writing an `OutboxEmail` row in the *same* database transaction as the
user mutation, and sending from a background relay afterward.

The subtle part is that Identity's own `IEmailSender<TUser>` callback cannot provide
this. `UserManager.CreateAsync` / `ResetPasswordAsync` call `SaveChangesAsync`
internally and Identity invokes the sender only *after* the method returns, so an
enqueue inside that callback lands in a **later** transaction, which is exactly the
lost-after-commit case. Critical flows therefore own the transaction boundary
explicitly and bypass the auto-email callback:

```csharp
await using var tx = await identityDb.Database.BeginTransactionAsync(ct);
var r = await userManager.CreateAsync(user, password);   // internal SaveChanges joins the ambient tx (uncommitted)
if (!r.Succeeded) return; // tx disposes -> rollback, no orphan user, no mail
var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
outbox.Enqueue(BuildConfirmEmail(user, token));          // OutboxEmail row on the same DbContext
await identityDb.SaveChangesAsync(ct);                   // user + outbox in one SaveChanges
await tx.CommitAsync(ct);                                // atomic: both commit or both roll back
```

A non-critical notification may enqueue after commit and accept a possible resend; the
confirm and reset flows may not, because the failure there is an account that exists and
can never be confirmed.

Because the Identity shim is wired to `IdentityDbContext`, mail emitted from the
**control plane** (break-glass alert, admin/proposal such as the terminal
`Failed(target_changed)` proposal notification, and invites) cannot get same-transaction
atomicity through it. The outbox therefore has its second home in `ControlPlaneDbContext`
with a direct `IEmailDispatcher.EnqueueAsync` path on the control-plane transaction, and
**one** `EmailRelayBackgroundService` polls both tables.

The relay:

- **Claims** a pending row with optimistic concurrency (`SKIP LOCKED`), so two relay
  instances never double-send.
- **Retries** transient failures (5xx, throttling, network) with exponential backoff
  plus jitter, capped at about six attempts, using `NextAttemptAt`.
- Wraps every outbound provider call in Polly `AddStandardResilienceHandler` (a single
  handler: total timeout ~30s, per-attempt ~10s, retry with jitter, circuit breaker;
  retries disabled for non-idempotent verbs) per ADR-0040.
- **Dead-letters** a row that exhausts the cap into `DeadLettered`, emits a security
  event on the audit lane, and pages. A dead-lettered break-glass alert is a security
  incident.
- **Redacts** the live token from the `Payload` once the row reaches `Sent`, and stores
  the `ProviderMessageId` for correlation.

The break-glass alert runs on a **priority lane** (sync-with-fallback) so it is never
stuck behind a confirmation backlog, and it bypasses both throttle limiters. It is
enqueued on the control-plane transaction, consistent with the break-glass ordering in
[15](15-admin-api.md), where `audit.RecordSuccessAndAlert()` runs, fail-closed, *before*
`SignInAsync` (ADR-0015).

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant EP as Endpoint (register / reset)
  participant DB as IdentityDbContext
  participant RB as EmailRelayBackgroundService
  participant PR as Provider adapter

  U->>EP: submit
  EP->>DB: BeginTransaction
  EP->>DB: CreateAsync / mint token (joins tx, uncommitted)
  EP->>DB: Enqueue OutboxEmail (Pending) same tx
  EP->>DB: SaveChanges + Commit (atomic)
  EP-->>U: constant-time response (no account disclosure)
  Note over RB: later, decoupled
  RB->>DB: claim Pending row (SKIP LOCKED), mark InFlight
  RB->>PR: SendAsync (Polly-wrapped)
  PR-->>RB: accepted + provider message id
  RB->>DB: mark Sent, store ProviderMessageId, redact token
```

```mermaid
stateDiagram-v2
  [*] --> Pending: enqueued in mutation tx
  Pending --> InFlight: relay claims (SKIP LOCKED)
  InFlight --> Sent: provider accepts
  InFlight --> Pending: transient failure, backoff + jitter (Attempts++)
  Pending --> DeadLettered: attempts exhausted (~6)
  DeadLettered --> [*]: security event + page
  Sent --> [*]: token redacted from Payload
```

### 5.2 Throttle and anti-abuse

Two limiters with different breach behavior, plus a deliberate fail-closed carve-out.

- **Per-recipient (anti-abuse, may deny).** A rolling-window cap: interim defaults of
  five security-emails per recipient per hour, a sub-cap of three password-resets per
  hour, and a hard ceiling of ten. Up to the ceiling the relay enqueues-with-delay;
  past it, it **drops with an audit event**. The counter is persisted (a Redis
  sorted-set, or an outbox row-count) so it survives restarts, keyed on `recipientHash`
  only (tenant-agnostic, because identity is global). **This limiter runs in the relay,
  on the dequeued row, after the constant-time HTTP response has already returned**: it
  is never applied synchronously before enqueue at the endpoint, because that would
  reintroduce a timing oracle and violate the anti-enumeration invariant in 5.3.
- **Global (reputation/quota, lossless).** A standalone
  `System.Threading.RateLimiting.TokenBucketRateLimiter` sized to about 80% of the
  adapter's provider quota. A breach applies `AcquireAsync` backpressure (wait, leave
  the row `Pending`); it never drops. Default-on, disable-able per adapter.
- **Redis fail-closed carve-out.** The per-recipient cap is a *security* control and is
  never "cap disabled" on a Redis outage, the single deliberate exception to the
  otherwise fail-open Redis-as-accelerator posture (ADR-0040, and 19 for the capacity
  view). On a Redis outage it degrades to a per-instance in-process bucket plus an
  outbox-row-count counter; the cap stays enforced, accepting per-instance inaccuracy
  rather than switching it off.
- The break-glass alert bypasses both limiters.

The throttle numbers are interim (owner: Product) and tracked on the Pre-GA checklist
(ADR-0042 / ADR-0038).

```mermaid
flowchart TD
  classDef drop fill:#f8cecc,stroke:#b85450,color:#000
  classDef wait fill:#fff2cc,stroke:#d6b656,color:#000
  classDef go fill:#d5e8d4,stroke:#82b366,color:#000

  A["Relay claims row (after constant-time response)"] --> BG{Break-glass?}
  BG -->|yes| SEND["Send (bypass both limiters)"]:::go
  BG -->|no| PR{Per-recipient count vs ceiling}
  PR -->|over ceiling| DROP["Drop with audit event"]:::drop
  PR -->|under ceiling| GL{Global token available?}
  GL -->|no| WAIT["Backpressure: leave Pending, wait"]:::wait
  GL -->|yes| SEND
  WAIT --> GL
```

### 5.3 Anti-enumeration on the reset and resend endpoints

`/forgotPassword` and `/resendConfirmationEmail` **always return the same response with
the same latency** whether or not the account exists or is confirmed: the handler runs
`FindByEmailAsync` plus `IsEmailConfirmedAsync`, silently skips on failure, and never
branches the HTTP result or the timing ("do not reveal that the user does not exist").
An endpoint rate limiter keys on IP plus email-hash, and it is a **different mechanism**
from the per-recipient relay throttle in 5.2: this one shapes request volume at the
edge, that one shapes send volume per mailbox after the response is already out.

A **latency-invariance test is a mandatory, permanent acceptance criterion**; it is what
keeps the per-recipient throttle from creeping back to the endpoint. These are net-new
custom minimal endpoints (they are not OIDC endpoints in the endpoint catalogue, and
`MapIdentityApi` is deliberately not mapped on this host); the webhook route
`/webhooks/email/{provider}` is likewise a custom minimal endpoint.

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant EP as /forgotPassword
  participant UM as UserManager
  participant OB as Outbox

  U->>EP: POST email
  EP->>UM: FindByEmailAsync + IsEmailConfirmedAsync
  alt exists and confirmed
    EP->>OB: enqueue reset mail (throttle applied later, in relay)
  else missing / unconfirmed
    Note over EP: silently skip, no enqueue
  end
  EP-->>U: identical 200 + identical latency (no disclosure)
```

### 5.4 Templating, i18n, and deliverability

`IEmailComposer` resolves a template by `(flow, tenant, culture)` from `TUser` and
renders **both** an HTML body and a plain-text body (multipart/alternative). The engine
is a sandboxed one (Fluid or Scriban, implementation-open), never Razor for
tenant-editable templates, which would execute C#. Per-tenant branding (from-address,
logo) comes from the tenant registry with a global fallback.

Two fallback chains keep rendering total:

- **String i18n:** requested culture (for example `nb-NO`) then neutral culture (`nb`)
  then the default `en` floor; never throw, warn once on a missing key. Reuse
  `IStringLocalizer` / `ResourceManager` fallback.
- **Template resolution:** `(flow, tenant, culture)` then tenant-override-any-culture
  then global-template-that-culture then global `en`. `en` is the hard floor that
  always renders.

Target locales are configuration-driven (for example `en`, `nb-NO`, `nl`, `vi`), not
hard-coded. Deliverability: SPF/DKIM/DMARC on a dedicated sending subdomain (for example
`auth.<domain>`), click-tracking off for security links, and HTTPS absolute links to the
canonical domain.

### 5.5 Bounce and complaint suppression, and webhooks

A canonical `SuppressionEntry` store (control-plane, tenant-columned, 02) is checked
**at dispatch, in the relay**: a lookup on `(TenantId, RecipientHash)` filtered by
`ExpiresAt`, immediately before `SendAsync`. The placement matters and is easy to get
wrong, because it is the same trap as the throttle: a synchronous suppression check at
the endpoint, before enqueue, would make a suppressed address behave differently from an
unsuppressed one at request time, which is a disclosure oracle of exactly the kind 5.3
forbids. The check therefore belongs after the constant-time response, on the dequeued
row.

Provider-native suppression lists are deliberately **not** used: account-wide lists leak
across tenants (violating the tenant-isolation decision), SMTP and File have none, and
the sync API would sit in the hot path.

Suppression is populated from provider webhooks at `/webhooks/email/{provider}`. Each
provider's native signature scheme is verified over the **raw** request body through the
`IWebhookSignatureVerifier` / `IWebhookEventParser` ports with a per-provider adapter; a
generic HMAC middleware is rejected because it is cryptographically impossible across
the three schemes without disabling native auth on a public endpoint.

| Provider | Verification |
|---|---|
| AWS SES via SNS | SigV2 SHA256 over the canonical string, host-pinned HTTPS `SigningCertURL` plus certificate-chain validation, auto-confirm subscription; prefer the AWS SDK built-in validator over hand-rolling |
| SendGrid | ECDSA/SHA256 over `(timestamp + raw body)` via the official (MIT) `RequestValidator`, rejecting timestamp skew; the curve is taken from the helper source, not assumed |
| Azure Event Grid / ACS | No HMAC scheme, so a high-entropy URL secret (constant-time compare) plus optional Entra bearer; handle `SubscriptionValidationEvent` and the CloudEvents OPTIONS handshake |

TTL rules: a permanent reason (hard bounce, complaint) sets `ExpiresAt` NULL and is
cleared only by an audited admin action; a soft/transient reason carries a TTL
(interim default 72h) with a sweeper. Events correlate back to sent mail via
`ProviderMessageId`. Whether the recipient is stored as a hash or encrypted, and the
soft-bounce TTL, are DPO-gated (DP.01) and tracked on the Pre-GA checklist; the interim
baseline is hash-only.

```mermaid
sequenceDiagram
  autonumber
  participant PV as Provider (bounce / complaint)
  participant WH as /webhooks/email/{provider}
  participant VF as IWebhookSignatureVerifier
  participant PS as IWebhookEventParser
  participant SUP as SuppressionEntry (control-plane)

  PV->>WH: POST raw body (+ signature / SNS envelope)
  WH->>VF: verify native scheme over RAW body
  alt signature invalid
    WH-->>PV: 4xx reject (no state change)
  else valid
    WH->>PS: parse event(s)
    PS->>SUP: upsert (TenantId, RecipientHash, Reason, ExpiresAt)
    WH-->>PV: 2xx ack
  end
```

### 5.6 Per-purpose token lifespans

A subclassed `DataProtectorTokenProvider` per purpose, confirmation ~4h, password-reset
~1h, change-email ~1h, registered through
`config.Tokens.EmailConfirmationTokenProvider` / `PasswordResetTokenProvider`; the global
one-day default is left unchanged. Tokens are `Base64Url`-encoded on mint and decoded on
consume, and are invalidated by `SecurityStamp` rotation (they are not intrinsically
single-use).

### 5.7 Change-email: the delivery slice

The change-email flow, policy, and its four-branch test are owned by the user-management
design (08): step-up (`acr` >= aal2) before initiate, verify the new address before the
switch takes effect, and on completion rotate the `SecurityStamp` (so the 1-2 minute
`ValidationInterval` forces re-login) and revoke the refresh-token family. This subsystem
owns only the *delivery* mechanics of two of those obligations:

- **Notify the old address on request** via a dedicated `EmailFlow.EmailChangeNotifyOld`
  template, a tripwire carrying a "contact support if this was not you" call to action
  and **no token or actionable link** (so it cannot itself become a phishing template),
  routed through the outbox like any security mail.
- **Verify the new address before the switch** via a ~1h change-email token sent to the
  new address; the old address remains the login until verification completes.

The step-up gate is enforced at the 08/endpoint layer, and the composer is invoked only
after it passes; the `SecurityStamp` rotation and refresh revocation are also 08's. This
doc coordinates with, and does not duplicate, that flow.

## 6. Dependencies and wiring

### 6.1 Patterns applied

Patterns applied (ADR-0066): **Transactional Outbox** (atomic enqueue plus at-least-once
relay), **Adapter** (per provider and per webhook scheme), **Strategy** (provider
selection via config), **Ports and Adapters** (the cloud-agnostic seam), and a thin
**Humble Object** (the `IdentityEmailSender<TUser>` shim holds no logic beyond
compose-and-enqueue).

### 6.2 Libraries

All permissive (MIT / Apache-2.0 / BSD-class) per ADR-0026; none is commercial or
copyleft. The identifiers are exact so the licence-scan gate can act on them. The
licences below are split by how they were established, because the gate consumes facts
and not recollections:

**Read from package metadata:**

| Package | Licence |
|---|---|
| `MailKit` (and its `MimeKit` dependency) | MIT |
| `SendGrid` | MIT |
| `Polly` | BSD-3-Clause |
| `System.Threading.RateLimiting` | MIT |

**Not verified offline, to be confirmed by the licence-scan gate:**
`AWSSDK.SimpleEmailV2`, `Azure.Communication.Email`, and the templating engine (`Fluid`
or `Scriban`, still implementation-open). Each is expected to be permissive, and none is
selected until the gate confirms it; recording them as unverified is deliberate rather
than an omission.

### 6.3 Wiring

`IdentityEmailSender<TUser>` is registered as `IEmailSender<TUser>`, with the auto-email
callback bypassed for the same-transaction confirm and reset flows (5.1). `OutboxEmail`
lives in both `IdentityDbContext` and `ControlPlaneDbContext`, and a single
`EmailRelayBackgroundService` polls both. The composer, the per-purpose token providers,
the two throttle limiters, and the suppression store plus its webhook verifier and parser
adapters are registered alongside. The two observability lanes stay separate: diagnostics
through `ILogger` and OpenTelemetry with PII redaction (ADR-0022), security and
dead-letter events through `ISecurityEventSink` (ADR-0008).

## 7. Error handling, edge cases, invariants

- **Atomic same-transaction enqueue** for confirm and reset, never through the Identity
  callback; both send-before-commit and lost-after-commit are designed out.
- **Idempotency**: a unique `IdempotencyKey` plus a claimed row, so two relays never
  double-send.
- **Anti-enumeration**: identical response and identical latency; the per-recipient cap
  and the suppression check both run in the relay, never synchronously at the endpoint.
- **Base64Url** encode on mint and decode on consume, with per-purpose short lifespans
  rather than the one-day default.
- **No secret, token, link, or body in any log**; the token is redacted from the
  `Payload` once `Sent`.
- **A permanent suppression has a NULL expiry** and is cleared only by an audited admin
  action.
- **Break-glass bypasses both throttles** and rides the priority lane; a dead-lettered
  break-glass alert pages.
- **Webhook bodies are verified by native signature over the raw body** before any
  suppression write, and an invalid signature changes no state.

## 8. Security and multi-tenancy notes

- **No secret logging (hard rule).** Never log the token, link, or body; log only the
  recipient hash, flow, tenant, correlation/idempotency id, provider message id, and
  status. The outbox `Payload` holds a live token, so it is redacted from the row once
  `Sent`.
- **Two lanes, never mixed.** Email security events (dead-letter, throttle drop,
  break-glass alert failure) go on the `ISecurityEventSink` audit lane (append-only,
  hash-chained, delivery-guaranteed; ADR-0008). Operational detail goes on the
  `ILogger` plus OpenTelemetry diagnostic lane with PII redaction via
  `Microsoft.Extensions.Telemetry` / `Microsoft.Extensions.Compliance.Redaction`
  (ADR-0022). The two are joined only by a correlation/trace id.
- **Tenant scope.** The control-plane `OutboxEmail` home and `SuppressionEntry` are
  tenant-columned (Pool query-filter plus FORCE RLS, Silo per-database); the
  `IdentityDbContext` home is global with no `TenantId`, because identity is global.
- **PII minimisation.** Suppression and logs carry a `RecipientHash`, never the address.
- **Anti-enumeration** and the relay-side placement of both the throttle and the
  suppression check are security invariants, not conveniences; both carry mandatory tests.
- **Webhook endpoints** are public and unauthenticated at the network edge, so signature
  verification over the raw body is the only trust boundary.
- **Verify-before-build (ADR-0021).** The `IEmailSender<TUser>` contract,
  `DataProtectionTokenProviderOptions`, and each provider's request builder are
  version-sensitive; they are re-verified at each .NET / OpenIddict bump through the seam
  catalogue and the contract-regression suite.

### Audit events

The audit catalog (03) has no email-specific events yet. This design **reuses** the
existing generic events where they fit (`break_glass` for the break-glass alert path,
`degraded_mode_enabled` for the Redis fail-closed degrade, `mass_revoke` where relevant)
and proposes a **minimal** net-new set, `email_dead_letter`, `email_send_suppressed`,
and `email_throttle_drop` (snake_case, matching the catalog convention). Per the design
decision rule these are raised as a proposed addition to the ADR-0008 catalog and flagged
in Open items, not settled inside this feature doc; the audit minimum-catalog line on the
Pre-GA checklist (ADR-0008) is where they land.

## 9. Testing

- **Outbox atomicity:** a rolled-back user mutation leaves no outbox row (no orphan
  user with no mail); a committed one leaves exactly one; two concurrent relays never
  double-send (idempotency-key + `SKIP LOCKED`).
- **Latency invariance:** `/forgotPassword` and `/resendConfirmationEmail` return
  identical response and timing for existing/confirmed, existing/unconfirmed, and
  missing accounts, a permanent invariant, and neither the per-recipient cap nor the
  suppression check runs before enqueue.
- **Throttle under Redis outage:** the per-recipient cap stays enforced per-instance
  when Redis is down (never "cap disabled").
- **Webhook signature verification:** each provider adapter accepts a genuine signed
  payload and rejects a tampered body, a bad signature, and (SendGrid) a skewed
  timestamp.
- **Suppression:** a suppressed `RecipientHash` is not sent, and the check happens at
  dispatch rather than at the endpoint.
- **i18n fallback:** a missing key falls through to the `en` floor and warns once; a
  missing tenant template falls through to the global template.
- **Token hygiene:** `Base64Url` round-trip on mint/consume; a rotated `SecurityStamp`
  invalidates an outstanding token; per-purpose lifespans (confirm 4h, reset 1h).
- **Break-glass lane:** the alert is not starved behind a confirmation backlog and
  bypasses both limiters; a dead-lettered alert pages.
- **No-secret-logging:** no token, link, or body appears in any log.

## 10. Open and build-time items

- **Proposed audit events** (`email_dead_letter`, `email_send_suppressed`,
  `email_throttle_drop`), raised as an addition to the ADR-0008 catalog; tracked under
  the audit minimum-catalog Pre-GA line.
- **Throttle numbers** (per-recipient 5/hr, sub-cap 3, ceiling 10) and the Redis
  fail-closed deviation: interim (Nam, 2026-07-04), await Product ratification
  (Pre-GA checklist, ADR-0042 / ADR-0038).
- **Suppression** hash-versus-encrypt and soft-bounce TTL (72h) plus complaint
  auto-expiry: interim hash-only baseline, await DPO ratification (DP.01; Pre-GA
  checklist, ADR-0038).
- **Licences to confirm at the gate:** `AWSSDK.SimpleEmailV2`,
  `Azure.Communication.Email`, and whichever templating engine is chosen (6.2).
- **Ops / DNS:** SPF/DKIM/DMARC on the sending subdomain; provider account and quota.
- **Same-transaction outbox** remains a named implementation-time code spike to validate
  the transaction-boundary mechanism with running code (per the architecture-grading
  bucket).
- **Verify-before-build:** re-verify the version-sensitive Identity/provider contracts at
  each .NET / OpenIddict bump (ADR-0021).

## 11. Sources

- ADR-0038 (email/notification subsystem), ADR-0006 / ADR-0009 (cloud-agnostic port,
  `ISecretResolver`), ADR-0008 (audit sink), ADR-0022 (diagnostic lane), ADR-0024
  (hexagonal ports), ADR-0013 (step-up), ADR-0021 (verify-before-build), ADR-0040
  (resilience and the fail-closed carve-out), ADR-0015 (break-glass alert), ADR-0042
  (abuse/throttle numbers), ADR-0026 (licences), ADR-0066 (patterns), ADR-0028 /
  ADR-0037 (packaging, PostgreSQL DDL).
- Design docs: [02 data tier](02-data.md) (schema SSOT for `OutboxEmail` /
  `SuppressionEntry`), [03 audit](03-audit.md) (two lanes, security events),
  [08 user management](08-user-management.md) (the self-service endpoints, the
  change-email flow, `RequireConfirmedAccount`, the `SecurityStamp` rotation),
  [11 login, consent, and logout](11-login-consent-ui.md) (the reset/confirm pages, and
  the back-channel logout fan-out that reuses this outbox chassis),
  [15 admin API](15-admin-api.md) (the break-glass ordering),
  [01 foundations](01-foundations.md) (ports catalog, `Cloud:Provider` selector, package
  graph), [19 observability](19-observability-capacity-slo.md) (the capacity view of the
  Redis posture).
- [Architecture](../architecture/README.md): components view (email subsystem),
  runtime view 8 (transactional email outbox).
- Package licences read from package metadata where stated in 6.2; the remainder are
  explicitly marked unverified there rather than assumed.
- [Pre-GA ratification checklist](../PRE-GA-RATIFICATION-CHECKLIST.md) (throttle numbers;
  suppression hash/TTL; audit minimum catalog).

---

[Prev: Federation and the claims profile](09-federation-and-claims-profile.md) · [Index](README.md) · Next: [Login, consent, and logout UI](11-login-consent-ui.md)
