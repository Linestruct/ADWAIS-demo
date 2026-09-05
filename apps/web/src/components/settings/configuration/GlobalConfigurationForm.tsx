// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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
  embedded?: boolean;
}

export function GlobalConfigurationForm({ config, updateConfig, disabled, embedded = false }: GlobalConfigurationFormProps) {
  const content = (
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
          <InlineEditField
            label="Materialized View Refresh Interval (Minutes)"
            value={config.matViewRefreshIntervalMinutes ?? 60}
            kind="number"
            required
            requirement="At least 5"
            disabled={disabled}
            validate={val => val >= 5 ? undefined : 'Enter a value of at least 5.'}
            onCommit={(val) => updateConfig.mutateAsync({ matViewRefreshIntervalMinutes: val })}
          />
        </div>
      ) : (
        <FormSkeleton>
          <FormSkeleton.Input labelWidth="w-28" />
        </FormSkeleton>
      )}
    </div>
  );

  if (embedded) return content;

  return (
    <SettingsPanel
      title="Global Configuration"
      subtitle="Deployment-wide parameters"
      icon={<Settings size={24} />}
    >
      {content}
    </SettingsPanel>
  );
}