// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using System.Threading.Tasks;
using Adwais.Api.Middleware;
using Adwais.Application.Common.Access;
using Adwais.Infrastructure.Persistence;
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
    public async Task InvokeAsync_InDevelopment_NoAuthHeader_SetsPlatformAdminPrincipal()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.NotNull(_context.User);
        Assert.True(_context.User.HasClaim(AccessClaimTypes.IsPlatformAdmin, "true"));
        Assert.True(_context.User.IsInRole("Admin"));
        Assert.False(_context.User.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId));
        Assert.True(_context.User.HasClaim(
            c => c.Type == ClaimTypes.NameIdentifier && c.Value == AnalyticsDbContext.SystemUserGuid.ToString()));
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_ScopeResolvesToPlatform()
    {
        _envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(_context);

        var scope = Adwais.Api.Services.CurrentAccessService.Resolve(_context.User);
        Assert.NotNull(scope);
        Assert.True(scope.IsPlatformAdmin);
    }

    [Fact]
    public async Task InvokeAsync_InProduction_NoAuthHeader_DoesNotSetPrincipal()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns("Production");
        var nextCalled = false;
        var middleware = CreateMiddleware(ctx => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        Assert.True(nextCalled);
        Assert.False(_context.User.Identities.Any(identity => identity.IsAuthenticated));
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
        Assert.False(_context.User.HasClaim(c => c.Type == AccessClaimTypes.IsPlatformAdmin));
    }
}
