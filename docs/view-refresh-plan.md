# Materialized view refresh plan

Coalesced, platform-owned refresh for the eight materialized views. Org-scoped writes mark the views dirty. One job rebuilds everything at most once per cadence, and only when something is dirty. The cadence is platform configuration.

## Problem

The views are rebuilt `CONCURRENTLY` by two daily jobs. Any org admin can force an immediate platform-wide rebuild through the manual trigger endpoints. A backfill leaves the views stale until the next daily run or a manual trigger. The rebuild cost is deployment-wide, imposed by a single org's action.

A rebuild of a materialized view always rebuilds the whole view. There is no partial refresh in Postgres. This plan therefore bounds the frequency and coalesces bursts. It does not reduce the per-rebuild cost.

## Dirty tracking

New table:

```sql
CREATE TABLE materialized_view_dirty (
    organization_id uuid PRIMARY KEY REFERENCES organizations(id) ON DELETE CASCADE,
    marked_at timestamptz NOT NULL
);
```

A row means "this organization changed data that the views must incorporate." Multiple orgs dirty within a cycle still collapse into one rebuild, because the job only asks whether any row exists.

Write paths that mark dirty:

- `OrderIngestionService.ExecuteIngestionAsync` on successful completion, for the tenant's organization. Covers scheduled fetch and manual backfill.
- `OrganizationConfigService.UpdateConfigAsync` when the reporting timezone changes. Replaces the current immediate `IReportingRollupRefresher` call.

Reads never mark. The flag is per org so per-org refresh remains possible later, but the job ignores which orgs are dirty.

## Refresh job

One job replaces both daily jobs:

- `RefreshMaterializedViewsJob` refreshes all eight views in dependency order when `materialized_view_dirty` has any row, then clears the table.
- Skips everything when no org is dirty.
- Cadence comes from `GlobalConfig.MatViewRefreshIntervalMinutes`, default 60.
- The job reschedules itself after each run, mirroring `MonitorSynchronizationJob`.
- `IReportingRollupRefresher` is removed; nothing refreshes views synchronously anymore.

Tradeoff: a timezone change is visible in the rollups only after the next cycle, up to the cadence. Accepted.

## Configuration

`GlobalConfig` gains `MatViewRefreshIntervalMinutes` with default 60. `GlobalConfigResponseDto`, `UpdateGlobalConfigRequestDto`, and `GlobalConfigService.UpdateConfigAsync` carry it. Updating the interval reschedules the job, mirroring the interval handling in `UpdateFetchIntervalsAsync`.

## Authorization

The manual trigger endpoints that invoke the view jobs move from `AdminOnly` to `PlatformAdminOnly`. Which endpoints those are: the ones wiring `RefreshFinancialMaterializedViewJob` or `RefreshMonitoringMaterializedViewJob` in `BackgroundJobController`. Org-scoped sync triggers (order, uptime, latency, monitor) stay `AdminOnly`.

## Frontend

`PlatformConfigurationView` becomes a single full-width column. The `GlobalConfigurationForm` keeps no column split and no categorization: event retention and the materialized view refresh interval sit in the same panel, one after the other.

## Migration

- `AddMaterializedViewDirty` table.
- `AddMatViewRefreshInterval` column on `global_config` with default 60.
- The eight existing views and their daily recurring jobs are untouched by schema changes.

## Sequence and effort

| Step | Contents | Estimate |
|---|---|---|
| 1 | Dirty table, marker writes, migration | half day |
| 2 | Coalesced refresh job, remove daily jobs and refresher | half day |
| 3 | Config field, DTOs, reschedule on update | half day |
| 4 | Trigger gating to platform admin | two hours |
| 5 | Platform page full width, new field, codegen | two hours |
| 6 | Backend and web suites, smoke run | half day |

## Tests

- Ingestion completion marks the tenant's org dirty; timezone change marks dirty; no write path refreshes synchronously.
- Job skips when clean, refreshes once and clears when dirty, respects the cadence.
- Interval update persists and reschedules.
- Trigger endpoints reject org admins with 403 and allow platform admins.
- Platform page renders the retention and interval fields full width.