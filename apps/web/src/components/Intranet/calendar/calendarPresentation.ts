// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { CalendarEventDto } from '@types';
import { formatDateTime } from '../../../utils/dateTime';

const EVENT_STYLES: Record<string, { badge: string; circle: string }> = {
  Meeting: { badge: 'bg-sky-200 hover:bg-sky-300 text-sky-950 border-0', circle: 'bg-sky-500' },
  Fika: { badge: 'bg-amber-200 hover:bg-amber-300 text-amber-950 border-0', circle: 'bg-amber-500' },
  Social: { badge: 'bg-purple-200 hover:bg-purple-300 text-purple-950 border-0', circle: 'bg-purple-500' },
  Birthday: { badge: 'bg-pink-200 hover:bg-pink-300 text-pink-950 border-0', circle: 'bg-pink-500' },
  GoLive: { badge: 'bg-emerald-200 hover:bg-emerald-300 text-emerald-950 border-0', circle: 'bg-emerald-500' },
  ExternalSync: { badge: 'bg-cyan-200 hover:bg-cyan-300 text-cyan-950 border-0', circle: 'bg-cyan-500' },
};

const DEFAULT_STYLE = {
  badge: 'bg-surface-container hover:bg-surface-container-high text-on-surface border-0',
  circle: 'bg-slate-500',
};

export const getEventBadgeClass = (eventType?: string) =>
  eventType ? EVENT_STYLES[eventType]?.badge ?? DEFAULT_STYLE.badge : DEFAULT_STYLE.badge;

export const getEventCircleColor = (eventType?: string) =>
  eventType ? EVENT_STYLES[eventType]?.circle ?? DEFAULT_STYLE.circle : DEFAULT_STYLE.circle;

export const getEventEmoji = (type?: string) => {
  switch (type) {
    case 'Meeting': return '🤝';
    case 'Fika': return '☕';
    case 'Social': return '🎉';
    case 'Birthday': return '🎂';
    case 'GoLive': return '🚀';
    case 'ExternalSync': return '🔄';
    default: return '📅';
  }
};

const isSameDay = (a: Date, b: Date) =>
  a.getFullYear() === b.getFullYear()
  && a.getMonth() === b.getMonth()
  && a.getDate() === b.getDate();

const eventDates = (event: CalendarEventDto) => {
  const start = new Date(event.startTime ?? '');
  const end = new Date(event.endTime ?? '');
  return Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) ? null : { start, end };
};

const formatTime = (date: Date) => formatDateTime(date, { hour: '2-digit', minute: '2-digit' });

export const getEventDayTimingLabel = (event: CalendarEventDto, day: Date) => {
  const dates = eventDates(event);
  if (!dates) return 'Time unavailable';
  if (isSameDay(dates.start, dates.end)) return formatTime(dates.start);
  if (isSameDay(day, dates.start)) return `${formatTime(dates.start)} · Starts →`;
  if (isSameDay(day, dates.end)) return `← Ends ${formatTime(dates.end)}`;
  return '← Continues →';
};

export const getEventRangeLabel = (event: CalendarEventDto) => {
  const dates = eventDates(event);
  if (!dates) return 'Time unavailable';

  const formatDate = (date: Date) => formatDateTime(
    date,
    { weekday: 'short', month: 'short', day: 'numeric' },
    'en-SE',
  );

  if (isSameDay(dates.start, dates.end)) {
    return `${formatDate(dates.start)}, ${formatTime(dates.start)}–${formatTime(dates.end)}`;
  }

  return `${formatDate(dates.start)}, ${formatTime(dates.start)} → ${formatDate(dates.end)}, ${formatTime(dates.end)}`;
};
