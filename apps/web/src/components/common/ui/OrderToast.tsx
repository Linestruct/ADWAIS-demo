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
      className="w-[calc(100vw-2rem)] lg:w-[480px] bg-primary-container rounded-xl lg:rounded-2xl p-3.5 lg:p-6 m3-elevation-2 flex items-center gap-5 lg:gap-6 animate-in fade-in slide-in-from-bottom-4 duration-300"
      onClick={(e) => e.stopPropagation()}
    >
      <div className="w-11 h-11 lg:w-16 lg:h-16 rounded-lg lg:rounded-xl bg-surface flex items-center justify-center shrink-0 overflow-hidden shadow-sm">
        {faviconUrl && !imgError ? (
          <img 
            src={faviconUrl} 
            alt="Tenant favicon" 
            className="w-6 h-6 lg:w-8 lg:h-8 object-contain" 
            onError={() => setImgError(true)}
          />
        ) : (
          <ShoppingBag className="w-6 h-6 lg:w-8 lg:h-8 text-on-surface-variant" />
        )}
      </div>
      {/* Order Info */}
      <div className="flex-grow min-w-0 text-left">
        <div className="text-sm uppercase font-black tracking-wide font-mono">
          New Order Placed
        </div>
        <div className="lg:text-xl font-black truncate mt-0.5 lg:mt-1">
          {order.tenantName || 'Unknown Tenant'}
        </div>
        <div className="text-sm mt-1 lg:mt-2 font-mono">
          {formatDateTime(order.createdDate, {
            day: 'numeric', month: 'long', year: 'numeric',
            hour: 'numeric', minute: 'numeric', hour12: false,
          }, 'en-SE')}
        </div>
        <div className="text-sm font-bold ">{displayValue}</div>
      </div>
      {/* Close Button */}
      <button 
        onClick={() => toast.dismiss(t)} 
        className="p-1.5 lg:p-2 hover:bg-surface/5 rounded-lg transition-colors cursor-pointer shrink-0"
      >
        <X className="w-5 h-5 lg:w-6 lg:h-6" />
      </button>
    </div>
  );
}
