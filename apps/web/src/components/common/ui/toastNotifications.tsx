// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { toast } from 'sonner';
import { ErrorToast } from './ErrorToast';

export function showErrorToast(
  title: string,
  description: string,
  options?: { id?: string; duration?: number },
) {
  return toast.custom(
    (t) => <ErrorToast title={title} description={description} onClose={() => toast.dismiss(t)} />,
    {
      ...options,
      closeButton: false,
      dismissible: true,
      classNames: { toast: 'headless-toast-reset' },
    },
  );
}
