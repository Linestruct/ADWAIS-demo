// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';

interface TileCardProps {
  header: ReactNode;
  headerActions?: ReactNode;
  children: ReactNode;
  isUnassigned?: boolean;
  className?: string;
}

export function TileCard({
  header,
  headerActions,
  children,
  isUnassigned = false,
  className = '',
}: TileCardProps) {
  return (
    <article className={`flex shrink-0 flex-col overflow-hidden rounded-xl m3-elevation-1 transition-colors hover:m3-elevation-2 ${className} ${isUnassigned ? 'border-error border' : ''}`}>
      <header className={`flex items-center justify-between gap-3 p-4`}>
        <div className="flex flex-col gap-1 min-w-0 flex-1">
          {header}
        </div>
        {headerActions && (
          <div className="flex items-center gap-2 ml-2">
            {headerActions}
          </div>
        )}
      </header>
      <div className="flex flex-col gap-3 px-4 pb-4">
        {children}
      </div>
    </article>
  );
}
