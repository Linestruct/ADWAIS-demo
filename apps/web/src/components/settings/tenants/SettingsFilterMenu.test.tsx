// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import {
  MonitorSettingsFilterMenu,
  TenantSettingsFilterMenu,
} from './SettingsFilterMenu';

describe('SettingsFilterMenu', () => {
  it('combines tenant filters and sorting in the downward-opening menu', () => {
    const setFilters = vi.fn();
    const setSort = vi.fn();

    render(
      <TenantSettingsFilterMenu
        filters={{ token: 'all', fetch: 'all' }}
        setFilters={setFilters}
        sort="asc"
        setSort={setSort}
      />,
    );

    const trigger = screen.getByRole('button', { name: 'Filters' });
    fireEvent.click(trigger);
    fireEvent.click(screen.getByRole('button', { name: 'Missing' }));
    fireEvent.click(screen.getByRole('button', { name: 'Z–A' }));

    expect(setFilters).toHaveBeenCalledWith({ token: 'missing', fetch: 'all' });
    expect(setSort).toHaveBeenCalledWith('desc');
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('heading', { name: 'Filter tenants' }).parentElement?.parentElement)
      .not.toHaveStyle({ transform: 'translateY(-100%)' });
  });

  it('keeps the monitor menu open while selecting from the portaled tag menu', () => {
    const setFilters = vi.fn();

    render(
      <MonitorSettingsFilterMenu
        filters={{
          assignment: 'all',
          tag: 'all',
          status: 'all',
          type: 'all',
        }}
        setFilters={setFilters}
        tags={['DEV', 'PROD']}
        types={['API', 'Ping']}
        sort="asc"
        setSort={vi.fn()}
      />,
    );

    const trigger = screen.getByRole('button', { name: 'Filters' });
    fireEvent.click(trigger);
    fireEvent.click(screen.getByRole('combobox', { name: 'Tag' }));
    const prod = screen.getByRole('option', { name: 'PROD' });
    fireEvent.pointerDown(prod);
    fireEvent.click(prod);

    expect(setFilters).toHaveBeenCalledWith({
      assignment: 'all',
      tag: 'PROD',
      status: 'all',
      type: 'all',
    });
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });
});
