// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { Settings2, Eye } from 'lucide-react';
import { useGlobalConfigQuery, useUpdateConfigMutation } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SecureButton } from '../../components/common/ui/SecureButton';
import { usePostApiDashboardSession } from '../../api/generated/endpoints';
import { isAdminRole } from '../../utils/roles';
import { platformJobCatalog } from '../../utils/recurringJobLabels';

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

    const toggleVisibleJob = (jobId: string, enabled: boolean) => {
        const current = config?.visibleRecurringJobs ?? [];
        const next = enabled
            ? [...current, jobId]
            : current.filter((id) => id !== jobId);
        void updateConfig.mutateAsync({ visibleRecurringJobs: next });
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
            <SettingsPanel
                title="Visible Scheduled Jobs"
                subtitle="Platform-wide jobs that organizations can see in their job schedules"
                icon={<Eye size={24} />}
            >
                <div className="flex flex-col gap-2">
                    {platformJobCatalog().map((job) => {
                        const checked = (config?.visibleRecurringJobs ?? []).includes(job.id);
                        return (
                            <label
                                key={job.id}
                                className={`flex min-h-11 cursor-pointer items-center gap-3 rounded-xl border px-4 py-2 transition-colors ${checked ? 'border-secondary bg-secondary-container/30' : 'border-outline-variant bg-surface-container-low hover:bg-surface-container'}`}
                            >
                                <input
                                    type="checkbox"
                                    checked={checked}
                                    disabled={disabled}
                                    onChange={(e) => toggleVisibleJob(job.id, e.target.checked)}
                                    className="h-5 w-5 rounded border-outline-variant text-secondary focus:ring-2 focus:ring-secondary/40 cursor-pointer"
                                />
                                <span className="text-sm font-bold text-on-surface">{job.label}</span>
                                <span className="ml-auto text-xs font-medium text-on-surface-variant break-all">{job.id}</span>
                            </label>
                        );
                    })}
                </div>
            </SettingsPanel>
        </div>
    );
}