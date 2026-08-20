// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

interface EmptyStateProps {
  message: string;
  className?: string;
  isTableRow?: boolean;
  colSpan?: number;
  variant?: 'default' | 'minimal';
}

export function EmptyState({ message, className = '', isTableRow = false, colSpan = 1, variant = 'default' }: EmptyStateProps) {
  const baseClasses = variant === 'default' 
    ? "text-center py-10 text-on-surface-variant text-sm font-medium border-2 border-dashed border-outline-variant rounded-xl bg-surface"
    : "flex items-center justify-center h-full w-full text-sm font-bold text-on-surface-variant uppercase tracking-wide text-center p-4";

  const content = (
    <div className={`${baseClasses} ${className}`}>
      {message}
    </div>
  );

  if (isTableRow) {
    return (
      <tr>
        <td colSpan={colSpan} className="p-0">
          {content}
        </td>
      </tr>
    );
  }

  return content;
}
