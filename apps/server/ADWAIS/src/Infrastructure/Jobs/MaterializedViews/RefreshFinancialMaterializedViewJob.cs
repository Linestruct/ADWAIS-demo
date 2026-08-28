// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.MaterializedViews;

public class RefreshFinancialMaterializedViewJob(IDbContextFactory<AnalyticsDbContext> dbContextFactory)
{
    public async Task ExecuteAsync()
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Refresh materialized views in sequential order to respect data dependencies.
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_financial_daily_tenant_rollup;", ct);
        await dbContext.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY v_mat_financial_daily_global_rollup;", ct);
    }
}


