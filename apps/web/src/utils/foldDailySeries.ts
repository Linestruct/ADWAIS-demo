// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

const MIN_DAILY_GAP_MS = 20 * 60 * 60 * 1000;
const MAX_DAILY_GAP_MS = 2 * 24 * 60 * 60 * 1000;

export function foldDailySeries<T extends { timestamp: string }>(
  points: readonly T[],
  aggregateChunk: (chunk: readonly T[]) => T,
  threshold = 90,
  chunkSize = 7,
): { points: T[]; isFolded: boolean } {
  const gapMs = points.length > 1
    ? new Date(points[1].timestamp).getTime() - new Date(points[0].timestamp).getTime()
    : 0;
  const isDaily = gapMs >= MIN_DAILY_GAP_MS && gapMs < MAX_DAILY_GAP_MS;
  const isFolded = isDaily && points.length > threshold;

  if (!isFolded) {
    return { points: [...points], isFolded: false };
  }

  const foldedPoints: T[] = [];
  for (let index = 0; index < points.length; index += chunkSize) {
    foldedPoints.push(aggregateChunk(points.slice(index, index + chunkSize)));
  }

  return { points: foldedPoints, isFolded: true };
}
