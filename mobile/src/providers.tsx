import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { AuthProvider } from './features/auth/context/AuthProvider';

// Defaults per docs/tech-specs/mobile-architecture.md §5: longer staleTime
// than web because mobile networks are worse and screens are re-entered
// constantly.
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      retry: 1,
    },
  },
});

// AuthProvider sits inside QueryClientProvider: logout needs useQueryClient()
// to clear the cache (auth-spec.md §4 "Client contract").
export function Providers({ children }: { children: ReactNode }) {
  return (
    <SafeAreaProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    </SafeAreaProvider>
  );
}
