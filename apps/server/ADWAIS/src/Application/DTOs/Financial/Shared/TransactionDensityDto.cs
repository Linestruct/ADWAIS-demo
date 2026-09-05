// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Financial;

public record TransactionDensityDto(
    int TotalCount,
    int MinCount,
    int MaxCount,
    double AverageCountPerBucket,
    TransactionDensitySampleQuality SampleQuality,
    TransactionDensityPeriod RequestedPeriod,
    TransactionDensityPeriod EffectivePeriod,
    string TimeZoneId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    IReadOnlyList<TransactionDensityPointDto> Points);
