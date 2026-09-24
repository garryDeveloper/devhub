// A tiny fake of the DevHub auth API for tests, installed as `global.fetch`.
// MSW would be the web equivalent, but under React Native's Jest environment it needs stream
// polyfills; routing a handful of endpoints by hand is simpler and just as honest, because the
// real apiFetch (headers, 401 interception, single-flight refresh) still runs unmodified.
//
// `access-valid` is the only valid access token and `refresh-valid` the only valid refresh token.
// Each successful refresh rotates to `refresh-valid` again so a test can refresh repeatedly.

export const USER = {
  id: '1',
  email: 'dario@example.com',
  displayName: 'Dario',
  avatarUrl: null,
};

export interface FakeApi {
  calls: Record<string, number>;
  /** Bodies sent to POST /api/auth/logout, to assert which token was revoked. */
  logoutBodies: unknown[];
  /** Override a route; return undefined to fall through to the default handler. */
  override: (path: string, handler: RouteHandler) => void;
}

type RouteHandler = (
  init: RequestInit | undefined,
) => Promise<Response | undefined> | Response | undefined;

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });

export const unauthorized = () =>
  json({ title: 'Unauthorized', status: 401 }, 401);

const tick = () => new Promise((resolve) => setTimeout(resolve, 10));

function bearer(init: RequestInit | undefined): string | undefined {
  return (init?.headers as Record<string, string> | undefined)?.Authorization;
}

export function installFakeApi(): FakeApi {
  const calls: Record<string, number> = {};
  const logoutBodies: unknown[] = [];
  const overrides = new Map<string, RouteHandler>();

  const defaults: Record<string, RouteHandler> = {
    '/api/me': (init) =>
      bearer(init) === 'Bearer access-valid' ? json(USER) : unauthorized(),

    '/api/auth/refresh': async (init) => {
      // Keeps the refresh on the wire long enough for concurrent 401s to pile up on it.
      await tick();
      const { refreshToken } = JSON.parse(String(init?.body)) as {
        refreshToken: string;
      };
      return refreshToken === 'refresh-valid'
        ? json({
            accessToken: 'access-valid',
            expiresIn: 900,
            refreshToken: 'refresh-valid',
          })
        : unauthorized();
    },

    '/api/auth/login': (init) => {
      const { password } = JSON.parse(String(init?.body)) as {
        password: string;
      };
      return password === 'correct-password'
        ? json({
            accessToken: 'access-valid',
            expiresIn: 900,
            refreshToken: 'refresh-valid',
            user: USER,
          })
        : json(
            {
              title: 'Authentication failed.',
              status: 401,
              detail: 'Invalid email or password.',
            },
            401,
          );
    },

    '/api/auth/logout': (init) => {
      logoutBodies.push(JSON.parse(String(init?.body)));
      return new Response(null, { status: 204 });
    },
  };

  globalThis.fetch = jest.fn(
    async (input: RequestInfo | URL, init?: RequestInit) => {
      const path = new URL(String(input)).pathname;
      calls[path] = (calls[path] ?? 0) + 1;

      const response =
        (await overrides.get(path)?.(init)) ?? (await defaults[path]?.(init));
      if (!response) throw new Error(`fakeApi: unhandled request to ${path}`);
      return response;
    },
  ) as typeof fetch;

  return {
    calls,
    logoutBodies,
    override: (path, handler) => overrides.set(path, handler),
  };
}
