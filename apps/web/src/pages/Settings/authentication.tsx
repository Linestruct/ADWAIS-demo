// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import React, { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { useContext } from 'react';
import { AuthContext } from 'react-oidc-context';
import { KeyRound, LogOut, MonitorSmartphone } from 'lucide-react';
import { useActivateKioskMutation } from '../../hooks/useKioskAuth';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { SecureButton } from '../../components/common/ui/SecureButton';
import { Input } from '../../components/common/ui/Input';
import { removeKioskToken } from '../../utils/auth';
import { ErrorAlert } from '../../components/common/ui/ErrorAlert';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { useDeleteApiDashboardSession } from '../../api/generated/endpoints';

export function AuthenticationSettings() {
  const [activationCode, setActivationCode] = useState('');
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');
  
  const { role } = useCurrentUser();
  const isStaff = role === 'Admin' || role === 'Employee';

  const activateMutation = useActivateKioskMutation(() => {
    setSuccessMsg('Kiosk activated successfully!');
    setErrorMsg('');
    setActivationCode('');
    setTimeout(() => setSuccessMsg(''), 5000);
  });

  const handleActivate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!isStaff) return;
    setErrorMsg('');
    setSuccessMsg('');
    if (!activationCode || activationCode.length !== 6) {
      setErrorMsg('Code must be exactly 6 characters.');
      return;
    }

    activateMutation.mutate(
      { activationCode },
      {
        onError: (err: Error) => {
          setErrorMsg(err.message || 'Failed to activate kiosk. Code may be expired or invalid.');
        }
      }
    );
  };

  const auth = useContext(AuthContext);
  const navigate = useNavigate();
  const dashboardSessionMutation = useDeleteApiDashboardSession();

  const handleSignOut = async () => {
    await dashboardSessionMutation.mutateAsync().catch(() => undefined);
    removeKioskToken();
    if (auth?.isAuthenticated) {
      await auth.signoutRedirect();
    } else {
      navigate({ to: '/login' });
    }
  };

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <SettingsPanel className="flex-1 max-h-none">
      <SettingsPanelHeader
        title="Authentication"
        subtitle="Authorize kiosk displays and manage the session on this device."
        icon={<KeyRound size={24} />}
      />
      <div className="custom-scrollbar flex-1 overflow-y-auto px-6 py-6">
        <div className="flex flex-col gap-6 max-w-3xl">
          <div className="space-y-4">
            <h3 className="flex items-center gap-2 text-lg font-bold text-on-surface">
              <MonitorSmartphone size={20} /> Kiosk activation
            </h3>
            <p className="text-sm text-on-surface-variant">Enter the code shown on a kiosk to authorize that display.</p>

            <form onSubmit={handleActivate} className="flex flex-col gap-4">
              <Input
                label="Activation Code"
                type="text"
                className="text-center font-mono text-xl uppercase tracking-[0.35em]"
                placeholder={isStaff ? "XXXXXX" : "LOCKED"}
                value={activationCode}
                onChange={(e) => setActivationCode(e.target.value.toUpperCase().slice(0, 6))}
                disabled={activateMutation.isPending || !isStaff}
              />

              {errorMsg && (
                <ErrorAlert
                  title="Unable to activate kiosk"
                  message={errorMsg}
                  onDismiss={() => setErrorMsg('')}
                />
              )}
              {successMsg && (
                <div className="rounded-xl bg-primary-container p-3 text-sm font-semibold text-on-primary-container">
                  {successMsg}
                </div>
              )}

              <SecureButton
                type="submit"
                locked={!isStaff}
                lockTitle="Requires Staff (Employee or Admin) role"
                loading={activateMutation.isPending}
                loadingText="Activating…"
                className="inline-flex min-h-11 w-fit cursor-pointer items-center justify-center gap-2 self-start rounded-full bg-on-primary-container px-5 text-base font-bold text-primary-container transition-colors hover:bg-brand-btn-quaternary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary"
                disabled={activationCode.length !== 6}
              >
                Activate Kiosk
              </SecureButton>
            </form>
          </div>

          <hr className="border-outline-variant" />

          <div className="space-y-4">
            <h3 className="flex items-center gap-2 text-lg font-bold text-on-surface">
              <KeyRound size={20} /> Current session
            </h3>
            <p className="text-sm text-on-surface-variant">Review the signed-in account or remove access from this device.</p>

            <div className="rounded-xl bg-surface-container-high p-4">
              <span className="text-sm font-bold uppercase tracking-wider text-on-surface-variant">Signed in as</span>
              <p className="mt-1 break-all text-base font-bold text-on-surface">
                {auth?.user?.profile?.preferred_username || auth?.user?.profile?.email || 'Kiosk session'}
              </p>
            </div>
            
            <div className="flex flex-wrap items-center justify-between gap-3">
              <span className="text-sm font-medium text-on-surface-variant">This only signs out the current device.</span>
              <button
                type="button"
                onClick={handleSignOut}
                className="inline-flex min-h-11 cursor-pointer items-center justify-center gap-2 rounded-full hover:bg-error px-5 text-base font-bold hover:text-on-error transition-colors bg-error-container text-on-error-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error"
              >
                <LogOut size={18} aria-hidden="true" />
                Sign Out
              </button>
            </div>
          </div>
        </div>
      </div>
    </SettingsPanel>
    </div>
  );
}
