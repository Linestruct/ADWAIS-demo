// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities.OrderData;

public class DailyFinancialTenantRollup
{
    public DateTimeOffset CreatedDate { get; set; }
    public Guid TenantId { get; set; }
    public decimal Volume { get; set; }
    public decimal Revenue { get; set; }
}

