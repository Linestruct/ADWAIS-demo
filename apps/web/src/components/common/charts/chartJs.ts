// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import {
  BarController,
  BarElement,
  BubbleController,
  CategoryScale,
  Chart as ChartJS,
  Filler,
  Legend,
  LineController,
  LineElement,
  LinearScale,
  LogarithmicScale,
  PointElement,
  ScatterController,
  Tooltip,
  type ChartType,
  type Plugin,
  type TooltipModel,
} from 'chart.js';
import { getTenantFaviconUrl } from '../../../utils/tenantHelper';

const htmlTooltips = new WeakMap<object, HTMLDivElement>();
const activeTooltipElements = new Set<HTMLDivElement>();

export const htmlTooltipPlugin: Plugin = {
  id: 'htmlTooltipCleanup',
  beforeDestroy(chart) {
    const element = htmlTooltips.get(chart);
    if (element) {
      element.remove();
      activeTooltipElements.delete(element);
      htmlTooltips.delete(chart);
    }
  },
};

if (typeof document !== 'undefined') {
  const dismissTooltips = (e: Event) => {
    if (e.type === 'pointerdown') {
      const target = e.target as HTMLElement | null;
      if (target?.closest('canvas') || target?.closest('.chartjs-html-tooltip')) return;
    }
    activeTooltipElements.forEach((element) => {
      if (element.isConnected) {
        element.style.opacity = '0';
      } else {
        activeTooltipElements.delete(element);
      }
    });
  };
  document.addEventListener('pointerdown', dismissTooltips, true);
  document.addEventListener('keydown', (e) => {
    if ((e as KeyboardEvent).key === 'Escape') dismissTooltips(e);
  });
}

ChartJS.register(
  BarController,
  BarElement,
  BubbleController,
  CategoryScale,
  Filler,
  Legend,
  LineController,
  LineElement,
  LinearScale,
  LogarithmicScale,
  PointElement,
  ScatterController,
  Tooltip,
  htmlTooltipPlugin,
);

export const CHART_FONT = 'Manrope, sans-serif';

export function chartColor(variable: string, fallback: string): string {
  if (typeof document === 'undefined') return fallback;
  return getComputedStyle(document.documentElement).getPropertyValue(variable).trim() || fallback;
}

export const chartTooltip = {
  backgroundColor: (context: { chart: { canvas: HTMLCanvasElement } }) =>
    getComputedStyle(context.chart.canvas).getPropertyValue('--color-surface').trim()
      || chartColor('--color-surface', '#fff'),
  titleColor: chartColor('--color-on-surface', '#1f2937'),
  bodyColor: chartColor('--color-on-surface-variant', '#64748b'),
  borderColor: chartColor('--color-outline-variant', '#e2e8f0'),
  borderWidth: 1,
  cornerRadius: 10,
  padding: { x: 16, y: 12 },
  caretPadding: 10,
  boxPadding: 7,
  titleSpacing: 4,
  titleMarginBottom: 4,
  bodySpacing: 7,
  titleFont: { family: CHART_FONT, size: 16, weight: 'bold' as const },
  bodyFont: { family: CHART_FONT, size: 14, weight: 'bold' as const, lineHeight: 1.35 },
  displayColors: true,
  usePointStyle: true,
};

export type ChartTooltipTone = 'default' | 'primary' | 'positive' | 'negative' | 'warning' | 'muted';

export type ChartTooltipRow = {
  label: string;
  value: string;
  tone?: ChartTooltipTone;
};

export type ChartTooltipContent = {
  title: string;
  tag?: { label: string; color?: string };
  groups: ChartTooltipRow[][];
};

const tooltipToneColor = (tone: ChartTooltipTone | undefined): string => {
  switch (tone) {
    case 'primary': return chartColor('--color-brand-btn-primary', '#0f766e');
    case 'positive': return chartColor('--color-success', '#16a34a');
    case 'negative': return chartColor('--color-error', '#ef4444');
    case 'warning': return chartColor('--color-warning', '#f97316');
    case 'muted': return chartColor('--color-on-surface-variant', '#64748b');
    default: return chartColor('--color-on-surface', '#1f2937');
  }
};

function getHtmlTooltip<TType extends ChartType>(chart: ChartJS<TType>): HTMLDivElement {
  const existing = htmlTooltips.get(chart);
  if (existing?.isConnected) return existing;

  const element = document.createElement('div');
  element.classList.add('chartjs-html-tooltip');
  Object.assign(element.style, {
    position: 'absolute',
    zIndex: '30',
    minWidth: '230px',
    maxWidth: 'min(300px, calc(100% - 16px))',
    padding: '12px 16px',
    border: `1px solid ${chartColor('--color-outline-variant', '#e2e8f0')}`,
    borderRadius: '10px',
    background: chartColor('--color-surface', '#fff'),
    boxShadow: '0 8px 20px rgba(15, 23, 42, 0.13)',
    color: chartColor('--color-on-surface', '#1f2937'),
    fontFamily: CHART_FONT,
    fontSize: '14px',
    lineHeight: '1.35',
    pointerEvents: 'none',
    opacity: '0',
    transition: 'opacity 80ms ease',
  });
  chart.canvas.parentElement?.appendChild(element);
  htmlTooltips.set(chart, element);
  activeTooltipElements.add(element);
  return element;
}

function renderHtmlTooltip(element: HTMLDivElement, content: ChartTooltipContent) {
  element.replaceChildren();

  const header = document.createElement('div');
  Object.assign(header.style, { display: 'flex', alignItems: 'center', gap: '10px', paddingBottom: '4px' });
  const title = document.createElement('strong');
  title.textContent = content.title;
  Object.assign(title.style, { flex: '1', minWidth: '0', fontSize: '14px', fontWeight: '800' });
  header.appendChild(title);
  if (content.tag) {
    const tag = document.createElement('span');
    tag.textContent = content.tag.label;
    Object.assign(tag.style, {
      flexShrink: '0', padding: '2px 7px', borderRadius: '4px', color: '#fff',
      background: content.tag.color || chartColor('--color-on-surface-variant', '#64748b'),
      fontSize: '10px', fontWeight: '800', letterSpacing: '0.08em', textTransform: 'uppercase',
    });
    header.appendChild(tag);
  }
  element.appendChild(header);

  content.groups.filter(group => group.length > 0).forEach((group, groupIndex) => {
    const groupElement = document.createElement('div');
    Object.assign(groupElement.style, {
      display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) auto', columnGap: '18px', rowGap: '7px',
      paddingTop: groupIndex === 0 ? '0' : '11px',
      marginTop: groupIndex === 0 ? '0' : '10px',
      borderTop: groupIndex === 0 ? 'none' : `1px solid ${chartColor('--color-outline-variant', '#e2e8f0')}`,
    });
    group.forEach(row => {
      const label = document.createElement('span');
      label.textContent = row.label;
      Object.assign(label.style, { color: chartColor('--color-on-surface-variant', '#64748b'), fontWeight: '500' });
      const value = document.createElement('strong');
      value.textContent = row.value;
      Object.assign(value.style, {
        color: tooltipToneColor(row.tone), fontWeight: '750', textAlign: 'right',
        fontVariantNumeric: 'tabular-nums', whiteSpace: 'nowrap',
      });
      groupElement.append(label, value);
    });
    element.appendChild(groupElement);
  });
}

export function createHtmlTooltip<TType extends ChartType>(
  getContent: (tooltip: TooltipModel<TType>) => ChartTooltipContent | null,
) {
  return ({ chart, tooltip }: { chart: ChartJS<TType>; tooltip: TooltipModel<TType> }) => {
    const element = getHtmlTooltip(chart);
    if (tooltip.opacity === 0 || tooltip.dataPoints.length === 0) {
      element.style.opacity = '0';
      return;
    }

    const content = getContent(tooltip);
    if (!content) {
      element.style.opacity = '0';
      return;
    }
    renderHtmlTooltip(element, content);
    element.style.opacity = '1';

    const parent = chart.canvas.parentElement;
    if (!parent) return;
    const margin = 8;
    const preferredLeft = chart.canvas.offsetLeft + tooltip.caretX + 14;
    const tooltipWidth = element.offsetWidth;
    const tooltipHeight = element.offsetHeight;
    const left = preferredLeft + tooltipWidth <= parent.clientWidth - margin
      ? preferredLeft
      : chart.canvas.offsetLeft + tooltip.caretX - tooltipWidth - 14;
    const preferredTop = chart.canvas.offsetTop + tooltip.caretY - tooltipHeight / 2;
    element.style.left = `${Math.max(margin, Math.min(left, parent.clientWidth - tooltipWidth - margin))}px`;
    element.style.top = `${Math.max(margin, Math.min(preferredTop, parent.clientHeight - tooltipHeight - margin))}px`;
  };
}

export function chartTick(fontSize = 12, fontWeight: 600 | 700 = 600) {
  return {
    color: chartColor('--color-chart-tick', '#64748b'),
    font: { family: CHART_FONT, size: fontSize, weight: fontWeight },
  };
}

export const chartLegendLabels = {
  ...chartTick(14, 600),
  usePointStyle: true,
  pointStyleWidth: 14,
  boxWidth: 9,
  boxHeight: 9,
  padding: 18,
};

export const horizontalGrid = {
  color: chartColor('--color-chart-grid', '#e2e8f0'),
  borderDash: [3, 4],
  drawTicks: false,
};

export function scaleBubbleRadii(values: number[], minRadius = 5, maxRadius = 20, useLog = false): number[] {
  const transform = useLog
    ? (v: number) => Math.log10(Math.max(1, v))
    : (v: number) => Math.sqrt(Math.max(0, v));
  const normalized = values.map(transform);
  const min = Math.min(...normalized);
  const max = Math.max(...normalized);
  if (min === max) return normalized.map(() => (minRadius + maxRadius) / 2);
  return normalized.map(value => minRadius + ((value - min) / (max - min)) * (maxRadius - minRadius));
}

type TenantPointMeta = {
  tenantName?: string | null;
  orderProviderEndpoint?: string | null;
  url?: string | null;
};

type FaviconCacheEntry = {
  image: HTMLImageElement;
  status: 'loading' | 'loaded' | 'failed';
  waitingCharts: Set<ChartJS<'bubble'>>;
};

const chartFaviconCache = new Map<string, FaviconCacheEntry>();
const pendingFaviconRedraws = new Set<ChartJS<'bubble'>>();
let faviconRedrawFrame: number | null = null;

function scheduleFaviconRedraw(chart: ChartJS<'bubble'>) {
  pendingFaviconRedraws.add(chart);
  if (faviconRedrawFrame !== null) return;
  faviconRedrawFrame = requestAnimationFrame(() => {
    pendingFaviconRedraws.forEach(pendingChart => {
      const canvas = pendingChart.canvas;
      if (canvas && ChartJS.getChart(canvas) === pendingChart) pendingChart.draw();
    });
    pendingFaviconRedraws.clear();
    faviconRedrawFrame = null;
  });
}

function getChartFavicon(url: string, chart: ChartJS<'bubble'>): FaviconCacheEntry {
  const cached = chartFaviconCache.get(url);
  if (cached) {
    if (cached.status === 'loading') cached.waitingCharts.add(chart);
    return cached;
  }

  const image = new Image();
  const entry: FaviconCacheEntry = {
    image,
    status: 'loading',
    waitingCharts: new Set([chart]),
  };
  const redrawWaitingCharts = () => {
    entry.waitingCharts.forEach(scheduleFaviconRedraw);
    entry.waitingCharts.clear();
  };
  image.onload = () => {
    entry.status = 'loaded';
    redrawWaitingCharts();
  };
  image.onerror = () => {
    entry.status = 'failed';
    redrawWaitingCharts();
  };
  image.decoding = 'async';
  image.src = url;
  chartFaviconCache.set(url, entry);
  return entry;
}

export const tenantMarkerPlugin: Plugin<'bubble'> = {
  id: 'tenantMarkers',
  afterDatasetsDraw(chart) {
    const context = chart.ctx;
    chart.data.datasets.forEach((dataset, datasetIndex) => {
      const tenantMeta = (dataset as typeof dataset & { tenantMeta?: TenantPointMeta[] }).tenantMeta;
      if (!tenantMeta) return;
      const elements = chart.getDatasetMeta(datasetIndex).data;
      elements.forEach((element, index) => {
        const tenant = tenantMeta[index];
        const name = tenant?.tenantName;
        const radius = (dataset.data[index] as { r?: number } | undefined)?.r ?? 0;
        if (radius < 5) return;
        const faviconUrl = getTenantFaviconUrl(tenant?.orderProviderEndpoint || tenant?.url);
        const favicon = faviconUrl ? getChartFavicon(faviconUrl, chart) : null;

        context.save();
        if (favicon?.status === 'loaded') {
          const imageSize = radius * 1.2;
          context.beginPath();
          context.arc(element.x, element.y, Math.max(1, radius - 2), 0, Math.PI * 2);
          context.clip();
          context.globalAlpha = 0.9;
          // context.globalCompositeOperation = 'luminosity';
          context.drawImage(
            favicon.image,
            element.x - imageSize / 2,
            element.y - imageSize / 2,
            imageSize,
            imageSize,
          );
        } else if (name && radius >= 7) {
          context.fillStyle = '#fff';
          context.font = `800 ${Math.max(9, radius * 0.9)}px ${CHART_FONT}`;
          context.textAlign = 'center';
          context.textBaseline = 'middle';
          context.fillText(name.charAt(0).toUpperCase(), element.x, element.y);
        }
        context.restore();
      });
    });
  },
};

export function referenceLinesPlugin(xValue?: number, yValue?: number): Plugin<'bubble'> {
  return {
    id: `referenceLines-${xValue ?? 'none'}-${yValue ?? 'none'}`,
    beforeDatasetsDraw(chart) {
      const { ctx, chartArea, scales } = chart;
      ctx.save();
      ctx.strokeStyle = chartColor('--color-chart-prev-line', '#94a3b8');
      ctx.lineWidth = 2;
      ctx.setLineDash([5, 5]);
      if (xValue != null) {
        const x = scales.x.getPixelForValue(xValue);
        ctx.beginPath(); ctx.moveTo(x, chartArea.top); ctx.lineTo(x, chartArea.bottom); ctx.stroke();
      }
      if (yValue != null) {
        const y = scales.y.getPixelForValue(yValue);
        ctx.beginPath(); ctx.moveTo(chartArea.left, y); ctx.lineTo(chartArea.right, y); ctx.stroke();
      }
      ctx.restore();
    },
  };
}

export type QuadrantLabels = {
  topLeft?: string;
  topRight?: string;
  bottomLeft?: string;
  bottomRight?: string;
};

export function matrixQuadrantsPlugin(xValue: number, yValue: number, labels?: QuadrantLabels): Plugin<'bubble'> {
  return {
    id: `matrixQuadrants-${xValue}-${yValue}`,
    beforeDraw(chart) {
      const { ctx, chartArea, scales } = chart;
      if (!chartArea || !scales.x || !scales.y) return;

      const splitX = Math.min(chartArea.right, Math.max(chartArea.left, scales.x.getPixelForValue(xValue)));
      const splitY = Math.min(chartArea.bottom, Math.max(chartArea.top, scales.y.getPixelForValue(yValue)));
      const quadrants = [
        { color: 'rgba(59, 130, 246, 0.045)', x: chartArea.left, y: chartArea.top, width: splitX - chartArea.left, height: splitY - chartArea.top },
        { color: 'rgba(34, 197, 94, 0.05)', x: splitX, y: chartArea.top, width: chartArea.right - splitX, height: splitY - chartArea.top },
        { color: 'rgba(239, 68, 68, 0.035)', x: chartArea.left, y: splitY, width: splitX - chartArea.left, height: chartArea.bottom - splitY },
        { color: 'rgba(245, 158, 11, 0.045)', x: splitX, y: splitY, width: chartArea.right - splitX, height: chartArea.bottom - splitY },
      ];

      ctx.save();
      quadrants.forEach(quadrant => {
        ctx.fillStyle = quadrant.color;
        ctx.fillRect(quadrant.x, quadrant.y, quadrant.width, quadrant.height);
      });

      if (labels) {
        ctx.font = `800 11px ${CHART_FONT}`;
        ctx.fillStyle = chartColor('--color-on-surface-variant', '#64748b');
        ctx.globalAlpha = 0.55;

        if (labels.topLeft && splitX > chartArea.left + 80 && splitY > chartArea.top + 30) {
          ctx.textAlign = 'left';
          ctx.textBaseline = 'top';
          ctx.fillText(labels.topLeft.toUpperCase(), chartArea.left + 14, chartArea.top + 12);
        }
        if (labels.topRight && chartArea.right - splitX > 80 && splitY > chartArea.top + 30) {
          ctx.textAlign = 'right';
          ctx.textBaseline = 'top';
          ctx.fillText(labels.topRight.toUpperCase(), chartArea.right - 14, chartArea.top + 12);
        }
        if (labels.bottomLeft && splitX > chartArea.left + 80 && chartArea.bottom - splitY > 30) {
          ctx.textAlign = 'left';
          ctx.textBaseline = 'bottom';
          ctx.fillText(labels.bottomLeft.toUpperCase(), chartArea.left + 14, chartArea.bottom - 12);
        }
        if (labels.bottomRight && chartArea.right - splitX > 80 && chartArea.bottom - splitY > 30) {
          ctx.textAlign = 'right';
          ctx.textBaseline = 'bottom';
          ctx.fillText(labels.bottomRight.toUpperCase(), chartArea.right - 14, chartArea.bottom - 12);
        }
      }
      ctx.restore();
    },
  };
}

type HullPoint = { x: number; y: number; r: number };

function computeConvexHull(pts: HullPoint[]): HullPoint[] {
  if (pts.length <= 2) return pts;
  const sorted = [...pts].sort((a, b) => a.x === b.x ? a.y - b.y : a.x - b.x);
  const lower: HullPoint[] = [];
  for (const p of sorted) {
    while (lower.length >= 2) {
      const o = lower[lower.length - 2];
      const a = lower[lower.length - 1];
      if ((a.x - o.x) * (p.y - o.y) - (a.y - o.y) * (p.x - o.x) <= 0) lower.pop();
      else break;
    }
    lower.push(p);
  }
  const upper: HullPoint[] = [];
  for (let i = sorted.length - 1; i >= 0; i--) {
    const p = sorted[i];
    while (upper.length >= 2) {
      const o = upper[upper.length - 2];
      const a = upper[upper.length - 1];
      if ((a.x - o.x) * (p.y - o.y) - (a.y - o.y) * (p.x - o.x) <= 0) upper.pop();
      else break;
    }
    upper.push(p);
  }
  lower.pop();
  upper.pop();
  return lower.concat(upper);
}

export function cohortHullPlugin(cohortColors: Record<string, string>): Plugin<'bubble'> {
  return {
    id: 'cohortHull',
    beforeDatasetsDraw(chart) {
      const { ctx, chartArea } = chart;
      if (!chartArea) return;

      chart.data.datasets.forEach((dataset, datasetIndex) => {
        const meta = chart.getDatasetMeta(datasetIndex);
        const tenantMeta = (dataset as typeof dataset & { tenantMeta?: { type?: string }[] }).tenantMeta;
        if (!tenantMeta || meta.hidden) return;

        const cohortPoints: Record<string, HullPoint[]> = {};
        meta.data.forEach((element, index) => {
          const type = tenantMeta[index]?.type || 'Mixed';
          const r = (dataset.data[index] as { r?: number } | undefined)?.r ?? 8;
          if (!cohortPoints[type]) cohortPoints[type] = [];
          cohortPoints[type].push({ x: element.x, y: element.y, r });
        });

        ctx.save();
        Object.entries(cohortPoints).forEach(([type, pts]) => {
          if (pts.length === 0) return;
          const colorHex = cohortColors[type] || cohortColors.Mixed || '#8b5cf6';
          ctx.fillStyle = colorHex + '14'; // ~8% opacity fill
          ctx.strokeStyle = colorHex + '66'; // ~40% opacity stroke
          ctx.lineWidth = 1.5;
          ctx.setLineDash([4, 4]);

          const padding = 14;

          if (pts.length === 1) {
            const p = pts[0];
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.r + padding, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();
          } else if (pts.length === 2) {
            const [p1, p2] = pts;
            const dx = p2.x - p1.x;
            const dy = p2.y - p1.y;
            const dist = Math.hypot(dx, dy);
            if (dist < 1) {
              ctx.beginPath();
              ctx.arc(p1.x, p1.y, Math.max(p1.r, p2.r) + padding, 0, Math.PI * 2);
              ctx.fill();
              ctx.stroke();
            } else {
              const nx = -dy / dist;
              const ny = dx / dist;
              const r1 = p1.r + padding;
              const r2 = p2.r + padding;
              ctx.beginPath();
              ctx.arc(p1.x, p1.y, r1, Math.atan2(ny, nx), Math.atan2(-ny, -nx));
              ctx.arc(p2.x, p2.y, r2, Math.atan2(-ny, -nx), Math.atan2(ny, nx));
              ctx.closePath();
              ctx.fill();
              ctx.stroke();
            }
          } else {
            const hull = computeConvexHull(pts);
            if (hull.length < 3) {
              const p1 = hull[0];
              ctx.beginPath();
              ctx.arc(p1.x, p1.y, p1.r + padding, 0, Math.PI * 2);
              ctx.fill();
              ctx.stroke();
              return;
            }

            // Offset vertices outwards
            const n = hull.length;
            const expandedPts: { x: number; y: number }[] = [];
            for (let i = 0; i < n; i++) {
              const prev = hull[(i - 1 + n) % n];
              const curr = hull[i];
              const next = hull[(i + 1) % n];

              // Normal vector of edge (prev -> curr)
              const v1x = curr.x - prev.x;
              const v1y = curr.y - prev.y;
              const len1 = Math.hypot(v1x, v1y) || 1;
              const n1x = -v1y / len1;
              const n1y = v1x / len1;

              // Normal vector of edge (curr -> next)
              const v2x = next.x - curr.x;
              const v2y = next.y - curr.y;
              const len2 = Math.hypot(v2x, v2y) || 1;
              const n2x = -v2y / len2;
              const n2y = v2x / len2;

              // Bisector normal
              let bisX = n1x + n2x;
              let bisY = n1y + n2y;
              const bisLen = Math.hypot(bisX, bisY);
              if (bisLen < 0.01) {
                bisX = n1x;
                bisY = n1y;
              } else {
                bisX /= bisLen;
                bisY /= bisLen;
              }

              const rPadded = curr.r + padding;
              expandedPts.push({
                x: curr.x + bisX * rPadded,
                y: curr.y + bisY * rPadded,
              });
            }

            ctx.beginPath();
            ctx.lineJoin = 'round';
            ctx.moveTo(expandedPts[0].x, expandedPts[0].y);
            for (let i = 1; i < expandedPts.length; i++) {
              ctx.lineTo(expandedPts[i].x, expandedPts[i].y);
            }
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
          }
        });
        ctx.restore();
      });
    },
  };
}
