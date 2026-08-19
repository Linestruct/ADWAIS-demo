# Multi-Organization Audit Discovery & Review Handover Findings

This document records the comprehensive audit results for the `feature/multi-organization` branch against `docs/multi-organization.md`, as evaluated on 2026-08-19.

---

## 1. Verdict
**CHANGES REQUIRED**

Applying migration `20260818232018_AddOrganizationConfig` drops `reporting_time_zone_id` from `global_config` which crashes database startup/seeding in `MaterializedViewOrchestrator.SyncViewsAsync`, `CalendarSubscriptionService.TriggerSyncAsync` creates `CalendarEvent` entities with `Guid.Empty` causing foreign key violations, `WithoutAuthorityClaims` fails to scrub short-form `"role"` claims from upstream IdPs allowing authorization bypass for unprovisioned users, multiple services suffer null-dereference crashes on unauthenticated/unscoped requests (`_currentAccess.Scope.OrganizationId`), and `TenantController` is completely unscoped.

---

## 2. Findings Summary

| ID | Severity | Area | Location | Issue |
|---|---|---|---|---|
| **F-01** | CRITICAL | Data/Bootstrapper | `MaterializedViewOrchestrator.cs:48,95` | `MaterializedViewOrchestrator` queries dropped column `reporting_time_zone_id` from `global_config WHERE id = 1`, causing application startup and seeding to crash with a SQL exception. |
| **F-02** | CRITICAL | Intranet/Sync | `CalendarSubscriptionService.cs:172-186` | `CalendarSubscriptionService.TriggerSyncAsync` instantiates new `CalendarEvent` without setting `OrganizationId`, resulting in `Guid.Empty` and causing an unhandled PostgreSQL foreign key violation on `fk_calendar_event_organization_organization_id`. |
| **F-03** | HIGH | Security/Auth | `LocalUserClaimsTransformation.cs:135-141` | `WithoutAuthorityClaims` only removes URI-style `ClaimTypes.Role` and ignores short-form `"role"` / `identity.RoleClaimType`. Upstream unprovisioned users with `"role": "Admin"` bypass ASP.NET Core authorization policies. |
| **F-04** | HIGH | Security/Multi-Tenant | `TenantController.cs:35-181` | `TenantController` does not enforce `ICurrentAccess`. Any authenticated staff member can view all tenants across all organizations, mutate other organizations' tenants, and create tenants with `Guid.Empty` organization ID. |
| **F-05** | HIGH | Intranet/Webhooks | `WebhooksController.cs:76` | Anonymous bulletin webhook calls `BulletinPostService.CreatePostAsync`, which requires an authenticated organization scope and immediately throws `UnauthorizedAccessException` (403). |
| **F-06** | HIGH | Auth/Hangfire | `DashboardSessionController.cs:29`, `AdminDashboardAuthorizationFilter.cs:15` | Hangfire dashboard session creation uses `[Authorize(Policy = "AdminOnly")]`, allowing organization admins to obtain Hangfire dashboard access across all background jobs. |
| **F-07** | MEDIUM | Error Handling | `WeatherService.cs:41`, `ReportingCalendar.cs:31`, `MonitorController.cs:36`, `OrganizationConfigService.cs:29`, `GlobalConfigService.cs:41,59,92,117,147,227` | `currentAccess.Scope` is dereferenced directly without null propagation, throwing `NullReferenceException` (500) when `Scope` is null instead of throwing `UnauthorizedAccessException` (403) or returning 400/404. |
| **F-08** | MEDIUM | Monitoring | `MonitorSynchronizationJob.cs:52-55` | `MonitorSynchronizationJob` builds `localMonitors.ToDictionary(m => m.ExternalId)` globally across all providers. If two organizations use separate provider accounts containing identical external IDs, the job crashes with duplicate key `ArgumentException`. |
| **F-09** | LOW | Database Migration | `20260818232018_AddOrganizationConfig.cs:142-248` | Migration `Down()` drops `organization_config` without copying data back to `global_config`, causing permanent configuration loss upon rollback. |
| **F-10** | LOW | User Administration | `UserController.cs:63-122` | `UserController` does not scope user listings or mutations per organization and creates users without `UserAccess` membership rows. |

---

## 3. Plan vs Implementation Remaining Matrix

| Plan item | Implemented? | Evidence | Safe to defer? |
|---|---|---|---|
| Org-keyed materialized views | NO | `MaterializedViewOrchestrator.cs:48,95` | NO — current unmigrated views crash startup once `AddOrganizationConfig` is applied. |
| Per-org unassigned monitor bucket | NO | `MonitorOrchestrationService.cs:603`, `MonitorController.cs:206` | YES — single global system tenant remains functional for demo/single-org operation. |
| Webhook tenant-org verification | NO | `WebhooksController.cs:31-58` | YES — webhook key remains platform-global. |
| Kiosk org claim | YES | `TokenService.cs:46-49`, `KioskService.cs:81,97` | N/A (Implemented). |
| Hangfire dashboard policy (Platform Admin only) | NO | `AdminDashboardAuthorizationFilter.cs:14-15` | NO — org admins can access platform-wide job execution. |
| Frontend scope & org-scoped lists | NO | `apps/web/src/hooks/useCurrentUser.ts:12-16`, `TenantController.cs:35-58` | YES — planned for Phase 3. |

---

## 4. Verification Baseline
- `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj`: PASSED (285 passed, 0 failed)
- `npx --package=typescript@7.0.2 tsc -b` (in `apps/web`): PASSED (0 errors)
- `npx eslint src/apiClient.ts src/apiClient.test.ts` (in `apps/web`): PASSED (0 errors)
- `npx vitest run src/apiClient.test.ts` (in `apps/web`): PASSED (9 passed)
- `pnpm migration:list`: Verified pending migrations: `RelaxKioskDeviceOrganization`, `TenantOrderFetchingDisabledByDefault`, `AddOrganizationConfig`.
