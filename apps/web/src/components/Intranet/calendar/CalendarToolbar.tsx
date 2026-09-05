// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { CalendarDays, CalendarRange, ListTodo, Plus, Settings } from 'lucide-react';
import { Button } from '../../common/ui/Button';

export type CalendarViewMode = 'month' | 'week' | 'schedule';

interface CalendarToolbarProps {
  viewMode: CalendarViewMode;
  isWriter: boolean;
  onViewModeChange: (mode: CalendarViewMode) => void;
  onAddEvent: () => void;
  onOpenSettings: () => void;
}

const VIEW_OPTIONS = [
  { mode: 'month', label: 'Month view', title: 'Month View', Icon: CalendarDays },
  { mode: 'week', label: 'Week view', title: 'Week View', Icon: CalendarRange },
  { mode: 'schedule', label: 'Schedule view', title: 'Schedule list', Icon: ListTodo },
] as const;

export function CalendarToolbar({ viewMode, isWriter, onViewModeChange, onAddEvent, onOpenSettings }: CalendarToolbarProps) {
  return (
    <div className="flex items-center flex-wrap gap-2">
      <div className="flex h-11 items-center rounded-full border border-outline p-1" role="group" aria-label="Calendar view">
        {VIEW_OPTIONS.map(({ mode, label, title, Icon }) => (
          <button
            key={mode}
            onClick={() => onViewModeChange(mode)}
            aria-label={label}
            className={`flex h-full w-10 items-center justify-center rounded-full transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary ${viewMode === mode ? 'bg-secondary text-on-secondary' : 'text-on-surface-variant hover:bg-brand-active hover:text-brand-text'}`}
            title={title}
          >
            <Icon size={18} />
          </button>
        ))}
      </div>
      <Button
        onClick={onAddEvent}
        disabled={!isWriter}
        variant="tonal"
        color="secondary"
        icon={<Plus size={16} />}
        className="whitespace-nowrap"
        title={isWriter ? undefined : 'Add Event (requires Employee/Admin permissions)'}
      >
        Add Event
      </Button>
      <button
        onClick={onOpenSettings}
        className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
        aria-label="Calendar settings"
        title="Calendar Settings"
      >
        <Settings size={20} />
      </button>
    </div>
  );
}
