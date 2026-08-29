// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { describe, expect, it } from 'vitest';
import { recurringJobDisplayName } from './recurringJobLabels';

describe('recurringJobDisplayName', () => {
  it('maps organization job ids to their fetch interval labels', () => {
    expect(recurringJobDisplayName('dispatch-order-fetch-00000000-0000-0000-0000-00000000000a')).toBe('Order Fetch');
    expect(recurringJobDisplayName('dispatch-monitoring-uptime-00000000-0000-0000-0000-00000000000a')).toBe('Uptime Fetch');
    expect(recurringJobDisplayName('dispatch-monitoring-latency-00000000-0000-0000-0000-00000000000a')).toBe('Latency Fetch');
    expect(recurringJobDisplayName('sync-monitoring-account-stats-00000000-0000-0000-0000-00000000000a')).toBe('User Stats Fetch');
    expect(recurringJobDisplayName('sync-monitoring-fleet-00000000-0000-0000-0000-00000000000a')).toBe('Fleet Sync');
    expect(recurringJobDisplayName('aggregate-intranet-feeds-00000000-0000-0000-0000-00000000000a')).toBe('Feed Fetch');
  });

  it('maps curated platform job ids to readable names', () => {
    expect(recurringJobDisplayName('refresh-financial-materialized-views')).toBe('Financial View Refresh');
    expect(recurringJobDisplayName('refresh-monitoring-materialized-views')).toBe('Monitoring View Refresh');
    expect(recurringJobDisplayName('refresh-stale-materialized-views')).toBe('Stale View Refresh');
    expect(recurringJobDisplayName('system-event-cleanup')).toBe('System Event Cleanup');
    expect(recurringJobDisplayName('sync-intranet-calendars')).toBe('Calendar Sync');
  });

  it('falls back to the raw id for unknown jobs', () => {
    expect(recurringJobDisplayName('some-unknown-job')).toBe('some-unknown-job');
  });
});