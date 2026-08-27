// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { OrganizationConfigurationForm } from './OrganizationConfigurationForm';
import type { OrganizationConfigDto, ProviderDescriptor, UpdateOrganizationConfigRequestDto } from '@types';

const config: OrganizationConfigDto = {
  weatherLocation: 'Stockholm, SE',
  weatherFetchIntervalMinutes: 15,
  reportingTimeZoneId: 'Europe/Stockholm',
  monitoringProvider: 'uptimerobot',
  monitoringProviderSettings: { apiKey: 'secret-value', channelId: '123' },
  monitoringProviderConfiguredSecretKeys: ['apiKey'],
  orderFetchIntervalMinutes: 30,
  uptimeFetchIntervalMinutes: 5,
  latencyFetchIntervalMinutes: 5,
  userStatsFetchIntervalMinutes: 15,
  feedFetchIntervalHours: 6,
};

const providers: ProviderDescriptor[] = [
  {
    id: 'uptimerobot',
    displayName: 'UptimeRobot',
    settings: [
      { key: 'apiKey', label: 'API Key', inputType: 'password', required: true, placeholder: 'Token' },
      { key: 'channelId', label: 'Channel ID', inputType: 'text', required: false, placeholder: undefined },
    ],
  },
];

function renderForm(overrides?: {
  orgName?: string | null;
  disabled?: boolean;
  updateConfig?: { mutateAsync: (variables: UpdateOrganizationConfigRequestDto) => Promise<void> };
}) {
  return render(
    <OrganizationConfigurationForm
      orgName={overrides?.orgName ?? 'Acme'}
      config={config}
      updateConfig={overrides?.updateConfig ?? { mutateAsync: vi.fn() }}
      providers={providers}
      disabled={overrides?.disabled}
    />,
  );
}

describe('OrganizationConfigurationForm', () => {
  it('shows the organization name and the org-scoped parameters', () => {
    renderForm();

    expect(screen.getByRole('heading', { name: 'Organization Configuration' })).toBeInTheDocument();
    expect(screen.getByText('Parameters for Acme')).toBeInTheDocument();
    expect(screen.getByText('Europe/Stockholm')).toBeInTheDocument();
    expect(screen.getByText('Stockholm, SE')).toBeInTheDocument();
    expect(screen.getByText('uptimerobot')).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
  });

  it('masks configured secret keys but shows plain provider settings', () => {
    renderForm();

    expect(screen.getByText('••••••••••••')).toBeInTheDocument();
    expect(screen.getByText('123')).toBeInTheDocument();
  });

  it('commits an edited timezone through the update callback', async () => {
    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    renderForm({ updateConfig: { mutateAsync } });

    fireEvent.click(screen.getByRole('button', { name: 'Edit Reporting Timezone' }));
    const input = screen.getByRole('textbox', { name: 'Reporting Timezone' });
    fireEvent.change(input, { target: { value: 'Europe/Paris' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Reporting Timezone' }));

    await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({ reportingTimeZoneId: 'Europe/Paris' }));
  });

  it('clears a configured secret key through the clear callback', async () => {
    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    const onClear = vi.fn().mockResolvedValue(undefined);
    render(
      <OrganizationConfigurationForm
        orgName="Acme"
        config={config}
        updateConfig={{ mutateAsync, onClear }}
        providers={providers}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Edit API Key' }));
    fireEvent.click(screen.getByRole('button', { name: 'Clear' }));

    await waitFor(() => expect(onClear).toHaveBeenCalledOnce());
    expect(mutateAsync).not.toHaveBeenCalled();
  });

  it('renders read-only with a banner and no edit affordances when disabled', () => {
    renderForm({ disabled: true });

    expect(screen.getByRole('status')).toHaveTextContent('only administrators can change them');
    expect(screen.queryByRole('button', { name: /^Edit / })).not.toBeInTheDocument();
  });
});