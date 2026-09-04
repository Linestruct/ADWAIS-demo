# Codebase backlog

Items deliberately deferred from recent work. Each entry has the context, the work, and the verification steps. Pick any item independently; none block another.

## 1. Model convention for enum string storage

Context: `DbEnum.StoreAsString` in `AnalyticsDbContext` still needs one line per enum property. Six call sites exist today. A future enum property can silently fall back to EF's integer storage if nobody adds the line.

Work: add an EF Core model convention (an `IPropertyAddedConvention`) that applies `HasConversion<string>()` to every enum-typed property automatically. Remove the six explicit `StoreAsString()` lines. Keep per-table check constraints explicit through `DbEnum.CheckConstraintSql`.

Caveat: a convention applies silently app-wide. Today every enum property is already converted, so the convention changes nothing in the current model. It future-proofs additions. If an unconverted enum exists when the convention lands, the scaffolded migration will surface it; review that diff deliberately.

Verify: `dotnet ef migrations has-pending-model-changes` reports no change before and after the convention. Full backend suite green.

## 2. Check constraints for the remaining string-stored enums

Context: `user_access.role` and `calendar_event.event_type` plus `calendar_event.recurrence` are constrained at the database level through `DbEnum.CheckConstraintSql`. Three enum columns still accept any string: `tenant.type` (TenantType), `orders.order_state` (OrderState), `system_event.level` (SystemEventLevel).

Work: add a `HasCheckConstraint` per table in `AnalyticsDbContext` using `DbEnum.CheckConstraintSql<TEnum>(column)`. One migration scaffolds all three. Existing data already holds enum names, so the constraint applies cleanly.

Verify: `pnpm migration:update` applies without violations. Insert a garbage value into one column with direct SQL and confirm the database rejects it.

## 3. Anyone with a valid oicd login can "log in" to the frontend
This is technically correct since the backend won't let the user access any data since theyre not provisioned but the frontend should provide some kind of feedback for this, but how?