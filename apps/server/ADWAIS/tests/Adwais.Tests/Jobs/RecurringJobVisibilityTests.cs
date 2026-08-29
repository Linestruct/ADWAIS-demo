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

    private static HashSet<string> Curated(string csv = RecurringJobVisibility.DefaultVisiblePlatformJobs)
        => RecurringJobVisibility.ParseVisibleJobs(csv).ToHashSet(StringComparer.Ordinal);

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
    public void CuratedPlatformJob_IsVisible()
    {
        Assert.True(RecurringJobVisibility.IsVisible("refresh-financial-materialized-views", _orgA, Curated()));
    }

    [Fact]
    public void NonCuratedPlatformJob_IsHidden()
    {
        Assert.False(RecurringJobVisibility.IsVisible("dev-runtime-data-seeder", _orgA, Curated()));
    }

    [Fact]
    public void RemovedFromVisibleSet_BecomesHidden()
    {
        var visible = Curated("refresh-financial-materialized-views");

        Assert.False(RecurringJobVisibility.IsVisible("refresh-monitoring-materialized-views", _orgA, visible));
    }

    [Fact]
    public void AddedToVisibleSet_BecomesVisible()
    {
        var visible = Curated(RecurringJobVisibility.DefaultVisiblePlatformJobs + ",dev-runtime-data-seeder");

        Assert.True(RecurringJobVisibility.IsVisible("dev-runtime-data-seeder", _orgA, visible));
    }

    [Fact]
    public void ParseVisibleJobs_SplitsAndTrimsCsv()
    {
        var parsed = RecurringJobVisibility.ParseVisibleJobs(" a, b ,c ");

        Assert.Equal(new[] { "a", "b", "c" }, parsed);
    }

    [Fact]
    public void JoinVisibleJobs_RoundTripsParse()
    {
        var jobs = new[] { "a", "b", "c" };

        Assert.Equal(jobs, RecurringJobVisibility.ParseVisibleJobs(RecurringJobVisibility.JoinVisibleJobs(jobs)));
    }
}