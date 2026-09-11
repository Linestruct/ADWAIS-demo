// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQueryClient } from '@tanstack/react-query';
import {
  useGetApiUsersIdMemberships,
  usePostApiUsersIdMemberships,
  useDeleteApiUsersIdMembershipsMembershipId,
  useGetApiOrganizations,
} from '../api/generated/endpoints';
import type { AddUserMembershipRequestDto, OrganizationSummaryDto, UserMembershipResponseDto } from '@types';
import { showErrorToast, showSuccessToast } from '../components/common/ui/toastNotifications';

export function useMembershipsQuery(userId: string) {
  return useGetApiUsersIdMemberships<UserMembershipResponseDto[], Error>(userId, {
    query: {
      queryKey: ['user-memberships', userId],
      select: (res) => res.data,
      retry: false,
    },
  });
}

export function useOrganizationsForPickerQuery({ enabled = true }: { enabled?: boolean } = {}) {
  return useGetApiOrganizations<OrganizationSummaryDto[], Error>(undefined, {
    query: {
      queryKey: ['organizations'],
      enabled,
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
        showSuccessToast('Membership added.');
        queryClient.invalidateQueries({ queryKey: ['user-memberships', userId] });
      },
      onError: (err: Error) => {
        showErrorToast('Failed to add membership', {
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
        showSuccessToast('Membership removed.');
        queryClient.invalidateQueries({ queryKey: ['user-memberships', userId] });
      },
      onError: (err: Error) => {
        showErrorToast('Failed to remove membership', {
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
