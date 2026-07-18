---
status: "accepted"
stack-record: true
date: 2026-07-13
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect and security lead
consulted: OWASP credential-stuffing / anti-automation guidance; ASP.NET Core rate-limiting middleware; RFC 8628 (device flow) and RFC 9126 (PAR)
informed: all contributors, via this repository
---

# Add a layered anti-automation and abuse-defense posture beyond IP rate-limiting and account lockout

## Context and Problem Statement

An internet-facing identity provider is a standing target for credential stuffing, bot signups, and flow-level flooding. The baseline controls already decided (per-IP rate limiting and ASP.NET Core Identity account lockout) are necessary but not sufficient, and one of them can be turned against the service:

* A botnet performing credential stuffing arrives from thousands of source IPs, so a per-IP rate limit never trips.
* Per-account lockout is itself a denial-of-service weapon: an attacker can deliberately fail logins to lock a victim's account (and, worst of all, the break-glass rescue account).
* Two build-interim flows add their own abuse surface. The device flow, as shipped, emits no polling `interval` and never returns `slow_down`, so there is no server-enforced polling backoff on unauthenticated device-code polling. Pushed Authorization Requests (PAR) can be spammed to exhaust the `request_uri` store.

What additional defenses does Nami need so that distributed automated abuse is resisted, lockout cannot be weaponized, and the advanced flows cannot be flooded, without degrading the experience of ordinary users?

## Decision Drivers

* Distributed (many-IP) credential stuffing must be resisted, which per-IP limits cannot do.
* Account lockout must not be usable to deny a victim (or the rescue account) access.
* Ordinary users should almost never see an interactive challenge.
* Anti-automation providers are third-party services and must stay behind a cloud-agnostic port and be disabled in development.
* The advanced flows (device, PAR) must have real DoS ceilings, not only protocol hints.

## Considered Options

* Rely on per-IP rate limiting and account lockout alone
* Always-on interactive challenge (CAPTCHA for everyone)
* A layered, risk-triggered anti-automation posture: a pluggable challenge port, lockout-DoS mitigation, and per-flow DoS ceilings

## Decision Outcome

Chosen option: "A layered, risk-triggered anti-automation posture", because the baseline controls miss distributed abuse and can be weaponized, while an always-on challenge punishes every legitimate user. The posture has three parts.

* **A. Pluggable, risk-triggered challenge layer.** A cloud-agnostic `IChallengeProvider` port (an adapter for CAPTCHA / Turnstile / proof-of-work, selected by configuration, following the same port discipline as the other cloud seams and **disabled in the Development profile**) is applied on the login, password-reset, device-verification, and signup surfaces. Challenges are **risk-triggered** (by failure count, velocity, or a new-device signal), not always-on, so a normal user is not shown a challenge.
* **B. Account-lockout DoS mitigation.** Failures are scoped **per source IP alongside** per-account lockout, so fail-spam from one IP does not fill a victim's lockout counter. A distinct "many lockouts on one account" alert is raised, separate from the brute-force-spike alert, so the two attack shapes are told apart. The **break-glass account (ADR-0015) is exempt from lockout** so an attacker cannot lock the rescue path; it is instead protected by challenge, alerting, and audit, and admin-unlock for ordinary accounts already exists in the admin surface.
* **C. Per-flow DoS ceilings for the advanced flows.** For the device flow, emit `interval=5`, enforce `slow_down` server-side using a Redis last-poll store with an accept-window that widens by 5s per RFC 8628 (in a handler ordered before OpenIddict consumes the device code), and cap the token endpoint with a hard 429 ceiling via ASP.NET Core rate limiting (the real DoS control against clients that ignore `slow_down`). For PAR, rate-limit `/par` per client (and per IP for public clients), bound the maximum outstanding `request_uri` per client with a 429 on breach, and shorten the `request_uri` lifetime from the 1h default to 5-600s, to prevent `request_uri` store exhaustion.

### Consequences

* Good, because distributed credential stuffing that defeats per-IP limits is met by a risk-triggered challenge, while ordinary users almost never see one.
* Good, because lockout can no longer be weaponized: a victim keeps access under IP-spam, and the rescue account cannot be locked out at all.
* Good, because the device and PAR flows gain real ceilings (429) rather than only protocol hints, closing polling-flood and `request_uri`-exhaustion vectors.
* Bad, because it adds a new cloud-agnostic port and provider adapters to build and maintain, and the risk-signal thresholds need tuning and monitoring.
* Bad, because the device-flow `slow_down` enforcement leans on handler ordering (running before device-code consumption), an undocumented seam that must be pinned and re-verified per bump (ADR-0021).
* Neutral, because the interactive challenge depends on a third-party provider; it stays behind the port, is selected by configuration, and is off in development so local and CI runs are unaffected.

### Confirmation

* Acceptance test 9.F6: a scripted credential-stuffing run triggers the challenge.
* Acceptance test 9.F7: an attacker cannot lock a victim's account by IP fail-spam, and the victim can still log in; the break-glass account cannot be locked.
* A device-flow contract test (ADR-0021, per bump) asserts that `interval` is emitted and that the backoff handler is ordered before device-code consumption.
* A PAR anti-flood test asserts the per-client `request_uri` ceiling returns 429 and that the shortened `request_uri` lifetime is applied.

## Pros and Cons of the Options

### Rely on per-IP rate limiting and account lockout alone

* Good, because it is already in place and needs no new components.
* Bad, because per-IP limits do not stop many-IP botnets, and per-account lockout is a ready-made DoS weapon against victims and the rescue account.

### Always-on interactive challenge

* Good, because it maximally suppresses automation.
* Bad, because it degrades every legitimate login, hurts accessibility and conversion, and is unnecessary for the overwhelming majority of low-risk requests.

### Layered, risk-triggered posture (chosen)

* Good, because it resists distributed abuse, neutralizes lockout-DoS, and caps the advanced flows, while keeping challenges rare for real users.
* Bad, because it is more to build and tune, and part of it depends on a version-sensitive handler-ordering seam.

## More Information

* This posture was added in the 2026-07-13 pre-implementation review (doc 08 §5, L2-1/L2-2), with the device-flow and PAR DoS controls from doc 07 (tasks 7.3a-d and 7.7a, the latter from the 2026-07-05 review). Password-breach checking and the credential-hardening baseline are a separate, adjacent decision (part of user management) and are not covered here.
* Related decisions: ADR-0006/ADR-0009 (the cloud-agnostic port discipline the `IChallengeProvider` port follows and the secret resolution its adapters use), ADR-0013 (MFA and step-up, the assurance layer this complements), ADR-0015 (the break-glass account that is exempt from lockout), ADR-0021 (the seam catalogue and contract-regression that pin the device-flow handler ordering), ADR-0040 (the rate-limiting and load-shedding posture whose 429 ceilings this reuses for the flows), and ADR-0041 (the abuse-alert rules that surface these attacks and the lockout-DoS alert).
* Authored in this repository in 2026-07 to record the settled abuse/bot-defense design as an ADR; neutral technologies and standards (CAPTCHA, Turnstile, proof-of-work, ASP.NET Core rate limiting, Redis, RFC 8628, RFC 9126) are named factually for identification only, and no commercial competitor is named.
