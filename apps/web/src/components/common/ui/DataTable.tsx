// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ReactNode } from 'react';

interface DataTableProps {
  children: ReactNode;
  filters?: ReactNode;
  className?: string;
  tableClassName?: string;
  viewportClassName?: string;
}

export function DataTable({
  children,
  filters,
  className = '',
  tableClassName = '',
  viewportClassName = '',
}: DataTableProps) {
  const defaultWhitespaceClass = tableClassName.split(/\s+/).some(className => className.startsWith('whitespace-'))
    ? ''
    : 'whitespace-nowrap';

  return (
    <div className={`flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden ${className}`}>
      {filters && <div className="shrink-0">{filters}</div>}
      <div className={`custom-scrollbar min-h-0 min-w-0 flex-1 overflow-x-auto overflow-y-auto ${viewportClassName}`}>
        <table className={`w-full ${defaultWhitespaceClass} text-left text-sm ${tableClassName}`}>
          {children}
        </table>
      </div>
    </div>
  );
}
