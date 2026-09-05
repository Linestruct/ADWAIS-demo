// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Interfaces;

/// <summary>
/// One merged-series row: a timestamped revenue and volume sample for a
/// tenant, or for the whole deployment when TenantId is null.
/// </summary>
public record FinancialSeriesRow(DateTimeOffset Timestamp, Guid? TenantId, decimal Revenue, int Volume);
