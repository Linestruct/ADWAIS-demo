// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

export const getTagColor = (tag: string): string => {
  const parts = tag.split(':');
  if (parts.length > 1 && parts[1]) {
    return parts[1].toLowerCase();
  }
  const tagName = parts[0].trim().toUpperCase();
  const colors = ['blue', 'green', 'red', 'orange', 'yellow', 'purple'];
  let hash = 0;
  for (let i = 0; i < tagName.length; i++) {
    hash = tagName.charCodeAt(i) + ((hash << 5) - hash);
  }
  const index = Math.abs(hash) % colors.length;
  return colors[index];
};

export const getTagStyle = (color: string): string => {
  switch (color?.toLowerCase()) {
    case 'blue':
      return 'bg-blue-50 text-blue-700 border-blue-200';
    case 'green':
      return 'bg-green-50 text-green-700 border-green-200';
    case 'red':
      return 'bg-red-50 text-red-700 border-red-200';
    case 'orange':
      return 'bg-orange-50 text-orange-700 border-orange-200';
    case 'yellow':
      return 'bg-yellow-50 text-yellow-700 border-yellow-200';
    case 'purple':
      return 'bg-purple-50 text-purple-700 border-purple-200';
    case 'grey':
    case 'slate':
    default:
      return 'bg-surface-container-low text-on-surface-variant border-outline-variant';
  }
};
