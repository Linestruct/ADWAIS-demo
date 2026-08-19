// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Administration;
using Adwais.Api.DTOs.Tenants;
using Adwais.Application.Common.Access;
using Adwais.Application.Interfaces;
using Adwais.Api.Validators.Tenants;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class TenantProviderValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("unsupported")]
    public void CreateTenant_WithMissingOrUnknownProvider_IsInvalid(string provider)
    {
        var source = new Mock<IOrderSource>();
        source.SetupGet(x => x.Provider).Returns("litium");
        var validator = new CreateTenantRequestDtoValidator(new[] { source.Object });

        var result = validator.Validate(new CreateTenantRequestDto
        {
            Name = "Tenant",
            OrderProvider = provider
        });

        Assert.False(result.IsValid);
    }
}

public class TenantControllerScopedTests
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<IMonitorOrchestrationService> _monitorServiceMock;
    private readonly Mock<IOrderSource> _orderSourceMock;
    private readonly Mock<ICurrentAccess> _currentAccessMock;
    private readonly Guid _org1 = Guid.NewGuid();
    private readonly Guid _org2 = Guid.NewGuid();

    public TenantControllerScopedTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AnalyticsDbContext(dbOptions);

        _monitorServiceMock = new Mock<IMonitorOrchestrationService>();
        _orderSourceMock = new Mock<IOrderSource>();
        _orderSourceMock.SetupGet(s => s.Provider).Returns("litium");
        _orderSourceMock.Setup(s => s.MergeSettings(It.IsAny<string?>(), It.IsAny<Dictionary<string, string?>>())).Returns("{}");
        _orderSourceMock.Setup(s => s.IsConfigured(It.IsAny<string?>())).Returns(true);

        _currentAccessMock = new Mock<ICurrentAccess>();
        _currentAccessMock.SetupGet(a => a.Scope).Returns(new AccessScope(_org1, null, [UserRole.Admin]));
    }

    [Fact]
    public async Task GetTenants_FiltersToCallerOrganization()
    {
        var t1 = new Tenant { Id = Guid.NewGuid(), Name = "Org1 Tenant", OrganizationId = _org1, OrderProvider = "litium" };
        var t2 = new Tenant { Id = Guid.NewGuid(), Name = "Org2 Tenant", OrganizationId = _org2, OrderProvider = "litium" };
        _dbContext.Tenants.AddRange(t1, t2);
        await _dbContext.SaveChangesAsync();

        var controller = new TenantController(_dbContext, _monitorServiceMock.Object, [_orderSourceMock.Object], _currentAccessMock.Object);
        var result = await controller.GetTenants(null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<TenantResponseDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(t1.Id, list[0].Id);
    }

    [Fact]
    public async Task CreateTenant_AssignsCallerOrganization()
    {
        var controller = new TenantController(_dbContext, _monitorServiceMock.Object, [_orderSourceMock.Object], _currentAccessMock.Object);
        var request = new CreateTenantRequestDto
        {
            Name = "New Tenant",
            OrderProvider = "litium",
            OrderFetchingEnabled = true,
            OrderProviderSettings = new Dictionary<string, string?> { ["url"] = "https://example.com" }
        };

        var result = await controller.CreateTenant(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<TenantResponseDto>(created.Value);
        var dbTenant = await _dbContext.Tenants.FindAsync(dto.Id);
        Assert.NotNull(dbTenant);
        Assert.Equal(_org1, dbTenant.OrganizationId);
    }

    [Fact]
    public async Task DeleteTenant_CrossOrg_ReturnsNotFound()
    {
        var otherOrgTenant = new Tenant { Id = Guid.NewGuid(), Name = "Org2 Tenant", OrganizationId = _org2, OrderProvider = "litium" };
        _dbContext.Tenants.Add(otherOrgTenant);
        await _dbContext.SaveChangesAsync();

        var controller = new TenantController(_dbContext, _monitorServiceMock.Object, [_orderSourceMock.Object], _currentAccessMock.Object);
        var result = await controller.DeleteTenant(otherOrgTenant.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateTenant_CrossOrg_ReturnsNotFound()
    {
        var otherOrgTenant = new Tenant { Id = Guid.NewGuid(), Name = "Org2 Tenant", OrganizationId = _org2, OrderProvider = "litium" };
        _dbContext.Tenants.Add(otherOrgTenant);
        await _dbContext.SaveChangesAsync();

        var controller = new TenantController(_dbContext, _monitorServiceMock.Object, [_orderSourceMock.Object], _currentAccessMock.Object);
        var result = await controller.UpdateTenant(otherOrgTenant.Id, new UpdateTenantRequestDto { Name = "Hacked" });

        Assert.IsType<NotFoundResult>(result);
    }
}
