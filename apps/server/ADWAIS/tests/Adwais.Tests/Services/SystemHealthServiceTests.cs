// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.System;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Jobs.MaterializedViews;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

[CollectionDefinition("StaticJobStorage", DisableParallelization = true)]
public class StaticJobStorageCollection;

[Collection("StaticJobStorage")]
public class SystemHealthServiceTests
{
    private static readonly System.Reflection.MethodInfo IngestionMethod =
        typeof(IOrderIngestionService).GetMethod(nameof(IOrderIngestionService.ExecuteIngestionAsync))!;

    private readonly DbContextOptions<AnalyticsDbContext> _options;
    private readonly Mock<IMonitoringApi> _monitoringApi = new();
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    public SystemHealthServiceTests()
    {
        _options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var storageMock = new Mock<JobStorage>();
        storageMock.Setup(storage => storage.GetMonitoringApi()).Returns(_monitoringApi.Object);
        JobStorage.Current = storageMock.Object;

        _monitoringApi.Setup(api => api.ProcessingJobs(0, 10))
            .Returns(AsJobList(new Dictionary<string, ProcessingJobDto>()));
        _monitoringApi.Setup(api => api.FailedJobs(0, 15))
            .Returns(AsJobList(new Dictionary<string, FailedJobDto>()));
    }

    private static JobList<T> AsJobList<T>(Dictionary<string, T> items) where T : class
        => new(items);

    private SystemHealthService CreateService(Guid? scopeOrgId)
    {
        var accessMock = new Mock<ICurrentAccess>();
        accessMock.Setup(access => access.Scope)
            .Returns(scopeOrgId is null ? null : new AccessScope(scopeOrgId.Value, null, [UserRole.Admin]));
        return new SystemHealthService(new AnalyticsDbContext(_options), accessMock.Object);
    }

    private void SeedSucceededJobs(params (string Key, Guid? OrgId)[] jobs)
    {
        var succeeded = jobs.ToDictionary(
            j => j.Key,
            j => new SucceededJobDto
            {
                Job = j.OrgId is null
                    ? new Job(typeof(RefreshStaleMaterializedViewsJob), typeof(RefreshStaleMaterializedViewsJob).GetMethod("ExecuteAsync")!, Array.Empty<object>())
                    : new Job(typeof(IOrderIngestionService), IngestionMethod, new object[] { j.OrgId.Value, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, CancellationToken.None }),
                SucceededAt = DateTime.UtcNow,
                TotalDuration = 1000
            });
        _monitoringApi.Setup(api => api.SucceededJobs(0, 15)).Returns(AsJobList(succeeded));
    }

    [Fact]
    public async Task GetRecentJobsAsync_OrgScope_KeepsOnlyThatOrgsJobs()
    {
        SeedSucceededJobs(("1", _orgA), ("2", _orgB), ("3", null));

        var result = (await CreateService(_orgA).GetRecentJobsAsync(CancellationToken.None)).ToList();

        var job = Assert.Single(result);
        Assert.Equal("1", job.JobId);
    }

    [Fact]
    public async Task GetRecentJobsAsync_OrgScope_KeepsOnlyThatOrgsJobsAndResolvesNames()
    {
        var tenantId = Guid.NewGuid();
        var succeeded = new Dictionary<string, SucceededJobDto>
        {
            ["1"] = new()
            {
                Job = new Job(typeof(IOrderIngestionService), IngestionMethod, new object[] { _orgA, tenantId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, CancellationToken.None }),
                SucceededAt = DateTime.UtcNow,
                TotalDuration = 1000
            }
        };
        _monitoringApi.Setup(api => api.SucceededJobs(0, 15)).Returns(AsJobList(succeeded));

        await using (var db = new AnalyticsDbContext(_options))
        {
            db.Tenants.Add(new Adwais.Domain.Entities.Tenant { Id = tenantId, OrganizationId = _orgA, Name = "Storefront A" });
            await db.SaveChangesAsync();
        }

        var result = (await CreateService(_orgA).GetRecentJobsAsync(CancellationToken.None)).ToList();

        var job = Assert.Single(result);
        Assert.Equal("Storefront A", job.TenantName);
    }

    [Fact]
    public async Task GetRecentJobsAsync_PlatformScope_KeepsAllJobs()
    {
        SeedSucceededJobs(("1", _orgA), ("2", _orgB), ("3", null));

        var result = (await CreateService(scopeOrgId: null).GetRecentJobsAsync(CancellationToken.None)).ToList();

        Assert.Equal(3, result.Count);
    }
}
