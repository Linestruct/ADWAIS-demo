// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Clock } from 'lucide-react';
import { SettingsPanel } from '../../common/layout/SettingsPanel';
import { InlineEditField } from '../../common/ui/InlineEditField';
import { FormSkeleton } from '../../common/ui/FormSkeleton';
import { ReadOnlyBanner } from '../../common/ui/ReadOnlyBanner';
import type { OrganizationConfigDto, UpdateOrganizationConfigRequestDto } from '@types';

interface FetchIntervalsPanelProps {
  config: OrganizationConfigDto | undefined;
  updateConfig: {
    mutateAsync: (variables: UpdateOrganizationConfigRequestDto) => Promise<void>;
  };
  disabled?: boolean;
}

export function FetchIntervalsPanel({ config, updateConfig, disabled }: FetchIntervalsPanelProps) {
  return (
    <SettingsPanel
      title="Fetch Intervals"
      subtitle="Organization job scheduling"
      icon={<Clock size={24} />}
    >
      <div className="flex flex-col gap-4">
        {disabled && config && (
          <ReadOnlyBanner message="You can review these schedules, but only administrators can change them." />
        )}

        {config ? (
          <div className="flex flex-col gap-2">
            <InlineEditField
              label="Order Fetch Interval (Min)"
              value={config.orderFetchIntervalMinutes ?? 30}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateConfig.mutateAsync({ orderFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="Uptime Fetch Interval (Min)"
              value={config.uptimeFetchIntervalMinutes ?? 5}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateConfig.mutateAsync({ uptimeFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="Latency Fetch Interval (Min)"
              value={config.latencyFetchIntervalMinutes ?? 5}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateConfig.mutateAsync({ latencyFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="User Stats Fetch Interval (Min)"
              value={config.userStatsFetchIntervalMinutes ?? 15}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateConfig.mutateAsync({ userStatsFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="Feed Fetch Interval (Hours)"
              value={config.feedFetchIntervalHours ?? 6}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateConfig.mutateAsync({ feedFetchIntervalHours: val })}
            />
          </div>
        ) : (
          <FormSkeleton>
            <FormSkeleton.Input labelWidth="w-48" />
            <FormSkeleton.Input labelWidth="w-48" />
            <FormSkeleton.Input labelWidth="w-48" />
            <FormSkeleton.Input labelWidth="w-56" />
            <FormSkeleton.Input labelWidth="w-48" />
          </FormSkeleton>
        )}
      </div>
    </SettingsPanel>
  );
}