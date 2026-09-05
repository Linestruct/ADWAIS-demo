// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Financial;

public record CrossSegmentCohortTenantResponseDto(
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

public record CrossSegmentCohortGroupResponseDto(
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

public record CrossSegmentDistributionResponseDto(
    IReadOnlyList<CrossSegmentCohortGroupResponseDto> Cohorts,
    IReadOnlyList<CrossSegmentCohortTenantResponseDto> Tenants);
