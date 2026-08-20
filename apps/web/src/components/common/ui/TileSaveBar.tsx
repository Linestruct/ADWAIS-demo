// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Loader2, CheckCircle2 } from 'lucide-react';
import { ErrorAlert } from './ErrorAlert';

interface TileSaveBarProps {
  isDirty: boolean;
  isPending: boolean;
  isSuccess?: boolean;
  isError?: boolean;
  errorMsg?: string | null;
  onSave: () => void;
  onCancel: () => void;
}

export function TileSaveBar({
  isDirty,
  isPending,
  isSuccess,
  isError,
  errorMsg,
  onSave,
  onCancel
}: TileSaveBarProps) {
  const [dismissedErrorMessage, setDismissedErrorMessage] = useState<string | null>(null);
  const resolvedErrorMessage = errorMsg || 'Failed to save updates.';
  const isErrorDismissed = isError && dismissedErrorMessage === resolvedErrorMessage;

  if (!isDirty && !isPending && !isSuccess && (!isError || isErrorDismissed)) return null;

  const handleSave = () => {
    setDismissedErrorMessage(null);
    onSave();
  };

  return (
    <div className="mt-2 flex flex-col gap-3 border-t border-outline-variant pt-3 animate-in fade-in duration-200">
      {isPending && (
        <div className="flex items-center gap-3 rounded-xl bg-surface px-3 py-2 text-sm font-semibold text-on-surface-variant animate-pulse">
          <Loader2 className="animate-spin text-brand-link h-3.5 w-3.5" />
          <span>Saving updates to remote fleet (may take a long time if being rate limited)...</span>
        </div>
      )}

      {isSuccess && !isDirty && (
        <div className="flex items-center gap-3 rounded-xl bg-surface px-3 py-2 text-sm font-bold text-growth animate-in slide-in-from-top-1 duration-200">
          <CheckCircle2 className="text-growth h-3.5 w-3.5" />
          <span>Updates saved successfully!</span>
        </div>
      )}

      {isError && !isErrorDismissed && (
        <ErrorAlert
          title="Unable to save changes"
          message={resolvedErrorMessage}
          onDismiss={() => setDismissedErrorMessage(resolvedErrorMessage)}
        />
      )}

      {isDirty && !isPending && (
        <div className="flex w-full flex-wrap-reverse items-center justify-end gap-2">
          <button
            onClick={handleSave}
            disabled={isPending}
            className="inline-flex min-h-10 cursor-pointer items-center justify-center rounded-full bg-on-primary-container px-5 text-sm font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:opacity-50"
          >
            Save Changes
          </button>
          <button
            onClick={onCancel}
            disabled={isPending}
            className="inline-flex min-h-10 cursor-pointer items-center justify-center rounded-full px-4 text-sm font-bold text-on-surface transition-colors hover:bg-surface-container-high focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:opacity-50"
          >
            Cancel
          </button>
        </div>
      )}
    </div>
  );
}
