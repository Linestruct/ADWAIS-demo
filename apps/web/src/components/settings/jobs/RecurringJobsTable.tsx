// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { CheckCircle2, Activity } from 'lucide-react';
import { EmptyState } from '../../common/ui/EmptyState';
import { TableSkeletonRows } from '../../common/ui/TableSkeletonRows';
import type { RecurringJobDto } from '@types';
import { formatDateTime } from '../../../utils/dateTime';

interface RecurringJobsTableProps {
  recurring: RecurringJobDto[] | undefined;
  isLoading?: boolean;
  isError?: boolean;
}

export function RecurringJobsTable({ recurring, isLoading = recurring === undefined, isError = false }: RecurringJobsTableProps) {
  return (
    <div className="custom-scrollbar h-full w-full overflow-auto text-left text-sm">
      <table className="w-full min-w-[720px] font-mono">
        <thead className="sticky top-0 z-10 bg-surface shadow-[0_1px_0_0_var(--md-sys-color-outline)] text-xs font-bold uppercase tracking-wider text-on-surface-variant">
          <tr>
            <th className="px-3 py-2">Job ID</th>
            <th className="px-3 py-2">Cron</th>
            <th className="px-3 py-2">Last Execution</th>
            <th className="px-3 py-2">Next Execution</th>
            <th className="px-3 py-2">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-outline-variant" aria-busy={isLoading} aria-label={isLoading ? 'Loading scheduled jobs' : undefined}>
          {isLoading ? (
            <TableSkeletonRows columnCount={5} />
          ) : isError ? (
            <tr>
              <td colSpan={5} className="p-0">
                <div role="alert" className="p-8 text-center text-on-surface-variant">
                  Unable to load scheduled jobs.
                </div>
              </td>
            </tr>
          ) : (
            recurring?.map((job) => (
              <tr key={job.id} className="transition-colors bg-surface-container-low hover:bg-surface-container">
                <td className="px-3 py-2 font-bold text-on-surface break-words max-w-[150px]">{job.id}</td>
                <td className="whitespace-nowrap px-3 py-2">
                  <span className="px-1 py-0.5 bg-brand-accent/10 text-brand-text rounded text-xs font-bold tracking-wide break-words max-w-[100px] inline-block">
                    {job.cron}
                  </span>
                </td>
                <td className="px-3 py-2 text-on-surface-variant font-bold whitespace-nowrap">
                  {formatDateTime(job.lastExecution, {
                    year: 'numeric', month: 'numeric', day: 'numeric',
                    hour: 'numeric', minute: 'numeric', second: 'numeric',
                  }) || 'Never'}
                </td>
                <td className="px-3 py-2 text-on-surface-variant whitespace-nowrap">
                  {formatDateTime(job.nextExecution, {
                    year: 'numeric', month: 'numeric', day: 'numeric',
                    hour: 'numeric', minute: 'numeric', second: 'numeric',
                  }) || 'Never'}
                </td>
                <td className="px-3 py-2">
                  {job.lastJobState === 'Succeeded' ? (
                    <span className="text-green-500 flex items-center gap-3 font-bold"><CheckCircle2 size={14} /> Succeeded</span>
                  ) : (
                    <span className="text-orange-500 flex items-center gap-3 font-bold"><Activity size={14} /> {job.lastJobState || 'Pending'}</span>
                  )}
                </td>
              </tr>
            ))
          )}
          {!isLoading && !isError && recurring?.length === 0 && (
            <EmptyState message="No recurring jobs configured." isTableRow colSpan={5} />
          )}
        </tbody>
      </table>
    </div>
  );
}
