// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useMemo } from 'react';
import type { BubbleDataPoint, ChartData, ChartDataset, ChartOptions } from 'chart.js';
import type { RevenueEfficiencyResponse, ComparisonPeriod } from '@types';
import { formatCompact, formatCurrency } from '@utils';
import { ChartPanel } from '../common/charts/ChartPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { chartColor, chartTick, createHtmlTooltip, horizontalGrid, matrixQuadrantsPlugin, referenceLinesPlugin, scaleBubbleRadii, tenantMarkerPlugin } from '../common/charts/chartJs';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';

const TYPE_COLORS: Record<string, [string, string]> = {
  B2C: ['--color-chart-1', '#0ea5e9'],
  Mixed: ['--color-chart-2', '#8b5cf6'],
  B2B: ['--color-chart-3', '#51B5B9'],
};

export const RevenueEfficiencyChart = memo(function RevenueEfficiencyChart({
  isLoading, isStale, response, comparison, onTenantSelect, className,
}: {
  isLoading?: boolean; isStale?: boolean; response: RevenueEfficiencyResponse;
  comparison?: ComparisonPeriod; onTenantSelect?: (tenantId: string) => void; className?: string;
}) {
  const tenants = useMemo(
    () => response.tenants.filter(tenant => tenant.orderVolume > 0 && tenant.averageOrderValue > 0),
    [response.tenants],
  );
  const isEmpty = tenants.length === 0;
  const radii = useMemo(() => scaleBubbleRadii(tenants.map(tenant => tenant.portfolioSharePercentage), 5, 22), [tenants]);
  const points: BubbleDataPoint[] = tenants.map((tenant, index) => ({
    x: tenant.orderVolume,
    y: tenant.averageOrderValue,
    r: radii[index],
  }));
  const dataset = {
    label: 'Tenants',
    data: points,
    backgroundColor: tenants.map(tenant => {
      const [variable, fallback] = TYPE_COLORS[tenant.type] || TYPE_COLORS.Mixed;
      return chartColor(variable, fallback) + 'B3';
    }),
    borderColor: '#fff',
    borderWidth: 2,
    hoverBorderWidth: 3,
    tenantMeta: tenants,
  } as ChartDataset<'bubble', BubbleDataPoint[]> & { tenantMeta: typeof tenants };
  const data: ChartData<'bubble'> = { datasets: [dataset] };
  const options: ChartOptions<'bubble'> = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    onClick: (_event, elements) => {
      const tenant = elements[0] ? tenants[elements[0].index] : undefined;
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
          const tenant = tenants[tooltip.dataPoints[0]?.dataIndex];
          if (!tenant) return null;
          const [typeVariable, typeFallback] = TYPE_COLORS[tenant.type] || TYPE_COLORS.Mixed;
          return {
            title: tenant.tenantName,
            tag: { label: tenant.type, color: chartColor(typeVariable, typeFallback) },
            groups: [
              [
                { label: 'Average Order Value', value: formatCurrency(tenant.averageOrderValue) },
                { label: 'Order Volume', value: formatCompact(tenant.orderVolume) },
                { label: 'Portfolio Share', value: `${tenant.portfolioSharePercentage.toFixed(1)}%` },
              ],
              [{
                label: 'Growth Velocity',
                value: `${tenant.growthVelocity > 0 ? '+' : ''}${tenant.growthVelocity.toFixed(1)}%`,
                tone: tenant.growthVelocity > 0 ? 'positive' : tenant.growthVelocity < 0 ? 'negative' : 'default',
              }],
            ],
          };
        }),
      },
    },
    scales: {
      x: { border: { display: false }, grid: horizontalGrid, title: { display: true, text: 'Order Volume (Transactions) →', color: chartColor('--color-chart-label', '#475569'), font: { family: 'Manrope, sans-serif', size: 14, weight: 800 } }, ticks: { ...chartTick(14), callback: value => formatCompact(Number(value)) } },
      y: { type: 'logarithmic', border: { display: false }, grid: horizontalGrid, title: { display: true, text: 'AOV (SEK, log scale) →', color: chartColor('--color-chart-label', '#475569'), font: { family: 'Manrope, sans-serif', size: 14, weight: 800 } }, ticks: { ...chartTick(14), callback: value => formatCompact(Number(value)) } },
    },
  };

  return (
    <ChartPanel isLoading={isLoading} isStale={isStale} title="Revenue Efficiency Matrix" comparison={comparison}
      className={className || 'h-full'} bodyClassName={isEmpty ? 'flex items-center justify-center' : 'flex-1 min-h-0'}
      legend={<span className="text-sm font-bold text-on-surface-variant uppercase tracking-wide bg-surface-container-low px-3 py-1.5 rounded">Size = % of Portfolio Revenue</span>}
    >
      {isEmpty ? <EmptyState message="No data available" variant="minimal" /> : (
        <div className="absolute inset-0"><ChartJsCanvas type="bubble" data={data} options={options} plugins={[matrixQuadrantsPlugin(response.medianOrderVolume, response.globalAverageOrderValue), referenceLinesPlugin(response.medianOrderVolume, response.globalAverageOrderValue), tenantMarkerPlugin]} /></div>
      )}
    </ChartPanel>
  );
});
