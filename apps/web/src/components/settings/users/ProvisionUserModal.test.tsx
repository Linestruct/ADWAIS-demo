// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ProvisionUserModal } from './ProvisionUserModal';

const testState = vi.hoisted(() => ({
  isPlatformAdmin: false,
  organizations: [
    { id: 'org-1', name: 'Acme' },
    { id: 'org-2', name: 'Other' },
  ],
  createUser: vi.fn(),
}));

vi.mock('../../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({
    role: testState.isPlatformAdmin ? 'PlatformAdmin' : 'Admin',
    user: testState.isPlatformAdmin ? { isPlatformAdmin: true } : { isPlatformAdmin: false },
  }),
}));

vi.mock('../../../hooks/useMembershipQueries', () => ({
  useOrganizationsForPickerQuery: () => ({ data: testState.organizations }),
}));

function renderModal() {
  render(
    <ProvisionUserModal
      isOpen
      onClose={() => undefined}
      createUser={{ mutate: testState.createUser, isPending: false }}
    />,
  );
}

describe('provision user modal', () => {
  beforeEach(() => {
    testState.isPlatformAdmin = false;
    testState.createUser.mockReset();
  });

  it('hides the organization select from staff', () => {
    renderModal();
    expect(screen.queryByLabelText('Organization')).toBeNull();
  });

  it('creates the user in the caller organization for staff', () => {
    renderModal();
    fireEvent.change(screen.getByLabelText('Email Address'), { target: { value: 'staff@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add user' }));
    expect(testState.createUser).toHaveBeenCalledWith(
      { email: 'staff@example.com', role: 'Admin', organizationId: null },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });

  it('shows the organization select to platform admins', () => {
    testState.isPlatformAdmin = true;
    renderModal();
    expect(screen.getByLabelText('Organization')).toBeInTheDocument();
    expect(screen.getByText('Acme')).toBeInTheDocument();
  });

  it('creates the user in the selected organization for platform admins', () => {
    testState.isPlatformAdmin = true;
    renderModal();
    fireEvent.change(screen.getByLabelText('Email Address'), { target: { value: 'owner@example.com' } });
    fireEvent.click(screen.getByRole('combobox', { name: 'Organization' }));
    fireEvent.click(screen.getByRole('option', { name: 'Other' }));
    fireEvent.click(screen.getByRole('button', { name: 'Add user' }));
    expect(testState.createUser).toHaveBeenCalledWith(
      { email: 'owner@example.com', role: 'Admin', organizationId: 'org-2' },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });
});
