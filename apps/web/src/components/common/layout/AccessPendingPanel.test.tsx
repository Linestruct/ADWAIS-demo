// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { AccessPendingPanel } from './AccessPendingPanel';

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
});
