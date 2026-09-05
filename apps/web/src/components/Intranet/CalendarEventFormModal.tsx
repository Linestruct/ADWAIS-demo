// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Calendar, Edit, X } from 'lucide-react';
import { EventType, RecurrenceType } from '@types';
import { DatePickerField, TimePickerField } from '../common/ui/DateTimePickerField';
import { FormField } from '../common/ui/FormField';

export type EventForm = {
  title: string;
  description: string;
  location: string;
  startTime: string;
  endTime: string;
  eventType: EventType;
  recurrence: RecurrenceType;
};

interface CalendarEventFormModalProps {
  mode: 'create' | 'edit';
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (event: React.FormEvent) => void;
  form: EventForm;
  onChange: (form: EventForm) => void;
}

export function CalendarEventFormModal({ mode, isOpen, onClose, onSubmit, form, onChange }: CalendarEventFormModalProps) {
  if (!isOpen) return null;

  const isEditing = mode === 'edit';
  const Icon = isEditing ? Edit : Calendar;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4 backdrop-blur-sm animate-in fade-in"
      role="presentation"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}
    >
      <form
        onSubmit={onSubmit}
        className="m3-elevation-4 flex w-full max-w-md flex-col overflow-hidden rounded-3xl border-0 bg-surface animate-in zoom-in-95"
        role="dialog"
        aria-modal="true"
        aria-labelledby={`${mode}-event-dialog-title`}
      >
        <div className="flex items-center justify-between bg-surface px-6 py-5">
          <h3 id={`${mode}-event-dialog-title`} className="flex items-center gap-4 text-xl font-bold text-on-surface">
            <Icon size={20} className="text-on-surface-variant" aria-hidden="true" />
            {isEditing ? 'Edit Calendar Event' : 'Add Calendar Event'}
          </h3>
          <button type="button" onClick={onClose} aria-label="Close event dialog" className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">
            <X size={20} aria-hidden="true" />
          </button>
        </div>

        <div className="flex max-h-[75vh] flex-col gap-4 overflow-y-auto bg-surface px-6 pb-6 custom-scrollbar">
          <FormField id={`${mode}-event-title`} label="Event Title" type="text" placeholder="Weekly Sync / Launch / etc." value={form.title} onChange={event => onChange({ ...form, title: event.target.value })} required />

          <div className="grid grid-cols-[minmax(0,1fr)_minmax(112px,0.65fr)] gap-3">
            <DatePickerField id={`${mode}-event-start-date`} label="Start date" value={form.startTime.split('T')[0] || ''} onChange={date => onChange({ ...form, startTime: `${date}T${form.startTime.split('T')[1] || '00:00'}` })} />
            <TimePickerField id={`${mode}-event-start-time`} label="Start time" value={form.startTime.split('T')[1] || ''} onChange={time => onChange({ ...form, startTime: `${form.startTime.split('T')[0] || new Date().toISOString().split('T')[0]}T${time}` })} />
            <DatePickerField id={`${mode}-event-end-date`} label="End date" value={form.endTime.split('T')[0] || ''} onChange={date => onChange({ ...form, endTime: `${date}T${form.endTime.split('T')[1] || '00:00'}` })} />
            <TimePickerField id={`${mode}-event-end-time`} label="End time" value={form.endTime.split('T')[1] || ''} onChange={time => onChange({ ...form, endTime: `${form.endTime.split('T')[0] || new Date().toISOString().split('T')[0]}T${time}` })} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <FormField as="select" id={`${mode}-event-category`} label="Event Category" value={form.eventType} onChange={event => onChange({ ...form, eventType: event.target.value as EventType })}>
                {Object.keys(EventType).map(key => <option key={key} value={key as EventType}>{key}</option>)}
            </FormField>
            <FormField id={`${mode}-event-location`} label="Location" type="text" placeholder="Kitchen / Zoom / Slack" value={form.location} onChange={event => onChange({ ...form, location: event.target.value })} />
          </div>

          <FormField as="textarea" id={`${mode}-event-description`} label="Description" rows={3} placeholder="Event scope, notes, etc." value={form.description} onChange={event => onChange({ ...form, description: event.target.value })} className="resize-none" />

          <FormField as="select" id={`${mode}-event-recurrence`} label="Repeat" value={form.recurrence} onChange={event => onChange({ ...form, recurrence: event.target.value as RecurrenceType })}>
            <option value={RecurrenceType.None}>Does not repeat</option>
            <option value={RecurrenceType.Daily}>Daily</option>
            <option value={RecurrenceType.Weekly}>Weekly</option>
            <option value={RecurrenceType.Monthly}>Monthly</option>
            <option value={RecurrenceType.Yearly}>Yearly</option>
          </FormField>

        </div>

        <div className="flex justify-end gap-3 bg-surface px-6 py-4">
          <button type="button" onClick={onClose} className="inline-flex min-h-11 items-center justify-center rounded-full px-4 text-base font-bold transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">Cancel</button>
          <button type="submit" className="inline-flex min-h-11 items-center justify-center rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">
            {isEditing ? 'Save Changes' : 'Save Event'}
          </button>
        </div>
      </form>
    </div>
  );
}
