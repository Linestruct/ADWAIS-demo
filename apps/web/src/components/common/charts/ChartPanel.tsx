// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';
import type { ComparisonPeriod } from '@types';
import { ChartSkeleton } from './ChartSkeleton';
import { Loader2 } from 'lucide-react';
import { ErrorAlert } from '../ui/ErrorAlert';

interface ChartPanelProps {
  title: string;
  subtitle?: string;
  comparison?: ComparisonPeriod;
  legend?: ReactNode;
  bodyClassName?: string;
  className?: string;
  isLoading?: boolean;
  isError?: boolean;
  isStale?: boolean;
  children: ReactNode;
}

export function ChartPanel({ title, subtitle, comparison, legend, bodyClassName = '', className = '', isLoading = false, isError = false, isStale = false, children }: ChartPanelProps) {
    const displaySubtitle = subtitle || (comparison ? (comparison === 'YearOverYear' ? 'vs. same period last year' : 'vs. preceding period') : undefined);
    return (
        <div className={`bg-surface border border-outline rounded-2xl sm:p-4 p-2 flex flex-col min-h-[600px] contained:min-h-0 relative overflow-hidden ${className}`}>
            {isStale && !isLoading && (
                <div className="absolute inset-0 bg-surface/50 backdrop-blur-[1px] z-20 flex flex-col flex-wrap items-center justify-center animate-in fade-in duration-200">
                    <Loader2 size={32} className="text-on-surface-variant animate-spin opacity-80" />
                    <span className="text-sm md:text-base text-on-surface-variant font-bold uppercase tracking-wide mt-2 bg-surface/80 px-2 py-1 rounded">Updating...</span>
                </div>
            )}
            <div className="flex justify-between items-start my-1 z-10">
                <div className="min-w-[35%] flex flex-col pl-1 sm:p-0 gap-1">
                    <span className="text-sm md:text-base font-bold text-on-surface-variant uppercase tracking-wide">
                        {title}
                    </span>
                    {displaySubtitle && (
                    <span className="text-sm md:text-base font-medium text-on-surface-variant">
                        {displaySubtitle}
                    </span>
                    )}
                </div>
                {legend}
            </div>
            <div className={`flex-1 min-h-[280px] contained:min-h-0 w-full flex flex-col z-10 relative ${bodyClassName}`}>
                {isLoading ? <ChartSkeleton /> : isError ? (
                  <div className="p-4"><ErrorAlert title={`${title} unavailable`} message={`${title} is temporarily unavailable.`} /></div>
                ) : children}
            </div>
        </div>
    );
}
