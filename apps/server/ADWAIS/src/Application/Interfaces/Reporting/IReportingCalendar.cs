// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Models;
using Adwais.Domain.Enums;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Provides the configured business-reporting calendar used to turn local
/// calendar boundaries into UTC instants for storage and querying.
/// </summary>
public interface IReportingCalendar
{
    Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken ct = default);

    Task<ResolvedPeriod> ResolvePeriodAsync(
        Timeframe timeframe,
        ComparisonType comparisonType = ComparisonType.Preceding,
        CancellationToken ct = default);

    DateTimeOffset GetStartOfDayUtc(DateTimeOffset instant, TimeZoneInfo timeZone);
}
