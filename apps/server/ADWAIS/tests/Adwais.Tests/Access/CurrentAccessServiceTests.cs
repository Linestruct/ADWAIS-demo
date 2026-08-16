// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using Adwais.Api.Services;
using Adwais.Application.Common.Access;
using Xunit;

namespace Adwais.Tests.Access;

public class CurrentAccessServiceTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));

    private static Claim Role(string role) => new(ClaimTypes.Role, role);

    private static Claim Org(Guid id) => new(AccessClaimTypes.OrganizationId, id.ToString());

    private static Claim Tenant(Guid id) => new(AccessClaimTypes.TenantId, id.ToString());

    [Fact]
    public void Resolve_AdminWithoutOrgClaim_ReturnsPlatformScope()
    {
        var scope = CurrentAccessService.Resolve(Principal(Role("Admin")));

        Assert.NotNull(scope);
        Assert.True(scope.IsPlatformAdmin);
        Assert.Null(scope.OrganizationId);
        Assert.Null(scope.TenantId);
    }

    [Fact]
    public void Resolve_OrgClaimWithoutTenant_ReturnsOrgScope()
    {
        var orgId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(Role("Employee"), Org(orgId)));

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
        Assert.False(scope.IsTenantRestricted);
    }

    [Fact]
    public void Resolve_OrgAndTenantClaims_ReturnsTenantRestrictedScope()
    {
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(Role("TenantViewer"), Org(orgId), Tenant(tenantId)));

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Equal(tenantId, scope.TenantId);
        Assert.True(scope.IsTenantRestricted);
    }

    [Fact]
    public void Resolve_AdminWithOrgClaim_ReturnsOrgScope()
    {
        var orgId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(Role("Admin"), Org(orgId)));

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.False(scope.IsPlatformAdmin);
    }

    [Fact]
    public void Resolve_NoClaims_ReturnsNull()
    {
        var scope = CurrentAccessService.Resolve(Principal());

        Assert.Null(scope);
    }

    [Fact]
    public void Resolve_ViewerWithoutOrgClaim_ReturnsNull()
    {
        var scope = CurrentAccessService.Resolve(Principal(Role("Viewer")));

        Assert.Null(scope);
    }

    [Fact]
    public void Resolve_InvalidOrgClaim_IsIgnored()
    {
        var scope = CurrentAccessService.Resolve(Principal(Role("Viewer"), new Claim(AccessClaimTypes.OrganizationId, "not-a-guid")));

        Assert.Null(scope);
    }

    [Fact]
    public void Resolve_InvalidTenantClaim_IsIgnored()
    {
        var orgId = Guid.NewGuid();
        var scope = CurrentAccessService.Resolve(Principal(
            Role("TenantViewer"),
            Org(orgId),
            new Claim(AccessClaimTypes.TenantId, "not-a-guid")));

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
    }
}
