# Multi-organization frontend

The SPA is organization-aware end to end. Staff see their own organization everywhere. Platform admins select any organization or work in platform view. No request carries authority by itself; the server re-validates the scope header against memberships on every call.

## Scope

`useCurrentUser` reads `/api/users/me` and returns the profile plus a derived `scope` object: `{ isAdmin, isPlatformAdmin, organizationId, organizationName, tenantId }`. The kiosk path derives the role from the JWT and resolves the organization name through `/me`.

Guards:

- `RoleBoundary` renders children by role precedence. `PlatformAdmin` outranks org roles.
- `OrgBoundary` renders children only when the current scope matches, so pages stop hand-checking roles.

## Selection and headers

`utils/orgSelection.ts` owns the selection as an external store. It persists to `sessionStorage`. Cross-tab sync is deferred.

`apiClient.ts` attaches the stored id as `X-ADWAIS-ORG-ID` on every request except:

- Calls that set the header themselves.
- `/api/organizations` requests (the picker must list orgs without filtering by one).
- Kiosk sessions (the organization comes from the token claim).

Single-org users never see the picker. Multi-org staff see only their organizations. Platform admins see every organization plus a "Platform overview" entry that sends no scope header.

On 403 with a stored selection, the client revalidates against `/api/users/me` and resets to the first membership with a notice. On 401 it logs out.

## Queries

List queries key by organization id (`['tenants', orgId]` and the same pattern for monitors, financial, fleet, calendar, and users queries). Switching organizations refetches. Switching back restores warm caches. No page renders another organization's cached rows.

"Unassigned" monitors mean `tenantId == null` or membership in the org's unassigned bucket, read from `GET /api/monitors/unassigned`. The legacy system-tenant sentinel is gone from the SPA.

## Pages

- Settings > Configuration shows the organization form. Staff edit their own org without picking anything. Platform admins address the org selected in the picker. The page carries no deployment-wide fields.
- Settings > Platform renders only in platform view (`isPlatformAdmin` with no org selected). One panel holds the embedded global configuration, the visible scheduled jobs toggles, and the Hangfire dashboard button. See `background-jobs.md` for the job model.
- Settings > Jobs shows manual triggers by access level. Platform rows render only in platform view. The scheduled table and recent jobs list reflect the current scope.
- Settings > Events shows the pipeline health panel only in platform view. The event list itself is deployment-wide; the backend does not attribute events to organizations yet.
- User detail carries a memberships panel: rows with organization, role, and remove actions, plus an add form. Org admins manage only their own organization. Platform admins see all rows and manage platform-admin rows. Removing the last platform-admin row is refused with a toast.
- Kiosk layouts show the organization name from `/me` in the header.
