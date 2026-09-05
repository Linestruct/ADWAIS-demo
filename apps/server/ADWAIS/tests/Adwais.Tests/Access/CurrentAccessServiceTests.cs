// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Security.Claims;
using Adwais.Api.Services;
using Adwais.Application.Common.Access;
using Adwais.Domain.Enums;
using Xunit;

namespace Adwais.Tests.Access;

public class CurrentAccessServiceTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));

    private static Claim Role(UserRole role) => new(ClaimTypes.Role, role.ToString());

    private static Claim Platform => new(AccessClaimTypes.IsPlatformAdmin, "true");

    private static Claim Org(Guid id) => new(AccessClaimTypes.OrganizationId, id.ToString());

    private static Claim Tenant(Guid id) => new(AccessClaimTypes.TenantId, id.ToString());

    [Fact]
    public void Resolve_PlatformClaim_ReturnsPlatformScope()
    {
        var scope = CurrentAccessService.Resolve(Principal(Platform, Role(UserRole.Admin)));

        Assert.NotNull(scope);
        Assert.True(scope.IsPlatformAdmin);
        Assert.Null(scope.OrganizationId);
        Assert.Null(scope.TenantId);
        Assert.Equal([UserRole.PlatformAdmin], scope.Roles);
    }

    [Fact]
    public void Resolve_OrgClaimWithoutTenant_ReturnsOrgScope()
    {
        var orgId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(Role(UserRole.Employee), Org(orgId)));

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
        Assert.False(scope.IsTenantRestricted);
        Assert.Equal([UserRole.Employee], scope.Roles);
    }

    [Fact]
    public void Resolve_OrgAndTenantClaims_ReturnsTenantRestrictedScope()
    {
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(Role(UserRole.TenantViewer), Org(orgId), Tenant(tenantId)));

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Equal(tenantId, scope.TenantId);
        Assert.True(scope.IsTenantRestricted);
        Assert.Equal([UserRole.TenantViewer], scope.Roles);
    }

    [Fact]
    public void Resolve_AdminRoleWithoutPlatformOrOrgClaim_ReturnsNull()
    {
        var scope = CurrentAccessService.Resolve(Principal(Role(UserRole.Admin)));

        Assert.Null(scope);
    }

    [Fact]
    public void Resolve_NoClaims_ReturnsNull()
    {
        var scope = CurrentAccessService.Resolve(Principal());

        Assert.Null(scope);
    }

    [Fact]
    public void Resolve_InvalidOrgClaim_IsIgnored()
    {
        var scope = CurrentAccessService.Resolve(Principal(
            Role(UserRole.Viewer),
            new Claim(AccessClaimTypes.OrganizationId, "not-a-guid")));

        Assert.Null(scope);
    }

    [Fact]
    public void Resolve_InvalidTenantClaim_IsIgnored()
    {
        var orgId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(
            Role(UserRole.TenantViewer),
            Org(orgId),
            new Claim(AccessClaimTypes.TenantId, "not-a-guid")));

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
    }

    [Fact]
    public void Resolve_IgnoresUnknownRoleClaims()
    {
        var orgId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(
            new Claim(ClaimTypes.Role, "Superhero"),
            Org(orgId)));

        Assert.NotNull(scope);
        Assert.Empty(scope.Roles);
    }
}
