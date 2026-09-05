// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Intranet;

/// <summary>
/// Request data for updating an external calendar subscription.
/// </summary>
/// <param name="Name">The replacement display name, or <see langword="null"/> to keep the current name.</param>
/// <param name="Url">The replacement iCalendar feed URL, or <see langword="null"/> to keep the current URL.</param>
/// <param name="IsActive">The replacement polling state, or <see langword="null"/> to keep the current state.</param>
public record UpdateCalendarSubscriptionDto(
    string? Name,
    string? Url,
    bool? IsActive
);
