// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using Adwais.Application.Common.Access;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Xunit;

namespace Adwais.Tests.Common;

public class UserRoleResolverTests
{
    private static UserAccess Membership(Guid? organizationId, UserRole role)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrganizationId = organizationId,
            Role = role
        };

    [Fact]
    public void Dominant_PlatformMembership_WinsOverAnyOrgRole()
    {
        var memberships = new[]
        {
            Membership(Guid.NewGuid(), UserRole.Admin),
            Membership(null, UserRole.PlatformAdmin)
        };

        Assert.Equal(UserRole.PlatformAdmin, UserRoleResolver.Dominant(memberships));
    }

    [Fact]
    public void Dominant_WithoutPlatform_ReturnsHighestOrgRole()
    {
        var memberships = new[]
        {
            Membership(Guid.NewGuid(), UserRole.Viewer),
            Membership(Guid.NewGuid(), UserRole.Employee),
            Membership(Guid.NewGuid(), UserRole.Admin)
        };

        Assert.Equal(UserRole.Admin, UserRoleResolver.Dominant(memberships));
    }

    [Fact]
    public void Dominant_TenantViewer_RanksBelowOrgRoles()
    {
        var memberships = new[]
        {
            Membership(Guid.NewGuid(), UserRole.TenantViewer),
            Membership(Guid.NewGuid(), UserRole.Viewer)
        };

        Assert.Equal(UserRole.Viewer, UserRoleResolver.Dominant(memberships));
    }

    [Fact]
    public void Dominant_EmptyMemberships_ReturnsNull()
    {
        Assert.Null(UserRoleResolver.Dominant(Array.Empty<UserAccess>()));
    }

    [Fact]
    public void Dominant_OnlyTenantViewer_ReturnsTenantViewer()
    {
        var memberships = new[] { Membership(Guid.NewGuid(), UserRole.TenantViewer) };

        Assert.Equal(UserRole.TenantViewer, UserRoleResolver.Dominant(memberships));
    }
}