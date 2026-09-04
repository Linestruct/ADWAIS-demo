// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Services; // Assuming KioskService is implemented here

namespace Adwais.Tests.Services;

public class KioskServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly KioskService _kioskService;

    public KioskServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AnalyticsDbContext(_dbOptions);
        _tokenServiceMock = new Mock<ITokenService>();
        _kioskService = new KioskService(_dbContext, _tokenServiceMock.Object);
    }

    [Fact]
    public async Task RegisterDeviceAsync_ShouldSavePendingDeviceAndReturnAlphanumericCode()
    {
        // Arrange
        var deviceId = "kiosk-device-1";

        // Act
        var code = await _kioskService.RegisterDeviceAsync(deviceId);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);

        // Verify state in DB
        await using var db = new AnalyticsDbContext(_dbOptions);
        var device = await db.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == deviceId);
        Assert.NotNull(device);
        Assert.False(device.IsAuthorized);
        Assert.Equal(code, device.ActivationCode);
        Assert.True(device.ActivationCodeExpires > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ActivateDeviceAsync_ShouldAuthorizeDevice_WhenCodeIsValidAndActive()
    {
        // Arrange
        var code = "AB39XZ";
        var deviceId = "kiosk-device-2";
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.Add(new KioskDevice
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                ActivationCode = code,
                ActivationCodeExpires = DateTimeOffset.UtcNow.AddMinutes(10),
                IsAuthorized = false,
                CreatedDate = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act
        var organizationId = Guid.NewGuid();
        var result = await _kioskService.ActivateDeviceAsync(code, organizationId);

        // Assert
        Assert.True(result);

        // Verify DB update
        await using var dbCtx = new AnalyticsDbContext(_dbOptions);
        var device = await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == deviceId);
        Assert.NotNull(device);
        Assert.True(device.IsAuthorized);
        Assert.NotNull(device.AuthorizedAt);
        Assert.Equal(organizationId, device.OrganizationId);
    }

    [Fact]
    public async Task ActivateDeviceAsync_ShouldReturnFalse_WhenCodeIsExpired()
    {
        // Arrange
        var code = "CD40XY";
        var deviceId = "kiosk-device-3";
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.Add(new KioskDevice
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                ActivationCode = code,
                ActivationCodeExpires = DateTimeOffset.UtcNow.AddMinutes(-1), // Expired
                IsAuthorized = false,
                CreatedDate = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act
        var result = await _kioskService.ActivateDeviceAsync(code, Guid.NewGuid());

        // Assert
        Assert.False(result);

        // Verify DB remains unauthorized
        await using var dbCtx = new AnalyticsDbContext(_dbOptions);
        var device = await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == deviceId);
        Assert.NotNull(device);
        Assert.False(device.IsAuthorized);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnToken_WhenDeviceIsAuthorized()
    {
        // Arrange
        var deviceId = "kiosk-device-4";
        var organizationId = Guid.NewGuid();
        var expectedToken = "mock-jwt-token-30-days";
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.Add(new KioskDevice
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                OrganizationId = organizationId,
                ActivationCode = "CODE12",
                ActivationCodeExpires = DateTimeOffset.UtcNow.AddMinutes(10),
                IsAuthorized = true,
                CreatedDate = DateTimeOffset.UtcNow,
                AuthorizedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        _tokenServiceMock.Setup(s => s.GenerateKioskToken(deviceId, "Viewer", organizationId))
            .Returns(expectedToken);

        // Act
        var token = await _kioskService.GetTokenAsync(deviceId);

        // Assert
        Assert.Equal(expectedToken, token);
        _tokenServiceMock.Verify(s => s.GenerateKioskToken(deviceId, "Viewer", organizationId), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenDeviceIsNotAuthorized()
    {        // Arrange
        var deviceId = "kiosk-device-5";
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.Add(new KioskDevice
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                ActivationCode = "CODE34",
                ActivationCodeExpires = DateTimeOffset.UtcNow.AddMinutes(10),
                IsAuthorized = false,
                CreatedDate = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act
        var token = await _kioskService.GetTokenAsync(deviceId);

        // Assert
        Assert.Null(token);
        _tokenServiceMock.Verify(s => s.GenerateKioskToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldStampLastSeenAt_WhenDeviceIsAuthorized()
    {
        var deviceId = "kiosk-device-6";
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.Add(new KioskDevice
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                OrganizationId = Guid.NewGuid(),
                ActivationCode = "CODE56",
                ActivationCodeExpires = DateTimeOffset.UtcNow.AddMinutes(10),
                IsAuthorized = true,
                CreatedDate = DateTimeOffset.UtcNow.AddDays(-2),
                AuthorizedAt = DateTimeOffset.UtcNow.AddDays(-2),
                LastSeenAt = null
            });
            await db.SaveChangesAsync();
        }

        _tokenServiceMock.Setup(s => s.GenerateKioskToken(deviceId, "Viewer", It.IsAny<Guid?>()))
            .Returns("mock-jwt-token");

        var before = DateTimeOffset.UtcNow;
        await _kioskService.GetTokenAsync(deviceId);

        await using var dbCtx = new AnalyticsDbContext(_dbOptions);
        var device = await dbCtx.KioskDevices.SingleAsync(d => d.DeviceId == deviceId);
        Assert.NotNull(device.LastSeenAt);
        Assert.True(device.LastSeenAt >= before);
    }

    [Fact]
    public async Task RegisterDeviceAsync_ShouldPurgeStalePendingDevices()
    {
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.AddRange(
                new KioskDevice
                {
                    Id = Guid.NewGuid(),
                    DeviceId = "kiosk-stale-pending",
                    ActivationCode = "OLD001",
                    ActivationCodeExpires = DateTimeOffset.UtcNow.AddDays(-8),
                    IsAuthorized = false,
                    CreatedDate = DateTimeOffset.UtcNow.AddDays(-9)
                },
                new KioskDevice
                {
                    Id = Guid.NewGuid(),
                    DeviceId = "kiosk-fresh-pending",
                    ActivationCode = "NEW001",
                    ActivationCodeExpires = DateTimeOffset.UtcNow.AddMinutes(5),
                    IsAuthorized = false,
                    CreatedDate = DateTimeOffset.UtcNow.AddMinutes(-5)
                },
                new KioskDevice
                {
                    Id = Guid.NewGuid(),
                    DeviceId = "kiosk-authorized",
                    OrganizationId = Guid.NewGuid(),
                    ActivationCode = "AUTH01",
                    ActivationCodeExpires = DateTimeOffset.UtcNow.AddDays(-30),
                    IsAuthorized = true,
                    CreatedDate = DateTimeOffset.UtcNow.AddDays(-31),
                    AuthorizedAt = DateTimeOffset.UtcNow.AddDays(-31),
                    LastSeenAt = DateTimeOffset.UtcNow.AddDays(-20)
                });
            await db.SaveChangesAsync();
        }

        await _kioskService.RegisterDeviceAsync("kiosk-brand-new");

        await using var dbCtx = new AnalyticsDbContext(_dbOptions);
        Assert.Null(await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == "kiosk-stale-pending"));
        Assert.NotNull(await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == "kiosk-fresh-pending"));
        Assert.NotNull(await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == "kiosk-authorized"));
        Assert.NotNull(await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == "kiosk-brand-new"));
    }

    [Fact]
    public async Task GetDevicesAsync_ShouldFilterByOrganization()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.AddRange(
                new KioskDevice { Id = Guid.NewGuid(), DeviceId = "kiosk-a1", OrganizationId = orgA, ActivationCode = "A00001", ActivationCodeExpires = DateTimeOffset.UtcNow, IsAuthorized = true, CreatedDate = DateTimeOffset.UtcNow },
                new KioskDevice { Id = Guid.NewGuid(), DeviceId = "kiosk-b1", OrganizationId = orgB, ActivationCode = "B00001", ActivationCodeExpires = DateTimeOffset.UtcNow, IsAuthorized = true, CreatedDate = DateTimeOffset.UtcNow },
                new KioskDevice { Id = Guid.NewGuid(), DeviceId = "kiosk-orphan", OrganizationId = null, ActivationCode = "C00001", ActivationCodeExpires = DateTimeOffset.UtcNow, IsAuthorized = false, CreatedDate = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var orgADevices = await _kioskService.GetDevicesAsync(orgA);
        var allDevices = await _kioskService.GetDevicesAsync(null);

        Assert.Single(orgADevices);
        Assert.Equal("kiosk-a1", orgADevices[0].DeviceId);
        Assert.Equal(3, allDevices.Count);
    }

    [Fact]
    public async Task DeleteDeviceAsync_ShouldRemoveOnlyReachableDevice()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.AddRange(
                new KioskDevice { Id = Guid.NewGuid(), DeviceId = "kiosk-del-own", OrganizationId = orgA, ActivationCode = "D00001", ActivationCodeExpires = DateTimeOffset.UtcNow, IsAuthorized = true, CreatedDate = DateTimeOffset.UtcNow },
                new KioskDevice { Id = Guid.NewGuid(), DeviceId = "kiosk-del-other", OrganizationId = orgB, ActivationCode = "D00002", ActivationCodeExpires = DateTimeOffset.UtcNow, IsAuthorized = true, CreatedDate = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        Assert.True(await _kioskService.DeleteDeviceAsync("kiosk-del-own", orgA));
        Assert.False(await _kioskService.DeleteDeviceAsync("kiosk-del-other", orgA));
        Assert.False(await _kioskService.DeleteDeviceAsync("kiosk-missing", null));

        await using var dbCtx = new AnalyticsDbContext(_dbOptions);
        Assert.Null(await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == "kiosk-del-own"));
        Assert.NotNull(await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == "kiosk-del-other"));
    }
}
