// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Plus, Repeat2 } from 'lucide-react';
import type { CalendarEventDto } from '@types';
import { formatDateTime } from '../../../utils/dateTime';
import { getEventCircleColor } from './calendarPresentation';

export interface CalendarCell {
  date: Date;
  isCurrentMonth: boolean;
}

interface MonthCalendarViewProps {
  cells: CalendarCell[];
  isWriter: boolean;
  getEventsForDay: (date: Date) => CalendarEventDto[];
  onDayClick: (date: Date) => void;
  onEventClick: (event: CalendarEventDto) => void;
}

export function MonthCalendarView({ cells, isWriter, getEventsForDay, onDayClick, onEventClick }: MonthCalendarViewProps) {
  return (
    <div className="flex min-h-0 flex-1 flex-col bg-surface px-4 py-2">
      <div className="mb-1 grid grid-cols-7 gap-1 text-center text-sm font-black uppercase tracking-wide text-on-surface-variant">
        {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map(day => <div key={day}>{day}</div>)}
      </div>
      <div className="grid min-h-[350px] flex-1 grid-cols-7 gap-px overflow-hidden rounded-2xl border border-outline-variant bg-surface-container shadow-sm">
        {cells.map(cell => {
          const dayEvents = getEventsForDay(cell.date);
          const isToday = cell.date.toDateString() === new Date().toDateString();
          return (
            <div
              key={cell.date.toISOString()}
              onClick={() => onDayClick(cell.date)}
              className={`group relative flex min-h-[50px] flex-col p-1.5 transition-all md:min-h-[65px] ${cell.isCurrentMonth ? '' : 'bg-surface-container-low opacity-40'} ${isToday ? 'bg-brand-active' : 'bg-surface hover:bg-brand-hover'} ${isWriter ? 'cursor-pointer' : ''}`}
            >
              <div className="mb-1 flex items-center justify-between">
                <span className={`text-sm font-black ${isToday ? 'rounded-full bg-brand-bg-secondary px-1.5 py-0.5 font-bold text-white' : 'text-slate-650'}`}>{cell.date.getDate()}</span>
                {isWriter && cell.isCurrentMonth && <Plus size={10} className="text-on-surface-variant opacity-0 transition group-hover:opacity-100" />}
              </div>
              <div className="flex flex-1 flex-wrap items-center justify-center gap-2 py-1">
                {dayEvents.map(event => (
                  <span
                    key={`${event.id}-${event.startTime}`}
                    onClick={e => { e.stopPropagation(); onEventClick(event); }}
                    className="flex cursor-pointer items-center gap-0.5 transition-all hover:scale-125"
                    title={`${event.title || 'Event'} (${formatDateTime(event.startTime, { hour: '2-digit', minute: '2-digit' })})${event.isRecurring ? ` · Repeats ${(event.recurrence || 'recurring').toLowerCase()}` : ''}`}
                  >
                    <span className={`h-2.5 w-2.5 rounded-full border border-white shadow-sm ${getEventCircleColor(event.eventType)}`} />
                    {event.isRecurring && <Repeat2 size={10} aria-label="Recurring event" />}
                  </span>
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
