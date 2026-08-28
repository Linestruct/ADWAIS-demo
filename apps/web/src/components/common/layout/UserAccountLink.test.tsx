// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import type { ReactNode } from 'react';
import { render } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { UserAccountLink } from './UserAccountLink';

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, className }: { children: ReactNode; className?: string }) => (
    <a className={className}>{children}</a>
  ),
}));

describe('UserAccountLink', () => {
  it('renders a uniform 44px square account control', () => {
    const { container } = render(<UserAccountLink label="Alex" />);

    expect(container.querySelector('a')).toHaveClass('h-11', 'w-11');
  });
});