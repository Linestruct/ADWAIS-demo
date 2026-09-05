// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
