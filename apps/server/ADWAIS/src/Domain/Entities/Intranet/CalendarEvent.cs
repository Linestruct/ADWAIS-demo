// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Enums;

namespace Adwais.Domain.Entities.Intranet;

public class CalendarEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public EventType EventType { get; set; }
    public bool IsRecurring { get; set; }
    public RecurrenceType Recurrence { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? ExternalUid { get; set; }
    public Guid? CalendarSubscriptionId { get; set; }
    public CalendarSubscription? CalendarSubscription { get; set; }
    public Organization? Organization { get; set; }
}
