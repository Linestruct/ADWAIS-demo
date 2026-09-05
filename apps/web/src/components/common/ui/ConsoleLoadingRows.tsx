// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Skeleton } from './Skeleton';

interface ConsoleLoadingRowsProps {
  label: string;
  rowCount?: number;
}

export function ConsoleLoadingRows({ label, rowCount = 5 }: ConsoleLoadingRowsProps) {
  return (
    <div className="flex flex-col gap-3 p-3" aria-busy="true" aria-label={label}>
      {Array.from({ length: rowCount }, (_, index) => (
        <div key={index} className="flex items-center gap-4" aria-hidden="true">
          <Skeleton className="h-4 w-36 bg-console-hover" />
          <Skeleton className="h-4 flex-1 bg-console-hover" />
          <Skeleton className="h-4 w-16 bg-console-hover" />
        </div>
      ))}
    </div>
  );
}
