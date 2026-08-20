// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Financial;

public record KpiDto(
    decimal CurrentRevenue,
    decimal PreviousRevenue,
    decimal RevenueGrowthPercentage,
    int TransactionVolume,
    decimal VolumeGrowthPercentage,
    decimal AverageOrderValue,
    decimal AovGrowthPercentage,
    int ActiveTenants,
    decimal ActiveTenantsGrowthPercentage,
    decimal AverageRevenuePerTenant,
    decimal ArptGrowthPercentage);


