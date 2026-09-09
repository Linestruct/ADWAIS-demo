import { createLazyFileRoute } from '@tanstack/react-router';
import { Diagnostics } from '../../pages/Diagnostics';

export const Route = createLazyFileRoute('/settings/diagnostics')({
  component: Diagnostics,
});
