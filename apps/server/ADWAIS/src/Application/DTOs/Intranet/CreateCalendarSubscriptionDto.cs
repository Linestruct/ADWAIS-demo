// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Intranet;

/// <summary>
/// Request data for creating an external calendar subscription.
/// </summary>
/// <param name="Name">The display name shown for the subscription.</param>
/// <param name="Url">The URL of the iCalendar feed to poll.</param>
/// <param name="IsActive">A value indicating whether the subscription should be polled.</param>
public record CreateCalendarSubscriptionDto(
    string Name,
    string Url,
    bool IsActive
);
