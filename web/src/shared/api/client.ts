// One place for HTTP calls: base URL, headers and RFC 7807 error normalization.
// Authenticated requests carry the in-memory access token (DEVHUB-020). Refresh-on-401
// interception (single-flight retry) is added in DEVHUB-021 — this client does not retry yet.

import { getAccessToken } from './authToken';

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

export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...authHeader(),
      ...init?.headers,
    },
  });

  if (!res.ok) {
    throw await toApiError(res);
  }

  return res.status === 204 ? (undefined as T) : ((await res.json()) as T);
}
