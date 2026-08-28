// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { PlatformConfigurationView } from './platform';
import type { GlobalConfigDto } from '@types';

vi.mock('../../hooks/useJobSettingsQueries', () => ({
  useGlobalConfigQuery: () => ({ data: { systemEventRetentionDays: 30 } as GlobalConfigDto }),
  useUpdateConfigMutation: () => ({ mutateAsync: vi.fn() }),
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => ({ role: 'Admin' }),
}));

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

describe('PlatformConfigurationView', () => {
  it('shows the deployment-wide configuration', () => {
    render(<PlatformConfigurationView />, { wrapper });

    expect(screen.getByRole('heading', { name: 'Global Configuration' })).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Organization Configuration' })).not.toBeInTheDocument();
  });
});