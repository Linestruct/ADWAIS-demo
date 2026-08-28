// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import type { ReactNode } from 'react';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { MobileNavigationMenu } from './MobileNavigationMenu';

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children }: { children: ReactNode }) => <a>{children}</a>,
}));

vi.mock('../dashboard/NotificationToggleWidget', () => ({
  NotificationToggleWidget: () => <button type="button" aria-label="notifications" />,
}));

vi.mock('./OrgPicker', () => ({
  OrgPicker: ({ className }: { className?: string }) => (
    <div data-testid="org-picker" className={className} />
  ),
}));

describe('MobileNavigationMenu', () => {
  it('keeps the notification toggle and organization picker in a flexible row', () => {
    render(
      <MobileNavigationMenu
        isOpen
        pathname="/financial"
        financialTimeframe="T30"
        fleetTimeframe="T30"
        onClose={vi.fn()}
      />,
    );

    const picker = screen.getByTestId('org-picker');
    const controls = picker.parentElement;

    expect(picker).toHaveClass('min-w-0', 'flex-1');
    expect(controls).toHaveClass('flex', 'w-full', 'min-w-0', 'flex-nowrap', 'gap-2');
    expect(controls).toContainElement(screen.getByRole('button', { name: 'notifications' }));
  });
});