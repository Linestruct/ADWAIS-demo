import type { ReactNode } from 'react';
import { Activity, AlertTriangle, CheckCircle2, Clock3, Radio, ShieldCheck, TerminalSquare, XCircle } from 'lucide-react';
import { ConsoleLoadingRows } from '../components/common/ui/ConsoleLoadingRows';
import { ConsolePanel } from '../components/common/layout/ConsolePanel';
import { EmptyState } from '../components/common/ui/EmptyState';
import { SettingsPanel } from '../components/common/layout/SettingsPanel';
import { SettingsPanelHeader } from '../components/common/layout/SettingsPanelHeader';
import { TableSkeletonRows } from '../components/common/ui/TableSkeletonRows';
import { DataTable } from '../components/common/ui/DataTable';
import {
  PipelineRunsTable,
  PipelineRunsToolbar,
  usePipelineRunsTable,
} from '../components/settings/diagnostics/PipelineRunsTable';
import { useCurrentUser } from '../hooks/useCurrentUser';
import { useDiagnosticsQueries } from '../hooks/useDiagnosticsQueries';
import { useOrgSelection } from '../hooks/useOrgSelection';
import type { DiagnosticEventDto, OrganizationDiagnosticsDto, PlatformDiagnosticsDto } from '@types';

const date = (value?: string | null) => value ? new Date(value).toLocaleString() : 'Never';

export function Diagnostics() {
  const { user, isLoading: userLoading } = useCurrentUser();
  const { selectedOrgId, setSelectedOrgId } = useOrgSelection();
  const platformScopeActive = user?.isPlatformAdmin === true && selectedOrgId === null;
  const organizationId = selectedOrgId ?? user?.organizationId ?? null;
  const { organizationQuery, platformQuery, runsQuery, eventsQuery } = useDiagnosticsQueries({
    organizationId,
    platformScopeActive,
  });

  const runs = runsQuery.data ?? [];
  const events = eventsQuery.data ?? [];
  const runTableState = usePipelineRunsTable(runs);
  const overviewLoading = userLoading || organizationQuery.isLoading || platformQuery.isLoading;
  const overviewError = organizationQuery.isError || platformQuery.isError;

  return (
    <div className="flex h-full min-h-0 min-w-0 flex-col gap-4 landscape-contained:grid landscape-contained:grid-cols-[minmax(0,2fr)_minmax(18rem,1fr)] landscape-contained:grid-rows-[auto_minmax(0,1fr)]">
      <SettingsPanel
        title={platformScopeActive ? 'Platform operations' : 'Pipeline diagnostics'}
        subtitle={platformScopeActive ? 'Installation health and organizations affected by recent failures.' : 'Find failed and active work for this organization.'}
        icon={platformScopeActive ? <ShieldCheck size={24} /> : <Activity size={24} />}
        className="min-h-[200px] shrink-0 max-h-none landscape-contained:col-start-2 landscape-contained:row-start-1"
        contentClassName="custom-scrollbar flex min-h-0 flex-1 flex-col gap-3 overflow-x-hidden overflow-y-auto p-3 sm:p-4"
      >
        {overviewLoading && (
          <div className="min-w-0 flex-1 overflow-hidden border border-outline-variant bg-surface">
            <table className="w-full text-left text-sm">
              <tbody><TableSkeletonRows columnCount={platformScopeActive ? 5 : 4} /></tbody>
            </table>
          </div>
        )}
        {overviewError && (
          <div role="alert" className="rounded-xl bg-error-container p-4 text-sm font-semibold text-on-error-container">
            Diagnostics are temporarily unavailable.
          </div>
        )}
        {!overviewLoading && !overviewError && !platformScopeActive && organizationQuery.data && (
          <OrganizationSummary organization={organizationQuery.data} />
        )}
        {!overviewLoading && !overviewError && platformScopeActive && platformQuery.data && (
          <PlatformOverview platform={platformQuery.data} onSelectOrganization={setSelectedOrgId} />
        )}
        {!overviewLoading && !overviewError && !organizationQuery.data && !platformQuery.data && (
          <EmptyState message="No diagnostics are available." />
        )}
      </SettingsPanel>

      <SettingsPanel
        className="min-h-[420px] flex-1 max-h-none contained:min-h-0 landscape-contained:col-start-1 landscape-contained:row-span-2 landscape-contained:row-start-1"
        contentClassName="flex min-h-0 flex-1 flex-col overflow-hidden"
      >
        <SettingsPanelHeader
          title="Pipeline runs"
          subtitle="Find failures and active work by organization, tenant and pipeline."
          icon={<Activity size={24} />}
        >
          <PipelineRunsToolbar state={runTableState} />
        </SettingsPanelHeader>
        <PipelineRunsTable
          runs={runs}
          organizationId={organizationId}
          platformScopeActive={platformScopeActive}
          state={runTableState}
          isLoading={runsQuery.isLoading}
          isError={runsQuery.isError}
          className="flex-1"
          viewportClassName="border border-outline-variant bg-surface"
        />
      </SettingsPanel>

      <ConsolePanel
        title="Diagnostic events"
        subtitle="Recorded failures, warnings and recovery information."
        icon={<TerminalSquare size={18} />}
        className="min-h-[220px] max-h-[280px] flex-none landscape-contained:col-start-2 landscape-contained:row-start-2 landscape-contained:self-start landscape-contained:h-auto"
      >
        {eventsQuery.isLoading ? (
          <ConsoleLoadingRows label="Loading diagnostic events" />
        ) : eventsQuery.isError ? (
          <div role="alert" className="flex h-full items-center justify-center p-4 text-center text-console-text">
            Unable to load diagnostic events.
          </div>
        ) : events.length > 0 ? (
          <div className="flex min-w-0 flex-col divide-y divide-console-border">
            {events.map(event => (
              <EventRow
                key={event.id || String(event.timestamp) + ':' + String(event.code)}
                event={event}
                showOrganization={platformScopeActive}
              />
            ))}
          </div>
        ) : (
          <div className="flex h-full items-center justify-center p-4 text-center text-console-text">
            No diagnostic events recorded.
          </div>
        )}
      </ConsolePanel>
    </div>
  );
}

function OrganizationSummary({ organization }: { organization: OrganizationDiagnosticsDto }) {
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 landscape-contained:grid-cols-2">
      <Metric label="Tracked pipelines" value={organization.pipelineCount ?? 0} icon={<Activity size={18} />} />
      <Metric label="Healthy" value={organization.healthyCount ?? 0} icon={<CheckCircle2 size={18} />} />
      <Metric label="Need attention" value={organization.attentionCount ?? 0} icon={<AlertTriangle size={18} />} />
      <Metric label="Running now" value={organization.runningCount ?? 0} icon={<Clock3 size={18} />} />
    </div>
  );
}

function PlatformOverview({
  platform,
  onSelectOrganization,
}: {
  platform: PlatformDiagnosticsDto;
  onSelectOrganization: (organizationId: string) => void;
}) {
  const health = platform.health;
  const organizations = platform.affectedOrganizations ?? [];

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col gap-3">
      <div className="grid shrink-0 grid-cols-2 gap-3 sm:grid-cols-5 landscape-contained:grid-cols-2">
        <Metric label="Database" value={health?.databaseStatus ?? 'Unknown'} icon={<Radio size={18} />} />
        <Metric label="Workers" value={health?.hangfire?.status ?? 'Unknown'} icon={<Activity size={18} />} />
        <Metric label="Queued" value={health?.hangfire?.enqueuedCount ?? 0} icon={<Clock3 size={18} />} />
        <Metric label="Processing" value={health?.hangfire?.processingCount ?? 0} icon={<Radio size={18} />} />
        <Metric label="Failed jobs" value={health?.hangfire?.failedCount ?? 0} icon={<XCircle size={18} />} />
      </div>

      <section className="flex min-h-0 min-w-0 flex-1 flex-col">
        <div className="flex shrink-0 items-start justify-between gap-3 pb-2">
          <div>
            <h3 className="m-0 text-lg font-bold text-on-surface">Organizations needing attention</h3>
            <p className="m-0 mt-1 text-sm text-on-surface-variant">Recent failures, active work or diagnostic events.</p>
          </div>
          <span className="shrink-0 rounded-full bg-surface-container px-2.5 py-1 text-sm font-bold text-on-surface-variant">{organizations.length}</span>
        </div>
        <DataTable
          className="min-h-0 flex-1"
          viewportClassName="border border-outline-variant bg-surface"
          tableClassName="whitespace-nowrap"
        >
          <caption className="sr-only">Organizations with recent diagnostic activity</caption>
          <thead className="sticky top-0 z-10 border-b border-outline-variant bg-surface-container-high text-on-surface-variant">
            <tr>
              <th className="px-4 py-3 text-xs font-black uppercase tracking-wide sm:px-5">Organization</th>
              <th className="w-32 px-4 py-3 text-xs font-black uppercase tracking-wide sm:px-5">Failed runs</th>
              <th className="w-32 px-4 py-3 text-xs font-black uppercase tracking-wide sm:px-5">Active runs</th>
              <th className="w-32 px-4 py-3 text-xs font-black uppercase tracking-wide sm:px-5">Recent events</th>
              <th className="w-40 px-4 py-3 text-xs font-black uppercase tracking-wide sm:px-5">Open</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-outline-variant">
            {organizations.map(item => (
              <tr key={item.organizationId || item.organizationName} className="bg-surface transition-colors hover:bg-surface-container">
                <td className="px-4 py-3 font-bold text-on-surface sm:px-5">{item.organizationName || item.organizationId || 'Organization'}</td>
                <td className="px-4 py-3 text-on-surface-variant sm:px-5">{item.failedRuns ?? 0}</td>
                <td className="px-4 py-3 text-on-surface-variant sm:px-5">{item.activeRuns ?? 0}</td>
                <td className="px-4 py-3 text-on-surface-variant sm:px-5">{item.recentEvents ?? 0}</td>
                <td className="px-4 py-3 sm:px-5">
                  {item.organizationId && (
                    <button
                      type="button"
                      onClick={() => onSelectOrganization(item.organizationId as string)}
                      className="min-h-10 rounded-full px-4 text-sm font-bold text-secondary transition-colors hover:bg-secondary-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-secondary"
                    >
                      View organization
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {organizations.length === 0 && <EmptyState message="No organizations need attention." isTableRow colSpan={5} />}
          </tbody>
        </DataTable>
      </section>
    </div>
  );
}

function EventRow({ event, showOrganization }: { event: DiagnosticEventDto; showOrganization: boolean }) {
  const level = event.level || 'Information';
  const isError = level === 'Error' || level === 'Critical';
  const isWarning = level === 'Warning';
  const owner = showOrganization
    ? [event.organizationName || event.organizationId, event.tenantName || event.tenantId].filter(Boolean).join(' · ')
    : event.tenantName || event.tenantId;

  return (
    <div className="flex min-w-0 flex-col px-3 py-2 text-on-surface-variant transition-colors hover:bg-console-hover">
      <div className="flex min-w-0 items-start justify-between gap-4">
        <span className="break-words font-bold text-console-text">{event.message || 'Diagnostic event'}</span>
        <span className={'shrink-0 text-xs font-black uppercase ' + (isError ? 'text-console-red' : isWarning ? 'text-console-yellow' : 'text-console-blue')}>{level}</span>
      </div>
      <div className="mt-1 flex flex-wrap gap-x-5 gap-y-1 text-xs">
        <span>{date(event.timestamp)}</span>
        {event.source && <span>{event.source}</span>}
        {event.code && <span>{event.code}</span>}
        {owner && <span>{owner}</span>}
        {event.suggestedAction && <span>{event.suggestedAction}</span>}
      </div>
    </div>
  );
}

function Metric({ label, value, icon }: { label: string; value: string | number; icon: ReactNode }) {
  return (
    <div className="rounded-xl bg-surface-container-high p-3">
      <div className="flex items-center gap-2 text-xs font-black uppercase tracking-wide text-on-surface-variant">{icon}{label}</div>
      <div className="mt-2 truncate text-xl font-black text-on-surface">{value}</div>
    </div>
  );
}
