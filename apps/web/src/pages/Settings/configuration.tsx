// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useOrgSelection } from '../../hooks/useOrgSelection';
import { useOrganizationConfigQuery, useUpdateOrganizationConfigMutation } from '../../hooks/useOrganizationQueries';
import { FetchIntervalsPanel } from '../../components/settings/configuration/FetchIntervalsPanel';
import { CalendarSubscriptionsPanel } from '../../components/settings/configuration/CalendarSubscriptionsPanel';
import { OrganizationConfigurationForm } from '../../components/settings/organization/OrganizationConfigurationForm';
import { useMonitoringProviderDescriptorsQuery } from '../../hooks/useIntegrationQueries';
import { EmptyState } from '../../components/common/ui/EmptyState';
import { isAdminRole } from '../../utils/roles';

export function ConfigurationView() {
    const { data: monitoringProviders = [] } = useMonitoringProviderDescriptorsQuery();
    const { role, scope } = useCurrentUser();
    const { selectedOrgId, organizations } = useOrgSelection();
    const disabled = !isAdminRole(role);

    const orgId = scope.isPlatformAdmin
        ? selectedOrgId
        : selectedOrgId ?? scope.organizationId;
    const orgName = organizations.find(org => org.id === orgId)?.name ?? scope.organizationName;
    const { data: orgConfig } = useOrganizationConfigQuery(orgId);
    const updateOrgConfig = useUpdateOrganizationConfigMutation(orgId);

    if (!orgId) {
        return (
            <div className="flex h-full items-center justify-center">
                <EmptyState message="No organization selected." />
            </div>
        );
    }

    return (
        <div className="grid grid-cols-1 landscape-contained:grid-cols-2 gap-4 contained:h-full contained:min-h-0">
            <div className="flex flex-col gap-4 min-h-0 h-full">
                <FetchIntervalsPanel config={orgConfig} updateConfig={updateOrgConfig} disabled={disabled} />
                <CalendarSubscriptionsPanel disabled={disabled} />
            </div>
            <div className="flex flex-col gap-4 min-h-0 h-full">
                <OrganizationConfigurationForm orgName={orgName} config={orgConfig} updateConfig={updateOrgConfig} providers={monitoringProviders} disabled={disabled} />
            </div>
        </div>
    );
}