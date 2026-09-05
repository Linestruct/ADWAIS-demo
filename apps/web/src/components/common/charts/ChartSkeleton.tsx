// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT


export function ChartSkeleton() {
  return (
    <div className="w-full h-full min-h-[300px] flex flex-col justify-end gap-4 p-4 pt-10 animate-pulse">
      <div className="w-full flex-1 flex items-end gap-4">
        <div className="w-full bg-surface-container rounded-t-md h-[40%]"></div>
        <div className="w-full bg-surface-container rounded-t-md h-[70%]"></div>
        <div className="w-full bg-surface-container rounded-t-md h-[50%]"></div>
        <div className="w-full bg-surface-container rounded-t-md h-[90%]"></div>
        <div className="w-full bg-surface-container rounded-t-md h-[60%]"></div>
        <div className="w-full bg-surface-container rounded-t-md h-[80%]"></div>
        <div className="w-full bg-surface-container rounded-t-md h-[100%]"></div>
      </div>
      <div className="w-full h-6 border-t border-outline-variant flex justify-between pt-2">
        <div className="w-8 h-3 bg-surface-container rounded"></div>
        <div className="w-8 h-3 bg-surface-container rounded"></div>
        <div className="w-8 h-3 bg-surface-container rounded"></div>
      </div>
    </div>
  );
}
