// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useRef, useState } from 'react';
import { Pencil, Trash2, X } from 'lucide-react';
import type { BulletinPostResponseDto } from '@types';
import { formatDateTime } from '../../../utils/dateTime';
import { Button } from '../../common/ui/Button';

interface BulletinPostDetailProps {
  post: BulletinPostResponseDto;
  canManage: boolean;
  deleting: boolean;
  onClose: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

export function BulletinPostDetail({ post, canManage, deleting, onClose, onEdit, onDelete }: BulletinPostDetailProps) {
  const closeRef = useRef<HTMLButtonElement>(null);
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  useEffect(() => {
    closeRef.current?.focus();
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  return (
    <div className="absolute bottom-0 left-0 right-0 top-0 z-20 flex bg-scrim p-3 md:p-5" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}>
      <article role="dialog" aria-modal="true" aria-labelledby="bulletin-post-detail-title" className="m-auto flex max-h-full w-full max-w-3xl flex-col overflow-hidden rounded-3xl bg-surface-container m3-elevation-4">
        <header className="flex flex-wrap  items-start justify-between gap-1 border-b border-outline-variant px-6 py-4">
          <div className="min-w-0">
            <h3 id="bulletin-post-detail-title" className="text-xl font-bold text-on-surface">{post.title || 'Untitled'}</h3>
            <p className="mt-1 text-sm font-medium text-on-surface-variant">
              {post.author?.name || 'Unknown author'} · {formatDateTime(post.createdAt, { day: 'numeric', month: 'short', year: 'numeric' })}
              {post.updatedAt ? ' · Edited' : ''}
            </p>
          </div>
          <div className="flex shrink-0 gap-2 max-w-[100px] sm:max-w-none">
            {canManage && <button type="button" onClick={onEdit} aria-label={`Edit ${post.title || 'bulletin post'}`} className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant hover:bg-primary-container hover:text-on-primary-container"><Pencil className="h-5 w-5" aria-hidden="true" /></button>}
            {canManage && <button type="button" onClick={() => setConfirmingDelete(true)} aria-label={`Delete ${post.title || 'bulletin post'}`} className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant hover:bg-error-container hover:text-error"><Trash2 className="h-5 w-5" aria-hidden="true" /></button>}
            <button ref={closeRef} type="button" onClick={onClose} aria-label="Close bulletin post" className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant hover:bg-surface-container-high"><X className="h-5 w-5" aria-hidden="true" /></button>
          </div>
        </header>
        <div className="min-h-0 overflow-y-auto px-6 py-4 custom-scrollbar">
          <p className="whitespace-pre-wrap break-words text-base leading-relaxed text-on-surface [overflow-wrap:anywhere]">{post.body || ''}</p>
        </div>
        {confirmingDelete && (
          <footer className="flex flex-wrap items-center justify-between gap-3 border-t border-outline-variant bg-surface-container px-6 py-4 text-base">
            <span className="font-semibold">Delete this bulletin post?</span>
            <div className="flex gap-2">
              <button type="button" onClick={() => setConfirmingDelete(false)} disabled={deleting} className="min-h-10 rounded-full px-4 text-base font-bold hover:bg-surface-container-high disabled:opacity-50">Cancel</button>
              <Button type="button" onClick={onDelete} disabled={deleting} variant="filled" color="error" className="!min-h-10 !text-base">{deleting ? 'Deleting…' : 'Delete'}</Button>
            </div>
          </footer>
        )}
      </article>
    </div>
  );
}
