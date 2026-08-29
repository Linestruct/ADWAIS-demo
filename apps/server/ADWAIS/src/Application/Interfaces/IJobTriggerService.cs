// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Enqueues org-scoped job work on demand. An organization scope triggers
/// only that organization's units; a platform scope fans out to all
/// organizations. No trigger fires a platform loop job directly.
/// </summary>
public interface IJobTriggerService
{
    Task TriggerOrderSyncAsync(Guid? organizationId, CancellationToken ct = default);
    Task TriggerUptimeSyncAsync(Guid? organizationId, CancellationToken ct = default);
    Task TriggerLatencySyncAsync(Guid? organizationId, CancellationToken ct = default);
    Task TriggerFleetSyncAsync(Guid? organizationId, CancellationToken ct = default);
    Task TriggerAccountStatsSyncAsync(Guid? organizationId, CancellationToken ct = default);
}