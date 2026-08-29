// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Domain.Entities;

public class GlobalConfig
{
    public int Id { get; set; }
    public DateTimeOffset? LastPolled { get; set; }
    public int SystemEventRetentionDays { get; set; }
    public int MatViewRefreshIntervalMinutes { get; set; }
    public string? VisibleRecurringJobsCsv { get; set; }
}