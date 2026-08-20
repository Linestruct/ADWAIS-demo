// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { memo, useEffect, useRef } from 'react';
import { TRANSACTION_DENSITY_PALETTE } from './transactionDensityPalette';
import { TRANSACTION_DENSITY_HEATMAP_TUNING } from './transactionDensityHeatmapTuning';

const MAX_RENDER_WIDTH = 1200;
const MAX_RENDER_HEIGHT = 600;

const paletteRgb = TRANSACTION_DENSITY_PALETTE.map(hex => [
  parseInt(hex.slice(1, 3), 16),
  parseInt(hex.slice(3, 5), 16),
  parseInt(hex.slice(5, 7), 16),
] as const);

interface TransactionDensityHeatmapCanvasProps {
  values: number[][];
  min: number;
  max: number;
  interpolationSteps?: number | null;
}

function createGaussianKernel(sigma: number, radius: number) {
  const normalizedSigma = Math.max(0, sigma);
  const normalizedRadius = Math.max(0, Math.trunc(radius));
  if (normalizedSigma === 0 || normalizedRadius === 0) return [1];

  const kernel = Array.from({ length: normalizedRadius * 2 + 1 }, (_, index) => {
    const offset = index - normalizedRadius;
    return Math.exp(-(offset * offset) / (2 * normalizedSigma * normalizedSigma));
  });
  const total = kernel.reduce((sum, weight) => sum + weight, 0);
  return kernel.map(weight => weight / total);
}

function resolveIndex(index: number, length: number, wrap: boolean) {
  if (wrap) return ((index % length) + length) % length;
  return Math.max(0, Math.min(length - 1, index));
}

function blurValues(values: number[][]) {
  const rowCount = values.length;
  const columnCount = values[0].length;
  const horizontalKernel = createGaussianKernel(
    TRANSACTION_DENSITY_HEATMAP_TUNING.horizontalBlurSigma,
    TRANSACTION_DENSITY_HEATMAP_TUNING.horizontalBlurRadius,
  );
  const verticalKernel = createGaussianKernel(
    TRANSACTION_DENSITY_HEATMAP_TUNING.verticalBlurSigma,
    TRANSACTION_DENSITY_HEATMAP_TUNING.verticalBlurRadius,
  );
  const horizontalRadius = Math.floor(horizontalKernel.length / 2);
  const verticalRadius = Math.floor(verticalKernel.length / 2);

  const horizontallyBlurred = values.map(row => row.map((_, columnIndex) =>
    horizontalKernel.reduce((sum, weight, kernelIndex) => {
      const sourceColumn = resolveIndex(
        columnIndex + kernelIndex - horizontalRadius,
        columnCount,
        TRANSACTION_DENSITY_HEATMAP_TUNING.wrapHours,
      );
      return sum + row[sourceColumn] * weight;
    }, 0),
  ));

  const blurred = horizontallyBlurred.map((row, rowIndex) => row.map((_, columnIndex) =>
    verticalKernel.reduce((sum, weight, kernelIndex) => {
      const sourceRow = resolveIndex(
        rowIndex + kernelIndex - verticalRadius,
        rowCount,
        TRANSACTION_DENSITY_HEATMAP_TUNING.wrapWeekdays,
      );
      return sum + horizontallyBlurred[sourceRow][columnIndex] * weight;
    }, 0),
  ));

  if (!TRANSACTION_DENSITY_HEATMAP_TUNING.preserveValueRange) return blurred;

  const sourceValues = values.reduce<number[]>((all, row) => all.concat(row), []);
  const blurredValues = blurred.reduce<number[]>((all, row) => all.concat(row), []);
  const sourceMin = Math.min(...sourceValues);
  const sourceMax = Math.max(...sourceValues);
  const blurredMin = Math.min(...blurredValues);
  const blurredMax = Math.max(...blurredValues);
  const blurredRange = blurredMax - blurredMin;
  if (blurredRange === 0) return blurred;

  return blurred.map(row => row.map(value =>
    sourceMin + ((value - blurredMin) / blurredRange) * (sourceMax - sourceMin),
  ));
}

function transitionMix(mix: number, width: number) {
  if (width <= 0) return mix < 0.5 ? 0 : 1;

  const clampedWidth = Math.max(0, Math.min(1, width));
  const start = (1 - clampedWidth) / 2;
  const progress = Math.max(0, Math.min(1, (mix - start) / clampedWidth));
  return progress * progress * (3 - 2 * progress);
}

function quantizeMix(mix: number, steps: number | null | undefined) {
  if (steps == null || steps < 1) return mix;
  const normalizedSteps = Math.max(1, Math.trunc(steps));
  return Math.round(mix * normalizedSteps) / normalizedSteps;
}

/**
 * Renders the density values as a continuous scalar field. The bitmap is kept
 * at CSS-pixel resolution (and capped) because the heatmap is intentionally
 * smooth; labels and interaction remain in the DOM layer above it.
 */
export const TransactionDensityHeatmapCanvas = memo(function TransactionDensityHeatmapCanvas({
  values,
  min,
  max,
  interpolationSteps,
}: TransactionDensityHeatmapCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || values.length === 0 || values[0]?.length === 0) return;

    let animationFrame = 0;

    const draw = () => {
      const width = Math.max(1, Math.min(Math.round(canvas.clientWidth), MAX_RENDER_WIDTH));
      const height = Math.max(1, Math.min(Math.round(canvas.clientHeight), MAX_RENDER_HEIGHT));
      if (canvas.width !== width) canvas.width = width;
      if (canvas.height !== height) canvas.height = height;

      const context = canvas.getContext('2d');
      if (!context) return;

      const rowCount = values.length;
      const columnCount = values[0].length;
      const range = max - min;
      const displayValues = blurValues(values);
      const image = context.createImageData(width, height);
      const pixels = image.data;
      const paletteLastIndex = paletteRgb.length - 1;

      for (let y = 0; y < height; y += 1) {
        const sourceY = Math.max(0, Math.min(rowCount - 1, ((y + 0.5) / height) * rowCount - 0.5));
        const top = Math.floor(sourceY);
        const bottom = Math.min(top + 1, rowCount - 1);
        const verticalMix = quantizeMix(
          transitionMix(
            sourceY - top,
            TRANSACTION_DENSITY_HEATMAP_TUNING.verticalTransitionWidth,
          ),
          interpolationSteps,
        );

        for (let x = 0; x < width; x += 1) {
          const sourceX = Math.max(0, Math.min(columnCount - 1, ((x + 0.5) / width) * columnCount - 0.5));
          const left = Math.floor(sourceX);
          const right = Math.min(left + 1, columnCount - 1);
          const horizontalMix = quantizeMix(
            transitionMix(
              sourceX - left,
              TRANSACTION_DENSITY_HEATMAP_TUNING.horizontalTransitionWidth,
            ),
            interpolationSteps,
          );

          const topValue = displayValues[top][left] + (displayValues[top][right] - displayValues[top][left]) * horizontalMix;
          const bottomValue = displayValues[bottom][left] + (displayValues[bottom][right] - displayValues[bottom][left]) * horizontalMix;
          const value = topValue + (bottomValue - topValue) * verticalMix;
          const normalized = range === 0 ? 0 : Math.max(0, Math.min(1, (value - min) / range));
          const palettePosition = normalized * paletteLastIndex;
          const lowerColorIndex = Math.floor(palettePosition);
          const upperColorIndex = Math.min(lowerColorIndex + 1, paletteLastIndex);
          const colorMix = palettePosition - lowerColorIndex;
          const lowerColor = paletteRgb[lowerColorIndex];
          const upperColor = paletteRgb[upperColorIndex];
          const pixelIndex = (y * width + x) * 4;

          pixels[pixelIndex] = lowerColor[0] + (upperColor[0] - lowerColor[0]) * colorMix;
          pixels[pixelIndex + 1] = lowerColor[1] + (upperColor[1] - lowerColor[1]) * colorMix;
          pixels[pixelIndex + 2] = lowerColor[2] + (upperColor[2] - lowerColor[2]) * colorMix;
          pixels[pixelIndex + 3] = 255;
        }
      }

      context.putImageData(image, 0, 0);
    };

    const scheduleDraw = () => {
      cancelAnimationFrame(animationFrame);
      animationFrame = requestAnimationFrame(draw);
    };

    scheduleDraw();

    if (typeof ResizeObserver !== 'undefined') {
      const resizeObserver = new ResizeObserver(scheduleDraw);
      resizeObserver.observe(canvas);
      return () => {
        cancelAnimationFrame(animationFrame);
        resizeObserver.disconnect();
      };
    }

    window.addEventListener('resize', scheduleDraw);
    return () => {
      cancelAnimationFrame(animationFrame);
      window.removeEventListener('resize', scheduleDraw);
    };
  }, [interpolationSteps, max, min, values]);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 h-full w-full"
    />
  );
});
