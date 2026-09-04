// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import React, { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { isStaffRole } from '../../utils/roles';
import { useContext } from 'react';
import { AuthContext } from 'react-oidc-context';
import { KeyRound, LogOut, MonitorSmartphone } from 'lucide-react';
import { useActivateKioskMutation } from '../../hooks/useKioskAuth';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { SecureButton } from '../../components/common/ui/SecureButton';
import { Button } from '../../components/common/ui/Button';
import { FormField } from '../../components/common/ui/FormField';
import { removeKioskToken } from '../../utils/auth';
import { ErrorAlert } from '../../components/common/ui/ErrorAlert';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { useDeleteApiDashboardSession } from '../../api/generated/endpoints';
import { KioskDevicesPanel } from '../../components/settings/kiosk/KioskDevicesPanel';

export function AuthenticationSettings() {
  const [activationCode, setActivationCode] = useState('');
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');
  
  const { role } = useCurrentUser();
  const isStaff = isStaffRole(role);

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
        <div className="grid grid-cols-1 items-start gap-6 xl:grid-cols-2">
          <div className="rounded-xl border border-outline-variant p-6 space-y-4">
            <h3 className="flex items-center gap-2 text-lg font-bold text-on-surface">
              <MonitorSmartphone size={20} /> Kiosk activation
            </h3>
            <p className="text-sm text-on-surface-variant">Enter the code shown on a kiosk to authorize that display.</p>

            <form onSubmit={handleActivate} className="flex flex-col gap-4">
<FormField
                label="Activation Code"
                type="text"
                className="font-mono text-xl uppercase tracking-[0.35em]"
                style={{ textAlign: 'center' }}
                placeholder={isStaff ? "Input code" : "LOCKED"}
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

          <div className="rounded-xl border border-outline-variant p-6 space-y-4">
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
              <Button
                type="button"
                onClick={handleSignOut}
                variant="tonal"
                color="error"
                icon={<LogOut size={18} aria-hidden="true" />}
              >
                Sign Out
              </Button>
            </div>
          </div>
        </div>

        <div className="mt-6 rounded-xl border border-outline-variant p-6">
          <KioskDevicesPanel />
        </div>
      </div>
    </SettingsPanel>
    </div>
  );
}
