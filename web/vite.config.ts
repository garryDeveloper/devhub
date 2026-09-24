import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // Fixed base URL so MSW handlers match regardless of a developer's .env.local (and CI has none).
    env: { VITE_API_URL: 'http://api.test' },
  },
});
