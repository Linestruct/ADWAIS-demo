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
    triggerJob: vi.fn(),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
    useCurrentUser: () => ({ role: testState.role }),
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

describe('background job access', () => {
    beforeEach(() => {
        testState.triggerJob.mockReset();
    });

    it.each([
        ['kiosk', 'Viewer' as const],
        ['demo', 'Viewer' as const],
        ['unauthenticated', null],
    ])('disables materialized refreshes for %s users', (_userType, role) => {
        testState.role = role;
        render(<BackgroundJobsView />);

        const historicRefresh = getJobButton('Refresh Historic Orders');
        expect(historicRefresh).toBeDisabled();
        expect(getJobButton('Refresh Monitoring')).toBeDisabled();
        expect(historicRefresh).toHaveTextContent('Staff');

        fireEvent.click(historicRefresh);
        expect(testState.triggerJob).not.toHaveBeenCalled();
    });

    it('allows staff to refresh materialized views but not run admin jobs', () => {
        testState.role = 'Employee';
        render(<BackgroundJobsView />);

        expect(getJobButton('Monitor Sync')).toBeDisabled();
        const historicRefresh = getJobButton('Refresh Historic Orders');
        expect(historicRefresh).toBeEnabled();
        expect(getJobButton('Refresh Monitoring')).toBeEnabled();

        fireEvent.click(historicRefresh);
        expect(testState.triggerJob).toHaveBeenCalledWith('/api/job/trigger/refresh-historic-order-data');
    });

    it('allows admins to run every manual job', () => {
        testState.role = 'Admin';
        render(<BackgroundJobsView />);

        expect(getJobButton('Monitor Sync')).toBeEnabled();
        const orderSync = getJobButton('Order Sync');
        expect(orderSync).toBeEnabled();
        expect(getJobButton('Refresh Historic Orders')).toBeEnabled();
        expect(getJobButton('Refresh Monitoring')).toBeEnabled();

        fireEvent.click(orderSync);
        expect(testState.triggerJob).toHaveBeenCalledWith('/api/job/trigger/order-sync');
    });
});
