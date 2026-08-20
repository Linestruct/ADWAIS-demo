// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';

interface SectionHeaderProps {
  title: string;
  subtitle: string;
  icon: ReactNode;
  children?: ReactNode;
  className?: string;
  dark?: boolean;
}

export function SectionHeader({ title, subtitle, icon, children, className = '', dark = true }: SectionHeaderProps) {
  const bgClass = dark ? 'bg-brand-bg-secondary border-slate-700/50' : 'bg-surface border-outline-variant';
  const textClass = dark ? 'text-white' : 'text-on-surface';
  const subtitleClass = dark ? 'text-slate-300' : 'text-on-surface-variant';
  const iconBgClass = dark ? 'bg-surface/10 text-brand-accent' : 'bg-brand-accent/10 text-brand-accent';

  return (
    <div className={`flex flex-col md:flex-row md:items-center justify-between gap-6 md:gap-8 shrink-0 px-4 py-3 sm:p-4 shadow-sm rounded-xl z-10 ${bgClass} ${className}`}>
      <div className="flex items-center gap-4 w-full md:w-auto">
        <div className={`p-2 rounded-lg shadow-sm shrink-0 ${iconBgClass}`}>
          {icon}
        </div>
        <div className="min-w-0">
          <h2 className={`text-lg font-extrabold truncate ${textClass}`}>{title}</h2>
          <p className={`text-sm font-semibold truncate ${subtitleClass}`}>{subtitle}</p>
        </div>
      </div>
      {children && (
        <div className="flex flex-wrap items-center justify-end gap-2 w-full md:w-auto">
          {children}
        </div>
      )}
    </div>
  );
}
