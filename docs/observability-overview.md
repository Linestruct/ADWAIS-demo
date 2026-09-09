# ADWAIS observability

Observability has two jobs:

1. Let an organization find the tenant and pipeline run that failed, understand when it failed and the safe reason, and know what to do next.
2. Let a platform administrator see service health and investigate shared failures across every organization.

The first job is the ADWAIS product. The second is platform operations. They use the same durable run and event records, but different authorization and presentation. A platform administrator can view all organizations in the platform view; selecting an organization switches to that organization's view.

## Product surface

Diagnostics lives under Settings.

The page contains:

- A compact health summary. Organization users see their pipeline counts. Platform administrators see database/worker health and organizations with failed or active work.
- A filterable pipeline-run table. Each row identifies the organization when the platform view is active, then tenant, pipeline, pipeline type, run state, requested/completed times, and a safe result summary.
- An expandable run detail. It shows attempts, outcome, work count, request/trace references, and related safe events.
- A small diagnostic-event stream for failures, warnings, and recovery information.

The run table is the useful distinction from the scheduled-jobs page. Scheduled jobs answer what the scheduler is configured to dispatch. Diagnostics answers which business operation actually ran and whether it succeeded for a particular tenant or organization.

Organization users receive only rows and events they are allowed to see. The API does the organization/tenant filtering before pagination and projection. Platform users receive a cross-organization view with organization names on every row. Neither view exposes Hangfire arguments, provider responses, credentials, paths, or stack traces.

## Durable records

PipelineRun is one logical invocation of an application pipeline. Hangfire remains the scheduler and retry engine. The record stores:

- organization and optional tenant ownership;
- pipeline kind and resource identity;
- trigger, state, request/trace references, and timing;
- attempt count, retry time, safe outcome code/summary, and optional committed-work count.

SystemEvent is bounded application history for meaningful failures, recoveries, and selected incidents. FluentResults failures remain normal API outcomes and are not automatically written as events. Unexpected exceptions and explicit background-job outcomes can create events at the operation boundary.

Both records are safe projections. Technical exception details stay in structured logs and live traces. Event reads use separate organization/platform contracts and never return the entity's raw details.

## Signals

| Signal | What it is for | Where it is visible |
| --- | --- | --- |
| Pipeline runs and events | Product-facing history for a tenant/org operation | Scoped ADWAIS diagnostics |
| Logs | Technical context and exception details | Platform/operator logging |
| Traces | Follow an HTTP request or background attempt through application, database, provider, and Hangfire work | Aspire Dashboard when OTLP is enabled |
| Metrics | Counts, durations, provider outcomes, and worker/request health | Aspire Dashboard when OTLP is enabled |

OpenTelemetry instrumentation is part of the application boundary. ASP.NET Core, HttpClient, EF Core, and the Hangfire enqueue/execute boundary use W3C context. The application emits a small bounded set of pipeline/provider metrics and correlated structured logs.

OTLP is optional per installation. With OpenTelemetry:OtlpEndpoint or OTEL_EXPORTER_OTLP_ENDPOINT set, telemetry is exported to the Aspire Dashboard (or another OTLP receiver) for live technical investigation. With neither set, no telemetry backend is required and ADWAIS diagnostics still work from the application database. Aspire Dashboard is intentionally not the long-term history store.

## Health and safety

- /health/live reports that the process responds.
- /health/ready checks essential dependencies, initially the application database.
- A failed tenant/provider operation does not make the whole API unready.
- Unhandled requests return a generic 500 with a request reference; the technical exception is logged and may be recorded as a platform event.
- Global event deletion is disabled. Retention cleanup is a platform policy.
- Kiosks and tenant-restricted principals do not gain broader diagnostics access through query parameters or cached data.

## Delivery status

Implemented in the current slice:

- Multi-organization scoped diagnostics APIs and safe DTO projections.
- Durable pipeline-run tracking for ingestion, feed, monitor, and account-stat pipelines.
- Run detail endpoints with related safe events.
- Filterable ADWAIS run/failure explorer for organization and platform scopes.
- Scoped event reads, safe 500 handling, input validation, and disabled global clear.
- OpenTelemetry composition-root setup, EF Core instrumentation, W3C Hangfire propagation, bounded metrics, and optional OTLP export.
- Liveness/readiness probes.

Still to verify or extend:

- Exercise retry, cancellation, process interruption, and ambiguous enqueue cases against the real job host.
- Add integration coverage for two organizations, platform/self-hosted roles, tenant restrictions, and kiosk access.
- Validate Aspire Dashboard end to end when an OTLP endpoint is configured.
- Add deeper history pagination or recovery actions only if the run explorer proves insufficient.

This delivery does not build a second scheduler, a browser metrics service, a compliance-grade audit ledger, alert subscriptions, Grafana Cloud integration, or new tenant-user functionality.
