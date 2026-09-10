// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { CheckCircle2, X } from 'lucide-react';

type SuccessToastProps = {
  title: string;
  onClose: () => void;
};

export function SuccessToast({ title, onClose }: SuccessToastProps) {
  return (
    <div
      className="relative flex w-[356px] max-w-[calc(100vw-2rem)] items-start gap-3 rounded-xl bg-primary-container p-4 text-sm text-on-success-container m3-elevation-2"
      onClick={(event) => event.stopPropagation()}
    >
      <CheckCircle2 className="mt-0.5 shrink-0 text-success" size={20} aria-hidden="true" />
      <div className="min-w-0 flex-1 pr-5 font-bold">{title}</div>
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
