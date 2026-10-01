import { useMutation, useQueryClient } from '@tanstack/react-query';
import { qk } from '../../../shared/api/queryKeys';
import { createWorkspace } from '../api/workspacesApi';
import type { Workspace } from '../types';

export function useCreateWorkspace() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createWorkspace,
    onSuccess: async (created) => {
      // Written into the cache first, so the route we navigate to next can resolve the new slug
      // without waiting for a refetch — no "workspace not found" flash. Then refetched, so the
      // order and counts come from the server.
      queryClient.setQueryData<Workspace[]>(qk.workspaces, (old) => [
        ...(old ?? []),
        created,
      ]);
      await queryClient.invalidateQueries({ queryKey: qk.workspaces });
    },
  });
}
