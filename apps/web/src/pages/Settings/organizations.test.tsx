// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { OrganizationSummaryDto } from '@types';
import { OrganizationsView } from './organizations';

const testState = vi.hoisted(() => ({
  isPlatformAdmin: true,
  organizations: undefined as OrganizationSummaryDto[] | undefined,
  isLoading: false,
  isError: false,
  createOrganization: vi.fn(),
  renameOrganization: vi.fn(),
  deleteOrganization: vi.fn(),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({
    role: testState.isPlatformAdmin ? 'PlatformAdmin' : 'Admin',
    user: testState.isPlatformAdmin ? { isPlatformAdmin: true } : { isPlatformAdmin: false },
  }),
}));

vi.mock('../../hooks/useOrganizationQueries', () => ({
  useOrganizationSummariesQuery: () => ({
    data: testState.organizations,
    isLoading: testState.isLoading,
    isError: testState.isError,
  }),
  useCreateOrganizationMutation: () => ({
    mutate: testState.createOrganization,
    isPending: false,
  }),
  useRenameOrganizationMutation: () => ({
    mutate: testState.renameOrganization,
    isPending: false,
  }),
  useDeleteOrganizationMutation: () => ({
    mutate: testState.deleteOrganization,
    isPending: false,
  }),
}));

const twoOrgs: OrganizationSummaryDto[] = [
  { id: 'org-1', name: 'Acme', memberCount: 2, monitorCount: 5 },
  { id: 'org-2', name: 'Other', memberCount: 0, monitorCount: 0 },
];

describe('organizations page', () => {
  beforeEach(() => {
    testState.isPlatformAdmin = true;
    testState.organizations = undefined;
    testState.isLoading = false;
    testState.isError = false;
    testState.createOrganization.mockReset();
    testState.renameOrganization.mockReset();
    testState.deleteOrganization.mockReset();
  });

  it('hides the page from non-platform users', () => {
    testState.isPlatformAdmin = false;
    render(<OrganizationsView />);
    expect(screen.getByText('Platform administrators only.')).toBeInTheDocument();
  });

  it('lists organizations with member and monitor counts', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsView />);
    expect(screen.getByText('Acme')).toBeInTheDocument();
    expect(screen.getByText('Other')).toBeInTheDocument();
  });

  it('creates an organization from the inline form', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsView />);
    fireEvent.click(screen.getByRole('button', { name: 'Add organization' }));
    fireEvent.change(screen.getByLabelText('New organization name'), { target: { value: 'NewCo' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create' }));
    expect(testState.createOrganization).toHaveBeenCalledWith({ data: { name: 'NewCo' } });
  });

  it('renames an organization inline', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsView />);
    fireEvent.click(screen.getByRole('button', { name: 'Rename Acme' }));
    fireEvent.change(screen.getByLabelText('Rename Acme'), { target: { value: 'Renamed' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(testState.renameOrganization).toHaveBeenCalledWith(
      { id: 'org-1', data: { name: 'Renamed' } },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });

  it('confirms before deleting an organization', () => {
    testState.organizations = twoOrgs;
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    render(<OrganizationsView />);
    fireEvent.click(screen.getByRole('button', { name: 'Delete Acme' }));
    expect(confirmSpy).toHaveBeenCalledOnce();
    expect(testState.deleteOrganization).toHaveBeenCalledWith({ id: 'org-1' });
    confirmSpy.mockRestore();
  });

  it('skips deletion when the confirmation is dismissed', () => {
    testState.organizations = twoOrgs;
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    render(<OrganizationsView />);
    fireEvent.click(screen.getByRole('button', { name: 'Delete Acme' }));
    expect(testState.deleteOrganization).not.toHaveBeenCalled();
    confirmSpy.mockRestore();
  });
});
