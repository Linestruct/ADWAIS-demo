// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState, useMemo, useEffect, useRef } from 'react';
import { CollectionPanel } from '../common/dashboard/CollectionPanel';
import { ErrorAlert } from '../common/ui/ErrorAlert';
import { getApiIntranetEventsId, useGetApiUsersMe } from '../../api/generated/endpoints';
import { getKioskToken } from '../../utils/auth';
import { formatDateTime } from '../../utils/dateTime';
import { 
  useCalendarEventsQuery, 
  useCreateCalendarEventMutation, 
  useUpdateCalendarEventMutation, 
  useDeleteCalendarEventMutation,
  useCalendarTokenQuery,
  useRegenerateCalendarTokenMutation
} from '../../hooks/useCalendarQueries';
import { toast } from 'sonner';
import { EventType, RecurrenceType } from '@types';
import type { CalendarEventDto } from '@types';
import { getMockCalendarEvents } from './calendar/mockEvents';

// Sub-components
import { CalendarEventDetailModal } from './CalendarEventDetailModal';
import { CalendarEventFormModal } from './CalendarEventFormModal';
import { CalendarSettingsModal } from './CalendarSettingsModal';
import { CalendarToolbar, type CalendarViewMode } from './calendar/CalendarToolbar';
import { CalendarNavigation } from './calendar/CalendarNavigation';
import { MonthCalendarView } from './calendar/MonthCalendarView';
import { WeekCalendarView } from './calendar/WeekCalendarView';
import { ScheduleCalendarView } from './calendar/ScheduleCalendarView';

export function Calendar() {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [viewMode, setViewMode] = useState<CalendarViewMode>('week');
  const todayRef = useRef<HTMLDivElement | null>(null);
  
  // Modals/panels visibility
  const [selectedEvent, setSelectedEvent] = useState<CalendarEventDto | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isTokenRequested, setIsTokenRequested] = useState(false);

  // Form states
  const [eventForm, setEventForm] = useState<{
    title: string;
    description: string;
    location: string;
    startTime: string;
    endTime: string;
    eventType: EventType;
    recurrence: RecurrenceType;
  }>({
    title: '',
    description: '',
    location: '',
    startTime: '',
    endTime: '',
    eventType: 'General',
    recurrence: 'None'
  });

  const resetEventForm = () => {
    setEventForm({
      title: '',
      description: '',
      location: '',
      startTime: '',
      endTime: '',
      eventType: 'General' as EventType,
      recurrence: 'None' as RecurrenceType
    });
  };

  // Queries
  const { data: meData } = useGetApiUsersMe();
  const userRole = meData?.data?.role;
  const isKioskDevice = !!getKioskToken() && userRole !== 'Admin' && userRole !== 'Employee';
  const isWriter = !isKioskDevice && (userRole === 'Admin' || userRole === 'Employee');
  const isAdmin = !isKioskDevice && userRole === 'Admin';

  // Fetch a larger window to support month/week view offsets cleanly
  const rangeBoundaries = useMemo(() => {
    const start = new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1);
    const end = new Date(currentDate.getFullYear(), currentDate.getMonth() + 2, 0);
    return {
      start: start.toISOString(),
      end: end.toISOString()
    };
  }, [currentDate]);

  // Load events
  const { data: rawEvents = [], isLoading, isError } = useCalendarEventsQuery(rangeBoundaries.start, rangeBoundaries.end);

  useEffect(() => {
    if (viewMode === 'week' && !isLoading && todayRef.current) {
      const timer = setTimeout(() => {
        todayRef.current?.scrollIntoView({
          behavior: 'smooth',
          block: 'nearest',
          inline: 'center'
        });
      }, 100);
      return () => clearTimeout(timer);
    }
  }, [viewMode, currentDate, isLoading]);

  const events = useMemo(
    () => [...rawEvents, ...getMockCalendarEvents(currentDate)],
    [rawEvents, currentDate],
  );
  const { data: tokenData } = useCalendarTokenQuery(isWriter && isTokenRequested);

  // Mutations
  const createEventMutation = useCreateCalendarEventMutation(() => {
    setIsCreateModalOpen(false);
    resetEventForm();
  });
  const updateEventMutation = useUpdateCalendarEventMutation(() => {
    setIsEditModalOpen(false);
    setSelectedEvent(null);
    resetEventForm();
  });
  const deleteEventMutation = useDeleteCalendarEventMutation(() => {
    setSelectedEvent(null);
  });
  const regenerateTokenMutation = useRegenerateCalendarTokenMutation();

  // Calendar calculations
  const year = currentDate.getFullYear();
  const month = currentDate.getMonth();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const firstDayIndex = (new Date(year, month, 1).getDay() + 6) % 7; // Mon=0
  const prevMonthDays = new Date(year, month, 0).getDate();

  const calendarCells = useMemo(() => {
    const cells = [];
    
    // Prev Month Padding
    for (let i = firstDayIndex - 1; i >= 0; i--) {
      cells.push({
        date: new Date(year, month - 1, prevMonthDays - i),
        isCurrentMonth: false,
      });
    }
    
    // Current Month Days
    for (let i = 1; i <= daysInMonth; i++) {
      cells.push({
        date: new Date(year, month, i),
        isCurrentMonth: true,
      });
    }
    
    // Next Month Padding
    const remainingCells = 42 - cells.length;
    for (let i = 1; i <= remainingCells; i++) {
      cells.push({
        date: new Date(year, month + 1, i),
        isCurrentMonth: false,
      });
    }
    return cells;
  }, [year, month, daysInMonth, firstDayIndex, prevMonthDays]);

  // Week View Calculations
  const weekDays = useMemo(() => {
    const days = [];
    const currentDayOfWeek = (currentDate.getDay() + 6) % 7; // Mon=0
    const startOfWeek = new Date(currentDate);
    startOfWeek.setDate(currentDate.getDate() - currentDayOfWeek);
    
    for (let i = 0; i < 7; i++) {
      const day = new Date(startOfWeek);
      day.setDate(startOfWeek.getDate() + i);
      days.push(day);
    }
    return days;
  }, [currentDate]);

  const handlePrev = () => {
    if (viewMode === 'month') {
      setCurrentDate(new Date(year, month - 1, 1));
    } else if (viewMode === 'week') {
      const d = new Date(currentDate);
      d.setDate(d.getDate() - 7);
      setCurrentDate(d);
    } else {
      const d = new Date(currentDate);
      d.setDate(d.getDate() - 14);
      setCurrentDate(d);
    }
  };

  const handleNext = () => {
    if (viewMode === 'month') {
      setCurrentDate(new Date(year, month + 1, 1));
    } else if (viewMode === 'week') {
      const d = new Date(currentDate);
      d.setDate(d.getDate() + 7);
      setCurrentDate(d);
    } else {
      const d = new Date(currentDate);
      d.setDate(d.getDate() + 14);
      setCurrentDate(d);
    }
  };

  const handleCellClick = (date: Date) => {
    if (!isWriter) {
      toast.error('You do not have permission to add calendar events.');
      return;
    }
    const localDateTimeStr = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
    setEventForm({
      title: '',
      description: '',
      location: '',
      startTime: localDateTimeStr,
      endTime: new Date(date.getTime() + 60 * 60 * 1000 - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16),
      eventType: 'General' as EventType,
      recurrence: 'None' as RecurrenceType
    });
    setIsCreateModalOpen(true);
  };

  const handleCreateSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createEventMutation.mutate({
      title: eventForm.title,
      description: eventForm.description || null,
      location: eventForm.location || null,
      startTime: new Date(eventForm.startTime).toISOString(),
      endTime: new Date(eventForm.endTime).toISOString(),
      eventType: eventForm.eventType,
      isRecurring: eventForm.recurrence !== RecurrenceType.None,
      recurrence: eventForm.recurrence
    });
  };

  const handleEditSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedEvent?.id) return;
    if (selectedEvent.id.toString().startsWith('mock-')) {
      toast.success('Mock event updated successfully.');
      setIsEditModalOpen(false);
      setSelectedEvent(null);
      resetEventForm();
      return;
    }
    updateEventMutation.mutate({
      id: selectedEvent.id,
      dto: {
        title: eventForm.title,
        description: eventForm.description || null,
        location: eventForm.location || null,
        startTime: new Date(eventForm.startTime).toISOString(),
        endTime: new Date(eventForm.endTime).toISOString(),
        eventType: eventForm.eventType,
        isRecurring: eventForm.recurrence !== RecurrenceType.None,
        recurrence: eventForm.recurrence
      }
    });
  };

  const openEditModal = async (event: CalendarEventDto) => {
    setSelectedEvent(null);
    let editableEvent = event;
    if (event.isRecurring && event.id) {
      try {
        editableEvent = (await getApiIntranetEventsId(event.id)).data;
      } catch {
        toast.error('Failed to load the recurring series.');
        return;
      }
    }

    setEventForm({
      title: editableEvent.title || '',
      description: editableEvent.description || '',
      location: editableEvent.location || '',
      startTime: editableEvent.startTime ? new Date(new Date(editableEvent.startTime).getTime() - new Date(editableEvent.startTime).getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '',
      endTime: editableEvent.endTime ? new Date(new Date(editableEvent.endTime).getTime() - new Date(editableEvent.endTime).getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '',
      eventType: (editableEvent.eventType || 'General') as EventType,
      recurrence: (editableEvent.recurrence || 'None') as RecurrenceType
    });
    setSelectedEvent(editableEvent);
    setIsEditModalOpen(true);
  };

  const copyFeedLink = () => {
    if (!tokenData?.token) return;
    const feedUrl = `${window.location.origin}/api/intranet/calendar/feed.ics?token=${tokenData.token}`;
    navigator.clipboard.writeText(feedUrl);
    toast.success('Subscription link copied to clipboard.');
  };

  const getEventsForDay = (dayDate: Date) => {
    return events.filter(event => {
      if (!event.startTime || !event.endTime) return false;
      const start = new Date(event.startTime);
      const end = new Date(event.endTime);
      
      const d = new Date(dayDate);
      d.setHours(0,0,0,0);
      
      const s = new Date(start);
      s.setHours(0,0,0,0);
      
      const e = new Date(end);
      e.setHours(0,0,0,0);
      
      return d >= s && d <= e;
    });
  };

  const navLabel = useMemo(() => {
    if (viewMode === 'month') {
      return formatDateTime(currentDate, { month: 'long', year: 'numeric' }, 'en-SE');
    }
    if (viewMode === 'week') {
      const start = weekDays[0];
      const end = weekDays[6];
      return `${formatDateTime(start, { month: 'short', day: 'numeric' }, 'en-SE')} – ${formatDateTime(end, { month: 'short', day: 'numeric', year: 'numeric' }, 'en-SE')}`;
    }
    return `Upcoming Events`;
  }, [currentDate, viewMode, weekDays]);

  const scheduleEvents = useMemo(() => {
    const now = new Date(currentDate);
    now.setHours(0,0,0,0);
    return events
      .filter(e => e.endTime && new Date(e.endTime) >= now)
      .sort((a, b) => new Date(a.startTime ?? 0).getTime() - new Date(b.startTime ?? 0).getTime())
      .slice(0, 20);
  }, [events, currentDate]);

  return (
      <CollectionPanel
        title="Company Calendar"
      isLoading={isLoading}
      className="h-full relative select-none"
      actions={
        <CalendarToolbar
          viewMode={viewMode}
          isWriter={isWriter}
          onViewModeChange={setViewMode}
          onAddEvent={() => handleCellClick(new Date())}
          onOpenSettings={() => setIsSettingsOpen(true)}
        />
      }
    >
      {isError ? (
        <div className="p-4"><ErrorAlert title="Calendar unavailable" message="The company calendar is temporarily unavailable." /></div>
      ) : (
      <div className="flex flex-col h-full min-h-[350px] bg-surface-container">
        
        {/* Month/Week Navigation */}
        {viewMode !== 'schedule' && (
          <CalendarNavigation
            label={navLabel}
            onPrevious={handlePrev}
            onToday={() => setCurrentDate(new Date())}
            onNext={handleNext}
          />
        )}

        {/* ── MONTH VIEW ── */}
        {viewMode === 'month' && (
          <MonthCalendarView
            cells={calendarCells}
            isWriter={isWriter}
            getEventsForDay={getEventsForDay}
            onDayClick={handleCellClick}
            onEventClick={setSelectedEvent}
          />
        )}

        {/* ── WEEK VIEW ── */}
        {viewMode === 'week' && (
          <WeekCalendarView
            days={weekDays}
            isWriter={isWriter}
            todayRef={todayRef}
            getEventsForDay={getEventsForDay}
            onDayClick={handleCellClick}
            onEventClick={setSelectedEvent}
          />
        )}

        {/* ── SCHEDULE / LIST VIEW ── */}
        {viewMode === 'schedule' && (
          <ScheduleCalendarView events={scheduleEvents} onEventClick={setSelectedEvent} />
        )}

      </div>
      )}

      {/* Detail / View Modal */}
      {selectedEvent && (
        <CalendarEventDetailModal
          event={selectedEvent}
          isOpen={!!selectedEvent}
          onClose={() => setSelectedEvent(null)}
          isWriter={isWriter}
          onDelete={(id) => {
            if (id.toString().startsWith('mock-')) {
              toast.success('Mock event deleted successfully.');
              setSelectedEvent(null);
            } else {
              deleteEventMutation.mutate(id);
            }
          }}
          onEdit={openEditModal}
        />
      )}

      {/* Create Modal */}
      <CalendarEventFormModal
        mode="create"
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSubmit={handleCreateSubmit}
        form={eventForm}
        onChange={setEventForm}
      />

      {/* Edit Modal */}
      <CalendarEventFormModal
        mode="edit"
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        onSubmit={handleEditSubmit}
        form={eventForm}
        onChange={setEventForm}
      />

      {/* Calendar Settings Modal */}
      <CalendarSettingsModal
        isOpen={isSettingsOpen}
        onClose={() => {
          setIsSettingsOpen(false);
          setIsTokenRequested(false);
        }}
        isAdmin={isAdmin}
        isWriter={isWriter}
        token={tokenData?.token ?? undefined}
        isTokenRequested={isTokenRequested}
        onGenerateToken={() => setIsTokenRequested(true)}
        onCopyFeedLink={copyFeedLink}
        onRegenerateToken={() => regenerateTokenMutation.mutate(undefined)}
      />
    </CollectionPanel>
  );
}
