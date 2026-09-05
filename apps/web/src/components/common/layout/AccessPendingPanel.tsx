// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { LogOut } from 'lucide-react';
import { handleSessionInvalidation } from '../../../apiClient';
import { AuthLayout } from './AuthLayout';
import { AuthCard } from './AuthCard';
import { Button } from '../ui/Button';

export function AccessPendingPanel() {
  return (
    <AuthLayout>
      <AuthCard>
        <div className="flex flex-1 flex-col items-center justify-center gap-4 text-center">
          <h1 className="text-2xl font-bold text-on-surface">Account not provisioned</h1>
          <p className="max-w-md text-sm font-medium leading-relaxed text-on-surface-variant">
            You signed in successfully, but your account has no organization access yet.
            Ask an administrator to provision your account, then sign in again.
          </p>
          <Button
            type="button"
            variant="filled"
            color="primary"
            icon={<LogOut size={16} aria-hidden="true" />}
            onClick={() => void handleSessionInvalidation()}
          >
            Sign out
          </Button>
        </div>
      </AuthCard>
    </AuthLayout>
  );
}
