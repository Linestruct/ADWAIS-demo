// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ConnectivityStatus } from './ConnectivityStatus';

describe('ConnectivityStatus', () => {
  it('renders nothing when both connections are online', () => {
    const { container } = render(<ConnectivityStatus isOnline isBackendOnline />);

    expect(container).toBeEmptyDOMElement();
  });
});