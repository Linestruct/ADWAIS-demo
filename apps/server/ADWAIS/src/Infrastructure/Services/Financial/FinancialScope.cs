// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Resolves the tenant ids the current scope may see. Null means no
/// restriction (platform admin). An empty array means no access.
/// Shared by the financial services so the rule lives in one place.
/// </summary>
internal static class FinancialScope
{
    internal static async Task<Guid[]?> ResolveVisibleTenantIdsAsync(
        ICurrentAccess access,
        IApplicationDbContext context,
        CancellationToken ct)
    {
        var filter = OrganizationFilter.From(access.Scope);
        return await TenantVisibility.ResolveAsync(filter, context.Tenants, ct);
    }
}
