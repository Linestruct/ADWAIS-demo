// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using Adwais.Api.Extensions;
using Adwais.Domain.Entities;
using Xunit;

namespace Adwais.Tests.Extensions;

public class SchedulingIntervalResolverTests
{
    [Fact]
    public void Resolve_MissingConfig_ReturnsDeploymentDefaults()
    {
        var intervals = SchedulingIntervalResolver.Resolve(null);

        Assert.Equal(60, intervals.UptimeMinutes);
        Assert.Equal(10, intervals.LatencyMinutes);
        Assert.Equal(10, intervals.OrderFetchMinutes);
        Assert.Equal(60, intervals.UserStatsMinutes);
        Assert.Equal(2, intervals.FeedHours);
    }

    [Fact]
    public void Resolve_ConfiguredValues_PassThrough()
    {
        var config = new OrganizationConfig
        {
            OrganizationId = Guid.NewGuid(),
            UptimeFetchIntervalMinutes = 30,
            LatencyFetchIntervalMinutes = 5,
            OrderFetchIntervalMinutes = 120,
            UserStatsFetchIntervalMinutes = 90,
            FeedFetchIntervalHours = 6
        };

        var intervals = SchedulingIntervalResolver.Resolve(config);

        Assert.Equal((30, 5, 120, 90, 6), intervals);
    }

    [Fact]
    public void Resolve_ZeroIntervals_AreClampedToOneMinute()
    {
        var config = new OrganizationConfig
        {
            OrganizationId = Guid.NewGuid(),
            OrderFetchIntervalMinutes = 0,
            FeedFetchIntervalHours = 0
        };

        var intervals = SchedulingIntervalResolver.Resolve(config);

        Assert.Equal(1, intervals.OrderFetchMinutes);
        Assert.Equal(1, intervals.FeedHours);
    }
}
