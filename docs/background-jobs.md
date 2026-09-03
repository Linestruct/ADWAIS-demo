# Background jobs

Hangfire runs the job queue, the scheduler, and the worker pool. Jobs are either organization-scoped or platform-wide. The distinction decides payloads, triggers, cadences, and visibility.

## Job classes

Organization jobs carry `organizationId` as their first argument and resolve everything else from it:

- `OrderIngestionService.ExecuteIngestionAsync(orgId, tenantId, start, end)`
- `OrderIngestionService.IngestSingleOrderAsync(orgId, tenantId, provider, order)`
- `UpdateMonitorUptimeJob.ExecuteAsync(orgId, monitorId, start, end)`
- `UpdateMonitorLatencyJob.ExecuteAsync(orgId, monitorId, start, end)`
- Per-org dispatch, fleet, account-stats, and feed jobs take `orgId` first.

Each job validates that the entity belongs to the claimed organization. An `IOrgScopedJob` marker plus a Hangfire `IElectStateFilter` rejects any org job enqueued without a non-empty org id, so invalid payloads never enter storage.

Platform jobs take no organization: the two daily view refreshes, the stale refresh, fleet and account-stats schedulers (thin fan-outs), event cleanup, calendar sync, the dev seeder.

Workers run as a trusted system process with no caller context. Scoping applies at enqueue time, not execution time.

## Typed ids

`RecurringJobKind` plus `RecurringJobId` own every recurring job id. Organization kinds always carry a `-{organizationId}` suffix. Platform kinds never do.

- `RecurringJobId.For(kind, orgId)` builds canonical org ids. It refuses platform kinds.
- `RecurringJobId.Platform(kind)` builds platform ids. It refuses org kinds.
- `TryParse` returns kind plus optional org. `DisplayName` returns curated labels with the organization name for org jobs.
- `RecurringJobVisibility` classifies ids and decides org-scope read access.

Build ids only through this type. Parse ids only through it.

## Per-org cadences

One recurring job per organization per type:

- `dispatch-order-fetch-{orgId}`, `dispatch-monitoring-uptime-{orgId}`, `dispatch-monitoring-latency-{orgId}`, `sync-monitoring-account-stats-{orgId}`, `sync-monitoring-fleet-{orgId}`, `aggregate-intranet-feeds-{orgId}`.

Each fires an org-scoped dispatcher on that org's own `organization_config` intervals. The fleet job self-tunes its own org's cron from that org's monitor minimum.

Lifecycle:

- Bootstrap registers per-org jobs for every configured organization, with defaults for missing values. It also removes retired global ids.
- A config PATCH reschedules only the caller's org jobs. One org's one-minute interval never reaches another org.
- Org creation and deletion hook job registration and removal. Deletion does not exist yet; the hook lands with the platform CRUD work.

## Materialized view refresh

The eight views (two financial, three latency, three availability) carry `organization_id`. Writes mark their organization dirty in `materialized_view_dirty`:

- Order ingestion completion and single-order ingestion.
- Reporting timezone change.
- Uptime and latency writes (latency only when a response time was saved).

`RefreshStaleMaterializedViewsJob` (`refresh-stale-materialized-views`) runs on `GlobalConfig.MatViewRefreshIntervalMinutes` (default 60, minimum 5). It exits when nothing is dirty. Otherwise it rebuilds all views with `CONCURRENTLY` in dependency order and clears the table. A failed refresh leaves the rows, so the next run retries.

The two daily jobs rebuild unconditionally and clear the table after a successful rebuild. Dirty marks therefore shorten the stale window to one cadence without replacing the daily safety net.

PATCH on the global config reschedules only the stale job.

## Manual triggers

Five org-scoped triggers enqueue the caller's org units through `IJobTriggerService`: order sync, uptime sync, latency sync, fleet sync, account stats sync, feed sync. Platform scope fans out to every org. No trigger fires a platform loop directly. Firing goes through the recurring manager, so the scheduled-jobs table records last execution and status; next execution stays untouched.

Two view-refresh triggers (`refresh-historic-order-data`, `refresh-monitoring-data`) stay `PlatformAdminOnly` and fire the daily jobs unconditionally. Backfill validates that the tenant belongs to the caller's org.

## Recurring read surface

`GET /api/job/recurring` returns name, kind, platform flag, cron, and execution state per job. Platform scope returns everything. Organization scope returns the org's own jobs plus the managed platform set.

The managed set lives on `GlobalConfig.VisibleRecurringJobsCsv`, stored as kind names, edited from the platform settings page. Unknown entries are ignored on load. Removing a kind hides its job from orgs instantly. Adding one exposes it.

## Recent executions and health

`GET /api/system/health/jobs` filters rows by the payload org id. Platform scope keeps everything including platform jobs.

`GET /api/system/health` and `GET /api/job/recurring` mirror Hangfire internals and stay readable under the staff policies in force.

## Dashboard

`/hangfire` requires authentication plus the `is_platform_admin` claim. Normal browser navigation carries no org header, so the claim exists for platform members. The button in settings POSTs `/api/dashboard-session` (platform-gated, org-header-aware), which mints a five-minute cookie for the server-rendered UI.

In Development, requests without an `Authorization` header authenticate as the DevMock principal. With `DEV_MOCK_ORG_ID` unset it is the platform admin. With it set, it is that org's admin. Use `docs/smoke-testing.md` for the dev workflow.