// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useMemo } from 'react';
import type { Plugin } from 'chart.js';
import type { CrossSegmentCohortGroupResponseDto } from '@types';
import { chartColor } from '../../common/charts/chartJs';
import type { MetricType } from './crossSegmentTypes';
import { COHORT_COLORS } from './crossSegmentTypes';
import { getGroupQuartiles } from './crossSegmentHelpers';

export function useCrossSegmentPlugins(
  activeCohorts: string[],
  cohorts: CrossSegmentCohortGroupResponseDto[],
  selectedMetric: MetricType,
  cohortXMap: Record<string, number>
) {
  const logarithmicGridPlugin = useMemo<Plugin<'bubble'>>(() => ({
    id: 'logarithmicGrid',
    beforeDraw(chart) {
      const { ctx, chartArea, scales: { y } } = chart;
      if (!y || !chartArea) return;

      const minLog = y.min;
      const maxLog = y.max;
      const minPower = Math.floor(minLog);
      const maxPower = Math.ceil(maxLog);

      ctx.save();

      for (let p = minPower; p <= maxPower; p++) {
        const base = Math.pow(10, p);
        const subMultipliers = [2, 3, 4, 5, 6, 7, 8, 9];

        subMultipliers.forEach(m => {
          const val = base * m;
          const logVal = Math.log10(val);
          if (logVal < minLog || logVal > maxLog) return;

          const yPx = y.getPixelForValue(logVal);
          if (yPx >= chartArea.top && yPx <= chartArea.bottom) {
            ctx.strokeStyle = 'rgba(148, 163, 184, 0.65)';
            ctx.lineWidth = 1;
            ctx.setLineDash([3, 3]);
            ctx.beginPath();
            ctx.moveTo(chartArea.left, yPx);
            ctx.lineTo(chartArea.right, yPx);
            ctx.stroke();
          }
        });
      }

      ctx.restore();
    },
  }), []);

  const cohortBandsPlugin = useMemo<Plugin<'bubble'>>(() => ({
    id: 'cohortBands',
    beforeDatasetsDraw(chart) {
      const { ctx, scales: { x, y } } = chart;
      if (!x || !y) return;

      activeCohorts.forEach(cohortType => {
        const cohortGroup = cohorts.find(c => c.type === cohortType);
        const quartiles = getGroupQuartiles(cohortGroup, selectedMetric);
        const [variable, fallback] = COHORT_COLORS[cohortType] || COHORT_COLORS.Mixed;
        const colorHex = chartColor(variable, fallback);

        const centerX = cohortXMap[cohortType] ?? 1;
        const leftPx = x.getPixelForValue(centerX - 0.32);
        const rightPx = x.getPixelForValue(centerX + 0.32);
        const bandWidthPx = Math.abs(rightPx - leftPx);

        const q1Y = y.getPixelForValue(Math.log10(Math.max(1, quartiles.q1)));
        const medianY = y.getPixelForValue(Math.log10(Math.max(1, quartiles.median)));
        const q3Y = y.getPixelForValue(Math.log10(Math.max(1, quartiles.q3)));

        ctx.save();

        if (quartiles.q3 > quartiles.q1) {
          const topY = Math.min(q1Y, q3Y);
          const heightPx = Math.abs(q1Y - q3Y);
          ctx.fillStyle = `${colorHex}1F`;
          ctx.strokeStyle = `${colorHex}66`;
          ctx.lineWidth = 1;
          ctx.beginPath();
          ctx.roundRect(leftPx, topY, bandWidthPx, heightPx, 6);
          ctx.fill();
          ctx.stroke();
        }

        if (quartiles.median > 0) {
          ctx.strokeStyle = colorHex;
          ctx.lineWidth = 2;
          ctx.beginPath();
          ctx.moveTo(leftPx, medianY);
          ctx.lineTo(rightPx, medianY);
          ctx.stroke();
        }

        ctx.restore();
      });
    },
  }), [activeCohorts, cohorts, selectedMetric, cohortXMap]);

  return { logarithmicGridPlugin, cohortBandsPlugin };
}
