// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Application.DTOs.Intranet;

public record CalendarSubscriptionDto(
    Guid Id,
    string Name,
    string Url,
    bool IsActive,
    DateTimeOffset? LastPolledAt,
    DateTimeOffset? LastSuccessAt,
    string? LastSyncError
);
