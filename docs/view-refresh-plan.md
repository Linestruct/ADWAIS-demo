# Materialized view refresh plan

Additive. The existing refresh jobs and manual triggers keep working exactly as they do today. A new dirty-gated job rebuilds the views only when organizations have pending changes.

## Scope

Nothing existing is removed or changed:

- `RefreshFinancialMaterializedViewJob` (daily) stays.
- `RefreshMonitoringMaterializedViewJob` (daily) stays, and clears the dirty table after a successful rebuild.
- The manual trigger endpoints stay and keep triggering rebuilds unconditionally; their authorization moves to `PlatformAdminOnly` (see Authorization).
- Backfills and ingestion behave as today.

## Problem the new job solves

Today the views are rebuilt daily and on manual triggers. Between rebuilds, views can be stale for up to 24 hours after a backfill or timezone change. The new job shortens that to the configured cadence without making the daily safety net conditional.

## Dirty tracking

New table:

```sql
CREATE TABLE materialized_view_dirty (
    organization_id uuid PRIMARY KEY REFERENCES organizations(id) ON DELETE CASCADE,
    marked_at timestamptz NOT NULL
);
```

A row means "this organization changed data the views have not incorporated yet." Marking is idempotent per organization. The job only asks whether any row exists, never which orgs.

Write paths that mark dirty:

- `OrderIngestionService` on successful completion and on single-order ingestion, for the tenant's organization.
- `OrganizationConfigService` when the reporting timezone changes.
- `UpdateMonitorUptimeJob` after saving availability data.
- `UpdateMonitorLatencyJob` after saving response time data.

The monitoring write paths matter because the latency and availability views are built from tables these jobs write continuously. Without them, a monitoring-only organization would never become dirty.

## New job

`RefreshMaterializedViewsJob`, additive:

- Runs on the configured cadence (default 60 minutes).
- Checks whether any org is dirty. If not, exits without touching the views.
- If dirty, refreshes all eight views with `CONCURRENTLY` in dependency order, then clears the table.
- The dirty table doubles as the retry ledger: a failed refresh leaves the rows, so the next run retries.

## Interplay with the daily jobs

The daily jobs keep refreshing unconditionally on their schedule. After a successful rebuild, each daily job clears the dirty table. Consequences:

- Nothing dirty when the daily job runs: it refreshes as today and clears an empty table. Unchanged behavior.
- Orgs dirty when the daily job runs: the daily rebuild incorporates the changes and clears the table, so the next run of the new job finds nothing to do. No redundant rebuild.

A failed daily rebuild leaves the dirty rows, so the new job retries on the next cycle.

The only modification to existing code in this plan is the clear call in the two daily jobs, plus their dependency on the tracker.

## Configuration

`GlobalConfig.MatViewRefreshIntervalMinutes`, default 60, minimum 5. PATCH on the global config persists it and reschedules only the new job. The /platform page shows the field next to event retention, full width, no column split.

## Authorization

The manual trigger endpoints for the materialized view refresh jobs move to `PlatformAdminOnly`:

- `POST /api/job/trigger/refresh-historic-order-data`
- `POST /api/job/trigger/refresh-monitoring-data`

The gating is auth only. The endpoints keep their current behavior: they trigger a rebuild unconditionally. No other endpoint changes its policy.

## Migration

One migration: create `materialized_view_dirty`, add `mat_view_refresh_interval_minutes` to `global_config` with default 60.

## Sequence and effort

| Step | Contents | Estimate |
|---|---|---|
| 1 | Dirty table, tracker service, marker writes, migration | half day |
| 2 | New coalesced job, registered alongside the daily jobs | half day |
| 3 | Config field, DTOs, reschedule of the new job only | half day |
| 4 | /platform interval field, codegen | two hours |
| 5 | Backend and web suites | half day |

## Tests

- Tracker: idempotent marks, dirty state, clear.
- Ingestion and timezone change mark dirty; no synchronous refresh anywhere.
- Uptime and latency jobs mark dirty on successful writes, not on empty payloads.
- New job: skips when clean, refreshes once and clears when dirty, coalesces multiple orgs.
- Daily jobs: refresh unconditionally and clear the dirty table; a failed daily rebuild leaves the rows.
- Interval update persists and reschedules only the new job.
- Manual triggers stay unconditional; org admins get 403 on the trigger endpoints, platform admins pass.
- Existing daily jobs and trigger tests updated and green.