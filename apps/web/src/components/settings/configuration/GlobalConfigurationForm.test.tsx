// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { GlobalConfigDto, ProviderDescriptor } from '@types';
import { GlobalConfigurationForm } from './GlobalConfigurationForm';

const config: GlobalConfigDto = {
  monitoringProvider: 'uptimerobot',
  monitoringProviderConfiguredSecretKeys: ['apiKey'],
};

const providers: ProviderDescriptor[] = [{
  id: 'uptimerobot',
  displayName: 'UptimeRobot',
  settings: [{ key: 'apiKey', label: 'API Key', inputType: 'password', required: true }],
}];

describe('GlobalConfigurationForm', () => {
  it('sends only a replacement API key when edited', async () => {
    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    render(<GlobalConfigurationForm config={config} updateConfig={{ mutateAsync }} providers={providers} />);

    fireEvent.click(screen.getByRole('button', { name: 'Edit API Key' }));
    fireEvent.change(screen.getByLabelText('API Key'), { target: { value: 'next-key' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save API Key' }));

    await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({ monitoringProviderSettings: { apiKey: 'next-key' } }));
  });

  it('sends null when clearing a configured API key', async () => {
    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    render(<GlobalConfigurationForm config={config} updateConfig={{ mutateAsync }} providers={providers} />);

    fireEvent.click(screen.getByRole('button', { name: 'Edit API Key' }));
    fireEvent.click(screen.getByRole('button', { name: 'Clear' }));

    await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({ monitoringProviderSettings: { apiKey: null } }));
  });

  it('submits a provider change without replaying settings', async () => {
    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    const multipleProviders = [...providers, { id: 'other', displayName: 'Other', settings: [] }];
    render(<GlobalConfigurationForm config={config} updateConfig={{ mutateAsync }} providers={multipleProviders} />);

    fireEvent.click(screen.getByRole('button', { name: 'Edit Monitoring Provider' }));
    fireEvent.click(screen.getByRole('combobox', { name: 'Monitoring Provider' }));
    fireEvent.click(screen.getByRole('option', { name: 'Other' }));
    fireEvent.click(screen.getByRole('button', { name: 'Save Monitoring Provider' }));

    await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({ monitoringProvider: 'other' }));
  });
});
