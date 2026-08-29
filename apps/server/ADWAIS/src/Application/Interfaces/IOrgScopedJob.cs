// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Interfaces;

/// <summary>
/// Marks job payloads that must carry a non-empty organization id as their
/// first argument. The Hangfire enforcement filter rejects enqueues that
/// violate this shape.
/// </summary>
public interface IOrgScopedJob
{
}