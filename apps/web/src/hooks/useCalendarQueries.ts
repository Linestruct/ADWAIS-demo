// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useQueryClient } from '@tanstack/react-query';
import { 
  useGetApiIntranetEvents, 
  usePostApiIntranetEvents, 
  usePatchApiIntranetEventsId, 
  useDeleteApiIntranetEventsId,
  useGetApiIntranetCalendarToken,
  usePostApiIntranetCalendarTokenRegenerate,
  useGetApiIntranetCalendarSubscriptions,
  usePostApiIntranetCalendarSubscriptions,
  useDeleteApiIntranetCalendarSubscriptionsId,
  usePostApiIntranetCalendarSubscriptionsIdSync
} from '../api/generated/endpoints';
import type { 
  CalendarEventDto,
  CreateCalendarEventDto,
  UpdateCalendarEventDto,
  CalendarSubscriptionDto,
  CreateCalendarSubscriptionDto,
  CalendarTokenDto
} from '@types';
import { toast } from 'sonner';

export function useCalendarEventsQuery(start?: string, end?: string) {
  const params = start && end ? { start, end } : undefined;
  return useGetApiIntranetEvents<CalendarEventDto[], Error>(params, {
    query: {
      queryKey: ['calendar-events', start, end],
      select: (res) => res.data as CalendarEventDto[]
    }
  });
}

export function useCreateCalendarEventMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = usePostApiIntranetEvents<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Calendar event created successfully.');
        queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
        queryClient.invalidateQueries({ queryKey: ['todays-events'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to create event', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (dto: CreateCalendarEventDto) =>
      mutation.mutate({ data: dto })
  };
}

export function useUpdateCalendarEventMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = usePatchApiIntranetEventsId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Calendar event updated successfully.');
        queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
        queryClient.invalidateQueries({ queryKey: ['todays-events'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to update event', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (variables: { id: string; dto: UpdateCalendarEventDto }) =>
      mutation.mutate({ id: variables.id, data: variables.dto })
  };
}

export function useDeleteCalendarEventMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = useDeleteApiIntranetEventsId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Calendar event deleted successfully.');
        queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
        queryClient.invalidateQueries({ queryKey: ['todays-events'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to delete event', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (id: string) => mutation.mutate({ id })
  };
}

export function useCalendarTokenQuery(enabled = true) {
  return useGetApiIntranetCalendarToken<CalendarTokenDto, Error>({
    query: {
      queryKey: ['calendar-token'],
      enabled,
      select: (res) => res.data as CalendarTokenDto
    }
  });
}

export function useRegenerateCalendarTokenMutation() {
  const queryClient = useQueryClient();
  return usePostApiIntranetCalendarTokenRegenerate<Error>({
    mutation: {
      onSuccess: (res: { data: CalendarTokenDto }) => {
        toast.success('Feed token regenerated.');
        if (res?.data) {
          queryClient.setQueryData(['calendar-token'], res);
        } else {
          queryClient.invalidateQueries({ queryKey: ['calendar-token'] });
        }
      },
      onError: (err: Error) => {
        toast.error('Failed to regenerate token', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });
}

export function useCalendarSubscriptionsQuery() {
  return useGetApiIntranetCalendarSubscriptions<CalendarSubscriptionDto[], Error>({
    query: {
      queryKey: ['calendar-subscriptions'],
      select: (res) => res.data as CalendarSubscriptionDto[]
    }
  });
}

export function useCreateCalendarSubscriptionMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = usePostApiIntranetCalendarSubscriptions<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Subscription added successfully.');
        queryClient.invalidateQueries({ queryKey: ['calendar-subscriptions'] });
        queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to add subscription', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (dto: CreateCalendarSubscriptionDto) => 
      mutation.mutate({ data: dto })
  };
}

export function useDeleteCalendarSubscriptionMutation(onSuccessCallback?: () => void) {
  const queryClient = useQueryClient();
  const mutation = useDeleteApiIntranetCalendarSubscriptionsId<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Subscription deleted.');
        queryClient.invalidateQueries({ queryKey: ['calendar-subscriptions'] });
        queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
        if (onSuccessCallback) onSuccessCallback();
      },
      onError: (err: Error) => {
        toast.error('Failed to delete subscription', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (id: string) => mutation.mutate({ id })
  };
}

export function useSyncCalendarSubscriptionMutation() {
  const queryClient = useQueryClient();
  const mutation = usePostApiIntranetCalendarSubscriptionsIdSync<Error>({
    mutation: {
      onSuccess: () => {
        toast.success('Calendar synchronization triggered.');
        queryClient.invalidateQueries({ queryKey: ['calendar-subscriptions'] });
        queryClient.invalidateQueries({ queryKey: ['calendar-events'] });
      },
      onError: (err: Error) => {
        toast.error('Failed to trigger sync', {
          description: err.message || String(err),
          duration: Infinity
        });
      }
    }
  });

  return {
    ...mutation,
    mutate: (id: string) => mutation.mutate({ id })
  };
}
