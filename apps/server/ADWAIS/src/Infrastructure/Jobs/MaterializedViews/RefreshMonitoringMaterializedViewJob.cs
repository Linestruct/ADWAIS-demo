// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.MaterializedViews;

public class RefreshMonitoringMaterializedViewJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IViewRefreshTracker viewRefreshTracker)
{
    public async Task ExecuteAsync()
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        if (dbContext.Database.IsRelational())
        {
            // 1. Latency Views
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_latency_monitor_rollup;", ct);
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_latency_tenant_rollup;", ct);
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_latency_global_rollup;", ct);

            // 2. Availability Views
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_availability_monitor_rollup;", ct);
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_availability_tenant_rollup;", ct);
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_daily_availability_global_rollup;", ct);
        }

        // The rebuild incorporated all pending changes; drop the pending marks.
        await viewRefreshTracker.ClearDirtyAsync(ct);
    }
}


