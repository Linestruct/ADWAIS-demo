// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Activity, X } from 'lucide-react';
import { FormField } from '../../common/ui/FormField';
import { UPTIME_MONITOR_TYPES } from '../../../utils/monitorTypeHelper';

interface CreateMonitorModalProps {
  isOpen: boolean;
  onClose: () => void;
  createMonitor: {
    mutate: (monitor: { name: string; url: string; type: string; uptimeSla: number | null }, options?: { onSuccess?: () => void }) => void;
    isPending: boolean;
  };
}

export function CreateMonitorModal({ isOpen, onClose, createMonitor }: CreateMonitorModalProps) {
  const [draft, setDraft] = useState<{name: string; url: string; type: string; uptimeSla: string}>({ name: '', url: '', type: UPTIME_MONITOR_TYPES[0] || '', uptimeSla: '' });

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMonitor.mutate(
      {
        ...draft,
        uptimeSla: draft.uptimeSla === '' ? null : parseFloat(draft.uptimeSla)
      }, 
      {
        onSuccess: () => {
          setDraft({ name: '', url: '', type: UPTIME_MONITOR_TYPES[0] || '', uptimeSla: '' });
          onClose();
        }
      }
    );
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4 backdrop-blur-sm animate-in fade-in"
      role="presentation"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}
    >
      <form
        onSubmit={handleSubmit}
        className="m3-elevation-4 flex w-full max-w-md flex-col overflow-hidden rounded-3xl border-0 bg-surface animate-in zoom-in-95"
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-monitor-title"
      >
        <div className="flex items-center justify-between bg-surface px-6 py-5">
          <h3 id="create-monitor-title" className="flex items-center gap-4 text-xl font-bold text-on-surface">
            <Activity size={20} className="text-on-surface-variant" aria-hidden="true" />
            Create monitor
          </h3>
          <button type="button" onClick={onClose} aria-label="Close dialog" className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">
            <X size={20} aria-hidden="true" />
          </button>
        </div>

        <div className="flex flex-col gap-4 bg-surface px-6 pb-6">
          <p className="text-sm font-medium text-on-surface-variant mb-2">
            Add a new external availability check.
          </p>

          <FormField
            id="monitor-name"
            label="Name"
            type="text"
            placeholder="Monitor Name"
            value={draft.name}
            onChange={e => setDraft({ ...draft, name: e.target.value })}
            required
          />

          <FormField
            id="monitor-url"
            label="URL"
            type="url"
            placeholder="https://example.com"
            value={draft.url}
            onChange={e => setDraft({ ...draft, url: e.target.value })}
            required
          />

          <FormField
            as="select"
            id="monitor-type"
            label="Monitor Type"
            value={draft.type}
            onChange={e => setDraft({ ...draft, type: e.target.value })}
          >
            {UPTIME_MONITOR_TYPES.map(type => <option key={type} value={type}>{type}</option>)}
          </FormField>

          <FormField
            id="monitor-sla"
            label="Uptime SLA (%) (optional)"
            type="number"
            step="0.1"
            placeholder="e.g. 99.9"
            value={draft.uptimeSla}
            onChange={e => setDraft({ ...draft, uptimeSla: e.target.value })}
          />
        </div>

        <div className="flex justify-end gap-3 bg-surface px-6 py-4">
          <button type="button" onClick={onClose} disabled={createMonitor.isPending} className="inline-flex min-h-11 items-center justify-center rounded-full px-4 text-base font-bold transition-colors hover:bg-surface-container-high focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary disabled:cursor-not-allowed disabled:text-on-surface/[0.38] disabled:hover:bg-transparent">
            Cancel
          </button>
          <button type="submit" disabled={!draft.name || !draft.url || createMonitor.isPending} className="inline-flex min-h-11 items-center justify-center rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:hover:text-on-surface/[0.38]">
            {createMonitor.isPending ? 'Creating...' : 'Create monitor'}
          </button>
        </div>
      </form>
    </div>
  );
}
