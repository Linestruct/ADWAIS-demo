// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Access;

/// <summary>
/// The restriction a scope implies for data access. Denied means the scope
/// has no access at all. A null OrganizationId on a platform scope means no
/// organization restriction. A set TenantId pins access to one tenant.
/// </summary>
public readonly record struct OrganizationFilter(bool Denied, Guid? OrganizationId, Guid? TenantId)
{
    public static OrganizationFilter From(AccessScope? scope) => scope switch
    {
        null => new OrganizationFilter(true, null, null),
        { IsPlatformAdmin: true, OrganizationId: null } => new OrganizationFilter(false, null, null),
        _ => new OrganizationFilter(false, scope.OrganizationId, scope.TenantId)
    };

    /// <summary>
    /// Whether a tenant id is reachable through this filter. A tenant-restricted
    /// filter only allows its pinned tenant. Org and platform filters allow any
    /// tenant; their membership in the org is validated against the data.
    /// </summary>
    public bool AllowsTenant(Guid tenantId) =>
        !Denied && (TenantId is null || TenantId.Value == tenantId);
}
