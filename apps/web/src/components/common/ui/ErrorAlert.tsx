// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { AlertCircle, X } from 'lucide-react';

interface ErrorAlertProps {
  message: string;
  title?: string;
  onDismiss?: () => void;
}

export function ErrorAlert({ message, title = 'Something went wrong', onDismiss }: ErrorAlertProps) {
  return (
    <div
      role="alert"
      aria-live="assertive"
      className="flex items-start gap-4 text-sm text-on-error-container bg-error-container rounded-xl p-4 animate-in slide-in-from-top-1 duration-200"
    >
      <AlertCircle className="text-error h-4 w-4 shrink-0 mt-0.5" aria-hidden="true" />
      <div className="min-w-0 flex-1 select-text">
        <p className="font-bold">{title}</p>
        <p className="mt-0.5 font-medium leading-relaxed break-words whitespace-pre-wrap">{message}</p>
      </div>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          aria-label="Dismiss error"
          className="shrink-0 rounded-md p-1 text-error hover:bg-error-container hover:text-on-error-container focus:outline-none focus:ring-2 focus:ring-error/30 cursor-pointer"
        >
          <X className="h-4 w-4" aria-hidden="true" />
        </button>
      )}
    </div>
  );
}
