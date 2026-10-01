import type { Workspace } from '../types';

// The last workspace a user was in, so `/` can take a returning user straight back there.
// Keyed per user: two people sharing a browser must not land in each other's workspace. Only a
// hint — always checked against the user's actual workspace list (resolveHomeWorkspace), so a
// stale or foreign slug degrades to "first workspace", never to a broken screen.

const keyFor = (userId: string) => `devhub:lastWorkspace:${userId}`;

export function getLastWorkspaceSlug(userId: string): string | null {
  try {
    return localStorage.getItem(keyFor(userId));
  } catch {
    // Storage can be unavailable (private mode, blocked site data). Not remembering is fine.
    return null;
  }
}

export function setLastWorkspaceSlug(userId: string, slug: string): void {
  try {
    localStorage.setItem(keyFor(userId), slug);
  } catch {
    // See above.
  }
}

/** The remembered workspace if the user still belongs to it, else the first one, else null. */
export function resolveHomeWorkspace(
  workspaces: Workspace[],
  lastSlug: string | null,
): Workspace | null {
  return (
    workspaces.find((workspace) => workspace.slug === lastSlug) ??
    workspaces[0] ??
    null
  );
}
