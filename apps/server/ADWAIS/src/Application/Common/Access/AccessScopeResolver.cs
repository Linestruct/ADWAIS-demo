// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Entities;
using Adwais.Domain.Enums;

namespace Adwais.Application.Common.Access;

/// <summary>
/// Resolves the visibility scope for a user from their memberships.
/// </summary>
public static class AccessScopeResolver
{
    public static AccessScope? Resolve(IEnumerable<UserAccess> memberships)
    {
        var rows = memberships.ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        if (rows.Any(row => row.OrganizationId is null && row.Role == UserRole.Admin))
        {
            return new AccessScope(null, null);
        }

        var organizationId = rows
            .Select(row => row.OrganizationId)
            .FirstOrDefault(id => id is not null);

        if (organizationId is null)
        {
            return null;
        }

        var hasOrgLevelAccess = rows.Any(row =>
            row.OrganizationId == organizationId && row.TenantId is null);

        var tenantId = hasOrgLevelAccess
            ? null
            : rows
                .Where(row => row.OrganizationId == organizationId)
                .Select(row => row.TenantId)
                .FirstOrDefault(id => id is not null);

        return new AccessScope(organizationId, tenantId);
    }
}
