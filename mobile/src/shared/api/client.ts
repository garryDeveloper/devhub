// Same shape as web/src/shared/api/client.ts so both clients handle errors
// identically. Auth headers and refresh-on-401 are added in DEVHUB-022.

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

export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });

  if (!res.ok) {
    throw await toApiError(res);
  }

  return res.status === 204 ? (undefined as T) : ((await res.json()) as T);
}
