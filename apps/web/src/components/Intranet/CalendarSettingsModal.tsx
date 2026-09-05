// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { X, Settings, Link, RefreshCw, ShieldAlert } from 'lucide-react';
import { FormField } from '../common/ui/FormField';
import { Button } from '../common/ui/Button';

interface CalendarSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  isAdmin: boolean;
  isWriter: boolean;
  token?: string;
  isTokenRequested: boolean;
  onGenerateToken: () => void;
  onCopyFeedLink: () => void;
  onRegenerateToken: () => void;
}

export function CalendarSettingsModal({
  isOpen,
  onClose,
  isWriter,
  token,
  isTokenRequested,
  onGenerateToken,
  onCopyFeedLink,
  onRegenerateToken,
}: CalendarSettingsModalProps) {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-sm animate-in fade-in"
      role="presentation"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}
    >
      <div className="bg-surface rounded-3xl m3-elevation-4 border-0 w-full max-w-lg overflow-hidden flex flex-col animate-in zoom-in-95 max-h-[90vh]">
        <div className="flex items-center justify-between bg-surface px-6 py-5">
          <h3 className="flex items-center gap-4 text-xl font-bold text-on-surface">
            <Settings size={20} className="text-on-surface-variant animate-spin-slow" /> Calendar Settings
          </h3>
          <button onClick={onClose} aria-label="Close calendar settings" className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">
            <X size={20} />
          </button>
        </div>
        
        <div className="flex flex-1 flex-col gap-6 overflow-y-auto bg-surface px-6 pb-6 custom-scrollbar">
          {/* Personal ICS Subscription Feed */}
          <div className="flex flex-col gap-6">
            <h4 className="flex items-center gap-4 pl-1 text-base font-bold text-on-surface">
              <Link size={18} className="text-on-surface-variant" /> Subscribe to feed in Outlook / Google
            </h4>
            <p className="text-base leading-relaxed text-on-surface-variant">
              Subscribe to this calendar inside external clients using your personal read-only feed. This syncs all company calendar events automatically. Keep this token private.
            </p>
            <p className="text-base leading-relaxed text-on-surface-variant">
              To add a feed to the shared ADWAIS calendar, go to Settings {">"} Configuration
            </p>
            {isWriter ? (
              token ? (
                <div className="flex gap-4 animate-in fade-in duration-200">
                  <FormField
                    label="Calendar feed URL"
                    hideLabel
                    readOnly
                    value={`${window.location.origin}/api/intranet/calendar/feed.ics?token=${token}`}
                    containerClassName="min-w-0 flex-1"
                    className="text-sm text-on-surface-variant"
                  />
                  <Button onClick={onCopyFeedLink} variant="tonal" color="secondary" className="whitespace-nowrap !text-base">
                    Copy Link
                  </Button>
                  <button 
                    onClick={onRegenerateToken}
                    className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
                    aria-label="Regenerate calendar feed token"
                    title="Regenerate Token"
                  >
                    <RefreshCw size={16} />
                  </button>
                </div>
              ) : isTokenRequested ? (
                <div className="text-sm text-on-surface-variant italic flex items-center gap-2">
                  <RefreshCw size={12} className="animate-spin" /> Generating feed token...
                </div>
              ) : (
                <button
                  onClick={onGenerateToken}
                  className="mt-2 inline-flex min-h-11 items-center justify-center rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
                >
                  Generate Feed Link
                </button>
              )
            ) : (
              <div className="flex flex-col gap-8 mt-2">
                <Button disabled variant="tonal" color="surface" className="w-full !text-base">
                  Generate Feed Link (Access Restricted)
                </Button>
                <div className="flex gap-6 p-4 rounded-2xl border border-red-200 bg-red-50 text-red-800 text-sm leading-normal font-medium">
                  <ShieldAlert size={20} className="text-red-650 shrink-0 mt-0.5" />
                  <div>
                    <span className="font-bold">Access Denied:</span> You are not a configured user with a registered intranet identity or required role (Employee/Admin). Feed subscription generation is disabled.
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
