// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useCallback, useEffect, useRef, useState } from 'react';
import type { CSSProperties, ReactNode } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

const CARD_HEIGHT = 234;
const CARD_GAP = 16;
const VIEWPORT_BOTTOM_PADDING = 16;
const MIN_TWO_ROW_CARD_HEIGHT = 168;

interface BulletinPostCarouselProps {
  children: ReactNode;
}

export function BulletinPostCarousel({ children }: BulletinPostCarouselProps) {
  const viewportRef = useRef<HTMLDivElement>(null);
  const [rowCount, setRowCount] = useState(1);
  const [cardHeight, setCardHeight] = useState(CARD_HEIGHT);
  const [canScrollBack, setCanScrollBack] = useState(false);
  const [canScrollForward, setCanScrollForward] = useState(false);

  const updateOverflow = useCallback(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;
    setCanScrollBack(viewport.scrollLeft > 1);
    setCanScrollForward(viewport.scrollLeft + viewport.clientWidth < viewport.scrollWidth - 1);
  }, []);

  useEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;

    const updateLayout = () => {
      const availableHeight = Math.max(0, viewport.clientHeight - VIEWPORT_BOTTOM_PADDING);
      const twoRowCardHeight = Math.floor((availableHeight - CARD_GAP) / 2);
      const canFitTwoRows = twoRowCardHeight >= MIN_TWO_ROW_CARD_HEIGHT;

      setRowCount(canFitTwoRows ? 2 : 1);
      setCardHeight(canFitTwoRows ? Math.min(CARD_HEIGHT, twoRowCardHeight) : CARD_HEIGHT);
      window.requestAnimationFrame(updateOverflow);
    };

    updateLayout();
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(updateLayout);
    observer?.observe(viewport);
    window.addEventListener('resize', updateLayout);
    return () => {
      observer?.disconnect();
      window.removeEventListener('resize', updateLayout);
    };
  }, [updateOverflow]);

  const scroll = (direction: -1 | 1) => {
    const viewport = viewportRef.current;
    if (!viewport) return;
    const card = viewport.querySelector<HTMLElement>('[data-bulletin-post-card]');
    const distance = (card?.offsetWidth ?? Math.min(352, viewport.clientWidth)) + CARD_GAP;
    viewport.scrollBy({ left: direction * distance, behavior: 'smooth' });
  };

  const bodyLineCount = cardHeight >= 220 ? 3 : cardHeight >= 196 ? 2 : 1;
  const gridStyle = {
    gridTemplateRows: `repeat(${rowCount}, ${cardHeight}px)`,
    gridAutoColumns: 'minmax(17rem, 22rem)',
    '--bulletin-post-body-lines': bodyLineCount,
  } as CSSProperties;

  return (
    <div className="relative h-full min-h-0">
      <div
        ref={viewportRef}
        onScroll={updateOverflow}
        className="grid h-full min-h-0 snap-x snap-mandatory grid-flow-col content-start items-start gap-4 overflow-x-auto overflow-y-hidden overscroll-x-contain p-4 pt-0 scroll-px-4 custom-scrollbar md:snap-proximity"
        style={gridStyle}
        aria-label="Bulletin board"
      >
        {children}
      </div>

      {canScrollBack && (
        <button type="button" onClick={() => scroll(-1)} aria-label="Previous bulletin posts" className="absolute left-2 top-1/2 z-10 hidden h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-surface text-on-surface m3-elevation-2 hover:bg-surface-container md:flex">
          <ChevronLeft className="h-5 w-5" aria-hidden="true" />
        </button>
      )}
      {canScrollForward && (
        <button type="button" onClick={() => scroll(1)} aria-label="Next bulletin posts" className="absolute right-2 top-1/2 z-10 hidden h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-surface text-on-surface m3-elevation-2 hover:bg-surface-container md:flex">
          <ChevronRight className="h-5 w-5" aria-hidden="true" />
        </button>
      )}
    </div>
  );
}
