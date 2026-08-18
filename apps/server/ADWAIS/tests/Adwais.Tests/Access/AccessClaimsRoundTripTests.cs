// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Security.Claims;
using Adwais.Api.Services;
using Adwais.Application.Common.Access;
using Adwais.Domain.Enums;
using Xunit;

namespace Adwais.Tests.Access;

/// <summary>
/// Guarantees the claim format written by AccessClaimsBuilder and read back
/// by CurrentAccessService never drift apart. If one side changes, this test
/// fails instead of silently mis-scoping requests.
/// </summary>
public class AccessClaimsRoundTripTests
{
    [Fact]
    public void BuilderToResolver_PlatformScope_RoundTrips()
    {
        var scope = new AccessScope(null, null, [UserRole.Admin]);

        var resolved = Resolve(scope);

        Assert.NotNull(resolved);
        Assert.Null(resolved.OrganizationId);
        Assert.Null(resolved.TenantId);
        Assert.True(resolved.IsPlatformAdmin);
        Assert.Equal(scope.Roles, resolved.Roles);
    }

    [Fact]
    public void BuilderToResolver_OrganizationScope_RoundTrips()
    {
        var organizationId = Guid.NewGuid();
        var scope = new AccessScope(organizationId, null, [UserRole.Employee]);

        var resolved = Resolve(scope);

        Assert.NotNull(resolved);
        Assert.Equal(organizationId, resolved.OrganizationId);
        Assert.Null(resolved.TenantId);
        Assert.False(resolved.IsPlatformAdmin);
        Assert.Equal(scope.Roles, resolved.Roles);
    }

    [Fact]
    public void BuilderToResolver_TenantScope_RoundTrips()
    {
        var organizationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var scope = new AccessScope(organizationId, tenantId, [UserRole.TenantViewer]);

        var resolved = Resolve(scope);

        Assert.NotNull(resolved);
        Assert.Equal(organizationId, resolved.OrganizationId);
        Assert.Equal(tenantId, resolved.TenantId);
        Assert.True(resolved.IsTenantRestricted);
        Assert.Equal(scope.Roles, resolved.Roles);
    }

    [Fact]
    public void BuilderToResolver_MultipleRoles_RoundTrips()
    {
        var scope = new AccessScope(Guid.NewGuid(), null, [UserRole.Viewer, UserRole.Employee]);

        var resolved = Resolve(scope);

        Assert.NotNull(resolved);
        Assert.Equal(scope.Roles.OrderBy(role => role), resolved.Roles.OrderBy(role => role));
    }

    [Fact]
    public void Resolver_PrincipalWithoutAccessClaims_ReturnsNull()
    {
        var identity = new ClaimsIdentity("TestAuthentication");
        identity.AddClaim(new Claim(ClaimTypes.Name, "Nobody"));
        var principal = new ClaimsPrincipal(identity);

        Assert.Null(CurrentAccessService.Resolve(principal));
    }

    private static AccessScope? Resolve(AccessScope scope)
    {
        var identity = AccessClaimsBuilder.Build(Guid.NewGuid(), scope);
        return CurrentAccessService.Resolve(new ClaimsPrincipal(identity));
    }
}