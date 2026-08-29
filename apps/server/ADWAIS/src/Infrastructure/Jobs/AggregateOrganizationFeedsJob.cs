// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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