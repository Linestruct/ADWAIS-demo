// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;

namespace Adwais.Domain.Entities.Intranet;

public class CalendarSubscription
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastPolledAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public string? LastSyncError { get; set; }
    public ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
    public Organization? Organization { get; set; }
}
