// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MembershipsPanel } from './MembershipsPanel';
import type { UserMembershipResponseDto } from '@types';

const state = vi.hoisted(() => ({
  memberships: [] as UserMembershipResponseDto[],
  organizations: [] as { id: string; name: string }[],
  add: vi.fn(),
  remove: vi.fn(),
}));

vi.mock('../../../hooks/useMembershipQueries', () => ({
  useMembershipsQuery: () => ({ data: state.memberships }),
  useOrganizationsForPickerQuery: () => ({ data: state.organizations }),
  useAddMembershipMutation: () => ({ mutate: state.add, isPending: false }),
  useRemoveMembershipMutation: () => ({ mutate: state.remove, isPending: false }),
}));

function membership(id: string, overrides: Partial<UserMembershipResponseDto> = {}): UserMembershipResponseDto {
  return {
    id,
    userId: 'user-1',
    organizationId: 'org-1',
    organizationName: 'Acme',
    role: 'Viewer',
    ...overrides,
  };
}

describe('MembershipsPanel', () => {
  beforeEach(() => {
    state.memberships = [
      membership('m-1'),
      membership('m-2', { organizationId: null, organizationName: null, role: 'Admin' }),
    ];
    state.organizations = [{ id: 'org-1', name: 'Acme' }, { id: 'org-2', name: 'Beta' }];
    state.add.mockReset();
    state.remove.mockReset();
  });

  it('renders the server-scoped rows with organization names and roles', () => {
    render(<MembershipsPanel userId="user-1" isAdmin isPlatformAdmin />);

    expect(screen.getAllByText('Acme').length).toBeGreaterThan(0);
    expect(screen.getByRole('button', { name: 'Remove membership m-1' })).toBeInTheDocument();
  });

  it('labels platform rows for null organizations', () => {
    render(<MembershipsPanel userId="user-1" isAdmin isPlatformAdmin />);

    expect(screen.getByText('Platform')).toBeInTheDocument();
  });

  it('adds a membership with the selected organization and role', async () => {
    render(<MembershipsPanel userId="user-1" isAdmin isPlatformAdmin />);

    fireEvent.click(screen.getByRole('combobox', { name: 'Organization' }));
    fireEvent.click(screen.getByRole('option', { name: 'Beta' }));
    fireEvent.click(screen.getByRole('combobox', { name: 'Role' }));
    fireEvent.click(screen.getByRole('option', { name: 'Employee' }));
    fireEvent.click(screen.getByRole('button', { name: 'Add Membership' }));

    await waitFor(() =>
      expect(state.add).toHaveBeenCalledWith({ organizationId: 'org-2', role: 'Employee' }),
    );
  });

  it('offers a platform option to platform admins only', () => {
    const { unmount } = render(<MembershipsPanel userId="user-1" isAdmin isPlatformAdmin />);
    fireEvent.click(screen.getByRole('combobox', { name: 'Organization' }));
    expect(screen.getByRole('option', { name: /Platform \(no organization\)/ })).toBeInTheDocument();
    unmount();

    render(<MembershipsPanel userId="user-1" isAdmin isPlatformAdmin={false} />);
    fireEvent.click(screen.getByRole('combobox', { name: 'Organization' }));
    expect(screen.queryByRole('option', { name: /Platform \(no organization\)/ })).not.toBeInTheDocument();
  });

  it('forces the platform admin role when adding a platform membership', async () => {
    render(<MembershipsPanel userId="user-1" isAdmin isPlatformAdmin />);

    fireEvent.click(screen.getByRole('combobox', { name: 'Organization' }));
    fireEvent.click(screen.getByRole('option', { name: /Platform \(no organization\)/ }));
    fireEvent.click(screen.getByRole('button', { name: 'Add Membership' }));

    await waitFor(() =>
      expect(state.add).toHaveBeenCalledWith({ organizationId: null, role: 'PlatformAdmin' }),
    );
  });

  it('removes a membership row', async () => {
    render(<MembershipsPanel userId="user-1" isAdmin isPlatformAdmin />);

    fireEvent.click(screen.getByRole('button', { name: 'Remove membership m-1' }));

    await waitFor(() => expect(state.remove).toHaveBeenCalledWith('m-1'));
  });

  it('renders read-only for non-admin callers', () => {
    render(<MembershipsPanel userId="user-1" isAdmin={false} isPlatformAdmin={false} />);

    expect(screen.getByText('Acme')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Remove membership/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add Membership' })).not.toBeInTheDocument();
  });
});