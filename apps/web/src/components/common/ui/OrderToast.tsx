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
      className="relative flex w-[356px] max-w-[calc(100vw-2rem)] items-center gap-3 rounded-xl bg-primary-container p-4 font-['Manrope',_sans-serif] text-sm sm:w-[400px] sm:text-base m3-elevation-2 animate-in fade-in slide-in-from-bottom-4 duration-300"
      onClick={(e) => e.stopPropagation()}
    >
      <div className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-surface shadow-sm">
        {faviconUrl && !imgError ? (
          <img 
            src={faviconUrl} 
            alt="Tenant favicon" 
            className="h-7 w-7 object-contain"
            onError={() => setImgError(true)}
          />
        ) : (
          <ShoppingBag className="h-7 w-7 text-on-surface-variant" />
        )}
      </div>
      {/* Order Info */}
      <div className="flex-grow min-w-0 pr-5 text-left">
        <div className="text-xs font-bold uppercase tracking-wide sm:text-sm">
          New Order Placed
        </div>
        <div className="mt-0.5 truncate text-base font-extrabold sm:text-lg">
          {order.tenantName || 'Unknown Tenant'}
        </div>
        <div className="mt-1 text-sm font-medium sm:text-base">
          {formatDateTime(order.createdDate, {
            day: 'numeric', month: 'long', year: 'numeric',
            hour: 'numeric', minute: 'numeric', hour12: false,
          }, 'en-SE')}
        </div>
        <div className="text-sm font-bold sm:text-base">{displayValue}</div>
      </div>
      {/* Close Button */}
      <button
        type="button"
        onClick={() => toast.dismiss(t)}
        aria-label="Close notification"
        style={{ position: 'absolute', top: '0.75rem', right: '0.75rem' }}
        className="absolute right-3 top-3 flex h-7 w-7 items-center justify-center rounded-md p-1 opacity-60 transition-opacity hover:bg-surface/5 hover:opacity-100"
      >
        <X size={18} strokeWidth={1.75} aria-hidden="true" />
      </button>
    </div>
  );
}
