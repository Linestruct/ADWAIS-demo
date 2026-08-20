// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { FormField } from './FormField';
import { InlineEditField } from './InlineEditField';

describe('FormField', () => {
  it('connects labels, help text, and errors to the native control', () => {
    const { rerender } = render(
      <FormField label="Title" helperText="Keep it short" value="" onChange={() => undefined} />,
    );

    const input = screen.getByRole('textbox', { name: 'Title' });
    expect(input).toHaveAccessibleDescription('Keep it short');
    expect(input.parentElement).toHaveClass('bg-surface-container', 'focus-within:bg-primary-container');

    rerender(
      <FormField label="Title" error="A title is required" value="" onChange={() => undefined} />,
    );

    expect(input).toHaveAccessibleDescription('A title is required');
    expect(input).toHaveAttribute('aria-invalid', 'true');
  });

  it('allows an outlined field without changing the shared control API', () => {
    render(
      <FormField label="Endpoint" variant="outlined" value="" onChange={() => undefined} />,
    );

    expect(screen.getByRole('textbox', { name: 'Endpoint' }).parentElement)
      .toHaveClass('border-outline', 'bg-surface');
  });

  it('uses the custom Select while preserving native change events', () => {
    const onChange = vi.fn();
    render(
      <FormField as="select" label="Category" defaultValue="General" onChange={onChange}>
        <option value="General">General</option>
        <option value="Meeting">Meeting</option>
      </FormField>,
    );

    fireEvent.click(screen.getByRole('combobox', { name: 'Category' }));
    fireEvent.click(screen.getByRole('option', { name: 'Meeting' }));

    expect(onChange).toHaveBeenCalled();
    expect(screen.getByRole('combobox', { name: 'Category' })).toHaveTextContent('Meeting');
  });
});

describe('InlineEditField', () => {
  it('edits the whole display field and commits from its save action', async () => {
    const onCommit = vi.fn();
    render(
      <InlineEditField label="Interval" value={60} kind="number" onCommit={onCommit} />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Edit Interval' }));
    const input = screen.getByRole('spinbutton', { name: 'Interval' });
    fireEvent.change(input, { target: { value: '30' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Interval' }));

    await waitFor(() => expect(onCommit).toHaveBeenCalledWith(30));
  });

  it('shows validation feedback and stays in edit mode', () => {
    const onCommit = vi.fn();
    render(
      <InlineEditField
        label="Interval"
        value={60}
        kind="number"
        required
        validate={value => value > 0 ? undefined : 'Enter a value greater than 0.'}
        onCommit={onCommit}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Edit Interval' }));
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Interval' }), { target: { value: '0' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Interval' }));

    expect(screen.getByText('Enter a value greater than 0.')).toBeInTheDocument();
    expect(screen.getByRole('spinbutton', { name: 'Interval' })).toBeInTheDocument();
    expect(onCommit).not.toHaveBeenCalled();
  });

  it('commits checkbox changes immediately', async () => {
    const onCommit = vi.fn();
    render(
      <InlineEditField label="Fetch enabled" value={false} kind="checkbox" onCommit={onCommit} />,
    );

    fireEvent.click(screen.getByRole('checkbox', { name: 'Fetch enabled' }));

    await waitFor(() => expect(onCommit).toHaveBeenCalledWith(true));
  });

  it('uses the explicit clear callback for configured credentials', async () => {
    const onCommit = vi.fn();
    const onClear = vi.fn().mockResolvedValue(undefined);
    render(
      <InlineEditField
        label="API Key"
        value="configured"
        kind="password"
        canClear
        onCommit={onCommit}
        onClear={onClear}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Edit API Key' }));
    fireEvent.click(screen.getByRole('button', { name: 'Clear' }));

    await waitFor(() => expect(onClear).toHaveBeenCalledOnce());
    expect(onCommit).not.toHaveBeenCalled();
  });
});
