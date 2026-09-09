// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Application.Common.Access;

/// <summary>
/// The single way to turn an organization filter into the set of visible
/// tenant ids. Null means no restriction (platform admin). An empty array
/// means the scope has no access at all.
/// </summary>
public static class TenantVisibility
{
    public static async Task<Guid[]?> ResolveAsync(
        OrganizationFilter filter,
        IQueryable<Tenant> tenants,
        CancellationToken ct)
    {
        if (filter.Denied)
        {
            return [];
        }

        if (filter.OrganizationId is null)
        {
            return null;
        }

        if (filter.TenantId is { } tenantId)
        {
            return [tenantId];
        }

        return await tenants
            .Where(tenant => tenant.OrganizationId == filter.OrganizationId)
            .Select(tenant => tenant.Id)
            .ToArrayAsync(ct);
    }
}