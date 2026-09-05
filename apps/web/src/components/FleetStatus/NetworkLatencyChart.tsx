// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useMemo } from 'react';
import type { ChartData, ChartOptions, Plugin } from 'chart.js';
import type { LatencyPoint, ComparisonPeriod } from '@types';
import { formatChartLabel, inferBinSize } from '@utils';
import { ChartPanel } from '../common/charts/ChartPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';
import { chartColor, chartTick, createHtmlTooltip, horizontalGrid } from '../common/charts/chartJs';

function formatLatency(value: number | null | undefined): string {
  return value == null ? 'N/A' : `${Math.round(value)}ms`;
}

type LatencyChartPoint = LatencyPoint & { label: string; tooltipTitle: string };

function latencyGapPlugin(points: LatencyChartPoint[]): Plugin<'line'> {
  return {
    id: 'latencyGaps',
    beforeDatasetsDraw(chart) {
      const { ctx, scales } = chart;
      let index = 0;
      while (index < points.length) {
        if (points[index].currentState !== 'NoSamples') {
          index += 1;
          continue;
        }
        const gapStart = index;
        while (index < points.length && points[index].currentState === 'NoSamples') index += 1;
        const leftIndex = gapStart - 1;
        const rightIndex = index;
        if (leftIndex < 0 || rightIndex >= points.length) continue;

        const leftValue = points[leftIndex].average;
        const rightValue = points[rightIndex].average;
        if (leftValue == null || rightValue == null) continue;
        const x1 = scales.x.getPixelForValue(leftIndex);
        const y1 = scales.y.getPixelForValue(leftValue);
        const x2 = scales.x.getPixelForValue(rightIndex);
        const y2 = scales.y.getPixelForValue(rightValue);
        const dx = x2 - x1;
        const dy = y2 - y1;
        const length = Math.hypot(dx, dy) || 1;
        const alongX = dx / length;
        const alongY = dy / length;
        const perpendicularX = -alongY;
        const perpendicularY = alongX;
        const middleX = (x1 + x2) / 2;
        const middleY = (y1 + y2) / 2;

        ctx.save();
        ctx.strokeStyle = chartColor('--color-chart-prev-line', '#94a3b8');
        ctx.globalAlpha = 0.5;
        ctx.lineWidth = 2;
        ctx.setLineDash([]);
        ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke();

        [-5, 5].forEach(offset => {
          const markerX = middleX + alongX * offset;
          const markerY = middleY + alongY * offset;
          ctx.globalAlpha = 1;
          ctx.strokeStyle = chartColor('--color-surface', '#fff');
          ctx.lineWidth = 5;
          ctx.beginPath();
          ctx.moveTo(markerX - perpendicularX * 6, markerY - perpendicularY * 6);
          ctx.lineTo(markerX + perpendicularX * 6, markerY + perpendicularY * 6);
          ctx.stroke();
          ctx.strokeStyle = chartColor('--color-chart-prev-line', '#64748b');
          ctx.lineWidth = 2;
          ctx.stroke();
        });
        ctx.restore();
      }
    },
  };
}

export const NetworkLatencyChart = memo(function NetworkLatencyChart({ isLoading, isError, isStale, points, title = 'Network Latency', className, comparison = 'Preceding' }: {
  isLoading?: boolean; isError?: boolean; isStale?: boolean; points: LatencyPoint[]; title?: string; className?: string; comparison?: ComparisonPeriod;
}) {
  const chartData = useMemo(() => {
    if (!points.length) return [];

    // Weekly fold for 365D / large YTD — reduces visual noise at small widths.
    // Only applied for daily data (non-hourly) with enough points to warrant it.
    const needsFold = points.length > 90 &&
      points.length > 1 &&
      (new Date(points[1].timestamp).getTime() - new Date(points[0].timestamp).getTime()) < 2 * 24 * 60 * 60 * 1000;

    const foldedPoints: LatencyPoint[] = needsFold ? (() => {
      const avg = (vals: (number | null)[]): number | null => {
        const nonNull = vals.filter((v): v is number => v != null);
        return nonNull.length ? nonNull.reduce((s, v) => s + v, 0) / nonNull.length : null;
      };
      const result: LatencyPoint[] = [];
      for (let i = 0; i < points.length; i += 7) {
        const chunk = points.slice(i, i + 7);
        result.push({
          timestamp: chunk[0].timestamp,
          average: avg(chunk.map(p => p.average)),
          previousAverage: avg(chunk.map(p => p.previousAverage)),
          p10: avg(chunk.map(p => p.p10)),
          p90: avg(chunk.map(p => p.p90)),
          currentState: chunk.some(point => point.currentState === 'Observed') ? 'Observed' : 'NoSamples',
          previousState: chunk.some(point => point.previousState === 'Observed') ? 'Observed' : 'NoSamples',
        });
      }
      return result;
    })() : points;

    const formatDate = (ts: string) =>
      new Date(ts).toLocaleString('en-SE', { month: 'short', day: 'numeric', timeZone: 'Europe/Stockholm' });

    const binSize = needsFold ? 'day' : inferBinSize(foldedPoints.map(point => point.timestamp), foldedPoints.length <= 24);
    return foldedPoints.map((point, index) => {
      const label = formatChartLabel(point.timestamp, binSize, index);
      const tooltipTitle = needsFold
        ? (() => {
            const weekEndMs = new Date(point.timestamp).getTime() + 6 * 24 * 60 * 60 * 1000;
            const weekEnd = formatDate(new Date(weekEndMs).toISOString());
            return `${label} – ${weekEnd}`;
          })()
        : label;
      return { ...point, label, tooltipTitle };
    });
  }, [points]);

  const hasCurrentCollectionGap = useMemo(() => {
    let index = 0;
    while (index < chartData.length) {
      if (chartData[index].currentState !== 'NoSamples') {
        index += 1;
        continue;
      }

      const gapStart = index;
      while (index < chartData.length && chartData[index].currentState === 'NoSamples') index += 1;
      if (gapStart > 0 && index < chartData.length) return true;
    }
    return false;
  }, [chartData]);

  const hasAnyLatencyData = chartData.some(point =>
    point.currentState === 'Observed' || point.previousState === 'Observed');

  const yAxisMax = useMemo(() => {
    const maximum = Math.max(0, ...points.map(point => Math.max(point.average || 0, (((point.p90 || 0) + (point.previousAverage || 0) + (point.average || 0)) / 3) * 1.05 || 0)));
    return maximum > 0 ? Math.ceil(maximum * 1) : undefined;
  }, [points]);
  
  const data: ChartData<'line', (number | null)[], string> = {
    labels: chartData.map(point => point.label),
    datasets: [
      { label: '10th Percentile', data: chartData.map(point => point.p10), borderColor: 'transparent', pointRadius: 0 },
      { label: '90th Percentile', data: chartData.map(point => point.p90), borderColor: 'transparent', backgroundColor: chartColor('--color-brand-btn-primary', '#2563eb') + '26', pointRadius: 0, fill: '-1', tension: 0.3 },
      { label: 'Previous Period', data: chartData.map(point => point.previousAverage), borderColor: chartColor('--color-chart-prev-line', '#94a3b8'), borderWidth: 2, borderDash: [6, 6], pointRadius: 0, pointHoverRadius: 5, pointHoverBackgroundColor: chartColor('--color-chart-prev-line', '#94a3b8'), pointHoverBorderColor: '#fff', pointHoverBorderWidth: 2, tension: 0.3, spanGaps: true },
      { label: 'Current Period', data: chartData.map(point => point.average), borderColor: chartColor('--color-brand-btn-primary', '#2563eb'), borderWidth: 4, pointRadius: 0, pointHoverRadius: 7, pointHoverBackgroundColor: chartColor('--color-brand-btn-primary', '#2563eb'), pointHoverBorderColor: '#fff', pointHoverBorderWidth: 3, tension: 0.3, spanGaps: false },
    ],
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
            title: point.tooltipTitle,
            groups: [
              [
                { label: 'Current avg', value: point.currentState === 'NoSamples' ? 'No samples in bucket' : formatLatency(point.average), tone: point.currentState === 'NoSamples' ? 'muted' : 'primary' },
                { label: 'Previous avg', value: point.previousState === 'NoSamples' ? 'No samples in bucket' : formatLatency(point.previousAverage), tone: 'muted' },
              ],
              [
                { label: '90th percentile', value: formatLatency(point.p90), tone: 'negative' },
                { label: '10th percentile', value: formatLatency(point.p10), tone: 'positive' },
              ],
            ],
          };
        }),
      },
    },
    scales: {
      x: { border: { display: false }, grid: { display: false }, ticks: { ...chartTick(12, 700), autoSkip: true, maxRotation: 0, padding: 10 } },
      y: { max: yAxisMax, border: { display: false }, grid: horizontalGrid, ticks: { ...chartTick(12), callback: value => `${value}ms` } },
    },
  };
  
  const legend = (
    <div className="flex flex-wrap justify-end gap-x-5 gap-y-1.5 text-sm font-bold text-on-surface-variant uppercase tracking-wide bg-surface-container-low px-3 py-1.5 rounded-xl border border-outline-variant">
      <div className="flex items-center gap-2">
        <div className="w-3 h-3 rounded-full bg-brand-btn-primary" />
        <span>Current</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="w-3.5 h-3.5 rounded bg-brand-btn-primary/25 border border-brand-btn-primary/40" />
        <span>P10–P90 Band</span>
      </div>
      <div className="flex items-center gap-2">
        <div className="w-4 border-t-2 border-outline border-dashed" />
        <span>Previous</span>
      </div>
      {hasCurrentCollectionGap && (
        <div className="flex items-center gap-2">
          <span className="relative inline-flex w-5 justify-center overflow-hidden text-base leading-none text-outline">//</span>
          <span>Missing samples</span>
        </div>
      )}
    </div>
  );

  return (
    <ChartPanel isLoading={isLoading} isError={isError} isStale={isStale} title={title} comparison={comparison} legend={legend} className={className} bodyClassName={!hasAnyLatencyData ? 'flex items-center justify-center' : ''}>
      {!hasAnyLatencyData ? <EmptyState message="No latency samples in the selected period" variant="minimal" /> : <div className="absolute inset-0"><ChartJsCanvas type="line" data={data} options={options} plugins={[latencyGapPlugin(chartData)]} /></div>}
    </ChartPanel>
  );
});
