// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { RoleBoundary } from './RoleBoundary';

const testState = vi.hoisted(() => ({
  role: null as string | null,
}));

vi.mock('../../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ role: testState.role, isLoading: false }),
}));

describe('RoleBoundary', () => {
  it('allows the platform admin role through viewer boundaries', () => {
    testState.role = 'PlatformAdmin';
    render(<RoleBoundary requiredRole="Viewer">content</RoleBoundary>);

    expect(screen.getByText('content')).toBeInTheDocument();
  });

  it('allows the platform admin role through admin boundaries', () => {
    testState.role = 'PlatformAdmin';
    render(<RoleBoundary requiredRole="Admin">content</RoleBoundary>);

    expect(screen.getByText('content')).toBeInTheDocument();
  });

  it('denies a viewer for admin boundaries', () => {
    testState.role = 'Viewer';
    render(<RoleBoundary requiredRole="Admin">content</RoleBoundary>);

    expect(screen.queryByText('content')).not.toBeInTheDocument();
    expect(screen.getByText('Requires Admin role.')).toBeInTheDocument();
  });

  it('denies unauthenticated users for viewer boundaries', () => {
    testState.role = null;
    render(<RoleBoundary requiredRole="Viewer">content</RoleBoundary>);

    expect(screen.queryByText('content')).not.toBeInTheDocument();
  });
});