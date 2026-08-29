// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

const PLATFORM_LABELS: Record<string, string> = {
  'refresh-financial-materialized-views': 'Financial View Refresh',
  'refresh-monitoring-materialized-views': 'Monitoring View Refresh',
  'refresh-stale-materialized-views': 'Stale View Refresh',
  'system-event-cleanup': 'System Event Cleanup',
  'sync-intranet-calendars': 'Calendar Sync',
  'dev-runtime-data-seeder': 'Runtime Data Seeder',
};

const ORG_PREFIX_LABELS: Record<string, string> = {
  'dispatch-order-fetch': 'Order Fetch',
  'dispatch-monitoring-uptime': 'Uptime Fetch',
  'dispatch-monitoring-latency': 'Latency Fetch',
  'sync-monitoring-account-stats': 'User Stats Fetch',
  'sync-monitoring-fleet': 'Fleet Sync',
  'aggregate-intranet-feeds': 'Feed Fetch',
};

export function recurringJobDisplayName(jobId: string): string {
  const platformLabel = PLATFORM_LABELS[jobId];
  if (platformLabel) return platformLabel;

  for (const [prefix, label] of Object.entries(ORG_PREFIX_LABELS)) {
    if (jobId.startsWith(prefix)) return label;
  }

  return jobId;
}

export function platformJobCatalog(): { id: string; label: string }[] {
  return Object.entries(PLATFORM_LABELS).map(([id, label]) => ({ id, label }));
}