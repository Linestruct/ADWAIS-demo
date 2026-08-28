// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Entities;
using Adwais.Domain.Enums;

namespace Adwais.Application.Common.Access;

/// <summary>
/// Resolves the contexts a user may operate in from their memberships, and
/// selects the effective scope for a request from those contexts.
/// </summary>
public static class AccessScopeResolver
{
    public static MembershipResolution ResolveAllowed(IEnumerable<UserAccess> memberships)
    {
        var rows = memberships.ToList();
        // A platform admin is an explicit role on a membership row with no
        // organization. Membership writes enforce the invariant that platform
        // roles only exist on null-org rows and org rows never carry it.
        var isPlatformAdmin = rows.Any(row =>
            row.OrganizationId is null && row.Role == UserRole.PlatformAdmin);

        var orgLevelScopes = new List<AllowedScope>();
        var tenantScopes = new List<AllowedScope>();

        foreach (var orgGroup in rows
                     .Where(row => row.OrganizationId is not null)
                     .GroupBy(row => row.OrganizationId!.Value))
        {
            var orgLevelRoles = orgGroup
                .Where(row => row.TenantId is null)
                .Select(row => row.Role)
                .Distinct()
                .ToList();
            if (orgLevelRoles.Count > 0)
            {
                orgLevelScopes.Add(new AllowedScope(orgGroup.Key, null, orgLevelRoles));
            }

            foreach (var tenantGroup in orgGroup
                         .Where(row => row.TenantId is not null)
                         .GroupBy(row => row.TenantId!.Value))
            {
                tenantScopes.Add(new AllowedScope(
                    orgGroup.Key,
                    tenantGroup.Key,
                    tenantGroup.Select(row => row.Role).Distinct().ToList()));
            }
        }

        return new MembershipResolution(isPlatformAdmin, [.. orgLevelScopes, .. tenantScopes]);
    }

    public static AccessScope? SelectEffective(
        MembershipResolution resolution,
        Guid? requestedOrganizationId,
        Guid? requestedTenantId)
    {
        if (resolution.IsPlatformAdmin)
        {
            if (requestedOrganizationId is null)
            {
                return new AccessScope(null, null, [UserRole.PlatformAdmin]);
            }

            return new AccessScope(requestedOrganizationId, requestedTenantId, [UserRole.Admin]);
        }

        if (requestedOrganizationId is null)
        {
            var defaultScope = resolution.OrgScopes.FirstOrDefault(scope => scope.TenantId is null)
                ?? resolution.OrgScopes.FirstOrDefault();
            return defaultScope is null
                ? null
                : new AccessScope(defaultScope.OrganizationId, defaultScope.TenantId, defaultScope.Roles);
        }

        var scopesForOrg = resolution.OrgScopes
            .Where(scope => scope.OrganizationId == requestedOrganizationId)
            .ToList();
        if (scopesForOrg.Count == 0)
        {
            return null;
        }

        if (requestedTenantId is { } tenantId)
        {
            var tenantScope = scopesForOrg.FirstOrDefault(scope => scope.TenantId == tenantId);
            if (tenantScope is not null)
            {
                return new AccessScope(tenantScope.OrganizationId, tenantScope.TenantId, tenantScope.Roles);
            }

            var orgLevelScope = scopesForOrg.FirstOrDefault(scope => scope.TenantId is null);
            return orgLevelScope is null
                ? null
                : new AccessScope(orgLevelScope.OrganizationId, null, orgLevelScope.Roles);
        }

        var orgScope = scopesForOrg.FirstOrDefault(scope => scope.TenantId is null)
            ?? scopesForOrg.First();
        return new AccessScope(orgScope.OrganizationId, orgScope.TenantId, orgScope.Roles);
    }
}
