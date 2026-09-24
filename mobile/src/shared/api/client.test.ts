import { getAccessToken, setAccessToken } from './authToken';
import { ApiError, apiFetch, setSessionExpiredHandler } from './client';
import {
  getStoredRefreshToken,
  setStoredRefreshToken,
} from './refreshTokenStorage';
import { installFakeApi, unauthorized } from '../../test/fakeApi';
import type { FakeApi } from '../../test/fakeApi';

// Mobile port of web/src/shared/api/client.test.ts (DEVHUB-021 behaviour, DEVHUB-022).
describe('apiFetch token refresh', () => {
  let api: FakeApi;
  const onSessionExpired = jest.fn();

  beforeEach(async () => {
    api = installFakeApi();
    onSessionExpired.mockReset();
    setSessionExpiredHandler(onSessionExpired);
    // An expired access token with a still-valid refresh token.
    setAccessToken('access-expired');
    await setStoredRefreshToken('refresh-valid');
  });

  afterEach(() => {
    setSessionExpiredHandler(null);
  });

  it('refreshes once on a 401 and retries the original request', async () => {
    const me = await apiFetch<{ displayName: string }>('/api/me');

    expect(me.displayName).toBe('Dario');
    expect(api.calls['/api/auth/refresh']).toBe(1);
    expect(api.calls['/api/me']).toBe(2);
    expect(getAccessToken()).toBe('access-valid');
  });

  it('makes exactly one refresh call for five concurrent 401s', async () => {
    const results = await Promise.all(
      Array.from({ length: 5 }, () =>
        apiFetch<{ displayName: string }>('/api/me'),
      ),
    );

    expect(results.map((r) => r.displayName)).toEqual(Array(5).fill('Dario'));
    expect(api.calls['/api/auth/refresh']).toBe(1);
  });

  it('logs out once, without retrying, when the refresh token is revoked', async () => {
    await setStoredRefreshToken('refresh-revoked');

    const results = await Promise.allSettled(
      Array.from({ length: 5 }, () => apiFetch('/api/me')),
    );

    expect(results.every((r) => r.status === 'rejected')).toBe(true);
    expect(api.calls['/api/auth/refresh']).toBe(1);
    expect(api.calls['/api/me']).toBe(5);
    expect(onSessionExpired).toHaveBeenCalledTimes(1);
    expect(getAccessToken()).toBeNull();
    expect(await getStoredRefreshToken()).toBeNull();
  });

  it('does not refresh again when the retry also returns 401', async () => {
    api.override('/api/me', () => unauthorized());

    await expect(apiFetch('/api/me')).rejects.toBeInstanceOf(ApiError);

    expect(api.calls['/api/auth/refresh']).toBe(1);
    expect(api.calls['/api/me']).toBe(2);
  });

  it('does not refresh on a 401 from an auth endpoint', async () => {
    await expect(
      apiFetch('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email: 'a@b.c', password: 'wrong' }),
      }),
    ).rejects.toMatchObject({ status: 401 });

    expect(api.calls['/api/auth/refresh']).toBeUndefined();
    expect(onSessionExpired).not.toHaveBeenCalled();
  });

  it('keeps the session when the refresh fails with a network error', async () => {
    api.override('/api/auth/refresh', () => {
      throw new TypeError('Network request failed');
    });

    await expect(apiFetch('/api/me')).rejects.toBeInstanceOf(TypeError);

    expect(await getStoredRefreshToken()).toBe('refresh-valid');
    expect(onSessionExpired).not.toHaveBeenCalled();
  });
});
