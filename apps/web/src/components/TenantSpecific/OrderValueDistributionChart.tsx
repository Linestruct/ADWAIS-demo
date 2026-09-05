// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo } from 'react';
import type { ChartData, ChartOptions, Plugin } from 'chart.js';
import type { OrderBin } from '@types';
import { formatNumber } from '@utils';
import { ChartPanel } from '../common/charts/ChartPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';
import { chartColor, chartLegendLabels, chartTick, createHtmlTooltip, horizontalGrid } from '../common/charts/chartJs';

type MixedChart = 'bar' | 'line';

const thresholdLines: Plugin<MixedChart> = {
  id: 'distributionThresholds',
  beforeDatasetsDraw(chart) {
    const scale = chart.scales.right;
    if (!scale) return;
    const { ctx, chartArea } = chart;
    ctx.save();
    ctx.strokeStyle = '#ef444499';
    ctx.setLineDash([3, 3]);
    [70, 80].forEach(value => {
      const y = scale.getPixelForValue(value);
      ctx.beginPath(); ctx.moveTo(chartArea.left, y); ctx.lineTo(chartArea.right, y); ctx.stroke();
    });
    ctx.restore();
  },
};

export const OrderValueDistributionChart = memo(function OrderValueDistributionChart({ isLoading, isError, isStale, bins, className }: {
  isLoading?: boolean; isError?: boolean; isStale?: boolean; bins: OrderBin[]; className?: string;
}) {
  const data: ChartData<MixedChart, number[], string> = {
    labels: bins.map(bin => bin.binLabel),
    datasets: [
      { type: 'bar', label: 'Volume (Orders)', data: bins.map(bin => bin.orderCount), yAxisID: 'left', backgroundColor: chartColor('--color-brand-btn-primary', '#2563eb') + '99', borderRadius: 4, maxBarThickness: 60 },
      { type: 'line', label: 'Density Curve', data: bins.map(bin => bin.kdeDensity), yAxisID: 'left', borderColor: '#0d9488', borderWidth: 2.5, pointRadius: 0, pointHoverRadius: 7, pointHoverBackgroundColor: '#0d9488', pointHoverBorderColor: '#fff', pointHoverBorderWidth: 3, tension: 0.35 },
      { type: 'line', label: 'Cumulative %', data: bins.map(bin => bin.cumulativePercentage), yAxisID: 'right', borderColor: '#f97316', borderWidth: 2, borderDash: [4, 4], pointRadius: 0, pointHoverRadius: 6, pointHoverBackgroundColor: '#f97316', pointHoverBorderColor: '#fff', pointHoverBorderWidth: 2, tension: 0.35 },
    ],
  };
  const options: ChartOptions<MixedChart> = {
    responsive: true, maintainAspectRatio: false, animation: false, interaction: { mode: 'index', intersect: false },
    layout: { padding: { top: 12, bottom: 4 } },
    plugins: {
      legend: { position: 'bottom', labels: chartLegendLabels },
      tooltip: {
        enabled: false,
        external: createHtmlTooltip(tooltip => {
          const bin = bins[tooltip.dataPoints[0]?.dataIndex];
          if (!bin) return null;
          return {
            title: bin.binLabel,
            groups: [
              [
                { label: 'Volume', value: formatNumber(bin.orderCount), tone: 'primary' },
                { label: 'Density (KDE)', value: formatNumber(bin.kdeDensity), tone: 'primary' },
              ],
              [{ label: 'Cumulative', value: `${bin.cumulativePercentage.toFixed(1)}%`, tone: 'warning' }],
            ],
          };
        }),
      },
    },
    scales: {
      x: { border: { display: false }, grid: { display: false }, ticks: { ...chartTick(14, 700), autoSkip: true, maxTicksLimit: 8, sampleSize: 8, minRotation: 0, maxRotation: 45, padding: 6 } },
      left: { position: 'left', border: { display: false }, grid: horizontalGrid, ticks: chartTick(14, 700) },
      right: { position: 'right', min: 0, max: 100, border: { display: false }, grid: { display: false }, ticks: { ...chartTick(14, 700), color: '#f97316', callback: value => `${value}%` } },
    },
  };
  return (
    <ChartPanel isLoading={isLoading} isError={isError} isStale={isStale} title="Order Distribution & Shipping Threshold" className={className || ''} bodyClassName={!bins.length ? 'flex items-center justify-center' : ''}>
      {!bins.length ? <EmptyState message="No order distribution data available." variant="minimal" /> : <div className="absolute inset-0"><ChartJsCanvas type="bar" data={data} options={options} plugins={[thresholdLines]} /></div>}
    </ChartPanel>
  );
});
