// One place for HTTP calls: base URL, headers, RFC 7807 error normalization, and refresh-on-401.
// Authenticated requests carry the in-memory access token (DEVHUB-020). A 401 triggers one
// single-flight refresh and one retry (DEVHUB-021, frontend-web-architecture.md §3).

import { getAccessToken, setAccessToken } from './authToken';
import {
  clearStoredRefreshToken,
  getStoredRefreshToken,
  setStoredRefreshToken,
} from './refreshTokenStorage';

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;

  constructor(problem: ProblemDetails) {
    super(problem.detail ?? problem.title);
    this.name = 'ApiError';
    this.status = problem.status;
    this.title = problem.title;
    this.detail = problem.detail;
    this.errors = problem.errors;
  }
}

const API_URL = import.meta.env.VITE_API_URL;

function authHeader(): Record<string, string> {
  const token = getAccessToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function toApiError(res: Response): Promise<ApiError> {
  try {
    const body = (await res.json()) as Partial<ProblemDetails>;
    return new ApiError({
      type: body.type,
      title: body.title ?? res.statusText,
      status: body.status ?? res.status,
      detail: body.detail,
      errors: body.errors,
    });
  } catch {
    return new ApiError({
      title: res.statusText || 'Request failed',
      status: res.status,
    });
  }
}

// ---------------------------------------------------------------------------------------------
// Session expiry
//
// shared/ cannot depend on React or on features/, so the client only *announces* that the
// session is dead; AuthProvider registers the handler that clears the user and the query cache.
// ---------------------------------------------------------------------------------------------

let sessionExpiredHandler: (() => void) | null = null;

export function setSessionExpiredHandler(handler: (() => void) | null): void {
  sessionExpiredHandler = handler;
}

function expireSession(): void {
  setAccessToken(null);
  clearStoredRefreshToken();
  sessionExpiredHandler?.();
}

// ---------------------------------------------------------------------------------------------
// Single-flight refresh
//
// Two refreshes with the same token make the API see the second one as reuse and revoke the
// whole family (auth-spec.md §4), so there must only ever be one in flight. The first caller
// creates the promise; every concurrent caller awaits that same promise; it is dropped once it
// settles so the next expiry starts a fresh one.
// ---------------------------------------------------------------------------------------------

interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
}

let refreshInFlight: Promise<void> | null = null;

export function refreshTokenOnce(): Promise<void> {
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null;
  });
  return refreshInFlight;
}

async function doRefresh(): Promise<void> {
  const refreshToken = getStoredRefreshToken();
  if (!refreshToken) {
    expireSession();
    throw new ApiError({ title: 'Session expired', status: 401 });
  }

  try {
    // An /api/auth/* path, so this call bypasses the 401 interceptor: a failing refresh must
    // not try to refresh itself.
    const refreshed = await apiFetch<RefreshResponse>('/api/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
    setAccessToken(refreshed.accessToken);
    setStoredRefreshToken(refreshed.refreshToken);
  } catch (error) {
    // Only a 401 means the session is dead (auth-spec.md §4: every refresh failure is
    // `401 auth.invalid_refresh_token`). A network error or 5xx keeps the session so a Wi-Fi
    // blip does not log the user out; the original request just fails with that error.
    if (error instanceof ApiError && error.status === 401) {
      expireSession();
    }
    throw error;
  }
}

// ---------------------------------------------------------------------------------------------
// apiFetch
// ---------------------------------------------------------------------------------------------

// Every /api/auth/* call is anonymous: a 401 from /login means "wrong password", not "session
// expired", and a 401 from /refresh must not recurse.
function isAuthEndpoint(path: string): boolean {
  return path.startsWith('/api/auth/');
}

export function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  return send<T>(path, init, false);
}

async function send<T>(
  path: string,
  init: RequestInit | undefined,
  isRetry: boolean,
): Promise<T> {
  const tokenUsed = getAccessToken();
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...authHeader(),
      ...init?.headers,
    },
  });

  if (res.status === 401 && !isRetry && !isAuthEndpoint(path)) {
    // If the token changed while this request was on the wire, another request already
    // refreshed: retry with the new token instead of starting a second rotation.
    if (getAccessToken() === tokenUsed) {
      await refreshTokenOnce();
    }
    // Retried exactly once: a second 401 falls through and is thrown below.
    return send<T>(path, init, true);
  }

  if (!res.ok) {
    throw await toApiError(res);
  }

  return res.status === 204 ? (undefined as T) : ((await res.json()) as T);
}
