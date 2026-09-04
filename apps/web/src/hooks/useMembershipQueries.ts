// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useQueryClient } from '@tanstack/react-query';
import {
  useGetApiUsersIdMemberships,
  usePostApiUsersIdMemberships,
  useDeleteApiUsersIdMembershipsMembershipId,
  useGetApiOrganizations,
} from '../api/generated/endpoints';
import type { AddUserMembershipRequestDto, OrganizationSummaryDto, UserMembershipResponseDto } from '@types';
import { toast } from 'sonner';

export function useMembershipsQuery(userId: string) {
  return useGetApiUsersIdMemberships<UserMembershipResponseDto[], Error>(userId, {
    query: {
      queryKey: ['user-memberships', userId],
      select: (res) => res.data,
      retry: false,
    },
  });
}

export function useOrganizationsForPickerQuery() {
  return useGetApiOrganizations<OrganizationSummaryDto[], Error>(undefined, {
    query: {
      queryKey: ['organizations'],
      select: (res) => res.data as OrganizationSummaryDto[],
      retry: false,
    },
  });
}

export function useAddMembershipMutation(userId: string) {
  const queryClient = useQueryClient();
  const mutation = usePostApiUsersIdMemberships<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Membership added.');
        queryClient.invalidateQueries({ queryKey: ['user-memberships', userId] });
      },
      onError: (err: Error) => {
        toast.error('Failed to add membership', {
          description: err.message || String(err),
          duration: Infinity,
        });
      },
    },
  });

  return {
    ...mutation,
    mutate: (data: AddUserMembershipRequestDto) => mutation.mutate({ id: userId, data }),
  };
}

export function useRemoveMembershipMutation(userId: string) {
  const queryClient = useQueryClient();
  const mutation = useDeleteApiUsersIdMembershipsMembershipId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Membership removed.');
        queryClient.invalidateQueries({ queryKey: ['user-memberships', userId] });
      },
      onError: (err: Error) => {
        toast.error('Failed to remove membership', {
          description: err.message || String(err),
          duration: Infinity,
        });
      },
    },
  });

  return {
    ...mutation,
    mutate: (membershipId: string) => mutation.mutate({ id: userId, membershipId }),
  };
}