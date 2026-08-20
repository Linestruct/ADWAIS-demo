// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';
import { Skeleton } from '../ui/Skeleton';

type Extra =
    | 
    { type: "PoP"; value: number; } 
    |
    { type: "Desc"; value: string; };

interface FactPanelProps {
    label: string;
    value?: string;
    isLoading?: boolean;
    isError?: boolean;
    valueColor?: "green" | "red" | "yellow"; 
    extra?: Extra;
    hasExtra?: boolean;
    inverseTrend?: boolean;
    children?: ReactNode;
}

export function FactPanel({ label, value = '', isLoading, isError, valueColor, extra, hasExtra, inverseTrend, children }: FactPanelProps) {
    let valueColorClass = 'text-on-surface';
    let extraColor = '';
    let extraText = '';

    if (valueColor === 'green') valueColorClass = 'text-growth';
    if (valueColor === 'red') valueColorClass = 'text-decline';
    if (valueColor === 'yellow') valueColorClass = 'text-decline-warning';

    if (extra !== undefined && extra.type === "PoP") {
        if (extra.value > 0) {
            extraColor = inverseTrend ? 'text-decline' : 'text-growth';
            extraText = `▲ ${extra.value.toFixed(2)}% PoP`;
        } else if (extra.value < 0) {
            extraColor = inverseTrend ? 'text-growth' : 'text-decline';
            extraText = `▼ ${Math.abs(extra.value).toFixed(2)}% PoP`;
        } else {
            extraColor = 'text-on-surface-variant';
            extraText = `${extra.value.toFixed(2)}% PoP`;
        }
    }

    if (extra !== undefined && extra.type === "Desc") {
        extraText = extra.value;
        extraColor = 'text-on-surface-variant';
    }

    const showExtra = hasExtra || extra !== undefined;

    return (
        <div className="snap-start border border-outline bg-surface rounded-2xl px-4 py-3 md:px-5 md:py-4 flex flex-col justify-between min-h-[80px] md:min-h-[100px] min-w-[40vw] md:min-w-0 md:w-full transition-all overflow-hidden shrink-0">
            <h2 className="text-xs md:text-sm font-bold text-on-surface-variant uppercase tracking-wide mb-2 truncate">{label}</h2>
            {isLoading ? (
                <div className="flex flex-col mt-auto">
                    <Skeleton className="h-8 lg:h-9 xl:h-8 2xl:h-10 w-28 bg-surface-container-high" />
                    {showExtra && <Skeleton className="h-5 2xl:h-6 w-20 bg-surface-container" />}
                </div>
            ) : isError ? (
                <div className="flex flex-col mt-auto">
                    <span className="text-xl font-extrabold tracking-tight text-error">Unavailable</span>
                    <span className="text-sm font-bold tracking-wider text-on-surface-variant">Data request failed</span>
                </div>
            ) : children ? (
                <div className="flex flex-col mt-auto">
                    {children}
                </div>
            ) : (
                <div className="flex flex-col mt-auto">
                <span className={`text-xl md:text-2xl lg:text-3xl 2xl:text-4xl font-extrabold tracking-tight min-w-0 ${valueColorClass}`} style={{ wordBreak: 'break-word' }}>
                    {value}
                </span>

                    {showExtra && (
                        <span className={`text-sm 2xl:text-base font-bold tracking-wider ${extraColor || 'text-on-surface-variant'} whitespace-nowrap`}>
                        {extraText || 'N/A'}
                        </span>
                    )}
                </div>
            )}
        </div>
    );
}
