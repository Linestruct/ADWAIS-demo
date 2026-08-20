// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';

interface SubSectionHeaderProps {
  title: string;
  subtitle: string;
  icon: ReactNode;
  children?: ReactNode;
  className?: string;
}

export function SubSectionHeader({ title, subtitle, icon, children, className = '' }: SubSectionHeaderProps) {
  return (
    <header className={`flex flex-wrap items-start justify-between gap-3 p-4 ${className}`}>
      <div className="flex min-w-0 items-start gap-3">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary-container text-on-primary-container">
          {icon}
        </div>
        <div className="min-w-0 pt-0.5">
          <h3 className="text-base font-black text-on-surface">{title}</h3>
          <p className="mt-1 text-sm font-medium leading-5 text-on-surface-variant">{subtitle}</p>
        </div>
      </div>
      {children && (
        <div className="flex flex-wrap items-center justify-end gap-2">
          {children}
        </div>
      )}
    </header>
  );
}
