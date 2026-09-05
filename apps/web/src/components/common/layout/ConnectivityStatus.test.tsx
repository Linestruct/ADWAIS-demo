// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ConnectivityStatus } from './ConnectivityStatus';

describe('ConnectivityStatus', () => {
  it('renders nothing when both connections are online', () => {
    const { container } = render(<ConnectivityStatus isOnline isBackendOnline />);

    expect(container).toBeEmptyDOMElement();
  });
});