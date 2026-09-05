// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { KioskDeviceDto } from '@types';
import { KioskDevicesPanel } from './KioskDevicesPanel';

const testState = vi.hoisted(() => ({
  role: 'Admin' as string | null,
  user: { id: 'u' } as { id: string } | null,
  isUserLoading: false,
  devices: undefined as KioskDeviceDto[] | undefined,
  isLoading: false,
  isError: false,
  deleteDevice: vi.fn(),
}));

vi.mock('../../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({
    role: testState.role,
    user: testState.user,
    isLoading: testState.isUserLoading,
  }),
}));

vi.mock('../../../hooks/useKioskAuth', () => ({
  useKioskDevicesQuery: () => ({
    data: testState.devices,
    isLoading: testState.isLoading,
    isError: testState.isError,
  }),
  useDeleteKioskDeviceMutation: () => ({
    mutate: testState.deleteDevice,
    isPending: false,
  }),
}));

const twoDevices: KioskDeviceDto[] = [
  {
    deviceId: 'kiosk-1',
    organizationId: 'org-1',
    isAuthorized: true,
    authorizedAt: '2026-09-01T10:00:00Z',
    lastSeenAt: '2026-09-04T10:00:00Z',
    createdDate: '2026-09-01T09:00:00Z',
  },
  {
    deviceId: 'kiosk-2',
    organizationId: 'org-1',
    isAuthorized: true,
    authorizedAt: '2026-09-02T10:00:00Z',
    lastSeenAt: null,
    createdDate: '2026-09-02T09:00:00Z',
  },
];

describe('kiosk devices panel', () => {
  beforeEach(() => {
    testState.role = 'Admin';
    testState.user = { id: 'u' };
    testState.isUserLoading = false;
    testState.devices = undefined;
    testState.isLoading = false;
    testState.isError = false;
    testState.deleteDevice.mockReset();
  });

  it('disables removal for non-staff roles', () => {
    testState.role = 'Viewer';
    testState.devices = [
      {
        deviceId: 'kiosk-1',
        organizationId: 'org-1',
        isAuthorized: true,
        authorizedAt: '2026-09-01T10:00:00Z',
        lastSeenAt: null,
        createdDate: '2026-09-01T09:00:00Z',
      },
    ];
    render(<KioskDevicesPanel />);
    expect(screen.getByText('kiosk-1')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remove kiosk-1' })).toBeDisabled();
  });

  it('keeps the table shell when the user fails to load', () => {
    testState.role = null;
    testState.user = null;
    testState.isError = true;
    render(<KioskDevicesPanel />);
    expect(screen.getByText('Kiosk displays')).toBeInTheDocument();
    expect(screen.getByText('Unable to load kiosk displays.')).toBeInTheDocument();
  });

  it('shows a loading state', () => {
    testState.isLoading = true;
    render(<KioskDevicesPanel />);
    expect(screen.getByLabelText('Loading kiosk displays')).toBeInTheDocument();
  });

  it('lists devices with last-seen dates', () => {
    testState.devices = twoDevices;
    render(<KioskDevicesPanel />);
    expect(screen.getByText('kiosk-1')).toBeInTheDocument();
    expect(screen.getByText('kiosk-2')).toBeInTheDocument();
    expect(screen.getByText('Never')).toBeInTheDocument();
  });

  it('confirms before removing a display', () => {
    testState.devices = twoDevices;
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    render(<KioskDevicesPanel />);
    fireEvent.click(screen.getByRole('button', { name: 'Remove kiosk-1' }));
    expect(confirmSpy).toHaveBeenCalledOnce();
    expect(testState.deleteDevice).toHaveBeenCalledWith({ deviceId: 'kiosk-1' });
    confirmSpy.mockRestore();
  });

  it('skips removal when the confirmation is dismissed', () => {
    testState.devices = twoDevices;
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    render(<KioskDevicesPanel />);
    fireEvent.click(screen.getByRole('button', { name: 'Remove kiosk-1' }));
    expect(testState.deleteDevice).not.toHaveBeenCalled();
    confirmSpy.mockRestore();
  });
});
