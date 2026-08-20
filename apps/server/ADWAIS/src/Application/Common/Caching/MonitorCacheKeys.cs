// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Caching;

public static class GlobalCacheKeys
{
    public static string MonitorState(int monitorId) => $"monitor_state_{monitorId}";
    public const string UptimeRobotRateLimit = "UptimeRobotRateLimitEpoch";
}

public record LiveMonitorState(string StatusStr, double? CurrentLatency);
