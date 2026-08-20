// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useMemo } from 'react';
import type { BubbleDataPoint, ChartData, ChartDataset, ChartOptions, Plugin, Scale } from 'chart.js';
import type { PortfolioImpactResponse, ComparisonPeriod, TenantType } from '@types';
import { formatCompact, formatNumber } from '@utils';
import { ChartPanel } from '../common/charts/ChartPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { chartColor, chartTick, createHtmlTooltip, horizontalGrid, matrixQuadrantsPlugin, referenceLinesPlugin, tenantMarkerPlugin } from '../common/charts/chartJs';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';

const TYPE_COLORS: Record<string, [string, string]> = {
  B2C: ['--color-chart-1', '#0ea5e9'],
  Mixed: ['--color-chart-2', '#8b5cf6'],
  B2B: ['--color-chart-3', '#2563eb'],
};

const SYMLOG_C = 0.5;
const symlog = (v: number) => Math.sign(v) * Math.log10(1 + Math.abs(v) * SYMLOG_C);
const symlogInverse = (v: number) => Math.sign(v) * (Math.pow(10, Math.abs(v)) - 1) / SYMLOG_C;

function generateSymlogTicks(axis: Scale) {
  const tickValues = [0, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000];
  const allTicks = [...tickValues.map(t => -t).reverse().slice(0, -1), ...tickValues];
  
  let dataMin = Infinity;
  let dataMax = -Infinity;
  
  axis.chart.data.datasets.forEach(ds => {
    ds.data.forEach(p => {
      const point = p as BubbleDataPoint;
      if (!point) return;
      const v = point.y;
      if (typeof v === 'number') {
        if (v < dataMin) dataMin = v;
        if (v > dataMax) dataMax = v;
      }
    });
  });

  if (dataMin === Infinity) {
    dataMin = -1;
    dataMax = 1;
  }

  const range = dataMax - dataMin;
  const paddedMin = dataMin - range * 0.01;
  const paddedMax = dataMax + range * 0.01;
  
  let firstIdx = 0;
  while (firstIdx < allTicks.length && symlog(allTicks[firstIdx]) <= paddedMin) firstIdx++;
  firstIdx = Math.max(0, firstIdx - 1);
  
  let lastIdx = allTicks.length - 1;
  while (lastIdx >= 0 && symlog(allTicks[lastIdx]) >= paddedMax) lastIdx--;
  lastIdx = Math.min(allTicks.length - 1, lastIdx + 1);
  
  const ticks = allTicks.slice(firstIdx, lastIdx + 1);
  axis.ticks = ticks.map(v => ({ value: symlog(v) }));
  
  if (ticks.length > 0) {
    axis.min = symlog(ticks[0]);
    axis.max = symlog(ticks[ticks.length - 1]);
  }
}

function calculateMedian(values: number[]): number {
  if (values.length === 0) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 !== 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

export const PortfolioImpactMatrixChart = memo(function PortfolioImpactMatrixChart({ isLoading, isError, isStale, portfolioImpact, comparison, onTenantSelect, className }: {
  isLoading?: boolean; isError?: boolean; isStale?: boolean; portfolioImpact: PortfolioImpactResponse; comparison?: ComparisonPeriod;
  onTenantSelect?: (tenantId: string) => void; className?: string;
}) {
  const points = portfolioImpact.tenants;
  const isEmpty = points.length === 0;
  
  const bubbles: BubbleDataPoint[] = points.map((point) => ({
    x: point.portfolioSharePercentage ?? 0,
    y: symlog(point.growthPercentage),
    r: 15,
  }));

  const dataset = {
    label: 'Tenants',
    data: bubbles,
    backgroundColor: points.map(point => {
      const [variable, fallback] = TYPE_COLORS[point.type] || TYPE_COLORS.Mixed;
      return chartColor(variable, fallback) + 'B3';
    }),
    borderColor: '#fff',
    borderWidth: 2,
    hoverBorderWidth: 3,
    tenantMeta: points,
  } as ChartDataset<'bubble', BubbleDataPoint[]> & { tenantMeta: typeof points };

  const data: ChartData<'bubble'> = { datasets: [dataset] };

  const splitX = portfolioImpact.medianPortfolioShare ?? 0;
  const splitY = symlog(0);

  const cohortMedians = useMemo(() => {
    if (isEmpty) return [];
    const presentTypes: TenantType[] = ['B2B', 'B2C', 'Mixed'];
    
    return presentTypes.map(type => {
      const typePoints = points.filter(p => p.type === type);
      if (typePoints.length === 0) return null;

      const shares = typePoints.map(p => p.portfolioSharePercentage ?? 0);
      const growths = typePoints.map(p => p.growthPercentage);

      const medianShare = calculateMedian(shares);
      const medianGrowth = calculateMedian(growths);

      return {
        type,
        medianShare,
        medianGrowth,
        medianGrowthSymlog: symlog(medianGrowth),
        count: typePoints.length,
      };
    }).filter((item): item is NonNullable<typeof item> => item !== null);
  }, [points, isEmpty]);

  const cohortMedianLinesPlugin = useMemo<Plugin<'bubble'>>(() => ({
    id: 'cohortMedianLines',
    beforeDatasetsDraw(chart) {
      const { ctx, chartArea, scales: { x, y } } = chart;
      if (!x || !y || !chartArea) return;

      cohortMedians.forEach(({ type, medianShare, medianGrowthSymlog }) => {
        const [variable, fallback] = TYPE_COLORS[type] || TYPE_COLORS.Mixed;
        const colorHex = chartColor(variable, fallback);

        const xPx = x.getPixelForValue(medianShare);
        const yPx = y.getPixelForValue(medianGrowthSymlog);

        if (xPx >= chartArea.left && xPx <= chartArea.right && yPx >= chartArea.top && yPx <= chartArea.bottom) {
          ctx.save();
          ctx.strokeStyle = `${colorHex}80`;
          ctx.lineWidth = 1.5;
          ctx.setLineDash([4, 4]);

          ctx.beginPath();
          ctx.moveTo(xPx, chartArea.top);
          ctx.lineTo(xPx, chartArea.bottom);
          ctx.stroke();

          ctx.beginPath();
          ctx.moveTo(chartArea.left, yPx);
          ctx.lineTo(chartArea.right, yPx);
          ctx.stroke();

          ctx.fillStyle = colorHex;
          ctx.strokeStyle = '#ffffff';
          ctx.lineWidth = 2;
          ctx.setLineDash([]);
          ctx.beginPath();
          ctx.arc(xPx, yPx, 5, 0, Math.PI * 2);
          ctx.fill();
          ctx.stroke();

          ctx.restore();
        }
      });
    },
  }), [cohortMedians]);

  const options: ChartOptions<'bubble'> = {
    responsive: true, maintainAspectRatio: false, animation: false,
    onClick: (_event, elements) => {
      const tenant = elements[0] ? points[elements[0].index] : undefined;
      if (tenant?.tenantId) onTenantSelect?.(tenant.tenantId);
    },
    onHover: (event, elements) => {
      if (event.native) (event.native.target as HTMLElement).style.cursor = elements.length ? 'pointer' : 'default';
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        enabled: false,
        external: createHtmlTooltip(tooltip => {
          const point = points[tooltip.dataPoints[0]?.dataIndex];
          if (!point) return null;
          const [typeVariable, typeFallback] = TYPE_COLORS[point.type] || TYPE_COLORS.Mixed;
          return {
            title: point.tenantName,
            tag: { label: point.type, color: chartColor(typeVariable, typeFallback) },
            groups: [
              [
                { label: 'Current revenue', value: formatCompact(point.currentRevenue) },
                { label: 'Portfolio share', value: `${(point.portfolioSharePercentage ?? 0).toFixed(1)}%` },
              ],
              [
                {
                  label: 'Revenue growth',
                  value: `${point.growthPercentage > 0 ? '+' : ''}${point.growthPercentage.toFixed(1)}%`,
                  tone: point.growthPercentage > 0 ? 'positive' : point.growthPercentage < 0 ? 'negative' : 'default',
                },
                { label: 'Order volume', value: formatNumber(point.orderVolume) },
                {
                  label: 'Volume growth',
                  value: `${point.volumeGrowthPercentage > 0 ? '+' : ''}${point.volumeGrowthPercentage.toFixed(1)}%`,
                  tone: point.volumeGrowthPercentage > 0 ? 'positive' : point.volumeGrowthPercentage < 0 ? 'negative' : 'default',
                },
              ],
            ],
          };
        }),
      },
    },
    scales: {
      x: { 
        border: { display: false }, 
        grid: horizontalGrid, 
        title: { display: true, text: 'Portfolio revenue share (%) →', color: chartColor('--color-chart-label', '#475569'), font: { family: 'Manrope, sans-serif', size: 14, weight: 800 } }, 
        ticks: { ...chartTick(14), callback: value => `${Number(value).toFixed(1)}%` } 
      },
      y: { 
        border: { display: false }, 
        grid: horizontalGrid, 
        title: { display: true, text: 'Revenue growth (%) →', color: chartColor('--color-chart-label', '#475569'), font: { family: 'Manrope, sans-serif', size: 14, weight: 800 } }, 
        afterBuildTicks: generateSymlogTicks,
        ticks: { ...chartTick(14), callback: value => `${symlogInverse(Number(value)).toFixed(0)}%` } 
      },
    },
  };

  const chartPlugins = useMemo(() => [
    matrixQuadrantsPlugin(splitX, splitY, {
      topLeft: 'Emerging Drivers',
      topRight: 'Core Expansion',
      bottomLeft: 'Long-Tail Churn',
      bottomRight: 'Core Erosion',
    }),
    referenceLinesPlugin(splitX, splitY),
    cohortMedianLinesPlugin,
    tenantMarkerPlugin,
  ], [splitX, splitY, cohortMedianLinesPlugin]);

  return (
    <ChartPanel isLoading={isLoading} isError={isError} isStale={isStale} title="Portfolio Impact Matrix" comparison={comparison}
      className={className || 'h-full'} bodyClassName={isEmpty ? 'flex items-center justify-center' : 'flex-1 min-h-0 flex flex-col'}
    >
      {isEmpty ? <EmptyState message="No previous-period baseline data" variant="minimal" /> : (
        <div className="flex-1 min-h-0 w-full flex flex-col relative select-none">
          <div className="flex-1 min-h-0 relative">
            <ChartJsCanvas type="bubble" data={data} options={options} plugins={chartPlugins} />
          </div>

          {cohortMedians.length > 0 && (
            <div className="flex flex-wrap items-center justify-around gap-x-6 gap-y-1.5 px-4 pt-2 border-t border-outline-variant/30 shrink-0 z-20">
              {cohortMedians.map(({ type, medianShare, medianGrowth }) => {
                const [variable, fallback] = TYPE_COLORS[type] || TYPE_COLORS.Mixed;
                const colorHex = chartColor(variable, fallback);

                return (
                  <div key={type} className="flex items-center gap-1 text-sm">
                    <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: colorHex }} />
                    <span className="font-bold text-on-surface">{type} median:</span>
                    <span className="font-semibold text-on-surface border-r-2 border-slate-400 pr-1 ">
                      {medianGrowth > 0 ? '+' : ''}{medianGrowth.toFixed(1)}% growth
                    </span>
                    <span className="font-semibold text-on-surface">
                      {medianShare.toFixed(1)}% share 
                    </span>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </ChartPanel>
  );
});
