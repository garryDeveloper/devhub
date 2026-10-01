import { Navigate } from 'react-router-dom';
import { useAuth } from '../../features/auth/hooks/useAuth';
import { WorkspaceOnboarding } from '../../features/workspaces/components/WorkspaceOnboarding';
import { useWorkspaces } from '../../features/workspaces/hooks/useWorkspaces';
import {
  getLastWorkspaceSlug,
  resolveHomeWorkspace,
} from '../../features/workspaces/lib/lastWorkspace';
import { ErrorState } from '../../shared/components/ErrorState';
import { Skeleton } from '../../shared/components/Skeleton';

// `/` is not a page, it is a decision (DEVHUB-027): the workspace the user last used, else their
// first one, else onboarding. Lives in app/ because it joins two features — auth (who is this?)
// and workspaces (what can they see?) — and features never import each other.
export function HomeRedirect() {
  const { user } = useAuth();
  const workspaces = useWorkspaces();

  if (workspaces.isPending) {
    return (
      <div className="space-y-3" aria-label="Loading workspaces">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-24" />
      </div>
    );
  }

  if (workspaces.isError) {
    return (
      <ErrorState
        message="Could not load your workspaces."
        onRetry={() => void workspaces.refetch()}
      />
    );
  }

  const home = resolveHomeWorkspace(
    workspaces.data,
    user ? getLastWorkspaceSlug(user.id) : null,
  );

  if (!home) {
    return <WorkspaceOnboarding />;
  }

  // replace: `/` should not sit in history, or Back would bounce the user straight forward again.
  return <Navigate to={`/w/${home.slug}`} replace />;
}
