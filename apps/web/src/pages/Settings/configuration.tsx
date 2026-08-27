// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useGlobalConfigQuery, useFetchIntervalsQuery, useUpdateConfigMutation, useUpdateFetchIntervalsMutation } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useOrganizationConfigQuery, useUpdateOrganizationConfigMutation } from '../../hooks/useOrganizationQueries';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';
import { FetchIntervalsForm } from '../../components/settings/configuration/FetchIntervalsForm';
import { CalendarSubscriptionsPanel } from '../../components/settings/configuration/CalendarSubscriptionsPanel';
import { OrganizationConfigurationForm } from '../../components/settings/organization/OrganizationConfigurationForm';
import { useMonitoringProviderDescriptorsQuery } from '../../hooks/useIntegrationQueries';

export function ConfigurationView() {
    const { data: config } = useGlobalConfigQuery();
    const { data: intervals } = useFetchIntervalsQuery();
    const updateConfig = useUpdateConfigMutation();
    const updateIntervals = useUpdateFetchIntervalsMutation();
    const { data: monitoringProviders = [] } = useMonitoringProviderDescriptorsQuery();
    const { role, scope } = useCurrentUser();
    const disabled = role !== 'Admin';

    const orgId = scope.organizationId;
    const { data: orgConfig } = useOrganizationConfigQuery(orgId);
    const updateOrgConfig = useUpdateOrganizationConfigMutation(orgId);

    return (
        <div className="grid grid-cols-1 landscape-contained:grid-cols-2 gap-4 contained:h-full contained:min-h-0">
            {scope.isPlatformAdmin && (
                <div className="flex flex-col gap-4 min-h-0 h-full">
                    <FetchIntervalsForm intervals={intervals} updateIntervals={updateIntervals} disabled={disabled} />
                    <CalendarSubscriptionsPanel disabled={disabled} />
                </div>
            )}
            <div className="flex flex-col gap-4 min-h-0 h-full">
                {scope.isPlatformAdmin && (
                    <GlobalConfigurationForm config={config} updateConfig={updateConfig} providers={monitoringProviders} disabled={disabled} />
                )}
                {orgId && (
                    <OrganizationConfigurationForm orgName={scope.organizationName} config={orgConfig} updateConfig={updateOrgConfig} providers={monitoringProviders} disabled={disabled} />
                )}
            </div>
        </div>
    );
}
