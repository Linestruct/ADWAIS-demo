// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Common.Access;

/// <summary>
/// The organization filter a scope implies for org-level content. Denied means
/// the scope has no access at all. A null OrganizationId means no filter
/// (platform admin).
/// </summary>
public readonly record struct OrganizationFilter(bool Denied, Guid? OrganizationId)
{
    public static OrganizationFilter From(AccessScope? scope) => scope switch
    {
        null => new OrganizationFilter(true, null),
        { IsPlatformAdmin: true } => new OrganizationFilter(false, null),
        _ => new OrganizationFilter(false, scope.OrganizationId)
    };
}
