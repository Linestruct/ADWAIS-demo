# Multi-organization plan

Status: plan. Not implemented.

## Goal

Run one deployment that serves N organizations. Each organization manages its own tenants (e-commerce stores). A tenant is a data source, not a login. The tenant's own staff may later get a restricted viewer login.

Hierarchy:

- Platform admin: the ADWAIS owner. Sees everything.
- Organization: a consulting company. Sees its own tenants, monitors, and intranet.
- Tenant: a store. No logins today. May get a viewer role later.

## Access model

One SSO provider for now. Organizations are data partitions, not identity silos. Per-organization IdPs can come later. The membership model does not care how an identity arrived.

### Membership

New table `UserAccess`:

| Column | Type | Notes |
|---|---|---|
| UserId | Guid | |
| OrganizationId | Guid? | Null for platform admins |
| TenantId | Guid? | Null unless the user is a tenant viewer |
| Role | UserRole | Admin, Employee, Viewer, TenantViewer |

Rows:

- Platform admin: OrganizationId null, TenantId null, Role Admin.
- Org staff: OrganizationId set, TenantId null.
- Tenant viewer: OrganizationId set, TenantId set, Role TenantViewer.

A user can hold several rows. The platform admin role stays separate from org roles.

Add `TenantViewer` to `UserRole`. Do not build tenant login yet.

### Claims and scope

`LocalUserClaimsTransformation` adds `org_id` and `tenant_id` claims from membership. A request-scoped `ICurrentAccess` service exposes the scope:

- Platform admin: everything.
- Org member: one organization.
- Tenant viewer: one organization and one tenant.

All tenant-scoped queries apply the scope through one helper. No service filters by raw tenant ids.

## Surfaces touched

### Data model

- New entity `Organization` (Id, Name).
- New entity `UserAccess`.
- `Tenant.OrganizationId` (non-null). Backfill in migration.
- `User` gets no org column. Membership replaces it.
- `KioskDevice.OrganizationId`. Kiosk activation happens inside an org.
- `CalendarEvent.OrganizationId`, `CalendarSubscription.OrganizationId`, `BulletinPost.OrganizationId`, `FeedSource.OrganizationId`.
- `SystemEvent.OrganizationId` (nullable for platform-wide events).
- `GlobalConfig` splits. Platform-wide values stay. Per-org settings (weather location, reporting timezone, monitoring provider and its settings, fetch intervals) move to `OrganizationConfig`.
- Monitors: unassigned bucket per org. Either `Monitor.TenantId` becomes nullable, or one system-tenant row per org. The single `SystemTenantGuid` sentinel goes away as a global concept.
- `Order.OrganizationSystemId` is dormant. Drop it or fill it from `Tenant.OrganizationId`.

### Auth

- `LocalUserClaimsTransformation`: map membership rows to `org_id` and `tenant_id` claims plus the role. First-login provisioning needs a rule: which org does a new user join? Start with platform-admin assignment. Domain-based auto-assign later.
- Policies: `AdminOnly`, `StaffAccess`, `KioskOrStaffAccess` keep their role checks. Scope enforcement goes through `ICurrentAccess`. Tenant viewers get their own policy later.
- Kiosk JWT (`TokenService`): add an org id claim. Kiosk tokens are issued inside an org only.
- `/api/users/me`: return org and tenant membership for the frontend.
- Hangfire dashboard filter: platform admin only. Org admins do not see Hangfire.

### Statistics

- `FinancialService` every method: Kpis, AccumulatedRevenue, RevenueEfficiency, CrossSegmentDistribution, PortfolioImpact, NetGrowthAddition, OrderDistribution, TransactionDensity, CumulativeGrowthDelta, Orders. Each merge path and tenant dictionary gains org filtering through the scope helper. The system-tenant exclusion becomes per-org.
- Materialized views (`MaterializedViewOrchestrator`): add `organization_id` to the financial tenant rollup and the monitoring rollups. The date-only global rollups gain an org key instead of splitting per org. One view per domain serves org dashboards (filter by organization_id) and the platform total (group by date over the same view). Per-org views are ruled out: they break the EF view mappings, add DDL per org, and give nothing at this scale. All views stop reading `global_config WHERE id = 1` for the timezone. They read the org config instead. Views are created on startup when missing. Deployments with existing views must drop and recreate them.
- `FinancialRequestDto` tenantTypes filter: org-scoped tenant type cohorts.
- `GetOrdersAsync`: unscoped today. It becomes org-scoped, and tenant-scoped for tenant viewers.

### Fleet and monitoring

- `MonitorController`: list, analytics, availability, unassigned, assign, unassign. All become org-scoped. Unassigned lists the org's bucket, not a global one.
- `MonitorOrchestrationService`: same for every method. Assign and unassign only within the org.
- Monitoring provider: `GlobalConfig.MonitoringProvider` becomes per-org (`OrganizationConfig`). Each org configures its own UptimeRobot account. Provider settings, the rate-limit handler, and account stats follow the org.
- `MonitorSynchronizationJob`: sync per org and per provider account. New upstream monitors land in that org's unassigned bucket.
- Uptime and latency dispatchers: select monitors per org.

### Intranet

- `CalendarEventService`, `BulletinPostService`, `FeedService`, `CalendarFeedService`, `CalendarSubscriptionService`, `FeedAggregationService`: all filter by org through the scope helper.
- `CalendarEventController`, `CalendarFeedController`, `CalendarSubscriptionController`, `BulletinPostController`, `FeedController`: reads scoped by org, writes scoped by org and role.
- ICS feed (`api/intranet/calendar/feed.ics`): the token validates org membership and emits only that org's events.
- Feed sources: unique per org, not globally. `FeedAggregationJob` and `CalendarSyncJob` iterate per org.
- `WebhooksController` (Litium order webhook): the tenant lookup must verify the tenant belongs to the intended org. The webhook key stays platform-level for now.

### Weather

- `WeatherService` and `WeatherController`: weather location comes from `OrganizationConfig`, cached per org. The unscoped `weather:current` cache key becomes org-keyed.

### Jobs

- `OrderFetchDispatcherJob`: select tenants per org. Watermarks stay tenant-level and compose fine.
- Intervals (`GlobalConfigService` plus job registration in `ApplicationBootstrapperExtensions`): per-org intervals mean per-org recurring jobs or one dispatcher job that iterates orgs. Prefer one job per domain that iterates orgs. Per-org recurring jobs multiply with org count.
- `UpdateGlobalMonitoringStatsJob`: per org account.
- `SystemEventCleanupJob`, `RuntimeDataSeederJob`: org-aware.

### Frontend

- `useCurrentUser`: profile gains org id, org name, tenant id. This is the single source of role and scope.
- Settings: new Organization section (name, timezone, weather, monitoring provider, feeds). Tenant, monitor, and user lists become org-scoped. Platform admins see an org selector plus platform-wide views.
- `useTenantsViewModel`, `useMonitorQueries`, `useUserQueries`, `useFinancialQueries`, `useFleetQueries`: all already pass tenantId as a filter. The server enforces the boundary. The frontend needs no structural change beyond scoped lists.
- `SYSTEM_TENANT_ID` sentinel in `useTenantsViewModel`, `useMonitorQueries`, `MonitorDetail`: replace with org-scoped unassigned handling.
- Role checks keep the same shape. Tenant viewer UI comes later.
- Kiosk flow (`authentication.tsx`, `useKioskAuth`): activation inside the org.

### Demo data

- `DemoDataCatalog`, `DatabaseSeeder`, `RuntimeDataSeederJob`: seed one demo org (Motillo) with tenants, monitors, feeds, and users. Platform admin gets a seed account.

## Phases

1. Data model and identity: Organization, UserAccess, claims, ICurrentAccess, Tenant.OrganizationId, per-org intranet ids, OrganizationConfig. Backfill migration. Kiosk org claim.
2. Enforcement: apply scope to statistics, fleet, intranet, weather, and jobs. Replace global rollups with org-keyed rollups.
3. UI: org-scoped lists and org settings. Platform admin views.
4. Tenant viewers (later): membership rows with TenantId, TenantViewer role, restricted screens. No login UI until needed.

## Deferred

- Per-organization IdPs.
- Tenant login and onboarding flows.
- Tenant viewer screens.
- Per-org recurring Hangfire jobs. Iterate orgs in one job instead.
