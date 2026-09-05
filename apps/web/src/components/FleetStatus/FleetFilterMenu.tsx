// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { CSSProperties, ReactNode } from 'react';
import { BookmarkCheck, RotateCcw, Trash2, X } from 'lucide-react';
import {
  FilterChip,
  FilterPanelFrame,
  FilterSectionHeader,
  FloatingFilterMenu,
} from '../common/ui/FilterMenu';

const STATUS_OPTIONS = [
  { label: 'Up', value: 'UP' },
  { label: 'Down', value: 'DOWN' },
  { label: 'Paused', value: 'PAUSED' },
  { label: 'Starting', value: 'STARTING' },
  { label: 'Unknown', value: 'UNKNOWN' },
] as const;

export interface FleetFilterMenuProps {
  availableTags: string[];
  includedTags: string[];
  excludedTags: string[];
  unavailableIncludedTags: string[];
  unavailableExcludedTags: string[];
  hiddenStatuses: string[];
  onIncludedTagsChange: (tags: string[]) => void;
  onExcludedTagsChange: (tags: string[]) => void;
  onHiddenStatusesChange: (statuses: string[]) => void;
  onClearActive: () => void;
  onSaveDefault: () => void;
  onRestoreSaved: () => void;
  onForgetSaved: () => void;
  hasSavedPreferences: boolean;
  hasUnsavedChanges: boolean;
}

function UnavailableTag({ tag, onRemove }: { tag: string; onRemove: () => void }) {
  return (
    <button
      type="button"
      onClick={onRemove}
      aria-label={`Remove unavailable tag ${tag}`}
      title="This tag is not present on any currently available monitor"
      className="inline-flex min-h-8 cursor-pointer items-center gap-1.5 rounded-lg border border-dashed border-error bg-error-container px-2 text-sm font-bold text-on-error-container transition-colors hover:bg-error/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-secondary md:text-base"
    >
      <span>{tag}</span>
      <span className="font-medium">(unavailable)</span>
      <X size={15} aria-hidden="true" />
    </button>
  );
}

function toggleValue(values: string[], value: string) {
  return values.includes(value) ? values.filter(item => item !== value) : [...values, value];
}

function getFleetActiveFilterCount({
  includedTags,
  excludedTags,
  hiddenStatuses,
}: Pick<FleetFilterMenuProps, 'includedTags' | 'excludedTags' | 'hiddenStatuses'>) {
  return Number(includedTags.length > 0 || excludedTags.length > 0)
    + Number(hiddenStatuses.length > 0);
}

function PreferenceButton({
  children,
  disabled,
  onClick,
}: {
  children: ReactNode;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className="inline-flex min-h-10 cursor-pointer items-center justify-center gap-2 rounded-full border border-outline-variant bg-surface px-4 text-sm font-bold text-on-surface transition-colors hover:bg-surface-container focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-secondary disabled:cursor-not-allowed disabled:bg-on-surface/[0.10] disabled:text-on-surface/[0.38] md:text-base"
    >
      {children}
    </button>
  );
}

export function FleetFilterPanel({
  availableTags,
  includedTags,
  excludedTags,
  unavailableIncludedTags,
  unavailableExcludedTags,
  hiddenStatuses,
  onIncludedTagsChange,
  onExcludedTagsChange,
  onHiddenStatusesChange,
  onSaveDefault,
  onRestoreSaved,
  onForgetSaved,
  hasSavedPreferences,
  hasUnsavedChanges,
  embedded = false,
  floatingStyle,
}: FleetFilterMenuProps & { embedded?: boolean; floatingStyle?: CSSProperties }) {
  const toggleIncludedTag = (tag: string) => {
    const willInclude = !includedTags.includes(tag);
    onIncludedTagsChange(toggleValue(includedTags, tag));
    if (willInclude && excludedTags.includes(tag)) {
      onExcludedTagsChange(excludedTags.filter(item => item !== tag));
    }
  };
  const toggleExcludedTag = (tag: string) => {
    const willExclude = !excludedTags.includes(tag);
    onExcludedTagsChange(toggleValue(excludedTags, tag));
    if (willExclude && includedTags.includes(tag)) {
      onIncludedTagsChange(includedTags.filter(item => item !== tag));
    }
  };

  return (
    <FilterPanelFrame title="Filter fleet" embedded={embedded} floatingStyle={floatingStyle}>
      <div>
        <FilterSectionHeader
          label="Show statuses"
          active={hiddenStatuses.length > 0}
          onClear={() => onHiddenStatusesChange([])}
        />
        <p className="mb-2 text-sm text-on-surface-variant md:text-base">
          Uncheck a status to hide it from the matrix and aggregate facts.
        </p>
        <div className="flex flex-wrap gap-2">
          {STATUS_OPTIONS.map(option => (
            <FilterChip
              key={option.value}
              label={option.label}
              checked={!hiddenStatuses.includes(option.value)}
              onChange={() => onHiddenStatusesChange(toggleValue(hiddenStatuses, option.value))}
            />
          ))}
        </div>
      </div>

      <div className="min-h-0">
        <FilterSectionHeader
          label="Include tags"
          active={includedTags.length > 0}
          onClear={() => onIncludedTagsChange([])}
        />
        <p className="mb-2 text-sm text-on-surface-variant md:text-base">
          When selected, monitors matching any included tag are shown.
        </p>
        <div className="custom-scrollbar flex max-h-28 flex-wrap gap-2 overflow-y-auto pr-1">
          {availableTags.map(tag => (
            <FilterChip
              key={tag}
              label={tag}
              checked={includedTags.includes(tag)}
              onChange={() => toggleIncludedTag(tag)}
            />
          ))}
          {unavailableIncludedTags.map(tag => (
            <UnavailableTag
              key={`unavailable-included-${tag}`}
              tag={tag}
              onRemove={() => onIncludedTagsChange(includedTags.filter(item => item !== tag))}
            />
          ))}
          {availableTags.length === 0 && unavailableIncludedTags.length === 0 && (
            <span className="text-sm italic text-on-surface-variant md:text-base">No tags available</span>
          )}
        </div>
      </div>

      <div className="min-h-0">
        <FilterSectionHeader
          label="Exclude tags"
          active={excludedTags.length > 0}
          onClear={() => onExcludedTagsChange([])}
        />
        <p className="mb-2 text-sm text-on-surface-variant md:text-base">
          Excluded tags take precedence over included tags.
        </p>
        <div className="custom-scrollbar flex max-h-28 flex-wrap gap-2 overflow-y-auto pr-1">
          {availableTags.map(tag => (
            <FilterChip
              key={tag}
              label={tag}
              checked={excludedTags.includes(tag)}
              onChange={() => toggleExcludedTag(tag)}
            />
          ))}
          {unavailableExcludedTags.map(tag => (
            <UnavailableTag
              key={`unavailable-excluded-${tag}`}
              tag={tag}
              onRemove={() => onExcludedTagsChange(excludedTags.filter(item => item !== tag))}
            />
          ))}
          {availableTags.length === 0 && unavailableExcludedTags.length === 0 && (
            <span className="text-sm italic text-on-surface-variant md:text-base">No tags available</span>
          )}
        </div>
      </div>

      <div className="border-t border-outline-variant pt-4">
        <h3 className="text-sm font-black uppercase tracking-wide text-on-surface-variant md:text-base">
          Saved defaults
        </h3>
        <p className="mt-1 text-sm text-on-surface-variant md:text-base">
          Saved defaults are restored the next time you open Fleet Status.
        </p>
        <div className="mt-3 flex flex-wrap gap-2">
          <PreferenceButton disabled={!hasUnsavedChanges} onClick={onSaveDefault}>
            <BookmarkCheck size={17} aria-hidden="true" />
            Save as default
          </PreferenceButton>
          <PreferenceButton
            disabled={!hasSavedPreferences || !hasUnsavedChanges}
            onClick={onRestoreSaved}
          >
            <RotateCcw size={17} aria-hidden="true" />
            Restore saved
          </PreferenceButton>
          <PreferenceButton disabled={!hasSavedPreferences} onClick={onForgetSaved}>
            <Trash2 size={17} aria-hidden="true" />
            Forget default
          </PreferenceButton>
        </div>
      </div>
    </FilterPanelFrame>
  );
}

export function FleetFilterMenu(props: FleetFilterMenuProps) {
  return (
    <FloatingFilterMenu
      activeCount={getFleetActiveFilterCount(props)}
      ariaLabel="Fleet filter controls"
      clearLabel="Clear active fleet filters"
      onClearAll={props.onClearActive}
      width={560}
      placement="top"
      renderPanel={floatingStyle => <FleetFilterPanel {...props} floatingStyle={floatingStyle} />}
    />
  );
}
