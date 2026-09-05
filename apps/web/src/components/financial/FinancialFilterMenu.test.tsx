// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { FinancialFilterProps } from './FinancialFilterMenu';
import { FinancialFilterMenu } from './FinancialFilterMenu';

const tenants: FinancialFilterProps['tenants'] = [
  { id: 'a', name: 'Alpha', type: 'B2B' },
  { id: 'b', name: 'Beta', type: 'B2C' },
  { id: 'c', name: 'Combined', type: 'Mixed' },
];

const baseProps: FinancialFilterProps = {
  tenants,
  selectedTenantId: null,
  selectedTypes: [],
  onTenantChange: vi.fn(),
  onTypesChange: vi.fn(),
  onClearAll: vi.fn(),
};

describe('FinancialFilterMenu', () => {
  it('filters tenant choices by the selected business models', () => {
    const onTypesChange = vi.fn();
    const { rerender } = render(
      <FinancialFilterMenu {...baseProps} onTypesChange={onTypesChange} />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Filters' }));
    fireEvent.click(screen.getByRole('button', { name: 'B2B' }));
    expect(onTypesChange).toHaveBeenCalledWith(['B2B']);

    rerender(
      <FinancialFilterMenu {...baseProps} selectedTypes={['B2B']} onTypesChange={onTypesChange} />,
    );
    fireEvent.click(screen.getByRole('combobox', { name: 'Tenant' }));
    expect(screen.getByRole('option', { name: 'Alpha' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Beta' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Combined' })).not.toBeInTheDocument();
  });

  it('disables business models that do not apply to the selected tenant', () => {
    const onTypesChange = vi.fn();
    render(
      <FinancialFilterMenu
        {...baseProps}
        selectedTenantId="a"
        onTypesChange={onTypesChange}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Filters, 1 active' }));
    expect(screen.getByRole('button', { name: 'B2B' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'B2C' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Mixed' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'B2C' }));
    expect(onTypesChange).not.toHaveBeenCalled();
  });

  it('keeps tenant selection inside the open filter menu', () => {
    const onTenantChange = vi.fn();
    render(
      <FinancialFilterMenu {...baseProps} onTenantChange={onTenantChange} />,
    );

    const filters = screen.getByRole('button', { name: 'Filters' });
    fireEvent.click(filters);
    fireEvent.click(screen.getByRole('combobox', { name: 'Tenant' }));
    const alpha = screen.getByRole('option', { name: 'Alpha' });
    fireEvent.pointerDown(alpha);
    fireEvent.click(alpha);
    expect(onTenantChange).toHaveBeenCalledWith('a');
    expect(filters).toHaveAttribute('aria-expanded', 'true');
  });

  it('always renders reset with the correct enabled state', () => {
    const onClearAll = vi.fn();
    const { rerender } = render(
      <FinancialFilterMenu {...baseProps} onClearAll={onClearAll} />,
    );

    expect(screen.getByRole('button', { name: 'Clear all financial filters' })).toBeDisabled();
    rerender(
      <FinancialFilterMenu {...baseProps} selectedTypes={['Mixed']} onClearAll={onClearAll} />,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Clear all financial filters' }));
    expect(onClearAll).toHaveBeenCalledOnce();
  });
});
