// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Adwais.Application.Interfaces;

public interface IFeedAggregationService
{
    Task AggregateAllFeedsAsync(CancellationToken ct = default);
    Task AggregateSourceAsync(Guid sourceId, CancellationToken ct = default);
}
