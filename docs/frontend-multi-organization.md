# Multi-organization frontend plan

Status: in progress. Backend prerequisites B1 to B3 are implemented and tested (393 backend tests). W1 (client regeneration, required DTO members, scope-aware shared types) and W2 (scope-aware `useCurrentUser` plus `OrgBoundary`) are done. W3 (sentinel removal across the six files) is done; the sentinel string no longer appears in `apps/web/src`. Remaining: W4 organization settings, W5 runtime picker and switching, W6 kiosk polish, W7 membership administration UI.

## Goal

Make the SPA organization-aware end to end. Staff see their own organization everywhere. Platform admins can select any organization and see platform views. The hardcoded "system tenant" sentinel disappears. No visual redesign; existing pages gain scope awareness.

## Current state

Verified against the tree on 2026-08-26:

1. `GET /api/users/me` returns `organizationId`, `organizationName`, `tenantId`, `isPlatformAdmin`. The SPA ignores all four.
2. `packages/types/index.ts:112` defines `UserResponseDto` without those fields.
3. Six files hardcode the legacy sentinel `'00000000-0000-0000-0000-000000000001'`:
   - `src/hooks/useTenantsViewModel.ts:92` hides the bucket and drives assigned or unassigned monitor filters.
   - `src/hooks/useMonitorQueries.ts:77` fetches unassigned monitors by passing the sentinel as `tenantId`.
   - `src/components/settings/tenants/MonitorTile.tsx:17`.
   - `src/components/settings/tenants/TenantMonitorsPanel.tsx:26`.
   - `src/pages/Settings/MonitorDetail.tsx:205`.
4. With per-org buckets these sentinel checks are wrong twice over: new buckets have random ids so they leak through every filter, and real unassigned data now lives on `GET /api/monitors/unassigned`, which the server scopes by caller.
5. Settings > Configuration talks only to `api/global-config`. Per-organization settings live in `OrganizationConfig`; the service exists but has no HTTP surface yet.
6. There is no endpoint that lists organizations. A platform-admin org selector needs one.
7. Users administration needs no structural change; the server already scopes reads and writes by membership.

## Prerequisites (backend, small)

| ID | Item | Detail |
|---|---|---|
| B1 | `OrganizationConfigController` | GET plus PATCH at `api/organizations/{id}/config` and `api/me/config`. Scopes: staff read their own org; admins write their own org; platform admins address any org. Reuses `IOrganizationConfigService`. Updates reschedule jobs exactly like `GlobalConfigController` does today. |
| B2 | `GET /api/organizations` | Ids and names. Platform admins get every organization; everyone else gets only organizations from their own membership rows. Feeds the picker for both platform and multi-org staff users, and the kiosk header display fallback. |
| B3 | Membership administration endpoints | List, add, and remove `UserAccess` rows per user. Reach rules mirror the rest of the API: org admins manage memberships inside their own organization; platform admins address any organization and may create or remove platform-admin rows (null organization). Removing a platform admin's last platform-admin row is refused. Role changes stay on the existing user PATCH. |

Both ship before step 1 of the frontend sequence because client regeneration consumes them.

## Workstreams

### W1 Client and types regeneration

Regenerate the typed client from `docs/openapi/v1.json` using the existing pipeline after B1 and B2 land. Extend `packages/types/index.ts`:

```ts
export type UserResponseDto = {
  id: string;
  name: string;
  email: string | null;
  role: string;
  organizationId?: string | null;
  organizationName?: string | null;
  tenantId?: string | null;
  isPlatformAdmin?: boolean;
};
```

### W2 Scope foundation

Extend `useCurrentUser.ts`:

- Widen `UserProfile` to match `UserResponseDto`.
- Return a derived `scope` object: `{ isAdmin, isPlatformAdmin, organizationId, organizationName, tenantId }`.
- Kiosk path keeps deriving role from the JWT; kiosk tokens already carry an organization claim and `/api/users/me` resolves it.

Add one guard component next to the existing `RoleBoundary`: `OrgBoundary` renders children only when the current scope matches, so pages stop hand-checking roles.

Files: `useCurrentUser.ts`, `RoleBoundary.tsx` sibling, route-level `__root.tsx` context if needed.

Tests: hook unit tests for OIDC path, kiosk path, missing fields.

### W3 Sentinel removal and per-org unassigned monitors

Delete the constant from all six files. New rules:

- "Unassigned" means `tenantId == null` OR the monitor belongs to this organization's bucket. Buckets never appear in `GET /api/tenants` anymore, so presence in the tenants list becomes the definition of "real".
- Unassigned data comes from `GET /api/monitors/unassigned` through a new `useUnassignedMonitors()` query. Delete the sentinel-parameter hack in `useMonitorQueries.ts:77`.
- Assigned and unassigned filters in `useTenantsViewModel` and `TenantMonitorsPanel` use that query instead of id comparison.
- `MonitorTile` `isUnassigned` takes the monitor's membership in the unassigned result set.
- `MonitorDetail` drops the hardcoded filter from its assignment picker; the API already hides buckets.

Note for later phases, not this one: cross-org platform views of "all unassigned" paginate by org.

Tests: update `SettingsNavigation.test.tsx`, `SettingsListLoading.test.tsx`; add view-model tests proving filters survive the sentinel removal; mock-server test asserting the new query hits `/monitors/unassigned`.

### W4 Organization-aware settings

Split Settings > Configuration into platform-wide and per-org sections:

- Platform section keeps `GlobalConfigDto` fields, visible to platform admins only.
- New Organization section: name display, reporting timezone, weather location and interval, monitoring provider and masked keys, fetch intervals. Wired to B1 endpoints through regenerated hooks (`useGetApiOrganizationsByOrgIdConfig` naming family).
- Staff admins edit their own org without picking anything; the page derives the org from scope.
- Reuse the existing save and dirty-state patterns from `configuration.tsx`; add per-field masking rules matching the global config page.

Files: `pages/Settings/configuration.tsx` plus new `components/settings/organization/*`.

Tests: component tests per section with mocked hooks; verify staff cannot reach platform fields.

### W5 Organization picker and runtime scope switching

There is no picker at login. Users land on their default organization and switch any time from a picker in the settings shell and top bar.

Backend behavior this relies on: scope resolves per request from `X-ADWAIS-ORG-ID`; the server re-validates the header against memberships on every call. Switching is therefore safe at any moment and grants nothing the caller did not already hold.

Requirements:

1. `useOrgSelection` hook: current organization id, persisted to `sessionStorage`. Cross-tab sync is a documented follow-up, not part of this change.
2. `apiFetch` attaches the selected org as `X-ADWAIS-ORG-ID` on every request, mirroring how kiosk and demo tokens are resolved per call today.
3. Scoped list queries key by organization id (`['tenants', orgId]`), so switching cannot render another organization's cached rows and back-switching restores warm caches.
4. Switch flow: set selection, let changed keys remount and refetch. Mutations disable the picker while pending.
5. Revocation recovery: on 403 while a non-default selection is active, re-fetch `/api/users/me`, reset the selection to the first membership, and surface a notice. No redirect loops.
6. Population rules: platform admins see every organization plus a "Platform overview" entry that sends no scope header (deployment totals; financial and fleet pages already support that scope). Multi-org staff see only their own organizations; single-org users never see the picker. Both groups depend on B2 returning membership-scoped results for non-admin callers, so B2 is specified as membership rows for everyone, unrestricted for platform admins.
7. New users provisioned into several organizations default to their first membership until they pick one, matching the documented server default.

Files: new `hooks/useOrgSelection.ts`, settings shell layout, top bar component, affected query hooks gain the org key segment, `apiClient.ts` header injection.

Tests: selection persistence, header propagation through `queryFn`, cache separation between organizations (switch away and back without refetch artifacts), revocation fallback path, guard behavior for single-org users.

### W6 Kiosk polish

Small: show `organizationName` on the kiosk welcome strip when present. Remove any reliance on demo tokens referencing the old sentinel. Files: kiosk layout components, `useKioskAuth.ts` untouched unless types change.

### W7 Membership administration

The only write path to `UserAccess` today is user creation, which mints one membership in the caller's organization. Nothing can add, remove, or inspect memberships afterwards. Without W7, joining a second organization, moving a user between organizations, or promoting a platform admin is impossible.

Extend the users area:

- `UserDetail.tsx` gains a memberships panel listing the user's rows: organization name, role, and a remove action per row, plus an add form (organization picker from B2 within the caller's reach, role select).
- Reach follows B3: org admins see and manage only their own organization's memberships; platform admins see all rows and can add platform-admin memberships.
- The user list page stays as is; membership details load on the detail page on demand.

Files: `pages/Settings/UserDetail.tsx`, new `components/settings/users/MembershipsPanel.tsx`, hooks against B3 routes.

Tests: reach rules per caller type (org admin cannot see other-org rows; platform admin can), add and remove flows, refusal path when removing the last platform-admin row.

## Sequence and effort

| Step | Contents | Depends on | Estimate |
|---|---|---|---|
| 1 | W1 client and type regen plus shared-type widening | B1, B2 | half day |
| 2 | W2 scope foundation and guards | 1 | half day |
| 3 | W3 sentinel removal across six files | 1, 2 | one day |
| 4 | W4 organization settings sections | 1, 2 | one day |
| 5 | W5 platform selector | 1, 2, B2 | half day |
| 6 | W7 membership administration | 1, B3 | half day |
| 7 | W6 kiosk polish | 1, 2 | two hours |

Three to three and a half days sequential. Steps 4, 5, and 6 are independent once step 3 lands.

## Verification

- `npx --package=typescript@7.0.2 tsc -b` and eslint per changed file, vitest per touched area, from `apps/web`.
- Manual smoke against a local database with two organizations, following `docs/smoke-testing.md` scenarios marked frontend.
- Acceptance: no occurrence of the sentinel string remains in `apps/web/src`; staff users see zero cross-org content on every settings page; platform admin toggles organizations without reload bugs.

## Out of scope

Tenant viewer screens (phase four). Visual redesign. Server-side pagination changes. Merging duplicated view-model filter logic beyond what W3 requires.

## Backlog notes

- `SystemEventService` never populates `SystemEvent.OrganizationId`; events are deployment-wide. Attribution is a future backend task if org-filtered event lists become necessary.
- Cross-tab sync of the selected organization (localStorage plus storage events) is deferred; W5 uses sessionStorage.
