// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Activity, CheckCircle2, XCircle, Globe, Gauge } from 'lucide-react';
import { getTenantFaviconUrl } from '../../../utils/tenantHelper';
import type { UptimeMonitorDto } from '@types';

interface MonitorRowProps {
  m: UptimeMonitorDto;
  selected?: boolean;
  onSelect?: () => void;
  onDoubleClick?: () => void;
}

export function MonitorRow({ m, selected = false, onSelect, onDoubleClick }: MonitorRowProps) {
  const [imgError, setImgError] = useState(false);
  const faviconUrl = m.tenantImageUrl || getTenantFaviconUrl(m.tenantBaseUrl || m.url);
  const showIcon = !faviconUrl || imgError;

  return (
    <tr 
      className={`group transition-colors bg-surface border-b border-outline-variant hover:bg-surface-container cursor-pointer select-none ${selected ? 'bg-primary-container/10' : ''}`}
      onClick={onSelect}
      onDoubleClick={onDoubleClick}
    >
      <td className="w-12 px-4 py-3 align-middle sm:px-5">
        <input 
          type="checkbox" 
          checked={selected} 
          onChange={(e) => { e.stopPropagation(); onSelect?.(); }}
          onClick={(e) => e.stopPropagation()}
          className="h-5 w-5 rounded border-outline-variant text-secondary focus:ring-2 focus:ring-secondary/40 cursor-pointer"
        />
      </td>
      <td className="px-4 py-3 align-middle sm:px-5">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary-container text-on-primary-container overflow-hidden">
             {showIcon ? (
                 <Activity size={20} />
             ) : (
                 <img src={faviconUrl} alt="" className="h-full w-full object-cover" onError={() => setImgError(true)} />
             )}
          </div>
          <div className="flex flex-col w-full max-w-[250px]">
            <span className="text-base font-bold text-on-surface truncate">{m.name || 'Unnamed Monitor'}</span>
          </div>
        </div>
      </td>
      <td className="px-4 py-3 align-middle sm:px-5 text-sm font-bold text-on-surface-variant truncate max-w-[250px]">
        {m.url || 'N/A'}
      </td>
      <td className="w-32 px-4 py-3 align-middle sm:px-5">
        <span className="inline-flex items-center gap-1.5 rounded-full bg-secondary-container px-3 py-1 text-sm font-bold text-on-secondary-container">
            {m.type === 'Ping' ? <Globe size={12} /> : <Gauge size={12} />}
            {m.type}
        </span>
      </td>
      <td className="w-32 px-4 py-3 align-middle sm:px-5">
        <span className="text-sm font-bold text-on-surface-variant">
            {m.tenantId ? 'Assigned' : 'Unassigned'}
        </span>
      </td>
      <td className="w-32 px-4 py-3 align-middle sm:px-5">
        <span className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-sm font-bold ${
          m.uptimeMonitorEnabled ? 'bg-success-container text-on-success-container' : 'bg-surface-container text-on-surface-variant'
        }`}>
          {m.uptimeMonitorEnabled ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
          {m.uptimeMonitorEnabled ? 'Enabled' : 'Paused'}
        </span>
      </td>
    </tr>
  );
}
