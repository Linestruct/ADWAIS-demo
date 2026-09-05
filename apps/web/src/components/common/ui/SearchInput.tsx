// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { Search } from 'lucide-react';

interface SearchInputProps {
  value: string;
  onChange: (val: string) => void;
  placeholder?: string;
  className?: string;
}

export function SearchInput({ value, onChange, placeholder = 'Search...', className = '' }: SearchInputProps) {
  return (
    <div className={`relative min-w-0 ${className}`}>
      <Search className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-on-surface-variant" size={18} aria-hidden="true" />
      <input 
        type="text" 
        placeholder={placeholder} 
        aria-label={placeholder}
        value={value}
        onChange={e => onChange(e.target.value)}
        className="h-11 w-44 rounded-full border border-outline-variant bg-surface-container-low pl-10 pr-4 text-base font-semibold text-on-surface outline-none transition-colors placeholder:text-on-surface-variant hover:bg-surface-container focus:ring-2 focus:ring-secondary sm:w-52"
      />
    </div>
  );
}
