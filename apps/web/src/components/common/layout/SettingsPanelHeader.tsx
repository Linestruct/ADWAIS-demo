// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';
import { ArrowLeft } from 'lucide-react';

interface SettingsPanelHeaderProps {
  title: string;
  subtitle?: string;
  icon?: ReactNode;
  iconContainerClassName?: string;
  children?: ReactNode;
  className?: string;
  onBack?: () => void;
}

export function SettingsPanelHeader({
  title,
  subtitle,
  icon,
  iconContainerClassName,
  children,
  className = '',
  onBack,
}: SettingsPanelHeaderProps) {
  return (
    <header className={`flex shrink-0 flex-wrap items-center justify-between gap-3 border-b border-outline-variant px-4 py-3 sm:px-5 sm:py-4 ${className}`}>
      <div className="flex min-w-0 items-center gap-3 sm:gap-4">
        {onBack && (
          <button
            type="button"
            onClick={onBack}
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container-high focus-visible:outline focus-visible:outline-2 focus-visible:outline-secondary"
            aria-label="Navigate back"
          >
            <ArrowLeft size={20} />
          </button>
        )}
        {icon && (
          <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${iconContainerClassName || 'bg-primary-container text-on-primary-container'}`}>
            {icon}
          </div>
        )}
        <div className="min-w-0">
          <h2 className="m-0 text-base font-black uppercase tracking-wide text-on-surface-variant sm:text-lg">
            {title}
          </h2>
          {subtitle && (
            <p className="m-0 text-sm font-bold leading-5 text-on-surface-variant">
              {subtitle}
            </p>
          )}
        </div>
      </div>
      {children && (
        <div className="flex min-w-0 flex-wrap items-center justify-end gap-2">
          {children}
        </div>
      )}
    </header>
  );
}
