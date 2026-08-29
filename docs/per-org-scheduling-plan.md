# Per-org scheduling plan

Give every organization its own cadence for fetch and sync jobs. Replace the global recurring jobs with per-org recurring jobs. The org-scoped read surface returns, filtered to the caller's organization.

## Problem

Cadences are global today. Bootstrap resolves all crons from the first configured organization. A config PATCH from any org reschedules the global recurring job for every org. One org setting a 1-minute interval applies to everyone. Per-org interval values in `organization_config` drive nothing except cache TTLs.

## Model

One recurring job per organization per cadence type, with a namespaced id:

- `dispatch-order-fetch-{orgId}` on `OrderFetchIntervalMinutes`.
- `dispatch-monitoring-uptime-{orgId}` on `UptimeFetchIntervalMinutes`.
- `dispatch-monitoring-latency-{orgId}` on `LatencyFetchIntervalMinutes`.
- `sync-monitoring-account-stats-{orgId}` on `UserStatsFetchIntervalMinutes`.
- `sync-monitoring-fleet-{orgId}` on the minimum monitor interval within that org.
- `aggregate-intranet-feeds-{orgId}` on `FeedFetchIntervalHours`.

Each per-org recurring job is an org-scoped dispatcher: it loads only that org's tenants or monitors and enqueues per-entity jobs with the org id first. The shared dispatch logic already exists in `JobTriggerService`; extract it into per-org dispatcher classes that both the recurring jobs and the manual triggers call.

The global recurring jobs disappear. Their registrations are removed from storage at bootstrap (`RemoveIfExists` on the old ids), mirroring the retired-job cleanup pattern. Platform-only work keeps its jobs: the two view-refresh daily jobs and `refresh-stale-materialized-views` stay global.

## Registration lifecycle

- Bootstrap: for each organization config, register the org's jobs with the org's own values. Orgs without a config row get the defaults.
- Config PATCH: `UpdateFetchIntervalsAsync` and `UpdateFeedIntervalAsync` reschedule only the caller's org jobs. No cross-org reach.
- Org creation: the platform CRUD path registers the new org's jobs.
- Org deletion: `RemoveIfExists` for all `-{orgId}` job ids. Deletion does not exist yet; the hook lands with the platform CRUD work.

## Fleet self-tuning

The fleet job currently reschedules a global cron from the minimum interval across all orgs. Per-org model: `sync-monitoring-fleet-{orgId}` reschedules only its own org's job from the minimum across that org's monitors. The global minimum computation dies.

## Read surface

`GET /api/job/recurring` becomes org-scoped:

- Org-scoped callers: `AdminOnly`, rows filtered by the `-{orgId}` id prefix. They see their org's cadences, next runs, and states only.
- Platform scope: no filter, all orgs.
- Kiosk stays out.

The Scheduled Jobs panel on the jobs page returns for org admins, showing their org's rows. The `PlatformAdminOnly` gate on this surface was correct while the table was a global mirror; per-org ids give it org meaning, so the gate relaxes.

Unchanged: the Hangfire dashboard stays platform-only. Recent executions stay org-filtered. The view-refresh triggers stay platform-gated.

## Web

- `useRecurringJobsQuery` gains the org segment in its query key.
- The Scheduled Jobs panel shows for all admins, not just platform view.
- No other page changes.

## Tests

- Bootstrap registers per-org jobs with each org's own values and removes the retired global ids.
- Config PATCH reschedules only the caller's org jobs; other orgs' crons are untouched.
- Per-org dispatchers enqueue only their org's entities with org id first.
- Fleet self-tuning reschedules only the org's own job.
- Recurring read endpoint: org admins see only their prefix rows, platform sees all, kiosk gets 403.
- Web: recurring query key carries the org segment; panel visible to admins.

## Sequence and effort

| Step | Contents | Estimate |
|---|---|---|
| 1 | Extract per-org dispatcher classes shared with triggers | half day |
| 2 | Per-org recurring registration at bootstrap, retire global ids | half day |
| 3 | Config PATCH reschedules only caller org jobs | half day |
| 4 | Fleet and feed per-org self-tuning | half day |
| 5 | Recurring read surface org filter and policy | half day |
| 6 | Web query key and panel visibility | two hours |
| 7 | Suites | half day |

No schema changes. No migration. Hangfire storage holds the recurring jobs.