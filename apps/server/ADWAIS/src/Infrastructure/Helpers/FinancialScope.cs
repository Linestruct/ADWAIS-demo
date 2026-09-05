// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Interfaces;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Helpers;

/// <summary>
/// Resolves the tenant ids the current scope may see. Null means no
/// restriction (platform admin). An empty array means no access.
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

    internal static async Task<Result> ValidateExplicitTenantAsync(
        Guid? tenantId,
        Guid[]? visibleTenantIds,
        IApplicationDbContext context,
        CancellationToken ct)
    {
        if (!tenantId.HasValue)
            return Result.Ok();

        var exists = await context.Tenants.AsNoTracking().AnyAsync(tenant => tenant.Id == tenantId.Value, ct);
        if (!exists)
            return Result.Fail(new NotFoundError("tenant", tenantId.Value));

        return visibleTenantIds is not null && !visibleTenantIds.Contains(tenantId.Value)
            ? Result.Fail(new ScopeDeniedError("the requested tenant", "the current scope"))
            : Result.Ok();
    }
}
