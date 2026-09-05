// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useMemo, useRef, useState, type KeyboardEvent as ReactKeyboardEvent, type PointerEvent as ReactPointerEvent, type ReactNode } from 'react';
import { CalendarDays, ChevronLeft, ChevronRight, Clock, Keyboard, Pencil } from 'lucide-react';

interface PickerDialogProps {
  open: boolean;
  onClose: () => void;
}

type FieldSize = 'compact' | 'standard';
type DateView = 'calendar' | 'month' | 'year' | 'input';
type TimeStage = 'hour' | 'minute';

const pad = (value: number) => String(value).padStart(2, '0');
const actionClass = 'min-h-12 rounded-full px-4 font-bold text-secondary transition-colors hover:bg-secondary-container hover:text-on-secondary-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary';

function useEscapeToClose(open: boolean, onClose: () => void) {
  useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [open, onClose]);
}

function PickerDialog({ open, onClose, titleId, children, compact = false }: PickerDialogProps & {
  titleId: string;
  children: ReactNode;
  compact?: boolean;
}) {
  useEscapeToClose(open, onClose);
  if (!open) return null;
  return (
    <div
      className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-900/50 p-4 backdrop-blur-sm animate-in fade-in"
      role="presentation"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}
    >
      <section
        className={`m3-elevation-4 w-full overflow-hidden rounded-[28px] border border-outline-variant bg-surface text-on-surface animate-in zoom-in-95 ${compact ? 'max-w-[328px]' : 'max-w-[360px]'}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        {children}
      </section>
    </div>
  );
}

function parseDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return year && month && day ? new Date(year, month - 1, day) : new Date();
}

function formatIsoDate(date: Date) {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function changeDateMonth(date: Date, month: number) {
  const maxDay = new Date(date.getFullYear(), month + 1, 0).getDate();
  return new Date(date.getFullYear(), month, Math.min(date.getDate(), maxDay));
}

function changeDateYear(date: Date, year: number) {
  const maxDay = new Date(year, date.getMonth() + 1, 0).getDate();
  return new Date(year, date.getMonth(), Math.min(date.getDate(), maxDay));
}

export function Md3DatePickerDialog({ open, value, onClose, onConfirm }: PickerDialogProps & {
  value: string;
  onConfirm: (value: string) => void;
}) {
  const [selected, setSelected] = useState(() => parseDate(value));
  const [visibleMonth, setVisibleMonth] = useState(() => {
    const date = parseDate(value);
    return new Date(date.getFullYear(), date.getMonth(), 1);
  });
  const [view, setView] = useState<DateView>('calendar');
  const [dateInput, setDateInput] = useState(() => formatIsoDate(parseDate(value)));
  const selectedYearRef = useRef<HTMLButtonElement>(null);
  const updateSelected = (next: Date) => {
    setSelected(next);
    setDateInput(formatIsoDate(next));
  };

  useEffect(() => {
    if (view === 'year') selectedYearRef.current?.scrollIntoView({ block: 'center' });
  }, [view]);

  const days = useMemo(() => {
    const year = visibleMonth.getFullYear();
    const month = visibleMonth.getMonth();
    const mondayOffset = (new Date(year, month, 1).getDay() + 6) % 7;
    const count = new Date(year, month + 1, 0).getDate();
    return [...Array.from({ length: mondayOffset }, () => null), ...Array.from({ length: count }, (_, index) => index + 1)];
  }, [visibleMonth]);
  const yearOptions = useMemo(() => Array.from({ length: 101 }, (_, index) => visibleMonth.getFullYear() - 50 + index), [visibleMonth]);

  const selectMonth = (month: number) => {
    const next = changeDateMonth(changeDateYear(selected, visibleMonth.getFullYear()), month);
    updateSelected(next);
    setVisibleMonth(new Date(next.getFullYear(), next.getMonth(), 1));
    setView('calendar');
  };
  const selectYear = (year: number) => {
    const next = changeDateYear(changeDateMonth(selected, visibleMonth.getMonth()), year);
    updateSelected(next);
    setVisibleMonth(new Date(next.getFullYear(), next.getMonth(), 1));
    setView('calendar');
  };
  const commitDateInput = (nextValue: string) => {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(nextValue)) return;
    const next = parseDate(nextValue);
    if (formatIsoDate(next) !== nextValue) return;
    updateSelected(next);
    setVisibleMonth(new Date(next.getFullYear(), next.getMonth(), 1));
  };

  return (
    <PickerDialog open={open} onClose={onClose} titleId="md3-date-picker-title">
      <header className="border-b border-outline-variant bg-secondary-container px-6 pb-4 pt-5 text-on-secondary-container">
        <p className="text-sm font-bold tracking-wide">Select date</p>
        <div className="mt-2 flex items-center justify-between gap-3">
          <h3 id="md3-date-picker-title" className="min-w-0 truncate text-3xl font-medium tracking-tight">
            {selected.toLocaleDateString('en-SE', { weekday: 'short', month: 'short', day: 'numeric' })}
          </h3>
          <button type="button" onClick={() => setView(current => current === 'input' ? 'calendar' : 'input')} className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full hover:bg-secondary/10" aria-label={view === 'input' ? 'Show calendar' : 'Enter date'}>
            {view === 'input' ? <CalendarDays size={20} /> : <Pencil size={20} />}
          </button>
        </div>
      </header>

      <div className={`bg-surface px-3 ${view === 'input' ? 'py-6' : 'min-h-[336px] pt-2'}`}>
        {view === 'input' ? (
          <div className="flex items-start px-3 py-2">
            <label className="relative w-full rounded-xl border-2 border-outline bg-surface px-4 pb-2 pt-5 focus-within:border-secondary">
              <span className="absolute left-3 top-0 -translate-y-1/2 bg-surface px-1 text-xs font-bold text-on-surface-variant">Date</span>
              <input
                type="text"
                inputMode="numeric"
                autoComplete="off"
                placeholder="YYYY-MM-DD"
                value={dateInput}
                onChange={event => {
                  setDateInput(event.target.value);
                  commitDateInput(event.target.value);
                }}
                onBlur={() => {
                  commitDateInput(dateInput);
                  if (formatIsoDate(parseDate(dateInput)) !== dateInput) setDateInput(formatIsoDate(selected));
                }}
                className="w-full bg-transparent text-base font-medium tabular-nums text-on-surface outline-none placeholder:text-on-surface-variant"
                aria-label="Date in year-month-day format"
              />
            </label>
          </div>
        ) : view === 'month' ? (
          <div className="grid grid-cols-3 gap-2 p-3" aria-label="Select month">
            {Array.from({ length: 12 }, (_, month) => (
              <button key={month} type="button" onClick={() => selectMonth(month)} className={`min-h-12 rounded-full px-2 text-sm font-bold ${visibleMonth.getMonth() === month ? 'bg-secondary text-on-secondary' : 'text-on-surface hover:bg-secondary-container'}`}>
                {new Date(2000, month, 1).toLocaleDateString('en-SE', { month: 'short' })}
              </button>
            ))}
          </div>
        ) : view === 'year' ? (
          <div className="grid max-h-[320px] grid-cols-3 gap-2 overflow-y-auto p-3 custom-scrollbar" aria-label="Select year">
            {yearOptions.map(year => (
              <button ref={visibleMonth.getFullYear() === year ? selectedYearRef : undefined} key={year} type="button" onClick={() => selectYear(year)} className={`min-h-12 rounded-full text-sm font-bold ${visibleMonth.getFullYear() === year ? 'bg-secondary text-on-secondary' : 'text-on-surface hover:bg-secondary-container'}`}>
                {year}
              </button>
            ))}
          </div>
        ) : (
          <>
            <div className="grid min-h-14 grid-cols-[1fr_auto] items-center border-b border-outline-variant px-1">
              <div className="flex min-w-0 items-center">
                <button type="button" onClick={() => setView('month')} className="min-h-12 truncate rounded-full px-3 text-sm font-extrabold text-on-surface hover:bg-secondary-container" aria-label="Choose month">
                  {visibleMonth.toLocaleDateString('en-SE', { month: 'long' })}
                </button>
                <button type="button" onClick={() => setView('year')} className="min-h-12 rounded-full px-3 text-sm font-extrabold text-on-surface hover:bg-secondary-container" aria-label="Choose year">
                  {visibleMonth.getFullYear()}
                </button>
              </div>
              <div className="flex">
                <button type="button" onClick={() => setVisibleMonth(month => new Date(month.getFullYear(), month.getMonth() - 1, 1))} className="flex h-12 w-12 items-center justify-center rounded-full text-on-surface hover:bg-secondary-container" aria-label="Previous month"><ChevronLeft size={20} /></button>
                <button type="button" onClick={() => setVisibleMonth(month => new Date(month.getFullYear(), month.getMonth() + 1, 1))} className="flex h-12 w-12 items-center justify-center rounded-full text-on-surface hover:bg-secondary-container" aria-label="Next month"><ChevronRight size={20} /></button>
              </div>
            </div>
            <div className="grid grid-cols-7 text-center text-xs font-bold text-on-surface-variant" aria-hidden="true">
              {['M', 'T', 'W', 'T', 'F', 'S', 'S'].map((day, index) => <span key={`${day}-${index}`} className="py-2">{day}</span>)}
            </div>
            <div className="grid grid-cols-7" role="grid" aria-label="Calendar days">
              {days.map((day, index) => {
                if (day === null) return <span key={`blank-${index}`} />;
                const date = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth(), day);
                const isSelected = formatIsoDate(date) === formatIsoDate(selected);
                const isToday = formatIsoDate(date) === formatIsoDate(new Date());
                return (
                  <button key={day} type="button" aria-selected={isSelected} onClick={() => updateSelected(date)} className={`mx-auto flex h-11 w-11 items-center justify-center rounded-full text-sm font-bold transition-colors ${isSelected ? 'bg-secondary text-on-secondary' : isToday ? 'border border-secondary text-on-surface' : 'text-on-surface hover:bg-secondary-container'}`}>
                    {day}
                  </button>
                );
              })}
            </div>
          </>
        )}
      </div>
      <footer className="flex justify-end gap-2 border-t border-outline-variant bg-surface px-4 py-2">
        <button type="button" onClick={onClose} className={actionClass}>Cancel</button>
        <button type="button" onClick={() => { onConfirm(formatIsoDate(selected)); onClose(); }} className={actionClass}>OK</button>
      </footer>
    </PickerDialog>
  );
}

function parseTime(value: string) {
  const [hour, minute] = value.split(':').map(Number);
  return {
    hour: Number.isInteger(hour) && hour >= 0 && hour <= 23 ? hour : 9,
    minute: Number.isInteger(minute) && minute >= 0 && minute <= 59 ? minute : 0,
  };
}

function DialLabel({ value, ringIndex, radius, selected }: { value: number; ringIndex: number; radius: number; selected: boolean }) {
  const angle = (ringIndex / 12) * Math.PI * 2;
  return (
    <span
      className={`pointer-events-none absolute flex h-9 w-9 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full text-xs font-extrabold ${selected ? 'text-on-secondary' : 'text-on-surface'}`}
      style={{ left: `${50 + Math.sin(angle) * radius}%`, top: `${50 - Math.cos(angle) * radius}%` }}
    >
      {pad(value)}
    </span>
  );
}

export function Md3TimePickerDialog({ open, value, onClose, onConfirm }: PickerDialogProps & {
  value: string;
  onConfirm: (value: string) => void;
}) {
  const initial = parseTime(value);
  const [hour, setHour] = useState(initial.hour);
  const [minute, setMinute] = useState(initial.minute);
  const [stage, setStage] = useState<TimeStage>('hour');
  const [inputMode, setInputMode] = useState(false);
  const dragging = useRef(false);
  const dialRef = useRef<HTMLDivElement>(null);

  const setFromPointer = (event: ReactPointerEvent<HTMLDivElement>) => {
    const dial = dialRef.current;
    if (!dial) return;
    const rect = dial.getBoundingClientRect();
    const dx = event.clientX - (rect.left + rect.width / 2);
    const dy = event.clientY - (rect.top + rect.height / 2);
    const angle = (Math.atan2(dx, -dy) + Math.PI * 2) % (Math.PI * 2);
    if (stage === 'minute') {
      setMinute(Math.round(angle / (Math.PI * 2) * 60) % 60);
      return;
    }
    const index = Math.round(angle / (Math.PI * 2) * 12) % 12;
    const innerRing = Math.hypot(dx, dy) < rect.width * 0.36;
    setHour(innerRing ? (index === 0 ? 0 : index + 12) : (index === 0 ? 12 : index));
  };

  const handlePointerDown = (event: ReactPointerEvent<HTMLDivElement>) => {
    dragging.current = true;
    event.currentTarget.setPointerCapture(event.pointerId);
    setFromPointer(event);
  };
  const handlePointerMove = (event: ReactPointerEvent<HTMLDivElement>) => {
    if (dragging.current) setFromPointer(event);
  };
  const handlePointerUp = (event: ReactPointerEvent<HTMLDivElement>) => {
    if (!dragging.current) return;
    setFromPointer(event);
    dragging.current = false;
    event.currentTarget.releasePointerCapture(event.pointerId);
    if (stage === 'hour') setStage('minute');
  };
  const handleDialKeyDown = (event: ReactKeyboardEvent<HTMLDivElement>) => {
    const direction = event.key === 'ArrowUp' || event.key === 'ArrowRight' ? 1 : event.key === 'ArrowDown' || event.key === 'ArrowLeft' ? -1 : 0;
    if (!direction) return;
    event.preventDefault();
    if (stage === 'hour') setHour(current => (current + direction + 24) % 24);
    else setMinute(current => (current + direction + 60) % 60);
  };

  const selectedAngle = stage === 'hour' ? (hour % 12) / 12 * 360 : minute / 60 * 360;
  const selectedRadius = stage === 'hour' && (hour === 0 || hour >= 13) ? 29 : 42;
  const selectedRadians = selectedAngle / 180 * Math.PI;

  return (
    <PickerDialog open={open} onClose={onClose} titleId="md3-time-picker-title" compact={inputMode}>
      <div className="bg-surface px-6 pb-4 pt-5">
        <p className="text-sm font-bold text-on-surface-variant">Enter time</p>
        <h3 id="md3-time-picker-title" className="sr-only">Select time</h3>
        <div className="mt-4 flex items-center justify-center gap-2">
          <input data-md3-ripple type="number" min="0" max="23" value={pad(hour)} onFocus={() => setStage('hour')} onChange={event => setHour(Math.max(0, Math.min(23, Number(event.target.value))))} onKeyDown={event => { if (event.key === 'Enter') event.preventDefault(); }} className={`w-[88px] rounded-xl border-0 px-3 py-3 text-center text-4xl font-medium tabular-nums text-on-surface outline-none focus:ring-2 focus:ring-secondary ${stage === 'hour' ? 'bg-secondary-container' : 'bg-surface-container-high'}`} aria-label="Hour" />
          <span className="text-4xl text-on-surface">:</span>
          <input data-md3-ripple type="number" min="0" max="59" value={pad(minute)} onFocus={() => setStage('minute')} onChange={event => setMinute(Math.max(0, Math.min(59, Number(event.target.value))))} onKeyDown={event => { if (event.key === 'Enter') event.preventDefault(); }} className={`w-[88px] rounded-xl border-0 px-3 py-3 text-center text-4xl font-medium tabular-nums text-on-surface outline-none focus:ring-2 focus:ring-secondary ${stage === 'minute' ? 'bg-secondary-container' : 'bg-surface-container-high'}`} aria-label="Minute" />
        </div>
        {!inputMode && (
          <div
            ref={dialRef}
            className="relative mx-auto mt-6 aspect-square w-full max-w-[280px] touch-none rounded-full bg-surface-container-high outline-none focus-visible:ring-2 focus-visible:ring-secondary"
            role="slider"
            tabIndex={0}
            aria-label={stage === 'hour' ? 'Hour dial' : 'Minute dial'}
            aria-valuemin={0}
            aria-valuemax={stage === 'hour' ? 23 : 59}
            aria-valuenow={stage === 'hour' ? hour : minute}
            onKeyDown={handleDialKeyDown}
            onPointerDown={handlePointerDown}
            onPointerMove={handlePointerMove}
            onPointerUp={handlePointerUp}
            onPointerCancel={() => { dragging.current = false; }}
          >
            <span className="pointer-events-none absolute left-1/2 top-1/2 h-2 w-2 -translate-x-1/2 -translate-y-1/2 rounded-full bg-secondary" />
            <span className="pointer-events-none absolute left-1/2 top-1/2 h-0.5 origin-left bg-secondary" style={{ width: `${selectedRadius}%`, transform: `rotate(${selectedAngle - 90}deg)` }} />
            <span className="pointer-events-none absolute flex h-10 w-10 -translate-x-1/2 -translate-y-1/2 rounded-full bg-secondary" style={{ left: `${50 + Math.sin(selectedRadians) * selectedRadius}%`, top: `${50 - Math.cos(selectedRadians) * selectedRadius}%` }} />
            {stage === 'minute'
              ? Array.from({ length: 12 }, (_, index) => <DialLabel key={index} value={index * 5} ringIndex={index} radius={42} selected={minute === index * 5} />)
              : <>
                  {Array.from({ length: 12 }, (_, index) => <DialLabel key={`outer-${index}`} value={index === 0 ? 12 : index} ringIndex={index} radius={42} selected={hour === (index === 0 ? 12 : index)} />)}
                  {Array.from({ length: 12 }, (_, index) => <DialLabel key={`inner-${index}`} value={index === 0 ? 0 : index + 12} ringIndex={index} radius={29} selected={hour === (index === 0 ? 0 : index + 12)} />)}
                </>}
          </div>
        )}
      </div>
      <footer className="flex items-center justify-between gap-2 border-t border-outline-variant bg-surface px-3 py-2">
        <button
          type="button"
          onClick={() => setInputMode(current => !current)}
          className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-on-surface-variant hover:bg-secondary-container hover:text-on-secondary-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary"
          aria-label={inputMode ? 'Show clock dial' : 'Enter time with keyboard'}
        >
          {inputMode ? <Clock size={20} /> : <Keyboard size={20} />}
        </button>
        <div className="flex gap-2">
          <button type="button" onClick={onClose} className={actionClass}>Cancel</button>
          <button type="button" onClick={() => { onConfirm(`${pad(hour)}:${pad(minute)}`); onClose(); }} className={actionClass}>OK</button>
        </div>
      </footer>
    </PickerDialog>
  );
}

const fieldSizeClasses: Record<FieldSize, string> = {
  compact: 'min-h-11 px-3 text-sm',
  standard: 'min-h-14 px-4 text-base',
};

export function DatePickerField({ id, value, label, onChange, size = 'compact', disabled = false }: {
  id: string;
  value: string;
  label: string;
  onChange: (value: string) => void;
  size?: FieldSize;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const display = value ? parseDate(value).toLocaleDateString('en-SE', { year: 'numeric', month: 'short', day: 'numeric' }) : 'Choose date';
  return (
    <div className="flex min-w-0 flex-col gap-2">
      <span id={`${id}-label`} className="pl-1 text-sm font-bold text-on-surface-variant">{label}</span>
      <button type="button" onClick={() => setOpen(true)} disabled={disabled} className={`group flex w-full min-w-0 items-center gap-3 rounded-xl border border-outline bg-surface text-left font-medium text-on-surface transition-colors hover:border-secondary hover:bg-secondary-container disabled:cursor-not-allowed disabled:border-transparent disabled:bg-on-surface/[0.12] disabled:text-on-surface/[0.38] ${fieldSizeClasses[size]}`} aria-labelledby={`${id}-label`}>
        <CalendarDays size={18} className="shrink-0 text-on-surface-variant group-disabled:text-on-surface/[0.38]" aria-hidden="true" />
        <span className="truncate">{display}</span>
      </button>
      {open && <Md3DatePickerDialog open value={value} onClose={() => setOpen(false)} onConfirm={onChange} />}
    </div>
  );
}

export function TimePickerField({ id, value, label, onChange, size = 'compact', disabled = false }: {
  id: string;
  value: string;
  label: string;
  onChange: (value: string) => void;
  size?: FieldSize;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  return (
    <div className="flex min-w-0 flex-col gap-2">
      <span id={`${id}-label`} className="pl-1 text-sm font-bold text-on-surface-variant">{label}</span>
      <button type="button" onClick={() => setOpen(true)} disabled={disabled} className={`group flex w-full min-w-0 items-center gap-3 rounded-xl border border-outline bg-surface text-left font-bold tabular-nums text-on-surface transition-colors hover:border-secondary hover:bg-secondary-container disabled:cursor-not-allowed disabled:border-transparent disabled:bg-on-surface/[0.12] disabled:text-on-surface/[0.38] ${fieldSizeClasses[size]}`} aria-labelledby={`${id}-label`}>
        <Clock size={18} className="shrink-0 text-on-surface-variant group-disabled:text-on-surface/[0.38]" aria-hidden="true" />
        <span>{value || '--:--'}</span>
      </button>
      {open && <Md3TimePickerDialog open value={value} onClose={() => setOpen(false)} onConfirm={onChange} />}
    </div>
  );
}

export function DateTimePickerField({ id, value, label, onChange, size = 'compact', disabled = false }: {
  id: string;
  value: string;
  label: string;
  onChange: (value: string) => void;
  size?: FieldSize;
  disabled?: boolean;
}) {
  const [date = '', time = ''] = value.split('T');
  return (
    <fieldset className="grid min-w-0 grid-cols-[minmax(0,1fr)_auto] gap-2">
      <legend className="sr-only">{label}</legend>
      <DatePickerField id={`${id}-date`} value={date} label={`${label} date`} size={size} disabled={disabled} onChange={nextDate => onChange(`${nextDate}T${time || '00:00'}`)} />
      <TimePickerField id={`${id}-time`} value={time} label={`${label} time`} size={size} disabled={disabled} onChange={nextTime => onChange(`${date || formatIsoDate(new Date())}T${nextTime}`)} />
    </fieldset>
  );
}
