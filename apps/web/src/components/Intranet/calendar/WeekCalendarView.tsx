// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { RefObject } from 'react';
import { Repeat2 } from 'lucide-react';
import type { CalendarEventDto } from '@types';
import { formatDateTime } from '../../../utils/dateTime';
import { getEventBadgeClass, getEventDayTimingLabel, getEventEmoji } from './calendarPresentation';

interface WeekCalendarViewProps {
  days: Date[];
  isWriter: boolean;
  todayRef: RefObject<HTMLDivElement | null>;
  getEventsForDay: (date: Date) => CalendarEventDto[];
  onDayClick: (date: Date) => void;
  onEventClick: (event: CalendarEventDto) => void;
}

export function WeekCalendarView({ days, isWriter, todayRef, getEventsForDay, onDayClick, onEventClick }: WeekCalendarViewProps) {
  return (
    <div className="flex min-w-0 flex-1 divide-x divide-slate-100 overflow-x-auto border-t border-outline-variant bg-surface custom-scrollbar">
      {days.map(day => {
        const dayEvents = getEventsForDay(day);
        const isToday = day.toDateString() === new Date().toDateString();
        return (
          <div data-md3-ripple key={day.toISOString()} ref={isToday ? todayRef : undefined} onClick={() => onDayClick(day)} className={`flex text-on-surface-variant h-full w-[200px] shrink-0 flex-col bg-surface p-2 transition-colors hover:bg-surface-container-low ${isWriter ? 'cursor-pointer' : ''}`}>
            <div className="mb-2 flex items-start justify-between gap-3 border-b border-outline-variant pb-2">
              <div className="flex min-w-0 flex-col">
                <span className="truncate text-sm font-black uppercase leading-tight tracking-wide text-on-surface-variant">{formatDateTime(day, { weekday: 'short' }, 'en-SE')}</span>
                <span className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-base font-black leading-none ${isToday ? 'bg-brand-accent text-brand-text' : 'text-on-surface'}`}>{day.getDate()}</span>
              </div>
              <span className="mt-0.5 shrink-0 whitespace-nowrap text-sm font-bold text-on-surface-variant">{dayEvents.length} {dayEvents.length === 1 ? 'Event' : 'Events'}</span>
            </div>
            <div className="flex flex-1 flex-col gap-2 overflow-y-auto p-1 custom-scrollbar">
              {dayEvents.map(event => (
                <div data-md3-ripple key={`${event.id}-${event.startTime}`} onClick={e => { e.stopPropagation(); onEventClick(event); }} className={`m3-elevation-1 flex min-h-16 shrink-0 cursor-pointer flex-col gap-2 rounded-xl p-3 text-sm font-bold leading-tight transition-all hover:m3-elevation-2 ${getEventBadgeClass(event.eventType)}`}>
                  <div className="flex items-center gap-2"><span>{getEventEmoji(event.eventType)}</span><span className="opacity-70">{getEventDayTimingLabel(event, day)}</span></div>
                  <span className="flex items-center gap-1.5"><span className="truncate">{event.title}</span>{event.isRecurring && <Repeat2 size={13} className="shrink-0" aria-label="Recurring event" />}</span>
                </div>
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}
