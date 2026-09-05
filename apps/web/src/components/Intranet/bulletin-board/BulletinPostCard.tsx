// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import type { KeyboardEvent } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import type { BulletinPostResponseDto } from '@types';
import { formatDateTime } from '../../../utils/dateTime';
import { Button } from '../../common/ui/Button';

interface BulletinPostCardProps {
  post: BulletinPostResponseDto;
  canManage: boolean;
  confirmingDelete: boolean;
  deleting: boolean;
  onOpen: () => void;
  onEdit: () => void;
  onRequestDelete: () => void;
  onCancelDelete: () => void;
  onConfirmDelete: () => void;
}

function getInitials(name?: string | null) {
  if (!name) return '?';
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]?.toUpperCase()).join('');
}

export function BulletinPostCard({
  post,
  canManage,
  confirmingDelete,
  deleting,
  onOpen,
  onEdit,
  onRequestDelete,
  onCancelDelete,
  onConfirmDelete,
}: BulletinPostCardProps) {
  const handleKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    if (event.target !== event.currentTarget) return;
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onOpen();
    }
  };

  const authorName = post.author?.name || 'Unknown author';

  return (
    <article
      role="button"
      tabIndex={0}
      aria-label={post.title || 'Untitled bulletin post'}
      onClick={onOpen}
      onKeyDown={handleKeyDown}
      className="group relative flex h-full w-full min-w-0 max-w-full flex-col gap-4 overflow-hidden rounded-xl bg-surface-container p-4 text-left transition-all duration-200 landscape-contained:gap-2 landscape-contained:p-3 hover:bg-surface-container-high hover:m3-elevation-1 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-accent/40"
    >
      <div className="flex min-w-0 items-start gap-3">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary-container text-xs font-bold text-on-primary-container" aria-hidden="true">
          {getInitials(post.author?.name)}
        </div>
        <div className="min-w-0 flex-1">
          <h3 className="truncate text-base font-bold text-on-surface">{post.title || 'Untitled'}</h3>
          <p className="mt-1 truncate text-sm font-medium text-on-surface-variant">
            {authorName} · {formatDateTime(post.createdAt, { day: 'numeric', month: 'short', year: 'numeric' })}
            {post.updatedAt ? ' · Edited' : ''}
          </p>
        </div>
      </div>

      <p
        className="shrink-0 overflow-hidden break-words whitespace-pre-wrap text-base leading-6 text-on-surface-variant [display:-webkit-box] [-webkit-box-orient:vertical] [overflow-wrap:anywhere]"
        style={{
          height: 'calc(var(--bulletin-post-body-lines, 3) * 1.5rem)',
          WebkitLineClamp: 'var(--bulletin-post-body-lines, 3)',
        }}
      >
        {post.body || ''}
      </p>
      {!confirmingDelete && (
      <div className="mt-auto flex items-center justify-between gap-2">
        <span className="text-sm font-bold text-brand-link">Read more</span>
        {canManage && (
        <div className="flex gap-1 opacity-70 transition-opacity md:opacity-0 md:group-hover:opacity-100 md:group-focus-within:opacity-100">
        <button type="button" aria-label={`Edit ${post.title || 'bulletin post'}`} onClick={event => { event.stopPropagation(); onEdit(); }} className="flex h-10 w-10 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface hover:text-on-primary-container focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary">
          <Pencil className="h-4 w-4" aria-hidden="true" />
        </button>
        <button type="button" aria-label={`Delete ${post.title || 'bulletin post'}`} onClick={event => { event.stopPropagation(); onRequestDelete(); }} className="flex h-10 w-10 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-error-container hover:text-error focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error">
          <Trash2 className="h-4 w-4" aria-hidden="true" />
        </button>
        </div>
        )}
      </div>
      )}

      {confirmingDelete && (
        <div className="mt-auto flex flex-col gap-2 rounded-lg text-sm" onClick={event => event.stopPropagation()}>
          <span className="text-on-error-container font-semibold">Delete this bulletin post?</span>
          <div className="flex justify-end gap-1">
            <button type="button" onClick={onCancelDelete} disabled={deleting} className="inline-flex min-h-9 items-center justify-center rounded-full px-3 text-sm font-bold transition-colors hover:bg-surface-container disabled:opacity-50">Cancel</button>
            <Button type="button" onClick={onConfirmDelete} disabled={deleting} variant="filled" color="error" className="!min-h-9 !px-4">{deleting ? 'Deleting…' : 'Delete'}</Button>
          </div>
        </div>
      )}
    </article>
  );
}
