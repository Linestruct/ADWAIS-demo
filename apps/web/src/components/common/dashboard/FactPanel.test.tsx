// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { FactPanel } from './FactPanel';

describe('FactPanel', () => {
  it('shows unavailable instead of an empty metric when the request fails', () => {
    render(<FactPanel label="Uptime" isError />);

    expect(screen.getByText('Unavailable')).toBeInTheDocument();
    expect(screen.getByText('Data request failed')).toBeInTheDocument();
  });
});
