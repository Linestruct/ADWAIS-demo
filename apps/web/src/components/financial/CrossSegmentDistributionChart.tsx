// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useMemo, useState } from 'react';
import type { ChartOptions } from 'chart.js';
import type { CrossSegmentDistributionResponse, TenantType } from '@types';
import { formatCompact } from '@utils';
import { ChartPanel } from '../common/charts/ChartPanel';
import { EmptyState } from '../common/ui/EmptyState';
import { Select } from '../common/ui/Select';
import { chartColor, chartTick, createHtmlTooltip, tenantMarkerPlugin } from '../common/charts/chartJs';
import { ChartJsCanvas } from '../common/charts/ChartJsCanvas';

import type { MetricType } from './crossSegment/crossSegmentTypes';
import { METRIC_OPTIONS, COHORT_COLORS } from './crossSegment/crossSegmentTypes';
import { getMetricValue, formatMetricValue, getGroupQuartiles, getOrdinalSuffix, getTenantPercentileRank } from './crossSegment/crossSegmentHelpers';
import { useCrossSegmentPlugins } from './crossSegment/crossSegmentPlugins';
import { useCrossSegmentBeeswarm } from './crossSegment/useCrossSegmentBeeswarm';

export const CrossSegmentDistributionChart = memo(function CrossSegmentDistributionChart({
  isLoading, isError, isStale, response, onTenantSelect, className,
}: {
  isLoading?: boolean; isError?: boolean; isStale?: boolean; response: CrossSegmentDistributionResponse;
  onTenantSelect?: (tenantId: string) => void; className?: string;
}) {
  const [selectedMetric, setSelectedMetric] = useState<MetricType>('aov');

  const tenants = useMemo(() => response.tenants ?? [], [response.tenants]);
  const cohorts = useMemo(() => response.cohorts ?? [], [response.cohorts]);
  const isEmpty = tenants.length === 0;

  const activeCohorts = useMemo(() => {
    const presentTypes = new Set(tenants.map(t => t.type));
    const list: TenantType[] = [];
    (['B2B', 'B2C', 'Mixed'] as TenantType[]).forEach(type => {
      if (presentTypes.has(type)) list.push(type);
    });
    return list.length > 0 ? list : (['B2B', 'B2C', 'Mixed'] as TenantType[]);
  }, [tenants]);

  const {
    cohortXMap,
    minLogVal,
    maxLogVal,
    tenantMetaList,
    data,
    majorTickValues,
  } = useCrossSegmentBeeswarm(tenants, activeCohorts, selectedMetric, isEmpty);

  const { logarithmicGridPlugin, cohortBandsPlugin } = useCrossSegmentPlugins(activeCohorts, cohorts, selectedMetric, cohortXMap);
  const chartPlugins = useMemo(() => [logarithmicGridPlugin, cohortBandsPlugin, tenantMarkerPlugin], [logarithmicGridPlugin, cohortBandsPlugin]);

  const options: ChartOptions<'bubble'> = useMemo(() => {
    const numCohorts = activeCohorts.length;
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      onClick: (_event, elements) => {
        const tenant = elements[0] ? tenantMetaList[elements[0].index] : undefined;
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
            const dataIndex = tooltip.dataPoints[0]?.dataIndex;
            const tenant = dataIndex !== undefined ? tenantMetaList[dataIndex] : undefined;
            if (!tenant) return null;
            const [typeVariable, typeFallback] = tenant.type ? (COHORT_COLORS[tenant.type] || COHORT_COLORS.Mixed) : COHORT_COLORS.Mixed;
            const metricVal = getMetricValue(tenant, selectedMetric);
            const rank = getTenantPercentileRank(tenant, selectedMetric);

            return {
              title: tenant.tenantName ?? '',
              tag: { label: tenant.type ?? 'Mixed', color: chartColor(typeVariable, typeFallback) },
              groups: [
                [
                  {
                    label: selectedMetric === 'aov' ? 'Avg order value' : selectedMetric === 'volume' ? 'Order volume' : 'Period revenue',
                    value: formatMetricValue(metricVal, selectedMetric),
                    tone: 'primary',
                  },
                  {
                    label: 'Cohort rank',
                    value: `${getOrdinalSuffix(rank)} percentile`,
                  },
                ],
              ],
            };
          }),
        },
      },
      scales: {
        x: {
          type: 'linear',
          min: 0.45,
          max: numCohorts + 0.55,
          border: { display: false },
          grid: { display: false },
          ticks: {
            ...chartTick(13, 700),
            callback: (val) => {
              const idx = Number(val) - 1;
              return activeCohorts[idx] ?? '';
            },
          },
        },
        y: {
          min: minLogVal,
          max: maxLogVal,
          border: { display: false },
          grid: {
            color: 'rgba(100, 116, 139, 0.40)',
            lineWidth: 1.5,
          },
          ticks: {
            ...chartTick(12, 600),
            afterBuildTicks: (scale: { ticks: Array<{ value: number }> }) => {
              scale.ticks = majorTickValues.map(v => ({ value: v }));
            },
            callback: (val) => formatCompact(Math.pow(10, Number(val))),
          },
        },
      },
    };
  }, [tenantMetaList, selectedMetric, onTenantSelect, minLogVal, maxLogVal, activeCohorts, majorTickValues]);

  return (
    <ChartPanel
      isLoading={isLoading}
      isError={isError}
      isStale={isStale}
      title="Cross-Segment Cohort Distribution"
      className={className || 'h-full'}
      bodyClassName={isEmpty ? 'flex items-center justify-center' : 'flex-1 min-h-0 flex flex-col'}
      legend={
        <div className="flex items-center gap-3">
          <Select
            aria-label="Distribution Metric"
            value={selectedMetric}
            onChange={(e) => setSelectedMetric(e.target.value as MetricType)}
            variant="outlined"
            size="sm"
            fullWidth={false}
            containerClassName="w-56 shrink-0"
          >
            {METRIC_OPTIONS.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </Select>
          <span className="text-sm font-bold text-on-surface-variant uppercase tracking-wide bg-surface-container-low px-3 py-1.5 rounded">Size = Current Revenue</span>
        </div>
      }
    >
      {isEmpty ? (
        <EmptyState message="No cohort data available" variant="minimal" />
      ) : (
        <div className="flex-1 min-h-0 w-full flex flex-col relative select-none">
          <div className="flex-1 min-h-0 relative">
            <ChartJsCanvas type="bubble" data={data} options={options} plugins={chartPlugins} />
          </div>

          <div className="flex flex-wrap items-center justify-around gap-x-6 gap-y-1.5 px-4 pt-2 border-t border-outline-variant/30 shrink-0 z-20">
            {activeCohorts.map((cohortType) => {
              const cohortGroup = cohorts.find(c => c.type === cohortType);
              const quartiles = getGroupQuartiles(cohortGroup, selectedMetric);
              const [variable, fallback] = COHORT_COLORS[cohortType] || COHORT_COLORS.Mixed;
              const colorHex = chartColor(variable, fallback);

              return (
                <div key={cohortType} className="flex items-center gap-2 text-sm">
                  <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: colorHex }} />
                  <span className="font-bold text-on-surface">{cohortType} median:</span>
                  <span className="font-semibold text-on-surface">{formatMetricValue(quartiles.median, selectedMetric)}</span>
                  <span className="font-semibold text-on-surface">
                    (IQR: {formatCompact(quartiles.q1)} – {formatCompact(quartiles.q3)})
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </ChartPanel>
  );
});
