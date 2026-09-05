// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useMemo } from 'react';
import type { ChartData, ChartOptions } from 'chart.js';
import type { NetGrowthAdditionPoint } from '@types';
import { foldDailySeries, formatChartLabel, inferBinSize, formatCompact, formatCurrency } from '@utils';
import { formatDateTime } from '../../utils/dateTime';
import { ChartPanel } from '../common/charts/ChartPanel';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';
import { EmptyState } from '../common/ui/EmptyState';
import { chartColor, chartTick, createHtmlTooltip, horizontalGrid } from '../common/charts/chartJs';

interface NetGrowthAdditionChartProps {
  isLoading?: boolean;
  isStale?: boolean;
  points: NetGrowthAdditionPoint[];
  className?: string;
}

export const NetGrowthAdditionChart = memo(function NetGrowthAdditionChart({
  isLoading,
  isStale,
  points,
  className,
}: NetGrowthAdditionChartProps) {
  const chartData = useMemo(() => {
    type DisplayPoint = NetGrowthAdditionPoint & { intervalEnd?: string };
    const sourcePoints: DisplayPoint[] = points.map(point => ({ ...point }));
    const { points: workingPoints, isFolded } = foldDailySeries(sourcePoints, chunk => ({
      timestamp: chunk[0].timestamp,
      intervalEnd: chunk[chunk.length - 1].timestamp,
      netGrowthAddition: chunk.reduce((sum, point) => sum + point.netGrowthAddition, 0),
    }));
    const binSize = inferBinSize(
      workingPoints.map(point => point.timestamp),
      workingPoints.length > 0 && workingPoints.length <= 48 && !isFolded,
    );

    return workingPoints.map((point, index) => {
      const label = formatChartLabel(point.timestamp, isFolded ? 'day' : binSize, index);
      const tooltipTitle = point.intervalEnd && point.intervalEnd !== point.timestamp
        ? `${label} – ${formatDateTime(point.intervalEnd, { month: 'short', day: 'numeric', timeZone: 'Europe/Stockholm' }, 'en-SE')}`
        : label;

      return { ...point, label, tooltipTitle };
    });
  }, [points]);

  const positiveColor = chartColor('--Rcolor-success', '#10b981');
  const negativeColor = chartColor('--color-error', '#ef4444');
  const neutralColor = chartColor('--color-outline', '#94a3b8');

  const data: ChartData<'bar', number[], string> = {
    labels: chartData.map(point => point.label),
    datasets: [{
      label: 'Revenue change',
      data: chartData.map(point => point.netGrowthAddition),
      backgroundColor: context => {
        const value = context.parsed.y ?? 0;
        return value > 0 ? positiveColor : value < 0 ? negativeColor : neutralColor;
      },
      hoverBackgroundColor: context => {
        const value = context.parsed.y ?? 0;
        return value > 0 ? positiveColor : value < 0 ? negativeColor : neutralColor;
      },
      borderRadius: 3,
      maxBarThickness: 44,
    }],
  };

  const options: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: { display: false },
      tooltip: {
        enabled: false,
        external: createHtmlTooltip(tooltip => {
          const point = chartData[tooltip.dataPoints[0]?.dataIndex];
          if (!point) return null;

          const change = point.netGrowthAddition;
          return {
            title: point.tooltipTitle,
            groups: [[{
              label: 'Change from previous interval',
              value: `${change > 0 ? '+' : ''}${formatCurrency(change)}`,
              tone: change > 0 ? 'positive' : change < 0 ? 'negative' : 'default',
            }]],
          };
        }),
      },
    },
    scales: {
      x: {
        border: { display: false },
        grid: { display: false },
        ticks: { ...chartTick(14, 700), autoSkip: true, maxRotation: 0 },
      },
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
    <ChartPanel
      isLoading={isLoading}
      isStale={isStale}
      title="Net Growth Addition"
      className={className || ''}
      bodyClassName={points.length === 0 ? 'flex items-center justify-center' : ''}
    >
      {points.length === 0 ? (
        <EmptyState message="No revenue change data available" variant="minimal" />
      ) : (
        <div className="absolute inset-0">
          <ChartJsCanvas type="bar" data={data} options={options} />
        </div>
      )}
    </ChartPanel>
  );
});
