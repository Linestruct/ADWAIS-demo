// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;

namespace Adwais.Api.DTOs.Financial;

public record TransactionDensityPointResponseDto(
    [property: Required] int DayOfWeek,
    [property: Required] int Hour,
    [property: Required] int Count,
    [property: Required] decimal TotalRevenue);


