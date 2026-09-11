// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { useRemoveMembershipMutation } from './useMembershipQueries';

const toast = vi.hoisted(() => ({ success: vi.fn(), custom: vi.fn(), dismiss: vi.fn() }));
vi.mock('sonner', () => ({ toast }));

vi.mock('../api/generated/endpoints', async () => {
  const { useMutation, useQuery } = await vi.importActual<typeof import('@tanstack/react-query')>('@tanstack/react-query');
  return {
    useGetApiUsersIdMemberships: (id: string, options?: { query?: object }) =>
      useQuery({ queryKey: ['memberships', id], queryFn: async () => ({ data: [] }), ...options?.query }),
    useGetApiOrganizations: (options?: { query?: object }) =>
      useQuery({ queryKey: ['organizations'], queryFn: async () => ({ data: [] }), ...options?.query }),
    usePostApiUsersIdMemberships: (options?: { mutation?: object }) =>
      useMutation({ ...options?.mutation, mutationFn: async () => ({}) }),
    useDeleteApiUsersIdMembershipsMembershipId: (options?: { mutation?: object }) =>
      useMutation({
        ...options?.mutation,
        mutationFn: async ({ membershipId }: { membershipId: string }) => {
          if (membershipId === 'last-platform') {
            throw new Error('Cannot remove the last platform admin.');
          }
          return {};
        },
      }),
  };
});

const wrapper = ({ children }: { children: ReactNode }) => {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
};

describe('membership mutations', () => {
  beforeEach(() => {
    toast.success.mockClear();
    toast.custom.mockClear();
  });

  it('surfaces the refusal to remove the last platform-admin row as an error toast', async () => {
    const { result } = renderHook(() => useRemoveMembershipMutation('user-1'), { wrapper });

    result.current.mutate('last-platform');

    await waitFor(() => expect(toast.custom).toHaveBeenCalled());
  });
});
