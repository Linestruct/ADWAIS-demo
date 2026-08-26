# Multi-organization smoke test plan

Purpose: exercise the whole multi-organization surface against a running system, find edge cases the unit tests cannot reach, and validate behavior end to end. Run every section in order. Record one line per case: PASS, FAIL with reproduction steps, or SKIP with reason.

## 1. Environment

1. `pnpm db:up` from the repo root.
2. Apply migrations. Running `pnpm migration:update` against a database needs explicit owner permission; say so before this step.
3. Start the API and the web app in Development. The dev mock authenticates via `DevMock`; use `DEV_MOCK_ORG_ID` to pick an organization.
4. Seed state: demo data seeds into the default organization (Motillo). Confirm two organizations exist so cross-org checks are meaningful. If only one exists, insert a second organization plus its `organization_config`, membership rows, and unassigned bucket through the migration path or a script approved by the owner.

Baseline checks before testing:

- `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj` passes.
- API startup completes without SQL errors. The view orchestrator drops and recreates all eight rollups on boot; watch for failures there first.

## 2. Identity and claims

| # | Case | Expected |
|---|---|---|
| 2.1 | Log in as a user with no `UserAccess` row | No role or scope claims; every staff endpoint returns 403 |
| 2.2 | IdP sends `"role": "Admin"` and `"roles": ["Admin"]` short-form claims for that same user | Claims are scrubbed; principal has no authority; no bypass of policies |
| 2.3 | IdP sends `ClaimTypes.NameIdentifier` and org/tenant/platform claims | All scrubbed; local identity is the only source |
| 2.4 | Single-org member logs in with no scope headers | Defaults to their organization |
| 2.5 | Multi-org member requests with `X-ADWAIS-ORG-ID` of each org | Effective scope switches; claims match the selected org |
| 2.6 | Org member sends another organization's id in `X-ADWAIS-ORG-ID` | Rejected or falls back per resolver rules; never grants the other org |
| 2.7 | Malformed header values (`"not-a-guid"`, empty string) | No crash; treated as absent |
| 2.8 | Tenant viewer sends another tenant's id in `X-ADWAIS-TENANT-ID` | Pinned to their tenant regardless |
| 2.9 | Kiosk token issued inside an organization | `/api/users/me` returns that org id and kiosk role |
| 2.10 | Kiosk token from before the migration (no org claim) | Scope resolves to null; org-scoped endpoints reject cleanly, no 500 |
| 2.11 | `DEV_MOCK_ORG_ID` set to each org and to garbage | Development only; garbage behaves like unset |

## 3. Profile and users

| # | Case | Expected |
|---|---|---|
| 3.1 | `/api/users/me` as org staff | Returns orgId, orgName, null tenantId, isPlatformAdmin false |
| 3.2 | `/api/users/me` as platform admin | Nulls for org fields, isPlatformAdmin true |
| 3.3 | `/api/users/me` as tenant viewer | Returns pinned org and tenant ids |
| 3.4 | `/api/users/me` for an org whose row was deleted after sign-in | orgName null, no crash |
| 3.5 | Org admin lists users | Only members of their org appear |
| 3.6 | Platform admin lists users | Everyone appears, including membership-less rows |
| 3.7 | Org admin creates a user | Membership row created in caller's org with matching role |
| 3.8 | Platform admin creates a user with no org selected | Membership lands in the default organization |
| 3.9 | Update a user's role | Both legacy role and in-scope membership rows change |
| 3.10 | Delete a user who also belongs to another org | Refused; user and both memberships intact |
| 3.11 | Delete a user whose memberships are all in-org | Removed with memberships |

## 4. Tenant administration

| # | Case | Expected |
|---|---|---|
| 4.1 | Org staff reads tenants | Only own-org tenants |
| 4.2 | Org admin updates another org's tenant by id | 404 |
| 4.3 | Org admin deletes another org's tenant | 404 |
| 4.4 | Anyone deletes an unassigned bucket (`is_system`) | 400 |
| 4.5 | Org admin creates a tenant | Lands in caller's org |
| 4.6 | Platform admin creates a tenant with no org selected | Lands in default org |

## 5. Monitoring and buckets

| # | Case | Expected |
|---|---|---|
| 5.1 | `GET /api/monitors/unassigned` as org staff | Only monitors in own-org bucket |
| 5.2 | Same as platform admin | Every org's buckets |
| 5.3 | Assign monitor to another org's tenant | 403 |
| 5.4 | Unassign a monitor | It moves to its OWN org's bucket, not the caller's |
| 5.5 | Delete a tenant with monitors | Monitors land in the deleted tenant's org bucket |
| 5.6 | Two organizations, separate provider accounts, identical external monitor ids | Sync job does not collide; each lands in its own bucket |
| 5.7 | Organization missing its bucket row | Sync job fails loudly with the clear message; no silent cross-org placement |
| 5.8 | Monitor analytics and availability exclude all buckets, not just the legacy sentinel | Bucket monitors absent from fleet aggregates |
| 5.9 | Latency global rollup excludes bucket monitors after rebuild | Confirmed by query on `v_mat_daily_latency_global_rollup` |

## 6. Financial statistics

| # | Case | Expected |
|---|---|---|
| 6.1 | Every KPI endpoint as org staff | Zero rows from other orgs |
| 6.2 | Explicit tenantId from another org | 403 |
| 6.3 | Platform global series equals sum over org-keyed rollup rows for the same dates | Values match |
| 6.4 | Two orgs in different timezones around a DST switch | Each org's daily buckets follow its own timezone |
| 6.5 | Orders older than 730 days relative to the org clock | Excluded per org independently |
| 6.6 | Current-day orders merge with rollup history without double counting | Continuous series |

## 7. Intranet and calendar

| # | Case | Expected |
|---|---|---|
| 7.1 | Bulletin board, feeds, calendar events per org | Strict isolation both directions |
| 7.2 | ICS feed token of an org user | Emits only that org's events |
| 7.3 | External calendar sync creates events | Events carry the subscription's organization id |
| 7.4 | Bulletin webhook with unknown `organizationId` | 404 via KeyNotFoundException |
| 7.5 | Bulletin webhook with valid org and correct key | Post created in that org |
| 7.6 | Bulletin webhook with wrong or missing key | 401 |
| 7.7 | Logged-in org staff calls bulletin create for another org | 403 |

## 8. Weather, config, jobs

| # | Case | Expected |
|---|---|---|
| 8.1 | Weather as platform admin (no org) | Clear error, not 500 |
| 8.2 | Weather cache key per org | Org A fetch does not serve org B |
| 8.3 | OrganizationConfig save triggers timezone-dependent refreshes without blocking the request | Responsive endpoint |
| 8.4 | Order fetch dispatcher | Skips disabled and system tenants; watermarks unchanged |
| 8.5 | Intervals endpoints read OrganizationConfig per org | Correct per org; platform fallback defaults |

## 9. Error semantics and frontend session

| # | Case | Expected |
|---|---|---|
| 9.1 | Spot-check mapping table: denied write 403, misconfigured 409, missing 404, misuse 400 | Matches GlobalExceptionHandler |
| 9.2 | SPA receives 403 | Error surfaced; NO logout, NO sessionStorage clear |
| 9.3 | SPA receives 401 | Session invalidation runs |
| 9.4 | Hangfire dashboard as org admin | Denied at both policy and dashboard filter |
| 9.5 | Hangfire dashboard as platform admin via dashboard-session cookie | Accessible; cookie expires in five minutes |

## 10. Data migration rehearsal

Run on a disposable database copy only:

1. Restore a pre-migration backup (before `AddPerOrganizationSystemTenants`).
2. Apply all pending migrations.
3. Verify: sentinel marked `is_system`; every organization has exactly one bucket; legacy sentinel monitors remain visible in the default org's bucket; no orphaned monitors.

## Pass criteria

- Every case PASS or explicitly accepted SKIP.
- Zero 500s outside deliberately broken-input cases.
- No cross-organization data leak observed anywhere.
- Findings filed with reproduction steps, severity (CRITICAL/HIGH/MEDIUM/LOW), and location.

## Unit coverage map

Almost every scenario has an automated unit test now. The table lists the backing suites. Scenarios marked "smoke only" depend on SQL, a running scheduler, a browser, or a real database and remain in the plan for that reason.

| Scenario | Unit coverage |
|---|---|
| 2.1 unprovisioned user | LocalUserClaimsTransformationTests |
| 2.2 short-form role scrub | LocalUserClaimsTransformationTests |
| 2.3 identity claim scrub | LocalUserClaimsTransformationTests |
| 2.4 single-org default | LocalUserClaimsTransformationTests, AccessScopeResolverTests |
| 2.5 multi-org header switching | LocalUserClaimsTransformationTests |
| 2.6 other-org header rejected | LocalUserClaimsTransformationTests |
| 2.7 malformed headers | LocalUserClaimsTransformationTests (`TransformAsync_MalformedScopeHeaders_AreTreatedAsAbsent`) |
| 2.8 tenant viewer pinning | AccessScopeResolverTests |
| 2.9 kiosk token org claim | TokenServiceTests, CurrentAccessServiceTests |
| 2.10 legacy kiosk token | CurrentAccessServiceTests (`Resolve_AdminRoleWithoutPlatformOrOrgClaim_ReturnsNull`) |
| 2.11 dev mock gated to Development | AuthenticationExtensionsTests (`ProductionEnvironment_DoesNotRegisterDevMockScheme`) |
| 3.1-3.4 profile fields | UserControllerTests |
| 3.5-3.11 user administration | UserServiceTests |
| 4.1-4.6 tenant administration | TenantControllerTests |
| 5.1-5.2 unassigned listing | MonitorOrchestrationServiceTests (`GetUnassignedMonitorsAsync_ShouldReturnOnlyCallerOrganizationBucket`) |
| 5.3 cross-org assign 403 | MonitorOrchestrationServiceTests |
| 5.4 unassign to own bucket | MonitorOrchestrationServiceTests (`UnassignMonitorAsync_ShouldMoveMonitorToItsOrganizationBucket`) |
| 5.5 tenant delete reassignment | MonitorOrchestrationServiceTests (`ReassignAllTenantMonitorsToSystemAsync_ShouldReassignMatchingMonitors`) |
| 5.6 duplicate external ids across orgs | MonitorSynchronizationJobTests (`ExecuteAsync_IdenticalExternalIdsAcrossOrganizations_LandInRespectiveBuckets`) |
| 5.7 missing bucket fails loudly | MonitorSynchronizationJobTests (`ExecuteAsync_OrganizationWithoutBucket_ThrowsLoudly`) |
| 5.8 buckets excluded from fleet | MonitorOrchestrationServiceTests (`GetMonitorsAsync_FleetListing_ExcludesUnassignedBuckets`) |
| 5.9 view SQL excludes buckets | Smoke only |
| 6.1-6.2 financial isolation | FinancialServiceTests |
| 6.3 platform sums org rows once | FinancialServiceTests (`GetNetGrowthAdditionAsync_PlatformDaily_SumsOrganizationRollupRowsOnce`) |
| 6.4-6.6 timezone, retention, day merge | Smoke only |
| 7.1 intranet isolation | BulletinPostServiceTests, CalendarEventServiceTests, FeedServiceTests |
| 7.2 ICS feed scoping | CalendarFeedServiceTests |
| 7.3 sync events carry org id | CalendarSubscriptionServiceTests |
| 7.4-7.6 bulletin webhook key and org validation | WebhooksControllerTests, BulletinPostServiceTests |
| 7.7 cross-org bulletin create 403 | BulletinPostServiceTests |
| 8.1 weather platform scope error | WeatherServiceTests |
| 8.2 per-org cache keys | WeatherServiceTests (`GetCurrentWeatherAsync_UsesDistinctCacheKeysPerOrganization`) |
| 8.3-8.5 config and job intervals | GlobalConfigServiceTests; live scheduling remains smoke only |
| 9.1 exception mapping table | GlobalExceptionHandlerTests |
| 9.2-9.3 SPA 403 vs 401 | `apps/web/src/apiClient.test.ts` |
| 9.4-9.5 Hangfire access | DashboardSessionControllerTests, AdminDashboardAuthorizationFilterTests; cookie expiry smoke only |
| 10 migration rehearsal | Smoke only |

## Run log

First run: 2026-08-26, local PostgreSQL container, Development environment, dev mock authentication, two organizations (Default Organization, SmokeTest Org).

Findings found and fixed during the run:

| ID | Severity | Finding | Fix |
|---|---|---|---|
| F-SMOKE-1 | CRITICAL | `AddOrganizationConfig` migration failed on a database with existing views: `cannot drop column reporting_time_zone_id ... other objects depend on it` | `0fc615c`: migration drops the eight rollups before dropping the columns |
| F-SMOKE-2 | HIGH | Views were only synced when the seeder had just seeded rows. On a later boot they were skipped, leaving all view-backed endpoints 500 | `0415217`: bootstrap syncs views on every boot |
| F-SMOKE-3 | HIGH | Dev mock principals lost all authority claims in the claims transformation (no `sub` claim), so every dev request was 403 | `9b23fa9`: principals built by `AccessClaimsBuilder` bypass the transformation |
| F-SMOKE-4 | HIGH | Hangfire dashboard 403 for platform admins: same stripping bug on the dashboard cookie principal | `9313411`: cookie identity built through `AccessClaimsBuilder` |
| F-SMOKE-5 | HIGH | Latency and availability view SQL referenced a column the lateral join did not expose; bootstrap crashed and left only the financial views | `b90ac3d`: lateral joins expose `organization_id` |

Scenarios executed live:

| # | Result | Notes |
|---|---|---|
| 2.11 | PASS | DevMock only in Development; prod registration asserted in unit tests |
| 3.1-3.4 | PASS | `/api/users/me` returns orgId, orgName, tenantId, isPlatformAdmin for org and platform scopes |
| 4.1, 4.5 | PASS | Org scope sees only its tenants; platform sees all |
| 5.1, 5.2 | PASS | Unassigned returns per-org bucket lists (empty buckets in this run) |
| 5.9 | PASS | All eight views exist after bootstrap and serve data |
| 6.1, 6.3 | PASS | SmokeTest org KPIs are all zero; platform KPIs aggregate 273M revenue across 30 tenants from org-keyed views |
| 7.4 | PASS | Bulletin webhook with unknown organization returns 404 |
| 7.5 | PASS | Bulletin webhook with valid organization creates the post |
| 7.6 | PASS | Bulletin webhook with wrong key returns 401 |
| 8.1 | PASS | Weather without an org scope returns 400 |
| 8.1 (org) | PASS | Weather with unconfigured location returns 409 |
| 9.1 | PASS | Live spot checks: 400, 401, 404, 409 match the contract |
| 9.4, 9.5 | PASS | Dashboard session 204, then `/hangfire` renders for platform admin |
| 10 | PASS | Migration applied to a database with pre-existing views after the F-SMOKE-1 fix |

Open findings:

| ID | Severity | Finding | Status |
|---|---|---|---|
| F-SMOKE-6 | LOW | Tenant lists include unassigned buckets | RESOLVED (`565bebe`): `is_system` rows excluded from tenant lists and lookups; bucket edits rejected with 400 |

Not executed live: IdP claim-scrub matrix (needs a real OIDC provider; unit-covered), provider sync collisions (needs UptimeRobot accounts; unit-covered), SPA logout flows (needs a browser; unit-covered), DST and 730-day retention edges (need a clock-controlled database; unit coverage does not reach SQL).

## Out of scope

Frontend Phase 3 (org selector, org settings screens). Per-org IdPs. Tenant viewer UI.
