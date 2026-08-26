# Authentication

ADWAIS uses OpenID Connect (OIDC) for browser users and a signed JWT for kiosk displays. The backend validates both with ASP.NET Core authentication schemes. It has no MSAL or Microsoft Graph dependency.

## Browser sign-in

The web app uses `oidc-client-ts` and `react-oidc-context`. API requests carry the access token as a bearer token. The backend resolves the user by the `sub` claim and updates the local user record through `ExternalSubjectId`.

Frontend settings: `VITE_OIDC_AUTHORITY` and `VITE_OIDC_CLIENT_ID` are required. `VITE_OIDC_SCOPE` defaults to `openid profile email`.

## Demo access

| Route | Auth | Behavior |
| --- | --- | --- |
| `GET /api/demo/token` | Anonymous | Returns a Viewer kiosk token when `Authentication:EnableDemoAccess=true`. Returns `404` otherwise. |

Demo tokens are read-only. They pass `KioskOrStaffAccess`. Write operations require `StaffAccess` or `AdminOnly` and return `403`.

## Kiosk access

| Route | Auth | Behavior |
| --- | --- | --- |
| `POST /api/kiosk/register` | Anonymous | Registers a display. Returns a temporary activation code. |
| `POST /api/kiosk/activate` | `StaffAccess` | Authorizes a display with its activation code. |
| `GET /api/kiosk/token?deviceId=...` | Kiosk flow | Returns the display's 30-day kiosk JWT. |
| `POST /api/kiosk/swagger-admin-token` | Anonymous, Development only | Returns a development Admin kiosk token when the secret matches. |

Kiosk tokens use the configured kiosk issuer and carry their own role claims. They do not use database user provisioning.

## Current user

| Route | Auth | Behavior |
| --- | --- | --- |
| `GET /api/users/me` | `KioskOrStaffAccess` | Returns the local OIDC user for `sub`, or a transient kiosk user. The payload carries the effective scope: `organizationId`, `organizationName`, `tenantId`, and `isPlatformAdmin`. |

## Request scope

Staff and platform principals carry an effective scope resolved from membership (`UserAccess`). A request selects a scope with `X-ADWAIS-ORG-ID` and `X-ADWAIS-TENANT-ID`. Platform admins may select any organization; org members are limited to their own; tenant viewers are pinned. All data reads apply this scope through `OrganizationFilter` and `TenantVisibility`.

Upstream identity claims are scrubbed. Roles, name identifiers, and any scope claims from the IdP never grant authority; only local membership does.

## Development mock

In Development, requests without an `Authorization` header authenticate as platform admin through the `DevMock` scheme. Setting `DEV_MOCK_ORG_ID` scopes that principal to one organization instead. The scheme is not registered outside Development.

## Hangfire dashboard

| Route | Auth | Behavior |
| --- | --- | --- |
| `POST /api/dashboard-session` | `PlatformAdminOnly` bearer token | Creates a five-minute HttpOnly `adwais_dashboard` cookie for Hangfire. |
| `DELETE /api/dashboard-session` | Anonymous | Clears the dashboard cookie. Call during sign-out. |
| `GET /hangfire` | Dashboard cookie with the platform admin claim | Opens the Hangfire UI. |

The dashboard session endpoint bridges SPA bearer auth and the server-rendered Hangfire UI. Browser navigation does not carry the `Authorization` header, so Hangfire needs a short-lived cookie to populate `HttpContext.User`. The cookie is `Secure` outside development and follows the request scheme in development. Organization admins cannot mint a session or pass the dashboard filter.

## Authorization policies

| Policy | Roles | Use |
| --- | --- | --- |
| `KioskOrStaffAccess` | `Admin`, `Employee`, `Viewer` | Read-only dashboard and kiosk data, scoped by the request scope. |
| `StaffAccess` | `Admin`, `Employee` | Staff operations such as kiosk activation. |
| `AdminOnly` | `Admin`, inside the caller's scope | Admin mutations such as tenants, monitors, and users. |
| `PlatformAdminOnly` | Platform admin only | Deployment-wide surfaces such as Hangfire. |
