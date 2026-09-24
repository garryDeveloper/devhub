import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';
import '@testing-library/jest-dom/vitest';

// Without vitest's `globals: true`, Testing Library's own afterEach auto-cleanup never
// registers (it looks for a global `afterEach`), so unmount explicitly between tests.
afterEach(() => {
  cleanup();
});
