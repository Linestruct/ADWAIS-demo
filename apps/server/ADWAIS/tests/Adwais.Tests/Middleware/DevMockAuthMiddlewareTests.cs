// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using Adwais.Api.Middleware;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Adwais.Tests.Middleware;

public class DevMockAuthMiddlewareTests
{
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly DefaultHttpContext _context;

    public DevMockAuthMiddlewareTests()
    {
        _envMock = new Mock<IWebHostEnvironment>();
        _context = new DefaultHttpContext();
    }

    private DevMockAuthMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new DevMockAuthMiddleware(next, _envMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_NoAuthHeader_InjectsBearerHeader()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(s => s.GenerateKioskToken("00000000-0000-0000-0000-000000000002", "Admin")).Returns("mock-jwt-token");

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(s => s.GetService(typeof(ITokenService))).Returns(mockTokenService.Object);
        _context.RequestServices = serviceProviderMock.Object;

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.True(_context.Request.Headers.ContainsKey("Authorization"));
        Assert.Equal("Bearer mock-jwt-token", _context.Request.Headers.Authorization.ToString());
    }

    [Fact]
    public async Task InvokeAsync_InProduction_NoAuthHeader_DoesNotInjectHeader()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns("Production");
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.False(_context.Request.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_WithAuthHeader_DoesNotOverwriteHeader()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns("Development");
        _context.Request.Headers.Authorization = "Bearer existing-token";
        
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Bearer existing-token", _context.Request.Headers.Authorization.ToString());
    }
}
