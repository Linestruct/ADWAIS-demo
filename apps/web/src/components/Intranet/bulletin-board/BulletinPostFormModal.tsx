// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { Pencil, Plus, Trash2, X } from 'lucide-react';
import { ErrorAlert } from '../../common/ui/ErrorAlert';
import { FormField } from '../../common/ui/FormField';
import { Button } from '../../common/ui/Button';

interface BulletinPostFormModalProps {
  mode: 'create' | 'edit';
  isOpen: boolean;
  isPending: boolean;
  error: string | null;
  initialTitle?: string;
  initialBody?: string;
  canDelete?: boolean;
  isDeleting?: boolean;
  onClose: () => void;
  onSubmit: (title: string, body: string) => void;
  onDelete?: () => void;
}

type BulletinPostFormDialogProps = Omit<BulletinPostFormModalProps, 'isOpen'>;

function BulletinPostFormDialog({ mode, isPending, error, initialTitle = '', initialBody = '', canDelete = false, isDeleting = false, onClose, onSubmit, onDelete }: BulletinPostFormDialogProps) {
  const [title, setTitle] = useState(initialTitle);
  const [body, setBody] = useState(initialBody);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const isEditing = mode === 'edit';
  const Icon = isEditing ? Pencil : Plus;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4 backdrop-blur-sm" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget && !isPending && !isDeleting) onClose(); }}>
      <form role="dialog" aria-modal="true" aria-labelledby="bulletin-post-form-title" onSubmit={event => { event.preventDefault(); onSubmit(title.trim(), body.trim()); }} className="flex w-full max-w-lg flex-col overflow-hidden rounded-3xl bg-surface m3-elevation-4">
        <div className="flex items-center justify-between px-6 py-5">
          <h2 id="bulletin-post-form-title" className="flex items-center gap-4 text-xl font-bold text-on-surface">
            <Icon className="h-5 w-5 text-brand-link" aria-hidden="true" />
            {isEditing ? 'Edit bulletin post' : 'New bulletin post'}
          </h2>
          <button type="button" onClick={onClose} disabled={isPending || isDeleting} aria-label="Close bulletin post dialog" className="flex h-11 w-11 items-center justify-center rounded-full text-on-surface-variant transition-colors hover:bg-surface-container-high active:bg-surface-container-highest focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:opacity-50">
            <X className="h-5 w-5" aria-hidden="true" />
          </button>
        </div>

        <div className="flex flex-col gap-4 px-6 pb-6">
          {error && <ErrorAlert title={isEditing ? 'Unable to update bulletin post' : 'Unable to create bulletin post'} message={error} />}
          <FormField label="Title" autoFocus required maxLength={255} value={title} onChange={event => setTitle(event.target.value)} />
          <FormField as="textarea" label="Message" required maxLength={5000} rows={6} value={body} onChange={event => setBody(event.target.value)} className="resize-y" />
        </div>

        {confirmingDelete ? (
          <footer className="flex flex-wrap items-center justify-between gap-3 border-t border-outline-variant bg-surface-container px-6 py-4 text-base text-on-surface">
            <span className="font-semibold">Delete this bulletin post?</span>
            <div className="flex gap-2">
              <button type="button" onClick={() => setConfirmingDelete(false)} disabled={isDeleting} className="min-h-10 rounded-full px-4 text-base font-bold transition-colors hover:bg-surface-container-high active:bg-surface-container-highest focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:opacity-50">Cancel</button>
              <Button type="button" onClick={onDelete} disabled={isDeleting} variant="filled" color="error" className="!min-h-10 !text-base">
                {isDeleting && <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-error border-t-transparent" aria-hidden="true" />}
                {isDeleting ? 'Deleting…' : 'Delete'}
              </Button>
            </div>
          </footer>
        ) : (
          <footer className="flex flex-wrap items-center justify-between gap-3 px-6 pb-4">
            <div>
              {isEditing && canDelete && <Button type="button" onClick={() => setConfirmingDelete(true)} disabled={isPending || isDeleting} variant="text" color="error" icon={<Trash2 className="h-4 w-4" aria-hidden="true" />} className="!px-4 !text-base">Delete</Button>}
            </div>
            <div className="flex gap-3">
              <button type="button" onClick={onClose} disabled={isPending || isDeleting} className="inline-flex min-h-11 items-center justify-center rounded-full px-4 text-base font-bold text-on-surface transition-colors hover:bg-surface-container-high active:bg-surface-container-highest focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tertiary disabled:cursor-not-allowed disabled:opacity-50">Cancel</button>
              <Button type="submit" disabled={isPending || isDeleting} variant="tonal" color="primary" className="!gap-4 !text-base">
                {isPending && <span className="h-4 w-4 animate-spin rounded-full border-2 border-outline border-t-on-primary-container" aria-hidden="true" />}
                {isPending ? (isEditing ? 'Saving…' : 'Publishing…') : (isEditing ? 'Save changes' : 'Publish')}
              </Button>
            </div>
          </footer>
        )}
      </form>
    </div>
  );
}

export function BulletinPostFormModal({ isOpen, ...dialogProps }: BulletinPostFormModalProps) {
  if (!isOpen) return null;
  return <BulletinPostFormDialog {...dialogProps} />;
}
