# Observability: organization diagnostics and platform operations

Status: proposed design, not an approved implementation specification.

## Purpose and decisions

An organization monitors financial data and integrations for many tenants. A tenant is a monitored business entity, not implicitly a user or an organization. The platform can host one or many organizations. In a self-hosted installation, an organization user and the software operator can both hold platform administration rights.

| Audience | Questions |
|---|---|
| Organization | Is our data current? Which tenant or integration failed? Is it retrying? What can we fix? What reference should we give support? |
| Platform operator | Is the service available? Are workers running? Is a shared dependency failing? Which organizations are affected? What caused the failure? |

The proposed direction is:

- Give organizations an application-owned diagnostics API with safe events and pipeline history scoped to their data.
- Give platform administrators a separate operations API and access to technical telemetry.
- Use native health checks for host probes. They do not replace either diagnostics API.
- Keep queryable operational events in the application database. They are not a complete technical log or a compliance audit trail.
- Use structured logs as the baseline technical signal. OpenTelemetry export is an optional deployment capability, not a dependency of organization diagnostics.
- Decide whether to record a failure at the operation boundary. A failed FluentResults result is not automatically an event.

This is greenfield work: existing pages, navigation, and API shapes are not compatibility constraints. Revise or replace the UI around organization diagnostics and platform operations. Frontend implementation has not been inspected for this plan; that limits implementation detail, not design scope.

The organization experience should start with data freshness and actionable pipeline issues, then let users inspect a resource's runs and safe events. Platform operations should start with shared infrastructure and affected organizations, with an explicit transition into an organization's diagnostics. Users with both roles must always see which scope they are viewing. Page count and layout should follow these tasks rather than mirror controllers or preserve the current two pages.

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

Proposed routes, subject to repository naming conventions:

| Route | Contract |
|---|---|
| GET /api/organizations/{organizationId}/diagnostics/pipelines | Authorized resource status and scoped summary |
| GET /api/organizations/{organizationId}/diagnostics/runs | Scoped cursor-paginated run history |
| GET /api/organizations/{organizationId}/diagnostics/events | Scoped safe event history |
| GET /api/platform/diagnostics/health | Infrastructure/worker status and affected-org summary |
| GET /api/platform/diagnostics/runs | Cross-org support view of application runs |
| GET /api/platform/diagnostics/events | Platform event view with scope/audience filters |

Apply identical authorization to future single-record endpoints. Follow established denied-scope/missing-resource ProblemDetails contracts without unscoped existence lookups. Use FluentValidation and typed ValidationError for invalid queries. Require take in 1..100, validate enum/date filters and cursors, and order by timestamp plus ID. Index organization/time/ID, with tenant indexes as needed. Totals use the authorized query, not the returned page.

Remove ClearErrors as a product action. Clearing failure fields is not recovery and can conceal faults. Successful work resolves current failure state; retry/configuration changes use existing authorized business endpoints. No replacement acknowledgment subsystem is needed now. If added later, acknowledgment belongs to an issue/actor and never changes pipeline health.

Remove public DELETE .../clear history deletion. Scheduled retention handles normal cleanup; this design adds no org-admin deletion API. Disable legacy global clear routes during migration. If a temporary platform-only cleanup route must remain, reject olderThanDays outside an explicit positive range before mutation and record the actor. Negative-value behavior is not preserved.

## 7. Telemetry and hosting

Every installation gets application diagnostics and structured ILogger output. Configure JSON console logs with service/version/environment and operation correlation. Self-hosting requires no collector or hosted account; telemetry is never automatically sent to the software author. Application platform roles do not themselves grant access to an external log backend.

Enable OpenTelemetry only with explicit export configuration and an OTLP destination. Start with request, outbound HTTP, database tracing, and supported job-boundary activities. Verify compatibility with installed .NET/Npgsql/Hangfire versions; avoid duplicate database spans from overlapping EF/Npgsql instrumentation. The baseline works without export. An exporter does not supply a query backend; see [OpenTelemetry exporters](https://opentelemetry.io/docs/languages/dotnet/exporters/).

Initial metrics answer operator questions: request errors/latency, provider errors/latency, pipeline attempts/final outcomes/duration, and reliable worker/queue state. Use bounded labels such as pipeline kind, provider kind, and outcome code. Keep organization, tenant, run, actor, URL, and exception text out of metric labels. Per-org breakdowns use the scoped API and restricted logs/traces. See [OpenTelemetry .NET metric guidance](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices/).

Choose sampling, backend access, retention, and a useful dashboard/alert with the deployment. Keep final application failures independently of trace sampling. Export failure must not block requests/jobs. Organization pages do not query a telemetry backend or parse Prometheus output.

## 8. Retention and migration

Retention is a platform deployment policy. Events, completed runs, and external logs/traces have separate settings. Existing SystemEventRetentionDays governs rows, not exported logs. Set durations for the support window/volume before rollout; do not silently change retention in a schema migration. Exclude active/unresolved runs from cleanup until reconciled, and preserve current status/last success.

Use bounded cleanup batches and report cleanup failure. Keep occurrence-time ownership. Resource deletion must explicitly handle retention/anonymization, not accidentally cascade-delete history. Selected admin events remain operational records, not guaranteed tamper-proof auditing.

Migrate additively:

1. Add event ownership/audience/code/correlation and safe DTOs. New org events require validated ownership. Legacy rows start platform-only.
2. Backfill ownership only from reliable evidence. A current tenant relationship is insufficient if historical reassignment cannot be ruled out. Never assign unknown rows to a default org. Old messages/details remain platform-only unless deliberately converted to safe templates.
3. Add run tracking/explicit success semantics per pipeline. Mark incomplete historical coverage; never fabricate past successes or trust parsed legacy arguments as ownership.
4. Build or revise the organization and platform UI around these workflows, and update generated OpenAPI/client contracts. Replace old routes and consumers together where practical; legacy adapters are not required for compatibility. Any temporary adapters must enforce the new scope/redaction rules.
5. Remove obsolete global aggregations, entity-returning endpoints, raw job DTOs, and clear operations. Retire redundant fields only after all writers/readers migrate.

## 9. Delivery and verification

Use independently reviewable slices. Replace the old fixed 2.5-day estimate: this work includes authorization, schema changes, data semantics, and job lifecycle behavior.

| Slice | Deliverable | Required evidence |
|---|---|---|
| 1. Close exposure | Scoped safe event/job reads, query validation, disabled global clears, safe 500 detail | Org A cannot read/change B; negative input cannot mutate/query unsafely; no stack/payload leaks |
| 2. Event contract | Scope/audience/correlation, safe summaries, explicit failure policy | Validation creates no event; scheduled configuration failure does; original error survives persistence failure |
| 3. One complete pipeline | Order ingestion state/runs and org routes | Zero-order success, configuration failure, retry/recovery, exhausted retry, cancellation |
| 4. Other pipelines | Feed/monitor mappings to the same public meanings | Disabled/manual schedules, never-run resources, staleness, suppressed exceptions, correct aggregates |
| 5. Platform operations | Component reports and configured probes | DB outage fails readiness only; absent workers visible; org failure does not make API unready |
| 6. UI and cleanup | Organization/platform workflows, explicit scope navigation, removed legacy paths, retention enabled | Users can find stale data, understand failures, and inspect runs; degraded/unknown states render; scope changes cannot reuse broader cached data; cleanup preserves current status |
| Independent, optional telemetry | Follow a request into background execution in a configured deployment | Correlation across retries, safe signals, unavailable exporter has no business effect |

Cross-cutting tests cover two hosted organizations, self-hosting with multiple platform admins, a dual-role person, tenant restrictions, and a kiosk. Exercise lists/counts, unauthorized filter IDs, null scope, guessed record IDs, conflicting selection headers, and legacy rows.

Test duplicate callbacks, retry scheduling, process interruption, and enqueue/DB partial failure. Failed Results and swallowed exceptions must not appear successful. One resource's recent success cannot hide another's staleness; retention cannot erase last-success knowledge.

Test safe projections with sample credentials/private URLs in exceptions/provider responses, including the 500 response itself. Safe fields come from templates, not only string scrubbing. Verify no double event from service/global handler, and no unexpected incident for normal request cancellation.

## Proposed product defaults and boundaries

This proposal gives human Employee/Viewer users safe diagnostic reads within existing resource grants, excludes kiosks, removes manual error clearing, and removes org-level history deletion. These are defaults to review, not claims about current behavior. Retention durations and a managed-host telemetry backend remain deployment choices. Tenant-user boundaries are supported without building a tenant-user feature now.

UI redesign is in scope; detailed layouts remain implementation work. Out of scope: generic workflow engines, a browser metrics service, custom incident management, guaranteed audit delivery, and per-organization alert subscriptions. Add those for a concrete requirement.
