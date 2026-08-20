// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Interfaces;

/// <summary>
/// Rebuilds materialized rollups whose calendar-day grouping depends on the
/// configured reporting timezone.
/// </summary>
public interface IReportingRollupRefresher
{
    Task RefreshAsync(CancellationToken ct = default);
}
