// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQueryClient } from '@tanstack/react-query';
import { 
   useGetApiUsers, 
   usePostApiUsers, 
   usePatchApiUsersId, 
   useDeleteApiUsersId 
 } from '../api/generated/endpoints';
import type { UserResponseDto, UserRole } from '@types';
import { toast } from 'sonner';

export function useUsersQuery() {
  return useGetApiUsers<UserResponseDto[], Error>({
    query: {
      queryKey: ['users'],
      select: (res) => res.data as UserResponseDto[]
    }
  });
}

export function useCreateUserMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = usePostApiUsers<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('User created successfully.');
        queryClient.invalidateQueries({ queryKey: ['users'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to create user', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (
      user: { email: string; role: string },
      options?: Parameters<typeof mutation.mutate>[1]
    ) => 
      mutation.mutate({ 
        data: {
          email: user.email,
          role: user.role as UserRole
        }
      }, options),
  };
}

export function useUpdateUserMutation() {
  const queryClient = useQueryClient();
  const mutation = usePatchApiUsersId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('User updated successfully.');
        queryClient.invalidateQueries({ queryKey: ['users'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to update user', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (
      variables: { id: string; payload: Partial<UserResponseDto> },
      options?: Parameters<typeof mutation.mutate>[1]
    ) => 
      mutation.mutate({ 
        id: variables.id, 
        data: {
          name: variables.payload.name,
          role: variables.payload.role as UserRole
        }
      }, options),
    mutateAsync: async (variables: { id: string; payload: Partial<UserResponseDto> }) => {
      await mutation.mutateAsync({
        id: variables.id,
        data: {
          name: variables.payload.name,
          role: variables.payload.role as UserRole,
        },
      });
    },
  };
}

export function useDeleteUserMutation() {
  const queryClient = useQueryClient();
  const mutation = useDeleteApiUsersId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('User deleted successfully.');
        queryClient.invalidateQueries({ queryKey: ['users'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to delete user', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (id: string, options?: Parameters<typeof mutation.mutate>[1]) => 
      mutation.mutate({ id }, options)
  };
}
