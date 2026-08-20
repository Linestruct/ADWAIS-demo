// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { 
  useGetApiMonitors, 
  useGetApiMonitorsUnassigned, 
  usePostApiMonitors, 
  usePostApiMonitorsIdStart, 
  usePostApiMonitorsIdPause, 
  usePatchApiMonitorsId, 
  usePatchApiMonitorsIdAssignTenantId, 
  usePatchApiMonitorsIdUnassign,
  useDeleteApiMonitorsId
} from '../api/generated/endpoints';
import type { UptimeMonitorDto, ComparisonPeriod, UpdateMonitorRequestDto, Timeframe, ComparisonType } from '@types';
import { toast } from 'sonner';

export function useMonitorsQuery(timeframe?: string, tenantId?: string | null, comparison?: ComparisonPeriod) {
  return useGetApiMonitors<UptimeMonitorDto[], Error>(
    {
      timeframe: timeframe as Timeframe,
      tenantId: tenantId || undefined,
      comparison: comparison as ComparisonType
    },
    {
      query: {
        queryKey: ['monitors', timeframe, tenantId, comparison],
        select: (res) => res.data as UptimeMonitorDto[]
      }
    }
  );
}

export function useUnassignedMonitorsQuery(timeframe?: string, comparison?: ComparisonPeriod) {
  return useGetApiMonitorsUnassigned<UptimeMonitorDto[], Error>(
    {
      timeframe: timeframe as Timeframe,
      comparison: comparison as ComparisonType
    },
    {
      query: {
        queryKey: ['unassigned-monitors', timeframe, comparison],
        select: (res) => res.data as UptimeMonitorDto[]
      }
    }
  );
}

export function useCreateMonitorMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = usePostApiMonitors<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Monitor created successfully.');
        queryClient.invalidateQueries({ queryKey: ['monitors'] });
        queryClient.invalidateQueries({ queryKey: ['unassigned-monitors'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to create monitor', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });
  const { mutate: mutateRequest } = mutation;

  const mutate = useCallback((
      payload: { name: string; url: string; type?: string | null; uptimeSla: number | null; latencyDegradedFloor?: number | null },
      options?: Parameters<typeof mutateRequest>[1]
    ) => 
      mutateRequest({ 
        params: { tenantId: '00000000-0000-0000-0000-000000000001' }, 
        data: payload 
      }, options), [mutateRequest]);

  return {
    ...mutation,
    mutate
  };
}

export function useControlMonitorMutation() {
  const queryClient = useQueryClient();
  
  const startMutation = usePostApiMonitorsIdStart<Error>();
  const pauseMutation = usePostApiMonitorsIdPause<Error>();

  const isPending = startMutation.isPending || pauseMutation.isPending;

  const mutate = (
    { id, action }: { id: number; action: 'start' | 'pause' },
    options?: Parameters<typeof startMutation.mutate>[1]
  ) => {
    const activeMutation = action === 'start' ? startMutation : pauseMutation;
    activeMutation.mutate(
      { id },
      {
        ...options,
        onSuccess: (...args) => {
          toast.success(`Monitor ${action === 'start' ? 'started' : 'paused'} successfully.`);
          queryClient.invalidateQueries({ queryKey: ['monitors'] });
          if (options?.onSuccess) {
            options.onSuccess(...args);
          }
        },
        onError: (...args) => {
          const err = args[0] as Error;
          toast.error('Failed to control monitor', {
            description: err.message || String(err),
            duration: Infinity
          });
          if (options?.onError) {
            options.onError(...args);
          }
        }
      }
    );
  };

  return {
    isPending,
    mutate
  };
}

export function useUpdateMonitorMutation() {
  const queryClient = useQueryClient();
  const mutation = usePatchApiMonitorsId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Monitor updated successfully.');
        queryClient.invalidateQueries({ queryKey: ['monitors'] });
        queryClient.invalidateQueries({ queryKey: ['unassigned-monitors'] });
      }
    }
  });
  const { mutate: mutateRequest } = mutation;

  const mutate = useCallback((
      variables: { id: number; payload: UpdateMonitorRequestDto },
      options?: Parameters<typeof mutateRequest>[1]
    ) =>
      mutateRequest({ id: variables.id, data: variables.payload }, options),
    [mutateRequest]);

  return {
    ...mutation,
    mutate
  };
}

export function useAssignMonitorMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = usePatchApiMonitorsIdAssignTenantId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Monitor assigned successfully.');
        queryClient.invalidateQueries({ queryKey: ['monitors'] });
        queryClient.invalidateQueries({ queryKey: ['unassigned-monitors'] });
        queryClient.invalidateQueries({ queryKey: ['tenants'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to assign monitor', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });
  const { mutate: mutateRequest } = mutation;

  const mutate = useCallback((
      variables: { id: number; tenantId: string },
      options?: Parameters<typeof mutateRequest>[1]
    ) =>
      mutateRequest({ id: variables.id, tenantId: variables.tenantId }, options),
    [mutateRequest]);

  return {
    ...mutation,
    mutate
  };
}

export function useUnassignMonitorMutation() {
  const queryClient = useQueryClient();
  const mutation = usePatchApiMonitorsIdUnassign<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Monitor unassigned successfully.');
        queryClient.invalidateQueries({ queryKey: ['monitors'] });
        queryClient.invalidateQueries({ queryKey: ['unassigned-monitors'] });
        queryClient.invalidateQueries({ queryKey: ['tenants'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to unassign monitor', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });
  const { mutate: mutateRequest } = mutation;

  const mutate = useCallback((id: number, options?: Parameters<typeof mutateRequest>[1]) => 
      mutateRequest({ id }, options),
    [mutateRequest]);

  return {
    ...mutation,
    mutate
  };
}

export function useDeleteMonitorMutation() {
  const queryClient = useQueryClient();
  const mutation = useDeleteApiMonitorsId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Monitor deleted successfully.');
        queryClient.invalidateQueries({ queryKey: ['monitors'] });
        queryClient.invalidateQueries({ queryKey: ['unassigned-monitors'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to delete monitor', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });
  const { mutate: mutateRequest } = mutation;

  const mutate = useCallback((id: number, options?: Parameters<typeof mutateRequest>[1]) => 
      mutateRequest({ id }, options),
    [mutateRequest]);

  return {
    ...mutation,
    mutate
  };
}
