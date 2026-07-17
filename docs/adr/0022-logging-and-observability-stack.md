---
status: "accepted"
date: 2026-07-04
decision-makers: Nam Phuong Tran (@namphuongtran), acting as solution architect
consulted: Ops; Microsoft observability-with-OTLP guidance and the OpenTelemetry .NET logs documentation
informed: all contributors, via this repository
---

# Use native ILogger plus OpenTelemetry (OTLP) for logging and observability, and drop Serilog

## Context and Problem Statement

Early design documents listed Serilog as the structured-logging library. The question was whether Serilog is warranted or whether native `ILogger` plus OpenTelemetry is enough — whether Serilog adds bulk for little real benefit here. Two existing facts about the project shift the balance away from the general-purpose advice found online:

* Audit is already first-class and **separate** from logging (ADR-0008: `ISecurityEventSink` plus a hash-chain plus a delivery guarantee, not `ILogger`), so Serilog, if used, would serve only diagnostic logs and nothing security-critical.
* Observability is already OpenTelemetry-centric (native .NET 10 meters plus custom `Meter`/`ActivitySource` at the handler seam, exported over OTLP), so metrics and traces are already OpenTelemetry.

Given those, should Nami add Serilog or standardize on native `ILogger` plus OpenTelemetry?

## Decision Drivers

* Avoid bulk: do not run two parallel log pipelines or add dependencies without real benefit.
* Export must be vendor-neutral and cloud-agnostic, matching ADR-0006/0009.
* Audit must stay strictly separate from diagnostic logging (ADR-0008).
* Decide by this project's context, not the general-purpose default.

## Considered Options

* Serilog plus OpenTelemetry (a hybrid)
* Native `ILogger` plus OpenTelemetry (OTLP), dropping Serilog

## Decision Outcome

Chosen option: "Native `ILogger` plus OpenTelemetry (OTLP), dropping Serilog", because audit is already separate and observability is already OpenTelemetry-centric, so a second log pipeline would add bulk without a matching benefit.

Fixed parameters of the decision:

* **Diagnostic logging** is `Microsoft.Extensions.Logging` (`ILogger`) with source-generated `LoggerMessage` (structured and low-allocation).
* **Unified export through OpenTelemetry**: `builder.Logging.AddOpenTelemetry()` plus the OTLP logs exporter, so logs, metrics, and traces travel through one OTLP pipeline (vendor-neutral and cloud-agnostic, matching ADR-0006/0009). Trace-to-log correlation is native, because OpenTelemetry attaches the trace/span context to each log record.
* **PII/secret redaction** uses `Microsoft.Extensions.Telemetry` (redaction and enrichment), so no Serilog enricher is needed.
* **Audit is unchanged**: it remains the first-class `ISecurityEventSink` (ADR-0008), strictly separate from diagnostic logging and never routed through the OpenTelemetry/`ILogger` pipeline, which lacks tamper-evidence and a delivery guarantee; the two lanes join only by a correlation/trace id.
* **Dev and on-premises without a collector**: the native console logger, or OTLP to a local collector, or native file logging if needed.

### Consequences

* Good, because there is one telemetry pipeline (logs, metrics, and traces) over OTLP, which is cloud-agnostic and has fewer moving parts, with no parallel log pipeline.
* Good, because it drops dependencies (Serilog and its sinks) and stays consistent with the OpenTelemetry-centric observability and the `ISecurityEventSink` audit (ADR-0008).
* Bad, because it forgoes Serilog's mature sink ecosystem, especially rolling-file output; this is mitigated by routing OTLP to a collector (which can forward anywhere) and by using the native console/file providers on-premises without a collector. This is where Serilog is still ahead, but it is not decisive once the design is OTLP-first.
* This diverges from the common online "use both" default; it is a decision by project context (audit already separated, observability already OpenTelemetry), not by general trend.

### Confirmation

* Microsoft observability-with-OTLP guidance (.NET) confirms that `Microsoft.Extensions.Logging` plus the OTLP logs exporter is the native path and needs no Serilog; the OpenTelemetry .NET logs documentation confirms first-class, trace-correlated logs; and `Microsoft.Extensions.Telemetry` provides native PII redaction. The general-purpose advice that "Serilog and OpenTelemetry are complementary" was weighed against this project's context.
* Verify-before-build: confirm the exact API names (`Microsoft.Extensions.Telemetry` and `Microsoft.Extensions.Compliance.Redaction`) on .NET 10, the file/local-logging story for on-premises without a collector, and the OTLP collector topology (agent versus gateway) for production.

## Pros and Cons of the Options

### Serilog plus OpenTelemetry (a hybrid)

The common online default: Serilog structured logs bridged to OTLP via a Serilog OTLP sink, keeping the rich enricher and sink ecosystem.

* Good, because it keeps Serilog's mature sinks (notably rolling-file) and familiar enrichers.
* Bad, because it runs two parallel log pipelines and adds dependencies and configuration — exactly the bulk this decision set out to avoid — for a benefit that audit-separation and OTel-first observability have already made marginal.

### Native `ILogger` plus OpenTelemetry (OTLP), dropping Serilog (chosen)

`Microsoft.Extensions.Logging` with source-generated messages, exporting logs, metrics, and traces over one OTLP pipeline, with redaction from the Microsoft telemetry packages.

* Good, because it is one vendor-neutral pipeline with fewer dependencies and native trace-log correlation and redaction.
* Bad, because it gives up Serilog's sink ecosystem, mitigated by the collector and native providers.

## More Information

* Original decision: 2026-07-04.
* This is the diagnostic-logging lane. The audit lane stays on `ISecurityEventSink` (ADR-0008) and is never routed through this pipeline; the two lanes are joined only by a correlation/trace id.
* **Scope boundary:** this ADR fixes the observability *stack* (how signals are emitted and exported). The quantitative NFR targets, the SLO-as-release-gate, the error-budget freeze policy, and the burn-rate alerting built on the metrics this pipeline emits are a separate decision, recorded in ADR-0041. (An earlier corpus note that NFR/SLO should stay a document banner rather than an ADR is superseded by ADR-0041.)
* Related decisions: ADR-0006/0009 (cloud-agnostic ports; OTLP is vendor-neutral), ADR-0008 (the separate audit lane), and ADR-0041 (the NFR targets and SLO release gate built on this pipeline's signals).
* Imported into this repository and translated in 2026-07; content preserved, internal references generalized. Library and product citations (Serilog, OpenTelemetry, and the Microsoft telemetry packages) are retained as neutral technical references.
