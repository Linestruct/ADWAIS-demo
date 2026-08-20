// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Shield, User } from 'lucide-react';
import { getTagColor, getTagStyle } from '../../../utils/tagHelper';
import type { UserResponseDto } from '@types';

interface UserRowProps {
  u: UserResponseDto;
  selected?: boolean;
  onSelect?: () => void;
  onDoubleClick?: () => void;
}

export function UserRow({ u, selected = false, onSelect, onDoubleClick }: UserRowProps) {
  const avatarColor = getTagColor(u.name || u.email || 'Unknown');
  const avatarStyle = getTagStyle(avatarColor);

  return (
    <tr 
      className={`group transition-colors hover:bg-surface-container-low cursor-pointer select-none ${selected ? 'bg-primary-container/10' : ''}`}
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
          <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-lg font-bold overflow-hidden border ${avatarStyle}`}>
            {u.name?.charAt(0).toUpperCase() || '?'}
          </div>
          <div className="flex flex-col w-full max-w-[250px]">
            <span className="text-base font-bold text-on-surface truncate">{u.name || 'Unknown User'}</span>
          </div>
        </div>
      </td>
      <td className="px-4 py-3 align-middle sm:px-5 text-sm font-bold text-on-surface-variant truncate max-w-[250px]">
        {u.email}
      </td>
      <td className="w-44 px-4 py-3 align-middle sm:px-5">
        <span className={`inline-flex items-center gap-2 rounded-full px-3 py-1 text-sm font-bold ${
          u.role === 'Admin' ? 'bg-tertiary-container text-on-tertiary-container' :
          u.role === 'Employee' ? 'bg-secondary-container text-on-secondary-container' : 'bg-surface-container text-on-surface-variant'
        }`}>
          {u.role === 'Admin' ? <Shield size={12} /> : <User size={12} />}
          {u.role}
        </span>
      </td>
    </tr>
  );
}
