import AsyncStorage from '@react-native-async-storage/async-storage';

import type { Workspace } from '../types';

// The workspace id a user last picked, so the selection survives an app restart (DEVHUB-029
// acceptance criterion). Non-sensitive — just a UUID the server already scoped to this user — so
// AsyncStorage is fine; it is never a credential store (that's SecureStore, DEVHUB-022).
// Keyed per user id so two accounts on the same device never land in each other's workspace.
const keyFor = (userId: string) => `devhub:activeWorkspace:${userId}`;

export async function getStoredWorkspaceId(userId: string): Promise<string | null> {
  try {
    return await AsyncStorage.getItem(keyFor(userId));
  } catch {
    // Storage can fail to open. Not remembering the selection is fine; resolveActiveWorkspace
    // below falls back to the first workspace.
    return null;
  }
}

export async function setStoredWorkspaceId(
  userId: string,
  workspaceId: string,
): Promise<void> {
  try {
    await AsyncStorage.setItem(keyFor(userId), workspaceId);
  } catch {
    // See above.
  }
}

/** The remembered workspace if the user still belongs to it, else the first one, else null. */
export function resolveActiveWorkspace(
  workspaces: Workspace[],
  storedId: string | null,
): Workspace | null {
  return (
    workspaces.find((workspace) => workspace.id === storedId) ??
    workspaces[0] ??
    null
  );
}
