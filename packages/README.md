# Packages

Shared libraries in the workspace.

- `/packages/types` - TypeScript models. `index.ts` re-exports generated DTO types from `generated/`.
- `/packages/utils` - Helper utilities (formatting, dates, math).
- `/packages/ui` - Placeholder for shared UI components.
- `/packages/shared` - Placeholder for shared logic.

Packages link through pnpm workspaces. Apps reference them with `workspace:*`.
