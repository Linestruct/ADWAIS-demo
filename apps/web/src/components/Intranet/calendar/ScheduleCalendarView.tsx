// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Repeat2 } from 'lucide-react';
import type { CalendarEventDto } from '@types';
import { EmptyState } from '../../common/ui/EmptyState';
import { formatDateTime } from '../../../utils/dateTime';
import { getEventBadgeClass, getEventEmoji } from './calendarPresentation';

interface ScheduleCalendarViewProps {
  events: CalendarEventDto[];
  onEventClick: (event: CalendarEventDto) => void;
}

export function ScheduleCalendarView({ events, onEventClick }: ScheduleCalendarViewProps) {
  return (
    <div className="flex max-h-[500px] flex-1 flex-col overflow-y-auto border-t border-outline-variant bg-surface custom-scrollbar">
      {events.length === 0 ? (
        <EmptyState message="No upcoming events." variant="minimal" className="min-h-32" />
      ) : (
        <div className="flex flex-col gap-2 p-3">
          {events.map(event => (
            <div key={`${event.id}-${event.startTime}`} onClick={() => onEventClick(event)} className={`m3-elevation-1 flex cursor-pointer items-center justify-between gap-8 rounded-xl p-3 text-sm font-bold leading-tight transition-all hover:m3-elevation-2 ${getEventBadgeClass(event.eventType)}`}>
              <div className="flex min-w-0 flex-1 items-center gap-7">
                <span className="shrink-0 text-2xl">{getEventEmoji(event.eventType)}</span>
                <div className="flex min-w-0 flex-1 flex-col">
                  <span className="flex items-center gap-1.5"><span className="truncate text-base font-bold">{event.title}</span>{event.isRecurring && <Repeat2 size={14} className="shrink-0" aria-label="Recurring event" />}</span>
                  {(event.location || event.isRecurring) && (
                    <span className="mt-0.5 flex max-w-full items-center gap-3 truncate text-sm font-medium opacity-70">
                      {event.location && <span className="truncate uppercase tracking-wider">📍 {event.location}</span>}
                      {event.isRecurring && <span className="shrink-0">Repeats {(event.recurrence || 'recurring').toLowerCase()}</span>}
                    </span>
                  )}
                </div>
              </div>
              <div className="flex shrink-0 flex-col items-end pl-4 text-right">
                <span className="text-sm font-bold capitalize">{formatDateTime(event.startTime, { weekday: 'long', month: 'short', day: 'numeric' }, 'en-SE')}</span>
                <span className="mt-0.5 text-sm font-medium opacity-70">{formatDateTime(event.startTime, { hour: '2-digit', minute: '2-digit' })}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
