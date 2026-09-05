// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useNavigate, useSearch } from '@tanstack/react-router';
import { type PersistentDomain, setSavedTimeframe } from "../../../utils/timeframeStorage";
import type { Timeframe } from '../../../schemas';

interface PeriodSelectorProps {
  from: PersistentDomain;
  embedded?: boolean;
}

export function PeriodSelector({ from, embedded = false }: PeriodSelectorProps) {
  const navigate = useNavigate({ from });
  const search = useSearch({ strict: false });
  const timeframe = search.timeframe;

  const options = [
    { label: '1D', value: 'Today' },
    { label: '7D', value: 'T7' },
    { label: '30D', value: 'T30' },
    { label: '90D', value: 'T90' },
    { label: '365D', value: 'T365' },
    { label: 'YTD', value: 'Ytd' },
  ] as const;

  const handleSelect = (val: Timeframe) => {
    setSavedTimeframe(from, val);
    void navigate({ search: (old: Record<string, unknown>) => ({ ...old, timeframe: val }) });
  };

  const containerCls = embedded
    ? "grid grid-cols-3 w-full items-center"
    : "group grid grid-cols-3 md:flex bg-surface rounded-full m3-elevation-1 pointer-events-auto w-full md:w-auto max-w-[400px] md:max-w-none items-center min-h-14";

  return (
    <div className={containerCls}>
      {options.map((opt) => {
        const isActive = timeframe === opt.value;
        const buttonCls = isActive
          ? 'bg-primary-container text-on-primary-container'
          : 'text-on-surface-variant hover:bg-surface-container';

        return (
          <button
            key={opt.value}
            id={`period-${opt.value}`}
            onClick={() => handleSelect(opt.value)}
            className={`px-3 py-2 md:px-5 md:py-2 rounded-full text-xs md:text-sm min-h-14 min-w-[100px] font-black transition-all duration-200 tracking-wide uppercase cursor-pointer text-center ${buttonCls}`}
          >
            {opt.label}
          </button>
        );
      })}
    </div>
  );
}
