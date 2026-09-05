// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Hangfire;

namespace Adwais.Infrastructure.Jobs;

public class CalendarSyncJob(ICalendarSubscriptionService calendarSubscriptionService)
{
    private readonly ICalendarSubscriptionService _calendarSubscriptionService = calendarSubscriptionService;

    [Queue("default")]
    [AutomaticRetry(Attempts = 2, LogEvents = true)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        await _calendarSubscriptionService.TriggerAllSyncsAsync(ct);
    }
}
