# Multi-Organization Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remediate all critical, high, medium, and low findings identified during the multi-organization security and architecture review across database views, entity foreign keys, claim scrubbing, Hangfire authorization, null handling, and tenant scoping.

**Architecture:** Fix the SQL definitions in `MaterializedViewOrchestrator` to reference `organization_config` via `tenant.organization_id`. Ensure `CalendarEvent` sets `OrganizationId` upon sync. Strip all variant forms of role claims in `LocalUserClaimsTransformation`. Introduce a `PlatformAdminOnly` policy and claim check for Hangfire. Harden null-safety on `ICurrentAccess.Scope` across API controllers and domain services, and scope `TenantController` and `UserController`.

**Tech Stack:** ASP.NET Core 9, Entity Framework Core 9, PostgreSQL / Npgsql, Hangfire, React, TypeScript.

---

### Phase 1: Materialized Views & Database Schema Integrity (F-01, F-09)

**Files:**
- Modify: `apps/server/ADWAIS/src/Infrastructure/Helpers/MaterializedViewOrchestrator.cs`
- Modify: `apps/server/ADWAIS/src/Infrastructure/Migrations/20260818232018_AddOrganizationConfig.cs`
- Test: `apps/server/ADWAIS/tests/Adwais.Tests/Services/FinancialServiceTests.cs`

- [ ] **Step 1: Update SQL definitions in MaterializedViewOrchestrator.cs**
Update `v_mat_financial_daily_tenant_rollup`, `v_mat_daily_latency_monitor_rollup`, and `v_mat_daily_availability_monitor_rollup` to obtain reporting timezone information by joining `organization_config` with `tenant`.
- [ ] **Step 2: Update Migration 20260818232018_AddOrganizationConfig.cs Down() method**
Ensure `Down()` restores `global_config` values from `organization_config` before dropping the table.
- [ ] **Step 3: Run backend tests to verify Phase 1**
Run: `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj`

---

### Phase 2: Calendar Subscription Sync & Entity Integrity (F-02)

**Files:**
- Modify: `apps/server/ADWAIS/src/Infrastructure/Services/Calendar/CalendarSubscriptionService.cs`
- Modify: `apps/server/ADWAIS/tests/Adwais.Tests/Services/CalendarEventServiceTests.cs`

- [ ] **Step 1: Set OrganizationId when creating new CalendarEvent instances in CalendarSubscriptionService.cs**
In `TriggerSyncAsync`, assign `newEvent.OrganizationId = sub.OrganizationId`.
- [ ] **Step 2: Add test verifying OrganizationId assignment on external sync**
Add test in `CalendarEventServiceTests.cs`.
- [ ] **Step 3: Run tests to verify Phase 2**
Run: `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj --filter FullyQualifiedName~Calendar`

---

### Phase 3: Upstream Role Scrubbing & Claims Security (F-03, Q1, Q2)

**Files:**
- Modify: `apps/server/ADWAIS/src/Infrastructure/Security/LocalUserClaimsTransformation.cs`
- Modify: `apps/server/ADWAIS/src/Api/Services/CurrentAccessService.cs`
- Modify: `apps/server/ADWAIS/tests/Adwais.Tests/Services/LocalUserClaimsTransformationTests.cs`

- [ ] **Step 1: Enhance WithoutAuthorityClaims to remove short-form "role" and "roles" claims**
In `LocalUserClaimsTransformation.cs`, filter out `ClaimTypes.Role`, `"role"`, `"roles"`, and `identity.RoleClaimType`.
- [ ] **Step 2: Update CurrentAccessService.cs to read both ClaimTypes.Role and "role"**
In `CurrentAccessService.Resolve`, check both `ClaimTypes.Role` and `"role"`.
- [ ] **Step 3: Add unit tests in LocalUserClaimsTransformationTests.cs**
Test that external tokens with `"role": "Admin"` are properly scrubbed for unprovisioned users.
- [ ] **Step 4: Run tests to verify Phase 3**
Run: `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj --filter FullyQualifiedName~Claims`

---

### Phase 4: Hangfire Platform Admin Policy & Dashboard Protection (F-06)

**Files:**
- Modify: `apps/server/ADWAIS/src/Api/Extensions/AuthenticationExtensions.cs`
- Modify: `apps/server/ADWAIS/src/Api/Controllers/Authentication/DashboardSessionController.cs`
- Modify: `apps/server/ADWAIS/src/Api/Filters/AdminDashboardAuthorizationFilter.cs`
- Create: `apps/server/ADWAIS/tests/Adwais.Tests/Controllers/DashboardSessionControllerTests.cs`

- [ ] **Step 1: Register PlatformAdminOnly policy in AuthenticationExtensions.cs**
- [ ] **Step 2: Restrict DashboardSessionController to PlatformAdminOnly and emit IsPlatformAdmin claim in cookie**
- [ ] **Step 3: Update AdminDashboardAuthorizationFilter to require IsPlatformAdmin claim**
- [ ] **Step 4: Create DashboardSessionControllerTests.cs to test policy enforcement**
- [ ] **Step 5: Run tests to verify Phase 4**
Run: `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj --filter FullyQualifiedName~Dashboard`

---

### Phase 5: NullReferenceException Hardening (F-07)

**Files:**
- Modify: `apps/server/ADWAIS/src/Infrastructure/Services/Weather/WeatherService.cs`
- Modify: `apps/server/ADWAIS/src/Application/Services/ReportingCalendar.cs`
- Modify: `apps/server/ADWAIS/src/Api/Controllers/Analytics/MonitorController.cs`
- Modify: `apps/server/ADWAIS/src/Infrastructure/Services/Configuration/OrganizationConfigService.cs`
- Modify: `apps/server/ADWAIS/src/Infrastructure/Services/Configuration/GlobalConfigService.cs`

- [ ] **Step 1: Guard null CurrentAccess.Scope across domain services and controllers**
- [ ] **Step 2: Run test suite to verify no regressions**
Run: `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj`

---

### Phase 6: Multi-Tenant Scope in Tenant & User Controllers (F-04, F-10)

**Files:**
- Modify: `apps/server/ADWAIS/src/Api/Controllers/Administration/TenantController.cs`
- Modify: `apps/server/ADWAIS/src/Api/Controllers/Authentication/UserController.cs`
- Modify: `apps/server/ADWAIS/tests/Adwais.Tests/Controllers/TenantControllerTests.cs`

- [ ] **Step 1: Inject ICurrentAccess and enforce OrganizationFilter in TenantController.cs**
- [ ] **Step 2: Inject ICurrentAccess and enforce scope in UserController.cs**
- [ ] **Step 3: Update TenantControllerTests.cs and run tests**
Run: `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj --filter FullyQualifiedName~TenantController`

---

### Phase 7: Webhook & Monitor Sync Isolation (F-05, F-08)

**Files:**
- Modify: `apps/server/ADWAIS/src/Infrastructure/Services/Content/BulletinPostService.cs`
- Modify: `apps/server/ADWAIS/src/Api/Controllers/Integrations/WebhooksController.cs`
- Modify: `apps/server/ADWAIS/src/Infrastructure/Jobs/Monitor/MonitorSynchronizationJob.cs`
- Modify: `apps/server/ADWAIS/tests/Adwais.Tests/Controllers/IntranetControllerTests.cs`

- [ ] **Step 1: Allow webhook creation with target organization in BulletinPostService and WebhooksController**
- [ ] **Step 2: Scope MonitorSynchronizationJob local monitor lookup per organization**
- [ ] **Step 3: Update IntranetControllerTests and verify webhooks**
Run: `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj --filter FullyQualifiedName~Intranet`

---

### Phase 8: Full Verification

- [ ] **Step 1: Run full .NET backend test suite**
- [ ] **Step 2: Run web unit tests (Vitest)**
- [ ] **Step 3: Run web typecheck (tsc) and lint (eslint)**
- [ ] **Step 4: Verify migration status (pnpm migration:list)**
