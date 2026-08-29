// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ConnectivityStatus } from './ConnectivityStatus';

describe('ConnectivityStatus', () => {
  it('renders the shared backend-offline label at the shared size', () => {
    const { container } = render(
      <ConnectivityStatus isOnline isBackendOnline={false} />,
    );

    expect(screen.getByText('Server Offline')).toBeInTheDocument();
    expect(screen.getByText('Server Offline').parentElement).toHaveClass('h-11');
    expect(container.querySelector('svg')).toHaveAttribute('width', '16');
    expect(container.querySelector('svg')).toHaveAttribute('height', '16');
  });

  it('renders nothing when both connections are online', () => {
    const { container } = render(<ConnectivityStatus isOnline isBackendOnline />);

    expect(container).toBeEmptyDOMElement();
  });
});