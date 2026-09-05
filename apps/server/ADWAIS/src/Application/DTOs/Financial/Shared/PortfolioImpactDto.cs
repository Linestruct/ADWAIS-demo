// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Financial;

public record PortfolioImpactTenantDto(
    Guid TenantId,
    string TenantName,
    TenantType Type,
    decimal BaselineRevenue,
    decimal GrowthPercentage,
    decimal CurrentRevenue,
    decimal OrderVolume,
    decimal VolumeGrowthPercentage,
    decimal PortfolioSharePercentage,
    string? OrderProviderEndpoint);

public record PortfolioImpactDto(
    decimal MedianBaselineRevenue,
    decimal GlobalGrowthPercentage,
    decimal MedianPortfolioShare,
    IReadOnlyList<PortfolioImpactTenantDto> Tenants);
