// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { OrgPicker } from './OrgPicker';
import type { AccessScope } from '../../../hooks/useCurrentUser';
import type { OrganizationResponseDto } from '@types';

const state = vi.hoisted(() => ({
  selectedOrgId: null as string | null,
  setSelectedOrgId: vi.fn(),
  orgs: [] as OrganizationResponseDto[],
  scope: {} as AccessScope,
  isMutating: 0,
}));

vi.mock('../../../hooks/useOrgSelection', () => ({
  useOrgSelection: () => ({
    selectedOrgId: state.selectedOrgId,
    setSelectedOrgId: state.setSelectedOrgId,
    organizations: state.orgs,
  }),
}));

vi.mock('../../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ scope: state.scope }),
}));

vi.mock('@tanstack/react-query', async (importOriginal) => ({
  ...await importOriginal<typeof import('@tanstack/react-query')>(),
  useIsMutating: () => state.isMutating,
}));

function platformScope(): AccessScope {
  return {
    isAdmin: true,
    isPlatformAdmin: false,
    organizationId: 'org-1',
    organizationName: 'Acme',
    tenantId: null,
  };
}

describe('OrgPicker', () => {
  beforeEach(() => {
    state.selectedOrgId = null;
    state.orgs = [{ id: 'org-1', name: 'Acme' }, { id: 'org-2', name: 'Beta' }];
    state.scope = platformScope();
    state.isMutating = 0;
    state.setSelectedOrgId.mockReset();
  });

  it('renders nothing for a single-organization user', () => {
    state.orgs = [{ id: 'org-1', name: 'Acme' }];

    render(<OrgPicker />);

    expect(screen.queryByRole('combobox', { name: 'Active organization' })).not.toBeInTheDocument();
  });

  it('lists only reachable organizations for staff', () => {
    render(<OrgPicker />);

    fireEvent.click(screen.getByRole('combobox', { name: 'Active organization' }));

    expect(screen.getByRole('option', { name: 'Acme' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Beta' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: /Platform overview/ })).not.toBeInTheDocument();
  });

  it('adds a platform overview entry for platform admins', () => {
    state.scope = { ...platformScope(), isPlatformAdmin: true };

    render(<OrgPicker />);

    fireEvent.click(screen.getByRole('combobox', { name: 'Active organization' }));

    expect(screen.getByRole('option', { name: 'Platform overview' })).toBeInTheDocument();
  });

  it('switches to the selected organization', () => {
    render(<OrgPicker />);

    fireEvent.click(screen.getByRole('combobox', { name: 'Active organization' }));
    fireEvent.click(screen.getByRole('option', { name: 'Beta' }));

    expect(state.setSelectedOrgId).toHaveBeenCalledWith('org-2');
  });

  it('switches to the platform overview when a platform admin picks it', () => {
    state.scope = { ...platformScope(), isPlatformAdmin: true };
    state.selectedOrgId = 'org-1';

    render(<OrgPicker />);

    fireEvent.click(screen.getByRole('combobox', { name: 'Active organization' }));
    fireEvent.click(screen.getByRole('option', { name: 'Platform overview' }));

    expect(state.setSelectedOrgId).toHaveBeenCalledWith(null);
  });

  it('disables while a mutation is pending', () => {
    state.isMutating = 1;

    render(<OrgPicker />);

    expect(screen.getByRole('combobox', { name: 'Active organization' })).toBeDisabled();
  });

  it('applies layout classes to its outer wrapper', () => {
    render(<OrgPicker className="min-w-0 flex-1" />);

    const trigger = screen.getByRole('combobox', { name: 'Active organization' });
    expect(trigger.parentElement?.parentElement).toHaveClass('min-w-0', 'flex-1');
  });
});