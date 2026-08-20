// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { X, Clock, MapPin, User as UserIcon, Globe, Repeat2 } from 'lucide-react';
import type { CalendarEventDto } from '@types';
import { getEventRangeLabel } from './calendar/calendarPresentation';
import { Button } from '../common/ui/Button';

interface CalendarEventDetailModalProps {
  event: CalendarEventDto;
  isOpen: boolean;
  onClose: () => void;
  isWriter: boolean;
  onDelete: (id: string) => void;
  onEdit: (event: CalendarEventDto) => void;
}

const BADGE_STYLES: Record<string, string> = {
  Meeting: 'bg-blue-100 text-blue-800 border-0',
  Fika: 'bg-orange-100 text-orange-800 border-0',
  Social: 'bg-purple-100 text-purple-800 border-0',
  Birthday: 'bg-pink-100 text-pink-800 border-0',
  GoLive: 'bg-emerald-100 text-emerald-800 border-0',
  ExternalSync: 'bg-cyan-100 text-cyan-800 border-0',
};

const DEFAULT_BADGE = 'bg-surface-container text-on-surface border-0';

export function CalendarEventDetailModal({
  event,
  isOpen,
  onClose,
  isWriter,
  onDelete,
  onEdit
}: CalendarEventDetailModalProps) {
  if (!isOpen) return null;

  const badgeClass = BADGE_STYLES[event.eventType || ''] ?? DEFAULT_BADGE;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-sm animate-in fade-in"
      role="presentation"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}
    >
      <div className="bg-surface rounded-3xl m3-elevation-4 border-0 w-full max-w-md overflow-hidden flex flex-col animate-in zoom-in-95">
        <div className="flex justify-between items-center bg-surface px-6 py-5">
          <span className={`text-xs font-bold uppercase tracking-wider px-3 py-1.5 rounded-full ${badgeClass}`}>
            {event.eventType || 'Event'}
          </span>
          <button 
            onClick={onClose}
            className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
            aria-label="Close event details"
          >
            <X size={20} />
          </button>
        </div>
        
        <div className="flex flex-col gap-6 bg-surface px-6 pb-6">
          <div>
            <h3 className="text-xl font-bold leading-snug text-on-surface">{event.title}</h3>
            {event.description && (
              <p className="mt-3 whitespace-pre-wrap text-base leading-relaxed text-on-surface-variant">{event.description}</p>
            )}
          </div>
          
          <div className="flex flex-col gap-4 text-on-surface-variant text-base border-t border-outline-variant pt-4">
            <div className="flex items-center gap-4">
              <Clock size={16} className="text-on-surface-variant shrink-0" />
              <span className="font-bold text-on-surface">{getEventRangeLabel(event)}</span>
            </div>

            {event.location && (
              <div className="flex items-center gap-4">
                <MapPin size={16} className="text-on-surface-variant shrink-0" />
                <span className="font-medium text-on-surface-variant">{event.location}</span>
              </div>
            )}

            {event.isRecurring && (
              <div className="flex items-center gap-4">
                <Repeat2 size={16} className="shrink-0 text-on-surface-variant" />
                <span className="font-medium text-on-surface-variant">
                  {event.recurrence && event.recurrence !== 'None' ? `Repeats ${event.recurrence.toLowerCase()}` : 'Recurring event'}
                </span>
              </div>
            )}

            {event.userName && (
              <div className="flex items-center gap-4">
                <UserIcon size={16} className="text-on-surface-variant shrink-0" />
                <span>Created by <span className="font-bold text-on-surface">{event.userName}</span></span>
              </div>
            )}

            {event.externalUid && (
              <div className="flex items-center gap-4">
                <Globe size={16} className="text-on-surface-variant shrink-0" />
                <span className="text-emerald-600 font-bold bg-emerald-50 px-1.5 py-0.5 rounded border border-emerald-100">
                  Synced Event
                </span>
              </div>
            )}
          </div>

          {isWriter && !event.externalUid && (
            <div className="flex justify-end gap-4 pt-2">
              <Button
                onClick={() => event.id && onDelete(event.id)}
                variant="text"
                color="error"
                className="!px-4 !text-base"
              >
                Delete
              </Button>
              <Button onClick={() => onEdit(event)} variant="tonal" color="secondary" className="!text-base">
                Edit Event
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
