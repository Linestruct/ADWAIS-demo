// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ChartPanel } from './ChartPanel';

describe('ChartPanel', () => {
  it('shows unavailable instead of the empty chart content when the request fails', () => {
    render(<ChartPanel title="Revenue Performance" isError>Chart content</ChartPanel>);

    expect(screen.getByRole('alert')).toHaveTextContent('Revenue Performance unavailable');
    expect(screen.queryByText('Chart content')).not.toBeInTheDocument();
  });
});
