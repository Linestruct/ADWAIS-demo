// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { OrganizationSummaryDto } from '@types';
import { OrganizationsSection } from './OrganizationsSection';

const testState = vi.hoisted(() => ({
  isPlatformAdmin: true,
  forceNullUser: false,
  isUserLoading: false,
  organizations: undefined as OrganizationSummaryDto[] | undefined,
  isLoading: false,
  isError: false,
  createOrganization: vi.fn(),
  renameOrganization: vi.fn(),
  deleteOrganization: vi.fn(),
}));

vi.mock('../../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({
    role: testState.isPlatformAdmin ? 'PlatformAdmin' : 'Admin',
    user: testState.forceNullUser
      ? null
      : testState.isPlatformAdmin
        ? { isPlatformAdmin: true }
        : { isPlatformAdmin: false },
    isLoading: testState.isUserLoading,
  }),
}));

vi.mock('../../../hooks/useOrganizationQueries', () => ({
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

describe('organizations section', () => {
  beforeEach(() => {
    testState.isPlatformAdmin = true;
    testState.forceNullUser = false;
    testState.isUserLoading = false;
    testState.organizations = undefined;
    testState.isLoading = false;
    testState.isError = false;
    testState.createOrganization.mockReset();
    testState.renameOrganization.mockReset();
    testState.deleteOrganization.mockReset();
  });

  it('disables write actions for non-platform users', () => {
    testState.isPlatformAdmin = false;
    testState.organizations = twoOrgs;
    render(<OrganizationsSection />);
    expect(screen.getByText('Acme')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add organization' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Rename Acme' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Delete Acme' })).toBeDisabled();
  });

  it('keeps the table shell when the user fails to load', () => {
    testState.forceNullUser = true;
    testState.isError = true;
    render(<OrganizationsSection />);
    expect(screen.getByText('Organizations')).toBeInTheDocument();
    expect(screen.getByText('Unable to load organizations.')).toBeInTheDocument();
  });

  it('shows a skeleton while the user loads', () => {
    testState.isUserLoading = true;
    render(<OrganizationsSection />);
    expect(screen.getByLabelText('Loading organizations')).toBeInTheDocument();
  });

  it('lists organizations with member and monitor counts', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsSection />);
    expect(screen.getByText('Acme')).toBeInTheDocument();
    expect(screen.getByText('Other')).toBeInTheDocument();
  });

  it('creates an organization from the inline form', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsSection />);
    fireEvent.click(screen.getByRole('button', { name: 'Add organization' }));
    fireEvent.change(screen.getByLabelText('New organization name'), { target: { value: 'NewCo' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create' }));
    expect(testState.createOrganization).toHaveBeenCalledWith({ data: { name: 'NewCo' } });
  });

  it('renames an organization inline', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsSection />);
    fireEvent.click(screen.getByRole('button', { name: 'Rename Acme' }));
    fireEvent.change(screen.getByLabelText('Rename Acme'), { target: { value: 'Renamed' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(testState.renameOrganization).toHaveBeenCalledWith(
      { id: 'org-1', data: { name: 'Renamed' } },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });

  it('aborts adding an organization', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsSection />);
    fireEvent.click(screen.getByRole('button', { name: 'Add organization' }));
    fireEvent.change(screen.getByLabelText('New organization name'), { target: { value: 'NewCo' } });
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(testState.createOrganization).not.toHaveBeenCalled();
    expect(screen.queryByLabelText('New organization name')).toBeNull();
  });

  it('aborts a rename without saving', () => {
    testState.organizations = twoOrgs;
    render(<OrganizationsSection />);
    fireEvent.click(screen.getByRole('button', { name: 'Rename Acme' }));
    fireEvent.change(screen.getByLabelText('Rename Acme'), { target: { value: 'Renamed' } });
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(testState.renameOrganization).not.toHaveBeenCalled();
    expect(screen.getByText('Acme')).toBeInTheDocument();
  });

  it('confirms before deleting an organization', () => {
    testState.organizations = twoOrgs;
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    render(<OrganizationsSection />);
    fireEvent.click(screen.getByRole('button', { name: 'Delete Acme' }));
    expect(confirmSpy).toHaveBeenCalledOnce();
    expect(testState.deleteOrganization).toHaveBeenCalledWith({ id: 'org-1' });
    confirmSpy.mockRestore();
  });

  it('skips deletion when the confirmation is dismissed', () => {
    testState.organizations = twoOrgs;
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    render(<OrganizationsSection />);
    fireEvent.click(screen.getByRole('button', { name: 'Delete Acme' }));
    expect(testState.deleteOrganization).not.toHaveBeenCalled();
    confirmSpy.mockRestore();
  });

  it('shows a loading state', () => {
    testState.isLoading = true;
    render(<OrganizationsSection />);
    expect(screen.getByLabelText('Loading organizations')).toBeInTheDocument();
  });

  it('explains a failed organizations query', () => {
    testState.isError = true;
    render(<OrganizationsSection />);
    expect(screen.getByText('Unable to load organizations.')).toBeInTheDocument();
  });
});
