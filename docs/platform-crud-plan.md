# Platform-level CRUD plan

What platform administrators can still not do through the API and UI, and what it takes to close each gap. Grounded in the current OpenAPI surface.

## Current surface

| Area | Exists | Missing |
|---|---|---|
| Organizations | `GET /api/organizations` (list), config GET/PATCH per org and per me | Create, rename, deactivate, get by id |
| Users | List, create, update, delete | Choosing the target organization at creation |
| Memberships | List, add, remove per user | None |
| Kiosk devices | Register, activate, token, dev token | List devices, deauthorize, transfer between orgs |
| Tenants | Full CRUD, org-scoped by header | Cross-org platform view |
| Monitors | Full CRUD, assign, unassign, org-scoped by header | Cross-org platform view |
| Global config | Retention only | None (by design) |
| Calendar, bulletins, feeds, events | Full CRUD, org-scoped | None |

Platform admins reach any single org through the header picker. What is missing is managing the organizations themselves, administering devices, and platform-wide views.

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

## P2 Kiosk device administration

Backend:

- `GET /api/kiosk/devices`. Platform admins see all devices. Staff see their own org's devices.
- `DELETE /api/kiosk/devices/{deviceId}`. Deauthorizes and removes a device row. Platform admins any org. Staff their own org.
- Optional `PATCH /api/kiosk/devices/{deviceId}` to transfer a device between organizations. Platform admins only.

Frontend:

- Extend Settings > Authentication with a device list: name, status, binding org, activated date, deactivate action.
- The kiosk landing already recovers from a revoked token by re-registering, so deactivation takes effect on the display within its token lifetime.

Tests: device list scoping per caller, deactivate flow, web component tests.

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
| 3 | P2 backend: device list, deactivate, transfer | half day |
| 4 | P2 frontend: device admin in Authentication | half day |
| 5 | P3 backend and web: user creation org targeting | two hours |
| 6 | Codegen regeneration and smoke test after each backend step | recurring |

Steps 3 and 5 are independent of step 1. Step 4 depends on step 3.