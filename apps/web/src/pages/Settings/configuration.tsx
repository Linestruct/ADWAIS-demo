// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useGlobalConfigQuery, useFetchIntervalsQuery, useUpdateConfigMutation, useUpdateFetchIntervalsMutation } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';
import { FetchIntervalsForm } from '../../components/settings/configuration/FetchIntervalsForm';
import { CalendarSubscriptionsPanel } from '../../components/settings/configuration/CalendarSubscriptionsPanel';
import { useMonitoringProviderDescriptorsQuery } from '../../hooks/useIntegrationQueries';

export function ConfigurationView() {
    const { data: config } = useGlobalConfigQuery();
    const { data: intervals } = useFetchIntervalsQuery();
    const updateConfig = useUpdateConfigMutation();
    const updateIntervals = useUpdateFetchIntervalsMutation();
    const { data: monitoringProviders = [] } = useMonitoringProviderDescriptorsQuery();
    const { role } = useCurrentUser();
    const disabled = role !== 'Admin';

    return (
        <div className="grid grid-cols-1 landscape-contained:grid-cols-2 gap-4 contained:h-full contained:min-h-0">
            <div className="flex flex-col gap-4 min-h-0 h-full">
                <FetchIntervalsForm intervals={intervals} updateIntervals={updateIntervals} disabled={disabled} />
                <CalendarSubscriptionsPanel disabled={disabled} />
            </div>
            <div className="flex flex-col gap-4 min-h-0 h-full">
                <GlobalConfigurationForm config={config} updateConfig={updateConfig} providers={monitoringProviders} disabled={disabled} />
            </div>
        </div>
    );
}
