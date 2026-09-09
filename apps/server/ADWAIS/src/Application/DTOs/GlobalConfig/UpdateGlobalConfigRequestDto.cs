// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Jobs;

namespace Adwais.Application.DTOs.GlobalConfig;

public record UpdateGlobalConfigRequestDto(
    int? SystemEventRetentionDays = null,
    int? MatViewRefreshIntervalMinutes = null,
    RecurringJobKind[]? VisibleRecurringJobs = null);
