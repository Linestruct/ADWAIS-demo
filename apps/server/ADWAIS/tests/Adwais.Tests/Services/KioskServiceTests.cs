// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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
        var result = await _kioskService.ActivateDeviceAsync(code);

        // Assert
        Assert.True(result);

        // Verify DB update
        await using var dbCtx = new AnalyticsDbContext(_dbOptions);
        var device = await dbCtx.KioskDevices.SingleOrDefaultAsync(d => d.DeviceId == deviceId);
        Assert.NotNull(device);
        Assert.True(device.IsAuthorized);
        Assert.NotNull(device.AuthorizedAt);
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
        var result = await _kioskService.ActivateDeviceAsync(code);

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
        var expectedToken = "mock-jwt-token-30-days";
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.KioskDevices.Add(new KioskDevice
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                ActivationCode = "CODE12",
                ActivationCodeExpires = DateTimeOffset.UtcNow.AddMinutes(10),
                IsAuthorized = true,
                CreatedDate = DateTimeOffset.UtcNow,
                AuthorizedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        _tokenServiceMock.Setup(s => s.GenerateKioskToken(deviceId))
            .Returns(expectedToken);

        // Act
        var token = await _kioskService.GetTokenAsync(deviceId);

        // Assert
        Assert.Equal(expectedToken, token);
        _tokenServiceMock.Verify(s => s.GenerateKioskToken(deviceId), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenDeviceIsNotAuthorized()
    {
        // Arrange
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
        _tokenServiceMock.Verify(s => s.GenerateKioskToken(It.IsAny<string>()), Times.Never);
    }
}
