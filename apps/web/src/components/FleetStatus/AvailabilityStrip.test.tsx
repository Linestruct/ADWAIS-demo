// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AvailabilityStrip } from './AvailabilityStrip';

describe('AvailabilityStrip', () => {
  const points = [
    {
      date: '2026-07-20',
      endDate: '2026-07-20',
      uptimePercentage: 100,
      lowestMonitorUptimePercentage: 99.9,
      monitorCount: 2,
      isPartial: false,
    },
    {
      date: '2026-07-21',
      endDate: '2026-07-21',
      uptimePercentage: null,
      lowestMonitorUptimePercentage: null,
      monitorCount: 0,
      isPartial: false,
    },
    {
      date: '2026-07-22',
      endDate: '2026-07-22',
      uptimePercentage: 98.5,
      lowestMonitorUptimePercentage: 97.25,
      monitorCount: 2,
      isPartial: true,
    },
  ];

  it('preserves gaps and exposes each day as an interactive segment', () => {
    render(<AvailabilityStrip points={points} aggregate />);

    expect(screen.getByRole('group', { name: 'Availability over time' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /21 Jul: No data/i })).toBeInTheDocument();
    expect(screen.getByText('98.500%')).toBeInTheDocument();
    expect(screen.getByText(/2 monitors · Worst 97.250%/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /20 Jul: 100.000%/i }));
    expect(screen.getByText('100.000%')).toBeInTheDocument();
    expect(screen.getByText(/Worst 99.900%/i)).toBeInTheDocument();

    fireEvent.mouseLeave(screen.getByRole('group').parentElement!.parentElement!);
    expect(screen.getByText('98.500%')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /20 Jul: 100.000%/i }));
    fireEvent.pointerDown(document.body);
    expect(screen.getByText('98.500%')).toBeInTheDocument();
  });
});
