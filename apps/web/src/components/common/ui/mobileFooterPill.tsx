// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState, type Ref } from 'react';
import { SlidersHorizontal, X } from 'lucide-react';
import type { PersistentDomain } from '../../../utils/timeframeStorage.ts';
import { PeriodSelector } from '../charts/PeriodSelector.tsx';
import { SyncStatusWidget } from '../dashboard/SyncStatusWidget.tsx';

interface MobileFooterPillProps {
  domain: PersistentDomain;
  hasPageActions?: boolean;
  pageActionsPanelRef?: Ref<HTMLDivElement>;
  pageActionsIndicatorRef?: Ref<HTMLSpanElement>;
  pageActionsQuickRef?: Ref<HTMLDivElement>;
}

/** Mobile equivalent of the desktop dashboard footer. */
export function MobileFooterPill({
  domain,
  hasPageActions = false,
  pageActionsPanelRef,
  pageActionsIndicatorRef,
  pageActionsQuickRef,
}: MobileFooterPillProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      {isOpen && (
        <button
          type="button"
          className="fixed inset-0 z-30 cursor-default bg-scrim"
          data-md3-ripple="off"
          onClick={() => setIsOpen(false)}
          aria-label="Close dashboard controls"
        />
      )}

      <div className="relative z-40 flex flex-col items-start gap-2">
        {isOpen && (
          <div className="custom-scrollbar max-h-[calc(100dvh-5rem-env(safe-area-inset-bottom,0px))] w-[calc(100vw-1.5rem)] max-w-[440px] overflow-y-auto rounded-3xl bg-surface text-on-surface m3-elevation-4">
            {hasPageActions && (
              <>
                <div ref={pageActionsPanelRef} />
                <div className="mx-4 h-px bg-outline-variant" />
              </>
            )}

            <section className="flex flex-col gap-3 p-4">
              <h2 className="m-0 text-sm font-black uppercase tracking-wide text-on-surface-variant">Timeframe</h2>
              <PeriodSelector from={domain} embedded />
            </section>

            <div className="mx-4 h-px bg-outline-variant" />

            <section className="flex flex-col gap-3 p-4">
              <h2 className="m-0 text-sm font-black uppercase tracking-wide text-on-surface-variant">Sync status</h2>
              <SyncStatusWidget embedded />
            </section>
          </div>
        )}

        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setIsOpen(open => !open)}
            className={`relative flex h-14 w-14 cursor-pointer items-center justify-center transition-all duration-200 m3-elevation-2 hover:m3-elevation-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-secondary ${
              hasPageActions
                ? 'rounded-l-[28px] rounded-r-[8px]'
                : 'rounded-full'
            } ${
              isOpen
                ? 'bg-primary text-on-primary'
                : 'bg-surface-container-high hover:bg-surface-container-highest text-on-surface'
            }`}
            aria-label={isOpen ? 'Close dashboard controls' : 'Open dashboard controls'}
            aria-describedby={hasPageActions ? 'mobile-filter-status' : undefined}
            aria-expanded={isOpen}
          >
            {isOpen ? <X size={22} /> : <SlidersHorizontal size={22} />}
            {hasPageActions && (
              <span
                id="mobile-filter-status"
                ref={pageActionsIndicatorRef}
                className="pointer-events-none absolute right-[1px] top-[1px]"
              />
            )}
          </button>
          {hasPageActions && <div ref={pageActionsQuickRef} className="flex" />}
        </div>
      </div>
    </>
  );
}
