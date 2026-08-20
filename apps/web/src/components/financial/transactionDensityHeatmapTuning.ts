// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

/**
 * Visual-only heatmap tuning. These settings never alter source buckets,
 * summaries, quality classification, or tooltip values.
 *
 * Blur runs before interpolation. Radius selects neighbouring source buckets;
 * sigma controls how strongly those neighbours contribute. For a neighbour at
 * distance `d`, its unnormalised weight is:
 *
 * `Math.exp(-(d * d) / (2 * sigma * sigma))`
 *
 * All included weights are then normalised to sum to `1`. Sigma is a
 * non-negative decimal; radius and interpolation-step counts are whole numbers.
 * A conventional Gaussian radius is approximately `Math.ceil(3 * sigma)`.
 */
export const TRANSACTION_DENSITY_HEATMAP_TUNING = {
  /**
   * Horizontal Gaussian sigma, measured in hourly buckets. Decimals are valid.
   * Smaller values concentrate weight on the current hour; larger values make
   * the included hours more evenly influential. `0` disables horizontal blur,
   * as does a horizontal radius of `0`.
   *
   * Typical starting points: `0.5` subtle, `0.8` moderate, `1.25` broad.
   */
  horizontalBlurSigma: 0.5,

  /**
   * Whole hourly buckets sampled on each side. A radius of `2` produces the
   * offsets `[-2, -1, 0, 1, 2]`. Fractional values are truncated to prevent
   * asymmetric kernels; use `0`, `1`, `2`, etc. `0` disables horizontal blur
   * regardless of sigma.
   */
  horizontalBlurRadius: 2,

  /**
   * Vertical Gaussian sigma, measured in weekday buckets. Decimals are valid.
   * Smaller values concentrate weight on the current weekday; larger values
   * make included weekdays more evenly influential. `0` disables vertical blur,
   * as does a vertical radius of `0`.
   */
  verticalBlurSigma: 0,

  /**
   * Whole weekday buckets sampled on each side. A radius of `1` produces the
   * offsets `[-1, 0, 1]`. Fractional values are truncated to prevent asymmetric
   * kernels; use `0`, `1`, `2`, etc. `0` disables vertical blur regardless of
   * sigma.
   */
  verticalBlurRadius: 0,

  /**
   * Portion of the distance between hourly bucket centres that blends.
   * Clamped to `0–1`: `0` gives a hard edge and `1` blends the full distance.
   * The blend follows a smoothstep curve before optional step quantisation.
   */
  horizontalTransitionWidth: 1,

  /**
   * Portion of the distance between weekday bucket centres that blends.
   * Clamped to `0–1`: `0` gives a hard edge and `1` blends the full distance.
   * The blend follows a smoothstep curve before optional step quantisation.
   */
  verticalTransitionWidth: 0.5,

  /**
   * Rescales the blurred matrix to the unblurred matrix's minimum and maximum.
   * This retains palette contrast after blur but does not restore individual
   * bucket values; tooltips always use the original values either way.
   */
  preserveValueRange: true,

  /**
   * Allows horizontal blur to cross the boundary between hour 23 and hour 00.
   * When false, out-of-range samples clamp to the nearest edge hour.
   */
  wrapHours: true,

  /**
   * Allows vertical blur to cross the boundary between Sunday and Monday.
   * When false, out-of-range samples clamp to the nearest edge weekday.
   */
  wrapWeekdays: true,

  /**
   * Spatial interpolation steps for sparse samples. `1` is coarsest; larger
   * whole numbers make finer blocks. `null` selects continuous interpolation.
   * Fractional values are truncated. This affects presentation, not source data.
   */
  sparseInterpolationSteps: 5,

  /**
   * Spatial interpolation steps for indicative samples. `1` is coarsest; larger
   * whole numbers make finer blocks. `null` selects continuous interpolation.
   * Fractional values are truncated. This affects presentation, not source data.
   */
  indicativeInterpolationSteps: 7,
} as const;
