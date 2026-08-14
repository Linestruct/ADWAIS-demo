# ADWAIS backend

The analytics and monitoring server. It aggregates data from external sources (UptimeRobot, Litium, Shopify) and serves the frontend.

## Structure

Clean architecture in four projects:

- `Api` - controllers, auth, validators, filters.
- `Application` - business logic, DTOs, validation, interfaces.
- `Domain` - entities and enums. No outer dependencies.
- `Infrastructure` - EF Core, migrations, jobs, HTTP clients, caching.

Domain boundaries parse values early: `TenantId`, `ResolvedPeriod`, and `OrderState` are typed at the API edge.

EF Core conventions: schema configuration lives in `AnalyticsDbContext.cs` with the Fluent API. Do not use data annotations on entities.

## Local setup

Prerequisites: .NET 10 SDK and a running PostgreSQL (`localhost:5432`, database `analyticsdb`, user `postgres`, password `development_password`).

The tracked `src/Api/appsettings.Development.json` has the dev defaults. An ignored `src/.env` file is optional for overrides.

```bash
cp src/.env.example src/.env
```

| Variable | What it is |
|---|---|
| `Authentication__OidcAuthority` | OIDC authority URL |
| `Authentication__OidcAudience` | API audience in access tokens |
| `Authentication__EnableDemoAccess` | Set `true` to expose the demo token endpoint |

`EnableRuntimeDataSeeding` creates demo data at startup and registers a seeder job. Set `RESEED=true` for one startup to replace demo data. See `src/.env.example`.

## Commands

Run from the repository root:

| Command | What it does |
|---|---|
| `pnpm dev:api` | Start the API |
| `pnpm dev:api:watch` | Start the API with hot reload |
| `pnpm migration:add <Name>` | Create a migration (stop `dev:api` first) |
| `pnpm migration:update` | Apply migrations |
| `pnpm migration:remove` | Remove the last unapplied migration |
| `pnpm migration:list` | List migrations |

After API changes, regenerate frontend types:

```bash
pnpm --filter web codegen
```

Auth flows: [docs/authentication.md](../../../docs/authentication.md).
