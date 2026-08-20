// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

// Web-only helper functions for the ADWAIS frontend.

/** Format a revenue value (whole SEK) into a locale currency string */
export function formatCurrency(value: number, currency = 'SEK'): string {
  return new Intl.NumberFormat('sv-SE', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
    minimumFractionDigits: 0,
  }).format(value);
}

/** Compact number: 1 200 000 → "1,2 mn", 71 000 → "71 t" */
export function formatCompact(value: number): string {
  const absValue = Math.abs(value);
  const sign = value < 0 ? '-' : '';

  if (absValue >= 1_000_000) {
    const rawVal = absValue / 1_000_000;
    const formatted = rawVal.toFixed(1).replace('.', ',');
    const clean = formatted.endsWith(',0') ? formatted.slice(0, -2) : formatted;
    return `${sign}${clean}\u00a0mn`;
  }

  if (absValue >= 1_000) {
    const rawVal = absValue / 1_000;
    const formatted = rawVal.toFixed(1).replace('.', ',');
    const clean = formatted.endsWith(',0') ? formatted.slice(0, -2) : formatted;
    return `${sign}${clean}\u00a0t`;
  }

  return new Intl.NumberFormat('sv-SE').format(value);
}

/** Format a number with thousand separators */
export function formatNumber(value: number): string {
  return new Intl.NumberFormat('sv-SE').format(value);
}

export { formatChartLabel, inferBinSize } from './formatChartLabel';
export { foldDailySeries } from './foldDailySeries';
