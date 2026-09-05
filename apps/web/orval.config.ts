// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { defineConfig, defineTransformer } from 'orval';

const explodeArrayQueryParameters = defineTransformer(spec => {
  const methods = ['get', 'put', 'post', 'delete', 'options', 'head', 'patch', 'trace'] as const;

  for (const pathItem of Object.values(spec.paths ?? {})) {
    if (!pathItem) continue;

    for (const method of methods) {
      for (const parameter of pathItem[method]?.parameters ?? []) {
        if (
          '$ref' in parameter ||
          parameter.in !== 'query' ||
          !parameter.schema ||
          '$ref' in parameter.schema ||
          parameter.schema.type !== 'array'
        ) continue;

        // ASP.NET binds arrays from repeated keys: ?tags=prod&tags=dev.
        parameter.style = 'form';
        parameter.explode = true;
      }
    }
  }

  return spec;
});

export default defineConfig({
  adwais: {
    input: {
      target: '../../docs/openapi/v1.json',
      override: {
        transformer: explodeArrayQueryParameters,
      },
    },
    output: {
      mode: 'split',
      target: './src/api/generated/endpoints.ts',
      schemas: '../../packages/types/generated',
      client: 'react-query',
      override: {
        mutator: {
          path: './src/apiClient.ts',
          name: 'customClient',
        },
      },
    },
  },
});
