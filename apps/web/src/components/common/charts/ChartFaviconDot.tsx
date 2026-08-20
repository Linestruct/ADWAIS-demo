// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState, useId, type SVGProps } from 'react';
import { getTenantFaviconUrl } from '../../../utils/tenantHelper';

export interface ChartFaviconDotProps extends SVGProps<SVGGElement> {
  cx?: number;
  cy?: number;
  fill?: string;
  /** Recharts passes bubble area (px²), not radius. */
  size?: number;
  payload?: {
    tenantId?: string;
    tenantName?: string | null;
    type?: string;
    /** Tenant's base URL used for favicon resolution. */
    url?: string | null;
    orderProviderEndpoint?: string | null;
  };
}

export function ChartFaviconDot(props: ChartFaviconDotProps) {
  const { cx, cy, fill, size, payload, ...rest } = props;
  const elementId = useId();
  const [imgError, setImgError] = useState(false);

  if (cx == null || cy == null || !size) return null;

  const r = Math.sqrt(size / Math.PI);
  const tenantName = payload?.tenantName ?? '';
  const faviconUrl = getTenantFaviconUrl(payload?.orderProviderEndpoint || payload?.url);
  const clipId = `fav-clip-${elementId.replace(/:/g, '')}-${payload?.tenantId ?? ''}`;
  const imgSize = r * 1.5;
  const resolvedFill = fill || 'var(--color-chart-1)';
  const showLetter = !faviconUrl || imgError;

  return (
    <g {...rest} opacity={0.7} className="cursor-pointer hover:opacity-90 transition-opacity">
      <defs>
        <clipPath id={clipId}>
          <circle cx={cx} cy={cy} r={r - 1} />
        </clipPath>
      </defs>
      <circle
        cx={cx}
        cy={cy}
        r={r}
        fill={resolvedFill}
        fillOpacity={1}
        stroke="white"
        strokeWidth={2}
      />
      {showLetter && tenantName && (
        <text
          x={cx}
          y={cy}
          textAnchor="middle"
          dominantBaseline="central"
          fontSize={r * 1.5}
          fontWeight={800}
          fill="white"
          fillOpacity={1}
          style={{ pointerEvents: 'none', userSelect: 'none', fontFamily: 'Manrope, sans-serif', mixBlendMode: 'difference' }}
        >
          {tenantName.charAt(0).toUpperCase()}
        </text>
      )}
      {!showLetter && (
        <image
          href={faviconUrl!}
          x={cx - imgSize / 2}
          y={cy - imgSize / 2}
          width={imgSize}
          height={imgSize}
          clipPath={`url(#${clipId})`}
          opacity={1}
          style={{ pointerEvents: 'none', mixBlendMode: 'screen' }}
          onError={() => setImgError(true)}
        />
      )}
    </g>
  );
}
