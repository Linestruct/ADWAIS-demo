// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Common.Access;

/// <summary>
/// The restriction a scope implies for data access. Denied means the scope
/// has no access at all. A null OrganizationId means no restriction
/// (platform admin). A set TenantId pins access to one tenant.
/// </summary>
public readonly record struct OrganizationFilter(bool Denied, Guid? OrganizationId, Guid? TenantId)
{
    public static OrganizationFilter From(AccessScope? scope) => scope switch
    {
        null => new OrganizationFilter(true, null, null),
        { IsPlatformAdmin: true } => new OrganizationFilter(false, null, null),
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