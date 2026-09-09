// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities;

public class GlobalConfig
{
    public int Id { get; set; }
    public DateTimeOffset? LastPolled { get; set; }
    public int SystemEventRetentionDays { get; set; }
    public int MatViewRefreshIntervalMinutes { get; set; }
    public string? VisibleRecurringJobsCsv { get; set; }
}