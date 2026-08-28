# Observability plan

Replace the custom health and event services with native ASP.NET Core health checks and OpenTelemetry. The web surfaces stay; their data sources change.

## Current state

- `SystemHealthService` computes DB status, sync status, Hangfire stats, and last-sync timestamps by hand. Served by `GET /api/system/health` and `GET /api/system/health/jobs`.
- `SystemEventService` persists `system_event` rows and mirrors them to `ILogger`. Consumed by the events console, retention cleanup (`SystemEventCleanupJob`, `SystemEventRetentionDays`).
- No `AddHealthChecks` or OpenTelemetry package is referenced.

## Decision: keep the audit table

The events console reads from the database. There is no log store or observability backend in the deployment to query instead. The `system_event` table stays as the queryable audit trail, written by `SystemEventService`. OpenTelemetry adds the real-time signal: the same events flow out as structured logs and metrics without replacing the store.

## A. Health checks replace `SystemHealthService`

`AddHealthChecks()` with one endpoint, two views:

- `GET /healthz`: liveness. No auth, no internals. Simple pass/fail from the DB ping.
- `GET /health`: readiness, platform-gated (`PlatformAdminOnly`). Rich report consumed by the web pipeline page.

Checks:

- Database: EF Core connectivity check.
- Hangfire: connection ping against `JobStorage.Current`, queue depth as detail.
- Sync staleness: last successful sync timestamps against a threshold, with the per-org error counts as report details.
- Materialized views: dirty table age as detail (informational).

`SystemHealthService.GetHealthAsync` is deleted. The pipeline page reads the health report. `ClearErrorsAsync` moves to a small `SyncErrorService` or stays on the health controller backed by a dedicated service; it is a mutation, not a health concern.

Hangfire job stats (failed, processing, enqueued, scheduled counts) become OpenTelemetry gauges, pushed by a collector or read by the web from a metrics endpoint. Decide in implementation: expose metrics to the web via `MapMetrics` (platform-gated) or move those cards behind the Hangfire dashboard surface.

## B. OpenTelemetry replaces the hand-rolled signals

SDK wired once in `AddInfrastructure` or a new `AddObservability`:

- Traces: ASP.NET Core, EF Core, Npgsql, HttpClient, and the Hangfire job instrumentation package. Console exporter in Development, OTLP exporter via `OTEL_*` configuration in prod.
- Metrics: meters for ingestion (orders ingested), sync failures per org, job outcomes (succeeded, failed, retried) per job name, system event counts per level.
- Logs: OpenTelemetry logging with the same exporters. `SystemEventService` keeps writing rows and additionally emits the event as a structured log entry; the `ILogger` mirror already exists, so this is configuration, not code.

`SystemEventService` itself stays: it is the audit writer. Its logger mirror becomes the OTel log path.

## C. Web

- Pipeline health panel reads `GET /health` (platform-gated). Per-org error counts stay in the report details when the caller has org scope; platform sees everything.
- Events console unchanged (audit table).
- Remove `useSystemEventsViewModel` health branching in favor of the health report shape, or keep the view model and swap its query.

## Open points

- Metrics exposure to the web: dedicated metrics endpoint vs. dashboard surface.
- Whether `GET /api/system/health/jobs` (recent executions) survives as-is; the Hangfire scoping plan already moves it to org-filtered `AdminOnly`.
- Log export format (JSON vs OTLP) and whether `SystemEventRetentionDays` should also bound exported logs.

## Sequence and effort

| Step | Contents | Estimate |
|---|---|---|
| 1 | Health checks, two endpoints, delete `GetHealthAsync` | half day |
| 2 | OTel SDK, exporters, traces | half day |
| 3 | Metrics and meters | half day |
| 4 | Web pipeline panel to health report | half day |
| 5 | Suites and smoke run | half day |

## Tests

- Health checks: DB down reports unhealthy; Hangfire check reflects storage; sync staleness crosses threshold.
- Policies: liveness is anonymous, readiness rejects org admins with 403.
- Event service still persists rows and logs structured entries.
- Web renders the health report shape.