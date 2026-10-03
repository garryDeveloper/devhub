// Mirrors DevHub.Application.Workspaces.Contracts.WorkspaceDto (api-endpoints.md §2).
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

// Mirrors DevHub.Application.Workspaces.Contracts.MemberDto (api-endpoints.md §2).
export interface Member {
  /** The membership's own id — distinct from `user.id`. PATCH/DELETE address this one. */
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
