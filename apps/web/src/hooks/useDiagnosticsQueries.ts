import {
  useGetApiOrganizationsOrganizationIdDiagnosticsEvents,
  useGetApiOrganizationsOrganizationIdDiagnosticsPipelines,
  useGetApiOrganizationsOrganizationIdDiagnosticsRuns,
  useGetApiOrganizationsOrganizationIdDiagnosticsRunsRunId,
  useGetApiPlatformDiagnosticsEvents,
  useGetApiPlatformDiagnosticsHealth,
  useGetApiPlatformDiagnosticsRuns,
  useGetApiPlatformDiagnosticsRunsRunId,
} from '../api/generated/endpoints';
import type {
  DiagnosticEventDto,
  GetApiOrganizationsOrganizationIdDiagnosticsEventsParams,
  GetApiOrganizationsOrganizationIdDiagnosticsRunsParams,
  GetApiPlatformDiagnosticsEventsParams,
  GetApiPlatformDiagnosticsRunsParams,
  OrganizationDiagnosticsDto,
  PipelineRunDto,
  PipelineRunDetailsDto,
  PlatformDiagnosticsDto,
} from '@types';

const REFRESH_INTERVAL = 30_000;

export const diagnosticsKeys = {
  all: ['diagnostics'] as const,
  organization: (organizationId: string | null) => [...diagnosticsKeys.all, 'organization', organizationId] as const,
  platform: () => [...diagnosticsKeys.all, 'platform'] as const,
  organizationRuns: (organizationId: string | null, params: GetApiOrganizationsOrganizationIdDiagnosticsRunsParams) =>
    [...diagnosticsKeys.organization(organizationId), 'runs', params] as const,
  platformRuns: (params: GetApiPlatformDiagnosticsRunsParams) =>
    [...diagnosticsKeys.platform(), 'runs', params] as const,
  organizationEvents: (organizationId: string | null, params: GetApiOrganizationsOrganizationIdDiagnosticsEventsParams) =>
    [...diagnosticsKeys.organization(organizationId), 'events', params] as const,
  platformEvents: (params: GetApiPlatformDiagnosticsEventsParams) =>
    [...diagnosticsKeys.platform(), 'events', params] as const,
};

export function useOrganizationDiagnosticsQuery(organizationId: string | null, enabled = organizationId !== null) {
  return useGetApiOrganizationsOrganizationIdDiagnosticsPipelines<OrganizationDiagnosticsDto, Error>(organizationId ?? '', {
    query: {
      queryKey: diagnosticsKeys.organization(organizationId),
      enabled: enabled && organizationId !== null,
      refetchInterval: REFRESH_INTERVAL,
      select: (response) => response.data,
    },
  });
}

export function usePlatformDiagnosticsQuery(enabled: boolean) {
  return useGetApiPlatformDiagnosticsHealth<PlatformDiagnosticsDto, Error>({
    query: {
      queryKey: diagnosticsKeys.platform(),
      enabled,
      refetchInterval: REFRESH_INTERVAL,
      select: (response) => response.data,
    },
  });
}

export function useOrganizationDiagnosticRunsQuery(
  organizationId: string | null,
  params: GetApiOrganizationsOrganizationIdDiagnosticsRunsParams = { take: 100 },
  enabled = organizationId !== null,
) {
  return useGetApiOrganizationsOrganizationIdDiagnosticsRuns<PipelineRunDto[], Error>(organizationId ?? '', params, {
    query: {
      queryKey: diagnosticsKeys.organizationRuns(organizationId, params),
      enabled: enabled && organizationId !== null,
      refetchInterval: REFRESH_INTERVAL,
      select: (response) => response.data,
    },
  });
}

export function usePlatformDiagnosticRunsQuery(
  params: GetApiPlatformDiagnosticsRunsParams = { take: 100 },
  enabled = true,
) {
  return useGetApiPlatformDiagnosticsRuns<PipelineRunDto[], Error>(params, {
    query: {
      queryKey: diagnosticsKeys.platformRuns(params),
      enabled,
      refetchInterval: REFRESH_INTERVAL,
      select: (response) => response.data,
    },
  });
}

export function useOrganizationDiagnosticRunQuery(
  organizationId: string | null,
  runId: string | null,
  enabled = organizationId !== null && runId !== null,
) {
  return useGetApiOrganizationsOrganizationIdDiagnosticsRunsRunId<PipelineRunDetailsDto, Error>(
    organizationId ?? '',
    runId ?? '',
    {
      query: {
        queryKey: [...diagnosticsKeys.organization(organizationId), 'run', runId],
        enabled: enabled && organizationId !== null && runId !== null,
        select: (response) => response.data,
      },
    },
  );
}

export function usePlatformDiagnosticRunQuery(
  runId: string | null,
  enabled = runId !== null,
) {
  return useGetApiPlatformDiagnosticsRunsRunId<PipelineRunDetailsDto, Error>(runId ?? '', {
    query: {
      queryKey: [...diagnosticsKeys.platform(), 'run', runId],
      enabled: enabled && runId !== null,
      select: (response) => response.data,
    },
  });
}

export function useDiagnosticRunsQuery({
  organizationId,
  platformScopeActive,
  take = 100,
  enabled = true,
}: {
  organizationId: string | null;
  platformScopeActive: boolean;
  take?: number;
  enabled?: boolean;
}) {
  const params = { take };
  const organizationQuery = useOrganizationDiagnosticRunsQuery(organizationId, params, enabled && !platformScopeActive && organizationId !== null);
  const platformQuery = usePlatformDiagnosticRunsQuery(params, enabled && platformScopeActive);
  return platformScopeActive ? platformQuery : organizationQuery;
}

export function useOrganizationDiagnosticEventsQuery(
  organizationId: string | null,
  params: GetApiOrganizationsOrganizationIdDiagnosticsEventsParams = { take: 20 },
  enabled = organizationId !== null,
) {
  return useGetApiOrganizationsOrganizationIdDiagnosticsEvents<DiagnosticEventDto[], Error>(organizationId ?? '', params, {
    query: {
      queryKey: diagnosticsKeys.organizationEvents(organizationId, params),
      enabled: enabled && organizationId !== null,
      refetchInterval: REFRESH_INTERVAL,
      select: (response) => response.data,
    },
  });
}

export function usePlatformDiagnosticEventsQuery(
  params: GetApiPlatformDiagnosticsEventsParams = { take: 20 },
  enabled = true,
) {
  return useGetApiPlatformDiagnosticsEvents<DiagnosticEventDto[], Error>(params, {
    query: {
      queryKey: diagnosticsKeys.platformEvents(params),
      enabled,
      refetchInterval: REFRESH_INTERVAL,
      select: (response) => response.data,
    },
  });
}

export function useDiagnosticEventsQuery({
  organizationId,
  platformScopeActive,
  minLevel,
  take = 20,
}: {
  organizationId: string | null;
  platformScopeActive: boolean;
  minLevel?: GetApiPlatformDiagnosticsEventsParams['minLevel'];
  take?: number;
}) {
  const organizationParams: GetApiOrganizationsOrganizationIdDiagnosticsEventsParams = { take, minLevel };
  const platformParams: GetApiPlatformDiagnosticsEventsParams = { take, minLevel };
  const organizationQuery = useOrganizationDiagnosticEventsQuery(organizationId, organizationParams, !platformScopeActive && organizationId !== null);
  const platformQuery = usePlatformDiagnosticEventsQuery(platformParams, platformScopeActive);
  return platformScopeActive ? platformQuery : organizationQuery;
}

export function useDiagnosticsQueries({
  organizationId,
  platformScopeActive,
}: {
  organizationId: string | null;
  platformScopeActive: boolean;
}) {
  const organizationQuery = useOrganizationDiagnosticsQuery(organizationId, !platformScopeActive);
  const platformQuery = usePlatformDiagnosticsQuery(platformScopeActive);
  const runsQuery = useDiagnosticRunsQuery({ organizationId, platformScopeActive });
  const eventsQuery = useDiagnosticEventsQuery({
    organizationId,
    platformScopeActive,
    minLevel: platformScopeActive ? 'Warning' : 'Information',
  });

  return {
    organizationQuery,
    platformQuery,
    runsQuery,
    eventsQuery,
  };
}
