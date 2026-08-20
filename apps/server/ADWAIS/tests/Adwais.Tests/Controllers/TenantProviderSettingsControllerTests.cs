// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.Controllers;
using Adwais.Api.Controllers.Administration;
using Adwais.Api.DTOs.Tenants;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Adwais.Tests.Controllers;

public class TenantProviderSettingsControllerTests
{
    [Fact]
    public async Task UpdateTenant_OmittedAuthorizationPreservesItAndDoesNotReturnIt()
    {
        var (controller, db, tenant) = CreateController();
        const string originalSettings = "{\"endpointUrl\":\"https://old.example.com\",\"authorization\":\"ServiceAccount secret\"}";
        tenant.OrderProviderSettings = originalSettings;
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var result = await controller.UpdateTenant(tenant.Id, new UpdateTenantRequestDto
        {
            OrderProviderSettings = new Dictionary<string, string?> { ["endpointUrl"] = "https://new.example.com" }
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TenantResponseDto>(ok.Value);
        Assert.Equal("https://new.example.com", response.OrderProviderSettings!["endpointUrl"]);
        Assert.DoesNotContain("authorization", response.OrderProviderSettings.Keys);
        Assert.Contains("ServiceAccount secret", (await db.Tenants.AsNoTracking().SingleAsync()).OrderProviderSettings);
    }

    [Fact]
    public async Task UpdateTenant_NullAuthorizationClearsIt()
    {
        var (controller, db, tenant) = CreateController();
        tenant.OrderProviderSettings = "{\"endpointUrl\":\"https://example.com\",\"authorization\":\"ServiceAccount secret\"}";
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var result = await controller.UpdateTenant(tenant.Id, new UpdateTenantRequestDto
        {
            OrderProviderSettings = new Dictionary<string, string?> { ["authorization"] = null }
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TenantResponseDto>(ok.Value);
        Assert.Empty(response.OrderProviderConfiguredSecretKeys);
        Assert.DoesNotContain("ServiceAccount secret", (await db.Tenants.AsNoTracking().SingleAsync()).OrderProviderSettings);
    }

    [Fact]
    public async Task UpdateTenant_RedactedAuthorizationReturnsBadRequestWithoutChangingIt()
    {
        var (controller, db, tenant) = CreateController();
        const string originalSettings = "{\"endpointUrl\":\"https://example.com\",\"authorization\":\"ServiceAccount secret\"}";
        tenant.OrderProviderSettings = originalSettings;
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var result = await controller.UpdateTenant(tenant.Id, new UpdateTenantRequestDto
        {
            OrderProviderSettings = new Dictionary<string, string?> { ["authorization"] = "configured" }
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(originalSettings, (await db.Tenants.AsNoTracking().SingleAsync()).OrderProviderSettings);
    }

    private static (TenantController Controller, AnalyticsDbContext Db, Tenant Tenant) CreateController()
    {
        var db = new AnalyticsDbContext(new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var monitorService = new Mock<IMonitorOrchestrationService>();
        var controller = new TenantController(db, monitorService.Object, [new LitiumOrderSource(new HttpClient())]);
        return (controller, db, new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant",
            OrderProvider = "litium",
            OrderFetchingEnabled = false
        });
    }
}
