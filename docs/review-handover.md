# Review handover: multi-organization access model

## Mission

Review the entire feature branch with fresh eyes. Treat the summary below as a claim, not a fact. Assume it is wrong until the code proves it right. Your job is to find defects, not to confirm the work. You are the last line of defense before this merges.

Deliver one report in the exact format at the end of this document. A report that skips a mandatory section is incomplete. A claim without a `file:line` citation is unverified.

## Context

- Repository: `C:\Users\ollem\Git\motillo project\adwais-multiorg` (git worktree).
- Branch: `feature/multi-organization`. Base: `main`.
- Stack: .NET 10 backend (`apps/server/ADWAIS`), React/Vite/TypeScript frontend (`apps/web`), PostgreSQL, Hangfire, OIDC.
- Windows, PowerShell. Do not use `docker exec psql` or `psql`. Use only the pnpm scripts in `AGENTS.md`.
- Read `AGENTS.md` at the repo root first and obey it, including the BSL header rule, the commit style, and the database rule.
- Read `docs/multi-organization.md` first. It is the contract. The implementation must match it.

## The claim under review

One deployment serves N organizations. Each organization owns tenants, monitors, feeds, and intranet content. Platform admins see everything. Org staff see their org only. Tenant viewers are future work.

The implementation claims:

1. Membership (`UserAccess` table) is the single source of authority. Claims transformation maps membership to claims; no upstream claim survives.
2. A request-scoped `ICurrentAccess` exposes the effective scope. One enforcement idiom (`OrganizationFilter` + `TenantVisibility`) applies it everywhere.
3. Errors are precise: 401 unauthenticated, 403 denied, 409 misconfigured, 400 misuse, 404 missing. The SPA logs out only on 401.
4. `OrganizationConfig` per org replaces `GlobalConfig` for weather, timezone, monitoring provider, and intervals. Backfill migration is lossless.
5. All 29 commits on the branch are coherent and each concern is isolated.

## Mandatory questions

Answer all twelve. Each answer needs evidence (`file:line`), not opinion.

1. Can any upstream (IdP) claim grant authority that membership does not? Walk every path of `LocalUserClaimsTransformation`: missing sub, unknown user, null scope, valid scope, kiosk issuer, dev issuer.
2. Does the claim format round-trip exactly? Every claim `AccessClaimsBuilder` writes must be read by `CurrentAccessService.Resolve`, and vice versa. List every claim type on both sides.
3. Is there any data access path without scope enforcement? Audit every service, controller, job, and webhook that touches tenant or org data. List the paths you audited and the result for each.
4. Is the backfill migration lossless on a database with existing data? What happens to rows where `Tenant.OrganizationId` is null? Can `Up()` fail partway? Reorder the steps in your head and check the failure modes.
5. Can any principal escalate? Consider header spoofing, multiple membership rows, tenant viewer rows (future), kiosk principals, and the dev principal.
6. Does every throw site match the documented error contract? List any site that throws the wrong type or lets a 5xx escape for a user-facing condition.
7. Does the SPA ever log out on 403? Check every file under `apps/web/src` that inspects status codes, not only `apiClient.ts`.
8. Can `DEV_MOCK_ORG_ID` or the dev authentication scheme run outside the Development environment? Is there any other dev-only backdoor?
9. Do the org-keyed materialized views (plan section "Statistics") break the EF view mappings or the current single-tenant database when implemented? They are not built yet. Confirm they are not built, and confirm the plan wording stays consistent with the decision commit (`11e1fcd` on `main`).
10. Does any job still read `global_config` where `OrganizationConfig` is authoritative: timezone, weather location, monitoring provider, fetch intervals? Check the seeded jobs and the bootstrapper.
11. Is the kiosk flow consistent? What does the kiosk token carry today? What happens to existing kiosk tokens after the migration? The plan defers the kiosk org claim. Confirm the deferral is safe.
12. Rate-limit and UX: with 403 no longer triggering session invalidation, where does a denied user see the failure? Is there any endpoint where a denied response is silently swallowed?

## Coverage checklist

Every row gets one of: `CHECKED`, `NOT COVERED`, `NOT APPLICABLE`. Each row needs evidence. Rows without evidence are findings.

### Access model

- Membership rows to claims mapping (org, tenant, roles per scope).
- Effective scope selection: headers, platform admin, single-org default, multi-org default.
- `ICurrentAccess` and its Resolve implementation.
- `OrganizationFilter` and `TenantVisibility` semantics: null, empty, single, multi.
- Scrubbing: `WithoutAuthorityClaims` and every call site of the transformation.

### Enforcement

- `FinancialService`: all methods (Kpis, AccumulatedRevenue, RevenueEfficiency, CrossSegmentDistribution, PortfolioImpact, NetGrowthAddition, OrderDistribution, TransactionDensity, CumulativeGrowthDelta, Orders, GetOrdersAsync).
- `MonitorOrchestrationService`: list, analytics, availability, unassigned, assign, unassign.
- `MonitorController`: every endpoint.
- Intranet: `CalendarEventService`, `CalendarSubscriptionService`, `BulletinPostService`, `FeedService`, `CalendarFeedService`, `FeedAggregationService`, and their controllers, including the ICS feed token path.
- `WeatherService` and `WeatherController`: org config, cache key, scope.
- Webhooks: Litium order webhook tenant-org verification (plan says this is remaining; confirm).
- Jobs: `OrderFetchDispatcherJob`, `MonitorSynchronizationJob`, uptime and latency dispatchers, `UpdateGlobalMonitoringStatsJob`, `SystemEventCleanupJob`, `RuntimeDataSeederJob`, `FeedAggregationJob`, `CalendarSyncJob`, `GlobalConfigService`.
- `SystemHealthService`: both database paths.

### Data and migration

- Migration `20260818232018_AddOrganizationConfig`: order of operations, backfill, dropped columns.
- `Organization`, `UserAccess`, `Tenant.OrganizationId`, intranet ids, `KioskDevice.OrganizationId`.
- `OrganizationConfig` entity and its mapping to `GlobalConfig` leftovers.
- `DatabaseSeeder` and `RuntimeDataSeederJob` timezone reads.
- `Order.OrganizationSystemId`: confirm dormant status and the plan decision.

### Auth

- Policies: `AdminOnly`, `StaffAccess`, `KioskOrStaffAccess`.
- `TokenService` kiosk JWT.
- `/api/users/me` contract and its 401 and 403 behavior.
- Hangfire dashboard filter (plan says remaining; confirm).
- `GlobalExceptionHandler`: mapping table, log levels, and the `ConfigurationException` path.

### Frontend

- `apiClient.ts`: 401-only session invalidation; `X-Bypass-Global-401`.
- `useCurrentUser` and profile shape.
- Kiosk flow: `authentication.tsx`, `useKioskAuth`, `removeKioskToken`.
- Demo mode reload path.
- Any other status-code inspection under `apps/web/src`.

### Tests

- Round-trip tests: do they cover every scope shape and a claim-less principal?
- Scrub test: does it feed a genuine upstream role claim and forged scope claims?
- Error-semantics tests: 403, 409, 404, 400 expectations at every changed throw site.
- SPA tests: the four rewritten 403 tests in `apiClient.test.ts`.
- Test quality: are there tests that pass for the wrong reason?

## Verification commands

Run all of these and log the results.

- `dotnet test apps/server/ADWAIS/tests/Adwais.Tests/Adwais.Tests.csproj`
- From `apps/web`: `npx --package=typescript@7.0.2 tsc -b`
- From `apps/web`: `npx eslint <changed file>`
- From `apps/web`: `npx vitest run src/apiClient.test.ts`
- From the repo root: `pnpm migration:list` (read-only). Do not run `migration:update` without explicit permission from the owner.

## Response format

Return exactly this structure. Every section is mandatory.

```text
# Review report

## 1. Verdict
APPROVE | APPROVE WITH NITS | CHANGES REQUIRED
One sentence. Cite the top reason.

## 2. Executive summary
Max 15 lines. What is sound, what is not, in priority order.

## 3. Answers to the mandatory questions
For each of the 12 questions:
Q<n>. Answer: <one to three sentences>
     Evidence: <file:line list>
     Risk: NONE | LOW | MEDIUM | HIGH | CRITICAL

## 4. Coverage checklist result
Only rows that are NOT COVERED or whose evidence is weak. For each:
Row: <name>
Result: NOT COVERED | WEAK EVIDENCE
Evidence: <file:line list>

## 5. Findings
One table row per finding, sorted by severity:
| ID | Severity | Area | Location | Issue | Recommendation |
Severity scale: CRITICAL, HIGH, MEDIUM, LOW, NIT.
CRITICAL: merge blocker, security, data loss. HIGH: wrong behavior in a normal path.
MEDIUM: wrong behavior in an edge path. LOW: style, robustness. NIT: optional.
CRITICAL and HIGH findings need a reproduction path or a code trace, not just an assertion.

## 6. Plan-vs-implementation gaps
Table of every "Remaining" item in docs/multi-organization.md:
| Plan item | Implemented? | Evidence | Safe to defer? |

## 7. Test quality
Three lines minimum. What is over-tested, what is under-tested, what is missing.

## 8. Risk register
Top five risks, ranked. Each: what, why it matters, what would trigger it, who owns it.

## 9. Verification log
Each command you ran, its result, and the date.

## 10. Known unknowns
What you could not verify and why. Do not pad this section. If it is empty, say so.
```

## Ground rules

- Cite `file:line` for every claim. A claim without a citation is a finding.
- No generic praise. A PASS needs evidence that you read the code.
- If you cannot answer a mandatory question, mark it UNANSWERED and explain why. An unanswered mandatory question is a finding by itself.
- Do not trust the commit messages. Verify the code they claim.
- Do not run `migration:update` or touch the live database without explicit permission.
- Do not fix what you find. Report it.