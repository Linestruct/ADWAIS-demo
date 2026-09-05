// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';
import { SubSectionHeader } from './SubSectionHeader';

interface SettingsCardProps {
  title: string;
  subtitle: string;
  icon: ReactNode;
  /** Optional actions rendered in the header's right slot */
  headerActions?: ReactNode;
  children: ReactNode;
  className?: string;
}

export function SettingsCard({
  title,
  subtitle,
  icon,
  headerActions,
  children,
  className = '',
}: SettingsCardProps) {
  return (
    <article className={`flex border border-outline break-inside-avoid flex-col overflow-hidden rounded-xl ${className}`}>
      <SubSectionHeader title={title} subtitle={subtitle} icon={icon}>
        {headerActions}
      </SubSectionHeader>
      <div className="flex flex-col gap-4 px-4 pb-4">
        {children}
      </div>
    </article>
  );
}
