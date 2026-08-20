// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useMemo } from 'react';
import type { ChartData, ChartOptions } from 'chart.js';
import type { AccumulatedRevenuePointDto, ComparisonPeriod } from '@types';
import { ChartPanel } from '../common/charts/ChartPanel';
import { foldDailySeries, formatCurrency, formatCompact } from '@utils';
import { formatDateTime } from '../../utils/dateTime';
import { EmptyState } from '../common/ui/EmptyState';
import { chartColor, chartLegendLabels, chartTick, createHtmlTooltip, horizontalGrid } from '../common/charts/chartJs';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';

interface AccumulatedRevenueChartProps {
  isLoading?: boolean;
  isError?: boolean;
  isStale?: boolean;
  points: AccumulatedRevenuePointDto[];
  comparison?: ComparisonPeriod;
  className?: string;
}

type MixedChart = 'bar' | 'line';

export const AccumulatedRevenueChart = memo(function AccumulatedRevenueChart({ isLoading, isError, isStale, points, comparison, className }: AccumulatedRevenueChartProps) {
  const { chartData, isSubDaily } = useMemo(() => {
    if (!points.length) return { chartData: [], isSubDaily: false };

    const formatDate = (ts: string | number) =>
      formatDateTime(ts, { month: 'short', day: 'numeric', timeZone: 'Europe/Stockholm' }, 'en-SE');
    const formatTime = (ts: string | number) =>
      formatDateTime(ts, { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Stockholm' }, 'en-SE');

    // Weekly fold for 365D / large YTD — reduces visual noise at small widths.
    const { points: workingPoints } = foldDailySeries(points, chunk => {
      const last = chunk[chunk.length - 1];
      const sum = (key: keyof typeof chunk[0]) =>
        chunk.reduce((acc, point) => acc + (point[key] as number ?? 0), 0);

      return {
        timestamp: chunk[0].timestamp,
        currentRevenue: sum('currentRevenue'),
        currentRevenueB2C: sum('currentRevenueB2C'),
        currentRevenueB2B: sum('currentRevenueB2B'),
        currentRevenueMixed: sum('currentRevenueMixed'),
        previousRevenue: sum('previousRevenue'),
        currentAccumulated: last.currentAccumulated,
        previousAccumulated: last.previousAccumulated,
      };
    });

    // Re-derive gap from working points so isWeekly fires for folded data.
    const workingGapMs = workingPoints.length > 1
      ? new Date(workingPoints[1].timestamp).getTime() - new Date(workingPoints[0].timestamp).getTime()
      : 0;
    const isHourly   = workingGapMs > 0 && workingGapMs < 2 * 60 * 60 * 1000;
    const isSubDaily = workingGapMs >= 2 * 60 * 60 * 1000 && workingGapMs < 24 * 60 * 60 * 1000;
    const isWeekly   = workingGapMs >= 5 * 24 * 60 * 60 * 1000;

    const chartData = workingPoints.map((point, i) => {
      const ts = point.timestamp;
      let label: string;
      let tooltipTitle: string;

      if (isHourly) {
        label = formatTime(ts);
        tooltipTitle = formatDateTime(ts, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Stockholm' }, 'en-SE');
      } else if (isSubDaily) {
        label = formatDate(ts);
        const binEndMs = new Date(ts).getTime() + workingGapMs;
        tooltipTitle = `${formatDate(ts)} ${formatTime(ts)} – ${formatTime(binEndMs)}`;
      } else if (isWeekly) {
        const weekStart = formatDate(ts);
        const nextTs = workingPoints[i + 1]?.timestamp;
        const weekEnd = nextTs
          ? formatDate(new Date(new Date(nextTs).getTime() - 24 * 60 * 60 * 1000).toISOString())
          : weekStart;
        label = weekStart;
        tooltipTitle = weekEnd !== weekStart ? `${weekStart} – ${weekEnd}` : weekStart;
      } else {
        label = formatDate(ts);
        tooltipTitle = label;
      }

      return { ...point, label, tooltipTitle };
    });

    return { chartData, isSubDaily };
  }, [points]);



  const data: ChartData<MixedChart, (number | null)[], string> = {
    labels: chartData.map(point => point.label),
    datasets: [
      {
        type: 'bar',
        label: 'B2C Revenue',
        data: chartData.map(point => point.currentRevenueB2C),
        yAxisID: 'left',
        stack: 'revenue',
        backgroundColor: chartColor('--color-chart-1', '#0ea5e9') + 'a0',
        borderRadius: 0,
        maxBarThickness: 20,
        categoryPercentage: 0.9,
        barPercentage: 0.82,
        borderSkipped: false,
      },
      {
        type: 'bar',
        label: 'B2B Revenue',
        data: chartData.map(point => point.currentRevenueB2B),
        yAxisID: 'left',
        stack: 'revenue',
        backgroundColor: chartColor('--color-chart-3', '#2563eb') + 'a0',
        borderRadius: 0,
        maxBarThickness: 20,
        categoryPercentage: 0.9,
        barPercentage: 0.82,
        borderSkipped: false,
      },
      {
        type: 'bar',
        label: 'Mixed Revenue',
        data: chartData.map(point => point.currentRevenueMixed),
        yAxisID: 'left',
        stack: 'revenue',
        backgroundColor: chartColor('--color-chart-2', '#8b5cf6') + 'a0',
        borderRadius: 0,
        maxBarThickness: 20,
        categoryPercentage: 0.9,
        barPercentage: 0.82,
        borderSkipped: false,
      },
      {
        type: 'line',
        label: 'Previous Accumulated',
        data: chartData.map(point => point.previousAccumulated),
        yAxisID: 'right',
        borderColor: chartColor('--color-chart-prev-line', '#94a3b8'),
        borderWidth: 2,
        borderDash: [4, 4],
        pointRadius: 0,
        pointHoverRadius: 6,
        pointHoverBackgroundColor: chartColor('--color-chart-prev-line', '#94a3b8'),
        pointHoverBorderColor: '#fff',
        pointHoverBorderWidth: 2,
        tension: 0.3,
      },
      {
        type: 'line',
        label: 'Current Accumulated (Right)',
        data: chartData.map(point => point.currentAccumulated),
        yAxisID: 'right',
        borderColor: chartColor('--color-brand-btn-primary', '#2563eb'),
        borderWidth: 2,
        pointRadius: 0,
        pointHoverRadius: 7,
        pointHoverBackgroundColor: chartColor('--color-brand-btn-primary', '#2563eb'),
        pointHoverBorderColor: '#fff',
        pointHoverBorderWidth: 3,
        tension: 0.3,
      },
    ],
  };

  const options: ChartOptions<MixedChart> = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    interaction: { mode: 'index', intersect: false },
    layout: { padding: { top: 10, right: 4 } },
    plugins: {
      legend: { position: 'bottom', labels: chartLegendLabels },
      tooltip: {
        enabled: false,
        external: createHtmlTooltip(tooltip => {
          const index = tooltip.dataPoints[0]?.dataIndex;
          const point = chartData[index];
          if (!point) return null;
          return {
            title: point.tooltipTitle,
            groups: [
              [
                { label: 'Total Revenue', value: formatCurrency(point.currentRevenue), tone: 'primary' },
                { label: 'B2C Revenue', value: formatCurrency(point.currentRevenueB2C) },
                { label: 'B2B Revenue', value: formatCurrency(point.currentRevenueB2B) },
                { label: 'Mixed Revenue', value: formatCurrency(point.currentRevenueMixed) },
              ],
              [
                { label: 'Current Accumulated', value: formatCurrency(point.currentAccumulated), tone: 'primary' },
                { label: 'Previous Accumulated', value: formatCurrency(point.previousAccumulated), tone: 'muted' },
              ],
            ],
          };
        }),
      },
    },
    scales: {
      x: {
        stacked: true,
        border: { display: false },
        grid: { display: false },
        ticks: {
          ...chartTick(14),
          autoSkip: !isSubDaily,
          maxRotation: 0,
          // For sub-daily (T7) data: suppress repeated date labels within the same day.
          ...(isSubDaily && {
            callback: function(_value, index) {
              const label = chartData[index]?.label ?? '';
              const prevLabel = chartData[index - 1]?.label;
              return label !== prevLabel ? label : '';
            },
          }),
        },
      },
      left: {
        position: 'left',
        stacked: true,
        border: { display: false },
        grid: horizontalGrid,
        ticks: { ...chartTick(14), callback: value => formatCompact(Number(value)) },
      },
      right: {
        position: 'right',
        border: { display: false },
        grid: { display: false },
        ticks: { ...chartTick(14), callback: value => formatCompact(Number(value)) },
      },
    },
  };

  return (
    <ChartPanel isLoading={isLoading} isError={isError} isStale={isStale}
      title="Revenue Performance"
      comparison={comparison}
      className={className}
      bodyClassName={points.length === 0 ? 'flex items-center justify-center' : ''}
    >
      {points.length === 0 ? (
        <EmptyState message="No revenue data available" variant="minimal" />
      ) : (
        <div className="absolute inset-0"><ChartJsCanvas type="bar" data={data} options={options} /></div>
      )}
    </ChartPanel>
  );
});
