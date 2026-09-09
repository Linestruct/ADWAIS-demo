// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Interfaces;

/// <summary>
/// Marks job payloads that must carry a non-empty organization id as their
/// first argument. The Hangfire enforcement filter rejects enqueues that
/// violate this shape.
/// </summary>
public interface IOrgScopedJob
{
}