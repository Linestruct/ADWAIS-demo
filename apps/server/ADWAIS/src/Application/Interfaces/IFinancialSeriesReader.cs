// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Services;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Merged series pipeline for the financial endpoints. It splices live
/// order rows onto historical rollups, hourly or daily, on the tenant
/// or the global path.
/// </summary>
public interface IFinancialSeriesReader
{
    Task<IReadOnlyList<FinancialSeriesRow>> ReadTenantSeriesAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        bool isHourly,
        TenantSeriesFilter filter,
        CancellationToken ct = default);

    Task<IReadOnlyList<FinancialSeriesRow>> ReadGlobalSeriesAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        bool isHourly,
        Guid[]? visibleTenantIds = null,
        CancellationToken ct = default);
}
