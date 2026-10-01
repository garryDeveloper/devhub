import { apiFetch } from '../../../shared/api/client';
import type { Workspace } from '../types';

// Only the caller's memberships, ordered by name. Not paged: a user has few (api-endpoints.md §2).
export function fetchWorkspaces(): Promise<Workspace[]> {
  return apiFetch<Workspace[]>('/api/workspaces');
}

// No slug is sent: the server derives it from the name exactly as slugify() previews it, so the
// address the user saw is the address they get (DEVHUB-027: read-only preview).
export function createWorkspace(name: string): Promise<Workspace> {
  return apiFetch<Workspace>('/api/workspaces', {
    method: 'POST',
    body: JSON.stringify({ name }),
  });
}
