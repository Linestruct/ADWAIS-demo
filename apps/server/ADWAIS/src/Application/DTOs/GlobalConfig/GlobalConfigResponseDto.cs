// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Jobs;
using Adwais.Domain;

namespace Adwais.Application.DTOs.GlobalConfig;

public record GlobalConfigResponseDto(
    int Id,
    DateTimeOffset? LastPolled,
    int SystemEventRetentionDays,
    int MatViewRefreshIntervalMinutes,
    RecurringJobKind[] VisibleRecurringJobs);
