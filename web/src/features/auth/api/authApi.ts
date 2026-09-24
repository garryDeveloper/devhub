import { apiFetch } from '../../../shared/api/client';
import type { AuthResponse, AuthUser, RefreshResponse } from '../types';

export function registerRequest(
  email: string,
  password: string,
  displayName: string,
): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, password, displayName }),
  });
}

export function loginRequest(
  email: string,
  password: string,
): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

export function refreshRequest(refreshToken: string): Promise<RefreshResponse> {
  return apiFetch<RefreshResponse>('/api/auth/refresh', {
    method: 'POST',
    body: JSON.stringify({ refreshToken }),
  });
}

// Always resolves to 204 (auth-spec.md §4) — callers do not need to handle a failure case.
export function logoutRequest(refreshToken: string | null): Promise<void> {
  return apiFetch<void>('/api/auth/logout', {
    method: 'POST',
    body: JSON.stringify({ refreshToken }),
  });
}

export function fetchMe(): Promise<AuthUser> {
  return apiFetch<AuthUser>('/api/me');
}
