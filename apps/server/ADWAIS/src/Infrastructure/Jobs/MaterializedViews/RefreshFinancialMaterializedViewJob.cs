// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.MaterializedViews;

public class RefreshFinancialMaterializedViewJob(
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

        // Refresh materialized views in sequential order to respect data dependencies.
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_financial_daily_tenant_rollup;", ct);
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_financial_daily_global_rollup;", ct);
        }

        // The rebuild incorporated all pending changes; drop the pending marks.
        await viewRefreshTracker.ClearDirtyAsync(ct);
    }
}


