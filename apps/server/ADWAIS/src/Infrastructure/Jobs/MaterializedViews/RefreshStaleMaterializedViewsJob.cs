// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.MaterializedViews;

/// <summary>
/// Rebuilds the materialized views only when at least one organization has
/// pending changes. Coalesces all pending marks into a single rebuild.
/// The scheduled daily jobs rebuild unconditionally; this job shortens the
/// stale window in between.
/// </summary>
public class RefreshStaleMaterializedViewsJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IViewRefreshTracker viewRefreshTracker)
{
    public async Task ExecuteAsync()
    {
        if (!await viewRefreshTracker.IsDirtyAsync()) return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await RefreshViewsAsync(dbContext);
        await viewRefreshTracker.ClearDirtyAsync();
    }

    protected virtual async Task RefreshViewsAsync(AnalyticsDbContext dbContext, CancellationToken ct = default)
    {
        // Financial rollups
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_financial_daily_tenant_rollup;", ct);
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_financial_daily_global_rollup;", ct);

        // Latency rollups
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_latency_monitor_rollup;", ct);
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_latency_tenant_rollup;", ct);
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_latency_global_rollup;", ct);

        // Availability rollups
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_availability_monitor_rollup;", ct);
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_availability_tenant_rollup;", ct);
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_availability_global_rollup;", ct);
    }
}