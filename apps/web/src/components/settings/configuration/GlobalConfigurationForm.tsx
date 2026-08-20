// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Settings } from 'lucide-react';
import { SettingsPanel } from '../../common/layout/SettingsPanel';
import { InlineEditField } from '../../common/ui/InlineEditField';
import { FormSkeleton } from '../../common/ui/FormSkeleton';
import { ReadOnlyBanner } from '../../common/ui/ReadOnlyBanner';
import type { GlobalConfigDto, ProviderDescriptor, UpdateGlobalConfigRequestDto } from '@types';

interface GlobalConfigurationFormProps {
  config: GlobalConfigDto | undefined;
  updateConfig: {
    mutateAsync: (variables: UpdateGlobalConfigRequestDto) => Promise<void>;
  };
  providers: ProviderDescriptor[];
  disabled?: boolean;
}

export function GlobalConfigurationForm({ config, updateConfig, providers, disabled }: GlobalConfigurationFormProps) {
  const selectedProvider = providers.find(provider => provider.id === config?.monitoringProvider);
  const providerSettings = selectedProvider?.settings?.filter(setting => setting.key) ?? [];

  return (
    <SettingsPanel
      title="Global Configuration"
      subtitle="System-wide parameters"
      icon={<Settings size={24} />}
    >
      <div className="flex flex-col gap-4">
        {disabled && config && (
          <ReadOnlyBanner message="You can review these values, but only administrators can change them." />
        )}

        {config ? (
          <div className="flex flex-col gap-2">
            {providers.length > 0 && (
              <InlineEditField
                label="Monitoring Provider"
                value={config.monitoringProvider || ''}
                kind="select"
                options={providers.flatMap(provider => provider.id
                  ? [{ label: provider.displayName || provider.id, value: provider.id }]
                  : [])}
                required
                disabled={disabled}
                onCommit={monitoringProvider => updateConfig.mutateAsync({ monitoringProvider })}
              />
            )}
            {providerSettings.map(setting => {
              const key = setting.key!;
              const isSecret = setting.inputType === 'password';
              const value = isSecret
                ? config.monitoringProviderConfiguredSecretKeys?.includes(key) ? 'configured' : ''
                : config.monitoringProviderSettings?.[key] || '';

              return (
                <InlineEditField
                  key={key}
                  label={setting.label || key}
                  value={value}
                  kind={isSecret ? 'password' : 'text'}
                  required={setting.required}
                  placeholder={setting.placeholder || undefined}
                  canClear
                  disabled={disabled}
                  onCommit={nextValue => updateConfig.mutateAsync({ monitoringProviderSettings: { [key]: nextValue } })}
                  onClear={() => updateConfig.mutateAsync({ monitoringProviderSettings: { [key]: null } })}
                />
              );
            })}
            <InlineEditField
              label="Order Fetch Enabled"
              value={config.orderFetchEnabled ?? false}
              kind="checkbox"
              disabled={disabled}
              onCommit={(val) => updateConfig.mutateAsync({ orderFetchEnabled: val })}
            />
            <InlineEditField
              label="Monitoring Fetch Enabled"
              value={config.monitoringFetchEnabled ?? false}
              kind="checkbox"
              disabled={disabled}
              onCommit={(val) => updateConfig.mutateAsync({ monitoringFetchEnabled: val })}
            />
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
              label="Reporting Timezone"
              value={config.reportingTimeZoneId || 'Europe/Stockholm'}
              kind="text"
              required
              requirement="Valid IANA identifier"
              placeholder="e.g. Europe/Stockholm"
              disabled={disabled}
              onCommit={(val) => updateConfig.mutateAsync({ reportingTimeZoneId: val })}
            />
            <InlineEditField
              label="Weather Location"
              value={config.weatherLocation || ''}
              kind="text"
              required
              requirement="Required for weather"
              placeholder="e.g. Stockholm, SE"
              disabled={disabled}
              onCommit={(val) => updateConfig.mutateAsync({ weatherLocation: val || null })}
            />
            <InlineEditField
              label="Weather Fetch Interval (Min)"
              value={config.weatherFetchIntervalMinutes ?? 15}
              kind="number"
              required
              requirement="Greater than 0"
              disabled={disabled}
              validate={val => val > 0 ? undefined : 'Enter a value greater than 0.'}
              onCommit={(val) => updateConfig.mutateAsync({ weatherFetchIntervalMinutes: val })}
            />
          </div>
        ) : (
          <FormSkeleton>
            <FormSkeleton.Input labelWidth="w-28" />
            <FormSkeleton.Checkbox textWidth="w-36" />
            <FormSkeleton.Checkbox textWidth="w-36" />
            <FormSkeleton.Input labelWidth="w-36" />
            <FormSkeleton.Input labelWidth="w-32" />
            <FormSkeleton.Input labelWidth="w-36" />
          </FormSkeleton>
        )}
      </div>
    </SettingsPanel>
  );
}
