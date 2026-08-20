// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Clock } from 'lucide-react';
import { SettingsPanel } from '../../common/layout/SettingsPanel';
import { InlineEditField } from '../../common/ui/InlineEditField';
import { FormSkeleton } from '../../common/ui/FormSkeleton';
import { ReadOnlyBanner } from '../../common/ui/ReadOnlyBanner';
import type { FetchIntervalsDto } from '../../../hooks/useJobSettingsQueries';

interface FetchIntervalsFormProps {
  intervals: FetchIntervalsDto | undefined;
  updateIntervals: {
    mutateAsync: (variables: Partial<FetchIntervalsDto>) => Promise<void>;
  };
  disabled?: boolean;
}

export function FetchIntervalsForm({ intervals, updateIntervals, disabled }: FetchIntervalsFormProps) {
  return (
    <SettingsPanel
      title="Fetch Intervals"
      subtitle="Job scheduling metrics"
      icon={<Clock size={24} />}
    >
      <div className="flex flex-col gap-4">
        {disabled && intervals && (
          <ReadOnlyBanner message="You can review these schedules, but only administrators can change them." />
        )}

        {intervals ? (
          <div className="flex flex-col gap-2">
            <InlineEditField
              label="Order Fetch Interval (mins)"
              value={intervals.orderFetchIntervalMinutes}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateIntervals.mutateAsync({ orderFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="Latency Fetch Interval (mins)"
              value={intervals.latencyFetchIntervalMinutes}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateIntervals.mutateAsync({ latencyFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="Uptime Fetch Interval (mins)"
              value={intervals.uptimeFetchIntervalMinutes}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateIntervals.mutateAsync({ uptimeFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="User Stats Fetch Interval (mins)"
              value={intervals.userStatsFetchIntervalMinutes}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateIntervals.mutateAsync({ userStatsFetchIntervalMinutes: val })}
            />
            <InlineEditField
              label="Feed Fetch Interval (hours)"
              value={intervals.feedFetchIntervalHours}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateIntervals.mutateAsync({ feedFetchIntervalHours: val })}
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
