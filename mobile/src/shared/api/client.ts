// Same shape as web/src/shared/api/client.ts so both clients handle errors and token refresh
// identically (DEVHUB-021, ported in DEVHUB-022). The only difference is that the refresh token
// store is asynchronous (SecureStore), so reading and writing it is awaited.

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

// On a physical device, `localhost` is the phone — EXPO_PUBLIC_API_URL must
// be the dev machine's LAN IP with the API bound to 0.0.0.0.
const API_URL = process.env.EXPO_PUBLIC_API_URL;

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
// session is dead; AuthProvider registers the handler that clears the user and the query cache,
// which makes RootNavigator swap AppTabs for AuthStack.
// ---------------------------------------------------------------------------------------------

let sessionExpiredHandler: (() => void) | null = null;

export function setSessionExpiredHandler(handler: (() => void) | null): void {
  sessionExpiredHandler = handler;
}

async function expireSession(): Promise<void> {
  setAccessToken(null);
  await clearStoredRefreshToken();
  sessionExpiredHandler?.();
}

// ---------------------------------------------------------------------------------------------
// Single-flight refresh
//
// Two refreshes with the same token make the API see the second one as reuse and revoke the
// whole family (auth-spec.md §4), so there must only ever be one in flight. The first caller
// creates the promise; every concurrent caller awaits that same promise; it is dropped once it
// settles so the next expiry starts a fresh one.
//
// The promise is assigned synchronously, before doRefresh's first `await`. That matters more
// here than on web: reading SecureStore is async, and if the "is one in flight?" check came
// after that read, five concurrent 401s could all pass it before any of them set the promise.
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
  const refreshToken = await getStoredRefreshToken();
  if (!refreshToken) {
    await expireSession();
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
    await setStoredRefreshToken(refreshed.refreshToken);
  } catch (error) {
    // Only a 401 means the session is dead (auth-spec.md §4). A network error or 5xx keeps the
    // session: on a phone that loses signal in a lift, this is the difference between a retry
    // and being thrown back to the Welcome screen.
    if (error instanceof ApiError && error.status === 401) {
      await expireSession();
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
