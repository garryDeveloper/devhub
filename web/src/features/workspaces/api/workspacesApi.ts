import { apiFetch } from '../../../shared/api/client';
import type { Member, Workspace, WorkspaceRole } from '../types';

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

// The slug is immutable server-side and ignored if sent, so there is nothing to pass but the name
// (DEVHUB-028).
export function updateWorkspace(workspaceId: string, name: string): Promise<Workspace> {
  return apiFetch<Workspace>(`/api/workspaces/${workspaceId}`, {
    method: 'PATCH',
    body: JSON.stringify({ name }),
  });
}

// Any member may read the list (DEVHUB-025), ordered by joinedAt. Not paged: a workspace has few.
export function fetchMembers(workspaceId: string): Promise<Member[]> {
  return apiFetch<Member[]>(`/api/workspaces/${workspaceId}/members`);
}

export function addMember(workspaceId: string, email: string, role: WorkspaceRole): Promise<Member> {
  return apiFetch<Member>(`/api/workspaces/${workspaceId}/members`, {
    method: 'POST',
    body: JSON.stringify({ email, role }),
  });
}

export function changeMemberRole(workspaceId: string, memberId: string, role: WorkspaceRole): Promise<Member> {
  return apiFetch<Member>(`/api/workspaces/${workspaceId}/members/${memberId}`, {
    method: 'PATCH',
    body: JSON.stringify({ role }),
  });
}

// 204, nothing to return. Also used to leave a workspace: same endpoint, no role required when
// the member removes themselves (api-endpoints.md §2).
export function removeMember(workspaceId: string, memberId: string): Promise<void> {
  return apiFetch<void>(`/api/workspaces/${workspaceId}/members/${memberId}`, {
    method: 'DELETE',
  });
}
