// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React, { useState } from 'react';
import {Globe, RefreshCw, Trash2, Calendar} from 'lucide-react';
import { useCalendarSubscriptionsQuery, useCreateCalendarSubscriptionMutation, useDeleteCalendarSubscriptionMutation, useSyncCalendarSubscriptionMutation } from '../../../hooks/useCalendarQueries';
import { SettingsPanel } from '../../common/layout/SettingsPanel';
import { Input } from '../../common/ui/Input';
import { ReadOnlyBanner } from '../../common/ui/ReadOnlyBanner';

export function CalendarSubscriptionsPanel({ disabled }: { disabled?: boolean }) {
  const { data: subscriptions = [], isLoading } = useCalendarSubscriptionsQuery();
  
  const [subForm, setSubForm] = useState({
    name: '',
    url: '',
    isActive: true
  });

  const createSubMutation = useCreateCalendarSubscriptionMutation(() => {
    setSubForm({ name: '', url: '', isActive: true });
  });
  const deleteSubMutation = useDeleteCalendarSubscriptionMutation();
  const syncSubMutation = useSyncCalendarSubscriptionMutation();

  const handleCreateSub = (e: React.FormEvent) => {
    e.preventDefault();
    if (disabled) return;
    createSubMutation.mutate(subForm);
  };

  return (
      <SettingsPanel
          title="External Calendar Subscriptions"
          subtitle="Manage external .ics calendar integrations. These external calendars will be fetched, aggregated, and converted into intranet schedule blocks automatically."
          icon={<Calendar size={24} />}
      >
          <div className="flex flex-col gap-4">
        {disabled && (
          <ReadOnlyBanner message="You can review these subscriptions, but only administrators can add or remove them." />
        )}
          <form onSubmit={handleCreateSub} className="flex flex-col gap-4 rounded-xl bg-surface-container p-4">
            <h4 className="flex items-center gap-2 text-base font-black text-on-surface">
              <Globe size={18} className="text-on-surface-variant" aria-hidden="true" />
              Connect a calendar feed
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <Input
                label="Calendar name"
                type="text"
                placeholder="Swedish holidays"
                value={subForm.name}
                onChange={e => setSubForm({ ...subForm, name: e.target.value })}
                required
                disabled={disabled}
                className={"bg-surface border border-outline-variant"}
              />
              <Input
                label="iCal feed URL"
                type="url"
                placeholder="https://example.com/calendar.ics"
                value={subForm.url}
                onChange={e => setSubForm({ ...subForm, url: e.target.value })}
                required
                disabled={disabled}
                className={"bg-surface border border-outline-variant"}
              />
            </div>
            <div className="flex justify-end">
              <button 
                type="submit" 
                disabled={disabled || createSubMutation.isPending}
                className="inline-flex min-h-11 cursor-pointer items-center justify-center rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:hover:text-on-surface/[0.38]"
              >
                {createSubMutation.isPending ? 'Connecting…' : 'Add calendar'}
              </button>
            </div>
          </form>

        <div className="flex flex-col gap-2 mt-2">
          {isLoading ? (
            <div className="text-sm text-on-surface-variant italic text-center py-4">Loading subscriptions...</div>
          ) : subscriptions.length === 0 ? (
            <div className="text-sm text-on-surface-variant italic text-center py-4">No external calendar integrations active.</div>
          ) : (
            subscriptions.map((sub) => (
              <div key={sub.id} className="flex flex-wrap items-center justify-between gap-1 rounded-xl bg-surface-container p-4 transition-colors hover:bg-surface-container-high">
                <div className="flex min-w-0 flex-1 flex-col gap-1">
                  <span className="text-base font-bold text-on-surface">{sub.name}</span>
                  <span className="break-all text-sm text-on-surface-variant" title={sub.url || undefined}>{sub.url}</span>
                  {sub.lastSyncError && (
                    <span className="mt-1 text-sm font-bold leading-tight text-error">{sub.lastSyncError}</span>
                  )}
                </div>
                
                <div className="flex items-center gap-1">
                  <button 
                    type="button"
                    aria-label={`Sync ${sub.name || 'calendar'}`}
                    onClick={() => sub.id && syncSubMutation.mutate(sub.id)}
                    disabled={disabled || syncSubMutation.isPending}
                    className="flex h-10 w-10 cursor-pointer items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container-highest hover:text-on-surface focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:hover:text-on-surface/[0.38]"
                    title="Sync Now"
                  >
                    <RefreshCw size={18} className={syncSubMutation.isPending ? 'animate-spin' : ''} />
                  </button>
                  <button 
                    type="button"
                    aria-label={`Remove ${sub.name || 'calendar'}`}
                    onClick={() => sub.id && deleteSubMutation.mutate(sub.id)}
                    disabled={disabled || deleteSubMutation.isPending}
                    className="flex h-10 w-10 cursor-pointer items-center justify-center rounded-full text-error transition-colors hover:bg-error-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error disabled:cursor-not-allowed disabled:bg-on-surface/[0.1] disabled:text-on-surface/[0.38] disabled:hover:bg-on-surface/[0.1] disabled:hover:text-on-surface/[0.38]"
                    title="Remove Integration"
                  >
                    <Trash2 size={18} />
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </SettingsPanel>
  );
}
