# Applications

This directory holds the applications in the workspace.

## apps/web

The frontend dashboard.

- Stack: React 19, TypeScript, Vite, Tailwind CSS v3, TanStack Router, TanStack Query, Chart.js.
- Purpose: daily KPIs, financial graphs, monitor statistics, office events.

## apps/server

The backend runtime.

- Stack: .NET 10 Web API, Entity Framework Core, PostgreSQL, Hangfire.
- Purpose: data ingestion pipelines and client endpoints.
- Source: `/apps/server/ADWAIS`

## Workspace rules

- Do not run npm or pnpm inside these directories. Use `pnpm` from the root.
- Run tasks for one app with `pnpm --filter web <command>`.
