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
import { AuthSplash } from '../components/AuthSplash';
import type { AuthUser } from '../types';
import { AuthContext } from './authContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isBootstrapping, setIsBootstrapping] = useState(true);
  const [sessionExpired, setSessionExpired] = useState(false);
  const queryClient = useQueryClient();

  // A refresh rejected with 401 (DEVHUB-021) has already cleared both tokens in the client; this
  // clears the React side. No POST /logout: the family is already dead. No navigate() either —
  // this provider sits outside the router, and RequireAuth redirects as soon as `user` is null,
  // reading `sessionExpired` to add `expired=1`.
  useEffect(() => {
    setSessionExpiredHandler(() => {
      queryClient.clear();
      setUser(null);
      setSessionExpired(true);
    });
    return () => setSessionExpiredHandler(null);
  }, [queryClient]);

  // Bootstrap once on mount: turn a stored refresh token into a live session before anything
  // behind this provider renders (frontend-web-architecture.md §7).
  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      const storedRefreshToken = getStoredRefreshToken();
      if (!storedRefreshToken) {
        setIsBootstrapping(false);
        return;
      }

      // Same single-flight refresh as apiFetch: under StrictMode this effect runs twice, and two
      // parallel refreshes with one token would trip reuse detection and kill the session.
      // A 401 clears the tokens inside refreshTokenOnce; a network error keeps the stored token
      // so a reload can recover once the API is back.
      try {
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
    setStoredRefreshToken(response.refreshToken);
    setSessionExpired(false);
    setUser(response.user);
  }, []);

  const register = useCallback(
    async (email: string, password: string, displayName: string) => {
      const response = await registerRequest(email, password, displayName);
      setAccessToken(response.accessToken);
      setStoredRefreshToken(response.refreshToken);
      setSessionExpired(false);
      setUser(response.user);
    },
    [],
  );

  // Order matters (auth-spec.md §4 "Client contract"): read the refresh token before clearing
  // it, fire the revoke call without blocking the UI on it, then clear local state.
  const logout = useCallback(async () => {
    const storedRefreshToken = getStoredRefreshToken();
    if (storedRefreshToken) {
      void logoutRequest(storedRefreshToken).catch(() => {
        // Nothing the user can do about a failed revoke; the local cleanup below is what
        // actually logs them out on this device.
      });
    }

    setAccessToken(null);
    clearStoredRefreshToken();
    queryClient.clear();
    setSessionExpired(false);
    setUser(null);
  }, [queryClient]);

  const value = useMemo(
    () => ({ user, isBootstrapping, sessionExpired, login, register, logout }),
    [user, isBootstrapping, sessionExpired, login, register, logout],
  );

  if (isBootstrapping) {
    return <AuthSplash />;
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
