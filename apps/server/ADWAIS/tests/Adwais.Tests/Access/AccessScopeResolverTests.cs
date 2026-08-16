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
    public void Resolve_PlatformAdminMembership_ReturnsPlatformScope()
    {
        var orgId = Guid.NewGuid();
        var memberships = new[]
        {
            Access(null, null, UserRole.Admin),
            Access(orgId, null, UserRole.Admin)
        };

        var scope = AccessScopeResolver.Resolve(memberships);

        Assert.NotNull(scope);
        Assert.True(scope.IsPlatformAdmin);
        Assert.Null(scope.OrganizationId);
        Assert.Null(scope.TenantId);
    }

    [Fact]
    public void Resolve_SingleOrgMembership_ReturnsOrgScope()
    {
        var orgId = Guid.NewGuid();
        var memberships = new[] { Access(orgId, null, UserRole.Employee) };

        var scope = AccessScopeResolver.Resolve(memberships);

        Assert.NotNull(scope);
        Assert.False(scope.IsPlatformAdmin);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
        Assert.False(scope.IsTenantRestricted);
    }

    [Fact]
    public void Resolve_TenantViewerMembership_ReturnsTenantRestrictedScope()
    {
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var memberships = new[] { Access(orgId, tenantId, UserRole.TenantViewer) };

        var scope = AccessScopeResolver.Resolve(memberships);

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Equal(tenantId, scope.TenantId);
        Assert.True(scope.IsTenantRestricted);
    }

    [Fact]
    public void Resolve_OrgStaffWithTenantViewerRows_KeepsOrgScope()
    {
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var memberships = new[]
        {
            Access(orgId, null, UserRole.Admin),
            Access(orgId, tenantId, UserRole.TenantViewer)
        };

        var scope = AccessScopeResolver.Resolve(memberships);

        Assert.NotNull(scope);
        Assert.Equal(orgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
    }

    [Fact]
    public void Resolve_TenantRowFromOtherOrg_IsIgnored()
    {
        var firstOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var memberships = new[]
        {
            Access(firstOrgId, null, UserRole.Viewer),
            Access(otherOrgId, otherTenantId, UserRole.TenantViewer)
        };

        var scope = AccessScopeResolver.Resolve(memberships);

        Assert.NotNull(scope);
        Assert.Equal(firstOrgId, scope.OrganizationId);
        Assert.Null(scope.TenantId);
    }

    [Fact]
    public void Resolve_NoMemberships_ReturnsNull()
    {
        var scope = AccessScopeResolver.Resolve(Array.Empty<UserAccess>());

        Assert.Null(scope);
    }

    [Fact]
    public void Resolve_MembershipsWithoutOrg_ReturnsNull()
    {
        var memberships = new[] { Access(null, null, UserRole.Viewer) };

        var scope = AccessScopeResolver.Resolve(memberships);

        Assert.Null(scope);
    }
}
