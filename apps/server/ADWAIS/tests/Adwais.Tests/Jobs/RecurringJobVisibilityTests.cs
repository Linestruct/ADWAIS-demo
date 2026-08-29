// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using Adwais.Api.Jobs;
using Xunit;

namespace Adwais.Tests.Jobs;

public class RecurringJobVisibilityTests
{
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    [Fact]
    public void OrganizationJob_IsVisibleToItsOrg()
    {
        Assert.True(RecurringJobVisibility.VisibleToOrganizationScope($"dispatch-order-fetch-{_orgA}", _orgA));
    }

    [Fact]
    public void OrganizationJob_IsHiddenFromOtherOrgs()
    {
        Assert.False(RecurringJobVisibility.VisibleToOrganizationScope($"dispatch-order-fetch-{_orgA}", _orgB));
    }

    [Theory]
    [InlineData("refresh-financial-materialized-views")]
    [InlineData("refresh-monitoring-materialized-views")]
    [InlineData("refresh-stale-materialized-views")]
    [InlineData("system-event-cleanup")]
    [InlineData("sync-intranet-calendars")]
    public void CuratedPlatformJobs_AreVisible(string jobId)
    {
        Assert.True(RecurringJobVisibility.VisibleToOrganizationScope(jobId, _orgA));
    }

    [Theory]
    [InlineData("dev-runtime-data-seeder")]
    [InlineData("aggregate-intranet-feeds")]
    [InlineData("dispatch-order-fetch")]
    public void NonCuratedPlatformJobs_AreHidden(string jobId)
    {
        Assert.False(RecurringJobVisibility.VisibleToOrganizationScope(jobId, _orgA));
    }
}