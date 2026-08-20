// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.GlobalConfig;

namespace Adwais.Application.Interfaces;

public interface IGlobalConfigService
{
    Task<GlobalConfigResponseDto> GetConfigAsync(CancellationToken ct = default);
    Task<GlobalConfigResponseDto> UpdateConfigAsync(UpdateGlobalConfigRequestDto request, CancellationToken ct = default);
    Task TriggerFeedFetchAsync(CancellationToken ct = default);
    Task UpdateFeedIntervalAsync(int intervalHours, CancellationToken ct = default);
    Task<FetchIntervalsDto> GetFetchIntervalsAsync(CancellationToken ct = default);
    Task<FetchIntervalsDto> UpdateFetchIntervalsAsync(UpdateFetchIntervalsRequestDto request, CancellationToken ct = default);
}
