// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Text.Json;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Helpers;

internal sealed record FinancialTenantDetail(string Name, TenantType Type, string? OrderProviderEndpoint);

/// <summary>
/// Reads the non-system tenant lookup for the portfolio endpoints. Scope
/// comes from the caller-provided filter.
/// </summary>
internal static class FinancialTenantDetails
{
    internal static async Task<IReadOnlyDictionary<Guid, FinancialTenantDetail>> ReadAsync(
        IApplicationDbContext context,
        TenantSeriesFilter filter,
        CancellationToken ct)
    {
        return await filter.ApplyToTenants(context.Tenants
                .AsNoTracking()
                .Where(t => !t.IsSystem))
            .Select(t => new { t.Id, t.Name, t.Type, t.OrderProviderSettings })
            .ToDictionaryAsync(
                t => t.Id,
                t => new FinancialTenantDetail(t.Name, t.Type, GetEndpoint(t.OrderProviderSettings)),
                ct);
    }

    private static string? GetEndpoint(string? settings)
    {
        if (string.IsNullOrWhiteSpace(settings)) return null;
        try
        {
            using var document = JsonDocument.Parse(settings);
            return document.RootElement.TryGetProperty("endpointUrl", out var endpoint)
                ? endpoint.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
