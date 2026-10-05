import type { ReactNode } from 'react';
import { useCallback, useEffect, useMemo, useState } from 'react';

import { useAuth } from '../../auth/hooks/useAuth';
import { useWorkspaces } from '../hooks/useWorkspaces';
import {
  getStoredWorkspaceId,
  resolveActiveWorkspace,
  setStoredWorkspaceId,
} from '../lib/activeWorkspaceStorage';
import type { Workspace } from '../types';
import { ActiveWorkspaceContext } from './activeWorkspaceContext';

interface Resolved {
  userId: string;
  selectedId: string | null;
}

// Resolves which workspace is active and hands it to any screen that asks (DEVHUB-029). Nothing
// downstream needs to invalidate on a switch: every workspace-scoped query key is built from
// `activeWorkspace.id` (shared/api/queryKeys.ts `members`), so changing this value is itself what
// "resets workspace-scoped queries" means — React Query sees a new key and fetches it fresh,
// the same reasoning as web's DEVHUB-027 switcher.
export function ActiveWorkspaceProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const workspacesQuery = useWorkspaces();
  // `resolved` only updates from the async storage read (or a manual switch) — never
  // synchronously inside the effect — so a user switch doesn't flash the previous user's
  // workspace while the new user's stored id is still being read.
  const [resolved, setResolved] = useState<Resolved | null>(null);

  useEffect(() => {
    if (!user) return;
    let cancelled = false;
    void getStoredWorkspaceId(user.id).then((selectedId) => {
      if (!cancelled) setResolved({ userId: user.id, selectedId });
    });
    return () => {
      cancelled = true;
    };
  }, [user]);

  const storedIdLoaded = resolved?.userId === user?.id;
  const selectedId = storedIdLoaded ? (resolved?.selectedId ?? null) : null;

  const activeWorkspace = useMemo(() => {
    if (!user || !storedIdLoaded || !workspacesQuery.data) return null;
    return resolveActiveWorkspace(workspacesQuery.data, selectedId);
  }, [user, storedIdLoaded, workspacesQuery.data, selectedId]);

  const setActiveWorkspace = useCallback(
    (workspace: Workspace) => {
      if (!user) return;
      setResolved({ userId: user.id, selectedId: workspace.id });
      void setStoredWorkspaceId(user.id, workspace.id);
    },
    [user],
  );

  const value = useMemo(
    () => ({ activeWorkspace, setActiveWorkspace }),
    [activeWorkspace, setActiveWorkspace],
  );

  return (
    <ActiveWorkspaceContext.Provider value={value}>
      {children}
    </ActiveWorkspaceContext.Provider>
  );
}
