---
status: "accepted"
date: 2026-07-26
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OpenTelemetry .NET metrics documentation on cardinality limits, views, and overflow behaviour, and the SDK source at release tag core-1.17.0 for the non-matching-view behaviour the documentation omits (both verified 2026-07-26); ADR-0022 (the emission stack, whose stated scope boundary excludes this), ADR-0032 (the opt-in phone-home telemetry, a different signal with its own no-PII rule), ADR-0040 (the export path's fail-open classification), ADR-0044 and ADR-0065 (telemetry names as a stable contract), ADR-0016 and ADR-0053 (the erasure and data-subject obligations a metric tag would silently escape)
informed: all contributors, via this repository
---

# Bound metric cardinality with an allow-listed tag set, and keep personal data out of the diagnostics lane

## Context and Problem Statement

ADR-0022 chose the observability stack and drew its own boundary explicitly: it "fixes the observability *stack* (how signals are emitted and exported)", and what is built on that pipeline is someone else's decision. ADR-0044 and ADR-0065 then fixed what the instruments are **called** and that the names are a stable public contract. Between those two, nothing decides what a metric is allowed to be **tagged with**.

That gap is not a tidiness problem. A metric tag is a dimension, and the set of distinct values it takes becomes a time series each. Tagging a metric with `tenant_id`, `sub`, `session.id`, `jti`, or a client address produces a series per tenant, per user, per session, per token, or per address. This fails in two separate ways at once, and the two are usually discussed apart:

* **Operationally**, it is a cardinality explosion that can degrade or take down the metrics backend, which is the thing you were relying on to see the incident.
* **As data protection**, it writes personal data into a store that sits **outside every mechanism this project built for personal data**. Metrics are not the audit lane, so they are not covered by ADR-0008's retention. They are not the operational store, so crypto-shred (ADR-0016) does not reach them. A data-subject erasure request (ADR-0053) has no way to find a `sub` that ended up as a metric dimension in a third-party backend. The identifier survives every control by having left through a door nobody was watching.

The second failure is the more serious and the less obvious, and it is why this ADR is not filed as a performance concern.

Two decisions look like they might already cover this and do not. **ADR-0032** forbids PII in a telemetry payload, but that is the **opt-in phone-home** signal sent to a Nami-operated endpoint carrying a product version, a runtime, feature flags, and a bucketed tenant count. It is a different signal, a different destination, and a different consent model from the OTLP diagnostics lane, and its rule does not reach a metric dimension. **ADR-0022** carries framework-level redaction, which acts on log and trace attributes rather than on the dimensions of a metric stream.

The SDK provides a backstop but not a policy. OpenTelemetry .NET applies a default cardinality limit of 2000 measurement points per metric, and since version 1.10.0 it folds measurements beyond that into a single synthetic point tagged `otel.metric.overflow=true` rather than discarding them silently. That prevents unbounded growth. It does not prevent 2000 tenant identifiers being written into the metrics backend first.

## Decision Drivers

* A personal identifier must not be able to leave through a path where none of this project's data-protection mechanisms can reach it.
* The metrics backend must stay usable during the incident it exists to show.
* Per-tenant and per-user investigation is a real operational need and cannot simply be forbidden without an alternative.
* A limit that is silently not in effect is worse than no limit, because it reads as protection.
* The rule has to be reviewable by someone who is not an observability specialist.

## Considered Options

* No rule beyond the SDK default.
* An allow-listed tag set, per-metric caps, and exemplars as the sanctioned drill-down path.
* Redact or drop high-cardinality dimensions at the collector rather than at the source.
* Forbid per-tenant investigation entirely.

## Decision Outcome

Chosen: "an allow-listed tag set, per-metric caps, and exemplars". Relying on the SDK default accepts 2000 identifiers in the backend before anything happens. Redacting at the collector puts the control outside the process, on infrastructure an adopter supplies and Nami cannot assume, after the data has already left. Forbidding investigation is not an option, it is a refusal to solve the problem.

### A. Metric dimensions are allow-listed, not deny-listed (binding)

A metric tag may carry a value only from a **bounded** domain, meaning one whose distinct values are a small set fixed by the protocol or by configuration rather than growing with users, tenants, sessions, or requests. The permitted dimensions are `grant_type`, `token_type`, `scheme`, `result` or `outcome`, `error.type`, and `policy`.

Stating this as an allow-list rather than a list of forbidden fields is deliberate. A deny-list is only ever as good as the imagination of whoever last edited it, and the failure mode here is a field nobody thought of. Adding a dimension is a change to this list, which makes it a reviewable act.

The following are named explicitly as **never** permitted as a metric dimension, not because a deny-list is the mechanism but because each has been proposed at some point and the reason for each refusal is worth recording: `tenant_id`, `sub`, an unbounded `client_id`, `session.id`, `jti`, any raw token or token fragment, and any IP address. The first five are unbounded **and** identifying; a raw token is a credential; an address is both personal data and unbounded.

### B. Exemplars are the sanctioned route to a specific tenant or user (binding)

The operational need behind every request for a high-cardinality tag is "show me this one case". That is answered by **exemplars**: a trace-based exemplar filter attaches a trace identifier to a metric bucket, and the trace and its logs carry the detail, under the redaction and retention that lane already has (ADR-0022).

This is the part that makes rule A sustainable rather than merely restrictive. A prohibition with no alternative gets worked around; the correct answer here is genuinely better than the tag, because it lands the identifier in a lane that has a retention policy and a redaction pass instead of one that has neither.

### C. Per-metric caps are set below the SDK default where the shape is known (binding)

Where a metric's legitimate dimension space is known and small, a `View` sets a cardinality limit for that instrument well below the SDK's 2000, so a regression shows up as an overflow signal immediately rather than after two thousand series exist. The reference case is the token-issuance counter, whose permitted dimensions multiply out to a few dozen combinations at most.

The SDK's 2000-point default and its `otel.metric.overflow` marker remain the backstop for everything not individually capped. The relationship is deliberate: the default catches what this ADR failed to anticipate, and the per-metric cap catches what it did anticipate, faster.

### D. A test asserts the cap is actually attached (binding)

A `View` selects the instrument it configures by name. **A view whose selector matches no instrument is silently inert, and the instrument is then aggregated with the SDK defaults, including the default 2000-point limit.** The failure is therefore not a missing metric, which would be noticed, but a live metric carrying a cap nobody set.

This was verified in the SDK source rather than in the documentation, which is silent on the case. At release `core-1.17.0` the name overload of `AddView` compiles the selector into a delegate that returns `null` when the name does not match, a `null` result is skipped in the publish loop with no diagnostic call on that branch, and when nothing matched the SDK adds a null configuration under the comment "No views matched. Add null which will apply defaults". Construction time does not catch it either: `AddView` rejects a null or whitespace name, and a wildcard combined with a rename, but a well-formed name that matches nothing is a valid argument. The one message the SDK emits for that instrument states that measurements "will be processed and aggregated by the SDK", which is the same message it emits when the cap is live.

So the mechanism is not trusted on configuration alone. A test asserts that the configured views are attached to the instruments they name and that the resulting limit is in force. This is the whole reason rule D exists as a separate binding parameter rather than an implementation note: **an unattached cap is indistinguishable from an attached one by reading the configuration**, and the SDK's only signal on that path reads as reassurance. The same reasoning is why ADR-0043 asserts its invariants at startup rather than documenting them.

### E. This is a data-protection rule, and is recorded as one (binding)

The rule in A is carried in the data-protection posture, not only in the observability view, because its failure mode is a personal-data leak into a store outside the reach of erasure. It is distinct from ADR-0032's phone-home telemetry rule, which governs a different signal to a different destination, and the two must not be read as one control covering both.

### F. What is not decided here

The **inventory** of instruments, meaning which metrics exist at all, is a detailed-design artifact that moves with the code, not a decision. Their **names** are already a stable public contract under ADR-0044 section G and ADR-0065. This ADR governs only their dimensions.

### Consequences

* Good, because the one path by which a personal identifier could reach a store that erasure, retention, and the audit chain all miss is closed by construction rather than by review.
* Good, because the metrics backend cannot be taken down by a dimension added in good faith.
* Good, because the operational need is answered rather than refused, and answered by a route with better data-protection properties than the tag it replaces.
* Good, because an allow-list makes adding a dimension a visible act, and the attachment test makes a dead cap fail loudly.
* Bad, because per-tenant drill-down is one hop slower than a tag would be, through an exemplar into a trace; accepted, and the hop is where the retention and redaction live.
* Bad, because the allow-list will need extending as instruments are added, and an over-tight list invites someone to work around it; mitigated by the list being an ADR amendment, which is the visibility this trades for.
* Bad, because rule D compensates for behaviour that is confirmed in the SDK source but unspecified in its documentation, so it could change without a breaking-change note and the test would then be asserting a moving target; accepted, since a test that becomes redundant is a better failure than a cap that was never live.

## Pros and Cons of the Options

### No rule beyond the SDK default

* Good, because it needs no work and does bound unbounded growth.
* Bad, because it permits 2000 distinct identifiers per metric to reach the backend before the limit engages, which answers the operational failure and not the data-protection one.

### Allow-list plus per-metric caps plus exemplars (chosen)

* Good, because it addresses both failure modes at the source, keeps investigation possible, and is reviewable without observability expertise.
* Bad, because it is a list to maintain; accepted, since the maintenance is the review.

### Redact at the collector

* Good, because it is centralized and catches everything regardless of what the application emits.
* Bad, because the collector is infrastructure an adopter supplies, so Nami cannot assume it, cannot test it, and cannot ship it; and the data has already left the process by then. A control the product cannot guarantee is not a control the product can claim.

### Forbid per-tenant investigation

* Good, because nothing to leak.
* Bad, because the need is real and would be met by whatever workaround the operator invents, which is worse than a sanctioned path.

## More Information

* **Why this ADR exists.** The rule was found stated in the observability detailed design and carried into the architecture layer, with no ADR containing it, as the third of eight load-bearing ownerless claims. Investigating the cluster it belonged to dissolved two of the three: the meter inventory turned out to be owned as a naming and contract question by ADR-0065 and ADR-0044 section G with only a catalogue left over, and the lossy-not-blocking export invariant was a classification gap in ADR-0040, which now covers it as parameter E rather than needing an ADR. Only the tag rule was a decision, and it was larger than its label: filed as a cardinality rule, it is at least as much a data-protection rule.
* **External verification, 2026-07-26, OpenTelemetry .NET.** Quoted from the project's own metrics guidance: "OpenTelemetry has a default cardinality limit of `2000` per metric", and from SDK version 1.10.0 "any new measurement that could not be independently aggregated will be automatically aggregated using the overflow attribute", which was experimental and environment-variable-gated in 1.6.0 through 1.9.0. A per-instrument limit is set through `MetricStreamConfiguration.CardinalityLimit` on the view API.
* **Verified in the source, not in the documentation, 2026-07-26.** What happens to a view whose instrument-name selector matches nothing is the premise of rule D, and the documentation does not cover it, so the SDK source was read at release tag `core-1.17.0` (published 2026-07-16). In `src/OpenTelemetry/Metrics/Builder/MeterProviderBuilderExtensions.cs` the name overload of `AddView` compiles the selector into `string.Equals(instrument.Name, instrumentName, StringComparison.OrdinalIgnoreCase) ? metricStreamConfiguration : null`, so the match is case-insensitive and a non-match yields `null`; the same method throws at construction only for a null or whitespace name and for a wildcard combined with a rename, never for a name that matches nothing. In `src/OpenTelemetry/Metrics/MeterProviderSdk.cs` the publish loop adds a configuration only when it is non-null and emits no event on the null branch, then falls through to `metricStreamConfigs.Add(null)` under the comment "No views matched. Add null which will apply defaults", and the message logged for that instrument is that measurements "will be processed and aggregated by the SDK". The code in that path is identical on `main` and at the tag. **The residual, recorded rather than hidden:** this is source behaviour, not a documented contract, so it can change without a breaking-change note. An earlier revision of this ADR reported the question as unverified on the strength of the documentation alone; documentation silence is not an exhausted search when the dependency is permissive open source.
* Related decisions: ADR-0022 (the emission stack, and the stated scope boundary that leaves this open), ADR-0040 (the export path's fail-open classification, added as parameter E in the same change as this ADR), ADR-0008 (the audit lane, which is where an identifier is supposed to be recorded and which has the retention this lane lacks), ADR-0016 and ADR-0053 (the erasure and data-subject-rights mechanisms a metric dimension escapes), ADR-0032 (the separate opt-in phone-home telemetry and its own no-PII rule, deliberately not merged with this one), ADR-0044 and ADR-0065 (instrument names as a stable contract, which this ADR does not touch), ADR-0043 (the assert-rather-than-document precedent behind rule D), ADR-0021 (the decommission marker on the custom meter these rules apply to).
* Authored fresh for this repository. The design corpus states the bounded-tag set, the exemplar route, the per-metric view cap, and the attachment test in its capacity and observability document; the framing of the tag rule as a data-protection control that escapes erasure, and the allow-list-over-deny-list reasoning, are this repository's.
