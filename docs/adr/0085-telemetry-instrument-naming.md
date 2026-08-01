---
status: "accepted"
date: 2026-08-01
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: OpenTelemetry general naming guidance (fetched and quoted at source 2026-08-01); ADR-0077 (the cardinality cap that binds by instrument name, and the allow-listed tag set); ADR-0044 section G (telemetry names as part of the versioned public surface); ADR-0022 (the emission stack); ADR-0083 (the one instrument added since the catalogue was last complete)
informed: implementers of the custom meter and activity source, consumers of the OSS package who bind dashboards and alert rules to these names
---

# Namespace every custom instrument `nami.identity.` and freeze the catalogue as public API

## Context and Problem Statement

The protocol engine emits no OpenTelemetry, so every metric this product publishes
comes from our own meter inside our own handlers. That makes the instrument names
ours to choose, and ours to get wrong.

Two spellings are circulating in this repository right now. The catalogue in the
observability design uses the `nami.identity.` prefix throughout. Elsewhere, the bare
leaf name is used: counted rather than estimated, there are **16 unprefixed occurrences
across 11 lines in 3 files** (`keys_loaded` 7, `signing_key_days_to_expiry` 6,
`key_rotations` 3), spanning the key-management and observability designs and the
architecture's observability chapter. All three of those instruments are absent from the
catalogue entirely.

The consequence is not cosmetic, because **the cardinality cap attaches by name**:

```csharp
.AddView("nami.identity.tokens_issued", new MetricStreamConfiguration { CardinalityLimit = 50 })
```

A selector that matches no instrument **is not an error**. Nothing is raised, the build
passes, the dashboard stays green, and the cap that ADR-0077's mandatory
high-cardinality rule depends on is a silent no-op. The observability design already
says exactly this, in its own words, and the repository violates its own statement in
sixteen places. That combination is worth naming: a rule stated in one document and
broken in three others is not an implementer's slip, it is an absent decision.

Deciding now is nearly free because no code exists. It is expensive later: these names
are the join key for every consumer dashboard, alert rule, and recording rule shipped
with the product.

## Decision Drivers

* A guard that silently does nothing is worse than an absent guard, because it stops
  anyone from looking. The cap is the mandatory defence for a multi-tenant deployment.
* Our instruments run **inside the consumer's process**, next to their own meters. A
  generic name is a collision hazard we impose on somebody else.
* Instrument names are public API for a distributed package. Renaming after release
  breaks dashboards we do not control and cannot migrate.
* Whatever is chosen has to be enforceable by something executable. The prefix rule
  was already written in the observability design and drifted anyway.

## Considered Options

* **A. Namespace every custom instrument with the application name**, `nami.identity.`.
* **B. Namespace with a reverse domain name.**
* **C. Keep bare names** and rely on the meter name for disambiguation.

## Decision Outcome

Chosen: **Option A.**

**The evidence, quoted so it is not researched again** (OpenTelemetry general naming
guidance, fetched and quoted 2026-08-01):

* *"Use namespacing. Delimit the namespaces using a dot character."*
* For a name outside the semantic conventions the guidance offers exactly two forms:
  prefix by *"your company's reverse domain name, e.g. `com.acme.shopname`"*, or
  prefix by *"your application name, provided that the application name is reasonably
  unique within your organization (e.g. `myuniquemapapp.longitude`)"*.
* *"It is not recommended to use existing OpenTelemetry semantic convention namespace
  as a prefix for a new company- or application-specific attribute name. Doing so may
  result in a name clash in the future."*

**Read precisely:** those sentences are written about **attribute** names, on a page of
general naming guidance. Applying them to instrument names is an extension rather than
a quotation, and it is made deliberately because the reason given is identical: our
names run in someone else's process and must not clash. Saying so here is cheaper than
having a later reader discover that the quoted rule was about attributes.

`nami.identity` is the application name, is already this repository's canonical
lowercase product identifier (the same form as the assurance URN), and is the meter
name lowercased, so one spelling runs from package to URN to metric. Option B would be
equally conformant and buys nothing, since the application name is already unique.

### The canonical catalogue, public API from v1.0

| Instrument | Kind | Bounded tags |
|---|---|---|
| `nami.identity.tokens_issued` | Counter | `grant_type`, `token_type`, `result` |
| `nami.identity.token_issue.duration` | Histogram | `grant_type`, `token_type`, `result` |
| `nami.identity.validation_latency` | Histogram | `result`, `error.type` |
| `nami.identity.revocations` | Counter | `reason` |
| `nami.identity.login_outcomes` | Counter | `outcome`, `factor` |
| `nami.identity.consent` | Counter | `granted` or `denied` |
| `nami.identity.client_secret_validation` | Counter | `result` |
| `nami.identity.user_logout` | Counter | `scheme` |
| `nami.identity.key_rotations` | Counter | none |
| `nami.identity.keys_loaded` | Gauge | none |
| `nami.identity.signing_key_days_to_expiry` | ObservableGauge | `kid`, bounded by the key count |
| `nami.identity.abuse_detections` | Counter | `rule`, `severity` |

The last one is the ADR-0083 bridge: the bounded output of an unbounded input, which
is how a per-principal abuse finding reaches the metric lane without a forbidden tag.
Every tag here is drawn from the ADR-0077 allow-list, which remains the authority on
tags; this ADR is the authority on names.

### The rules that bind

1. **Every custom instrument is namespaced `nami.identity.`.** No exceptions,
   **including inside alert tables and runbook trigger columns**, where an unprefixed
   name is an alert rule that never fires. Four of the sixteen existing drifts sit in
   exactly those two places, and it is the place the rule is easiest to forget because the text is prose
   rather than code.
2. **Built-in instruments are never prefixed.** `http.server.request.duration`,
   `aspnetcore.rate_limiting.*`, `aspnetcore.authentication.*`, and the Kestrel meters
   are semantic-convention names. Prefixing them is exactly the error the third quote
   above warns against.
3. **A view selector is the FULL namespaced name**, never a bare leaf. This is the
   rule the cap depends on, and the only one whose violation is silent.
4. **Wildcards are legal and deliberately unused.** A wildcard selector is valid for
   configuration-only views, but each instrument needs its own budget rather than a
   shared one, so each gets its own view.
5. **Names are public API from v1.0.** Adding an instrument is a minor change;
   renaming or removing one is breaking, because it breaks dashboards we do not own.
   ADR-0044 section G already places telemetry names inside the versioned surface, and
   this ADR supplies the list that rule governs, which it previously did not have.

### Consequences

* Good, because the cap can no longer be silently inert through a name mismatch, which
  is the failure this repository was already exposed to in sixteen places.
* Good, because the catalogue now exists as a single list, so "is this instrument
  named correctly" is answerable by lookup rather than by grep.
* Good, because it closes the loop between ADR-0065, ADR-0044 section G and ADR-0022,
  which between them assert that telemetry names are contract and `nami.`-rooted while
  none of them says what the names are.
* Bad, because it is a rename across the design layer, and a reader of an older draft
  will see the bare form. Cheap now and impossible later, since after release the same
  change breaks consumers.
* Neutral, because the choice between the application-name and reverse-domain forms is
  a coin flip on conformance grounds; internal consistency decided it.

### Confirmation

* All three naming quotes were **fetched and read at source** on 2026-08-01, which is
  also what produced the precision note above: the prefix guidance is written for
  attribute names and is being extended to instrument names deliberately rather than
  cited as if it already said so.
* **The drift was counted, not estimated:** 16 unprefixed occurrences across 11 lines in
  3 files, against a catalogue that uses the prefix throughout and does not list any of
  the three instruments involved. All are fixed in the same change as this ADR.
* **This repository had already found the drift and deferred it, correctly attributed.**
  The observability design records that the key-health gauge names "do not carry the
  `nami.`-rooted prefix that the telemetry-naming rule (ADR-0065) requires", and chose to
  raise it rather than rename across a committed document. That attribution checks out:
  ADR-0065 does state that meter and metric names are contract "under a `nami.`-rooted
  naming scheme", pointing at ADR-0022 and ADR-0044. The reason the deferral never closed
  is the interesting part: **three ADRs asserted the rule and none held the list.**
  ADR-0065 points onward, ADR-0044 section G says names are part of the contract without
  saying which names, and ADR-0022 fixes the emission stack and explicitly not this. A
  rule with no enumeration has nothing to check against, so it cannot be enforced and the
  deferral had nowhere to land. This ADR supplies the enumeration.
* The observability design already states that a selector matching no instrument is
  "silently inert" and falls back to the SDK default, with source evidence read at an
  OpenTelemetry release tag. That statement is what makes rule 3 load-bearing rather
  than stylistic, and it was already true while the repository was breaking it.
* Tests: a test asserts each shipped view is attached to a real instrument, which is
  the executable form of rule 3; and the emitted tag set is asserted against the
  ADR-0077 allow-list so a forbidden tag cannot appear later.

## Pros and Cons of the Options

### A. Application-name namespace, `nami.identity.` (chosen)

* Good, because it is one of the two forms the guidance endorses, and it reuses an
  identifier that already runs from the package name to the assurance URN.
* Good, because it is what the catalogue already used, so the change is to the
  outliers rather than to the design of record.
* Bad, because it is longer to type in a view selector, which is exactly where the
  temptation to shorten it produced the current drift.

### B. Reverse-domain namespace

* Good, because it is the other endorsed form and is maximally collision-proof.
* Bad, because it introduces a second product identifier alongside the one already
  used everywhere else, and it buys nothing when the application name is already
  unique.

### C. Bare names with the meter for disambiguation

* Good, because it is shortest and the meter name does disambiguate at the SDK level.
* Bad, because a backend that flattens instruments by name loses the meter, so a bare
  `revocations` collides with anything else in the consumer's process. It is also the
  status quo that produced sixteen inconsistent sites and a cap that would not have
  attached.

## More Information

* **OpenTelemetry general naming guidance**
  (`https://opentelemetry.io/docs/specs/semconv/general/naming/`), fetched and quoted
  2026-08-01.
* Mechanism and the emitted catalogue: design
  [19](../design/19-observability-capacity-slo.md), which is the implementer source
  for tags, views, and caps. This ADR owns the names.
* Related decisions: ADR-0077 (the cardinality cap that binds by name, and the tag
  allow-list), ADR-0044 (telemetry names inside the versioned public surface),
  ADR-0022 (the emission stack), ADR-0083 (the `nami.identity.abuse_detections` bridge), ADR-0021
  (the decommission marker if the engine ever ships native telemetry, at which point
  these names are what a mapping layer has to preserve).
* Imported from the design corpus's instrument-naming decision on 2026-08-01. The tag
  columns were taken from **this** repository's existing catalogue rather than the
  corpus's, because the two differ on two instruments and this repository's tags are
  the ones its allow-list was written against.
