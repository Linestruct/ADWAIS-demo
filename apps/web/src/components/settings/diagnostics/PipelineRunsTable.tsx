import { Fragment, useMemo, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { ChevronDown, ChevronUp } from 'lucide-react';
import { DataTable } from '../../common/ui/DataTable';
import { EmptyState } from '../../common/ui/EmptyState';
import { SearchInput } from '../../common/ui/SearchInput';
import { Select } from '../../common/ui/Select';
import { TableSkeletonRows } from '../../common/ui/TableSkeletonRows';
import {
  useOrganizationDiagnosticRunQuery,
  usePlatformDiagnosticRunQuery,
} from '../../../hooks/useDiagnosticsQueries';
import type { DiagnosticEventDto, PipelineRunDto } from '@types';

export type RunStatusFilter = 'all' | 'active' | 'failed' | 'succeeded' | 'other';

export interface PipelineRunsTableState {
  search: string;
  setSearch: (value: string) => void;
  status: RunStatusFilter;
  setStatus: (value: RunStatusFilter) => void;
  kind: string;
  setKind: (value: string) => void;
  kinds: string[];
  visibleRuns: PipelineRunDto[];
}

interface PipelineRunsTableProps {
  runs?: PipelineRunDto[];
  organizationId: string | null;
  platformScopeActive: boolean;
  state?: PipelineRunsTableState;
  isLoading?: boolean;
  isError?: boolean;
  className?: string;
  viewportClassName?: string;
}

const activeStates = new Set(['Pending', 'Queued', 'Running']);

function runStatusFilter(state?: string | null): RunStatusFilter {
  if (state && activeStates.has(state)) return 'active';
  if (state === 'Failed' || state === 'RetryScheduled') return 'failed';
  if (state === 'Succeeded') return 'succeeded';
  return 'other';
}

function stateLabel(state?: string | null) {
  switch (state) {
    case 'RetryScheduled': return 'Retry scheduled';
    case 'Succeeded': return 'Succeeded';
    case 'Failed': return 'Failed';
    case 'Canceled': return 'Canceled';
    case 'Skipped': return 'Skipped';
    case 'Running': return 'Running';
    case 'Queued': return 'Queued';
    case 'Pending': return 'Pending';
    default: return state || 'Unknown';
  }
}

function statusClasses(state?: string | null) {
  if (state === 'Succeeded') return 'bg-success-container text-on-success-container';
  if (state === 'Failed') return 'bg-error-container text-on-error-container';
  if (state === 'RetryScheduled') return 'bg-warning-container text-on-warning-container';
  if (activeStates.has(state ?? '')) return 'bg-secondary-container text-on-secondary-container';
  return 'bg-surface-container text-on-surface-variant';
}

function kindLabel(kind?: string | null) {
  switch (kind) {
    case 'OrderIngestion': return 'Order ingestion';
    case 'FeedRefresh': return 'Feed refresh';
    case 'MonitorSync': return 'Monitor synchronization';
    case 'AccountStats': return 'Account statistics';
    default: return kind || 'Unknown';
  }
}

function formatDate(value?: string | null) {
  return value ? new Date(value).toLocaleString() : '—';
}

function runName(run: PipelineRunDto) {
  return run.resourceName || run.resourceKey || kindLabel(run.kind);
}

function searchableText(run: PipelineRunDto) {
  return [
    run.organizationName,
    run.tenantName,
    run.resourceName,
    run.resourceKey,
    run.kind,
    kindLabel(run.kind),
    run.state,
    run.outcomeCode,
    run.safeSummary,
  ].filter(Boolean).join(' ').toLocaleLowerCase();
}

// eslint-disable-next-line react-refresh/only-export-components
export function usePipelineRunsTable(runs: PipelineRunDto[] = []): PipelineRunsTableState {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<RunStatusFilter>('all');
  const [kind, setKind] = useState('all');
  const kinds = useMemo(
    () => [...new Set(runs.map(run => run.kind).filter(Boolean) as string[])].sort(),
    [runs],
  );
  const visibleRuns = useMemo(() => {
    const query = search.trim().toLocaleLowerCase();
    return [...runs]
      .filter(run => !query || searchableText(run).includes(query))
      .filter(run => status === 'all' || runStatusFilter(run.state) === status)
      .filter(run => kind === 'all' || run.kind === kind)
      .sort((left, right) => (right.requestedAt ?? '').localeCompare(left.requestedAt ?? ''));
  }, [kind, runs, search, status]);

  return { search, setSearch, status, setStatus, kind, setKind, kinds, visibleRuns };
}

export function PipelineRunsToolbar({ state }: { state: PipelineRunsTableState }) {
  return (
    <div className="flex min-w-0 flex-wrap items-center gap-2">
      <SearchInput
        value={state.search}
        onChange={state.setSearch}
        placeholder="Search runs..."
        className="flex-1 sm:flex-none"
      />
      <Select
        aria-label="Run status"
        value={state.status}
        onChange={event => state.setStatus(event.target.value as RunStatusFilter)}
        variant="filled"
        size="sm"
        fullWidth={false}
        className="min-w-32"
      >
        <option value="all">All statuses</option>
        <option value="active">Active</option>
        <option value="failed">Failed</option>
        <option value="succeeded">Succeeded</option>
        <option value="other">Other</option>
      </Select>
      <Select
        aria-label="Pipeline type"
        value={state.kind}
        onChange={event => state.setKind(event.target.value)}
        variant="filled"
        size="sm"
        fullWidth={false}
        className="min-w-36"
      >
        <option value="all">All pipeline types</option>
        {state.kinds.map(value => <option key={value} value={value}>{kindLabel(value)}</option>)}
      </Select>
    </div>
  );
}

function RunDetails({
  run,
  organizationId,
  platformScopeActive,
}: {
  run: PipelineRunDto;
  organizationId: string | null;
  platformScopeActive: boolean;
}) {
  const organizationQuery = useOrganizationDiagnosticRunQuery(
    organizationId,
    run.id ?? null,
    !platformScopeActive,
  );
  const platformQuery = usePlatformDiagnosticRunQuery(run.id ?? null, platformScopeActive);
  const query = platformScopeActive ? platformQuery : organizationQuery;
  const detailsRun = query.data?.run ?? run;
  const events = query.data?.events ?? [];

  return (
    <div className="grid gap-4 bg-surface-container-low px-4 py-4 text-sm text-on-surface sm:grid-cols-[minmax(0,1fr)_minmax(0,1.5fr)] sm:px-5">
      <dl className="grid grid-cols-2 gap-x-4 gap-y-3">
        <Detail label="Trigger" value={detailsRun.trigger || '—'} />
        <Detail label="Resource" value={detailsRun.resourceKey || detailsRun.resourceName || '—'} />
        <Detail label="Requested" value={formatDate(detailsRun.requestedAt)} />
        <Detail label="Started" value={formatDate(detailsRun.startedAt)} />
        <Detail label="Completed" value={formatDate(detailsRun.completedAt)} />
        <Detail label="Next retry" value={formatDate(detailsRun.nextRetryAt)} />
        <Detail label="Attempts" value={String(detailsRun.attemptCount ?? 0)} />
        <Detail label="Outcome" value={detailsRun.outcomeCode || '—'} />
        <Detail label="Work" value={detailsRun.workCount == null ? '—' : String(detailsRun.workCount)} />
      </dl>

      <div className="min-w-0 space-y-3">
        <div>
          <h4 className="m-0 text-xs font-black uppercase tracking-wide text-on-surface-variant">What happened</h4>
          <p className="m-0 mt-1 break-words text-on-surface">{detailsRun.safeSummary || 'No result details were recorded.'}</p>
        </div>
        {query.isLoading && <p className="m-0 text-on-surface-variant">Loading related events…</p>}
        {query.isError && <p role="alert" className="m-0 text-error">Unable to load related events.</p>}
        {!query.isLoading && !query.isError && events.length > 0 && <RunEvents events={events} />}
        {!query.isLoading && !query.isError && events.length === 0 && (
          <p className="m-0 text-on-surface-variant">No related diagnostic events.</p>
        )}
        {(detailsRun.requestId || detailsRun.traceId) && (
          <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-on-surface-variant">
            {detailsRun.requestId && <span>Request: <code className="break-all">{detailsRun.requestId}</code></span>}
            {detailsRun.traceId && <span>Trace: <code className="break-all">{detailsRun.traceId}</code></span>}
          </div>
        )}
      </div>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs font-black uppercase tracking-wide text-on-surface-variant">{label}</dt>
      <dd className="m-0 mt-1 break-words font-semibold text-on-surface">{value}</dd>
    </div>
  );
}

function RunEvents({ events }: { events: DiagnosticEventDto[] }) {
  return (
    <div>
      <h4 className="m-0 text-xs font-black uppercase tracking-wide text-on-surface-variant">Related events</h4>
      <div className="mt-2 divide-y divide-outline-variant rounded-xl border border-outline-variant bg-surface">
        {events.map(event => (
          <div key={event.id || String(event.timestamp) + ':' + String(event.code)} className="px-3 py-2">
            <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
              <span className="break-words font-semibold text-on-surface">{event.message || 'Diagnostic event'}</span>
              <span className="shrink-0 text-xs font-black uppercase text-on-surface-variant">{event.level || 'Information'}</span>
            </div>
            <div className="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-xs text-on-surface-variant">
              <span>{formatDate(event.timestamp)}</span>
              {event.code && <span>{event.code}</span>}
              {event.suggestedAction && <span>{event.suggestedAction}</span>}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function PipelineRunsTable({
  runs = [],
  organizationId,
  platformScopeActive,
  state,
  isLoading = false,
  isError = false,
  className = '',
  viewportClassName = 'border border-outline-variant bg-surface',
}: PipelineRunsTableProps) {
  const internalState = usePipelineRunsTable(runs);
  const tableState = state ?? internalState;
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);
  const columnCount = platformScopeActive ? 8 : 7;

  return (
    <DataTable
      className={'min-h-0 ' + className}
      tableClassName="min-w-[1120px] table-fixed whitespace-normal"
      viewportClassName={viewportClassName}
    >
      <caption className="sr-only">Pipeline run history</caption>
      <thead className="sticky top-0 z-10 border-b border-outline-variant bg-surface-container-high text-on-surface-variant">
        <tr>
          {platformScopeActive && <th className="w-40 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Organization</th>}
          <th className="w-40 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Tenant</th>
          <th className="w-48 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Pipeline</th>
          <th className="w-40 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Type</th>
          <th className="w-36 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Run status</th>
          <th className="w-52 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Requested / completed</th>
          <th className="w-72 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Result</th>
          <th className="w-28 px-4 py-4 text-sm font-black uppercase tracking-wide sm:px-5">Details</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-outline-variant">
        {isLoading && <TableSkeletonRows columnCount={columnCount} />}
        {!isLoading && isError && (
          <tr><td colSpan={columnCount} className="p-8 text-center text-on-surface-variant">Unable to load pipeline runs.</td></tr>
        )}
        {!isLoading && !isError && tableState.visibleRuns.map(run => {
          const key = run.id || String(run.requestedAt) + ':' + String(run.resourceKey) + ':' + String(run.kind);
          const selected = Boolean(run.id) && selectedRunId === run.id;
          const toggleSelected = () => {
            if (run.id) setSelectedRunId(selected ? null : run.id);
          };
          const handleRowKeyDown = (event: KeyboardEvent<HTMLTableRowElement>) => {
            if (event.target !== event.currentTarget) return;
            if (event.key === 'Enter' || event.key === ' ') {
              event.preventDefault();
              toggleSelected();
            }
          };
          return (
            <Fragment key={key}>
              <tr
                className="cursor-pointer bg-surface align-top transition-colors hover:bg-surface-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-inset focus-visible:outline-secondary"
                onClick={toggleSelected}
                onKeyDown={handleRowKeyDown}
                tabIndex={run.id ? 0 : undefined}
                aria-expanded={run.id ? selected : undefined}
              >
                {platformScopeActive && <td className="break-words px-4 py-3 font-bold text-on-surface sm:px-5">{run.organizationName || run.organizationId || 'Organization'}</td>}
                <td className="break-words px-4 py-3 font-semibold text-on-surface sm:px-5">{run.tenantName || 'Organization work'}</td>
                <td className="break-words px-4 py-3 font-bold text-on-surface sm:px-5">{runName(run)}</td>
                <td className="break-words px-4 py-3 text-on-surface-variant sm:px-5">{kindLabel(run.kind)}</td>
                <td className="px-4 py-3 sm:px-5">
                  <span className={'inline-flex max-w-full whitespace-normal rounded-full px-2.5 py-1 text-xs font-black ' + statusClasses(run.state)}>
                    {stateLabel(run.state)}
                  </span>
                </td>
                <td className="break-words px-4 py-3 text-sm text-on-surface-variant sm:px-5">
                  <span className="block"><strong className="font-bold text-on-surface">Requested</strong> {formatDate(run.requestedAt)}</span>
                  <span className="mt-1 block"><strong className="font-bold text-on-surface">Completed</strong> {formatDate(run.completedAt)}</span>
                </td>
                <td className="break-words px-4 py-3 text-on-surface-variant sm:px-5">
                  <span className="block">{run.safeSummary || (activeStates.has(run.state || '') ? 'Work is in progress.' : 'No result details recorded.')}</span>
                  {run.outcomeCode && <span className="mt-1 block text-xs font-bold uppercase">{run.outcomeCode}</span>}
                </td>
                <td className="px-4 py-3 sm:px-5">
                  <button
                    type="button"
                    aria-expanded={selected}
                    onClick={event => {
                      event.stopPropagation();
                      toggleSelected();
                    }}
                    disabled={!run.id}
                    className="inline-flex min-h-9 items-center gap-1 rounded-full px-3 text-sm font-bold text-secondary transition-colors hover:bg-secondary-container disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    {selected ? <ChevronUp size={16} aria-hidden="true" /> : <ChevronDown size={16} aria-hidden="true" />}
                    <span>{selected ? 'Hide' : 'View'}</span>
                  </button>
                </td>
              </tr>
              {selected && (
                <tr>
                  <td colSpan={columnCount} className="p-0">
                    <RunDetails run={run} organizationId={organizationId} platformScopeActive={platformScopeActive} />
                  </td>
                </tr>
              )}
            </Fragment>
          );
        })}
        {!isLoading && !isError && tableState.visibleRuns.length === 0 && (
          <EmptyState
            message={runs.length === 0 ? 'No pipeline runs recorded.' : 'No runs match these filters.'}
            isTableRow
            colSpan={columnCount}
          />
        )}
      </tbody>
    </DataTable>
  );
}
