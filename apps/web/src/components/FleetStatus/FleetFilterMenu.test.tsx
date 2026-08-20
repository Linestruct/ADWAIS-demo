// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { FleetFilterMenu, FleetFilterPanel } from './FleetFilterMenu';

const baseProps = {
  availableTags: ['dev', 'prod'],
  includedTags: [],
  excludedTags: [],
  unavailableIncludedTags: [],
  unavailableExcludedTags: [],
  hiddenStatuses: [],
  onIncludedTagsChange: vi.fn(),
  onExcludedTagsChange: vi.fn(),
  onHiddenStatusesChange: vi.fn(),
  onClearActive: vi.fn(),
  onSaveDefault: vi.fn(),
  onRestoreSaved: vi.fn(),
  onForgetSaved: vi.fn(),
  hasSavedPreferences: false,
  hasUnsavedChanges: false,
};

describe('FleetFilterMenu', () => {
  it('treats statuses as visible by default and hides an unchecked status', () => {
    const onHiddenStatusesChange = vi.fn();
    render(
      <FleetFilterPanel
        {...baseProps}
        embedded
        onHiddenStatusesChange={onHiddenStatusesChange}
      />,
    );

    const down = screen.getByRole('button', { name: 'Down' });
    expect(down).toHaveAttribute('aria-pressed', 'true');
    fireEvent.click(down);
    expect(onHiddenStatusesChange).toHaveBeenCalledWith(['DOWN']);
  });

  it('keeps include and exclude tag choices mutually exclusive', () => {
    const onIncludedTagsChange = vi.fn();
    const onExcludedTagsChange = vi.fn();
    render(
      <FleetFilterPanel
        {...baseProps}
        embedded
        includedTags={['prod']}
        onIncludedTagsChange={onIncludedTagsChange}
        onExcludedTagsChange={onExcludedTagsChange}
      />,
    );

    const prodButtons = screen.getAllByRole('button', { name: 'prod' });
    expect(prodButtons[0]).toHaveAttribute('aria-pressed', 'true');
    expect(prodButtons[1]).toHaveAttribute('aria-pressed', 'false');
    fireEvent.click(prodButtons[1]);
    expect(onExcludedTagsChange).toHaveBeenCalledWith(['prod']);
    expect(onIncludedTagsChange).toHaveBeenCalledWith([]);
  });

  it('exposes explicit saved-default actions with useful disabled states', () => {
    const onSaveDefault = vi.fn();
    const onRestoreSaved = vi.fn();
    const onForgetSaved = vi.fn();
    render(
      <FleetFilterPanel
        {...baseProps}
        embedded
        hasSavedPreferences
        hasUnsavedChanges
        onSaveDefault={onSaveDefault}
        onRestoreSaved={onRestoreSaved}
        onForgetSaved={onForgetSaved}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Save as default' }));
    fireEvent.click(screen.getByRole('button', { name: 'Restore saved' }));
    fireEvent.click(screen.getByRole('button', { name: 'Forget default' }));
    expect(onSaveDefault).toHaveBeenCalledOnce();
    expect(onRestoreSaved).toHaveBeenCalledOnce();
    expect(onForgetSaved).toHaveBeenCalledOnce();
  });

  it('shows unavailable tags as removable instead of silently applying them', () => {
    const onIncludedTagsChange = vi.fn();
    render(
      <FleetFilterPanel
        {...baseProps}
        embedded
        includedTags={['retired']}
        unavailableIncludedTags={['retired']}
        onIncludedTagsChange={onIncludedTagsChange}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Remove unavailable tag retired' }));
    expect(onIncludedTagsChange).toHaveBeenCalledWith([]);
  });

  it('counts filter groups only and clears active filters without touching scope', () => {
    const onClearActive = vi.fn();
    const { rerender } = render(
      <FleetFilterMenu {...baseProps} onClearActive={onClearActive} />,
    );

    expect(screen.getByRole('button', { name: 'Clear active fleet filters' })).toBeDisabled();

    rerender(
      <FleetFilterMenu
        {...baseProps}
        includedTags={['prod']}
        excludedTags={['dev']}
        hiddenStatuses={['DOWN']}
        onClearActive={onClearActive}
      />,
    );
    expect(screen.getByRole('button', { name: 'Filters, 2 active' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear active fleet filters' }));
    expect(onClearActive).toHaveBeenCalledOnce();
  });
});
