// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Interfaces;

/// <summary>
/// One merged-series row: a timestamped revenue and volume sample for a
/// tenant, or for the whole deployment when TenantId is null.
/// </summary>
public record FinancialSeriesRow(DateTimeOffset Timestamp, Guid? TenantId, decimal Revenue, int Volume);
