// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Domain.Entities.OrderData;

public class DailyFinancialGlobalRollup
{
    public DateTimeOffset CreatedDate { get; set; }
    public Guid OrganizationId { get; set; }
    public decimal GlobalVolume { get; set; }
    public decimal GlobalRevenue { get; set; }
}

