// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

/** Shared Chart.js configuration constants */

export const CHART_AXIS_TICK = {
  fill: 'var(--color-chart-tick)',
  fontWeight: 700,
  fontFamily: 'Manrope, sans-serif',
} as const;

export const CHART_AXIS_TICK_SM = {
  ...CHART_AXIS_TICK,
  fontSize: 11,
} as const;

export const CHART_AXIS_TICK_MD = {
  ...CHART_AXIS_TICK,
  fontSize: 12,
} as const;

export const CHART_AXIS_TICK_LG = {
  ...CHART_AXIS_TICK,
  fontSize: 13,
  fontWeight: 600,
} as const;

export const CHART_GRID_STROKE = 'var(--color-chart-grid)';
export const CHART_GRID_PROPS = {
  stroke: CHART_GRID_STROKE,
  strokeDasharray: '3 4',
  vertical: false,
} as const;

export const CHART_COLORS = {
  primary: 'var(--color-brand-btn-primary)',
  prevLine: 'var(--color-chart-prev-line)',
  text: 'var(--color-brand-text)',
  growth: 'var(--color-growth)',
  decline: 'var(--color-decline)',
  statusUp: 'var(--color-status-up)',
  statusDown: 'var(--color-status-down)',
  grid: 'var(--color-chart-grid)',
  tick: 'var(--color-chart-tick)',
  label: 'var(--color-chart-label)',
} as const;
