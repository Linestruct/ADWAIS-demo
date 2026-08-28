# Hangfire org scoping plan

Attribution and trigger scoping for Hangfire jobs. Per-entity work becomes org-parameterized and org-triggerable. Platform-wide aggregation stays platform-gated. Reads follow the same boundary.

## Model

Hangfire jobs run as a trusted system process with no caller context. Scoping therefore means:

- Attribution: org-touching jobs carry `organizationId` as their first argument.
- Enforcement: the system refuses org-touching jobs enqueued without an org id.
- Triggers: org admins enqueue their org's work, never a platform job.
- Boundary: per-entity work is org-scopable. Aggregation jobs (view refresh, fleet fan-out, account stats fan-out) are not.

## 1. orgId-first payloads

Job signatures gain `Guid organizationId` as the first parameter:

- `OrderIngestionService.ExecuteIngestionAsync(organizationId, tenantId, start, end, ct)`
- `OrderIngestionService.IngestSingleOrderAsync(organizationId, tenantId, provider, order, ct)`
- `UpdateMonitorUptimeJob.ExecuteAsync(organizationId, monitorId, start, end)`
- `UpdateMonitorLatencyJob.ExecuteAsync(organizationId, monitorId, start, end)`
- New per-org jobs created in step 2 take `organizationId` first.

Call sites updated: `OrderFetchDispatcherJob`, `UptimeDispatcherJob`, `LatencyDispatcherJob`, `IngestionController`, kiosk order webhooks, tests. The jobs validate the entity belongs to the claimed org before touching data.

## 2. Enforcement filter

Marker interface `IOrgScopedJob` implemented by org-touching job classes. A Hangfire `IElectStateFilter` inspects the enqueued job's type and args and throws when an `IOrgScopedJob` payload lacks a non-empty `Guid` first argument. Applies at enqueue time, so no invalid payload can enter storage. Platform jobs are untouched.

## 3. Per-org fan-out of loop jobs

`MonitorSynchronizationJob` and `UpdateGlobalMonitoringStatsJob` split into:

- A slim platform scheduler job on the existing cadence that fans out per-org jobs.
- New `SyncOrganizationFleetJob(organizationId)` and `SyncOrganizationAccountStatsJob(organizationId)`, each with the org's provider calls and independent failure and retry.

Note: `MonitorSynchronizationJob` also computes the lowest upstream interval used to reschedule the dispatchers. That computation stays in the scheduler job. The per-org sync jobs hold only the per-org monitor sync and status writes.

The dispatchers already fan out per tenant and per monitor; they only gain the org argument.

## 4. Org-scoped triggers

- `POST /api/ingestion/backfill`: reject when the tenant belongs to another org (scope check).
- `POST /api/job/trigger/order-sync`, `uptime-sync`, `latency-sync`, `user-stats-sync`, `monitor-sync`: org admins trigger by enqueueing the org-scoped units for their org (the org's tenant order fetches, the org's monitor jobs, the org's fleet and account-stats jobs). No trigger fires a platform loop job.
- `POST /api/job/trigger/refresh-historic-order-data` and `refresh-monitoring-data`: stay `PlatformAdminOnly` (platform aggregation).
- `feed-fetch`: stays as-is (org-scoped feeds already).

## 5. Read surfaces

- `GET /api/job/recurring`: `PlatformAdminOnly`. Raw Hangfire internals (ids, cron, queues) have no org meaning.
- `GET /api/system/health`: `PlatformAdminOnly`. Platform-wide metrics (Hangfire stats, DB status, aggregate counts).
- `GET /api/system/health/jobs` (recent executions): `AdminOnly`, org-filtered. After step 1, the org id is the first argument, so filtering is a direct arg read; rows without an org arg (platform jobs) are dropped for org admins and kept for platform admins. Kiosk loses access to all three.

## 6. Web

- Recent executions query key gains the org segment (`useOrgScopedKeys`), so switching orgs refetches filtered rows.
- Org trigger buttons stay visible to org admins; the two platform refresh rows stay hidden outside platform view (done).
- Pipeline health and scheduled jobs sections move to the platform surface, matching their policies.

## 7. Tests

- Filter: enqueue of an `IOrgScopedJob` without an org id is rejected; with a valid org id passes.
- Jobs: entity belonging to another org is refused.
- Fan-out: scheduler enqueues one job per org; one org's provider failure does not fail the scheduler or other orgs.
- Triggers: org admin cannot backfill another org's tenant; triggers enqueue only their org's units.
- Reads: org admins see only their org's executions; platform admins see everything; kiosk gets 403 on all three.
- Web: query key includes the org segment; recent rows refetch on org switch.

## Sequence and effort

| Step | Contents | Estimate |
|---|---|---|
| 1 | orgId-first payloads and call sites | half day |
| 2 | Marker interface and enforcement filter | two hours |
| 3 | Fleet and account-stats fan-out | half day |
| 4 | Org-scoped triggers and backfill check | half day |
| 5 | Read surface policies and filtering | half day |
| 6 | Web keys and visibility | two hours |
| 7 | Suites | half day |

No schema changes. No migration.