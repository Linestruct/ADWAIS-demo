// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Financial;

public record CrossSegmentCohortTenantDto(
    Guid TenantId,
    string TenantName,
    TenantType Type,
    decimal AverageOrderValue,
    int OrderVolume,
    decimal PeriodRevenue,
    decimal PortfolioSharePercentage,
    int AovPercentileRank,
    int VolumePercentileRank,
    int RevenuePercentileRank,
    string? OrderProviderEndpoint);

public record CrossSegmentCohortGroupDto(
    TenantType Type,
    int TenantCount,
    decimal MedianAov,
    decimal Q1Aov,
    decimal Q3Aov,
    decimal MedianVolume,
    decimal Q1Volume,
    decimal Q3Volume,
    decimal MedianRevenue,
    decimal Q1Revenue,
    decimal Q3Revenue);

public record CrossSegmentDistributionDto(
    IReadOnlyList<CrossSegmentCohortGroupDto> Cohorts,
    IReadOnlyList<CrossSegmentCohortTenantDto> Tenants);
