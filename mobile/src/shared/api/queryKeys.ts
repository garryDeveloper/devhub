// Same conventions as web/src/shared/api/queryKeys.ts — centralized, typed
// query keys, never an inline string array.
//
// Workspace-scoped data is keyed UNDER the workspace id: switching the active workspace changes
// the key, so a stale screen can never render another workspace's data and switching needs no
// manual invalidation (DEVHUB-029, same reasoning as web's DEVHUB-027).
export const qk = {
  health: ['health'] as const,
  workspaces: ['workspaces'] as const,
  members: (workspaceId: string) =>
    ['workspaces', workspaceId, 'members'] as const,
};
