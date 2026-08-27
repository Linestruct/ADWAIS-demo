// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useQueryClient } from '@tanstack/react-query';
import { useGetApiOrganizationsIdConfig, usePatchApiOrganizationsIdConfig } from '../api/generated/endpoints';
import type { OrganizationConfigDto, UpdateOrganizationConfigRequestDto } from '@types';
import { toast } from 'sonner';

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
        toast.success('Organization configuration updated.');
        queryClient.invalidateQueries({ queryKey: ['organization-config', orgId] });
        queryClient.invalidateQueries({ queryKey: ['job-recurring'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to update organization configuration', {
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