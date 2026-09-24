import { useQueryClient } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useCallback, useEffect, useMemo, useState } from 'react';

import { setAccessToken } from '../../../shared/api/authToken';
import {
  refreshTokenOnce,
  setSessionExpiredHandler,
} from '../../../shared/api/client';
import {
  clearStoredRefreshToken,
  getStoredRefreshToken,
  setStoredRefreshToken,
} from '../../../shared/api/refreshTokenStorage';
import {
  fetchMe,
  loginRequest,
  logoutRequest,
  registerRequest,
} from '../api/authApi';
import type { AuthUser } from '../types';
import { AuthContext } from './authContext';

// Mobile port of web/src/features/auth/context/AuthProvider.tsx (DEVHUB-022). Differences:
// - The refresh token store (SecureStore) is async, so every read/write is awaited.
// - Nothing navigates imperatively. RootNavigator renders AuthStack or AppTabs from `user`, so
//   setting `user` to null *is* the redirect, and the removed AppTabs cannot be gone back to.
// - While bootstrapping this renders nothing: the native splash (expo-splash-screen, held in
//   App.tsx) stays on screen until RootNavigator's NavigationContainer is ready.
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isBootstrapping, setIsBootstrapping] = useState(true);
  const [sessionExpired, setSessionExpired] = useState(false);
  const queryClient = useQueryClient();

  // A refresh rejected with 401 has already cleared both tokens in the client; this clears the
  // React side. No POST /logout: the family is already dead.
  useEffect(() => {
    setSessionExpiredHandler(() => {
      queryClient.clear();
      setUser(null);
      setSessionExpired(true);
    });
    return () => setSessionExpiredHandler(null);
  }, [queryClient]);

  // Cold start: stored refresh token → refresh → GET /api/me (mobile-architecture.md §4).
  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      try {
        if (!(await getStoredRefreshToken())) return;
        // Same single-flight refresh as apiFetch. A 401 clears the tokens inside
        // refreshTokenOnce; a network error keeps the stored token so reopening the app with
        // signal restores the session instead of forcing a new login.
        await refreshTokenOnce();
        const me = await fetchMe();
        if (!cancelled) setUser(me);
      } catch {
        setAccessToken(null);
      } finally {
        if (!cancelled) setIsBootstrapping(false);
      }
    }

    void bootstrap();
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest(email, password);
    setAccessToken(response.accessToken);
    await setStoredRefreshToken(response.refreshToken);
    setSessionExpired(false);
    setUser(response.user);
  }, []);

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      const response = await registerRequest(email, password, displayName);
      setAccessToken(response.accessToken);
      await setStoredRefreshToken(response.refreshToken);
      setSessionExpired(false);
      setUser(response.user);
    },
    [],
  );

  // Order matters (auth-spec.md §4 "Client contract"): read the refresh token before clearing
  // it, fire the revoke call without blocking the UI on it, then clear local state. Setting
  // `user` to null last is step 5 — RootNavigator swaps to AuthStack (Welcome).
  const logout = useCallback(async () => {
    const storedRefreshToken = await getStoredRefreshToken();
    if (storedRefreshToken) {
      void logoutRequest(storedRefreshToken).catch(() => {
        // Nothing the user can do about a failed revoke; the local cleanup below is what
        // actually logs them out on this device.
      });
    }

    setAccessToken(null);
    await clearStoredRefreshToken();
    queryClient.clear();
    setSessionExpired(false);
    setUser(null);
  }, [queryClient]);

  const value = useMemo(
    () => ({ user, isBootstrapping, sessionExpired, login, register, logout }),
    [user, isBootstrapping, sessionExpired, login, register, logout],
  );

  if (isBootstrapping) {
    return null;
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
