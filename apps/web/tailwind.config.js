// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import plugin from 'tailwindcss/plugin'

const withOpacity = (variable, defaultOpacity = 1) => ({ opacityValue }) =>
  `rgba(var(${variable}), ${opacityValue ?? defaultOpacity})`;

const halveCssLength = (value) => {
  const match = String(value).trim().match(/^(-?(?:\d+|\d*\.\d+))([a-z%]*)$/i);

  if (!match) {
    throw new Error(`Flex gap fallback cannot halve the spacing value "${value}" at build time.`);
  }

  return `${Number(match[1]) / 2}${match[2]}`;
};

/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Material 3 Semantic Colors mapped directly to CSS variables
        primary: withOpacity('--md-sys-color-primary-rgb'),
        'on-primary': withOpacity('--md-sys-color-on-primary-rgb'),
        'primary-container': withOpacity('--md-sys-color-primary-container-rgb'),
        'on-primary-container': withOpacity('--md-sys-color-on-primary-container-rgb'),
        secondary: withOpacity('--md-sys-color-secondary-rgb'),
        'on-secondary': withOpacity('--md-sys-color-on-secondary-rgb'),
        'secondary-container': withOpacity('--md-sys-color-secondary-container-rgb'),
        'on-secondary-container': withOpacity('--md-sys-color-on-secondary-container-rgb'),
        tertiary: withOpacity('--md-sys-color-tertiary-rgb'),
        'on-tertiary': withOpacity('--md-sys-color-on-tertiary-rgb'),
        'tertiary-container': withOpacity('--md-sys-color-tertiary-container-rgb'),
        'on-tertiary-container': withOpacity('--md-sys-color-on-tertiary-container-rgb'),
        error: withOpacity('--md-sys-color-error-rgb'),
        'on-error': withOpacity('--md-sys-color-on-error-rgb'),
        'error-container': withOpacity('--md-sys-color-error-container-rgb'),
        'on-error-container': withOpacity('--md-sys-color-on-error-container-rgb'),
        success: withOpacity('--md-sys-color-success-rgb'),
        'on-success': withOpacity('--md-sys-color-on-success-rgb'),
        'success-container': withOpacity('--md-sys-color-success-container-rgb'),
        'on-success-container': withOpacity('--md-sys-color-on-success-container-rgb'),
        warning: withOpacity('--md-sys-color-warning-rgb'),
        'on-warning': withOpacity('--md-sys-color-on-warning-rgb'),
        'warning-container': withOpacity('--md-sys-color-warning-container-rgb'),
        'on-warning-container': withOpacity('--md-sys-color-on-warning-container-rgb'),
        background: withOpacity('--md-sys-color-background-rgb'),
        'on-background': withOpacity('--md-sys-color-on-background-rgb'),
        surface: withOpacity('--md-sys-color-surface-rgb'),
        'on-surface': withOpacity('--md-sys-color-on-surface-rgb'),
        'on-surface-variant': withOpacity('--md-sys-color-on-surface-variant-rgb'),
        'surface-container-lowest': withOpacity('--md-sys-color-surface-container-lowest-rgb'),
        'surface-container-low': withOpacity('--md-sys-color-surface-container-low-rgb'),
        'surface-container': withOpacity('--md-sys-color-surface-container-rgb'),
        'surface-container-high': withOpacity('--md-sys-color-surface-container-high-rgb'),
        'surface-container-highest': withOpacity('--md-sys-color-surface-container-highest-rgb'),
        'surface-dim': withOpacity('--md-sys-color-surface-dim-rgb'),
        'surface-bright': withOpacity('--md-sys-color-surface-bright-rgb'),
        outline: withOpacity('--md-sys-color-outline-rgb'),
        'outline-variant': withOpacity('--md-sys-color-outline-variant-rgb'),
        'inverse-surface': withOpacity('--md-sys-color-inverse-surface-rgb'),
        'inverse-on-surface': withOpacity('--md-sys-color-inverse-on-surface-rgb'),
        'inverse-primary': withOpacity('--md-sys-color-inverse-primary-rgb'),
        // Legacy Brand mappings mapped to CSS variables
        'brand-bg-primary': withOpacity('--color-brand-bg-primary-rgb'),
        'brand-bg-secondary': withOpacity('--color-brand-bg-secondary-rgb'),
        'brand-bg-tertiary': withOpacity('--color-brand-bg-tertiary-rgb'),
        'brand-bg-quaternary': withOpacity('--color-brand-bg-quaternary-rgb'),
        'brand-btn-primary': withOpacity('--color-brand-btn-primary-rgb'),
        'brand-btn-border-primary': withOpacity('--color-brand-btn-border-primary-rgb'),
        'brand-btn-secondary': withOpacity('--color-brand-btn-secondary-rgb'),
        'brand-btn-border-secondary': withOpacity('--color-brand-btn-border-secondary-rgb'),
        'brand-btn-quaternary': withOpacity('--color-brand-btn-quaternary-rgb'),
        'brand-text': withOpacity('--color-brand-text-rgb'),
        'brand-accent': withOpacity('--color-brand-accent-rgb'),
        'brand-link': withOpacity('--color-brand-link-rgb'),
        'brand-hover': withOpacity('--color-brand-hover-rgb'),
        'brand-active': withOpacity('--color-brand-active-rgb'),
        'calendar-today-bg': withOpacity('--color-calendar-today-bg-rgb'),
        'status-up': {
          DEFAULT: withOpacity('--color-status-up-rgb'),
          container: withOpacity('--color-status-up-container-rgb', 0.7),
          'container-hover': withOpacity('--color-status-up-container-rgb', 0.9),
          'on-container': withOpacity('--color-status-up-on-container-rgb'),
          muted: withOpacity('--color-status-up-muted-rgb'),
        },
        'status-down': {
          DEFAULT: withOpacity('--color-status-down-rgb'),
          container: withOpacity('--color-status-down-container-rgb', 0.7),
          'container-hover': withOpacity('--color-status-down-container-rgb', 0.9),
          'on-container': withOpacity('--color-status-down-on-container-rgb'),
          muted: withOpacity('--color-status-down-muted-rgb'),
        },
        'status-degraded': {
          DEFAULT: withOpacity('--color-status-degraded-rgb'),
          container: withOpacity('--color-status-degraded-container-rgb', 0.7),
          'container-hover': withOpacity('--color-status-degraded-container-rgb', 0.9),
          'on-container': withOpacity('--color-status-degraded-on-container-rgb'),
          muted: withOpacity('--color-status-degraded-muted-rgb'),
        },
        'status-paused': {
          DEFAULT: withOpacity('--color-status-paused-rgb'),
          container: withOpacity('--color-status-paused-container-rgb', 0.7),
          'container-hover': withOpacity('--color-status-paused-container-rgb', 0.9),
          'on-container': withOpacity('--color-status-paused-on-container-rgb'),
          muted: withOpacity('--color-status-paused-muted-rgb'),
        },
        'status-starting': {
          DEFAULT: withOpacity('--color-status-starting-rgb'),
          container: withOpacity('--color-status-starting-container-rgb', 0.7),
          'container-hover': withOpacity('--color-status-starting-container-rgb', 0.9),
          'on-container': withOpacity('--color-status-starting-on-container-rgb'),
          muted: withOpacity('--color-status-starting-muted-rgb'),
        },
        'status-unknown': {
          DEFAULT: withOpacity('--color-status-unknown-rgb'),
          container: withOpacity('--color-status-unknown-container-rgb', 0.7),
          'container-hover': withOpacity('--color-status-unknown-container-rgb', 0.9),
          'on-container': withOpacity('--color-status-unknown-on-container-rgb'),
          muted: withOpacity('--color-status-unknown-muted-rgb'),
        },
        'chart-grid': withOpacity('--color-chart-grid-rgb'),
        'chart-tick': withOpacity('--color-chart-tick-rgb'),
        'chart-prev-line': withOpacity('--color-chart-prev-line-rgb'),
        'chart-label': withOpacity('--color-chart-label-rgb'),
        'chart-1': withOpacity('--color-chart-1-rgb'),
        'chart-2': withOpacity('--color-chart-2-rgb'),
        'chart-3': withOpacity('--color-chart-3-rgb'),
        growth: withOpacity('--color-growth-rgb'),
        decline: withOpacity('--color-decline-rgb'),
        'decline-warning': withOpacity('--color-decline-warning-rgb'),
        'console-bg': withOpacity('--color-console-bg-rgb'),
        'console-header': withOpacity('--color-console-header-rgb'),
        'console-border': withOpacity('--color-console-border-rgb'),
        'console-hover': withOpacity('--color-console-hover-rgb'),
        'console-hover-dark': withOpacity('--color-console-hover-dark-rgb'),
        'console-text': withOpacity('--color-console-text-rgb'),
        'console-icon': withOpacity('--color-console-icon-rgb'),
        'console-blue': withOpacity('--color-console-blue-rgb'),
        'console-green': withOpacity('--color-console-green-rgb'),
        'console-yellow': withOpacity('--color-console-yellow-rgb'),
        'console-red': withOpacity('--color-console-red-rgb'),
        'console-error-bg': withOpacity('--color-console-error-bg-rgb'),
        'console-error-text': withOpacity('--color-console-error-text-rgb'),
      },
      fontFamily: {
        sans: ['Manrope', 'system-ui', 'sans-serif'],
        display: ['Manrope', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'SFMono-Regular', 'Menlo', 'monospace'],
      },
      boxShadow: {
        card: '0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)',
        'elevation-1': '0 1px 2px 0 rgba(0, 0, 0, 0.3), 0 1px 3px 1px rgba(0, 0, 0, 0.15)',
        'elevation-2': '0 1px 2px 0 rgba(0, 0, 0, 0.3), 0 2px 6px 2px rgba(0, 0, 0, 0.15)',
        'elevation-3': '0 1px 3px 0 rgba(0, 0, 0, 0.3), 0 4px 8px 3px rgba(0, 0, 0, 0.15)',
        'elevation-4': '0 2px 3px 0 rgba(0, 0, 0, 0.3), 0 6px 10px 4px rgba(0, 0, 0, 0.15)',
        'elevation-5': '0 4px 4px 0 rgba(0, 0, 0, 0.3), 0 8px 12px 6px rgba(0, 0, 0, 0.15)',
        'status-up-glow': '0 0 8px var(--color-status-up-glow)',
        'status-down-glow': '0 0 8px var(--color-status-down-glow)',
        'status-degraded-glow': '0 0 8px var(--color-status-degraded-glow)',
        'status-unknown-glow': '0 0 8px var(--color-status-unknown-glow)',
        'status-paused-glow': '0 0 6px var(--color-status-paused-glow)',
        'status-starting-glow': '0 0 6px var(--color-status-starting-glow)',
      },
      screens: {
        'contained': { 'raw': '(min-width: 1024px) and (min-height: 800px) and (orientation: landscape), (min-width: 1024px) and (min-height: 1350px) and (orientation: portrait)' },
        'landscape-lg': { 'raw': '(orientation: landscape) and (min-width: 1024px)' },
        'portrait-lg': { 'raw': '(orientation: portrait) and (min-width: 1024px)' },
        'landscape-contained': { 'raw': '(orientation: landscape) and (min-width: 1024px) and (min-height: 800px)' },
        'portrait-contained': { 'raw': '(orientation: portrait) and (min-width: 1024px) and (min-height: 1350px)' }
      }
    },
  },
  plugins: [
    plugin(function({ addComponents, theme, e }) {
      const spacing = theme('spacing') || {};
      const screens = theme('screens') || {};
      const flexGapComponents = {};

      const getMediaQuery = (screen) => {
        if (typeof screen === 'string') return `(min-width: ${screen})`;
        if (screen?.raw) return screen.raw;

        const conditions = [];
        if (screen?.min) conditions.push(`(min-width: ${screen.min})`);
        if (screen?.max) conditions.push(`(max-width: ${screen.max})`);
        return conditions.join(' and ');
      };
      
      Object.entries(spacing).forEach(([key, value]) => {
        const escapedKey = key.replace('.', '\\.');
        const halfValue = halveCssLength(value);

        // Store the parent's requested gap on each child. The fallback margin
        // is applied to the child, so resolving a variable from the container
        // breaks whenever that child is itself another `.flex` element.
        flexGapComponents[`.flex.gap-${escapedKey} > *`] = {
          '--tw-parent-flex-gap-row': value,
          '--tw-parent-flex-gap-column': value,
        };
        flexGapComponents[`.flex.gap-x-${escapedKey} > *`] = {
          '--tw-parent-flex-gap-column': value,
        };
        flexGapComponents[`.flex.gap-y-${escapedKey} > *`] = {
          '--tw-parent-flex-gap-row': value,
        };

        // Wrap Flex Gap Fallback (Negative Margin Hack)
        flexGapComponents[`.flex.flex-wrap.gap-${escapedKey}`] = {
          'margin': `-${halfValue}`,
        };
        flexGapComponents[`.flex.flex-wrap.gap-x-${escapedKey}`] = {
          'margin-left': `-${halfValue}`,
          'margin-right': `-${halfValue}`,
        };
        flexGapComponents[`.flex.flex-wrap.gap-y-${escapedKey}`] = {
          'margin-top': `-${halfValue}`,
          'margin-bottom': `-${halfValue}`,
        };

        // Children positive margins
        flexGapComponents[`.flex.flex-wrap.gap-${escapedKey} > *`] = {
          'margin': halfValue,
        };
        flexGapComponents[`.flex.flex-wrap.gap-x-${escapedKey} > *`] = {
          'margin-left': halfValue,
          'margin-right': halfValue,
        };
        flexGapComponents[`.flex.flex-wrap.gap-y-${escapedKey} > *`] = {
          'margin-top': halfValue,
          'margin-bottom': halfValue,
        };

        // Re-assert wrapping margins on siblings (overrides non-wrapping sibling rules)
        flexGapComponents[`.flex.flex-wrap.gap-${escapedKey} > :not([hidden]) ~ :not([hidden])`] = {
          'margin': halfValue,
        };
        flexGapComponents[`.flex.flex-wrap.gap-x-${escapedKey} > :not([hidden]) ~ :not([hidden])`] = {
          'margin-left': halfValue,
        };
        flexGapComponents[`.flex.flex-wrap.gap-y-${escapedKey} > :not([hidden]) ~ :not([hidden])`] = {
          'margin-top': halfValue,
        };
      });

      const visibleSibling = '> :not([hidden]) ~ :not([hidden])';
      flexGapComponents['.flex > *'] = {
        '--tw-parent-flex-gap-row': '0px',
        '--tw-parent-flex-gap-column': '0px',
      };
      flexGapComponents[`.flex:not(.flex-wrap) ${visibleSibling}`] = {
        'margin-top': '0px',
        'margin-left': 'var(--tw-parent-flex-gap-column, 0px)',
      };
      flexGapComponents[`.flex.flex-col:not(.flex-wrap) ${visibleSibling}`] = {
        'margin-top': 'var(--tw-parent-flex-gap-row, 0px)',
        'margin-left': '0px',
      };

      // Respect every configured Tailwind screen, including raw custom screens
      // such as `contained`, when the responsive utility changes direction.
      Object.entries(screens).forEach(([bp, screen]) => {
        const mediaQuery = getMediaQuery(screen);
        if (!mediaQuery) return;

        const bpKey = `@media ${mediaQuery}`;
        flexGapComponents[bpKey] = flexGapComponents[bpKey] || {};
        const rowClass = e(`${bp}:flex-row`);
        const colClass = e(`${bp}:flex-col`);

        flexGapComponents[bpKey][`.flex.flex-col.${rowClass}:not(.flex-wrap) ${visibleSibling}`] = {
          'margin-top': '0px',
          'margin-left': 'var(--tw-parent-flex-gap-column, 0px)',
        };
        flexGapComponents[bpKey][`.flex.${colClass}:not(.flex-wrap) ${visibleSibling}`] = {
          'margin-top': 'var(--tw-parent-flex-gap-row, 0px)',
          'margin-left': '0px',
        };
      });

      // Native flex gap and margin fallbacks must never run together. A small
      // runtime capability test adds this class only on affected browsers.
      const scopedFlexGapComponents = {};
      Object.entries(flexGapComponents).forEach(([selector, rules]) => {
        if (selector.startsWith('@media')) {
          scopedFlexGapComponents[selector] = Object.fromEntries(
            Object.entries(rules).map(([mediaSelector, mediaRules]) => [
              `.no-flexbox-gap ${mediaSelector}`,
              mediaRules,
            ]),
          );
        } else {
          scopedFlexGapComponents[`.no-flexbox-gap ${selector}`] = rules;
        }
      });

      addComponents(scopedFlexGapComponents);
    }),
    plugin(function({ addUtilities}) {
      const elevations = {
        '.m3-elevation-0': {
          boxShadow: 'none',
          backgroundImage: 'none',
        },
        '.m3-elevation-1': {
          boxShadow: '0 1px 2px 0 rgba(0, 0, 0, 0.3), 0 1px 3px 1px rgba(0, 0, 0, 0.15)',
        },
        '.m3-elevation-2': {
          boxShadow: '0 1px 2px 0 rgba(0, 0, 0, 0.3), 0 2px 6px 2px rgba(0, 0, 0, 0.15)',
        },
        '.m3-elevation-3': {
          boxShadow: '0 1px 3px 0 rgba(0, 0, 0, 0.3), 0 4px 8px 3px rgba(0, 0, 0, 0.15)',
        },
        '.m3-elevation-4': {
          boxShadow: '0 2px 3px 0 rgba(0, 0, 0, 0.3), 0 6px 10px 4px rgba(0, 0, 0, 0.15)',
        },
        '.m3-elevation-5': {
          boxShadow: '0 4px 4px 0 rgba(0, 0, 0, 0.3), 0 8px 12px 6px rgba(0, 0, 0, 0.15)',
        },
      };

      addUtilities(elevations, ['hover', 'focus', 'responsive']);
    }),
  ],
}
