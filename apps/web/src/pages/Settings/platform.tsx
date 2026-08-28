// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useGlobalConfigQuery, useUpdateConfigMutation } from '../../hooks/useJobSettingsQueries';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { GlobalConfigurationForm } from '../../components/settings/configuration/GlobalConfigurationForm';

export function PlatformConfigurationView() {
    const { data: config } = useGlobalConfigQuery();
    const updateConfig = useUpdateConfigMutation();
    const { role } = useCurrentUser();
    const disabled = role !== 'Admin';

    return (
        <div className="grid grid-cols-1 landscape-contained:grid-cols-2 gap-4 contained:h-full contained:min-h-0">
            <GlobalConfigurationForm config={config} updateConfig={updateConfig} disabled={disabled} />
        </div>
    );
}