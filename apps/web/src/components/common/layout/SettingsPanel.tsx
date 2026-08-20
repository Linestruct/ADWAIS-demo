// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';
import { SettingsPanelHeader } from './SettingsPanelHeader';

interface SettingsPanelProps {
  children: ReactNode;
  className?: string;
  title?: string;
  subtitle?: string;
  icon?: ReactNode;
  headerActions?: ReactNode;
  contentClassName?: string;
}

export function SettingsPanel({ 
  children, 
  className = '', 
  title, 
  subtitle, 
  icon, 
  headerActions,
  contentClassName = 'custom-scrollbar flex-1 flex flex-col gap-3 overflow-y-auto p-3 sm:p-4' 
}: SettingsPanelProps) {
  const flexClass = className.includes('shrink-0') ? '' : 'flex-1';
  const maxHClass = className.includes('max-h-') ? '' : 'max-h-[500px]';
  
  return (
    <section className={`flex ${flexClass} contained:max-h-none ${maxHClass} min-h-0 min-w-0 flex-col overflow-hidden rounded-2xl border border-outline bg-surface ${className}`}>
      {title && (
        <SettingsPanelHeader title={title} subtitle={subtitle} icon={icon}>
          {headerActions}
        </SettingsPanelHeader>
      )}
      {title ? (
        <div className={contentClassName}>
          {children}
        </div>
      ) : (
        children
      )}
    </section>
  );
}
