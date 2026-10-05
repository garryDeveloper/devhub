import { createContext } from 'react';

import type { Workspace } from '../types';

export interface ActiveWorkspaceContextValue {
  /** Null while workspaces are loading, on error, or when the user has none. */
  activeWorkspace: Workspace | null;
  setActiveWorkspace: (workspace: Workspace) => void;
}

export const ActiveWorkspaceContext =
  createContext<ActiveWorkspaceContextValue | null>(null);
