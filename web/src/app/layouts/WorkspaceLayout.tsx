import { useEffect } from 'react';
import { Link, Outlet, useParams } from 'react-router-dom';
import { useAuth } from '../../features/auth/hooks/useAuth';
import { useWorkspaces } from '../../features/workspaces/hooks/useWorkspaces';
import { setLastWorkspaceSlug } from '../../features/workspaces/lib/lastWorkspace';
import { EmptyState } from '../../shared/components/EmptyState';
import { ErrorState } from '../../shared/components/ErrorState';
import { Skeleton } from '../../shared/components/Skeleton';

// The gate for every /w/:workspaceSlug/* route: nothing workspace-scoped renders until the slug
// is known to be one of the user's workspaces. An unknown slug — not a member, or no such
// workspace — gets the same "not found" either way, like the API's 404 (DEVHUB-024).
export function WorkspaceLayout() {
  const { workspaceSlug } = useParams();
  const { user } = useAuth();
  const workspaces = useWorkspaces();

  const workspace = workspaces.data?.find(
    (candidate) => candidate.slug === workspaceSlug,
  );

  // Remembered only once the slug is confirmed, so a mistyped URL never becomes the user's home.
  useEffect(() => {
    if (user && workspace) setLastWorkspaceSlug(user.id, workspace.slug);
  }, [user, workspace]);

  if (workspaces.isPending) {
    return (
      <div className="space-y-3" aria-label="Loading workspace">
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

  if (!workspace) {
    return (
      <EmptyState
        title="Workspace not found"
        description="It does not exist, or you are not a member of it."
        action={
          <Link
            to="/"
            className="text-sm font-medium text-blue-600 hover:underline"
          >
            Go to your workspaces
          </Link>
        }
      />
    );
  }

  return <Outlet />;
}
