// Centralized, typed query keys. Feature tickets extend this object —
// never inline a string array as a query key.
//
// Workspace-scoped data is keyed UNDER the workspace id (frontend-web-architecture.md §4):
// switching workspace changes the key, so workspace A's data can never render inside B and the
// switcher never has to invalidate anything by hand (DEVHUB-027).
export const qk = {
  health: ['health'] as const,
  workspaces: ['workspaces'] as const,
  projects: (workspaceId: string) =>
    ['workspaces', workspaceId, 'projects'] as const,
  members: (workspaceId: string) =>
    ['workspaces', workspaceId, 'members'] as const,
};
