// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { Settings2 } from 'lucide-react';
import { useGlobalConfigQuery, useUpdateConfigMutation, useRecurringJobsQuery } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';
import { SettingsPanel } from '../../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../../components/common/layout/SettingsPanelHeader';
import { SecureButton } from '../../components/common/ui/SecureButton';
import { CheckboxField } from '../../components/common/ui/FormField';
import { usePostApiDashboardSession } from '../../api/generated/endpoints';
import { isAdminRole } from '../../utils/roles';
import { isPlatformWideJob, recurringJobDisplayName } from '../../utils/recurringJobLabels';

export function PlatformConfigurationView() {
    const { data: config } = useGlobalConfigQuery();
    const { data: recurringJobs } = useRecurringJobsQuery();
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
        <div className="flex h-full min-h-0 flex-col gap-4">
            <SettingsPanel className="flex-1 max-h-none">
                <SettingsPanelHeader
                    title="Platform"
                    subtitle="Deployment-wide parameters"
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
                </SettingsPanelHeader>

                <div className="custom-scrollbar flex-1 overflow-y-auto px-6 py-6">
                    <div className="grid grid-cols-1 items-start gap-10 xl:grid-cols-2">
                        <section>
                            <h3 className="text-lg font-bold text-on-surface">Global Configuration</h3>
                            <div className="mt-4">
                                <GlobalConfigurationForm config={config} updateConfig={updateConfig} disabled={disabled} embedded />
                            </div>
                        </section>

                        <section>
                            <h3 className="text-lg font-bold text-on-surface">Visible Scheduled Jobs</h3>
                            <p className="mt-1 text-sm text-on-surface-variant">
                                Platform-wide jobs that organizations can see in their job schedules.
                            </p>
                            <div className="mt-4 flex flex-col gap-2">
                                {(recurringJobs ?? [])
                                    .filter((job) => isPlatformWideJob(job.id))
                                    .sort((a, b) => recurringJobDisplayName(a.id).localeCompare(recurringJobDisplayName(b.id)))
                                    .map((job) => (
                                        <CheckboxField
                                            key={job.id}
                                            label={recurringJobDisplayName(job.id)}
                                            meta={job.id}
                                            checked={(config?.visibleRecurringJobs ?? []).includes(job.id)}
                                            disabled={disabled}
                                            onChange={(e) => toggleVisibleJob(job.id, e.target.checked)}
                                        />
                                    ))}
                            </div>
                        </section>
                    </div>
                </div>
            </SettingsPanel>
        </div>
    );
}