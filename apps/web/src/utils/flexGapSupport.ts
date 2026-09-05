// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

/** Enables the margin-based flex-gap fallback only when the browser needs it. */
export function applyFlexGapFallbackClass() {
  const flex = document.createElement('div');
  flex.style.position = 'absolute';
  flex.style.visibility = 'hidden';
  flex.style.display = 'flex';
  flex.style.flexDirection = 'column';
  flex.style.rowGap = '1px';

  const firstChild = document.createElement('div');
  const secondChild = document.createElement('div');
  firstChild.style.height = '1px';
  secondChild.style.height = '1px';
  flex.append(firstChild, secondChild);
  document.body.appendChild(flex);

  const supportsFlexGap = flex.scrollHeight === 3;
  flex.remove();
  document.documentElement.classList.toggle('no-flexbox-gap', !supportsFlexGap);
}
