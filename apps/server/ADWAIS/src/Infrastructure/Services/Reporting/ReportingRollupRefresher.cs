// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Jobs.MaterializedViews;

namespace Adwais.Infrastructure.Services;

public sealed class ReportingRollupRefresher(
    RefreshFinancialMaterializedViewJob financialJob,
    RefreshMonitoringMaterializedViewJob monitoringJob) : IReportingRollupRefresher
{
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await financialJob.RefreshAsync(ct);
        await monitoringJob.RefreshAsync(ct);
    }
}
