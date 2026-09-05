// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export type MetricType = 'aov' | 'volume' | 'revenue';

export const METRIC_OPTIONS: Array<{ value: MetricType; label: string }> = [
  { value: 'aov', label: 'Average order value' },
  { value: 'volume', label: 'Order volume' },
  { value: 'revenue', label: 'Period revenue' },
];

export const COHORT_COLORS: Record<string, [string, string]> = {
  B2C: ['--color-chart-1', '#0ea5e9'],
  Mixed: ['--color-chart-2', '#8b5cf6'],
  B2B: ['--color-chart-3', '#2563eb'],
};

export interface CohortGroupQuartiles {
  q1: number;
  median: number;
  q3: number;
}
