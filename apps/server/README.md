# Server

This directory holds the ADWAIS backend solution and the local PostgreSQL setup.

## PostgreSQL

The API needs a running PostgreSQL. The local container is defined in [docker-compose.yml](/apps/server/docker-compose.yml).

- Engine: PostgreSQL 15 (Alpine)
- Host: `localhost:5432`
- User: `postgres`
- Password: `development_password`
- Database: `analyticsdb`

Commands:

```bash
pnpm db:up            # start
docker compose down   # stop, keep data
docker compose down -v # stop, delete data
```

Data persists in the `postgres_data` volume.
