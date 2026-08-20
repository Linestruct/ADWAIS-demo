// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Financial;

public record RevenueEfficiencyTenantDto(
    Guid TenantId,
    string TenantName,
    TenantType Type,
    decimal AverageOrderValue,
    decimal OrderVolume,
    decimal PortfolioSharePercentage,
    decimal GrowthVelocity,
    string? OrderProviderEndpoint
);

public record RevenueEfficiencyDto(
    decimal GlobalAverageOrderValue,
    decimal MedianOrderVolume,
    decimal MedianPortfolioShare,
    IReadOnlyList<RevenueEfficiencyTenantDto> Tenants
);


