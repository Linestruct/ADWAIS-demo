// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { CalendarEventDto } from '@types';

/** Mock events for design verification, generated relative to the given date. */
export function getMockCalendarEvents(date: Date): CalendarEventDto[] {
  const today = new Date(date);
  today.setHours(0, 0, 0, 0);

  const at = (hour: number, minute = 0) => {
    const d = new Date(today);
    d.setHours(hour, minute, 0, 0);
    return d.toISOString();
  };

  return [
    {
      id: 'mock-today-meeting',
      title: 'Project Status Sync',
      description:
        'Weekly sync across the dashboard teams: status on the ongoing tenant migration, open incidents, and priorities for the week ahead.',
      location: 'Meeting Room A',
      startTime: at(10, 0),
      endTime: at(11, 0),
      eventType: 'Meeting',
      isRecurring: false,
      recurrence: 'None'
    },
    {
      id: 'mock-today-fika',
      title: 'Afternoon Fika',
      description: 'Join the team for afternoon coffee and snacks.',
      location: 'Kitchen',
      startTime: at(14, 30),
      endTime: at(15, 0),
      eventType: 'Fika',
      isRecurring: false,
      recurrence: 'None'
    }
  ];
}
