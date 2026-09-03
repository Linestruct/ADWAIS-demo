# Financial service refactor plan

Status: proposed. Not started.

## Goal

Split `src/Application/Services/FinancialService.cs` into focused units without changing any behavior. Reduce per-file complexity so future changes stay reviewable and testable.

Evidence today: 1,104 lines, the two merge pipelines still exist as near twins (`GetMergedTenantDataAsync`, `GetMergedGlobalDataAsync`), scoping resolves once through `OrganizationFilter` plus `TenantVisibility` but consumers still re-apply visibility checks per call site, nine `SystemTenantGuid` references remain, and the pure statistics helpers still sit inside the service class with no direct tests. Complexity and coverage numbers below date from the 2026-08-26 litmus scan; re-scan before starting.
Pure, data-oriented functions are preferred over methods.

## Non-goals

- No endpoint, DTO, route, or OpenAPI contract changes.
- No aggregation or rounding behavior changes. Numbers must be identical.
- No performance tuning. Structure only.

## Problems

1. Three domains share one class:
   - KPI style: `GetKpisAsync`, `GetOrdersAsync`.
   - Time series: `GetAccumulatedRevenueAsync`, `GetNetGrowthAdditionAsync`, `GetCumulativeGrowthDeltaAsync`, `GetRevenueEfficiencyAsync`.
   - Distributions: `GetCrossSegmentDistributionAsync`, `GetPortfolioImpactAsync`, `GetOrderDistributionAsync`, `GetTransactionDensityAsync`.
2. The merge pipeline exists twice. `GetMergedTenantDataAsync` (line 53) and `GetMergedGlobalDataAsync` (line 170) are near twins. Each forks on `isHourly`, splices live rows onto materialized rollups, and applies tenant filters separately.
3. Scoping is sprinkled. Visibility resolves once (`GetVisibleTenantIdsAsync`) but every consumer re-applies `visibleTenantIds.Contains(...)` in its own phrasing. Some order filters still use the legacy `SystemTenantGuid` sentinel while tenant-set queries moved to `!t.IsSystem`.
4. Pure statistics helpers (`CalculatePercentileRank`, quartiles, median, adaptive bin count, growth percentage) sit inside the service class with no direct tests.

## Target shape

| Unit | Contents | Location |
|---|---|---|
| `FinancialMath` | Percentiles, quartiles, median, adaptive bins, growth percentage. Static, pure. | Application |
| `TenantSeriesFilter` | Record built once per call: visible tenant ids, explicit tenant id, tenant type set, system exclusion. Exposes ready-made predicates. | Application |
| `FinancialSeriesReader` | Single merged series pipeline (history plus fresh rows, hourly or daily, tenant or global path). Used by all series consumers. | Infrastructure |
| `FinancialKpiService` | Kpis, Orders. | Infrastructure |
| `FinancialSeriesService` | Accumulated revenue, revenue efficiency, net growth addition, cumulative growth delta. | Infrastructure |
| `FinancialDistributionService` | Cross segment distribution, portfolio impact, order distribution, transaction density. | Infrastructure |

Interfaces split accordingly (`IFinancialKpiService`, `IFinancialSeriesService`, `IFinancialDistributionService`). Controllers inject what they need. This changes composition only; generated clients keep their shapes.

## Steps

Each step compiles, keeps the full suite green, and is its own commit.

1. Characterization first. Pin current outputs for every endpoint against one known fixture set: mixed tenant types, a cancelled order, hourly and daily periods, rollup history plus same-day live rows, scoped and platform callers. These tests may not change during the refactor.
2. Extract `FinancialMath`. Move the statics, add table tests per formula.
3. Introduce `TenantSeriesFilter`. Replace the scattered visibility contains-checks mechanically, one method per commit if needed.
4. Collapse the merge twins into `FinancialSeriesReader`. Golden-compare outputs from the characterization tests after each move.
5. Split the class into the three services. Rewire DI and controllers. Confirm `docs/openapi/v1.json` regenerates with no diff.
6. Unify remaining sentinel usage. Prefer `!IsSystem`; keep any legacy id comparison only with a comment stating why.
7. Run `dotnet-litmus scan --baseline apps/server/ADWAIS/litmus-test-baseline.json` and report the delta. Target: complexity under 60 per resulting file, coverage at or above current levels, branch files risk table without High entries.

## Risks

| Risk | Mitigation |
|---|---|
| Numeric drift in percentile or quartile paths | Step 2 moves formulas with table tests before anything calls them differently |
| A missed scoping path leaks cross-org data | Step 3 is mechanical replacement; boundary tests from the current suite must stay green untouched |
| DI or controller wiring breaks late | Interface split surfaces at compile time; no runtime surprise |
| Refactor drags and blocks feature work | Steps are independent commits; abort after any step still leaves the repo better and green |

## Effort

Steps 1 and 2: half a day. Steps 3 and 4: half a day. Step 5: two hours. Steps 6 and 7: one hour. One day total, sequential.
