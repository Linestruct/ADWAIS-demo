// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useEffect, useId, useRef } from 'react';
import {
  Chart as ChartJS,
  type ChartData,
  type ChartOptions,
  type ChartType,
  type DefaultDataPoint,
  type Plugin,
} from 'chart.js';
import './chartJs';

interface ChartJsCanvasProps<
  TType extends ChartType,
  TData = DefaultDataPoint<TType>,
  TLabel = unknown,
> {
  type: TType;
  data: ChartData<TType, TData, TLabel>;
  options?: ChartOptions<TType>;
  plugins?: Plugin<TType>[];
  className?: string;
}

export function ChartJsCanvas<
  TType extends ChartType,
  TData = DefaultDataPoint<TType>,
  TLabel = unknown,
>({ type, data, options, plugins = [], className }: ChartJsCanvasProps<TType, TData, TLabel>) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const reactId = useId();
  const canvasId = `chart-${reactId.replace(/:/g, '')}`;

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    ChartJS.getChart(canvas)?.destroy();
    const chart = new ChartJS<TType, TData, TLabel>(canvas, { type, data, options, plugins });

    return () => {
      if (ChartJS.getChart(canvas) === chart) chart.destroy();
    };
  }, [data, options, plugins, type]);

  return <canvas ref={canvasRef} id={canvasId} className={className} />;
}

