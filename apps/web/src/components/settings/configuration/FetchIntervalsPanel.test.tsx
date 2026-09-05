// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { FetchIntervalsPanel } from './FetchIntervalsPanel';
import type { OrganizationConfigDto, UpdateOrganizationConfigRequestDto } from '@types';

const config: OrganizationConfigDto = {
  weatherLocation: 'Stockholm, SE',
  weatherFetchIntervalMinutes: 15,
  reportingTimeZoneId: 'Europe/Stockholm',
  monitoringProvider: 'uptimerobot',
  monitoringProviderSettings: {},
  monitoringProviderConfiguredSecretKeys: [],
  orderFetchEnabled: true,
  monitoringFetchEnabled: true,
  orderFetchIntervalMinutes: 30,
  uptimeFetchIntervalMinutes: 5,
  latencyFetchIntervalMinutes: 5,
  userStatsFetchIntervalMinutes: 15,
  feedFetchIntervalHours: 6,
};

function renderPanel(overrides?: {
  disabled?: boolean;
  updateConfig?: { mutateAsync: (variables: UpdateOrganizationConfigRequestDto) => Promise<void> };
}) {
  return render(
    <FetchIntervalsPanel
      config={config}
      updateConfig={overrides?.updateConfig ?? { mutateAsync: vi.fn() }}
      disabled={overrides?.disabled}
    />,
  );
}

describe('FetchIntervalsPanel', () => {
  it('commits an edited interval through the update callback', async () => {
    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    renderPanel({ updateConfig: { mutateAsync } });

    fireEvent.click(screen.getByRole('button', { name: 'Edit Order Fetch Interval (Min)' }));
    const input = screen.getByRole('spinbutton', { name: 'Order Fetch Interval (Min)' });
    fireEvent.change(input, { target: { value: '120' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Order Fetch Interval (Min)' }));

    await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({ orderFetchIntervalMinutes: 120 }));
  });

  it('renders read-only when disabled', () => {
    renderPanel({ disabled: true });

    expect(screen.getByRole('status')).toHaveTextContent('only administrators can change them');
    expect(screen.queryByRole('button', { name: /^Edit / })).not.toBeInTheDocument();
  });
});