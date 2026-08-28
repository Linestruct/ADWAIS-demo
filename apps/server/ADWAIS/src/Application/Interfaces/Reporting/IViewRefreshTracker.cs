// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Threading;
using System.Threading.Tasks;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Tracks which organizations have data that the materialized views must incorporate.
/// A row means "this organization changed data that the views have not been rebuilt for".
/// The platform refresh job coalesces all pending marks into a single rebuild.
/// </summary>
public interface IViewRefreshTracker
{
    Task MarkDirtyAsync(Guid organizationId, CancellationToken ct = default);
    Task<bool> IsDirtyAsync(CancellationToken ct = default);
    Task ClearDirtyAsync(CancellationToken ct = default);
}