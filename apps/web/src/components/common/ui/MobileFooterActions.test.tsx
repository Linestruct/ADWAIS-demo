// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { MobileFooterActions } from './MobileFooterActions';
import { MobileFooterActionsSlotContext } from './MobileFooterActionsContext';

function renderActions(activeCount: number, onClearAll = vi.fn(), onOpenSettings?: () => void) {
  const panel = document.createElement('div');
  const indicator = document.createElement('span');
  const quickAction = document.createElement('div');
  document.body.append(panel, indicator, quickAction);

  const view = render(
    <MobileFooterActionsSlotContext.Provider value={{ panel, indicator, quickAction }}>
      <MobileFooterActions
        activeCount={activeCount}
        clearLabel="Clear all fleet filters"
        onClearAll={onClearAll}
        settingsAction={onOpenSettings ? { label: 'Monitor settings', onClick: onOpenSettings } : undefined}
      >
        <div>Fleet filters</div>
      </MobileFooterActions>
    </MobileFooterActionsSlotContext.Provider>,
  );

  return {
    ...view,
    onClearAll,
    cleanupSlots: () => {
      panel.remove();
      indicator.remove();
      quickAction.remove();
    },
  };
}

describe('MobileFooterActions', () => {
  it('shows the active count and exposes a one-tap clear action', () => {
    const { onClearAll, cleanupSlots, unmount } = renderActions(2);

    expect(screen.getByText('2 active filters')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear all fleet filters' }));
    expect(onClearAll).toHaveBeenCalledOnce();

    unmount();
    cleanupSlots();
  });

  it('keeps the clear action visible but disabled without active filters', () => {
    const { cleanupSlots, unmount } = renderActions(0);

    expect(screen.queryByText(/active filters/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Clear all fleet filters' })).toBeDisabled();

    unmount();
    cleanupSlots();
  });

  it('renders a full-size settings action in the mobile FAB', () => {
    const onOpenSettings = vi.fn();
    const { cleanupSlots, unmount } = renderActions(0, vi.fn(), onOpenSettings);

    const settings = screen.getByRole('button', { name: 'Monitor settings' });
    expect(settings).toHaveClass('h-14', 'w-14');
    fireEvent.click(settings);
    expect(onOpenSettings).toHaveBeenCalledOnce();

    unmount();
    cleanupSlots();
  });
});
