// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { MonitorSmartphone, Trash2 } from 'lucide-react';
import { useDeleteKioskDeviceMutation, useKioskDevicesQuery } from '../../../hooks/useKioskAuth';
import { Button } from '../../common/ui/Button';
import { EmptyState } from '../../common/ui/EmptyState';
import { TableSkeletonRows } from '../../common/ui/TableSkeletonRows';

function formatDateTime(value: string | null | undefined): string {
  if (!value) return 'Never';
  return new Date(value).toLocaleString();
}

export function KioskDevicesPanel() {
  const { data: devices, isLoading, isError } = useKioskDevicesQuery();
  const deleteDevice = useDeleteKioskDeviceMutation();

  const handleDelete = (deviceId: string | null) => {
    if (!deviceId) return;
    if (confirm(`Remove kiosk display "${deviceId}"? It returns to the activation screen.`)) {
      deleteDevice.mutate({ deviceId });
    }
  };

  return (
    <div className="space-y-4">
      <h3 className="flex items-center gap-2 text-lg font-bold text-on-surface">
        <MonitorSmartphone size={20} /> Kiosk displays
      </h3>
      <p className="text-sm text-on-surface-variant">
        Displays authorized for this organization. Removing one returns it to the activation screen.
      </p>

      <div className="border border-outline-variant rounded-xl overflow-hidden bg-surface custom-scrollbar overflow-auto">
        <table className="w-full whitespace-nowrap text-left text-sm">
          <thead className="sticky top-0 z-10 border-b border-outline-variant bg-surface-container-high text-on-surface-variant">
            <tr>
              <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Device</th>
              <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Last seen</th>
              <th className="px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Authorized</th>
              <th className="w-20 px-4 py-4 sm:px-5"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-outline-variant" aria-busy={isLoading} aria-label={isLoading ? 'Loading kiosk displays' : undefined}>
            {isLoading && <TableSkeletonRows columnCount={4} />}
            {!isLoading && !isError && (devices || []).map((d) => (
              <tr key={d.deviceId}>
                <td className="px-4 py-3 font-mono text-sm sm:px-5">{d.deviceId}</td>
                <td className="px-4 py-3 sm:px-5">{formatDateTime(d.lastSeenAt)}</td>
                <td className="px-4 py-3 sm:px-5">{formatDateTime(d.authorizedAt)}</td>
                <td className="px-4 py-3 sm:px-5">
                  <Button
                    onClick={() => handleDelete(d.deviceId)}
                    disabled={deleteDevice.isPending}
                    variant="text"
                    color="error"
                    icon={<Trash2 size={16} />}
                    aria-label={`Remove ${d.deviceId}`}
                  >
                    Remove
                  </Button>
                </td>
              </tr>
            ))}
            {!isLoading && isError && (
              <tr>
                <td colSpan={4} className="p-0">
                  <div role="alert" className="p-8 text-center text-on-surface-variant">
                    Unable to load kiosk displays.
                  </div>
                </td>
              </tr>
            )}
            {!isLoading && !isError && devices?.length === 0 && (
              <EmptyState message="No kiosk displays authorized." isTableRow colSpan={4} />
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
