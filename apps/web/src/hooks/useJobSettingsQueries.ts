// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQueryClient, useMutation } from '@tanstack/react-query';
import {
  useGetApiGlobalConfig,
  useGetApiJobRecurring,
  usePatchApiGlobalConfig,
  useGetApiGlobalConfigIntervals,
  usePatchApiGlobalConfigIntervals,
  usePostApiIngestionBackfill
} from '../api/generated/endpoints';
import { customClient } from '../apiClient';
import { useDiagnosticRunsQuery } from './useDiagnosticsQueries';
import { useOrgSelection } from './useOrgSelection';
import type { GlobalConfigDto, RecurringJobDto, BackgroundJobStatusDto, UpdateGlobalConfigRequestDto } from '@types';
import { toast } from 'sonner';
import { getKioskToken } from '../utils/auth';
import { useCurrentUser } from './useCurrentUser';

export function useGlobalConfigQuery() {
  return useGetApiGlobalConfig<GlobalConfigDto, Error>({
    query: {
      queryKey: ['global-config'],
      select: (res) => res.data,
      retry: false
    }
  });
}

export function useRecurringJobsQuery() {
  const { selectedOrgId } = useOrgSelection();
  return useGetApiJobRecurring<RecurringJobDto[], Error>({
    query: {
      queryKey: ['job-recurring', selectedOrgId],
      select: (res) => (res as unknown as { data: RecurringJobDto[] }).data
    }
  });
}

export function useRecentJobsQuery() {
  const { selectedOrgId } = useOrgSelection();
  const { user } = useCurrentUser();
  const organizationId = selectedOrgId ?? user?.organizationId ?? null;
  const platformScopeActive = user?.isPlatformAdmin === true && selectedOrgId === null;

  const runsQuery = useDiagnosticRunsQuery({
    organizationId,
    platformScopeActive,
    enabled: !getKioskToken() && (platformScopeActive || organizationId !== null),
  });

  return {
    ...runsQuery,
    data: runsQuery.data?.map((run): BackgroundJobStatusDto => {
      const started = run.startedAt ? new Date(run.startedAt).getTime() : null;
      const isActive = run.state === 'Pending' || run.state === 'Queued' || run.state === 'Running';
      return {
        jobId: run.id ?? '',
        jobName: run.resourceName ?? run.kind ?? 'Pipeline',
        jobArgs: null,
        state: isActive ? 'Processing' : run.state ?? 'Unknown',
        createdAt: run.requestedAt ?? null,
        durationSeconds: started === null || !run.completedAt
          ? null
          : Math.max(0, (new Date(run.completedAt).getTime() - started) / 1000),
        exceptionMessage: run.state === 'Failed' ? run.safeSummary ?? null : null,
        tenantName: run.tenantName ?? null,
        monitorName: run.kind === 'MonitorSync' ? run.resourceName ?? null : null,
      };
    }),
  };
}

export function useTriggerJobMutation() {
  return useMutation<unknown, Error, string>({
    mutationFn: (endpoint: string) => customClient<unknown>(endpoint, { method: 'POST' }),
    onSuccess: () => {
      toast.success('Job triggered successfully.');
    },
    onError: (err: Error) => {
        toast.error('Failed to trigger job', {
          description: err.message || String(err),
          duration: Infinity
      });
    }
  });
}

export function useBackfillMutation(onSuccessCallback?: () => void) {
  const mutation = usePostApiIngestionBackfill<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Backfill initiated.');
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Backfill failed', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (payload: { tenantId: string; startDate: string; endDate: string }) =>
      mutation.mutate({
        params: {
          TenantId: payload.tenantId,
          StartDate: payload.startDate ? new Date(payload.startDate).toISOString() : undefined,
          EndDate: payload.endDate ? new Date(payload.endDate).toISOString() : undefined
        }
      })
  };
}

export function useUpdateConfigMutation() {
  const queryClient = useQueryClient();
  const mutation = usePatchApiGlobalConfig<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Configuration updated.');
        queryClient.invalidateQueries({ queryKey: ['global-config'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to update configuration', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (payload: UpdateGlobalConfigRequestDto) =>
      mutation.mutate({ data: payload }),
    mutateAsync: async (payload: UpdateGlobalConfigRequestDto) => {
      await mutation.mutateAsync({ data: payload });
    },
  };
}

export interface FetchIntervalsDto {
  latencyFetchIntervalMinutes: number;
  uptimeFetchIntervalMinutes: number;
  orderFetchIntervalMinutes: number;
  userStatsFetchIntervalMinutes: number;
  feedFetchIntervalHours: number;
}

export function useFetchIntervalsQuery() {
  return useGetApiGlobalConfigIntervals<FetchIntervalsDto, Error>({
    query: {
      queryKey: ['fetch-intervals'],
      select: (res) => res.data as FetchIntervalsDto
    }
  });
}

export function useUpdateFetchIntervalsMutation() {
  const queryClient = useQueryClient();
  const mutation = usePatchApiGlobalConfigIntervals<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Fetch intervals updated.');
        queryClient.invalidateQueries({ queryKey: ['fetch-intervals'] });
        queryClient.invalidateQueries({ queryKey: ['job-recurring'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to update intervals', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (payload: Partial<FetchIntervalsDto>) =>
      mutation.mutate({ data: payload }),
    mutateAsync: async (payload: Partial<FetchIntervalsDto>) => {
      await mutation.mutateAsync({ data: payload });
    },
  };
}
