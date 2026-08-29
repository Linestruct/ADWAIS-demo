// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using Adwais.Application.Common.Jobs;
using Xunit;

namespace Adwais.Tests.Jobs;

public class RecurringJobIdTests
{
    private readonly Guid _orgId = Guid.NewGuid();

    [Fact]
    public void For_BuildsCanonicalOrganizationScopedId()
    {
        Assert.Equal($"dispatch-order-fetch-{_orgId}", RecurringJobId.For(RecurringJobKind.OrderFetch, _orgId));
    }

    [Fact]
    public void For_RejectsPlatformWideKinds()
    {
        Assert.Throws<ArgumentException>(() => RecurringJobId.For(RecurringJobKind.SystemEventCleanup, _orgId));
    }

    [Fact]
    public void Platform_ReturnsBaseId()
    {
        Assert.Equal("system-event-cleanup", RecurringJobId.Platform(RecurringJobKind.SystemEventCleanup));
    }

    [Fact]
    public void Platform_RejectsOrganizationScopedKinds()
    {
        Assert.Throws<ArgumentException>(() => RecurringJobId.Platform(RecurringJobKind.OrderFetch));
    }

    [Fact]
    public void TryParse_OrganizationScopedId_ReturnsKindAndOrg()
    {
        var id = RecurringJobId.For(RecurringJobKind.OrderFetch, _orgId);

        Assert.True(RecurringJobId.TryParse(id, out var kind, out var orgId));
        Assert.Equal(RecurringJobKind.OrderFetch, kind);
        Assert.Equal(_orgId, orgId);
    }

    [Fact]
    public void TryParse_PlatformId_ReturnsKindAndNoOrg()
    {
        Assert.True(RecurringJobId.TryParse("system-event-cleanup", out var kind, out var orgId));
        Assert.Equal(RecurringJobKind.SystemEventCleanup, kind);
        Assert.Null(orgId);
    }

    [Fact]
    public void TryParse_PlatformIdWithOrgSuffix_Fails()
    {
        Assert.False(RecurringJobId.TryParse($"system-event-cleanup-{_orgId}", out _, out _));
    }

    [Fact]
    public void TryParse_UnknownId_Fails()
    {
        Assert.False(RecurringJobId.TryParse("some-unknown-job", out _, out _));
    }

    [Fact]
    public void IsOrganizationScoped_ClassifiesKinds()
    {
        Assert.True(RecurringJobId.IsOrganizationScoped(RecurringJobKind.FleetSync));
        Assert.False(RecurringJobId.IsOrganizationScoped(RecurringJobKind.CalendarSync));
    }

    [Fact]
    public void DisplayName_OrganizationJob_AppendsOrgName()
    {
        var id = RecurringJobId.For(RecurringJobKind.OrderFetch, _orgId);

        Assert.Equal($"Order Fetch (Default Organization)", RecurringJobId.DisplayName(id, "Default Organization"));
    }

    [Fact]
    public void DisplayName_OrganizationJob_WithoutOrgName_UsesLabelOnly()
    {
        var id = RecurringJobId.For(RecurringJobKind.LatencyFetch, _orgId);

        Assert.Equal("Latency Fetch", RecurringJobId.DisplayName(id));
    }

    [Fact]
    public void DisplayName_PlatformJob_UsesCuratedLabel()
    {
        Assert.Equal("Financial View Refresh", RecurringJobId.DisplayName("refresh-financial-materialized-views"));
    }

    [Fact]
    public void DisplayName_UnknownId_FallsBackToRawId()
    {
        Assert.Equal("some-unknown-job", RecurringJobId.DisplayName("some-unknown-job"));
    }
}