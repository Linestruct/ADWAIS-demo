// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { Settings2 } from 'lucide-react';
import { useGlobalConfigQuery, useUpdateConfigMutation } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SecureButton } from '../../components/common/ui/SecureButton';
import { usePostApiDashboardSession } from '../../api/generated/endpoints';
import { isAdminRole } from '../../utils/roles';

export function PlatformConfigurationView() {
    const { data: config } = useGlobalConfigQuery();
    const updateConfig = useUpdateConfigMutation();
    const { role } = useCurrentUser();
    const disabled = !isAdminRole(role);
    const dashboardSessionMutation = usePostApiDashboardSession();

    const openDashboard = async () => {
        const dashboardTab = window.open('about:blank', '_blank');
        if (dashboardTab) dashboardTab.opener = null;

        try {
            await dashboardSessionMutation.mutateAsync();
            if (dashboardTab) {
                dashboardTab.location.replace('/hangfire');
            } else {
                window.location.assign('/hangfire');
            }
        } catch (error) {
            dashboardTab?.close();
            window.alert(error instanceof Error ? error.message : 'Unable to open the Hangfire dashboard.');
        }
    };

    return (
        <div className="flex flex-col gap-4 contained:h-full contained:min-h-0">
            <GlobalConfigurationForm config={config} updateConfig={updateConfig} disabled={disabled} />
            <SettingsPanel
                title="Hangfire Dashboard"
                subtitle="Scheduler internals, queues, and recurring jobs"
                icon={<Settings2 size={24} />}
            >
                <SecureButton
                    onClick={openDashboard}
                    locked={!isAdminRole(role)}
                    lockTitle="Requires Admin privileges"
                    loading={dashboardSessionMutation.isPending}
                    loadingText="Opening Dashboard..."
                    className="flex min-h-11 w-fit cursor-pointer items-center justify-center gap-2 rounded-full border border-outline enabled:hover:bg-surface-container px-5 text-sm font-bold text-on-surface transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary"
                >
                    Open Hangfire Dashboard
                </SecureButton>
            </SettingsPanel>
        </div>
    );
}