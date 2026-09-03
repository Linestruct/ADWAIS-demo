// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using Adwais.Application.Common.Jobs;
using Xunit;

namespace Adwais.Tests.Jobs;

public class RecurringJobVisibilityTests
{
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    private static HashSet<RecurringJobKind> Curated(string? csv = null)
        => RecurringJobVisibility.ParseVisibleKinds(csv ?? RecurringJobVisibility.DefaultVisiblePlatformJobs).ToHashSet();

    [Fact]
    public void OrganizationJob_IsVisibleToItsOrg()
    {
        Assert.True(RecurringJobVisibility.IsVisible($"dispatch-order-fetch-{_orgA}", _orgA, Curated()));
    }

    [Fact]
    public void OrganizationJob_IsHiddenFromOtherOrgs()
    {
        Assert.False(RecurringJobVisibility.IsVisible($"dispatch-order-fetch-{_orgA}", _orgB, Curated()));
    }

    [Fact]
    public void PlatformJob_WithKindInVisibleSet_IsVisible()
    {
        Assert.True(RecurringJobVisibility.IsVisible("refresh-financial-materialized-views", _orgA, Curated()));
    }

    [Fact]
    public void PlatformJob_WithKindNotInVisibleSet_IsHidden()
    {
        Assert.False(RecurringJobVisibility.IsVisible("dev-runtime-data-seeder", _orgA, Curated()));
    }

    [Fact]
    public void RemovedFromVisibleSet_BecomesHidden()
    {
        var visible = Curated("FinancialViewRefresh");

        Assert.False(RecurringJobVisibility.IsVisible("refresh-monitoring-materialized-views", _orgA, visible));
    }

    [Fact]
    public void AddedToVisibleSet_BecomesVisible()
    {
        var visible = Curated(RecurringJobVisibility.DefaultVisiblePlatformJobs + ",RuntimeDataSeeder");

        Assert.True(RecurringJobVisibility.IsVisible("dev-runtime-data-seeder", _orgA, visible));
    }

    [Fact]
    public void ParseVisibleKinds_SplitsAndValidatesCsv()
    {
        var parsed = RecurringJobVisibility.ParseVisibleKinds("OrderFetch, SystemEventCleanup ,unknown-entry");

        Assert.Equal(new[] { RecurringJobKind.OrderFetch, RecurringJobKind.SystemEventCleanup }, parsed);
    }

    [Fact]
    public void JoinVisibleKinds_RoundTripsParse()
    {
        var kinds = new[] { RecurringJobKind.OrderFetch, RecurringJobKind.SystemEventCleanup };

        Assert.Equal(kinds, RecurringJobVisibility.ParseVisibleKinds(RecurringJobVisibility.JoinVisibleKinds(kinds)));
    }

    [Fact]
    public void IsPlatformWide_ClassifiesByIdShape()
    {
        Assert.True(RecurringJobVisibility.IsPlatformWide("system-event-cleanup"));
        Assert.False(RecurringJobVisibility.IsPlatformWide($"dispatch-order-fetch-{_orgA}"));
    }
}