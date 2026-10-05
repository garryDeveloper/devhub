import { apiFetch } from '../../../shared/api/client';
import type { Member, Workspace } from '../types';

// Only the caller's memberships, ordered by name. Not paged: a user has few (api-endpoints.md §2).
export function fetchWorkspaces(): Promise<Workspace[]> {
  return apiFetch<Workspace[]>('/api/workspaces');
}

// Any member may read the list, ordered by joinedAt. Not paged: a workspace has few.
// Workspace administration (add/role/remove) is web-only by design (DEVHUB-029) — not ported.
export function fetchMembers(workspaceId: string): Promise<Member[]> {
  return apiFetch<Member[]>(`/api/workspaces/${workspaceId}/members`);
}
