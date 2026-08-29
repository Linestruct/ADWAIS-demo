// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Adwais.Application.Interfaces;

public interface IFeedAggregationService
{
    Task AggregateAllFeedsAsync(CancellationToken ct = default);
    Task AggregateOrgFeedsAsync(Guid organizationId, CancellationToken ct = default);
    Task AggregateSourceAsync(Guid sourceId, CancellationToken ct = default);
}
