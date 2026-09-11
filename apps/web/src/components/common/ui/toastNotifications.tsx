// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { toast } from 'sonner';
import { ErrorToast } from './ErrorToast';
import { SuccessToast } from './SuccessToast';

type ErrorToastOptions = { id?: string; duration?: number; description?: string };

export function showErrorToast(
  title: string,
  descriptionOrOptions: string | ErrorToastOptions = '',
  options?: ErrorToastOptions,
) {
  const description = typeof descriptionOrOptions === 'string'
    ? descriptionOrOptions
    : descriptionOrOptions.description ?? '';
  const toastOptions = typeof descriptionOrOptions === 'string'
    ? options
    : descriptionOrOptions;
  const sonnerOptions = toastOptions
    ? {
        ...(toastOptions.id === undefined ? {} : { id: toastOptions.id }),
        ...(toastOptions.duration === undefined ? {} : { duration: toastOptions.duration }),
      }
    : {};

  return toast.custom(
    (t) => <ErrorToast title={title} description={description} onClose={() => toast.dismiss(t)} />,
    {
      ...sonnerOptions,
      closeButton: false,
      dismissible: true,
      classNames: { toast: 'headless-toast-reset' },
    },
  );
}

export function showSuccessToast(title: string) {
  return toast.custom(
    (t) => <SuccessToast title={title} onClose={() => toast.dismiss(t)} />,
    {
      closeButton: false,
      dismissible: true,
      classNames: { toast: 'headless-toast-reset' },
    },
  );
}
