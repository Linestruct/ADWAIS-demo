// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ConfigurationView } from './configuration';
import type { AccessScope } from '../../hooks/useCurrentUser';
import type { OrganizationConfigDto } from '@types';

const state = vi.hoisted(() => ({
  currentUser: { role: 'Admin' as 'Admin' | 'Employee' | 'Viewer' | null, scope: {} as AccessScope },
  orgConfigArgs: [] as (string | null)[],
  selectedOrgId: null as string | null,
  organizations: [] as { id: string; name: string }[],
}));

const orgConfig: OrganizationConfigDto = {
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

vi.mock('../../hooks/useJobSettingsQueries', () => ({
  useGlobalConfigQuery: () => ({ data: undefined }),
  useUpdateConfigMutation: () => ({ mutateAsync: vi.fn() }),
}));

vi.mock('../../hooks/useIntegrationQueries', () => ({
  useMonitoringProviderDescriptorsQuery: () => ({ data: [] }),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => state.currentUser,
}));

vi.mock('../../hooks/useOrgSelection', () => ({
  useOrgSelection: () => ({
    selectedOrgId: state.selectedOrgId,
    setSelectedOrgId: vi.fn(),
    organizations: state.organizations,
  }),
}));

vi.mock('../../hooks/useOrganizationQueries', () => ({
  useOrganizationConfigQuery: (orgId: string | null) => {
    state.orgConfigArgs.push(orgId);
    return { data: orgConfig };
  },
  useUpdateOrganizationConfigMutation: () => ({ mutateAsync: vi.fn() }),
}));

vi.mock('../../components/settings/configuration/CalendarSubscriptionsPanel', () => ({
  CalendarSubscriptionsPanel: () => null,
}));

function scopeFor(overrides: Partial<AccessScope>): AccessScope {
  return {
    isAdmin: true,
    isPlatformAdmin: false,
    organizationId: null,
    organizationName: null,
    tenantId: null,
    ...overrides,
  };
}

describe('ConfigurationView', () => {
  beforeEach(() => {
    state.currentUser = { role: 'Admin', scope: scopeFor({}) };
    state.orgConfigArgs = [];
    state.selectedOrgId = null;
    state.organizations = [{ id: 'org-1', name: 'Acme' }, { id: 'org-2', name: 'Beta' }];
  });

  it('shows only the organization section for staff admins and derives the org from scope', () => {
    state.currentUser = {
      role: 'Admin',
      scope: scopeFor({ organizationId: 'org-1', organizationName: 'Acme' }),
    };

    render(<ConfigurationView />);

    expect(screen.getByRole('heading', { name: 'Organization Configuration' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Global Configuration' })).not.toBeInTheDocument();
    expect(state.orgConfigArgs).toEqual(['org-1']);
  });

  it('shows both sections for platform admins with an organization selected', () => {
    state.currentUser = {
      role: 'Admin',
      scope: scopeFor({ isPlatformAdmin: true, organizationId: 'org-1', organizationName: 'Acme' }),
    };
    state.selectedOrgId = 'org-1';

    render(<ConfigurationView />);

    expect(screen.getByRole('heading', { name: 'Global Configuration' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Organization Configuration' })).toBeInTheDocument();
    expect(state.orgConfigArgs).toEqual(['org-1']);
  });

  it('shows only the platform section while a platform admin is on the platform overview', () => {
    state.currentUser = {
      role: 'Admin',
      scope: scopeFor({ isPlatformAdmin: true, organizationId: 'org-1', organizationName: 'Acme' }),
    };

    render(<ConfigurationView />);

    expect(screen.getByRole('heading', { name: 'Global Configuration' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Organization Configuration' })).not.toBeInTheDocument();
    expect(state.orgConfigArgs).toEqual([null]);
  });

  it('addresses the selected organization when a staff member switches', () => {
    state.currentUser = {
      role: 'Admin',
      scope: scopeFor({ organizationId: 'org-1', organizationName: 'Acme' }),
    };
    state.selectedOrgId = 'org-2';

    render(<ConfigurationView />);

    expect(screen.getByText('Parameters for Beta')).toBeInTheDocument();
    expect(state.orgConfigArgs).toEqual(['org-2']);
  });

  it('renders no sections for a user without an organization', () => {
    state.currentUser = { role: 'Employee', scope: scopeFor({ isAdmin: false }) };

    render(<ConfigurationView />);

    expect(screen.queryByRole('heading', { name: 'Global Configuration' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Organization Configuration' })).not.toBeInTheDocument();
    expect(state.orgConfigArgs).toEqual([null]);
  });

  it('keeps the organization section editable only for admins', () => {
    state.currentUser = {
      role: 'Viewer',
      scope: scopeFor({ isAdmin: false, organizationId: 'org-1', organizationName: 'Acme' }),
    };

    render(<ConfigurationView />);

    expect(screen.getByText('Parameters for Acme')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Edit / })).not.toBeInTheDocument();
  });
});