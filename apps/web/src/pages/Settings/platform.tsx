// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useGlobalConfigQuery, useUpdateConfigMutation } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';
import { isAdminRole } from '../../utils/roles';

export function PlatformConfigurationView() {
    const { data: config } = useGlobalConfigQuery();
    const updateConfig = useUpdateConfigMutation();
    const { role } = useCurrentUser();
    const disabled = !isAdminRole(role);

    return (
        <div className="flex flex-col gap-4 contained:h-full contained:min-h-0">
            <GlobalConfigurationForm config={config} updateConfig={updateConfig} disabled={disabled} />
        </div>
    );
}