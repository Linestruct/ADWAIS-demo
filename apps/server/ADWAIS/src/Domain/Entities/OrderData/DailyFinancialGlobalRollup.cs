// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities.OrderData;

public class DailyFinancialGlobalRollup
{
    public DateTimeOffset CreatedDate { get; set; }
    public decimal GlobalVolume { get; set; }
    public decimal GlobalRevenue { get; set; }
}

