// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useMemo, useRef, useState, Fragment, memo } from 'react';
import { createPortal } from 'react-dom';
import type { TransactionDensityPeriod, TransactionDensityResponseDto } from '@types';
import { formatCurrency, formatNumber } from '@utils';
import { formatDateTime } from '../../utils/dateTime';
import { ChartPanel } from '../common/charts/ChartPanel';
import {EmptyState} from "../common/ui/EmptyState.tsx";
import { Select } from '../common/ui/Select';
import { TransactionDensityHeatmapCanvas } from './TransactionDensityHeatmapCanvas';
import { TRANSACTION_DENSITY_HEATMAP_TUNING } from './transactionDensityHeatmapTuning';
import { TRANSACTION_DENSITY_PALETTE } from './transactionDensityPalette';

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const HOUR_LABEL_INTERVAL = 2;
const TOOLTIP_WIDTH = 208;
const TOOLTIP_ESTIMATED_HEIGHT = 150;
const TOOLTIP_VIEWPORT_MARGIN = 8;
const TOOLTIP_OFFSET = 10;
const PERIOD_OPTIONS: Array<{ value: TransactionDensityPeriod; label: string }> = [
  { value: 'Auto', label: 'Auto' },
  { value: 'T30', label: '30 days' },
  { value: 'T90', label: '90 days' },
  { value: 'T180', label: '180 days' },
  { value: 'T365', label: '365 days' },
];

function formatHour(hour: number) {
  return `${hour.toString().padStart(2, '0')}:00`;
}

export const TransactionDensityChart = memo(function TransactionDensityChart({
  isLoading, isError, isStale, response, selectedPeriod, onPeriodChange,
  className }: { isLoading?: boolean; isError?: boolean; isStale?: boolean;
  response: TransactionDensityResponseDto;
  selectedPeriod?: TransactionDensityPeriod;
  onPeriodChange?: (period: TransactionDensityPeriod) => void;
  className?: string;
}) {
  const [hoverInfo, setHoverInfo] = useState<{
    day: number;
    hour: number;
    count: number;
    revenue: number;
    x: number;
    y: number;
    placement: 'left' | 'right' | 'center';
    pinned: boolean;
  } | null>(null);
  const viewportRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!hoverInfo?.pinned) return;

    const dismissOutsideTile = (event: PointerEvent) => {
      const target = event.target;
      if (target instanceof Element && target.closest('[data-density-cell]')) return;
      setHoverInfo(null);
    };
    const dismissWithEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setHoverInfo(null);
    };

    document.addEventListener('pointerdown', dismissOutsideTile, true);
    document.addEventListener('keydown', dismissWithEscape);
    return () => {
      document.removeEventListener('pointerdown', dismissOutsideTile, true);
      document.removeEventListener('keydown', dismissWithEscape);
    };
  }, [hoverInfo?.pinned]);

  const {
    points,
    totalCount,
    minCount,
    maxCount,
    averageCountPerBucket,
    sampleQuality,
    effectivePeriod,
    timeZoneId,
    periodStart,
    periodEnd,
  } = response;
  const isEmpty = points.length === 0 || totalCount === 0;
  const timeZoneLabel = timeZoneId === 'Europe/Stockholm' ? 'Stockholm time' : timeZoneId;
  const periodLabel = periodStart && periodEnd
    ? `${formatDateTime(periodStart, { day: 'numeric', month: 'short', timeZone: timeZoneId })}–${formatDateTime(periodEnd, { day: 'numeric', month: 'short', timeZone: timeZoneId })} · ${timeZoneLabel}`
    : undefined;
  const isSparse = sampleQuality === 'Sparse';
  const isIndicative = sampleQuality === 'Indicative';
  const matrix = useMemo(() => {
    const nextMatrix = Array.from({ length: 7 }, () => Array.from({ length: 24 }, () => ({ count: 0, totalRevenue: 0 })));

    points.forEach(p => {
      const dayIndex = p.dayOfWeek - 1;
      const hour = p.hour;
      if (dayIndex >= 0 && dayIndex <= 6 && hour >= 0 && hour <= 23) {
        nextMatrix[dayIndex][hour] = { count: p.count, totalRevenue: p.totalRevenue };
      }
    });

    return nextMatrix;
  }, [points]);
  const countMatrix = useMemo(
    () => matrix.map(row => row.map(cell => cell.count)),
    [matrix],
  );

  const getTooltipInfo = (
    element: HTMLElement,
    day: number,
    hour: number,
    count: number,
    revenue: number,
    pinned: boolean,
  ) => {
    const rect = element.getBoundingClientRect();
    const viewportRect = viewportRef.current?.getBoundingClientRect();
    const visibleLeft = Math.max(viewportRect?.left ?? 0, TOOLTIP_VIEWPORT_MARGIN);
    const visibleRight = Math.min(viewportRect?.right ?? window.innerWidth, window.innerWidth - TOOLTIP_VIEWPORT_MARGIN);
    const visibleMidpoint = (visibleLeft + visibleRight) / 2;
    const preferredPlacement = rect.left + rect.width / 2 < visibleMidpoint ? 'right' : 'left';
    const fitsRight = visibleRight - rect.right >= TOOLTIP_WIDTH + TOOLTIP_OFFSET;
    const fitsLeft = rect.left - visibleLeft >= TOOLTIP_WIDTH + TOOLTIP_OFFSET;
    const placement = preferredPlacement === 'right'
      ? (fitsRight ? 'right' : fitsLeft ? 'left' : 'center')
      : (fitsLeft ? 'left' : fitsRight ? 'right' : 'center');
    const halfTooltipHeight = TOOLTIP_ESTIMATED_HEIGHT / 2;

    return {
      day,
      hour,
      count,
      revenue,
      x: placement === 'right'
        ? rect.right + TOOLTIP_OFFSET
        : placement === 'left'
          ? rect.left - TOOLTIP_OFFSET
          : visibleMidpoint,
      y: Math.min(
        Math.max(rect.top + rect.height / 2, TOOLTIP_VIEWPORT_MARGIN + halfTooltipHeight),
        window.innerHeight - TOOLTIP_VIEWPORT_MARGIN - halfTooltipHeight,
      ),
      placement,
      pinned,
    } as const;
  };

  return (
    <ChartPanel isLoading={isLoading} isError={isError} isStale={isStale}
      title="Transaction Density Matrix"
      subtitle={periodLabel}
      className={className || "h-full relative"}
      bodyClassName={isEmpty ? 'flex items-center h-full justify-center' : 'flex-1 min-h-0 flex flex-col'}
      legend={
      <div className="flex flex-wrap items-end justify-end items-center gap-4">
        {selectedPeriod && onPeriodChange && (
          <Select
            aria-label="Transaction density period"
            value={selectedPeriod}
            onChange={(event) => onPeriodChange(event.target.value as TransactionDensityPeriod)}
            variant="outlined"
            size="sm"
            fullWidth={false}
            containerClassName="shrink-0 border-r border-outline-variant pr-4"
          >
            {PERIOD_OPTIONS.map(option => (
              <option key={option.value} value={option.value}>
                {option.value === 'Auto'
                  ? `Auto · ${effectivePeriod === 'T365' ? '1y' : `${effectivePeriod.slice(1)}d`}`
                  : option.label}
              </option>
            ))}
          </Select>
        )}
        <div className="text-right leading-none">
          <strong className="block text-2xl font-black tabular-nums text-on-surface">{formatNumber(totalCount)}</strong>
          <span className="mt-1 block text-xs font-bold uppercase tracking-wider text-on-surface-variant">transactions</span>
        </div>
        <div className="w-36 min-w-28" aria-label={`Density ranges from ${minCount} to ${maxCount} transactions per bucket`}>
          <div className="mb-1 flex justify-between text-base font-bold tabular-nums text-on-surface-variant">
            <span>{formatNumber(minCount)}</span>
            <span>{formatNumber(maxCount)}</span>
          </div>
          <div
            className="h-5 rounded-md"
            style={{ background: `linear-gradient(to right, ${TRANSACTION_DENSITY_PALETTE.join(', ')})` }}
          />
        </div>
      </div>}
    >
      {isEmpty ? (
        <EmptyState message={"No data available"} variant={"minimal"}/>
      ) : (
        <div ref={viewportRef} onScroll={() => setHoverInfo(null)} className="flex-1 flex flex-col h-full w-full relative overflow-x-auto custom-scrollbar">
          {(isSparse || isIndicative) && (
            <div className="mb-2 flex min-w-[600px] items-center justify-between gap-4 rounded-lg bg-tertiary-container px-3 py-2 text-sm text-on-tertiary-container lg:min-w-0">
              <span className="font-bold">
                {isSparse
                  ? 'Sparse sample — individual peaks may not represent a stable pattern.'
                  : 'Indicative sample — use the broader pattern rather than individual peaks.'}
              </span>
              <span className="shrink-0 tabular-nums">
                {averageCountPerBucket.toFixed(1)} per bucket · {effectivePeriod.slice(1)} days
              </span>
            </div>
          )}
          
          <div className="flex min-h-[180px] min-w-[600px] flex-1 flex-col lg:min-w-0">
            <div className="grid grid-cols-[40px_repeat(24,_minmax(0,_1fr))]">
              <div />
              {Array.from({ length: 24 }).map((_, hour) => (
                <div key={hour} className="pb-1 text-center text-[14px] font-[600] font-['Manrope',_sans-serif] text-[color:var(--color-chart-tick)]">
                  {hour % HOUR_LABEL_INTERVAL === 0 ? formatHour(hour) : null}
                </div>
              ))}
            </div>

            <div className="h-full grid min-h-[140px] flex-1 grid-cols-[40px_minmax(0,_1fr)]">
              <div className="grid grid-rows-[repeat(7,_minmax(0,_1fr))]">
                {DAYS.map(day => (
                  <div key={day} className="flex items-center justify-end pr-2 text-[14px] font-[600] font-['Manrope',_sans-serif] text-[color:var(--color-chart-tick)]">
                    {day}
                  </div>
                ))}
              </div>

              <div className="relative min-h-0 overflow-hidden">
                <TransactionDensityHeatmapCanvas
                  values={countMatrix}
                  min={minCount}
                  max={maxCount}
                  interpolationSteps={isSparse
                    ? TRANSACTION_DENSITY_HEATMAP_TUNING.sparseInterpolationSteps
                    : isIndicative
                      ? TRANSACTION_DENSITY_HEATMAP_TUNING.indicativeInterpolationSteps
                      : null}
                />

                <div className="relative z-10 grid h-full grid-cols-[repeat(24,_minmax(0,_1fr))] grid-rows-[repeat(7,_minmax(0,_1fr))]">
                  {DAYS.map((day, dayIndex) => (
                    <Fragment key={day}>
                      {Array.from({ length: 24 }).map((_, hourIndex) => {
                        const cellData = matrix[dayIndex][hourIndex];
                        const isActive = hoverInfo?.day === dayIndex && hoverInfo.hour === hourIndex;
                        return (
                          <button
                            key={hourIndex}
                            type="button"
                            data-density-cell
                            aria-label={`${day} at ${formatHour(hourIndex)}: ${cellData.count} transactions`}
                            className={`min-h-[20px] cursor-pointer p-0 transition-colors focus-visible:outline-none ${isIndicative || isSparse ? 'border-b border-r border-white/10' : 'border-0'} ${isActive ? 'ring-2 ring-inset ring-on-surface' : 'hover:bg-white/10 hover:ring-1 hover:ring-inset hover:ring-white/70 focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-on-surface'}`}
                            onMouseEnter={(event) => {
                              const info = getTooltipInfo(event.currentTarget, dayIndex, hourIndex, cellData.count, cellData.totalRevenue || 0, false);
                              setHoverInfo(current => current?.pinned ? current : info);
                            }}
                            onMouseLeave={() => setHoverInfo(current => current?.pinned ? current : null)}
                            onClick={(event) => {
                              const info = getTooltipInfo(event.currentTarget, dayIndex, hourIndex, cellData.count, cellData.totalRevenue || 0, true);
                              setHoverInfo(current => current?.pinned && current.day === dayIndex && current.hour === hourIndex ? null : info);
                            }}
                          />
                        );
                      })}
                    </Fragment>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Custom Tooltip */}
          {hoverInfo && createPortal(
            <div 
              className={`fixed z-50 min-w-[230px] max-w-[min(300px,calc(100vw-16px))] -translate-y-1/2 rounded-[10px] border border-outline-variant bg-surface px-4 py-3 text-[14px] leading-[1.35] text-on-surface pointer-events-none ${hoverInfo.placement === 'left' ? '-translate-x-full' : hoverInfo.placement === 'center' ? '-translate-x-1/2' : ''}`}
              style={{ left: hoverInfo.x, top: hoverInfo.y, boxShadow: '0 8px 20px rgba(15, 23, 42, 0.13)', fontFamily: 'Manrope, sans-serif' }}
            >
              <div className="flex items-center gap-[10px] pb-1">
                <strong className="flex-1 min-w-0 font-[800]">
                  {DAYS[hoverInfo.day]} at {formatHour(hoverInfo.hour)}
                </strong>
              </div>
              <div className="grid grid-cols-[minmax(0,1fr)_auto] gap-x-[18px] gap-y-[7px] pt-[11px] mt-[10px] border-t border-outline-variant">
                <span className="font-medium text-on-surface-variant">Transactions</span>
                <strong className="font-[750] tabular-nums whitespace-nowrap text-right text-brand-btn-primary">{hoverInfo.count}</strong>
                <span className="font-medium text-on-surface-variant">Revenue</span>
                <strong className="font-[750] tabular-nums whitespace-nowrap text-right text-brand-btn-primary">{formatCurrency(hoverInfo.revenue).replace(/\s*kr/i, '\u00a0SEK')}</strong>
              </div>
            </div>,
            document.body
          )}

        </div>
      )}
    </ChartPanel>
  );
});
