// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Xunit;

namespace Adwais.Tests.Access;

public class AccessScopeResolverTests
{
    private static UserAccess Access(Guid? organizationId, Guid? tenantId, UserRole role = UserRole.Admin)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrganizationId = organizationId,
            TenantId = tenantId,
            Role = role
        };

    [Fact]
    public void ResolveAllowed_PlatformRow_FlagsPlatformAdmin()
    {
        var orgId = Guid.NewGuid();
        var memberships = new[]
        {
            Access(null, null, UserRole.PlatformAdmin),
            Access(orgId, null, UserRole.Admin)
        };

        var resolution = AccessScopeResolver.ResolveAllowed(memberships);

        Assert.True(resolution.IsPlatformAdmin);
        Assert.Equal(1, resolution.OrgScopes.Count);
    }

    [Fact]
    public void ResolveAllowed_GroupsOrgLevelAndTenantScopesPerOrg()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var memberships = new[]
        {
            Access(orgA, null, UserRole.Employee),
            Access(orgB, null, UserRole.Admin),
            Access(orgB, tenantB, UserRole.TenantViewer)
        };

        var resolution = AccessScopeResolver.ResolveAllowed(memberships);

        Assert.False(resolution.IsPlatformAdmin);
        Assert.Equal(3, resolution.OrgScopes.Count);
        var scopeB = Assert.Single(resolution.OrgScopes, scope => scope.OrganizationId == orgB && scope.TenantId is null);
        Assert.Equal([UserRole.Admin], scopeB.Roles);
        Assert.Single(resolution.OrgScopes, scope => scope.OrganizationId == orgB && scope.TenantId == tenantB);
    }

    [Fact]
    public void SelectEffective_PlatformAdminWithoutRequest_ReturnsPlatformScope()
    {
        var resolution = new MembershipResolution(true, []);

        var scope = AccessScopeResolver.SelectEffective(resolution, null, null);

        Assert.NotNull(scope);
        Assert.True(scope.IsPlatformAdmin);
        Assert.Equal([UserRole.PlatformAdmin], scope.Roles);
    }

    [Fact]
    public void SelectEffective_AdminRequestingOrg_ReturnsOrgScopeWithAdminRole()
    {
        var orgId = Guid.NewGuid();
        var resolution = new MembershipResolution(true, []);

        var scope = AccessScopeResolver.SelectEffective(resolution, orgId, null);

        Assert.NotNull(scope);
        Assert.False(scope.IsPlatformAdmin);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
        Assert.Equal([UserRole.Admin], scope.Roles);
    }

    [Fact]
    public void SelectEffective_OrgMemberRequestingOwnOrg_ReturnsOrgScope()
    {
        var orgId = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
            [new AllowedScope(orgId, null, [UserRole.Employee])]);

        var scope = AccessScopeResolver.SelectEffective(resolution, orgId, null);

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
        Assert.Equal([UserRole.Employee], scope.Roles);
    }

    [Fact]
    public void SelectEffective_OrgMemberRequestingOtherOrg_ReturnsNull()
    {
        var ownOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
            [new AllowedScope(ownOrg, null, [UserRole.Employee])]);

        var scope = AccessScopeResolver.SelectEffective(resolution, otherOrg, null);

        Assert.Null(scope);
    }

    [Fact]
    public void SelectEffective_SingleOrgMemberWithoutRequest_DefaultsToOwnOrg()
    {
        var orgId = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
            [new AllowedScope(orgId, null, [UserRole.Employee])]);

        var scope = AccessScopeResolver.SelectEffective(resolution, null, null);

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
    }

    [Fact]
    public void SelectEffective_MultiOrgMemberWithoutRequest_DefaultsToFirstOrg()
    {
        var firstOrg = Guid.NewGuid();
        var secondOrg = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
        [
            new AllowedScope(firstOrg, null, [UserRole.Viewer]),
            new AllowedScope(secondOrg, null, [UserRole.Admin])
        ]);

        var scope = AccessScopeResolver.SelectEffective(resolution, null, null);

        Assert.NotNull(scope);
        Assert.Equal(firstOrg, scope.OrganizationId);
        Assert.Equal([UserRole.Viewer], scope.Roles);
    }

    [Fact]
    public void SelectEffective_TenantViewerWithoutRequest_PinnedToTenant()
    {
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
            [new AllowedScope(orgId, tenantId, [UserRole.TenantViewer])]);

        var scope = AccessScopeResolver.SelectEffective(resolution, null, null);

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Equal(tenantId, scope.TenantId);
        Assert.Equal([UserRole.TenantViewer], scope.Roles);
    }

    [Fact]
    public void SelectEffective_TenantViewerRequestingWrongTenant_ReturnsNull()
    {
        var orgId = Guid.NewGuid();
        var ownTenant = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
            [new AllowedScope(orgId, ownTenant, [UserRole.TenantViewer])]);

        var scope = AccessScopeResolver.SelectEffective(resolution, orgId, otherTenant);

        Assert.Null(scope);
    }

    [Fact]
    public void SelectEffective_TenantViewerRequestingOwnTenant_ReturnsTenantScope()
    {
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
            [new AllowedScope(orgId, tenantId, [UserRole.TenantViewer])]);

        var scope = AccessScopeResolver.SelectEffective(resolution, orgId, tenantId);

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Equal(tenantId, scope.TenantId);
    }

    [Fact]
    public void SelectEffective_OrgMemberWithTenantRows_RequestingOrgOnly_ReturnsOrgScope()
    {
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resolution = new MembershipResolution(false,
        [
            new AllowedScope(orgId, null, [UserRole.Employee]),
            new AllowedScope(orgId, tenantId, [UserRole.TenantViewer])
        ]);

        var scope = AccessScopeResolver.SelectEffective(resolution, orgId, null);

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
        Assert.Equal([UserRole.Employee], scope.Roles);
    }

    [Fact]
    public void SelectEffective_NoMemberships_ReturnsNull()
    {
        var resolution = new MembershipResolution(false, []);

        Assert.Null(AccessScopeResolver.SelectEffective(resolution, null, null));
    }
}
