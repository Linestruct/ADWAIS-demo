// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useGlobalConfigQuery, useUpdateConfigMutation } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useOrgSelection } from '../../hooks/useOrgSelection';
import { useOrganizationConfigQuery, useUpdateOrganizationConfigMutation } from '../../hooks/useOrganizationQueries';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';
import { CalendarSubscriptionsPanel } from '../../components/settings/configuration/CalendarSubscriptionsPanel';
import { OrganizationConfigurationForm } from '../../components/settings/organization/OrganizationConfigurationForm';
import { useMonitoringProviderDescriptorsQuery } from '../../hooks/useIntegrationQueries';

export function ConfigurationView() {
    const { data: config } = useGlobalConfigQuery();
    const updateConfig = useUpdateConfigMutation();
    const { data: monitoringProviders = [] } = useMonitoringProviderDescriptorsQuery();
    const { role, scope } = useCurrentUser();
    const { selectedOrgId, organizations } = useOrgSelection();
    const disabled = role !== 'Admin';

    const orgId = scope.isPlatformAdmin
        ? selectedOrgId
        : selectedOrgId ?? scope.organizationId;
    const orgName = organizations.find(org => org.id === orgId)?.name ?? scope.organizationName;
    const { data: orgConfig } = useOrganizationConfigQuery(orgId);
    const updateOrgConfig = useUpdateOrganizationConfigMutation(orgId);

    return (
        <div className="grid grid-cols-1 landscape-contained:grid-cols-2 gap-4 contained:h-full contained:min-h-0">
            {scope.isPlatformAdmin && (
                <div className="flex flex-col gap-4 min-h-0 h-full">
                    <GlobalConfigurationForm config={config} updateConfig={updateConfig} disabled={disabled} />
                </div>
            )}
            <div className="flex flex-col gap-4 min-h-0 h-full">
                {orgId && (
                    <>
                        <OrganizationConfigurationForm orgName={orgName} config={orgConfig} updateConfig={updateOrgConfig} providers={monitoringProviders} disabled={disabled} />
                        <CalendarSubscriptionsPanel disabled={disabled} />
                    </>
                )}
            </div>
        </div>
    );
}
