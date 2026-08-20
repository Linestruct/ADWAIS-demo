// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { ChangeEvent } from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { Select } from './Select';

describe('Select', () => {
  it('uses a labelled trigger and emits a real select change event', () => {
    let changedValue = '';
    const onChange = vi.fn((event: ChangeEvent<HTMLSelectElement>) => {
      changedValue = event.target.value;
    });

    render(
      <Select label="Period" value="T30" onChange={onChange}>
        <option value="T30">30 days</option>
        <option value="T90">90 days</option>
      </Select>,
    );

    const trigger = screen.getByRole('combobox', { name: 'Period' });
    fireEvent.click(trigger);
    fireEvent.click(screen.getByRole('option', { name: '90 days' }));

    expect(onChange).toHaveBeenCalledOnce();
    expect(onChange.mock.calls[0][0].target).toBeInstanceOf(HTMLSelectElement);
    expect(changedValue).toBe('T90');
  });

  it('connects hint and error text to the control', () => {
    const { rerender } = render(
      <Select label="Tenant" hint="Choose one tenant">
        <option>Example</option>
      </Select>,
    );

    const select = screen.getByRole('combobox', { name: 'Tenant' });
    expect(select).toHaveAccessibleDescription('Choose one tenant');

    rerender(
      <Select label="Tenant" error="A tenant is required">
        <option>Example</option>
      </Select>,
    );

    expect(select).toHaveAccessibleDescription('A tenant is required');
    expect(select).toHaveAttribute('aria-invalid', 'true');
  });

  it('forwards disabled state and supports compact presentation variants', () => {
    render(
      <Select aria-label="Type" variant="plain" size="xs" fullWidth={false} disabled indicator={null}>
        <option>B2B</option>
      </Select>,
    );

    const select = screen.getByRole('combobox', { name: 'Type' });
    expect(select).toBeDisabled();
    expect(select).toHaveClass('h-6', 'w-auto', 'bg-transparent');
  });

  it('renders a padded, rounded menu independently of its trigger width', () => {
    render(
      <Select aria-label="Period" size="sm" fullWidth={false}>
        <option value="T30">30 days</option>
        <option value="T365">365 days</option>
      </Select>,
    );

    fireEvent.click(screen.getByRole('combobox', { name: 'Period' }));

    expect(screen.getByRole('listbox')).toHaveAttribute('data-select-menu');
    expect(screen.getByRole('listbox')).toHaveClass('rounded-2xl', 'p-2');
    expect(screen.getByRole('option', { name: '365 days' })).toHaveClass('rounded-xl', 'px-3', 'py-2.5');
  });
});
