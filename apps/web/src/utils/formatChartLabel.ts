// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

const TIMEZONE = 'Europe/Stockholm';

type BinSize = 'hour' | 'day' | 'week' | 'month';

/**
 * Derives a display label from a UTC timestamp string.
 * Bin size determines formatting:
 * - hour:  "14:00" (Stockholm local)
 * - day:   "Mon 2", "Tue 3", etc.
 * - week:  "W1", "W2", etc. (index-based, caller passes index)
 * - month: "Jun", "Jul", etc.
 */
export function formatChartLabel(
  timestamp: string,
  binSize: BinSize,
  index?: number
): string {
  const date = new Date(timestamp);

  switch (binSize) {
    case 'hour':
      return date.toLocaleString('en-SE', {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        timeZone: TIMEZONE,
      });
    case 'day':
      return date.toLocaleString('en-SE', {
        month: 'short',
        day: 'numeric',
        timeZone: TIMEZONE,
      });
    case 'week':
      return `W${(index ?? 0) + 1}`;
    case 'month':
      return date.toLocaleString('en-SE', {
        month: 'short',
        year: '2-digit',
        timeZone: TIMEZONE,
      });
    default:
      return date.toISOString();
  }
}

/**
 * Infers bin size from the distance between two consecutive timestamps.
 */
export function inferBinSize(timestamps: string[], isHourly: boolean): BinSize {
  if (isHourly || timestamps.length < 2) return 'hour';

  const d1 = new Date(timestamps[0]).getTime();
  const d2 = new Date(timestamps[1]).getTime();
  const diffDays = (d2 - d1) / (1000 * 60 * 60 * 24);

  if (diffDays >= 28) return 'month';
  if (diffDays >= 7) return 'week';
  if (diffDays >= 1) return 'day';
  return 'hour';
}
