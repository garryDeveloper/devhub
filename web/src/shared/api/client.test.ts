import { delay, http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
  afterAll,
  afterEach,
  beforeAll,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';
import { getAccessToken, setAccessToken } from './authToken';
import { ApiError, apiFetch, setSessionExpiredHandler } from './client';
import {
  getStoredRefreshToken,
  setStoredRefreshToken,
} from './refreshTokenStorage';

const API = import.meta.env.VITE_API_URL;

// The fake API: `access-new` is the only valid access token, `refresh-old` the only valid
// refresh token. Tests start with the expired pair, so the first protected call always 401s.
let refreshCalls = 0;
let meCalls = 0;

const meHandler = http.get(`${API}/api/me`, ({ request }) => {
  meCalls++;
  if (request.headers.get('Authorization') !== 'Bearer access-new') {
    return HttpResponse.json(
      { title: 'Unauthorized', status: 401 },
      { status: 401 },
    );
  }
  return HttpResponse.json({ id: '1', displayName: 'Dario' });
});

const refreshHandler = http.post(
  `${API}/api/auth/refresh`,
  async ({ request }) => {
    refreshCalls++;
    // A delay keeps the refresh on the wire long enough for concurrent 401s to pile up on it.
    await delay(20);
    const body = (await request.json()) as { refreshToken: string };
    if (body.refreshToken !== 'refresh-old') {
      return HttpResponse.json(
        { title: 'Unauthorized', status: 401 },
        { status: 401 },
      );
    }
    return HttpResponse.json({
      accessToken: 'access-new',
      expiresIn: 900,
      refreshToken: 'refresh-new',
    });
  },
);

const server = setupServer(meHandler, refreshHandler);

const unauthorized = () =>
  HttpResponse.json({ title: 'Unauthorized', status: 401 }, { status: 401 });

describe('apiFetch token refresh', () => {
  const onSessionExpired = vi.fn();

  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterAll(() => server.close());

  beforeEach(() => {
    refreshCalls = 0;
    meCalls = 0;
    onSessionExpired.mockReset();
    setSessionExpiredHandler(onSessionExpired);
    setAccessToken('access-old');
    setStoredRefreshToken('refresh-old');
  });

  afterEach(() => {
    server.resetHandlers();
    setSessionExpiredHandler(null);
    localStorage.clear();
  });

  it('refreshes once on a 401 and retries the original request', async () => {
    const me = await apiFetch<{ displayName: string }>('/api/me');

    expect(me.displayName).toBe('Dario');
    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(2);
    expect(getAccessToken()).toBe('access-new');
    expect(getStoredRefreshToken()).toBe('refresh-new');
  });

  it('makes exactly one refresh call for five concurrent 401s', async () => {
    const results = await Promise.all(
      Array.from({ length: 5 }, () => apiFetch<{ displayName: string }>('/api/me')),
    );

    expect(results.map((r) => r.displayName)).toEqual(Array(5).fill('Dario'));
    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(10);
  });

  it('logs out when the refresh is rejected, without retrying', async () => {
    setStoredRefreshToken('refresh-revoked');

    await expect(apiFetch('/api/me')).rejects.toMatchObject({ status: 401 });

    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(1);
    expect(getAccessToken()).toBeNull();
    expect(getStoredRefreshToken()).toBeNull();
    expect(onSessionExpired).toHaveBeenCalledTimes(1);
  });

  it('logs out once, not five times, when five concurrent requests hit a revoked session', async () => {
    setStoredRefreshToken('refresh-revoked');

    const results = await Promise.allSettled(
      Array.from({ length: 5 }, () => apiFetch('/api/me')),
    );

    expect(results.every((r) => r.status === 'rejected')).toBe(true);
    expect(refreshCalls).toBe(1);
    expect(onSessionExpired).toHaveBeenCalledTimes(1);
  });

  it('does not refresh again when the retry also returns 401', async () => {
    server.use(
      http.get(`${API}/api/me`, () => {
        meCalls++;
        return unauthorized();
      }),
    );

    await expect(apiFetch('/api/me')).rejects.toBeInstanceOf(ApiError);

    expect(refreshCalls).toBe(1);
    expect(meCalls).toBe(2);
  });

  it('does not refresh on a 401 from an auth endpoint', async () => {
    server.use(http.post(`${API}/api/auth/login`, unauthorized));

    await expect(
      apiFetch('/api/auth/login', { method: 'POST', body: '{}' }),
    ).rejects.toMatchObject({ status: 401 });

    expect(refreshCalls).toBe(0);
    expect(onSessionExpired).not.toHaveBeenCalled();
  });

  it('keeps the session when the refresh fails with a network error', async () => {
    server.use(
      http.post(`${API}/api/auth/refresh`, () => {
        refreshCalls++;
        return HttpResponse.error();
      }),
    );

    await expect(apiFetch('/api/me')).rejects.toBeInstanceOf(TypeError);

    expect(refreshCalls).toBe(1);
    expect(getStoredRefreshToken()).toBe('refresh-old');
    expect(onSessionExpired).not.toHaveBeenCalled();
  });

  it('retries without refreshing when another request already rotated the token', async () => {
    server.use(
      http.get(
        `${API}/api/me`,
        ({ request }) => {
          meCalls++;
          // Simulates a parallel request finishing its refresh while this one was on the wire.
          setAccessToken('access-new');
          return request.headers.get('Authorization') === 'Bearer access-new'
            ? HttpResponse.json({ id: '1', displayName: 'Dario' })
            : unauthorized();
        },
      ),
    );

    const me = await apiFetch<{ displayName: string }>('/api/me');

    expect(me.displayName).toBe('Dario');
    expect(refreshCalls).toBe(0);
    expect(meCalls).toBe(2);
  });
});
