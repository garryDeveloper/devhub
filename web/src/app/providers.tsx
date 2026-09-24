import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { AuthProvider } from '../features/auth/context/AuthProvider';
import { ToastProvider } from '../shared/components/Toast';

// Defaults per docs/tech-specs/frontend-web-architecture.md §4.
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
  },
});

// AuthProvider sits inside QueryClientProvider: logout needs useQueryClient() to clear the
// cache (auth-spec.md §4 "Client contract").
export function Providers({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AuthProvider>{children}</AuthProvider>
      </ToastProvider>
    </QueryClientProvider>
  );
}
