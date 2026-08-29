// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain;

namespace Adwais.Application.DTOs.GlobalConfig;

public record GlobalConfigResponseDto(
    int Id,
    DateTimeOffset? LastPolled,
    int SystemEventRetentionDays,
    int MatViewRefreshIntervalMinutes,
    string[] VisibleRecurringJobs);
