// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useState } from 'react';
import { toast } from 'sonner';
import { ShoppingBag, X } from 'lucide-react';
import type { OrderDto } from '@types';
import { formatDateTime } from '../../../utils/dateTime';

type OrderToastProps = {
  order: OrderDto;
  t: string | number;
  faviconUrl: string | null;
  displayValue: string;
};

export function OrderToast({ order, t, faviconUrl, displayValue }: OrderToastProps) {
  const [imgError, setImgError] = useState(false);

  return (
    <div
      className="relative flex w-[356px] max-w-[calc(100vw-2rem)] items-center gap-3 rounded-xl bg-primary-container p-4 pr-11 m3-elevation-2 animate-in fade-in slide-in-from-bottom-4 duration-300"
      onClick={(e) => e.stopPropagation()}
    >
      <div className="flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-surface shadow-sm">
        {faviconUrl && !imgError ? (
          <img 
            src={faviconUrl} 
            alt="Tenant favicon" 
            className="h-6 w-6 object-contain"
            onError={() => setImgError(true)}
          />
        ) : (
          <ShoppingBag className="h-6 w-6 text-on-surface-variant" />
        )}
      </div>
      {/* Order Info */}
      <div className="flex-grow min-w-0 text-left">
        <div className="font-mono text-sm font-black uppercase tracking-wide">
          New Order Placed
        </div>
        <div className="mt-0.5 truncate font-black">
          {order.tenantName || 'Unknown Tenant'}
        </div>
        <div className="mt-1 font-mono text-sm">
          {formatDateTime(order.createdDate, {
            day: 'numeric', month: 'long', year: 'numeric',
            hour: 'numeric', minute: 'numeric', hour12: false,
          }, 'en-SE')}
        </div>
        <div className="text-sm font-bold">{displayValue}</div>
      </div>
      {/* Close Button */}
      <button
        type="button"
        onClick={() => toast.dismiss(t)}
        aria-label="Close notification"
        className="absolute right-2 top-2 flex h-5 w-5 items-center justify-center rounded-md p-1 opacity-60 transition-opacity hover:bg-surface/5 hover:opacity-100"
      >
        <X size={12} strokeWidth={1.5} aria-hidden="true" />
      </button>
    </div>
  );
}
