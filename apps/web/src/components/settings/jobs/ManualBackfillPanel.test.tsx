// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render, screen } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { ManualBackfillPanel } from './ManualBackfillPanel';

it('keeps every backfill control read-only for non-admins', () => {
  render(
    <ManualBackfillPanel
      tenants={[]}
      isLoading={false}
      isError={false}
      triggerBackfill={{ mutate: vi.fn(), isPending: false }}
      disabled
    />,
  );

  expect(screen.getByText('You can review this form, but only administrators can run a backfill.')).toBeVisible();
  expect(screen.getByRole('button', { name: 'Start Date date' })).toBeDisabled();
  expect(screen.getByRole('button', { name: 'End Date date' })).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Execute Backfill' })).toBeDisabled();
});
