// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.DTOs.Financial.Upstream;

namespace Adwais.Application.Interfaces;

public interface IOrderIngestionService
{
    Task<int> ExecuteIngestionAsync(Guid organizationId, Guid tenantId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);
    Task IngestSingleOrderAsync(Guid organizationId, Guid tenantId, string provider, OrderSourceOrder order, CancellationToken ct = default);
}
