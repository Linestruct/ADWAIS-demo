// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Domain.Entities.OrderData;

public class DailyFinancialTenantRollup
{
    public DateTimeOffset CreatedDate { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TenantId { get; set; }
    public decimal Volume { get; set; }
    public decimal Revenue { get; set; }
}

