// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useContext, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { ListRestart, Settings } from 'lucide-react';
import { MobileFooterActionsSlotContext } from './MobileFooterActionsContext';

interface MobileFooterActionsProps {
  activeCount: number;
  clearLabel: string;
  onClearAll: () => void;
  settingsAction?: {
    label: string;
    onClick: () => void;
  };
  children: ReactNode;
}

export function MobileFooterActions({
  activeCount,
  clearLabel,
  onClearAll,
  settingsAction,
  children,
}: MobileFooterActionsProps) {
  const slots = useContext(MobileFooterActionsSlotContext);

  return (
    <>
      {slots.panel && createPortal(children, slots.panel)}
      {slots.indicator && activeCount > 0 && createPortal(
        <>
          <span
            aria-hidden="true"
            className="flex h-5 min-w-5 items-center justify-center rounded-full bg-secondary-container px-1 text-xs font-bold text-on-secondary-container m3-elevation-1"
          >
            {activeCount}
          </span>
          <span className="sr-only">{activeCount} active filters</span>
        </>,
        slots.indicator,
      )}
      {slots.quickAction && createPortal(
        <div className="flex gap-1">
          {settingsAction && (
            <button
              type="button"
              onClick={settingsAction.onClick}
              aria-label={settingsAction.label}
              title={settingsAction.label}
              className="flex h-14 w-14 cursor-pointer items-center justify-center rounded-[8px] bg-surface text-on-surface outline-none transition-colors m3-elevation-2 hover:bg-surface-container hover:m3-elevation-3 focus-visible:ring-2 focus-visible:ring-secondary"
            >
              <Settings aria-hidden="true" size={20} strokeWidth={2.5} />
            </button>
          )}
          <button
            type="button"
            onClick={onClearAll}
            disabled={activeCount === 0}
            aria-label={clearLabel}
            title={clearLabel}
            className="flex h-14 w-14 cursor-pointer items-center justify-center rounded-l-[8px] rounded-r-[28px] bg-error-container text-on-error-container outline-none transition-colors enabled:m3-elevation-2 enabled:hover:brightness-95 enabled:hover:m3-elevation-3 focus-visible:ring-2 focus-visible:ring-secondary disabled:cursor-not-allowed disabled:bg-on-surface/[0.20] disabled:text-on-surface/[0.38]"
          >
            <ListRestart aria-hidden="true" size={20} strokeWidth={2.5} />
          </button>
        </div>,
        slots.quickAction,
      )}
    </>
  );
}
