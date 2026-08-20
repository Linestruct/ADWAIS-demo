// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Adwais.Tests.Jobs;

public class OrderFetchDispatcherJobTests
{
    [Fact]
    public async Task ExecuteAsync_EnqueuesAnyConfiguredOrderProvider()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenantId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(options))
        {
            db.GlobalConfigs.Add(new GlobalConfig { Id = 1, OrderFetchEnabled = true, OrderFetchIntervalMinutes = 60 });
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Other provider tenant",
                OrderProvider = "other-provider",
                OrderProviderSettings = "{}",
                OrderFetchingEnabled = true
            });
            await db.SaveChangesAsync();
        }

        var factory = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(options));
        var jobs = new Mock<IBackgroundJobClient>();
        jobs.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-id");

        var job = new OrderFetchDispatcherJob(
            factory.Object,
            jobs.Object,
            Mock.Of<ILogger<OrderFetchDispatcherJob>>(),
            Mock.Of<ISystemEventService>());

        await job.ExecuteAsync();

        jobs.Verify(x => x.Create(
            It.Is<Job>(queued => queued.Type == typeof(IOrderIngestionService)),
            It.IsAny<IState>()), Times.Once);
    }
}
