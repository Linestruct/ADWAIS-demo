// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;
using Adwais.Domain.Enums;

namespace Adwais.Api.DTOs.Financial;

public record TransactionDensityResponseDto(
    [property: Required] int TotalCount,
    [property: Required] int MinCount,
    [property: Required] int MaxCount,
    [property: Required] double AverageCountPerBucket,
    [property: Required] TransactionDensitySampleQuality SampleQuality,
    [property: Required] TransactionDensityPeriod RequestedPeriod,
    [property: Required] TransactionDensityPeriod EffectivePeriod,
    [property: Required] string TimeZoneId,
    [property: Required] DateTimeOffset PeriodStart,
    [property: Required] DateTimeOffset PeriodEnd,
    [property: Required] IReadOnlyList<TransactionDensityPointResponseDto> Points);
