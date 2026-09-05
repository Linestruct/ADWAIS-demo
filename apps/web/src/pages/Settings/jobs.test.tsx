// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { UserProfile } from '../../hooks/useCurrentUser';
import { BackgroundJobsView } from './jobs';

const testState = vi.hoisted(() => ({
    role: null as UserProfile['role'] | null,
    isPlatformAdmin: false,
    selectedOrgId: null as string | null,
    triggerJob: vi.fn(),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
    useCurrentUser: () => ({
        role: testState.role,
        user: testState.isPlatformAdmin ? { isPlatformAdmin: true } : null,
    }),
}));

vi.mock('../../hooks/useOrgSelection', () => ({
    useOrgSelection: () => ({ selectedOrgId: testState.selectedOrgId }),
}));

vi.mock('../../hooks/useJobSettingsQueries', () => ({
    useRecurringJobsQuery: () => ({ data: [] }),
    useRecentJobsQuery: () => ({ data: [] }),
    useTriggerJobMutation: () => ({ mutate: testState.triggerJob }),
    useBackfillMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));

vi.mock('../../hooks/useTenantQueries', () => ({
    useTenantsQuery: () => ({ data: [] }),
}));

vi.mock('../../components/settings/jobs/RecurringJobsTable', () => ({
    RecurringJobsTable: () => null,
}));

vi.mock('../../components/settings/jobs/ManualBackfillPanel', () => ({
    ManualBackfillPanel: () => null,
}));

function getJobButton(name: string) {
    return screen.getByRole('button', { name: new RegExp(name) });
}

function expectRefreshRowsHidden() {
    expect(screen.queryByRole('button', { name: /Refresh Historic Orders/ })).toBeNull();
    expect(screen.queryByRole('button', { name: /Refresh Monitoring/ })).toBeNull();
}

describe('background job access', () => {
    beforeEach(() => {
        testState.triggerJob.mockReset();
        testState.isPlatformAdmin = false;
        testState.selectedOrgId = null;
    });

    it.each([
        ['kiosk', 'Viewer' as const],
        ['demo', 'Viewer' as const],
        ['unauthenticated', null],
    ])('hides materialized refreshes for %s users', (_userType, role) => {
        testState.role = role;
        render(<BackgroundJobsView />);

        expectRefreshRowsHidden();
        expect(getJobButton('Monitor Sync')).toBeDisabled();
    });

    it('hides materialized refreshes for staff and disables admin jobs', () => {
        testState.role = 'Employee';
        render(<BackgroundJobsView />);

        expectRefreshRowsHidden();
        expect(getJobButton('Monitor Sync')).toBeDisabled();
    });

    it('hides materialized refreshes for org admins', () => {
        testState.role = 'Admin';
        render(<BackgroundJobsView />);

        expectRefreshRowsHidden();
        expect(getJobButton('Monitor Sync')).toBeEnabled();
        expect(getJobButton('Order Sync')).toBeEnabled();
    });

    it('shows materialized refreshes for platform admins in platform view', () => {
        testState.role = 'PlatformAdmin';
        testState.isPlatformAdmin = true;
        render(<BackgroundJobsView />);

        const historicRefresh = getJobButton('Refresh Historic Orders');
        expect(historicRefresh).toBeEnabled();
        expect(getJobButton('Refresh Monitoring')).toBeEnabled();

        fireEvent.click(historicRefresh);
        expect(testState.triggerJob).toHaveBeenCalledWith('/api/job/trigger/refresh-historic-order-data');
    });

    it('hides materialized refreshes for platform admins viewing an org', () => {
        testState.role = 'PlatformAdmin';
        testState.isPlatformAdmin = true;
        testState.selectedOrgId = 'org-1';
        render(<BackgroundJobsView />);

        expectRefreshRowsHidden();
        expect(getJobButton('Monitor Sync')).toBeEnabled();
    });
});