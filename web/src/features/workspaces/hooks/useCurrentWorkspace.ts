import { useParams } from 'react-router-dom';
import { resolveHomeWorkspace } from '../lib/lastWorkspace';
import type { Workspace } from '../types';
import { useWorkspaces } from './useWorkspaces';

// The URL is the tenant context: under /w/:workspaceSlug the workspace is whatever the URL says.
// Routes without a slug (/p/:projectKey, /notifications…) fall back to the last workspace used —
// until projects exist (EPIC 4) and a project can name its own workspace.
//
// Null when the list is not loaded yet, the user has none, or the URL slug is not one of theirs
// (WorkspaceLayout renders "not found" for that case).
export function useCurrentWorkspace(lastSlug: string | null): Workspace | null {
  // useMatch, not useParams: this runs in the shell, a parent route, and a parent's useParams
  // does not see the params of the child route that matched below it.
  const { workspaceSlug } = useParams();
  const { data: workspaces } = useWorkspaces();

  if (!workspaces) return null;

  if (workspaceSlug) {
    return (
      workspaces.find((workspace) => workspace.slug === workspaceSlug) ?? null
    );
  }

  return resolveHomeWorkspace(workspaces, lastSlug);
}
