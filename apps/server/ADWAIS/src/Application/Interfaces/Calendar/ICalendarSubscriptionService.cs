// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Intranet;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface ICalendarSubscriptionService
{
    Task<CalendarSubscriptionDto?> GetSubscriptionByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<CalendarSubscriptionDto>> GetSubscriptionsAsync(CancellationToken ct = default);
    Task<Result<CalendarSubscriptionDto>> CreateSubscriptionAsync(CreateCalendarSubscriptionDto dto, CancellationToken ct = default);
    Task<Result<CalendarSubscriptionDto>> UpdateSubscriptionAsync(Guid id, UpdateCalendarSubscriptionDto dto, CancellationToken ct = default);
    Task<Result> DeleteSubscriptionAsync(Guid id, CancellationToken ct = default);
    Task TriggerSyncAsync(Guid id, CancellationToken ct = default);
    Task TriggerAllSyncsAsync(CancellationToken ct = default);
}
