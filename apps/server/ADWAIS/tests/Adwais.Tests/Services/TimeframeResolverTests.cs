// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Services;
using Adwais.Domain.Enums;
using Xunit;

namespace Adwais.Tests.Services;

public class TimeframeResolverTests
{
    [Fact]
    public void Resolve_FixedPeriod_UsesReportingTimeZoneMidnight()
    {
        var stockholm = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        var now = new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

        var period = TimeframeResolver.Resolve(
            Timeframe.T30,
            ComparisonType.Preceding,
            stockholm,
            now);

        Assert.Equal(
            new DateTimeOffset(2026, 6, 19, 22, 0, 0, TimeSpan.Zero),
            period.CurrentStart);
        Assert.Equal(now, period.CurrentEnd);
    }

    [Theory]
    [InlineData(Timeframe.Today, 1, 48)]
    [InlineData(Timeframe.T7, 7, 42)]
    public void Resolve_RollingPeriod_CoversExactElapsedWindow(Timeframe timeframe, int days, int steps)
    {
        var now = new DateTimeOffset(2026, 7, 23, 1, 52, 45, TimeSpan.Zero);

        var period = TimeframeResolver.Resolve(timeframe, ComparisonType.Preceding, now: now);

        Assert.Equal(now.AddDays(-days), period.CurrentStart);
        Assert.Equal(now, period.CurrentEnd);
        Assert.Equal(now.AddDays(-(days * 2)), period.PreviousStart);
        Assert.Equal(now.AddDays(-days), period.PreviousEnd);
        Assert.Equal(steps, period.StepsInPeriod);
    }
}
