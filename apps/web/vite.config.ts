// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import { tanstackRouter } from '@tanstack/router-vite-plugin'
import { existsSync, readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import basicSsl from '@vitejs/plugin-basic-ssl'

const envDir = resolve(__dirname)

function assertNoEnvBom(mode: string) {
  const envFiles = ['.env', '.env.local', `.env.${mode}`, `.env.${mode}.local`]

  for (const envFile of envFiles) {
    const path = resolve(envDir, envFile)
    if (!existsSync(path)) continue

    const contents = readFileSync(path)
    if (contents[0] === 0xef && contents[1] === 0xbb && contents[2] === 0xbf) {
      throw new Error(`${envFile} contains a UTF-8 BOM. Save it as UTF-8 without BOM.`)
    }
  }
}

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  assertNoEnvBom(mode)

  return {
    envDir,
    plugins: [
      tanstackRouter({
        routesDirectory: './src/routes',
        generatedRouteTree: './src/routeTree.gen.ts',
      }),
      react(),
      basicSsl(),
    ],
    build: {
      target: 'chrome76',
      rollupOptions: {
        output: {
          manualChunks: (id) => {
            if (id.includes('node_modules/recharts') || id.includes('node_modules/d3')) return 'vendor-charts';
            if (id.includes('node_modules/react') || id.includes('node_modules/react-dom')) return 'vendor-react';
            if (id.includes('node_modules/@tanstack')) return 'vendor-router';
            if (id.includes('node_modules/lucide-react')) return 'icons';
          }
        }
      }
    },
    resolve: {
      alias: {
        '@types': resolve(__dirname, '../../packages/types/index.ts'),
        '@utils': resolve(__dirname, './src/utils/index.ts'),
      },
    },
    server: {
      host: '127.0.0.1',
      proxy: {
        '/api': {
          target: 'http://127.0.0.1:5002',
          changeOrigin: true,
          secure: false,
        },
        '/swagger': {
          target: 'http://127.0.0.1:5002',
          changeOrigin: true,
          secure: false,
        },
        '/openapi': {
          target: 'http://127.0.0.1:5002',
          changeOrigin: true,
          secure: false,
        },
        '/hangfire': {
          target: 'http://127.0.0.1:5002',
          changeOrigin: true,
          secure: false,
        },
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
    },
  }
})
