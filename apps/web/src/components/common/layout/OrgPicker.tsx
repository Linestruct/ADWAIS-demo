// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useIsMutating } from '@tanstack/react-query';
import { Building2 } from 'lucide-react';
import { Select } from '../ui/Select';
import { useOrgSelection } from '../../../hooks/useOrgSelection';
import { useCurrentUser } from '../../../hooks/useCurrentUser';

const PLATFORM_OVERVIEW = '';

type OrgPickerProps = {
  className?: string;
};

export function OrgPicker({ className = 'shrink-0' }: OrgPickerProps) {
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
    <div className={`flex min-w-0 items-center gap-2 ${className}`}>
      <Select
        aria-label="Active organization"
        leadingIcon={<Building2 size={18} className="text-on-surface" aria-hidden="true" />}
        value={current}
        onChange={(e) => setSelectedOrgId(e.target.value === PLATFORM_OVERVIEW ? null : e.target.value)}
        disabled={isMutating > 0}
        variant="filled"
        size="lg"
        className="min-h-11 min-w-[140px] !rounded-full"
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
