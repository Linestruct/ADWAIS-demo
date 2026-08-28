// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using Adwais.Api.Authentication;
using Adwais.Application.Common.Access;
using Adwais.Infrastructure.Persistence;
using Xunit;

namespace Adwais.Tests.Authentication;

public class DevMockAuthenticationHandlerTests
{
    [Fact]
    public void BuildForDev_InDevelopment_NoAuthHeader_ReturnsPlatformAdmin()
    {
        var principal = DevMockAuthenticationHandler.BuildForDev(isDevelopment: true, hasAuthHeader: false, mockOrganizationId: null);

        Assert.NotNull(principal);
        Assert.True(principal.HasClaim(AccessClaimTypes.IsPlatformAdmin, "true"));
        Assert.True(principal.IsInRole("PlatformAdmin"));
        Assert.True(principal.HasClaim(
            c => c.Type == ClaimTypes.NameIdentifier && c.Value == AnalyticsDbContext.SystemUserGuid.ToString()));
    }

    [Fact]
    public void BuildForDev_WithMockOrganizationId_ReturnsOrgScopedAdmin()
    {
        var orgId = Guid.NewGuid();

        var principal = DevMockAuthenticationHandler.BuildForDev(isDevelopment: true, hasAuthHeader: false, mockOrganizationId: orgId);

        Assert.NotNull(principal);
        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId && c.Value == orgId.ToString()));
        Assert.False(principal.HasClaim(c => c.Type == AccessClaimTypes.IsPlatformAdmin));
    }

    [Fact]
    public void BuildForDev_InDevelopment_WithAuthHeader_ReturnsNull()
    {
        Assert.Null(DevMockAuthenticationHandler.BuildForDev(isDevelopment: true, hasAuthHeader: true, mockOrganizationId: null));
    }

    [Fact]
    public void BuildForDev_InProduction_ReturnsNull()
    {
        Assert.Null(DevMockAuthenticationHandler.BuildForDev(isDevelopment: false, hasAuthHeader: false, mockOrganizationId: null));
    }
}
