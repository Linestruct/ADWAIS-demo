// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { AlertCircle, X } from 'lucide-react';

type ErrorToastProps = {
  title: string;
  description: string;
  onClose: () => void;
};

export function ErrorToast({ title, description, onClose }: ErrorToastProps) {
  return (
    <div
      className="relative flex w-[356px] max-w-[calc(100vw-2rem)] items-start gap-3 rounded-xl bg-error-container p-4 text-sm text-on-error-container m3-elevation-2"
      onClick={(event) => event.stopPropagation()}
    >
      <AlertCircle className="mt-0.5 shrink-0 text-error" size={20} aria-hidden="true" />
      <div className="min-w-0 flex-1 pr-5">
        <div className="font-bold">{title}</div>
        <div className="mt-1 break-words font-medium leading-relaxed [overflow-wrap:anywhere]">
          {description}
        </div>
      </div>
      <button
        type="button"
        onClick={onClose}
        aria-label="Close notification"
        style={{ position: 'absolute', top: '0.75rem', right: '0.75rem' }}
        className="absolute right-3 top-3 flex h-7 w-7 items-center justify-center rounded-md p-1 opacity-60 transition-opacity hover:bg-surface/5 hover:opacity-100"
      >
        <X size={18} strokeWidth={1.75} aria-hidden="true" />
      </button>
    </div>
  );
}
