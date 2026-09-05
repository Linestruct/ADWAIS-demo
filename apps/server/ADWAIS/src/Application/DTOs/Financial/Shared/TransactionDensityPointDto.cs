// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Financial;

public record TransactionDensityPointDto(
    int DayOfWeek, // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
    int Hour,      // 0-23
    int Count,
    decimal TotalRevenue);


