// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.GlobalConfig;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IGlobalConfigService
{
    Task<GlobalConfigResponseDto> GetConfigAsync(CancellationToken ct = default);
    Task<Result<GlobalConfigResponseDto>> UpdateConfigAsync(UpdateGlobalConfigRequestDto request, CancellationToken ct = default);
    Task TriggerFeedFetchAsync(CancellationToken ct = default);
    Task<Result> UpdateFeedIntervalAsync(int intervalHours, CancellationToken ct = default);
    Task<FetchIntervalsDto> GetFetchIntervalsAsync(CancellationToken ct = default);
    Task<Result<FetchIntervalsDto>> UpdateFetchIntervalsAsync(UpdateFetchIntervalsRequestDto request, CancellationToken ct = default);
}
