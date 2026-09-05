// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Financial.Upstream;

public sealed record OrderSourceOrder(
    string ExternalId,
    string OrderNumber,
    DateTimeOffset CreatedDate,
    OrderState State,
    decimal? TotalValueIncludingVat,
    decimal? TotalValueExcludingVat,
    string Currency);
