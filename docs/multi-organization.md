# Multi-organization model

One deployment serves many organizations. Each organization manages its own tenants (e-commerce stores). A tenant is a data source, not a login. The tenant's staff may later get a restricted viewer login.

Hierarchy:

- Platform admin: the ADWAIS owner. Sees everything.
- Organization: a consulting company. Sees its own tenants, monitors, and intranet.
- Tenant: a store. No logins today.

## Membership

Table `user_access`:

| Column | Type | Notes |
|---|---|---|
| UserId | Guid | |
| OrganizationId | Guid? | Null for platform admins |
| TenantId | Guid? | Null unless the user is a tenant viewer |
| Role | UserRole | Admin, Employee, Viewer, TenantViewer, PlatformAdmin |

The platform shape is structural. A null organization pairs only with `PlatformAdmin`. The `PlatformAdmin` role never pairs with an organization. A database check constraint rejects every other combination, so no write path can bypass the invariant.

A user can hold several rows. The platform role stays separate from org roles. Nobody can remove their own platform-admin membership.

Platform admins may select any organization or operate in the platform scope. Org members may select only orgs they belong to. Tenant viewers are pinned to their tenant.

## Effective scope

`LocalUserClaimsTransformation` resolves the user's allowed scopes from membership, then selects the effective scope for the request:

- The request carries the scope it wants: `X-ADWAIS-ORG-ID` and `X-ADWAIS-TENANT-ID` headers.
- With no org header, a platform admin gets the platform scope: `(null, null, [PlatformAdmin])`.
- With an org header, a platform admin gets that org's admin scope: `(orgId, tenantId, [Admin])`. Platform powers apply only in platform view.
- Single-org users default to their org. Multi-org users default to the first org until the picker selects one.

`AccessScopeResolver.SelectEffective` owns this logic. A request-scoped `ICurrentAccess` service exposes the scope.

## Claims

`AccessClaimsBuilder` emits claims for the effective scope only:

- `org_id` and `tenant_id` for the effective scope.
- Role claims for the roles valid inside that scope.
- `is_platform_admin` with value `true` only when the scope roles contain `PlatformAdmin`.

Upstream identity claims are scrubbed. Roles, name identifiers, and scope claims from the IdP never grant authority. Only local membership does.

## Policies

| Policy | Requirement | Use |
|---|---|---|
| `PlatformAdminOnly` | `is_platform_admin` claim | Platform aggregation, dashboard, global config, view-refresh triggers |
| `AdminOnly` | Roles Admin or PlatformAdmin | Writes: tenants, monitors, config, backfill, user management |
| `StaffAccess` | Roles Admin, Employee, or PlatformAdmin | Reads for staff plus activation flows |
| `KioskOrStaffAccess` | Roles Admin, Employee, Viewer, or PlatformAdmin | Kiosk-readable reads |

401 means unauthenticated. The SPA logs out. 403 means denied. The SPA surfaces it without logging out.

## Organization configuration

Table `organization_config`, one row per organization, holds provider settings, fetch toggles, fetch intervals with column defaults, the reporting timezone, weather location, monitors limits, and sync error state. A missing row falls back to defaults: the bootstrap creates rows for organizations that lack one. Settings pages PATCH per organization.

## Reporting

The eight materialized views carry `organization_id` and compute day boundaries in each organization's reporting timezone. See `background-jobs.md` for refresh ownership.

Kiosk tokens carry an organization claim. The kiosk header shows the organization name from `/api/users/me`.

`/api/users/me` returns the profile with the effective scope: `role`, `organizationId`, `organizationName`, `tenantId`, and `isPlatformAdmin`. The platform flag comes from membership, not scope. Wearing an organization never strips platform status.