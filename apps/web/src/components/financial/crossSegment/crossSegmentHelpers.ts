// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { formatCurrency, formatNumber } from '@utils';
import type { MetricType, CohortGroupQuartiles } from './crossSegmentTypes';

export function getMetricValue(tenant: { averageOrderValue?: number; orderVolume?: number; periodRevenue?: number }, metric: MetricType): number {
  switch (metric) {
    case 'aov': return Math.max(1, tenant.averageOrderValue ?? 0);
    case 'volume': return Math.max(1, tenant.orderVolume ?? 0);
    case 'revenue': return Math.max(1, tenant.periodRevenue ?? 0);
  }
}

export function formatMetricValue(value: number, metric: MetricType): string {
  switch (metric) {
    case 'aov':
    case 'revenue':
      return formatCurrency(value);
    case 'volume':
      return formatNumber(value);
  }
}

export function getGroupQuartiles(cohort: {
  q1Aov?: number; medianAov?: number; q3Aov?: number;
  q1Volume?: number; medianVolume?: number; q3Volume?: number;
  q1Revenue?: number; medianRevenue?: number; q3Revenue?: number;
} | undefined, metric: MetricType): CohortGroupQuartiles {
  if (!cohort) return { q1: 0, median: 0, q3: 0 };
  switch (metric) {
    case 'aov': return { q1: cohort.q1Aov ?? 0, median: cohort.medianAov ?? 0, q3: cohort.q3Aov ?? 0 };
    case 'volume': return { q1: cohort.q1Volume ?? 0, median: cohort.medianVolume ?? 0, q3: cohort.q3Volume ?? 0 };
    case 'revenue': return { q1: cohort.q1Revenue ?? 0, median: cohort.medianRevenue ?? 0, q3: cohort.q3Revenue ?? 0 };
  }
}

export function getOrdinalSuffix(n: number): string {
  const s = ['th', 'st', 'nd', 'rd'];
  const v = n % 100;
  return n + (s[(v - 20) % 10] || s[v] || s[0]);
}

export function getTenantPercentileRank(tenant: { aovPercentileRank?: number; volumePercentileRank?: number; revenuePercentileRank?: number }, metric: MetricType): number {
  switch (metric) {
    case 'aov': return tenant.aovPercentileRank ?? 50;
    case 'volume': return tenant.volumePercentileRank ?? 50;
    case 'revenue': return tenant.revenuePercentileRank ?? 50;
  }
}
