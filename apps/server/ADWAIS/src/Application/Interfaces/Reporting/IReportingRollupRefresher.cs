// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Interfaces;

/// <summary>
/// Rebuilds materialized rollups whose calendar-day grouping depends on the
/// configured reporting timezone.
/// </summary>
public interface IReportingRollupRefresher
{
    Task RefreshAsync(CancellationToken ct = default);
}
