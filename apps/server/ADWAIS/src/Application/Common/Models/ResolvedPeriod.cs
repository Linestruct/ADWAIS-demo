// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Application.Common.Models;

public record ResolvedPeriod
{
    public DateTimeOffset CurrentStart { get; }
    public DateTimeOffset CurrentEnd { get; }
    public DateTimeOffset PreviousStart { get; }
    public DateTimeOffset PreviousEnd { get; }
    public int StepsInPeriod { get; }
    public bool IsHourly { get; }
    public bool IncludeActualTime { get; }

    public ResolvedPeriod(
        DateTimeOffset currentStart,
        DateTimeOffset currentEnd,
        DateTimeOffset previousStart,
        DateTimeOffset previousEnd,
        int stepsInPeriod,
        bool isHourly,
        bool includeActualTime)
    {
        if (currentStart >= currentEnd)
            throw new ArgumentException("Period start must be before end.");
        if (previousStart >= previousEnd)
            throw new ArgumentException("Previous period must start before previous end.");
        if (previousStart >= currentStart)
            throw new ArgumentException("Previous period must start before current period.");

        CurrentStart = currentStart;
        CurrentEnd = currentEnd;
        PreviousStart = previousStart;
        PreviousEnd = previousEnd;
        StepsInPeriod = stepsInPeriod;
        IsHourly = isHourly;
        IncludeActualTime = includeActualTime;
    }
}
