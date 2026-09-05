// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Financial;

public record OrderDto(
    Guid AdwaisOrderId,
    string OrderNumber,
    Guid AdwaisTenantId,
    OrderState OrderState,
    DateTimeOffset CreatedDate,
    decimal TotalValueIncVat,
    decimal TotalValueExcVat,
    string? Currency,
    string? TenantName,
    string Provider,
    string ExternalId);
