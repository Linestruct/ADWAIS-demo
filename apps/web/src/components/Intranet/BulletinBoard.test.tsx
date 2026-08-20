// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BulletinBoard } from './BulletinBoard';

const post = {
  id: 'post-1',
  title: 'Office update',
  body: 'The full bulletin post body.',
  createdAt: '2026-07-15T10:00:00Z',
  author: { id: 'author-1', name: 'Jane Doe' },
};

const secondPost = { ...post, id: 'post-2', title: 'Second update' };

const queryState = {
  data: { data: [post] },
  isLoading: false,
  isError: false,
  queryKey: ['/api/intranet/bulletin-posts'],
};

const createMutation = {
  isPending: false,
  mutate: vi.fn(),
};

const deleteMutation = {
  isPending: false,
  variables: undefined as { id: string } | undefined,
  mutate: vi.fn(),
};

const updateMutation = {
  isPending: false,
  mutate: vi.fn(),
};

const invalidateQueries = vi.fn();
const setQueryData = vi.fn();
const currentUser = {
  user: { id: 'author-1', name: 'Jane Doe', role: 'Employee' },
  role: 'Employee' as string,
};

vi.mock('../../api/generated/endpoints', () => ({
  useGetApiIntranetBulletinPosts: () => queryState,
  usePostApiIntranetBulletinPosts: () => createMutation,
  usePatchApiIntranetBulletinPostsId: () => updateMutation,
  useDeleteApiIntranetBulletinPostsId: () => deleteMutation,
}));

vi.mock('../../hooks/useCurrentUser', () => ({
  useCurrentUser: () => currentUser,
}));

vi.mock('@tanstack/react-query', async () => {
  const actual = await vi.importActual<typeof import('@tanstack/react-query')>('@tanstack/react-query');
  return {
    ...actual,
    useQueryClient: () => ({
      invalidateQueries,
      getQueryData: () => queryState.data,
      setQueryData,
    }),
  };
});

describe('BulletinBoard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryState.data = { data: [post] };
    queryState.isError = false;
    createMutation.isPending = false;
    deleteMutation.isPending = false;
    deleteMutation.variables = undefined;
    updateMutation.isPending = false;
    currentUser.role = 'Employee';
  });

  it('keeps the New button visible but disables it for unauthorized users', () => {
    currentUser.role = 'Viewer';
    render(<BulletinBoard />);

    expect(screen.getByRole('button', { name: 'New' })).toBeDisabled();
  });

  it('distinguishes an unavailable bulletin board from an empty one', () => {
    queryState.data = { data: [] };
    const { rerender } = render(<BulletinBoard />);
    expect(screen.getByText('No bulletin posts yet.')).toBeInTheDocument();

    queryState.isError = true;
    rerender(<BulletinBoard />);
    expect(screen.getByRole('alert')).toHaveTextContent('Bulletin board unavailable');
  });

  it('opens the edit modal with the existing values and patches the bulletin post', () => {
    render(<BulletinBoard />);

    fireEvent.click(screen.getByLabelText('Edit Office update'));
    const title = screen.getByLabelText('Title');
    const message = screen.getByLabelText('Message');
    expect(title).toHaveValue('Office update');
    expect(message).toHaveValue('The full bulletin post body.');

    fireEvent.change(title, { target: { value: 'Updated office update' } });
    fireEvent.change(message, { target: { value: 'Updated bulletin post body.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    expect(updateMutation.mutate).toHaveBeenCalledWith(
      { id: 'post-1', data: { title: 'Updated office update', body: 'Updated bulletin post body.' } },
      expect.any(Object),
    );
  });

  it('opens the create modal and submits the entered bulletin post', () => {
    render(<BulletinBoard />);

    fireEvent.click(screen.getByRole('button', { name: /new/i }));
    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'New title' } });
    fireEvent.change(screen.getByLabelText('Message'), { target: { value: 'New body' } });
    fireEvent.click(screen.getByRole('button', { name: 'Publish' }));

    expect(createMutation.mutate).toHaveBeenCalledWith(
      { data: { title: 'New title', body: 'New body' } },
      expect.any(Object),
    );
  });

  it('opens an bulletin post in a contained detail dialog and switches to the full list view', () => {
    queryState.data = { data: [post, secondPost] };
    render(<BulletinBoard />);

    const card = screen.getAllByRole('button', { name: /office update/i })[0];
    if (!card) throw new Error('BulletinPost card was not rendered');
    fireEvent.click(card);
    expect(screen.getByRole('dialog', { name: 'Office update' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Close bulletin post' }));

    fireEvent.click(screen.getByRole('button', { name: /view all/i }));
    expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
  });

  it('requires confirmation before deleting an bulletin post', () => {
    render(<BulletinBoard />);

    fireEvent.click(screen.getByLabelText('Delete Office update'));
    expect(screen.getByText('Delete this bulletin post?')).toBeInTheDocument();
    expect(deleteMutation.mutate).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: /^Delete$/ }));
    expect(deleteMutation.mutate).toHaveBeenCalledWith(
      { id: 'post-1' },
      expect.any(Object),
    );
  });

  it('restores the bulletin post when optimistic deletion fails', () => {
    render(<BulletinBoard />);

    fireEvent.click(screen.getByLabelText('Delete Office update'));
    fireEvent.click(screen.getByRole('button', { name: /^Delete$/ }));

    const options = deleteMutation.mutate.mock.calls[0]?.[1];
    options?.onError?.(new Error('Delete failed'));
    options?.onSettled?.();

    expect(setQueryData).toHaveBeenCalledTimes(2);
    expect(invalidateQueries).toHaveBeenCalled();
  });
});
