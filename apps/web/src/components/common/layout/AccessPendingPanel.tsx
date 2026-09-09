// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Loader2, LogOut, RefreshCw } from 'lucide-react';
import type { ReactNode } from 'react';
import { handleSessionInvalidation } from '../../../apiClient';
import { AuthLayout } from './AuthLayout';
import { AuthCard } from './AuthCard';
import { Button } from '../ui/Button';

function AccessPanel({ title, message, children }: {
  title: string;
  message: string;
  children?: ReactNode;
}) {
  return (
    <AuthLayout>
      <AuthCard className="!bg-surface-container border-outline-variant">
        <div className="flex flex-1 flex-col items-center justify-center gap-4 text-center">
          <h1 className="text-2xl font-bold text-on-surface">{title}</h1>
          <p className="max-w-md text-sm font-medium leading-relaxed text-on-surface-variant">{message}</p>
          {children}
        </div>
      </AuthCard>
    </AuthLayout>
  );
}

export function AccessPendingPanel({ onRetry, isRetrying = false }: {
  onRetry?: () => void;
  isRetrying?: boolean;
}) {
  return (
    <AccessPanel
      title="Account not provisioned"
      message="You signed in successfully, but your account has no organization access yet. Ask an administrator to provision your account, then check again."
    >
      <div className="flex flex-wrap justify-center gap-3">
        {onRetry && (
          <Button
            type="button"
            variant="outlined"
            color="secondary"
            icon={isRetrying ? <Loader2 size={16} className="animate-spin" aria-hidden="true" /> : <RefreshCw size={16} aria-hidden="true" />}
            onClick={onRetry}
            disabled={isRetrying}
          >
            {isRetrying ? 'Checking…' : 'Check again'}
          </Button>
        )}
        <Button
          type="button"
          variant="filled"
          color="secondary"
          icon={<LogOut size={16} aria-hidden="true" />}
          onClick={() => void handleSessionInvalidation()}
        >
          Sign out
        </Button>
      </div>
    </AccessPanel>
  );
}

export function AccessLoadingPanel() {
  return (
    <AccessPanel title="Loading application" message="Please wait while ADWAIS starts.">
      <Loader2 size={32} className="animate-spin text-secondary" aria-hidden="true" />
    </AccessPanel>
  );
}

export function AccessUnavailablePanel({ onRetry, isRetrying = false, backendUnavailable = false }: {
  onRetry: () => void;
  isRetrying?: boolean;
  backendUnavailable?: boolean;
}) {
  return (
    <AccessPanel
      title={backendUnavailable ? 'Backend unavailable' : 'Unable to verify account access'}
      message={backendUnavailable
        ? 'The application server is not reachable right now. Try again when it is available.'
        : 'The application could not verify your account right now. Try again, or sign out and return later.'}
    >
      <div className="flex flex-wrap justify-center gap-3">
        <Button
          type="button"
          variant="outlined"
          color="secondary"
          icon={isRetrying ? <Loader2 size={16} className="animate-spin" aria-hidden="true" /> : <RefreshCw size={16} aria-hidden="true" />}
          onClick={onRetry}
          disabled={isRetrying}
        >
          {isRetrying ? 'Retrying…' : 'Try again'}
        </Button>
        <Button
          type="button"
          variant="filled"
          color="secondary"
          icon={<LogOut size={16} aria-hidden="true" />}
          onClick={() => void handleSessionInvalidation()}
        >
          Sign out
        </Button>
      </div>
    </AccessPanel>
  );
}
