// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Financial;

public record AccumulatedRevenuePointResponseDto(
    DateTimeOffset Timestamp,
    decimal CurrentRevenue,
    decimal CurrentRevenueB2C,
    decimal CurrentRevenueB2B,
    decimal CurrentRevenueMixed,
    decimal PreviousRevenue,
    decimal CurrentAccumulated,
    decimal PreviousAccumulated);


