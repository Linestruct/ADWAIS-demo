# Platform-level CRUD plan

What platform administrators can still not do through the API and UI, and what it takes to close each gap. Grounded in the current OpenAPI surface.

## Current surface

| Area | Exists | Missing |
|---|---|---|
| Organizations | `GET /api/organizations` (list), config GET/PATCH per org and per me | Create, rename, deactivate, get by id |
| Users | List, create, update, delete | Choosing the target organization at creation |
| Memberships | List, add, remove per user | None |
| Kiosk devices | Register, activate, 1-hour token | Read-only device list per org, persistent kiosk sessions |
| Tenants | Full CRUD, org-scoped by header | Cross-org platform view |
| Monitors | Full CRUD, assign, unassign, org-scoped by header | Cross-org platform view |
| Global config | Retention only | None (by design) |
| Calendar, bulletins, feeds, events | Full CRUD, org-scoped | None |

Platform admins reach any single org through the header picker. What is missing is managing the organizations themselves, keeping kiosk displays logged in plus seeing them per org, and platform-wide views.

## P1 Organizations CRUD

Backend, new endpoints on `OrganizationsController`:

- `POST /api/organizations` with `{ name }`. Platform admins only. Creates the organization row. Config is created lazily on first edit, so no seeding needed.
- `GET /api/organizations/{id}`. Platform admins get any org. Staff get their own org only, else 404.
- `PATCH /api/organizations/{id}` with `{ name }`. Platform admins any org. Org admins rename their own org only.
- `DELETE /api/organizations/{id}`. Platform admins only.

Delete semantics are the main design decision, listed under decisions. The safe default is a soft deactivate flag on `Organization` with a purge job for history. A hard delete must cascade through memberships, config, tenants, monitors, order history, latency and uptime samples, calendar rows, bulletin posts, feed sources, and kiosk devices.

Frontend, new Settings > Organizations page visible to platform admins:

- List all organizations with monitor and member counts.
- Create dialog, inline rename, deactivate action with confirmation.
- Reuses the org list query and the picker data.

Tests: controller reach rules per caller type, create and rename flows, delete cascade or deactivate behavior, web component tests with mocked hooks.

## P2 Kiosk sessions and device visibility

Kiosk auth is an alternative to OAuth for displays, not a managed device fleet. There is no device administration: no deactivate, no transfer, no per-device settings. The actual requirements:

1. Easy first auth. This works today: the display registers itself, shows an activation code (valid 10 minutes), staff activates it from Settings > Authentication, the device row binds to the staff member's org, and the display fetches a 1-hour token. No changes planned. `CreatedDate` is set once at insert; `AuthorizedAt` is set on activation and cleared on every re-register, which also deauthorizes the row.
2. Stay logged in while active. The display refreshes its token every 45 minutes while running and attempts one silent refresh before giving up a session, so an authorized display stays logged in indefinitely with no staff action. An unauthorized or unknown device still lands on the activation screen, which already picks up the token automatically once staff activates.
3. See active devices per org, and revoke the odd one. Nice to have plus one safety valve. Staff see their own org's authorized devices; platform admins see all. `LastSeenAt` is stamped on every token mint, so the list tells live displays apart from dead ones. `DELETE /api/kiosk/devices/{deviceId}` removes the row (staff own org, platform any org); the display drops to the activation screen at its next refresh, at most an hour later. No transfer, no per-device settings.
4. Dead fingerprints clean themselves up. A new local id orphans the old row, so register purges rows that never authorized and whose code expired over a week ago. Authorized rows are never purged: they are what lets a returning display skip staff.

Backend:

- `GET /api/kiosk/devices`. Returns device id, org id, authorized and last-seen dates, created date. Platform admins see all rows. Staff see their own org's rows. Org-less rows are platform-only.
- `DELETE /api/kiosk/devices/{deviceId}` with the same reach rules. Returns 204, or 404 for missing and foreign rows.

Frontend:

- Refresh the kiosk token on a 45-minute timer while a kiosk session exists, plus one silent retry before session invalidation. An authorized display then stays logged in indefinitely with no staff action.
- Show the device list where staff already manage people: a read-only section on the users page (or the Authentication page if it fits better at implementation time), with a delete action per row.

Known limitation, explicitly out of scope: kiosk tokens are stateless JWTs, so a stolen display stays valid until its hour runs out. If that ever matters, the lever is a validation-time device check, not more UI.

Tests: device list scoping per caller type, delete reach rules, token TTL at one hour, last-seen stamping, purge keeps authorized and fresh pending rows, refresh timing (no staff action across a simulated expiry), unauthorized device still gates on activation, web tests for silent refresh success and fallthrough, web component test for the read-only list.

## P3 User creation org targeting

Current `POST /api/users` mints the membership in the caller's organization. A platform admin must create the user and then add memberships through the membership API. Two changes:

- Backend: optional `organizationId` on `CreateUserRequestDto`. Staff keep the caller-org behavior. Platform admins may pass any org or no org (platform-only user).
- Frontend: optional organization select in the provision dialog, visible to platform admins.

Tests: create with explicit org, create without org for platform scope, staff cannot pass an org outside reach.

## P4 Cross-org platform views

Platform overview of tenants, monitors, and unassigned monitors across every organization. Requires server-side pagination by org plus an organization column in the responses. The frontend plan already notes this as a later phase. Not part of this CRUD batch.

## Decisions needed before P1 implementation

- Hard delete versus deactivate flag for organizations.
- Retention of order and sample history when an organization leaves.
- Bucket tenant behavior on deactivation: keep, archive, or remove.
- Whether rename should update the picker label through invalidation only, or via a dedicated event.

## Sequence and effort

| Step | Contents | Estimate |
|---|---|---|
| 1 | P1 backend: org create, get, rename, deactivate | half day |
| 2 | P1 frontend: Organizations page | half day |
| 3 | P2 backend and web: device list, token refresh, read-only list | half day |
| 4 | P3 backend and web: user creation org targeting | two hours |
| 5 | Codegen regeneration and smoke test after each backend step | recurring |

Steps 3 and 4 are independent of step 1.