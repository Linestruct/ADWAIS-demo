// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Adwais.Api.Controllers.Authentication;
using Adwais.Api.DTOs.Kiosk;
using Adwais.Application.Interfaces;

namespace Adwais.Tests.Controllers;

public class KioskAuthControllerTests
{
    private readonly Mock<IKioskService> _kioskServiceMock;
    private readonly KioskAuthController _controller;

    public KioskAuthControllerTests()
    {
        _kioskServiceMock = new Mock<IKioskService>();
        _controller = new KioskAuthController(_kioskServiceMock.Object);
    }

    [Fact]
    public async Task Register_ShouldCreatePendingDeviceAndReturnActivationCode()
    {
        // Arrange
        var deviceId = "kiosk-device-1";
        var expectedCode = "AB39XZ";
        _kioskServiceMock.Setup(s => s.RegisterDeviceAsync(deviceId))
            .ReturnsAsync(expectedCode);

        var request = new RegisterKioskRequestDto { DeviceId = deviceId };

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RegisterKioskResponseDto>(okResult.Value);
        Assert.Equal(expectedCode, response.ActivationCode);
        _kioskServiceMock.Verify(s => s.RegisterDeviceAsync(deviceId), Times.Once);
    }

    [Fact]
    public async Task Activate_ShouldMarkDeviceAsAuthorized_WhenCodeIsValid()
    {
        // Arrange
        var code = "XY98ZA";
        _kioskServiceMock.Setup(s => s.ActivateDeviceAsync(code))
            .ReturnsAsync(true);

        var request = new ActivateKioskRequestDto { ActivationCode = code };

        // Act
        var result = await _controller.Activate(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _kioskServiceMock.Verify(s => s.ActivateDeviceAsync(code), Times.Once);
    }

    [Fact]
    public async Task Activate_ShouldReturnBadRequest_WhenCodeIsExpiredOrInvalid()
    {
        // Arrange
        var code = "EX1234";
        _kioskServiceMock.Setup(s => s.ActivateDeviceAsync(code))
            .ReturnsAsync(false);

        var request = new ActivateKioskRequestDto { ActivationCode = code };

        // Act
        var result = await _controller.Activate(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Activation code has expired or is invalid.", badRequest.Value);
        _kioskServiceMock.Verify(s => s.ActivateDeviceAsync(code), Times.Once);
    }

    [Fact]
    public async Task GetToken_ShouldReturnToken_WhenDeviceIsAuthorized()
    {
        // Arrange
        var deviceId = "kiosk-device-4";
        var expectedToken = "mock-kiosk-jwt-token";
        _kioskServiceMock.Setup(s => s.GetTokenAsync(deviceId))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _controller.GetToken(deviceId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<KioskTokenResponseDto>(okResult.Value);
        Assert.Equal(expectedToken, response.Token);
        Assert.Equal(30, response.ExpiresInDays);
        _kioskServiceMock.Verify(s => s.GetTokenAsync(deviceId), Times.Once);
    }

    [Fact]
    public async Task GetToken_ShouldReturnUnauthorized_WhenDeviceIsNotAuthorized()
    {
        // Arrange
        var deviceId = "kiosk-device-5";
        _kioskServiceMock.Setup(s => s.GetTokenAsync(deviceId))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.GetToken(deviceId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Kiosk device is not authorized.", unauthorizedResult.Value);
        _kioskServiceMock.Verify(s => s.GetTokenAsync(deviceId), Times.Once);
    }

    [Fact]
    public void GenerateSwaggerAdminToken_InDevelopment_WithValidSecret_ReturnsToken()
    {
        // Arrange
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfig.Setup(c => c["Authentication:KioskJwtSecret"]).Returns("SuperSecretKeyForTestingKioskTokens32CharsMinimum!");
        
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(s => s.GenerateKioskToken("swagger-admin", "Admin")).Returns("generated-token");

        var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

        // Act
        var result = _controller.GenerateSwaggerAdminToken(
            "SuperSecretKeyForTestingKioskTokens32CharsMinimum!",
            mockConfig.Object,
            mockTokenService.Object,
            mockEnv.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<KioskTokenResponseDto>(okResult.Value);
        Assert.Equal("generated-token", response.Token);
    }

    [Fact]
    public void GenerateSwaggerAdminToken_InProduction_ReturnsNotFound()
    {
        // Arrange
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var mockTokenService = new Mock<ITokenService>();
        
        var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

        // Act
        var result = _controller.GenerateSwaggerAdminToken(
            "any-secret",
            mockConfig.Object,
            mockTokenService.Object,
            mockEnv.Object);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
