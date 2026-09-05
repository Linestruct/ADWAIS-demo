// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useMemo } from 'react';
import type { BubbleDataPoint, ChartData, ChartDataset } from 'chart.js';
import type { CrossSegmentCohortTenantResponseDto } from '@types';
import { chartColor, scaleBubbleRadii } from '../../common/charts/chartJs';
import type { MetricType } from './crossSegmentTypes';
import { COHORT_COLORS } from './crossSegmentTypes';
import { getMetricValue } from './crossSegmentHelpers';

export function useCrossSegmentBeeswarm(
  tenants: CrossSegmentCohortTenantResponseDto[],
  activeCohorts: string[],
  selectedMetric: MetricType,
  isEmpty: boolean
) {
  const cohortXMap = useMemo(() => {
    const map: Record<string, number> = {};
    activeCohorts.forEach((cohortType, idx) => {
      map[cohortType] = idx + 1;
    });
    return map;
  }, [activeCohorts]);


  const { minLogVal, maxLogVal } = useMemo(() => {
    if (isEmpty) return { minLogVal: 1, maxLogVal: 4 };

    const vals = tenants.map(t => getMetricValue(t, selectedMetric)).filter(v => v > 0);
    const rawMin = Math.min(...vals);
    const rawMax = Math.max(...vals);

    const logMin = Math.log10(rawMin);
    const logMax = Math.log10(rawMax);
    const logRange = Math.max(0.4, logMax - logMin);

    return {
      minLogVal: logMin - logRange * 0.10,
      maxLogVal: logMax + logRange * 0.10,
    };
  }, [isEmpty, tenants, selectedMetric]);

  const { bubbles, tenantMetaList } = useMemo(() => {
    const bubbleList: BubbleDataPoint[] = [];
    const metaList: typeof tenants = [];

    activeCohorts.forEach(cohortType => {
      const typeTenants = tenants.filter(t => t.type === cohortType);
      const cohortRadii = scaleBubbleRadii(typeTenants.map(t => t.periodRevenue ?? 0), 13, 20, true);
      const centerX = cohortXMap[cohortType] ?? 1;

      const items = typeTenants.map((t, index) => {
        const val = getMetricValue(t, selectedMetric);
        return {
          tenant: t,
          val,
          logVal: Math.log10(val),
          radius: cohortRadii[index] ?? 12,
        };
      }).sort((a, b) => b.logVal - a.logVal);

      const logSpan = maxLogVal - minLogVal;
      const plotHeightPx = 260;
      const dataToPixelY = (logV: number) => ((maxLogVal - logV) / logSpan) * plotHeightPx;

      const maxDataOffset = 0.33;
      const placed: Array<{ xDataOffset: number; yPx: number; radius: number }> = [];

      items.forEach(item => {
        const yPx = dataToPixelY(item.logVal);
        let bestXOffset = 0;
        let found = false;

        const dataStep = 0.07;
        const maxSteps = Math.floor(maxDataOffset / dataStep);

        for (let step = 0; step <= maxSteps; step++) {
          const offsets = step === 0 ? [0] : [step * dataStep, -step * dataStep];
          for (const offsetX of offsets) {
            let collision = false;
            const offsetPx = offsetX * 220;

            for (const p of placed) {
              const prevOffsetPx = p.xDataOffset * 220;
              const dx = offsetPx - prevOffsetPx;
              const dy = yPx - p.yPx;
              const minDist = item.radius + p.radius - 2;
              if (dx * dx + dy * dy < minDist * minDist) {
                collision = true;
                break;
              }
            }
            if (!collision) {
              bestXOffset = offsetX;
              found = true;
              break;
            }
          }
          if (found) break;
        }

        bestXOffset = Math.max(-maxDataOffset, Math.min(maxDataOffset, bestXOffset));

        placed.push({ xDataOffset: bestXOffset, yPx, radius: item.radius });
        bubbleList.push({
          x: centerX + bestXOffset,
          y: item.logVal,
          r: item.radius,
        });
        metaList.push(item.tenant);
      });
    });

    return { bubbles: bubbleList, tenantMetaList: metaList };
  }, [activeCohorts, tenants, selectedMetric, minLogVal, maxLogVal, cohortXMap]);

  const dataset = useMemo(() => ({
    label: 'Tenants',
    data: bubbles,
    backgroundColor: tenantMetaList.map(t => {
      const [variable, fallback] = t.type ? (COHORT_COLORS[t.type] || COHORT_COLORS.Mixed) : COHORT_COLORS.Mixed;
      return chartColor(variable, fallback) + 'B3';
    }),
    borderColor: '#ffffff',
    borderWidth: 1.5,
    hoverBorderWidth: 2.5,
    tenantMeta: tenantMetaList,
  }) as ChartDataset<'bubble', BubbleDataPoint[]> & { tenantMeta: typeof tenants }, [bubbles, tenantMetaList]);

  const data: ChartData<'bubble'> = useMemo(() => ({ datasets: [dataset] }), [dataset]);

  const majorTickValues = useMemo(() => {
    const minPower = Math.floor(minLogVal);
    const maxPower = Math.ceil(maxLogVal);
    const ticks: number[] = [];
    for (let p = minPower; p <= maxPower; p++) {
      if (p >= minLogVal && p <= maxLogVal) {
        ticks.push(p);
      }
    }
    if (ticks.length < 3) {
      const step = (maxLogVal - minLogVal) / 4;
      for (let i = 0; i <= 4; i++) {
        ticks.push(minLogVal + i * step);
      }
    }
    return ticks;
  }, [minLogVal, maxLogVal]);

  return {
    cohortXMap,
    minLogVal,
    maxLogVal,
    tenantMetaList,
    data,
    majorTickValues,
  };
}
