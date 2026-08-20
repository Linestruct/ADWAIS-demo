// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export const TRANSACTION_DENSITY_PALETTE = [
  '#000004', '#1b0c42', '#4b0c6b', '#781c6d',
  '#a52c60', '#cf4446', '#ed6925', '#fb9b06',
  '#f7d03c', '#fcffa4',
] as const;

export function getDiscreteDensityColor(value: number, min: number, max: number) {
  if (max === min) return TRANSACTION_DENSITY_PALETTE[0];
  const normalized = Math.max(0, Math.min(1, (value - min) / (max - min)));
  const index = Math.min(
    Math.floor(normalized * TRANSACTION_DENSITY_PALETTE.length),
    TRANSACTION_DENSITY_PALETTE.length - 1,
  );
  return TRANSACTION_DENSITY_PALETTE[index];
}
