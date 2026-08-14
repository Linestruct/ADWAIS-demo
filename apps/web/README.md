# Web app

The frontend dashboard. Built with React 19, TypeScript, Vite, Tailwind CSS v3, TanStack Router, TanStack Query, Chart.js, and Zod.

## Environment

Copy `apps/web/.env-example` to `apps/web/.env.local` and edit it.

```env
VITE_OIDC_AUTHORITY=https://your-idp.example.com
VITE_OIDC_CLIENT_ID=adwais-frontend
VITE_OIDC_SCOPE=openid profile email
VITE_DEMO_MODE=false
VITE_SSO_BUTTON_LABEL=Sign in with SSO
VITE_SSO_BUTTON_LOGO_URL=
```

`VITE_OIDC_AUTHORITY` and `VITE_OIDC_CLIENT_ID` are required unless `VITE_DEMO_MODE=true`. Demo mode adds a login option that requests a Viewer token from `/api/demo/token`.

The Vite project is scoped to this directory. Vite fails early when an env file has a UTF-8 BOM.

## Code generation

Frontend API code is generated. Do not edit it by hand.

| Output | Contents |
|---|---|
| `packages/types/generated/` | TypeScript interfaces for API DTOs |
| `src/api/generated/endpoints.ts` | React Query hooks |

Both are committed. Re-run codegen when the API changes.

Pipeline: `dotnet build` writes `docs/openapi/v1.json`, then `pnpm codegen` generates the files.

```bash
pnpm --filter web codegen
```

## Local development

Run from the repository root:

- Start: `pnpm dev:web`
- Codegen: `pnpm --filter web codegen`
- Build: `pnpm --filter web build`
- Lint: `pnpm --filter web lint`
