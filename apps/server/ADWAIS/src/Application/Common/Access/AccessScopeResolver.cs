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

        var platformRows = rows.Where(row => row.OrganizationId is null).ToList();
        if (platformRows.Any(row => row.Role == UserRole.Admin))
        {
            return new AccessScope(null, null, DistinctRoles(platformRows));
        }

        var organizationId = rows
            .Select(row => row.OrganizationId)
            .FirstOrDefault(id => id is not null);

        if (organizationId is null)
        {
            return null;
        }

        var orgLevelRows = rows
            .Where(row => row.OrganizationId == organizationId && row.TenantId is null)
            .ToList();

        if (orgLevelRows.Count > 0)
        {
            return new AccessScope(organizationId, null, DistinctRoles(orgLevelRows));
        }

        var tenantId = rows
            .Where(row => row.OrganizationId == organizationId)
            .Select(row => row.TenantId)
            .FirstOrDefault(id => id is not null);

        if (tenantId is null)
        {
            return null;
        }

        var tenantRows = rows
            .Where(row => row.OrganizationId == organizationId && row.TenantId == tenantId)
            .ToList();

        return new AccessScope(organizationId, tenantId, DistinctRoles(tenantRows));
    }

    private static IReadOnlyCollection<UserRole> DistinctRoles(IEnumerable<UserAccess> rows)
        => rows.Select(row => row.Role).Distinct().ToList();
}
