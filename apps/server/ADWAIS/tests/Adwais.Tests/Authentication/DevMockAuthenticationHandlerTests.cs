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
        var principal = DevMockAuthenticationHandler.BuildForDev(isDevelopment: true, hasAuthHeader: false);

        Assert.NotNull(principal);
        Assert.True(principal.HasClaim(AccessClaimTypes.IsPlatformAdmin, "true"));
        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.HasClaim(
            c => c.Type == ClaimTypes.NameIdentifier && c.Value == AnalyticsDbContext.SystemUserGuid.ToString()));
    }

    [Fact]
    public void BuildForDev_InDevelopment_WithAuthHeader_ReturnsNull()
    {
        Assert.Null(DevMockAuthenticationHandler.BuildForDev(isDevelopment: true, hasAuthHeader: true));
    }

    [Fact]
    public void BuildForDev_InProduction_ReturnsNull()
    {
        Assert.Null(DevMockAuthenticationHandler.BuildForDev(isDevelopment: false, hasAuthHeader: false));
    }
}
