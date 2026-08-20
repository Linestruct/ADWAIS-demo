// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain;
using Adwais.Domain.Enums;

namespace Adwais.Domain.Entities.OrderData;

public class Order
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = IntegrationProviders.Litium;
    public string ExternalId { get; set; } = string.Empty;
    public Guid? OrganizationSystemId { get; set; }
    public OrderState OrderState { get; set; }
    public required string OrderNumber { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public decimal TotalValueIncVat { get; set; }
    public decimal TotalValueExcVat { get; set; }
    public string? Currency { get; set; }

    public Tenant? Tenant { get; set; }
}

