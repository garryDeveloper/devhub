// Mirrors web/src/features/workspaces/types.ts (DevHub.Application.Workspaces.Contracts,
// api-endpoints.md §2). Mobile only ever reads these — no create/update/role/remove.
export type WorkspaceRole = 'Owner' | 'Member';

export interface Workspace {
  id: string;
  name: string;
  slug: string;
  /** The current user's role in this workspace, not a property of the workspace itself. */
  role: WorkspaceRole;
  memberCount: number;
  createdAt: string;
}

export interface Member {
  /** The membership's own id — distinct from `user.id`. */
  id: string;
  user: {
    id: string;
    email: string;
    displayName: string;
    avatarUrl: string | null;
  };
  role: WorkspaceRole;
  joinedAt: string;
}
