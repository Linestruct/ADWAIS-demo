// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '../../common/ui/Button';

interface CalendarNavigationProps {
  label: string;
  onPrevious: () => void;
  onToday: () => void;
  onNext: () => void;
}

export function CalendarNavigation({ label, onPrevious, onToday, onNext }: CalendarNavigationProps) {
  const iconButtonClass = '!h-9 !min-h-9 !w-9 !px-0 !border-surface-container-highest !text-on-surface-variant enabled:hover:!bg-surface-container focus-visible:!outline-tertiary';
  return (
    <div className="flex flex-wrap gap-2 items-center justify-end bg-surface px-5 pb-2">
      <h3 className="text-sm flex flex-1 whitespace-nowrap font-black uppercase tracking-wider text-on-surface">{label}</h3>
      <div className="flex items-center gap-1">
        <Button onClick={onPrevious} variant="outlined" color="surface" icon={<ChevronLeft size={20} />} className={iconButtonClass} aria-label="Previous period" />
        <Button onClick={onToday} variant="outlined" color="surface">Today</Button>
        <Button onClick={onNext} variant="outlined" color="surface" icon={<ChevronRight size={20} />} className={iconButtonClass} aria-label="Next period" />
      </div>
    </div>
  );
}
