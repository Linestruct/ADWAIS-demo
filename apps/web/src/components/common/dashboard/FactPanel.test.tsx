// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { FactPanel } from './FactPanel';

describe('FactPanel', () => {
  it('reserves the extra field with N/A when PoP data is unavailable', () => {
    render(<FactPanel label="Uptime" value="N/A" hasExtra />);

    expect(screen.getAllByText('N/A')).toHaveLength(2);
  });

  it('renders available PoP data instead of the fallback', () => {
    render(<FactPanel label="Uptime" value="99.9%" hasExtra extra={{ type: 'PoP', value: 1.5 }} />);

    expect(screen.getByText('▲ 1.50% PoP')).toBeInTheDocument();
  });

  it('shows unavailable instead of an empty metric when the request fails', () => {
    render(<FactPanel label="Uptime" isError />);

    expect(screen.getByText('Unavailable')).toBeInTheDocument();
    expect(screen.getByText('Data request failed')).toBeInTheDocument();
  });
});
