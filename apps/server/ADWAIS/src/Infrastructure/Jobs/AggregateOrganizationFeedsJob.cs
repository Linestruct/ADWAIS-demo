// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Hangfire;

namespace Adwais.Infrastructure.Jobs;

/// <summary>
/// Aggregates one organization's intranet feeds. Runs on the
/// organization's own recurring cadence.
/// </summary>
public class AggregateOrganizationFeedsJob(IFeedAggregationService feedAggregationService) : IOrgScopedJob
{
    private readonly IFeedAggregationService _feedAggregationService = feedAggregationService;

    [Queue("default")]
    [AutomaticRetry(Attempts = 2, LogEvents = true)]
    public async Task ExecuteAsync(Guid organizationId, CancellationToken ct)
    {
        await _feedAggregationService.AggregateOrgFeedsAsync(organizationId, ct);
    }
}