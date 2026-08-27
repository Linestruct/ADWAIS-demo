// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { GlobalConfigDto } from '@types';
import { GlobalConfigurationForm } from './GlobalConfigurationForm';

const config: GlobalConfigDto = {
  systemEventRetentionDays: 30,
};

describe('GlobalConfigurationForm', () => {
  it('commits the system event retention', async () => {
    const mutateAsync = vi.fn().mockResolvedValue(undefined);
    render(<GlobalConfigurationForm config={config} updateConfig={{ mutateAsync }} />);

    fireEvent.click(screen.getByRole('button', { name: 'Edit Event Retention (Days)' }));
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Event Retention (Days)' }), { target: { value: '60' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Event Retention (Days)' }));

    await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({ systemEventRetentionDays: 60 }));
  });

  it('shows a read-only banner when disabled', () => {
    render(<GlobalConfigurationForm config={config} updateConfig={{ mutateAsync: vi.fn() }} disabled />);

    expect(screen.getByRole('status')).toHaveTextContent('only administrators can change them');
    expect(screen.queryByRole('button', { name: /^Edit / })).not.toBeInTheDocument();
  });
});