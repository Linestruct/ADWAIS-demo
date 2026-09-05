// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Adwais.Application.Interfaces;

public interface ICalendarFeedService
{
    Task<string> GetUserCalendarFeedTokenAsync(Guid userId, CancellationToken ct = default);
    Task<string> RegenerateUserCalendarFeedTokenAsync(Guid userId, CancellationToken ct = default);
    Task<byte[]> GenerateIcsFeedAsync(string feedToken, CancellationToken ct = default);
}
