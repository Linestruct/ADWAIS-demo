// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Domain.Enums;
using Xunit;

namespace Adwais.Tests.Access;

public class OrganizationFilterTests
{
    [Fact]
    public void From_NullScope_IsDenied()
    {
        var filter = OrganizationFilter.From(null);

        Assert.True(filter.Denied);
    }

    [Fact]
    public void From_PlatformScope_IsUnrestricted()
    {
        var filter = OrganizationFilter.From(new AccessScope(null, null, [UserRole.Admin]));

        Assert.False(filter.Denied);
        Assert.Null(filter.OrganizationId);
    }

    [Fact]
    public void From_OrgScope_CarriesOrganizationId()
    {
        var orgId = Guid.NewGuid();
        var filter = OrganizationFilter.From(new AccessScope(orgId, null, [UserRole.Employee]));

        Assert.False(filter.Denied);
        Assert.Equal(orgId, filter.OrganizationId);
    }

    [Fact]
    public void From_TenantScope_CarriesOrganizationId()
    {
        var orgId = Guid.NewGuid();
        var filter = OrganizationFilter.From(new AccessScope(orgId, Guid.NewGuid(), [UserRole.TenantViewer]));

        Assert.False(filter.Denied);
        Assert.Equal(orgId, filter.OrganizationId);
    }
}
