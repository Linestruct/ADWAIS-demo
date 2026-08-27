// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { Settings } from 'lucide-react';
import { SettingsPanel } from '../../common/layout/SettingsPanel';
import { InlineEditField } from '../../common/ui/InlineEditField';
import { FormSkeleton } from '../../common/ui/FormSkeleton';
import { ReadOnlyBanner } from '../../common/ui/ReadOnlyBanner';
import type { GlobalConfigDto, UpdateGlobalConfigRequestDto } from '@types';

interface GlobalConfigurationFormProps {
  config: GlobalConfigDto | undefined;
  updateConfig: {
    mutateAsync: (variables: UpdateGlobalConfigRequestDto) => Promise<void>;
  };
  disabled?: boolean;
}

export function GlobalConfigurationForm({ config, updateConfig, disabled }: GlobalConfigurationFormProps) {
  return (
    <SettingsPanel
      title="Global Configuration"
      subtitle="Deployment-wide parameters"
      icon={<Settings size={24} />}
    >
      <div className="flex flex-col gap-4">
        {disabled && config && (
          <ReadOnlyBanner message="You can review these values, but only administrators can change them." />
        )}

        {config ? (
          <div className="flex flex-col gap-2">
            <InlineEditField
              label="Event Retention (Days)"
              value={config.systemEventRetentionDays ?? 30}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateConfig.mutateAsync({ systemEventRetentionDays: val })}
            />
          </div>
        ) : (
          <FormSkeleton>
            <FormSkeleton.Input labelWidth="w-28" />
          </FormSkeleton>
        )}
      </div>
    </SettingsPanel>
  );
}