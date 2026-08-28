// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

import { useIsMutating } from '@tanstack/react-query';
import { Building2 } from 'lucide-react';
import { Select } from '../ui/Select';
import { useOrgSelection } from '../../../hooks/useOrgSelection';
import { useCurrentUser } from '../../../hooks/useCurrentUser';

const PLATFORM_OVERVIEW = '';

export function OrgPicker() {
  const { selectedOrgId, setSelectedOrgId, organizations } = useOrgSelection();
  const { scope } = useCurrentUser();
  const isMutating = useIsMutating();

  const canSwitch = scope.isPlatformAdmin || organizations.length > 1;
  if (!canSwitch) return null;

  const options = [
    ...(scope.isPlatformAdmin ? [{ label: 'Platform overview', value: PLATFORM_OVERVIEW }] : []),
    ...organizations.map(org => ({ label: org.name, value: org.id })),
  ];
  const current = selectedOrgId ?? (scope.isPlatformAdmin ? PLATFORM_OVERVIEW : organizations[0]?.id ?? PLATFORM_OVERVIEW);

  return (
    <div className="flex shrink-0 items-center gap-2">
      <Building2 size={16} className="text-white/70" aria-hidden="true" />
      <Select
        aria-label="Active organization"
        value={current}
        onChange={(e) => setSelectedOrgId(e.target.value === PLATFORM_OVERVIEW ? null : e.target.value)}
        disabled={isMutating > 0}
        variant="filled"
        size="md"
        className="min-h-11 min-w-[140px]"
      >
        {options.map(option => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </Select>
    </div>
  );
}