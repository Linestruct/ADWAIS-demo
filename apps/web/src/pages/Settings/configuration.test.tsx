// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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
  CalendarSubscriptionsPanel: () => <h2>External Calendar Subscriptions</h2>,
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

  it('shows the org config, intervals, and calendar panels for an org user', () => {
    state.currentUser = {
      role: 'Admin',
      scope: scopeFor({ organizationId: 'org-1', organizationName: 'Acme' }),
    };

    render(<ConfigurationView />);

    expect(screen.getByRole('heading', { name: 'Organization Configuration' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Fetch Intervals' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'External Calendar Subscriptions' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Global Configuration' })).not.toBeInTheDocument();
    expect(state.orgConfigArgs).toEqual(['org-1']);
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

  it('shows the selected organization for a platform admin', () => {
    state.currentUser = {
      role: 'Admin',
      scope: scopeFor({ isPlatformAdmin: true, organizationId: 'org-1', organizationName: 'Acme' }),
    };
    state.selectedOrgId = 'org-1';

    render(<ConfigurationView />);

    expect(screen.getByRole('heading', { name: 'Organization Configuration' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Fetch Intervals' })).toBeInTheDocument();
    expect(state.orgConfigArgs).toEqual(['org-1']);
  });
});