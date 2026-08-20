// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useMemo } from 'react';
import type { ChartData, ChartOptions } from 'chart.js';
import type { CumulativeGrowthDeltaPoint, ComparisonPeriod } from '@types';
import { formatChartLabel, inferBinSize, formatCompact } from '@utils';
import { ChartPanel } from '../common/charts/ChartPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';
import { chartColor, chartTick, createHtmlTooltip, horizontalGrid } from '../common/charts/chartJs';

export const CumulativeGrowthDeltaChart = memo(function CumulativeGrowthDeltaChart({ isLoading, isError, isStale, points, comparison, className }: {
  isLoading?: boolean; isError?: boolean; isStale?: boolean; points: CumulativeGrowthDeltaPoint[]; comparison?: ComparisonPeriod; className?: string;
}) {
  const chartData = useMemo(() => {
    const binSize = inferBinSize(points.map(point => point.timestamp), points.length > 0 && points.length <= 24);
    return points.map((point, index) => ({ ...point, label: formatChartLabel(point.timestamp, binSize, index) }));
  }, [points]);
  const deltaColor = chartColor('--color-brand-btn-primary', '#2563eb');
  const data: ChartData<'line', number[], string> = {
    labels: chartData.map(point => point.label),
    datasets: [{
      label: 'Cumulative Growth Delta',
      data: chartData.map(point => point.cumulativeGrowthDelta),
      borderColor: deltaColor,
      borderWidth: 2.4,
      pointRadius: 0,
      pointHoverRadius: 7,
      pointHoverBackgroundColor: deltaColor,
      pointHoverBorderColor: '#fff',
      pointHoverBorderWidth: 3,
      stepped: 'after',
      tension: 0,
    }],
  };
  const options: ChartOptions<'line'> = {
    responsive: true, maintainAspectRatio: false, animation: false, interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: { display: false },
      tooltip: {
        enabled: false,
        external: createHtmlTooltip(tooltip => {
          const point = chartData[tooltip.dataPoints[0]?.dataIndex];
          if (!point) return null;
          return {
            title: point.label,
            groups: [
              [
                { label: 'Current Cumulative', value: `${formatCompact(point.currentCumulative)} SEK`, tone: 'primary' },
                { label: 'Previous Cumulative', value: `${formatCompact(point.previousCumulative)} SEK`, tone: 'muted' },
              ],
              [{
                label: 'Delta',
                value: `${point.cumulativeGrowthDelta > 0 ? '+' : ''}${formatCompact(point.cumulativeGrowthDelta)} SEK`,
                tone: point.cumulativeGrowthDelta > 0 ? 'positive' : point.cumulativeGrowthDelta < 0 ? 'negative' : 'default',
              }],
            ],
          };
        }),
      },
    },
    scales: {
      x: { border: { display: false }, grid: { display: false }, ticks: { ...chartTick(14, 700), autoSkip: true, maxRotation: 0 } },
      y: {
        border: { display: false },
        grid: horizontalGrid,
        ticks: {
          ...chartTick(14),
          callback: value => {
            const numericValue = Number(value);
            return `${numericValue > 0 ? '+' : ''}${formatCompact(numericValue)}`;
          },
        },
      },
    },
  };
  return (
    <ChartPanel isLoading={isLoading} isError={isError} isStale={isStale} title="Cumulative Growth Delta (Absolute)" comparison={comparison} className={className || ''} bodyClassName={!points.length ? 'flex items-center justify-center' : ''}>
      {!points.length ? <EmptyState message="No growth data available." variant="minimal" /> : <div className="absolute inset-0"><ChartJsCanvas type="line" data={data} options={options} /></div>}
    </ChartPanel>
  );
});
