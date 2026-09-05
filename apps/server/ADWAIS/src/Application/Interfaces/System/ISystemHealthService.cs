// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.System;

namespace Adwais.Application.Interfaces;

public interface ISystemHealthService
{
    Task<SystemHealthDto> GetHealthAsync(CancellationToken ct = default);
    Task ClearErrorsAsync(CancellationToken ct = default);
    Task<IEnumerable<BackgroundJobStatusDto>> GetRecentJobsAsync(CancellationToken ct = default);
}
