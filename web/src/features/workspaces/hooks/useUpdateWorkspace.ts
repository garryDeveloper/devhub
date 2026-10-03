import { useMutation, useQueryClient } from '@tanstack/react-query';
import { qk } from '../../../shared/api/queryKeys';
import { updateWorkspace } from '../api/workspacesApi';
import type { Workspace } from '../types';

export function useUpdateWorkspace(workspaceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (name: string) => updateWorkspace(workspaceId, name),
    onSuccess: (updated) => {
      // The switcher, the shell title and this very page all read the workspace from the same
      // list query — one write here means every one of them shows the new name immediately.
      queryClient.setQueryData<Workspace[]>(qk.workspaces, (old) =>
        old?.map((workspace) => (workspace.id === updated.id ? updated : workspace)),
      );
    },
  });
}
