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
