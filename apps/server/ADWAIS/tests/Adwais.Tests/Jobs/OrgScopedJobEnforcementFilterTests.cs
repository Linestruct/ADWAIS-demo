// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Jobs;
using Hangfire.Common;
using Xunit;

namespace Adwais.Tests.Jobs;

public class OrgScopedJobEnforcementFilterTests
{
    private sealed class OrgJob : IOrgScopedJob
    {
        public void Run(Guid organizationId, int value) { }
    }

    private sealed class NoArgOrgJob : IOrgScopedJob
    {
        public void Run() { }
    }

    private sealed class PlainJob
    {
        public void Run() { }
    }

    private static Job JobFor<T>(params object[] args)
        => new(typeof(T), typeof(T).GetMethod("Run")!, args);

    [Fact]
    public void EnsureOrgScope_WithValidOrgId_DoesNotThrow()
    {
        var job = JobFor<OrgJob>(Guid.NewGuid(), 42);

        var exception = Record.Exception(() => OrgScopedJobEnforcementFilter.EnsureOrgScope(job));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureOrgScope_WithEmptyOrgId_Throws()
    {
        var job = JobFor<OrgJob>(Guid.Empty, 42);

        Assert.Throws<InvalidOperationException>(() => OrgScopedJobEnforcementFilter.EnsureOrgScope(job));
    }

    [Fact]
    public void EnsureOrgScope_WithNoArgs_Throws()
    {
        var job = JobFor<NoArgOrgJob>();

        Assert.Throws<InvalidOperationException>(() => OrgScopedJobEnforcementFilter.EnsureOrgScope(job));
    }

    [Fact]
    public void EnsureOrgScope_WithStringOrgId_AcceptsParsableValue()
    {
        var job = JobFor<OrgJob>(Guid.NewGuid().ToString(), 42);

        var exception = Record.Exception(() => OrgScopedJobEnforcementFilter.EnsureOrgScope(job));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureOrgScope_WithUnparsableStringOrgId_Throws()
    {
        var job = JobFor<OrgJob>("not-a-guid", 42);

        Assert.Throws<InvalidOperationException>(() => OrgScopedJobEnforcementFilter.EnsureOrgScope(job));
    }

    [Fact]
    public void EnsureOrgScope_ForPlainJob_DoesNotThrow()
    {
        var job = JobFor<PlainJob>();

        var exception = Record.Exception(() => OrgScopedJobEnforcementFilter.EnsureOrgScope(job));

        Assert.Null(exception);
    }
}