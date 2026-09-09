# Observability: organization diagnostics and platform operations

Status: the scoped run/failure explorer and observability baseline are implemented; integration verification remains tracked below.

## Purpose and decisions

An organization monitors financial data and integrations for many tenants. A tenant is a monitored business entity, not implicitly a user or an organization. The platform can host one or many organizations. In a self-hosted installation, an organization user and the software operator can both hold platform administration rights.

| Audience | Questions |
|---|---|
| Organization | Is our data current? Which tenant or integration failed? Is it retrying? What can we fix? What reference should we give support? |
| Platform operator | Is the service available? Are workers running? Is a shared dependency failing? Which organizations are affected? What caused the failure? |

The proposed direction is:

- Build a new in-product diagnostics experience as the primary deliverable: organization users find and act on their data problems, while platform operators investigate shared technical problems.
- Give that experience application-owned APIs, safe events, and pipeline history scoped to the viewer's data.
- Give platform administrators a separate operations experience and access to technical telemetry.
- Use native health checks for host probes. They do not replace either diagnostics API.
- Keep queryable operational events in the application database. They are not a complete technical log or a compliance audit trail.
- Instrument the application with OpenTelemetry from the first pipeline implementation. When an installation enables OTLP export, Aspire Dashboard is the live technical-diagnostics target. The product diagnostics API remains available whether or not OTLP is enabled.
- Decide whether to record a failure at the operation boundary. A failed FluentResults result is not automatically an event.

This is greenfield work: existing pages, navigation, and API shapes are not compatibility constraints. The UI is the primary product outcome, not a later consumer of backend work. Diagnostics lives under Settings and is built around a filterable run/failure explorer. A row identifies the organization in platform view, tenant, pipeline, type, run state, requested/completed times, and a safe result. Expanding a row shows attempts, outcome, work count, request/trace references, and related safe events.

The organization experience answers which tenant or organization operation failed, when it failed, and what can be fixed. Platform operations answers whether shared services and workers are healthy and which organizations are affected, then lets a platform administrator inspect the same run history across all organizations. These are separate scopes and contracts; the platform view is not a second copy of the organization status table.

## 1. Scope and access

Ownership and visibility are separate. An unexpected exception can belong to organization A while its technical details remain platform-only.

Organization endpoints explicitly name one organization. The server validates that target against authenticated access before querying. A platform administrator can target an organization for support, but receives the same safe organization DTOs. Platform detail requires a platform endpoint. Holding both roles does not silently broaden an organization response.

Proposed initial access rules:

| Caller | Organization diagnostics | Platform operations | Mutations |
|---|---|---|---|
| Organization admin | Own organization and its tenants | None | Existing authorized retry/configuration actions |
| Employee or Viewer | Safe reads within existing organization/tenant access | None | None added here |
| Tenant-restricted principal | Only explicitly tenant-visible records for that tenant | None | Existing grants only |
| Kiosk/device principal | No diagnostic event or run history | None | None |
| Platform admin | Any explicitly targeted organization, using the safe contract | All organizations and platform diagnostics | Explicit platform operations |

Use the existing access model and small endpoint policies, not a new permissions framework. Distinguish device authentication from human roles; `KioskOrStaffAccess` is not an adequate diagnostics boundary. Tenant restrictions must be tested even if tenant-user access is not yet a supported product workflow. Deny access when a grant cannot be established.

Rules for lists, counts, details, lookups, and mutations:

- A missing access scope denies access. A null organization ID alone never proves platform authority.
- Routes, query parameters, and selection headers can narrow an authorized target; they cannot grant access.
- Filter by organization, tenant restriction, and visibility in the database before counting or paginating. Never filter a global sample afterward.
- Validate that referenced tenants, monitors, feeds, and integrations belong to the operation's organization. Optional tenant ID is not the organization boundary.
- Hide organization-wide summaries/events from tenant-restricted callers unless explicitly safe for them. Their summaries aggregate only permitted resources.
- Persist ownership when the operation happens. Resource deletion or reassignment must not reveal history to another organization.

## 2. Organization pipeline diagnostics

Organizations see business operations such as order ingestion, feed refresh, and monitor synchronization. They do not see Hangfire method names, serialized arguments, global queue counts, or raw exception messages.

For each pipeline/resource, return:

- Stable pipeline kind and resource identity with a safe display name.
- Enabled/configured state, schedule when applicable, and next expected run.
- Last attempt, last success, current execution state, and latest outcome.
- Freshness separately: current, overdue, never succeeded, unknown, or not applicable.
- A safe current issue code, explanation, suggested action, and reference/run ID.
- Known work counts with precise meanings, such as orders committed rather than merely fetched.

Execution, freshness, and configuration are separate fields. A running job can still have overdue data. A disabled integration is not failed. Zero orders returned can be successful.

Calculate freshness per resource from its actual schedule and a pipeline-specific grace period. Manual and event-driven pipelines do not inherit a periodic-sync threshold. Only report upstream data freshness if a meaningful watermark exists: a successful request proves synchronization completed, not that provider data is current. Unknown timestamps remain unknown. In particular, ingestion `LastPolled` currently changes after failed attempts and cannot be relabeled as last success.

Organization summaries count affected resources and their issues. The newest success across all resources cannot imply that every tenant is current. Shared outages can appear as safe failures of affected organization operations without exposing other organizations or global exception details.

### Durable run history

Add a small `PipelineRun` record for operations exposed to organizations. Scoped pagination and stable history independent of Hangfire retention/argument layout justify this record. Hangfire remains the scheduler and retry engine; this is not a second scheduler or workflow framework.

One record represents a logical invocation:

- Run ID, required organization ID, optional tenant ID, pipeline kind, resource identity.
- Trigger kind, optional initiating actor ID, request reference, initiating trace ID.
- Requested/queued time, first start time, last state-change time, completion time, attempt count, next retry time when known.
- State: pending dispatch, queued, running, retry scheduled, succeeded, failed, canceled, skipped, or unknown.
- Outcome code, safe summary, optional committed-work count. Internal Hangfire ID is omitted from organization DTOs.

Create/pass run identity through supported job entry points. Ownership comes from validated resource/job context, never a message or guessed argument position. Existing organization-first job arguments can remain an execution convention without becoming the history API.

Automatic retries update the same run; a new manual invocation creates a new run. Attempts have separate trace IDs linked through run ID. Conditional/idempotent updates prevent duplicate callbacks and older attempts overwriting newer outcomes. Persist one final failure or recovery event, not an event at each catch/rethrow layer.

Define the application outcome at each job boundary. A failed Result or caught-and-suppressed exception must not become a successful application run because Hangfire sees a normal return. Preserve business retry/idempotency behavior. Configuration failures generally need user action; provider timeouts may use the existing bounded retry policy. Do not retry every typed error automatically.

Database creation and Hangfire enqueue are not assumed atomic. Start with pending dispatch, carry run ID in trusted scheduler metadata, and attach scheduler identity after enqueue. Reconcile ambiguous dispatch and stale running records against known scheduler state. Do not enqueue duplicates during reconciliation or label uncertain work successful; report unknown/interrupted status when evidence is insufficient. Scheduler completion alone does not prove business success. Telemetry failures must not cause completed ingestion to execute again. Do not add an outbox solely for observability; stronger delivery guarantees belong to a separate business-execution requirement.

Existing resource fields supply configuration/current state during migration. Add explicit last-success state where needed and update it only after successful work. Preserve latest status/last success beyond history retention. Map each pipeline's current fields to the new meanings before switching its reads.

## 3. Events and technical logs

Evolve existing SystemEvent storage rather than returning entities or making separate org/platform copies of every event. Its purpose is bounded operational history: durable failures, recoveries, useful application incidents, and selected administrative actions.

| Field | Meaning |
|---|---|
| ID and occurrence time | Stable reference and UTC ordering |
| Organization ID | Owner; required for org operations, null only for platform/unattributed events |
| Tenant/resource identity | Optional narrower association, validated against owner |
| Kind and code | Stable classification, e.g. pipeline.failed / provider.timeout |
| Severity | Impact, independent of HTTP status and audience |
| Audience | Platform-only, organization staff, or explicitly tenant-visible |
| Safe summary/action | Deliberate user-facing text, never copied from an exception |
| Run, trace, request IDs | Optional links between history, work, telemetry, and support |
| Actor kind/ID | User, device, system, or unknown; ID only when established |

An org-owned platform-only event is valid. Org-visible events require an organization; tenant-visible events also require that tenant. Visibility defaults to platform-only. Actor identity comes from trusted server context and does not establish ownership. Org DTOs omit internal actor identifiers unless needed and authorized.

Use separate organization/platform DTO projections. Neither returns navigation entities or arbitrary metadata. Safe summaries/actions use a small code-based catalog with allowlisted, size-limited parameters. Useful guidance can say "The provider rejected the configured credentials; reconnect this integration" without exposing credentials, provider bodies, private URLs, SQL, paths, or stack traces.

Send exception detail to restricted structured logs, linked by event/run/request IDs. New events need no arbitrary raw Details payload. Legacy details remain platform-only and expire under retention. Exclude secrets/customer payloads even from platform logs. See [OpenTelemetry sensitive-data guidance](https://opentelemetry.io/docs/security/handling-sensitive-data/).

Unexpected request errors return a safe 500 with a request reference. Publish an organization-visible generic incident only when the operation established authorized org ownership; otherwise keep it platform-only. Replace the current exception handler's exception.Message in ProblemDetails.Detail for unhandled 500s. Preserve explicitly safe compatibility 4xx messages where required.

Write technical logs independently of event persistence. A database outage must not suppress the original error or recursively record logging failures. Event persistence is best effort: retain the original technical log and report persistence failure through the logger. Do not promise complete event history during database outages. This table is not an authoritative audit trail.

## 4. FluentResults signal policy

Keep expected failures as typed Results and existing ProblemDetails mappings. Do not add SystemEvent writes to result mapping or new ordinary uses of HttpContractException. The operation boundary decides the signal:

| Outcome | Caller response | Durable history | Technical signal |
|---|---|---|---|
| Validation, not found, ordinary scope denial, conflict | Existing typed ProblemDetails | None by default | Request/status metrics; no automatic error log |
| Interactive configuration failure | Safe correction guidance | No extra event unless it establishes lasting pipeline trouble | Optional diagnostic |
| Scheduled configuration failure | Run failed/blocked with corrective action | Meaningful pipeline failure/state-change event | Contextual warning |
| Interactive provider error/timeout | Safe typed ProblemDetails | Only if persistent pipeline state changes | Provider outcome/duration metrics and warning |
| Background provider error/timeout | Retry/final failure per policy | Retry state on run; final failure and later recovery events | Attempt metrics and correlated logs/traces |
| Unexpected exception | Generic 500 or failed attempt | Safe incident/final pipeline event at owning boundary | Error log with restricted exception detail |
| Compatibility HttpContractException 4xx | Preserve HTTP contract | None by default | Normal 4xx signal; warning only when useful |
| Actual security action/detection | Existing access response | Explicit security/admin event, normally platform-only | Dedicated signal; not an audit of every 403 |
| Normal cancellation, disabled pipeline, no work | Canceled/skipped/success as appropriate | No failure event | Normal outcome signal |

Multiple errors in one Result produce one operation outcome, not an event per error. Run history covers executions; events highlight significant failures and changes. Do not persist every successful poll. Recovery requires verified successful work, not dismissal. Repeated failures with the same issue code on the same resource remain visible in runs but do not need a new issue event each poll; emit an event when the issue changes or recovers.

Request IDs and trace IDs are distinct. Return a request reference in ProblemDetails and record it alongside Activity.TraceId when present. Do not assume HttpContext.TraceIdentifier is an OpenTelemetry trace ID. Background work captures trusted ownership/correlation when scheduled and creates its own execution trace; HTTP request scope is not implicitly available.

## 5. Platform operations and probes

Platform reports cover database availability, worker availability/heartbeat, queue age/depth, scheduler failures, materialized-view backlog where relevant, and affected organizations. Storage connectivity does not prove workers are processing jobs. Show component states and observation times rather than one unexplained traffic-light status.

Global infrastructure and cross-org summaries require platform authorization. Support views can filter application events/runs by organization. Hangfire dashboard access remains platform-only. Application APIs never expose raw serialized job payloads; safe job details can include kind, state, timing, and IDs.

Use ASP.NET Core health checks for minimal host endpoints:

| Endpoint | Meaning | Checks |
|---|---|---|
| /health/live | This process responds | No database, provider, or sync-freshness checks |
| /health/ready | This instance can serve intended requests | Essential dependencies, initially DB connectivity, with bounded execution time |

Probes return status only. Use the hosting network boundary rather than an interactive admin session; document how the host reaches them. No tenant/exception data is returned if publicly reachable. Configure correct host restart/routing behavior and verify startup/bootstrap behavior. Provider failures, stale tenant data, and failed jobs do not by themselves remove the whole web API from service.

Diagnostic APIs return HTTP 200 with degraded/unavailable component states when a valid report can be constructed. If a required query cannot execute, return safe 503 ProblemDetails, not an empty healthy result. Probe 503 behavior remains separate from page data loading. Health-check defaults are plain text and 503 for unhealthy results; see [Microsoft health-check documentation](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0).

## 6. API shape and unsafe mutations

These are backing contracts for the new UI, subject to repository naming conventions:

| Route | Contract |
|---|---|
| GET /api/organizations/{organizationId}/diagnostics/pipelines | Authorized resource status and scoped summary |
| GET /api/organizations/{organizationId}/diagnostics/runs | Scoped bounded latest-page run history; add a cursor when deeper history is required |
| GET /api/organizations/{organizationId}/diagnostics/runs/{runId} | One authorized run with related safe events |
| GET /api/organizations/{organizationId}/diagnostics/events | Scoped safe event history |
| GET /api/platform/diagnostics/health | Infrastructure/worker status and affected-org summary |
| GET /api/platform/diagnostics/pipelines | All organization pipeline status, safe for platform administrators; optional organization filter |
| GET /api/platform/diagnostics/runs | Cross-org support view of application runs |
| GET /api/platform/diagnostics/runs/{runId} | One cross-org run with related safe events |
| GET /api/platform/diagnostics/events | Platform event view with scope/audience filters |

Apply identical authorization to future single-record endpoints. Follow established denied-scope/missing-resource ProblemDetails contracts without unscoped existence lookups. Use FluentValidation and typed ValidationError for invalid queries. Require take in 1..100, validate enum/date filters and cursors, and order by timestamp plus ID. Index organization/time/ID, with tenant indexes as needed. Totals use the authorized query, not the returned page.

Remove ClearErrors as a product action. Clearing failure fields is not recovery and can conceal faults. Successful work resolves current failure state; retry/configuration changes use existing authorized business endpoints. No replacement acknowledgment subsystem is needed now. If added later, acknowledgment belongs to an issue/actor and never changes pipeline health.

Remove public DELETE .../clear history deletion. Scheduled retention handles normal cleanup; this design adds no org-admin deletion API. Disable legacy global clear routes during migration. If a temporary platform-only cleanup route must remain, reject olderThanDays outside an explicit positive range before mutation and record the actor. Negative-value behavior is not preserved.

## 7. OpenTelemetry and the backend

OpenTelemetry is part of the implementation, not a late exporter retrofit. Configure it once in a small `AddObservability` composition-root extension. That extension owns service/version/environment resource attributes, instrumentation, and conditional OTLP export. Domain entities and application DTOs must not depend on OpenTelemetry packages.

Instrument the same operation boundaries that write `PipelineRun` and safe application events. For each pipeline execution, the boundary:

1. Restores or starts an `Activity` from a single application `ActivitySource`.
2. Opens a structured log scope with trusted organization, tenant, pipeline, run, request, and trace correlation where known.
3. Records a small set of `Meter` counters/histograms for attempt, final outcome, duration, and provider activity.
4. Updates the durable run record and writes a safe event when the outcome is significant.

Use automatic instrumentation for inbound ASP.NET Core requests and outbound `HttpClient` calls. Enable one verified database instrumentation path for EF/Npgsql, not overlapping paths that create duplicate spans. Add a small Hangfire client/server filter or equivalent trusted job metadata path to propagate W3C trace context and run ID across enqueue and execution. Background work starts its own execution activity and links it to the initiating request when one exists. It must not rely on HTTP scope being present in a worker.

The baseline in this repository enables ASP.NET Core, `HttpClient`, and EF Core instrumentation. A Hangfire client/server filter carries W3C context into background execution activities. Package compatibility and sensitive-field behavior still need end-to-end verification with the selected Aspire deployment.

Initial metrics answer operator questions: request errors/latency, provider errors/latency, pipeline attempts/final outcomes/duration, and reliable worker/queue state. Use bounded labels such as pipeline kind, provider kind, and outcome code. Keep organization, tenant, run, actor, URL, and exception text out of metric labels. Per-organization investigation uses the scoped application API and restricted logs/traces. See [OpenTelemetry .NET metric guidance](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices/).

Aspire Dashboard is the selected live technical-diagnostics target. Run it standalone where live logs, traces, and metrics are useful. It receives OTLP and normally keeps telemetry in memory, so it is intentionally not a long-term history system. Do not adopt an Aspire AppHost solely for this purpose. [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone) documents its standalone OTLP receiver and in-memory behavior.

OTLP export is optional per installation. `AddObservability` must work with no OTLP endpoint configured: it creates no unavailable-backend failure path, and the API/workers still start and operate normally. In that mode, ADWAIS diagnostics continue to use durable `PipelineRun`, safe event, and current-state data. When an OTLP endpoint is configured, it targets Aspire Dashboard and provides live platform investigation only. Export failure must not block requests/jobs. Product diagnostics never query a telemetry backend or parse Prometheus output, and durable final application failures remain available even if traces are sampled out.

Use `OpenTelemetry:OtlpEndpoint` or the standard `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable. Leave both unset when the deployment does not run an OTLP receiver.

Keep standard OTLP configuration so a later retained backend can be introduced without changing pipeline instrumentation. Such a backend is not part of this delivery. Aspire access is restricted to platform operators; an application platform role does not itself grant access to its host/dashboard.

## 8. Retention and migration

Retention is a platform deployment policy. Events and completed runs retain history in the application database; Aspire Dashboard has no long-term retention requirement. A future retained telemetry backend will have separate log/trace/metric retention. Existing SystemEventRetentionDays governs rows, not exported logs. Set durations for the support window/volume before rollout; do not silently change retention in a schema migration. Exclude active/unresolved runs from cleanup until reconciled, and preserve current status/last success.

Use bounded cleanup batches and report cleanup failure. Keep occurrence-time ownership. Resource deletion must explicitly handle retention/anonymization, not accidentally cascade-delete history. Selected admin events remain operational records, not guaranteed tamper-proof auditing.

The observability schema migration is already applied for this local-development project. No historical event classification or compatibility backfill is part of this delivery. Future changes use ordinary EF migrations and only add fields needed by the current workflows.

1. Keep event ownership/audience/code/correlation and safe DTOs enforced for new writes. Existing unknown rows remain available only under the existing platform-safe rules until retention removes them.
2. Keep run tracking and explicit success semantics at each pipeline boundary. Never fabricate historical successes or infer ownership from parsed legacy arguments.
3. Build organization and platform UI with generated OpenAPI/client contracts. Legacy adapters are not required for this greenfield surface.
4. Remove obsolete global aggregations, entity-returning endpoints, raw job DTOs, and clear operations as their consumers disappear.

## 9. Delivery and verification

Use independently reviewable slices. Replace the old fixed 2.5-day estimate: this work includes authorization, schema changes, data semantics, and job lifecycle behavior.

| Slice | Deliverable | Required evidence |
|---|---|---|
| 1. Product workflow and safety baseline | Organization/platform run explorer, scoped safe event/run reads, query validation, disabled global clears, safe 500 detail | An org user can identify the tenant/run at issue and see only its safe data; Org A cannot read/change B; negative input cannot mutate/query unsafely; no stack/payload leaks |
| 2. Event contract | Scope/audience/correlation, safe summaries, explicit failure policy, UI event presentation | Validation creates no event; scheduled configuration failure does; original error survives persistence failure; safe causes/actions are understandable in the UI |
| 3. Pipeline run coverage | Order, feed, monitor, and account-stat run records, safe outcomes, and boundary instrumentation | A run identifies its organization/tenant, records success/failure, and exposes a safe reason; zero-work success and provider/configuration failure remain distinguishable |
| 4. Platform operations and live telemetry | Component reports, configured probes, EF/Hangfire tracing, and Aspire Dashboard when OTLP is enabled | DB outage fails readiness only; org failure does not make API unready; with OTLP configured, Aspire receives correlated logs/traces/metrics; without it, API/workers still operate normally |
| 5. Integration verification and cleanup | Role/scope tests, retry/interruption checks, retention, and removal of obsolete readers | Users can find a failed tenant/run; scope changes cannot reuse broader cached data; cleanup preserves current status; ambiguous job lifecycle states are visible |

Cross-cutting tests cover two hosted organizations, self-hosting with multiple platform admins, a dual-role person, tenant restrictions, and a kiosk. Exercise lists/counts, unauthorized filter IDs, null scope, guessed record IDs, conflicting selection headers, and legacy rows.

Test duplicate callbacks, retry scheduling, process interruption, and enqueue/DB partial failure. Failed Results and swallowed exceptions must not appear successful. One resource's recent success cannot hide another's staleness; retention cannot erase last-success knowledge.

Test safe projections with sample credentials/private URLs in exceptions/provider responses, including the 500 response itself. Safe fields come from templates, not only string scrubbing. Verify no double event from service/global handler, and no unexpected incident for normal request cancellation.

## Proposed product defaults and boundaries

This proposal gives human Employee/Viewer users safe diagnostic reads within existing resource grants, excludes kiosks, removes manual error clearing, and removes org-level history deletion. These are defaults to review, not claims about current behavior. Application-history retention remains a deployment choice; Aspire has no long-term retention role. Tenant-user boundaries are supported without building a tenant-user feature now.

UI redesign is in scope and the first run explorer is implemented. Out of scope: generic workflow engines, a browser metrics service, custom incident management, guaranteed audit delivery, Grafana Cloud integration, and per-organization alert subscriptions. Add those for a concrete requirement.
