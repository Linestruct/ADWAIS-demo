// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { AccessPendingPanel, AccessUnavailablePanel } from './AccessPendingPanel';

const mockHandleSessionInvalidation = vi.hoisted(() => vi.fn());
vi.mock('../../../apiClient', () => ({
  handleSessionInvalidation: () => mockHandleSessionInvalidation(),
}));

describe('AccessPendingPanel', () => {
  it('explains that the account is not provisioned and offers sign out', () => {
    render(<AccessPendingPanel />);

    expect(screen.getByText('Account not provisioned')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sign out' })).toBeInTheDocument();
  });

  it('signs out without looping back into the app', () => {
    render(<AccessPendingPanel />);

    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(mockHandleSessionInvalidation).toHaveBeenCalledOnce();
  });

  it('describes a backend outage separately from an account verification failure', () => {
    render(<AccessUnavailablePanel onRetry={vi.fn()} backendUnavailable />);

    expect(screen.getByText('Backend unavailable')).toBeInTheDocument();
    expect(screen.getByText(/server is not reachable/i)).toBeInTheDocument();
  });

  it('keeps a separate message for unexpected access-check failures', () => {
    render(<AccessUnavailablePanel onRetry={vi.fn()} />);

    expect(screen.getByText('Unable to verify account access')).toBeInTheDocument();
    expect(screen.getByText(/could not verify your account/i)).toBeInTheDocument();
  });
});
