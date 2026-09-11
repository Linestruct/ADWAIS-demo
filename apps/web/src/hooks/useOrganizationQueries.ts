// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQueryClient } from '@tanstack/react-query';
import {
  useDeleteApiOrganizationsId,
  useGetApiOrganizations,
  usePatchApiOrganizationsId,
  usePostApiOrganizations,
  useGetApiOrganizationsIdConfig,
  usePatchApiOrganizationsIdConfig,
} from '../api/generated/endpoints';
import type { OrganizationConfigDto, OrganizationSummaryDto, UpdateOrganizationConfigRequestDto } from '@types';
import { showErrorToast, showSuccessToast } from '../components/common/ui/toastNotifications';

function invalidateOrganizationQueries(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: ['organization-summaries'] });
  queryClient.invalidateQueries({ queryKey: ['organizations'] });
  queryClient.invalidateQueries({ queryKey: ['/api/organizations'] });
}

export function useOrganizationSummariesQuery() {
  return useGetApiOrganizations<OrganizationSummaryDto[], Error>(undefined, {
    query: {
      queryKey: ['organization-summaries'],
      select: (res) => res.data as OrganizationSummaryDto[],
    },
  });
}

export function useCreateOrganizationMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  return usePostApiOrganizations<Error>({
    mutation: {
      onSuccess: () => {
        showSuccessToast('Organization created.');
        invalidateOrganizationQueries(queryClient);
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        showErrorToast('Failed to create organization', {
          description: err.message || String(err),
          duration: Infinity,
        });
      },
    },
  });
}

export function useRenameOrganizationMutation() {
  const queryClient = useQueryClient();
  return usePatchApiOrganizationsId<Error>({
    mutation: {
      onSuccess: () => {
        showSuccessToast('Organization renamed.');
        invalidateOrganizationQueries(queryClient);
      },
      onError: (err: Error) => {
        showErrorToast('Failed to rename organization', {
          description: err.message || String(err),
          duration: Infinity,
        });
      },
    },
  });
}

export function useDeleteOrganizationMutation() {
  const queryClient = useQueryClient();
  return useDeleteApiOrganizationsId<Error>({
    mutation: {
      onSuccess: () => {
        showSuccessToast('Organization deleted.');
        invalidateOrganizationQueries(queryClient);
      },
      onError: (err: Error) => {
        showErrorToast('Failed to delete organization', {
          description: err.message || String(err),
          duration: Infinity,
        });
      },
    },
  });
}

export function useOrganizationConfigQuery(orgId: string | null) {
  return useGetApiOrganizationsIdConfig<OrganizationConfigDto, Error>(orgId ?? '', {
    query: {
      queryKey: ['organization-config', orgId],
      select: (res) => res.data,
      enabled: orgId !== null,
      retry: false,
    },
  });
}

export function useUpdateOrganizationConfigMutation(orgId: string | null) {
  const queryClient = useQueryClient();
  const mutation = usePatchApiOrganizationsIdConfig<Error>({
    mutation: {
      onSuccess: () => {
        showSuccessToast('Organization configuration updated.');
        queryClient.invalidateQueries({ queryKey: ['organization-config', orgId] });
        queryClient.invalidateQueries({ queryKey: ['job-recurring'] });
      },
      onError: (err: Error) => {
        showErrorToast('Failed to update organization configuration', {
          description: err.message || String(err),
          duration: Infinity,
        });
      },
    },
  });

  return {
    ...mutation,
    mutate: (payload: UpdateOrganizationConfigRequestDto) => {
      if (orgId) mutation.mutate({ id: orgId, data: payload });
    },
    mutateAsync: async (payload: UpdateOrganizationConfigRequestDto) => {
      if (orgId) await mutation.mutateAsync({ id: orgId, data: payload });
    },
  };
}
