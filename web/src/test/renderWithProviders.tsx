import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import type { ReactNode } from 'react';
import type { RouteObject } from 'react-router-dom';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { vi } from 'vitest';
import type { AuthContextValue } from '../features/auth/context/authContext';
import { AuthContext } from '../features/auth/context/authContext';
import { ToastProvider } from '../shared/components/Toast';

export const testUser = {
  id: 'user-1',
  email: 'dario@example.com',
  displayName: 'Dario',
  avatarUrl: null,
};

// Renders `routes` in a memory router behind a fresh QueryClient and a signed-in AuthContext.
// A fresh client per test: a shared cache would leak one test's data into the next.
// Returns the router so a test can assert where navigation ended up.
export function renderWithProviders(
  routes: RouteObject[],
  initialEntry: string,
  authOverrides: Partial<AuthContextValue> = {},
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  const auth: AuthContextValue = {
    user: testUser,
    isBootstrapping: false,
    sessionExpired: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    ...authOverrides,
  };

  const router = createMemoryRouter(routes, { initialEntries: [initialEntry] });

  const Providers = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AuthContext.Provider value={auth}>{children}</AuthContext.Provider>
      </ToastProvider>
    </QueryClientProvider>
  );

  render(
    <Providers>
      <RouterProvider router={router} />
    </Providers>,
  );

  return { router, queryClient };
}
