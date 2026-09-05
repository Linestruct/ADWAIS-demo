// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { ChartSkeleton } from '../charts/ChartSkeleton';
import type { ReactNode } from 'react';

interface CollectionPanelProps {
  title: string;
  actions?: ReactNode;
  className?: string;
  titleClassName?: string;
  isLoading?: boolean;
  children: ReactNode;
}

export function CollectionPanel({ title, actions, className = '', titleClassName = '', isLoading, children }: CollectionPanelProps) {
  return (
    <section className={`bg-surface border border-outline rounded-2xl p-0 flex flex-col min-h-0 overflow-hidden max-h-[600px] xl:max-h-none contained:max-h-none landscape-lg:max-h-none ${className}`}>
      <div className="gap-1 flex flex-wrap justify-between items-center p-4">
        <h2 className={`text-base md:text-lg font-bold text-on-surface-variant uppercase tracking-wide m-0 ${titleClassName}`}>
          {title}
        </h2>
        {actions && (
          <div className="flex items-center gap-8">
            {actions}
          </div>
        )}
      </div>
      <div className="flex-1 overflow-y-auto custom-scrollbar min-h-0">
        {isLoading ? <ChartSkeleton /> : children}
      </div>
    </section>
  );
}
