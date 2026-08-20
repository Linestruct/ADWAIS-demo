// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.Financial.Upstream;

namespace Adwais.Application.Interfaces;

public interface IOrderIngestionService
{
    Task<int> ExecuteIngestionAsync(Guid tenantId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);
    Task IngestSingleOrderAsync(Guid tenantId, string provider, OrderSourceOrder order, CancellationToken ct = default);
}
