// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Integrations;
using Adwais.Api.DTOs.Ingestion;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class IngestionControllerTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _options;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly Mock<IOrderSource> _orderSource = new();

    public IngestionControllerTests()
    {
        _options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _orderSource.SetupGet(source => source.Provider).Returns("litium");
        _orderSource.Setup(source => source.IsConfigured(It.IsAny<string?>())).Returns(true);
    }

    private IngestionController CreateController(Guid? scopeOrgId)
    {
        var accessMock = new Mock<ICurrentAccess>();
        accessMock.Setup(access => access.Scope)
            .Returns(scopeOrgId is null ? null : new AccessScope(scopeOrgId.Value, null, [UserRole.Admin]));
        return new IngestionController(
            new AnalyticsDbContext(_options),
            _backgroundJobClient.Object,
            [_orderSource.Object],
            accessMock.Object);
    }

    private async Task<Guid> SeedTenantAsync(Guid organizationId)
    {
        var tenantId = Guid.NewGuid();
        await using var db = new AnalyticsDbContext(_options);
        db.Tenants.Add(new Tenant { Id = tenantId, OrganizationId = organizationId, Name = "Tenant", OrderProvider = "litium" });
        await db.SaveChangesAsync();
        return tenantId;
    }

    private static HistoricalBackfillRequestDto RequestFor(Guid tenantId)
        => new() { TenantId = tenantId };

    [Fact]
    public async Task Backfill_OwnOrgTenant_EnqueuesJob()
    {
        var orgId = Guid.NewGuid();
        var tenantId = await SeedTenantAsync(orgId);
        _backgroundJobClient
            .Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns("job-1");

        var result = await CreateController(orgId).ExecuteHistoricalBackfill(RequestFor(tenantId), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        _backgroundJobClient.Verify(
            client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task Backfill_OtherOrgTenant_IsForbidden()
    {
        var ownOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var tenantId = await SeedTenantAsync(otherOrgId);

        var result = await CreateController(ownOrgId).ExecuteHistoricalBackfill(RequestFor(tenantId), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        _backgroundJobClient.Verify(
            client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task Backfill_PlatformScope_AllowsAnyTenant()
    {
        var orgId = Guid.NewGuid();
        var tenantId = await SeedTenantAsync(orgId);
        _backgroundJobClient
            .Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns("job-1");

        var result = await CreateController(scopeOrgId: null).ExecuteHistoricalBackfill(RequestFor(tenantId), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task Backfill_UnknownTenant_ReturnsNotFound()
    {
        var result = await CreateController(Guid.NewGuid())
            .ExecuteHistoricalBackfill(RequestFor(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}