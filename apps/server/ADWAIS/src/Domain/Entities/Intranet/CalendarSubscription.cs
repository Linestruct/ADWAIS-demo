// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Adwais.Domain.Entities.Intranet;

public class CalendarSubscription
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastPolledAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public string? LastSyncError { get; set; }
    public ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
}
